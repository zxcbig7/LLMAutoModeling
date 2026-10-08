# Tuning 攻略：從框架證據推導下一輪假設

> 定位：給未來 tuning skill 使用的**非權威推理參考**。本檔回答「目前卡在哪裡、為何值得試這個方向、什麼結果會推翻假設」。
> 進場、可寫範圍、候選池、實驗設計、勝負、promotion 與停止規則，一律依 [solver-tuning-guide.md](solver-tuning-guide.md)；本檔不另設門檻或擴大授權。
> 查證基準：2026-10-08 的 OptimFoundation source；CPLEX 語意優先參照 IBM 22.1.1。引用 22.1.2 的補充資料會另外標示，不能視為已在本機實測。

## §1 使用方式：先形成可被否證的解釋

不要從「哪顆參數常常有效」開始。先回答五件事：

1. **證據有效嗎？** 本輪的模型、資料、停止契約、環境、量測方式與 baseline 是否一致。
2. **終點卡在哪裡？** 沒有 incumbent、incumbent 品質不足、bound 證明不足，或已達標但耗時。
3. **時間花在哪個階段？** 模型套用、presolve、root relaxation、root cuts / heuristics、tree；框架沒提供的部分要承認未知。
4. **候選會作用在那個階段嗎？** 參數存在不代表這次求解有機會用到。
5. **改了以後應看見什麼？** 同時預測 solver 行為、正式結果與可能代價，不能只寫「預期更快」。

把推理直接寫進正式 `TuningHistory.md`：

```text
觀測：哪個 artifact、trial、seed、欄位或 log 片段支持症狀。
假設：目前最可能的瓶頸，以及至少一個尚未排除的替代解釋。
候選：現行 profile 允許的單一旋鈕；baseline 值 → candidate 值。
機制預測：哪些可觀測行為應改變；哪些可能變差。
結果預測：預期影響有解率、Optimal、SolveTimeMs 或 Gap 的哪一層。
反證：看到什麼，就不能繼續用原假設解釋。
裁決：實跑後依 guide 讀 eligibility、VsBaseline、summary 與 holdout。
```

上述是寫作框架；正式必填內容仍依 guide §3.0.1、§6.2。**設定影響行為是待驗假設，框架判出 win 也不自動證明因果。**

## §2 框架能回答哪些問題

### 2.1 先找對本輪 artifact

`OptProject.Experiment(name, description)` 建立 `OptExperiment`；`Run()` 的輸出前綴是 `{Project}-{Name}`。例如 experiment 名 `tuning-r2`，檔名是 `<Project>-tuning-r2-trial.csv`，不要把專案名再塞進 experiment 名。

| 證據 | 適合回答 | 不應拿來代替 |
| --- | --- | --- |
| `-trial.csv` | 每個 trial 的最終狀態、目標、bound、gap、時間、節點、迭代、軌跡摘要與 `VsBaseline` | 模型正確性驗證、完整 termination reason |
| `-meta.csv` | 模型組成、objective sense、baseline 明設設定、機器與缺值標記 | 資料 fingerprint、solver 全部有效預設值 |
| `-summary.csv` | 各 config 的 `Trials`、`FoundSolution`、`Wins`、`Losses`、`Ties`、`NotCompared` | 自動選 champion、品質 gate、promotion |
| `-trajectory.csv` | 取樣點上的 incumbent / bound / gap 變化 | 真正首解事件、完整搜尋歷史、最終結果 |
| framework log / CPLEX log | 實際 search method、停止原因線索、root / presolve / cuts / numerical 訊息 | 未輸出的統計欄位 |
| `TuningHistory.md` | 契約、版本、來源、假設、已否證方向、archive 與驗證結果 | 未保存的原始實驗證據 |

讀 CSV 使用 `Import-Csv` 或 CSV parser，先篩選目標 trial 與必要欄位，不以逗號切字串，也不整份塞入 context。trajectory 沒有 `Model` 欄，須用 `TrialId` 連回同一實驗的 trial 主表，不能只靠 label 跨模型配對。

目前 trial CSV 沒有 `Experiment`、`RunId`、`RootTimeSec` 或 `TFirstIncumbent` 欄位。歷史專案可能使用另一版 schema；先看 header，再依本版檔名定位，不能照舊腳本猜欄位。

同名 experiment 再跑會整組覆寫；這不是追加資料的資料庫。檔案被鎖住時 writer 可能改寫 `-locked-<timestamp>` 路徑，須核對 log，避免讀到原路徑的舊結果。archive 的不可變與命名規則依 guide §3.3。

`OptExperiment.Run()` 依序跑 trial，全部完成後才保存；途中 build / solve 丟例外會向外拋出，**不保證替失敗 cell 寫出一列 Error 或保存前面成功的 trial**。不完整或舊 CSV 不能支持本輪裁決。

來源：[OptExperiment.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptExperiment.cs)、[Experiment.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/Experiment.cs)、[ExpCsvWriter.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/ExpCsvWriter.cs)。

### 2.2 欄位的問題意識

| 欄位／設定 | 實際意義 | 適合問的問題 |
| --- | --- | --- |
| `ModelType` 與 meta 的變數、constraint、SOS 等數量 | CPLEX 實際接收模型的分類與組成 | 走 LP 還是 MIP？參數的適用前提存在嗎？ |
| `Status`、`ObjectiveValue`、`BestBound`、`Gap` | 求解結束後的結果 | 有 incumbent 嗎？距離停止契約還差在哪一側？ |
| `SolveTimeMs` | `Model.Solve()` 前後的 CPLEX clock 差 | solver 這段花多久？ |
| `BuildAndSolveTimeMs` | `model.ApplyTo` 耗時加上 `SolveTimeMs` | 模型套用是不是另一個顯著成本？ |
| `NodeCount`、`IterationCount` | 可取得時的節點與迭代數 | 時間差是否伴隨 tree / LP 工作量改變？ |
| `FirstSolutionMs` | callback 首次觀察到非 NaN objective 的時間 | 在觀測範圍內，incumbent 何時開始可見？ |
| `BoundChange` | 最後取樣 bound 減去第一取樣 bound | 觀測區間的 bound 有沒有淨移動？ |
| `LastBoundChangeMs` | 相鄰取樣 bound 不相等的最後時點 | 觀測區間內，最後可見變動在何時？ |
| `ConfigChanges` | 相對 baseline 的明設 solver 設定差異，忽略 seed | 這個 trial 實際比較了哪個設定差異？ |
| `VsBaseline` | 框架與同 model、同 seed baseline 的比較 | 本 trial 在既定比較順序下是 win、lose、tie 還是 n/a？ |

`SolveTimeMs` 不含匯出模型、IIS、solution export、log flush、CSV 寫檔；`BuildAndSolveTimeMs` 也不含引擎建立與套參數等完整程序成本。兩者都不是整支程式的 wall time，不能把 production 體感延遲全歸給 solver。

meta 的模型組成取同名 model 的首個有 metrics trial，不是每筆結構一致性稽核。meta 也沒有 input checksum、commit、精確 CPLEX 版本或完整硬體設定；這些比較前提要由 History 與實際輸入來源保證。

來源：[SolveMetrics.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/SolveMetrics.cs)、[OptEngine.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.cs)。

## §3 先理解觀測界線，才判斷瓶頸

### 3.1 缺值、無解與 LP

| 讀到的值 | 可以說 | 不能說 |
| --- | --- | --- |
| 軌跡摘要 `off` | 未啟用軌跡，且沒有點 | 搜尋沒有進展 |
| 軌跡摘要 `n/a` | 已啟用但沒有點 | 一定沒有 incumbent |
| 軌跡摘要 `none` | 有點，但對應 getter 沒有結果 | 該現象在整個求解期間從未發生 |
| `BoundChange = 0` | 首末取樣 bound 相等；只有一點也會得到 0 | 全程沒有改善、所有 cuts 無效 |
| `LastBoundChangeMs = 0` | 有一個實際數值 0 的結果 | 等同 `none` 或事件沒發生 |
| `NaN`、`Infinity`、`-Infinity`、trajectory 的 `#N/A` | 數值不適合直接作為有限量解讀 | 可以當 0、可以拿來算改善 |
| `NodeCount` 或 `IterationCount = n/a` | 這次未取得計數 | 節點／迭代數是 0 |

`Status = TimeLimit` 的三個終值 `ObjectiveValue`、`BestBound`、`Gap` 會全部是 NaN。這不表示搜尋從未產生 bound；框架在沒有可用解時不讀取這三個終值，過程證據要回 trajectory / log 查。

目前框架把 CPLEX 的 `Unknown` 概括映射成 `TimeLimit`，所以不能僅憑名稱斷定撞了 wall-clock 時限。同理，框架 `Infeasible` 也包含 CPLEX 的 infeasible-or-unbounded 狀態；需要 log 補足細分原因。

成功的純 LP 目前寫 `BestBound = ObjectiveValue`、`Gap = 0`；這是框架處理非 MIP 的方式，不能拿來證明「MIP bound 很強」。純 LP 沒有此處的 MIP 軌跡；`Optimal` 搭配 `FirstSolutionMs = n/a` 完全可能正常。

對無可用解的 MIP，先用最終狀態確認 no-incumbent，再解讀可取得的軌跡；不要因為 callback 沒有點、`FirstSolutionMs` 不是 `none`，就排除這個症狀。

### 3.2 取樣不是完整事件紀錄

目前 MIP callback 在 incumbent 值改變，或距上次記錄至少約 200 ms 時留下點，最多 2,000 點。**200 ms 不是固定頻率 timer**：CPLEX 決定何時呼叫 callback，單純 bound 改變也不保證立即記點。

因此：

- 沒有補終止點；末點可能早於 solve 結束，達到點數上限後更可能漏掉後半段。
- `FirstSolutionMs` 是「首次看見」而非精確首解事件；不能代替 root time。
- `BoundChange` 只比較取樣首尾，不是 root gap，也不是整場 bound 改善；min 的改善通常往上，max 通常往下。
- `LastBoundChangeMs` 以 double 不相等判斷，沒有 numerical tolerance；NaN 與 NaN 也會判不相等。非有限點未排除前，不能把它當可靠停滯時刻。
- `FirstSolutionMs` 的 getter 只排除 NaN，未排除 Infinity；讀到數字也要確認是有限值。
- 正式終值與勝負看 trial metrics，trajectory 負責解釋過程；兩者不同不代表可以覆蓋 trial 結果。

`CaptureTrajectory` 預設開啟，MIP informational callback 可能改變搜尋路徑與成本。本輪所有候選須保持相同量測方式，採用前依 guide 完成不擷取軌跡的驗證，不能把「關掉觀測」混成搜尋參數改善。

`ProjectConfig.Quiet()` 不等於沒有 log：目前 CPLEX output / warning 仍會收集並寫入 framework log。找 root 與 numerical 證據時，先按 trial 標記抽取需要的片段。

### 3.3 這些結論目前無法只靠 CSV 得到

- 精確 root relaxation time、root node processing time、presolve time、cut family 成本、node LP 平均成本、RSS 峰值。
- incumbent 的真正生成時刻、每個軌跡點對應的節點／root 階段、完整 deterministic ticks。
- 「某個參數必定沒生效」、「某個策略造成了改善」或「這份設定可泛化到別的 instance」。
- 模型／資料完全相同、沒有跨 trial 污染，以及 production 已正確套用 champion。

若需要 root 證據，讀 solver log 的原始區段與單位。root relaxation 求解與完整 root processing 不是同一段工作，不要混寫成一個自造的 `RootTimeSec` 指標。

## §4 診斷順序：先排除錯題，再選方向

### 4.1 可比性與配對

先核對同一模型、輸入、停止契約、環境、warm start 與軌跡設定；接著確認 baseline 與每個 candidate 都有同組 seed 的完整結果。

框架會用含 `baseline` 的第一個非 warmup label 找基準；找不到時退回第一個非 warmup trial。含 `warmup` 的 label 會被排除。它不會替你自動建立暖機或自動展開 seed。

config family 由 label 尾端的 `-s<數字>` 去除後形成；實際配對則使用 solver config / metrics 的 seed。名稱尾碼與實際 `Seed` 要一致，不能靠名字假裝已設定 seed，也不要重複同 model / seed 的 baseline 讓 `FirstOrDefault` 靜默挑一筆。

`NotCompared`、缺 trial、異常失敗、重複配對或契約不同，首先是實驗有效性問題；不應立即歸類成某顆策略無效。

### 4.2 分開「結果症狀」與「成本位置」

| 結果症狀 | 第一個問題 | 補充證據 |
| --- | --- | --- |
| MIP 沒有 incumbent | 可行性曾被驗證嗎？這次停在何種限制？ | Phase 2 驗證、Status、log、軌跡可見 bound |
| 有 incumbent，但品質長期不改善 | bound 已接近、還是兩側都卡住？ | 終值與不同時點的 objective / bound |
| incumbent 早出現、bound 停滯 | 是弱 relaxation、cuts 投入不足，還是 node 太貴？ | 有限軌跡點、root / cuts / tree log、總時間 |
| 已達 Optimal 但慢 | 慢在模型套用、root 還是 tree？ | 兩個時間欄、log、nodes / iterations |
| 解在 root 附近結束 | 實際有多少 tree 工作可供分支策略改善？ | ModelType、NodeCount、root 與搜尋結束 log |
| 出現 numerical warnings | 先處理數值穩定性還是模型／資料問題？ | warning 原文、係數與 bounds 的來源 |

guide 的 Primal-search / Dual-bound / Node-cost 判準沒有固定「大、小、早」百分比門檻。不要自創 `FirstSolutionMs / SolveTimeMs > 0.5` 等硬 gate；說明同契約 baseline 的具體觀測、替代解釋與信心。

節點多不等於差，節點少不等於快。`IterationCount / NodeCount` 最多是粗略線索，不能當精確每 node LP 成本；計數可能含其他階段，且 `NodeCount = 0` 時不能相除。

### 4.3 參數能力與 workflow 授權是兩層

每次選候選先查 guide §2.2 的 profile 列，再查 [CplexConfig.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs) 與 [OptEngine.Configuration.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.Configuration.cs) 的型別、值域、mapping。

- `null` 是不設參數，不是 0；0、-1、false 的含義由各參數決定。`NodeSelect = 0` 是 depth-first，不能通稱「0 = auto」。
- snapshot 保存非 null 的明設設定，不是 solver 全部有效值的 dump。預設值會受版本影響，History 要記版本與比較契約。
- `Presolve` 與 `PreIndicator` 是同一底層設定的別名，不能當兩顆獨立因素。
- `MipSearch = 1` 雖可設定，代表 traditional search，會違反現行 guide 的 dynamic-search gate。
- IBM 22.1.1 與 wrapper 支援 `Emphasis = 5`，但現行 profile 候選列只安排 0–4 的方向；本攻略不自行擴充到 5。
- `NodeCuts`、`McfCuts` 有 wrapper mapping，且列在 guide 全池；它們未列入 §2.2 的直接 profile 候選，不能因 API 存在就自行插入掃描。
- `BendersStrategy = 1/2/3`、MIP 的 `OptimalityTarget = 2` 有模型條件拒絕風險；依 guide §2.2.2 排除，不把它們當一般 MILP probe。
- `Tuning*`、`SolutionPool*` 有設定欄位，不表示 `Run()` 會自動呼叫 tuner 或 Populate；目前路徑執行的是 Solve。

## §5 方向 playbook：每次只驗一個假設

下列值是**候選假說，不是推薦最佳值**；實際輪次與先後依 guide。每次改動前都要先確認 baseline 現值、適用階段、候選池及歷史去重。

### 5.1 連第一個 incumbent 都沒有

**證據**：已驗正確的 MIP，在固定契約下無可用解；log 排除執行錯誤，並確認真正停止原因。終值 NaN 不等於沒有搜尋 bound。

**假設與單旋鈕**：搜尋投入偏向證明、找可行解不足。依 No-incumbent 列試 `Emphasis = 4`，後續才依 guide 比較 `1` 等方向；不要同輪再加 heuristic 或 cuts。

**預測與反證**：預期某些 baseline 無解的 seed 出現 incumbent；若仍無解，且 log 顯示大部分時間尚在 root relaxation，則「tree 中找解投入不足」的解釋不成立，應先辨識 root 成本。

**副作用**：偏重可行解可能延緩 bound 與 proof；首解較早也可能品質不佳。正式勝負仍由框架與 guide 裁決。

RINS 需要已有 incumbent，不能把「增加 RINS」當從零生出首解的直接機制；它可能在首解出現後才有意義。此前提參照 [IBM 22.1.2 RINS](https://www.ibm.com/docs/en/icos/22.1.2?topic=parameters-rins-heuristic-frequency)；emphasis 語意見 [IBM 22.1.1](https://www.ibm.com/docs/en/icos/22.1.1?topic=optimizer-emphasizing-feasibility-optimality)。

### 5.2 有 incumbent，但改善太慢

**證據**：最終有解；多個可用取樣顯示 objective 停滯，bound 仍有進展，且尚有搜尋預算。

**假設與單旋鈕**：現有解附近的搜尋投入不足。依 Primal-search 列單獨試 `RinsHeuristicFrequency`，或在另一輪試 `HeuristicEffort`；先確認有 incumbent 與足夠作用時間。

**預測與反證**：預期 incumbent 改善出現在過程中，終值 objective 不退步；若 heuristic / subMIP 花更多時間，最後品質與 gap 卻未改善，就不支持增加這種投入。

**副作用**：啟發式會消耗原本給 tree / bound 的時間，不能只把某次首解提早當成功。

若 baseline 已把 `HeuristicFrequency` 設為非預設，先把 `HeuristicEffort` 候選標為相容性未滿足；不能暗中重設另一顆湊成「一輪一顆」。IBM 明言兩者同時非預設的行為 undefined。來源：[IBM 22.1.1 HeuristicEffort](https://www.ibm.com/docs/en/icos/22.1.1?topic=parameters-mip-heuristic-effort)。

### 5.3 incumbent 早出現，bound 長期不動

**證據**：多個有限取樣點、終值與 log 一致支持 dual 側停滯；先排除只有一點、2000 點上限、numerical 假變動與取樣未涵蓋尾段。

**假設與單旋鈕**：證明端投入不足。依 Dual-bound 列先試 `Emphasis = 3`；另一輪才測一個 cut family，例如 `MirCuts`，不能一次把所有 cuts 開大。

**預測與反證**：預期同契約下 bound 更接近可行 objective、gap 更小，或更早達 Optimal。若 cuts 增多、root 時間增加，但可見 bound 與終值沒有改善，則不支持加強該 family。

**副作用**：更強 cuts 可能增加 LP 成本，`Emphasis = 3` 可能犧牲 incumbent 改善。比較前仍要通過 incumbent 品質不退步的 gate。

`Emphasis = 2` 是 optimality，`3` 才是 best-bound emphasis；不是數字越大越強。cuts 的支援級數因 family 而異，不要一律填 3。來源：[IBM emphasis](https://www.ibm.com/docs/en/icos/22.1.1?topic=optimizer-emphasizing-feasibility-optimality)、[IBM cuts](https://www.ibm.com/docs/en/icos/22.1.1?topic=cuts-parameters-affecting)。

### 5.4 root cuts / preprocessing 花很多時間

**證據**：CSV 只能提示「解很慢、tree 很少」；需要 log 區分 root relaxation、cut passes、probing 與 presolve，不能拿首個軌跡時間代替。

**假設與單旋鈕**：某個 root 工作成本高於帶來的後續節省。若符合 Node-cost 的減 cuts 方向，先測一個已允許的 cut family 關閉值；`CutPasses` / `Probe` 的測試仍須符合當前 profile 候選，不因 log 有名稱就擴池。

**預測與反證**：預期該段工作減少且總 solve 有收益；若 root 變快但後續 tree 變大、最終 gap 變差，便推翻「這些工作只是浪費」的假設。

**副作用**：減 cuts / presolve 可能削弱 relaxation，節省 upfront 時間卻付出更大搜尋成本。

`CutPasses` 正值限制 root passes；-1 的官方說明涉及關閉 cut separation，不能宣稱只是精準關掉 root。若討論 `NodeCuts`，其 -1 只關 tree cuts、保留 root；這是辨識語意，不是本攻略授權新候選。來源：[IBM cuts](https://www.ibm.com/docs/en/icos/22.1.1?topic=cuts-parameters-affecting)、[IBM presolve](https://www.ibm.com/docs/en/icos/22.1.1?topic=mip-preprocessing-presolver-aggregator)。

### 5.5 LP relaxation / node 工作昂貴

**證據**：log 指向 continuous solve 成本，搭配時間、nodes、iterations；不能僅由節點少就判 node 太貴。

**假設與單旋鈕**：目前 LP algorithm 不適合這個結構。在允許的 Node-cost 列，單獨比較 `RootAlgorithm` 的 2（dual simplex）或 4（barrier）；若證據指向後續 continuous subproblems，再於另一輪測 `NodeAlgorithm`。

**預測與反證**：預期對應 solve 區段與總 solve 時間改善；若 barrier iterations 很少但 crossover / 後續 tree 拖長整場，不能宣稱 algorithm 較好。

**副作用**：barrier 的記憶體與 crossover 成本、不同 relaxation 路徑都可能改變後續搜尋。`NodeAlgorithm` 也可能影響 root cut 後再求解，所以 NodeCount 為 0 不足證明它未生效。

來源：[IBM 22.1.1 initial MIP relaxation](https://www.ibm.com/docs/en/icos/22.1.1?topic=parameters-algorithm-initial-mip-relaxation)、[IBM solving subproblems](https://www.ibm.com/docs/en/icos/22.1.1?topic=optimizer-solving-subproblems)。

### 5.6 純 LP 已達標，但仍耗時

**證據**：`ModelType = LP` 並核對 meta 組成；MIP 的 incumbent / cuts / branching 剖面不適用。

**假設與單旋鈕**：連續演算法與矩陣結構不匹配。依 guide 的 LP 列，先單獨比較 `RootAlgorithm`；選定 barrier 方向後，才討論有條件適用的 barrier 內部參數。

**預測與反證**：預期在相同品質契約下更快達 Optimal；如果變化只在模型套用時間而 solve 幾乎不變，就不支持 solver algorithm 是主因。

**副作用**：不同演算法的 memory、crossover、迭代單位不同；不能跨演算法直接比 iteration 數量判優劣，也不能用 LP gap=0 宣稱品質更好。

本 wrapper 用 `RootAlgorithm` 表達此選擇，不存在另一顆可憑 C API 名稱猜出的 `LPMethod`。LP 語意補充來源：[IBM 22.1.2 continuous linear problems](https://www.ibm.com/docs/en/icos/22.1.2?topic=parameters-algorithm-continuous-linear-problems)。

### 5.7 數值警告反覆出現

**證據**：log 有 singularity、feasibility loss 或反覆 perturbation 等具體訊息；只看到 runtime 波動不算 numerical 證據。

**假設與單旋鈕**：數值困難導致反覆修復。依數值不穩列試 `NumericalEmphasis = true`，保留停止與可行性／整數容差。

**預測與反證**：預期警告或修復模式減少，解仍通過驗證；若警告持續、結果不變式破裂或 bound 矛盾，應回到資料／模型／框架診斷，不能繼續加旋鈕掩蓋。

**副作用**：更謹慎計算可能耗更多時間與記憶體；穩定性改善不等於速度改善，也不保證模型正確。

係數尺度、Big-M、bounds 若需要修改，依 guide 退回對應 phase。來源：[IBM numerical emphasis](https://www.ibm.com/docs/en/icos/22.1.1?topic=parameters-numerical-precision-emphasis)、[IBM numeric difficulties](https://www.ibm.com/docs/en/icos/22.1.1?topic=problems-numeric-difficulties)。

### 5.8 看起來是記憶體或平行資源問題

**證據**：log / 執行環境顯示 node file、資源不足或外部求解互相競爭；CSV 沒有 RSS，不能自行補出記憶體峰值。

**假設與方向**：資源配置不合機器或模型，而非搜尋旋鈕失效。依 guide §2.3 的環境定版處理 `Threads` 等設定，不能混入策略輪當 candidate。

**預測與反證**：預期資源競爭或儲存切換減少，整體工作可在定版環境穩定量測；若增加 threads 只增加同步／記憶體成本，便不支持「核心越多越快」。

**副作用**：`MemoryLimitMb` 對應 WorkMem，不是 process RSS 硬上限；framework 套用它時還會設定 node-file strategy=0，之後明設 `NodeFileStrategy` 才覆蓋。這是隱含交互，不可只看 property 差一顆。

`ParallelMode = 1` 有助可重現路徑，但固定 seed 不能保證 wall-clock 截止時停在完全相同位置。來源：[IBM parallel mode](https://www.ibm.com/docs/en/icos/22.1.1?topic=parameters-parallel-mode-switch)、[IBM threads](https://www.ibm.com/docs/en/icos/22.1.1?topic=threads-parameter)。

## §6 設計能學到東西的一輪

### 6.1 單旋鈕不是只看賦值行數

先列 baseline → candidate 的完整差異，再確認底層 mapping 與相容性。`Presolve` 別名、`MemoryLimitMb` 副作用、heuristic 參數互斥，都可能讓「只寫一行」不是單純的因果實驗。

同一輪可比較同一旋鈕的少量不同值；不要同時 sweep emphasis、cuts、heuristics。某值沒有新機制理由、適用條件未成立，或有效 config 已做過，就沒有必要只為填滿候選數再跑一次。

先檢查現行 guide 的已否證與去重規則；新的 seed 或 label 不構成新方向。要測組合效果也應沿 guide 的 baseline / promotion 閉環，不能把幾個歷史上贏過的設定一次堆起來。

### 6.2 把正式勝負與機制證據分開記

框架目前依序比較：有解勝無解；都有解時 Optimal 勝非 Optimal；都 Optimal 比 `SolveTimeMs`；都 Feasible 比 `Gap`；都無解則 tie。**它不直接比較 objective 品質，也不檢查所有契約與正確性。**

時間或 gap 精確 double 相等才 tie，沒有實務改善幅度門檻。極短求解的微小時間差也可能產生 win；讀取結果時不能擅自把它改判 tie，也不能把 win 當成機制已改善。

正式裁決依 guide §4：先 eligibility，再讀 `VsBaseline` 與 summary；5 個 tuning seed 的 `Losses = 0`、`Wins ≥ 3`，且配對完整，才有後續 holdout 資格。holdout、production 驗證不可省略。

| 正式結果 | 機制證據 | 應記錄的解讀 |
| --- | --- | --- |
| 勝出 | 行為符合預測 | 假設獲支持，仍須通過後續 gate |
| 勝出 | nodes / iterations / 軌跡近似不變 | 勝出存在；原因未確定，可能是量測變異，保留疑點並走 holdout |
| 未勝出 | 預測行為確實改變 | 旋鈕可能有效作用，但代價抵銷收益；不能說它未生效 |
| 未勝出 | 觀測不足或作用前提未滿足 | 本輪不足以支持方向；記清缺口與不適用條件 |
| 結果異常 | 解品質或 bound 不變式破裂 | 先查正確性／契約，不進效能排名 |

不要另外計算平均、百分比、綜合分數或自行重比勝負。需要描述差異，就引用同 seed 的兩個原始值及來源欄位。

### 6.3 保存負面知識，但不把推論寫成定理

每個方向至少留下「測了什麼、作用前提、觀測結果、guide 裁決、尚未排除的解釋」。區分未測、不適用、已測未勝出、結果異常，以及 guide 已判定整類否證。

guide §7.1 B 對 bound 軌跡起訖一致有整類否證／跳過政策，執行仍遵守該政策；敘事上不要額外推論成「所有 heuristic 在任何模型都無效」。相同首末 bound 無法單獨證明中途路徑或 primal 側完全相同。

達到 guide 的候選耗盡、無勝出輪數、預算或異常停止條件，就按規範收尾。retain + 有來源的反證，比沒有可靠理由的 champion 更有參考價值。

## §7 四個完整思考例

以下數字與情境**全為虛構教學例**，不代表本 repo 的 benchmark 或已驗證參數。

### 例 1：無解終值 NaN，卻有 bound 軌跡

**觀測**：MIP 的 baseline s11 顯示 TimeLimit，objective / bound / gap 都是 NaN；trajectory 有有限 bound 點，objective 為 `#N/A`。log 確認時間限制；小 instance 已完成正確性驗證。

**推理**：可以說此 trial 沒有可用 incumbent，不能說 solver 沒有 bound。trajectory 的數值不足以取代終值或創造 gap。先走 No-incumbent 方向。

**計畫**：依 guide 測 `Emphasis = 4`，其餘契約不變。預期至少部分 seed 出現可用 incumbent；若 log 顯示仍把預算耗在 initial relaxation，便不支持「只需加強找整數解」的解釋。

**回填**：假設 summary 顯示 Wins=3、Losses=0、Ties=2、NotCompared=0，僅表示達本輪勝出條件；仍須 eligibility / holdout。若首次取得 incumbent 改變進場情境，依 guide 重建 baseline 證據。

### 例 2：bound 推進了，整體卻沒贏

**觀測**：min 模型 s22，baseline 的 incumbent=110、bound=90；候選的 incumbent=110、bound=96。候選用單一更強 cut family，log 顯示 root 工作增加。

**推理**：dual 行為支持「該 cut 有助界限」；不能僅因此判 champion，也不能直接手算 gap，應讀 trial 的 solver gap 與 `VsBaseline`。

**回填**：假設 summary 為 Wins=2、Losses=1、Ties=2。依 guide retain。「新增成本抵銷界限收益」只是待查解釋；須回到敗方 seed 的 Status、Gap、incumbent 與 log 找原因。兩邊都 Feasible 時框架比較的是 Gap，不能僅憑 root 變慢解釋 lose，也不能把「cuts 無效」當成跨模型定理。

**下一步**：依已否證與候選順序決定是否還有合法方向；不把 cuts、emphasis、probe 一起加上試運氣。

### 例 3：短解題出現很多 win，但機制沒有改變

**觀測**：BP，NodeCount=0；baseline s33 為 125 ms，candidate 為 109 ms；兩者 iterations 與可見軌跡相同。summary 顯示 4 win、0 lose、1 tie。

**推理**：正式 win 不可改判；目前沒有足夠證據說旋鈕減少了 solver 工作。NodeCount=0 也不代表 LP，更不代表所有 root / continuous 參數都無效。

**驗證**：走 guide holdout，另保留 log 證據。假設 holdout 有 1 lose，裁決 retain；不要為保留 champion 改用平均值、刪掉那個 seed 或事後自訂 tie tolerance。

**負面知識**：記成「此設定未通過 holdout，機制未證實」，並按 guide 停止政策處理後續方向。

### 例 4：LP 沒有軌跡，慢的是模型套用

**觀測**：ModelType=LP，Optimal，Gap=0，FirstSolutionMs=n/a；SolveTimeMs=900，BuildAndSolveTimeMs=12000。log 沒有數值異常。

**推理**：無軌跡是正常可見性限制，不是 No-incumbent。兩個時間欄顯示模型套用可能是主要成本，但仍不能把 12000 ms 當完整程式 wall time。

**計畫**：若需要驗證 solver algorithm 假設，可依 LP 列單獨測 RootAlgorithm；預測 solve 段可能變化，模型套用成本不會因此消失。

**回填**：若 solve 小幅改善而主要成本仍在套用，明確回報 tuning 對整體需求的限制。修改模型組裝或 framework 是另一項工作，不能在本輪順手進行。

## §8 維護與查證入口

本攻略描述目前可觀測能力。framework 的 CSV、callback、snapshot、比較或參數 mapping 改動時，先更新證據解讀；不要用舊 benchmark 的 header 或數字替現行 source 定義行為。

| 要核對的問題 | 直接來源 |
| --- | --- |
| Workflow、profile 候選、實驗與停止 gate | [solver-tuning-guide.md](solver-tuning-guide.md) |
| API 與產物概念 | [developer-guide.md](../../../../OptimFoundation/OptimFoundation/specs/developer-guide.md) |
| trial 排程、clone、例外、輸出命名 | [OptExperiment.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptExperiment.cs) |
| baseline、seed、summary 與比較順序 | [Experiment.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/Experiment.cs) |
| CSV 欄位、marker 與鎖檔行為 | [ExpCsvWriter.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/ExpCsvWriter.cs) |
| 軌跡摘要 getter | [SolveMetrics.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/SolveMetrics.cs) |
| Status、LP/MIP metrics、callback 與量測範圍 | [OptEngine.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.cs) |
| 參數名稱、型別、別名與實際 mapping | [CplexConfig.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs)、[OptEngine.Configuration.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.Configuration.cs) |
| snapshot 保存內容 | [ConfigSnapshot.cs](../../../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/ConfigSnapshot.cs) |

若 guide 的硬規則與現行能力出現衝突，記錄具體來源並依規範處理；本攻略不默默開例外。遇到框架沒有的觀測，就把未知寫出來，讓下一輪回答一個更小、可驗證的問題。
