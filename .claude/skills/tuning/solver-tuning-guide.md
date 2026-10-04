# Solver Tuning — Phase 3 端到端調校規範

> **這份文件是什麼**：模型與資料已凍結、正確性已驗的專案跑太慢或收斂不了時，怎麼系統性地調 CPLEX solver 旋鈕，從「進場判斷」到「champion 寫回 production baseline 並驗證」的完整標準流程。
> **怎麼用**：從 §0 的進場 gate 開始，**gate 沒過就不要往下讀**。gate 的第一件事是分 §0.0.1 的三個進場情境——**情境決定每個 seed 實際比到哪一項（A 比時間、B 比 gap、C 比有沒有找到解），也決定結果不變式怎麼驗**。旋鈕分類查 [`cplex-parameter-reference.md`](cplex-parameter-reference.md)，實際 property 與型別查 sibling `CplexConfig.cs` / developer guide，NEVER 憑記憶寫欄位名。
> **前置**：專案已通過 Phase 2 的解驗證協定四步（`status.json` 的 `solveVerified: true`），且**使用者主動提出**效能問題。求解**沒有**收斂到 `Optimal`（撞時限停在 `Feasible`，甚至沒找到任何解）**不是進場的阻礙，而是最典型的進場理由**——見 §0.0.1。
> **本檔自足**：讀這一份就能從症狀走到 promotion 完成，流程判斷不需要開其他文件。天條全文在 [`../AGENTS.md`](../AGENTS.md)；Experiment / Config 的 **API 簽名、runner 行為、exp 分支的 R0-ready 形狀契約**在 [`../coding/optimfoundation-api-guide.md`](../coding/optimfoundation-api-guide.md) §8–§9，本檔 NEVER 複製那一份的內容。

### Canonical 邊界（AI MUST 先判斷）

- **本檔是 Phase 3 的唯一權威**。進場條件、可動範圍、實驗設計、評分規則、promotion 流程一律以本檔為準。
- **權威順序**：[`../AGENTS.md`](../AGENTS.md) 的天條 → 本檔 → [`../coding/optimfoundation-api-guide.md`](../coding/optimfoundation-api-guide.md) §9 的框架簽名 → 任何既有專案 code。
- **只有一條路線**：調 `CplexConfig` 的 solver 旋鈕。沒有第二種手段。落在 §1 判定表其他格的訴求一律退回對應 phase。

---

## 術語與符號表

本表是本檔所有 tuning 專有名詞、符號與縮寫的**唯一集中定義**。`CplexConfig` 的分類看 [`cplex-parameter-reference.md`](cplex-parameter-reference.md)，個別 property、型別與語意以 sibling `CplexConfig.cs` / developer guide 為準。

### 流程與證據

| 名詞 | 定義 |
| --- | --- |
| **tuning 週期** | 在同一模型、資料、停止契約與環境契約下，從 R0 校準開始，到 promotion 或 retain 收尾為止的一段可比較實驗序列。任一契約／環境變更即結束舊週期，必須重跑 R0。 |
| **round／R<N>** | 一輪正式策略實驗與其分析、裁決、History 紀錄；`N` 為遞增整數。R0 是校準輪，不計入正式 round。 |
| **R0 校準輪** | 不改任何搜尋策略旋鈕，只重跑 baseline 的多 seed 量測；確認進場情境、收斂剖面與契約健檢，也記下 baseline 每個 seed 的表現，是進入 R1 的硬 gate。 |
| **baseline** | 當前 production `CplexConfig` 的完整設定，也是每輪比較的對照組與 variant 的起點。 |
| **production baseline** | `Program.cs` 中正式 solve 路徑實際使用的具名 `CplexConfig`。只有 promotion 通過後才能寫回它。 |
| **variant** | 從 baseline 複製、在一輪中只改一個搜尋策略旋鈕的 candidate config；不是另一份平行 baseline。 |
| **config snapshot** | 某個 baseline 或 variant 在執行時真正生效的完整 `CplexConfig` 值。它必須 materialize，不能只記「從當時 baseline Clone」。 |
| **exp 分支／round config archive** | `Program.cs` 中只供 `-- exp` 執行的實驗設定區塊；每個已跑 round 的 config snapshot 都保留在此，供重跑與稽核。 |
| **experiment** | 一次具名的 `OptExperiment` 執行；`project.Experiment(name, ...)` 的 `name` 固定為 `tuning-r<N>`（不含專案名），每輪遞增；log 檔名、紀錄檔名與 marker 用的 `FullName` 才是 `<Project>-tuning-r<N>`。會保存 Trial、config、metrics 與 convergence 證據，寫成 `Experiment/<Project>-tuning-r<N>-{trial,meta,summary,trajectory}.csv` 一組檔。 |
| **trial／cell** | experiment 中一次「特定 model × 特定 config × 特定 seed」的單次求解與量測結果。 |
| **config label** | `AddConfig` 的唯一名稱；必須含 `r<N>-` 前綴，用來把 Trial、config snapshot 與 History 的當輪證據連起來。 |
| **TuningHistory** | 專案根的 `TuningHistory.md`，納入版控的永久分析與決策報告；不是只放結論的索引。 |
| **provenance** | 可回溯「設定從哪一個 experiment／trial／round 而來」的來源資訊，包括名稱、label、日期與 config diff。 |
| **champion** | 通過 eligibility、逐 seed 比大小（一個 seed 都不輸、至少贏 3 個）與 hold-out 驗證，因而具備 promotion 資格的 variant。 |
| **promotion** | 將 champion 的完整設定寫回 production baseline，並以正式 production 路徑 build、solve、ValidateRules 驗證的動作。 |
| **retain** | 沒有可靠 champion 時保留現有 production baseline 的合法裁決；仍必須保存實驗與分析證據。 |
| **rejected** | candidate 或已嘗試 promotion 因違反正確性、不變式、holdout 或 production 驗證而被否決的裁決。 |

### 契約、旋鈕與量測

| 名詞 | 定義 |
| --- | --- |
| **停止契約** | 定義何時停止求解的共同條件，例如 `MipGap`、`TimeLimit`、各種 limit 與數值容差；同一週期內固定，不可當 variant 掃描。 |
| **環境契約** | 定義量測基準的共同執行環境，例如 `Threads`、`ParallelMode`、記憶體與 node-file 設定；先 sizing 定版，之後固定。 |
| **搜尋策略**（舊稱策略旋鈕） | 改變 solver 搜尋路徑、但不改停止／量測共同基準的 `CplexConfig` 欄位；是 variant 的唯一候選池，共 109 顆（§2.2.1）。 |
| **重複量測**（舊稱量測器材） | 用於可重現或抽樣的設定，例如 `Seed`、`ClockType`；不是可拿來競賽排名的 variant。 |
| **seed** | 控制 solver 隨機性的一次重複量測條件。所有 variant 使用同一組 tuning seeds；它不是「比較誰較好」的候選設定。 |
| **holdout seed** | 完全不參與 variant 選擇的保留 seed，只在 champion 選出後用來估計泛化效果，不能反過來挑 champion。 |
| **warm-up** | 同一批實驗中首次求解可能承受的冷啟動成本；此筆會標記並排除，不進排名。 |
| **主指標** | 當前進場情境下，逐 seed 比大小實際分出勝負的那一項：A 多半比到 runtime、B 比到 endGap、C 比到有沒有找到 incumbent。比較順序固定（§4.2），不必另外換算或彙總。 |
| **逐 seed 比大小** | 同一個 seed 的 variant 直接跟 baseline 比：有沒有找到解 → 有沒有證明最佳 → 都證明最佳比時間 → 都沒證明比 gap；都沒找到解算平手。框架寫在主表 `VsBaseline` 欄與 `-summary.csv` 的 `Wins` / `Losses` / `Ties` / `NotCompared`（§4.2），NEVER 自己比。 |
| **勝出規則** | 一個 tuning seed 都不能輸，而且至少贏 3 個（K = 5）；hold-out 的 3 個 seed 一個都不能輸（§4.4、§4.6）。 |
| **runtime** | 單一 Trial 從開始到 solver 停止的耗時（主表 `SolveTimeMs`，只計 `Solve()`）；只在情境 A 當主指標，情境 B 的多數 trial 都會等於 `TimeLimit`。 |
| **endGap** | Trial 停止時實際達到的 gap（主表 `Gap`）；情境 B 的主指標。 |
| **`FirstSolutionMs`** | 首個可行 incumbent 出現的時間（主表欄）；診斷收斂剖面用，不參與比大小。 |
| **`BoundChange`** | 求解過程中 dual bound 從首個觀測點到末個觀測點的淨變化（主表欄）；診斷收斂剖面用，不參與比大小。 |
| **`LastBoundChangeMs`** | dual bound 最後一次變動的時間（主表欄），越早代表越早停滯；用於辨識 Dual-bound 剖面。 |
| **收斂剖面／瓶頸** | 從 trajectory 的 incumbent、bound 與時間軌跡判出的求解障礙類型（如 Primal-search、Dual-bound、Node-cost）；它決定可掃的搜尋策略類別。 |
| **eligibility gate** | 排名前的合格檢查：先淘汰錯誤、品質不達標、越界或違反不變式的 Trial，剩下者才能比較。 |
| **lexicographic 比較** | 固定優先序的比較方式：先正確性／品質（eligibility），再逐 seed 比大小；不把不同層級的數字混成單一分數。 |

### 求解結果與數學用語

| 名詞 | 定義 |
| --- | --- |
| **incumbent** | solver 目前找到的最佳可行整數解；在 min 問題是目前最小 objective，在 max 問題是目前最大 objective。 |
| **objective** | 模型目標式在 incumbent 上的值；它不是 runtime，也不能在不同停止契約下任意混比。 |
| **BestBound／dual bound** | 對最佳可能 objective 的證明界；min 問題是下界、max 問題是上界，不能越過已知 incumbent。 |
| **`Gap` / `MipGap`** | incumbent 與 BestBound 的相對差距。主表 `Gap` 是求解結果實際達到的差距；`CplexConfig.MipGap` 是停止契約門檻，達到即停，不是搜尋策略旋鈕。 |
| **Optimal** | solver 已證明 incumbent 為全域最佳解的狀態。 |
| **Feasible** | solver 有可行 incumbent、但尚未完成最佳性證明，通常因停止契約而停止的狀態。 |
| **TimeLimit** | 到達時限而停止；本規範中特指沒有可用 incumbent 的情境 C，與帶 incumbent 的 `Feasible` 必須分開判讀。 |
| **Infeasible** | 模型沒有任何滿足所有 hard constraints 的解；是模型／資料問題，不是 tuning 問題。 |
| **Unbounded** | 目標可沿某方向無限改善，通常代表漏了界限；是模型問題，不是 tuning 問題。 |
| **IIS／conflict** | 不可行模型中的最小（或縮小的）衝突 constraint 集，用來取證並退回 Phase 1／2，不用來在 Phase 3 改模型。 |
| **dynamic search** | CPLEX 動態調整搜尋策略的模式；本 guide 要求它全程啟用，避免手動策略設定被靜默忽略。 |

---

## §0 心智模型

### 0.0 進場條件 — 先驗正確，再調效能

**依序確認，任一項不成立就停下回報，先回 Phase 2 修**：

1. `dotnet build` 通過
2. **Status 落在可進場的三態之一**（見下表）——**NEVER 要求 `Optimal`**
3. Phase 2 的解驗證協定四步已過：① Status 五態診斷 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity

`status.json` 的 `solveVerified: true` 是**必要條件但不充分**——MUST **實跑一次確認**，不能只信 JSON。

Why: 對錯的模型調參數只會**更快地得到錯答案**，而且會讓那個錯答案看起來更可信（「我們還做了效能調校」）。

#### 0.0.1 Status 決定進場情境（本階段的第一個分岔）

**「沒在時限內收斂到 `Optimal`」正是本階段最典型的進場理由，不是進場的阻礙。** 要求 `Optimal` 才准進 Phase 3，等於把最需要調校的專案永遠擋在門外——而 Phase 2 手上沒有任何合法工具能修「太慢」。

| 框架 `SolveStatus` | 進場情境 | 可否進場 | 逐 seed 比大小時多半比到（主指標） |
| --- | --- | --- | --- |
| `Optimal` | **情境 A · 收斂已達成** | ✅ | **runtime**（把同一個答案更快解出來） |
| `Feasible` | **情境 B · 撞限制、有 incumbent、gap 未收** | ✅ **最典型** | **同時間預算下的 endGap** |
| `TimeLimit`（中止且無任何可用解） | **情境 C · 連第一個可行解都找不到** | ✅ **但有附加前提** | **有沒有找到 incumbent** |
| `Infeasible` | 前提破裂 | ❌ | 走 §1.1 取證，退回 Phase 1 / 2 |
| `Unbounded` | 前提破裂 | ❌ | 退回 Phase 2 補界限 constraint |
| `Error` / `NotSolved` | 執行面問題 | ❌ | 先修到跑得起來 |

**情境 C 的附加前提**：`TimeLimit` 代表沒有任何解，②③ 兩步驗不了——這時 `solveVerified` MUST 是在**小 instance** 上取得的（`status.json` 的 `verifiedOn` 為 `small-instance:*`）。沒有任何 instance 驗過模型就進場 = 你在對一個從未被驗證過的模型調參數，**停下退回 Phase 2**。

**NEVER 為了「進場好看」而放寬契約**：把 `MipGap` 調鬆或 `TimeLimit` 調大讓 `Feasible` 變成 `Optimal`，是動停止契約（§2.0.1），會讓整個 before/after 失去共同基準。情境 B 就用情境 B 的量法。

Why 要在進場就分情境：比大小的順序（§4.2）會自動比到對的那一項——情境 B 兩邊都撞時限就比 gap、情境 C 先比有沒有找到解，不必另外選指標。但情境決定**結果不變式怎麼驗**（§0.1.1：A 要 objective 一致，B / C 只要不退步）、**瓶頸剖面怎麼判**（§2.1），也決定本輪目標該寫成「更快」、「gap 更小」還是「找得到解」。情境判錯，不變式就會把正常結果當越界、或把越界放行。

**另一個前置**：資料驗證。`OptData.Load` 自動檢查 Set／Parameter 重複 key 與 Parameter 數值 sanity；Parameter→Set 關聯由開發者掌握，Set-driven lookup 使用 `FindParameterOrLog` 留下缺值 Warning。資料契約未驗清楚就別調參數，否則只會更快得到錯答案。

Gate 順序固定：**資料驗證（載入時自動）→ 解驗證協定四步（Phase 2）→ 才進本檔**。

#### 0.0.2 `ModelType` 決定走哪條流程（與 0.0.1 的 Status 一起在進場時判）

**資料來源**：Phase 2 正式求解紀錄（`<Project>-solve-trial.csv` 的 `ModelType` 欄）或任一輪 `<Project>-tuning-r<N>-trial.csv` 的 `ModelType` 欄；同一個實驗 `-meta.csv` 的 `model.<Model>.modelType`。值由框架讀 CPLEX 模型判定（`IsMIP()` + NbinVars / NintVars / Ncols），**NEVER 自行推論，NEVER 開 `.lp` / `.mps` 判斷**（天條「讀檔」，`../AGENTS.md`）——`.lp` 的整數 / 二元變數宣告在檔尾，讀檔被截斷就會把 MILP 看成 LP，整期 tuning 的前提跟著錯。

**進場一致性檢查（機械可判）**：`ModelType = LP` ⇔ `-meta.csv` 的 `model.<Model>.binaryVarCount = integerVarCount = semiContinuousVarCount = semiIntegerVarCount = sosCount = 0`。兩邊對不上 → 停下查明，NEVER 挑一個相信就往下跑。

| `ModelType` | 流程 |
| --- | --- |
| `BP` / `IP` / `MILP` | 走本規範完整流程（§2.1 剖面 → §2.2 候選） |
| `LP` | 走下表的 LP 分支，其餘步驟照常 |

**LP 分支**——LP 沒有分支定界（B&B），MIP 那一整套判讀都不成立：

| 步驟 | LP 的作法 | 為什麼 |
| --- | --- | --- |
| 進場情境（§0.0.1） | 解完就是情境 A（`Optimal`），主指標 = runtime；時限內解不完才是情境 C | LP 沒有「有 incumbent、gap 未收」的中間狀態 |
| §2.3.1 dynamic search 檢查 | 不適用，記「N/A（LP）」 | dynamic search 是 MIP 搜尋方法，LP 的 log 沒有這一行 |
| §3.0 R0 | 照跑 K = 5 個 seed | 之後每一輪一樣逐 seed 比大小（兩邊都 Optimal → 比時間） |
| §3.0 產出 B 剖面 | **不分類**，剖面直接記「LP」 | callback 不觸發，軌跡是空的：`FirstSolutionMs` / `LastBoundChangeMs` / `BoundChange` 寫 `n/a`（CPLEX 沒提供），NEVER 拿來分類 |
| §3.0 產出 C 契約探針 | 不跑，記「N/A（LP）」 | LP 的 gap 恆為 0，`MipGap = 0` 探針沒有東西可量 |
| 候選旋鈕（§2.2） | 只用 §2.2 表的 **LP** 列 | cuts、啟發式、`Emphasis`、分支選擇只作用在 B&B，對 LP 無效 |
| `NodeCount` / `IterationCount` | `NodeCount` 恆為 0，不看；`IterationCount` 可當輔助 | 沒有 B&B 節點 |
| §7 早停 B（bound 軌跡一致） | 不適用 | 沒有 bound 軌跡 |

★ 軟性限制式會加入連續的彈性變數，所以 BP / IP 模型加了 soft constraint 會判成 `MILP`——這是正確的，照 MILP 流程走。

### 0.1 凍結範圍 — 可寫區域白名單

進場時模型與資料已凍結、正確性已驗（§0.0.1 的三個進場情境之一）。本階段的可寫區域是**白名單**（只有名單上的可以動，其餘一律不行）：

| # | 可寫區域 | 用途 | 限制 |
| --- | --- | --- | --- |
| 1 | `Program.cs` 的具名 `CplexConfig` production baseline | promotion 唯一寫回點 | 只改欄位值與其上方 provenance 註解 |
| 2 | `Program.cs` **exp 分支內**的 variant 定義與 round config archive | 每輪實驗的 `Clone()`、旋鈕設定與可重跑設定快照 | 只在 exp 分支內；當輪一律 `baseline.Clone()` 起手；**每個已執行 round 的完整設定區塊 MUST 保留，NEVER 被下一輪覆寫或刪除**（§3.3） |
| 3 | 專案根 `TuningHistory.md` | 決策紀錄 | 每輪追加，NEVER 改寫歷史節 |
| 4 | `status.json` 的 Phase 3 欄位 | 進度 | 只更新自己負責的欄位 |
| 5 | `Experiments/`（專案根的 Phase 3 archive） | 每輪一組的 CSV／trajectory 原始證據（主表 + 說明檔 + 彙總，有軌跡再加；已 archive 的檔不可變） | 只允許把 `bin/.../Experiment/` 的本輪檔案複製進來（目標已存在就拒絕、不覆寫，§3.3.1）；納入 source control；NEVER 手改內容 |

**白名單之外一律唯讀，包含**：model 組裝 chain、`Dataload`、`Set/` `Parameter/` `Variable/` `Constraint/` `Objective/` `Solution/`、`Data/*.csv`、`Model.md`、`ProjectConfig`、csproj、OptimFoundation 框架與 `dlls/`。

#### Phase 3 延伸架構（唯一允許的 Phase 2 專案擴充）

Phase 2 的八個固定資料夾仍然不變。**只有進入 Phase 3 後，專案根可額外且只能額外出現** `TuningHistory.md`、`status.json` 與 `Experiments/`；這不是自創架構，而是本檔明定的 extension。檔案樹固定如下——每輪一組檔，檔名帶輪次（`<Project>-tuning-r<N>[-holdout]-*.csv`），新增一輪就多一組、不會動到先前 round：

```text
Projects/<Project>/
├── TuningHistory.md
├── status.json
└── Experiments/
    ├── <Project>-tuning-r<N>[-holdout]-trial.csv ← 主表，一列一 trial（18 欄）；每輪各一個檔
    ├── <Project>-tuning-r<N>[-holdout]-meta.csv ← 說明檔，模型大小／環境／baseline 完整設定
    ├── <Project>-tuning-r<N>[-holdout]-summary.csv ← 彙總，每組設定一列（14 欄；跟 baseline 逐 seed 比：贏／輸／平手幾個，框架判好）
    └── <Project>-tuning-r<N>[-holdout]-trajectory.csv ← 有收集到軌跡才有
```

`bin/<Configuration>/net8.0/Experiment/` 是 framework 的**暫存輸出**，不是 archive。每次成功執行 R<N> 後，MUST 將 `<Project>-tuning-r<N>[-holdout]-trial.csv`、`-meta.csv`、`-summary.csv` 複製到專案根 `Experiments/`（有 `-trajectory.csv` 就一併搬）；archive 不可變：這一輪任何一個檔已在 `Experiments/` 就視為已 archive，NEVER 重跑、改開 r<N+1>；複製時目標已存在就拒絕、停下回報，NEVER 覆寫。NEVER rename、人工編輯或任何方式讓已 archive 的檔消失或變動。`dotnet clean` 後 bin 可以消失，但 archive 的所有既有 round 必須仍完整存在於 `Experiments/`。

#### 0.1.1 Phase 2 結果不變式（本階段最硬的驗收點）

**tuning NEVER 改變 solver 該回答的那個答案。** 進場時 MUST 先記錄 Phase 2 的求解結果當作基線，寫進 `TuningHistory.md` 契約區塊：

```text
phase2Status     = Optimal | Feasible | TimeLimit
phase2Objective  = <值>（TimeLimit 時為 n/a）
phase2Bound      = <值>（TimeLimit 時為 n/a）
phase2Gap        = <值>（TimeLimit 時為 n/a）
```

不變式**依 `phase2Status` 分兩套，套錯會把正常結果誤判成越界、或把越界放行**：

**情境 A（`phase2Status = Optimal`）— 在 `MipGap` 容差內一致**

| 情況 | 判定 | 動作 |
| --- | --- | --- |
| 同契約下 objective 與基線的差距 ≤ `MipGap` × \|`phase2Objective`\| | 正常 | 繼續 |
| 某 trial 的 objective 偏離基線超過這個容差 | **異常 trial** | 該 trial 淘汰並記錄，NEVER 當成「解更好」直接採用 |
| promotion 後 production objective 偏離基線超過這個容差 | **調壞了** | **立即回退 baseline**，記 `rejected` |

容差用停止契約的 `MipGap`；沒設就用 CPLEX 預設 `1e-4`。

Why: CPLEX 只要 gap ≤ `MipGap` 就回報 `Optimal`，所以換設定或換 seed 可以合法停在差距 `MipGap` 以內的另一個解（`MipGap = 0.01` 就是 1% 以內）——要求完全相等會把這些正常結果誤判成異常。超出容差才代表出事，只有三種可能：契約被動到、數值容差被放寬（`IntegralityTolerance` 之類）、或框架/模型被誤改——三種都是本階段的越界。

**情境 B / C（`phase2Status = Feasible` 或 `TimeLimit`）— 單調不退步 + bound 不矛盾**

基線只是一個 incumbent，不是最佳解。**要求 objective 嚴格相等在這裡是錯的**——incumbent 變好本來就是 tuning 該有的產出。改判三條，全部機械可驗：

| # | 規則 | 違反代表什麼 |
| --- | --- | --- |
| 1 | **不得變差**：min 問題 objective 不得高於 `phase2Objective`；max 反之（容差取 `MipGap` 量級） | 該 trial 更差，正常淘汰（不是越界） |
| 2 | **bound 不得越線**：min 問題任一 trial 的 `BestBound` 不得高於全期已知最佳 incumbent；max 反之 | **越界**——bound 越過已知可行解在數學上不可能，代表容差被放寬或模型被誤改 |
| 3 | **Optimal 錨點**：任一 trial 若回報 `Optimal`，其 objective 即為真最佳值；此後任何 trial 的 incumbent **優於**它 → 越界 | 同上 |

規則 2 與 3 是情境 B / C 唯一能偵測「調壞了」的手段——**MUST 每輪執行**。放棄了它們，情境 B 就變成沒有任何越界防線的裸奔。

★ 專案若無 `Solution/` 與 `ValidateRules`（解驗證協定未實作），上述不變式就是唯一能自動檢查「解沒被調壞」的手段，不可省略。

#### 0.1.2 交付時的 diff 檢查（機械可驗）

**`git diff --name-only` MUST 只出現下列 Phase 3 extension 檔**：

```text
Projects/<Project>/Program.cs
Projects/<Project>/TuningHistory.md
Projects/<Project>/Experiments/<Project>-tuning-r<N>[-holdout]-trial.csv
Projects/<Project>/Experiments/<Project>-tuning-r<N>[-holdout]-meta.csv
Projects/<Project>/Experiments/<Project>-tuning-r<N>[-holdout]-summary.csv
Projects/<Project>/Experiments/<Project>-tuning-r<N>[-holdout]-trajectory.csv ← 有收集到軌跡才有
```

（`status.json` 若已納管則可額外出現；不建立中間草稿區。）上列 `Experiments/` 的檔只能新增、不能修改：已 archive 的檔出現在 diff 視為越界。

多出任何 `.csv` / `Constraint_*.cs` / `Objective/*.cs` / `Model.md` / csproj 的改動 = **本階段越界，MUST 全部還原**。

`Program.cs` 內部的 diff 也要逐行檢查，**只允許兩種**：baseline 的欄位值與其 provenance 註解、exp 分支內的 variant 定義。model 組裝 chain 出現在 diff 裡 = 越界。

Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，該輪實驗證據**整批作廢**。而 §0.1.1 的結果不變式是同一件事的另一面：**結構不變**（diff 檢查）且**結果不變**（objective 對照），兩者都過才算沒有越界。

### 0.2 完成的定義

**完成 ≠ 跑出 experiment CSV。**

完成 = champion 已 promotion 到 production baseline **且** production 重跑驗證通過（或有證據支持保留原 baseline）。只產出 experiment 報表而 production 仍跑舊 config，**不算完成**——下一輪會有人重做同一組實驗。

沒有可靠勝者時，「**retain + 證據**」也是合法交付，但一樣要寫進 `TuningHistory.md`。

### 0.3 本階段不做的事

- NEVER **主動建議** tuning——使用者提出才做
- NEVER 用 soft constraint / penalty 讓 infeasible 模型「有解」——旋鈕不會把 infeasible 變 feasible，只會讓你更快確認它 infeasible
- NEVER 以 tuning 名義移項 / 改號 / 翻方向 / 四捨五入或修改輸入精度
- NEVER 沒有 `OptExperiment` 記錄就宣稱改善
- NEVER 自行升級去改模型結構——那是新一輪 Phase 1，由使用者決定要不要走

---

## §1 範圍界線 — 先確認這真的是 tuning

使用者的訴求落在下表哪一格，決定你要不要繼續：

| 使用者要的 | 是不是 tuning | 動作 |
| --- | --- | --- |
| 太慢 / gap 收不下來，**已 `Optimal` 但想更快** | ✅ **是**（情境 A） | 進 §2 |
| 撞 `TimeLimit` 停下、**有 incumbent 但 gap 未收**（`Feasible`） | ✅ **是**（情境 B，最典型） | 進 §2；主指標是 endGap 不是 runtime |
| 跑滿時限**連第一個可行解都沒有**（`TimeLimit`） | ✅ **是**（情境 C） | **先確認 `verifiedOn` 是 `small-instance:*`**——模型從未被任何 instance 驗過 → 退回 Phase 2；驗過才進 §2，剖面直接判 No-incumbent（§3.0 產出 B） |
| 換參數值、換一批資料 | ❌ 否 | 換 `Data/*.csv` 重跑即可，本階段不介入 |
| 加刪約束、改 Big-M、reformulation、換 formulation | ❌ 否 | 退回 Phase 1 改 `Model.md`，確認後由 Phase 2 重走轉譯 |
| 要放鬆某條限制（soft constraint / penalty） | ❌ 否 | 那是**建模決定**：回 Phase 1 寫進 Model.md（含 penalty 的具名 PARAM 與被放鬆的條目），確認後由 Phase 2 照常轉譯 |
| `Infeasible` | ❌ 否 | **前提破裂**：跑 IIS 拿最小衝突集當**證據**，退回 Phase 1 / 2（見 §1.1） |
| `Unbounded` | ❌ 否 | **前提破裂**：某方向漏了界，退回 Phase 2 補該變數的上限 constraint |

**使用者指定的方向不是 gate 的豁免。** 「試試 emphasis」「加 cuts」是 §3 的 variant 候選；正確性沒過就先回 Phase 2，並說清楚為什麼還不能調。指定方向也不解除「一輪只改一個旋鈕」。

### 1.1 Infeasible 的取證流程

Infeasible 幾乎都是模型或資料的錯，不是 solver 的錯。在這裡調旋鈕沒有任何意義——你要產出的是「該退回哪裡、退回去要修什麼」的**證據**，不是修法本身。

1. 取衝突限制式名稱清單：讀 solver log 的 `[衝突限制式摘要] 數量=N 名稱=a|b|…` 那一行（`Solve()` 遇到 Infeasible 會自動 `RefineConflict` 並寫出這行；名稱數 MUST 等於 N），或呼叫 `engine.GetConflictConstraints()`
   `IIS/*.ilp` 只在要看式子內容時用上面的名稱 grep 那幾行，**NEVER 整檔讀進 context**；拿到名稱後回 `Constraint/` 找對應 `.cs` 與 Model.md 條目
2. 對每個名稱，回 Model.md 找該條的語意
3. 判定根因：

| 根因 | 徵狀 | 退回哪裡 |
| --- | --- | --- |
| 資料值矛盾 | 需求總量 > 總產能 | Phase 1（資料前提）或換 CSV |
| 模型結構矛盾 | 兩條 hard constraint 互斥 | Phase 1 改 Model.md |
| Big-M 太小 | 砍掉了合法解 | Phase 1 重推 M 的上界 |
| 界限設錯 | LB > UB | Phase 1 / 2 |

4. **NEVER 建議改成 soft constraint** 讓它有解——放鬆模型是改語意，屬 Phase 1 由使用者決定
5. **NEVER 自己動手修**——本階段只出證據

---

## §2 旋鈕分類與瓶頸診斷

先分類旋鈕，再從**收斂軌跡**量化瓶頸，最後才對應候選。**不是盲搜，也不是憑印象讀 log。**

### 2.0 旋鈕分類（本階段的地基）

任何旋鈕動手前先歸類。**歸錯類，後面所有比較都無效。**

`CplexConfig` 接線了 **182 顆**旋鈕（= IBM CPLEX 22.1.1 的 .NET API 全部可設參數）。
其中**只有 109 顆能拿來做實驗**，其餘 73 顆一律凍結。分五類，用「這顆在管什麼」命名：

| 類別 | 在管什麼 | 顆數 | 實驗中的角色 |
| --- | --- | --- | --- |
| **停止條件** | 什麼時候算解完 | 27 | 整個 tuning 週期**固定共用**，NEVER 進 variant 池。變更 = 週期重啟（§2.0.1） |
| **執行資源** | 用多少機器、怎麼平行、怎麼計時 | 9 | **R0 之前先單獨定版**（§2.3），之後整期凍結。NEVER 與搜尋策略同輪比較 |
| **重複量測** | 同一個設定再量一次用的 | 2 | `ClockType` 整期固定；`Seed` 是 §3.0 的**自變數**，NEVER 當 champion |
| **搜尋策略** | CPLEX 用什麼路線找答案 | **109** | **唯一的 variant 池**（全清單見 §2.2.1） |
| **非 tuning** | 輸出、顯示、讀檔上限、診斷、solution pool、內建 tune | 35 | 不改求解路線，NEVER 拿來掃描 |

> 這五個名稱在 2026-08 從「契約／環境／器材／策略」改過來，語意不變，只是改成看名字就知道在管什麼。
> `CplexConfig` 每顆 property 的 XML 註解都標了它屬於哪一類。

三句判準：

- **改了之後，兩次求解還算不算在做同一件事？** 不算 → 停止條件
- **改了之後，量測的尺還算不算同一把？** 不算 → 執行資源
- **改了之後，答案的終點一樣、只是走法不同？** 是 → 搜尋策略，可以掃

❌ 最常見的兩個歸類錯誤：

- 把 `MipGap` 當速度旋鈕掃 —— 它是停止條件。兩個不同 `MipGap` 的 trial 比 runtime，等於兩個跑者跑不同長度的賽道比秒數
- 把 `Seed` 當 candidate 排名 —— 它不是搜尋策略，是同一個策略的第二次抽樣。它「贏」只證明變異大，不證明它比較好

#### 2.0.1 契約變更協定

tuning 過程中發現契約可能訂錯（例：`MipGap` 過鬆導致 production 常態交付次佳解）→

1. **回報，NEVER 自行變更** —— 這是品質決策，使用者拍板
2. 契約一改 → **歷史 runtime 全部作廢**（終點線移了），重跑 §2.3 sizing 與 §3.0 R0
3. `TuningHistory.md` 記一筆「契約變更」分隔線

#### 2.0.2 `Threads` 為什麼是環境層而不是策略層

它同時改變三件與量測有關的東西：

1. **記憶體預算** —— CPLEX 的 `MemoryLimitMb`（WorkMem）管的是 **live tree 大小**，不是總記憶體；threads 多 → 樹長得快 → 更早觸發儲存策略切換
2. **同步成本結構** —— `ParallelMode` 決定論模式的同步代價隨 threads 上升
3. **重跑的差異** —— threads 越高，執行緒時序造成的路徑分歧越大，同一個設定換 seed 重跑的結果差得越多

第 3 點是關鍵：本階段每個判定都是「同一個 seed 跟 baseline 比大小」。**threads 浮動時做比較，等於一次改了兩件事，比出來的輸贏說不清是誰造成的。**

已知交互（歸類依據）：

| 組合 | CPLEX 機制 | 後果 |
| --- | --- | --- |
| threads × `MemoryLimitMb` | WorkMem 管 live tree 大小，超過即換儲存策略 | threads 多 → 更早觸發切換 |
| threads × `NodeFileStrategy` | **框架的雷**：`LoadConfig()` 設 `MemoryLimitMb` 時強制 `MIP.Strategy.File = 0`；CPLEX 預設是 `1`（壓縮後留記憶體） | 框架把壓縮策略**關掉了**，比預設更糟；threads 越高越早 OOM |
| threads × `RootAlgorithm = 6`（concurrent） | concurrent optimizer 讓多個 LP 演算法各佔一 thread | root 吃掉 threads，B&B 平行度下降 |
| threads × **首解幾乎即最佳的模型** | root / ramp-up 佔比高，樹未長大即解完 | 純同步開銷、零收益 |
| threads × cuts × heuristics | cut pass 同時牽動 heuristic 與 probing 的平行配置 | 三方耦合，一輪一顆的設計掃不出來（§9 反模式） |

### 2.1 收斂軌跡剖面 → 瓶頸判斷

**資料來源**：本輪主表 `<Project>-tuning-r<N>-trial.csv` 的 `FirstSolutionMs` / `BoundChange` / `LastBoundChangeMs` / `SolveTimeMs` 欄。前三欄是框架從收斂軌跡算好的（軌跡沒開寫 `off`，有收集但沒發生寫 `none`），**NEVER 自己從 `-trajectory.csv` 重算**。只有要看曲線形狀（endGap / bound 何時停滯）才去 `<Project>-tuning-r<N>-trajectory.csv`（欄位 `TrialId, TrialLabel, PointIndex, ElapsedMs, ObjectiveValue, BestBound, Gap`，`TrialId` 對回同一個實驗的 trial.csv）用 label 抽單一 trial 的列，NEVER 整份讀。

★ **`ModelType = LP` 不做本節分類**（§0.0.2）：LP 沒有 B&B，callback 不觸發，軌跡是空的，四個量都是 `n/a`。

★ **`NodeCount` / `IterationCount` 自 2026-08-25 起會填實際值**，但**不能單獨看**：node 少不等於快——實測 `VariableSelect=3`（強分支）node 更多且慢 4 倍，而 `Emphasis=3` node 更少卻也慢 2.4 倍。要搭配 runtime 一起判讀；`IterationCount / NodeCount` 才看得出每個節點貴不貴。仍 NEVER 只憑它們就下結論。

分類用四個量，全部從主表讀：

| 量 | 定義 | 主表欄位 |
| --- | --- | --- |
| 第一個解的時間 | CPLEX 呼叫 callback 時第一次看到可行解的時間 | `FirstSolutionMs`；`none` = 有軌跡點但沒看到可行解；`n/a` = callback 一次都沒被呼叫（presolve / root 就解完） |
| 第一個解的時間佔比 | 找到第一個解花掉的時間佔比（primal 側佔全程比例） | `FirstSolutionMs` ÷ `SolveTimeMs` |
| 界的總變動 | dual 側淨移動（末點 − 首點） | `BoundChange` |
| 界最後變動的時間 | bound 最後一次變動的時點 | `LastBoundChangeMs`；`none` = bound 從未變動 |

分類 → 決定候選旋鈕類。**只掃對應那一類，NEVER 查全表盲掃**：

| 剖面 | 判準 | 瓶頸 | 候選 |
| --- | --- | --- | --- |
| **No-incumbent** | `FirstSolutionMs` **不存在**（主表 `FirstSolutionMs = none`，`Status = TimeLimit`） | 連一個可行解都生不出來 | 同 Primal-search，但**優先度最高且順序不同**：`Emphasis = 4` → `1` → `NodeSelect = 0`（DFS）→ `DiveType = 2/3` → `RinsHeuristicFrequency` / `HeuristicEffort` |
| **Dual-bound** | `FirstSolutionMs ÷ SolveTimeMs` 小、`BoundChange` 小、`LastBoundChangeMs` 早 | 證明 bound | `Emphasis = 3`（**BESTBOUND**）、`GomoryCuts` `MirCuts` `CoverCuts` `CliqueCuts` `FlowCoverCuts` `CutsFactor` `CutPasses` `Probe` `Symmetry` |
| **Primal-search** | `FirstSolutionMs ÷ SolveTimeMs` 大 | 找可行解 | `Emphasis = 1`（FEASIBILITY）或 `4`（HIDDENFEAS）、`RinsHeuristicFrequency` `HeuristicEffort` `NodeSelect = 0` `DiveType` |
| **Node-cost** | 兩者皆不明顯、`NodeCount` 少而 `SolveTimeMs` 長（`IterationCount ÷ NodeCount` 大 = 每個節點很貴） | 單 node 太貴 | 減 cuts、`RootAlgorithm` `NodeAlgorithm` `Presolve`（threads 已在 §2.3 定版，不在此掃） |
| **數值不穩** | log 有數值警告、解不穩定 | ill-conditioning | `NumericalEmphasis = true`（係數量級問題要回 Phase 1） |

雜訊大（同一個設定換 seed 結果差很多）不另成一類：這時沒有設定能每個 seed 都不輸，§4.4 的勝出規則自然判不出勝者，連續 3 輪無人勝出就依 §7.1 D 停止。

★ **判讀邊界**：軌跡只含 CPLEX 實際呼叫 callback 時觀察到的點（時間取 callback 的 `GetCplexTime − GetStartTime`），框架不補點，所以軌跡末點不一定等於最終結果。正式 eligibility 與勝負一律以 `<Project>-tuning-r<N>-trial.csv` 的 Trial metrics 為準；trajectory 只用來解讀求解過程。

★ **No-incumbent 的資料形狀**：`Status = TimeLimit` 時 `ObjectiveValue` / `BestBound` / `Gap` 全是 `NaN`，trial.csv 那幾欄寫 `NaN`，`FirstSolutionMs` 寫 `none`（軌跡沒開時是 `off`）。**NEVER 把 `NaN` 當 0 參與任何計算**——診斷看主表 `FirstSolutionMs` 是否為 `none`；勝負讀 `VsBaseline` 與 `-summary.csv` 的 `Wins` / `Losses`（框架逐 seed 比時先看有沒有找到解，§4.2）。

★ **剖面在情境 B / C 下 MUST 每輪重判**：找到 incumbent 之後瓶頸會從 No-incumbent 轉成 Primal-search 或 Dual-bound，沿用 R0 的剖面會繼續掃已經不是瓶頸的那一類。情境 A 沿用 R0 剖面即可。

★ **跨 variant 的 bound 軌跡若起訖完全一致** → 該類旋鈕對 dual 側無效，整類標記「已否證」（§7 早停 B）。

### 2.2 剖面 → 候選旋鈕（只列搜尋策略）

本表只含**搜尋策略**。停止條件（`MipGap` `TimeLimit` `NodeLimit` `IntegerSolutionLimit`）與執行資源（`Threads` 等）不在此掃 —— 見 §2.0。

| 剖面 | 候選（依序試，一輪一顆） |
| --- | --- |
| **No-incumbent** | `Emphasis = 4`（HIDDENFEAS）→ `Emphasis = 1` → `NodeSelect = 0`（DFS，最快下潛到葉節點）→ `DiveType = 2/3` → `RinsHeuristicFrequency` → `HeuristicEffort`。**NEVER 先加 cuts**——cut 推的是 bound，這裡連可行解都沒有，加 cuts 只會讓每個 node 更貴 |
| **Dual-bound** | `Emphasis = 3` → 加切割（`GomoryCuts` / `MirCuts` / `CoverCuts` / `CliqueCuts` / `FlowCoverCuts`）→ `CutsFactor` / `CutPasses` → `Probe` → `Symmetry` |
| **Primal-search** | `Emphasis = 1` → `RinsHeuristicFrequency` → `HeuristicEffort` → `NodeSelect = 0`（DFS）→ `DiveType` → `Emphasis = 4`（前者無效時的後備） |
| **Node-cost** | 減 cuts（各 cut 設 `-1`）→ `RootAlgorithm` / `NodeAlgorithm` → `Presolve` |
| **數值不穩** | `NumericalEmphasis = true` |
| **LP**（`ModelType = LP`，§0.0.2） | `RootAlgorithm`（1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent）→ 選到 `4` 才試 Barrier 內部參數（如 `BarrierCrossover`）→ `DualSimplexPricing` / `PrimalSimplexPricing` → `PreIndicator` / `PresolvePasses` / `AggregatorLimit` / `DependencyCheck` → `Scaling` → `SimplexPerturbationIndicator`；log 有數值警告時 `NumericalEmphasis = true` |

#### 2.2.1 搜尋策略全池（109 顆）—— 掃描的合法上限

**這是「可以動」的完整清單，不是每輪的候選清單。** 每輪仍照 §2.1 判剖面 → 只從 §2.2 對應那一列取候選 → 一輪一顆。

**永遠適用純 MILP（86 顆）**

| 群組 | 顆數 | 成員 |
| --- | --- | --- |
| emphasis 與搜尋分支 | 18 | `AdvancedStart` `BacktrackTolerance` `BestBoundInterval` `BranchDirection` `DiveType` `Emphasis` `KappaStatistics` `MipSearch` `NodeSelect` `NumericalEmphasis` `OptimalityTarget` `PriorityOrderType` `Probe` `SolutionType` `StrongBranchingCandidateLimit` `StrongBranchingIterationLimit` `UsePriorityOrder` `VariableSelect` |
| 啟發式與 solution polishing | 13 | `CardinalityLocalSearch` `FeasibilityPumpHeuristic` `HeuristicEffort` `HeuristicFrequency` `LocalBranchingHeuristic` `PolishAfterAbsoluteMipGap` `PolishAfterDetTime` `PolishAfterMipGap` `PolishAfterNodes` `PolishAfterSolutions` `PolishAfterTime` `RepairTries` `RinsHeuristicFrequency` |
| 切割平面 | 22 | `AggregationLimitForCut` `BqpCuts` `CliqueCuts` `CoverCuts` `CutPasses` `CutsFactor` `DisjunctiveCuts` `EachCutLimit` `FlowCoverCuts` `FlowPathCuts` `GomoryCandidateLimit` `GomoryCuts` `GomoryPassLimit` `GubCoverCuts` `ImpliedBoundCuts` `LiftAndProjectCuts` `LocalImpliedBoundCuts` `McfCuts` `MirCuts` `NodeCuts` `RltCuts` `ZeroHalfCuts` |
| 前處理 | 16 | `AggregatorFill` `AggregatorLimit` `BoundStrengthening` `CoefficientReduction` `DependencyCheck` `LpFolding` `NodePresolve` `PreIndicator` `PresolveDual` `PresolvePasses` `PresolveReduce` `PresolveReformulations` `RelaxedLpPresolve` `RepeatPresolve` `Scaling` `Symmetry` |
| root / node LP 演算法 | 13 | `RootAlgorithm` `NodeAlgorithm` `DualSimplexPricing` `MarkowitzTolerance` `PrimalSimplexPricing` `SimplexCrash` `SimplexDynamicRows` `SimplexPerturbationConstant` `SimplexPerturbationIndicator` `SimplexPerturbationLimit` `SimplexPricingCandidateList` `SimplexRefactorFrequency` `SimplexSingularityLimit` |
| subMIP（RINS 等啟發式的內部求解） | 4 | `SubMipNodeAlgorithm` `SubMipNodeLimit` `SubMipRootAlgorithm` `SubMipScaling` |

**有條件才有意義（23 顆）** —— 條件不成立時設了不會有作用，別浪費輪次：

| 群組 | 顆數 | 什麼條件下才生效 |
| --- | --- | --- |
| Barrier 內部 | 8 | `RootAlgorithm` / `NodeAlgorithm` 選 `4`（barrier）才生效 |
| Sifting 內部 | 2 | 選 `5`（sifting）才生效 |
| Network 內部 | 2 | 選 `3`（network）才生效 |
| QP / MIQCP / SOS | 6 | 模型要有二次項或 SOS 集合 |
| Benders | 4 | 模型要有 Benders 分解註記 |
| FeasOpt | 1 | 只在呼叫 feasopt 放鬆不可行模型時 |

#### 2.2.2 兩顆會炸掉整輪的旋鈕（實測）

這兩個不是「設了沒作用」，是**直接丟例外**。掃進 variant 池會讓整輪實驗中斷：

| 旋鈕 | 危險值 | CPLEX 回報 | 純 MILP 上的安全值 |
| --- | --- | --- | --- |
| `BendersStrategy` | `1` `2` `3` | `Error 2000: No Benders decomposition available` | 只有 `-1` 與 `0` |
| `OptimalityTarget` | `2` | `Error 1017: Not available for mixed-integer problems` | `0` `1` `3` |

★ 另有一顆執行資源旋鈕 `CpuMask` 在本機平台**完全不能用**（`Error 1811: Attempt to invoke unsupported operation`，
連官方文件列的預設值 `"auto"` 都設不進去；CPLEX 自己的 Interactive Optimizer 執行 `set cpumask auto` 同樣被拒）。
它屬執行資源類本來就不掃，列在這裡是避免有人拿它當「讓重跑結果比較穩」的手段。

★ `Seed` 的上限是 **2100000000**，超過丟 `Error 1015`。官方 Parameters Reference 只寫「any integer」沒給上限。

> 以上由 `OptimFoundation.Cplex.Tests` 的 `SolverParamValueMatrixTests` 實測得出：
> 182 顆旋鈕 × 官方文件列出的每一個合法值 = 538 個案例，各跑一次真實求解。
> 538 個裡只有這 7 個值被 CPLEX 拒絕，其餘全部可用。

#### `Emphasis` 五個值的官方語意 —— 選錯等於掃錯方向

| 值 | 名稱 | 行為 | 適用剖面 |
| --- | --- | --- | --- |
| 0 | BALANCED（預設） | 平衡「快速證明最佳」與「早期找到高品質可行解」 | — |
| 1 | FEASIBILITY | 更頻繁產出可行解，**犧牲證明最佳的速度** | Primal-search |
| 2 | OPTIMALITY | 較少心力找早期可行解 | 兩者之間，**不是 bound 專用** |
| 3 | **BESTBOUND** | **更大力度推動 best bound，找可行解變成幾乎附帶** | **Dual-bound** |
| 4 | HIDDENFEAS | 全力找「很難找到」的高品質可行解；`1` 找不到好解時才用 | Primal-search 後備 |

★ **推 bound 是 `3` 不是 `2`。** 這是實測踩過的坑：Dual-bound 剖面下試 `Emphasis = 2` 完全沒有效果。

#### 記憶體相關（屬環境層，在 §2.3 處理，不進 variant 池）

`TreeMemoryLimitMb` + `NodeFileStrategy = 2/3`，**順序有雷**：框架設 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy`（附錄 A ★）。

### 2.3 環境定版（sizing）—— R0 之前的一次性步驟

執行資源旋鈕會改變重跑結果的差異（§2.0.2），所以 **MUST 先定死，才跑 R0 與之後的比較**。這一步只做一次，**不計入輪次**。

★ **多數情況下這一步只是抄值。** Phase 2 出口已要求 `productionBaseline` 明設 `Threads`（依實機核心數實測）、`ParallelMode = 1` 與固定 `Seed`（coding guide §5 的 `CplexConfig` 表）。**同一台實機接手時沿用、不重跑 sizing**，把值抄進契約區塊即可。只有下列三種情況才執行下面的掃描：換機器、換 CPLEX 版本、Phase 2 未明設（視為交付缺陷，記成 finding）。

**做法**：固定 `ParallelMode = 1`（決定論，換取可比性），只掃 `Threads`，候選三個：**實體核心數 / −1 / −2**。NEVER 用邏輯核心數（超執行緒下多 thread 爭用記憶體存取，常比實體核心數更慢）。每個候選跑 3 個 seed，label 用 `s1-threads<N>-s<seed>`，現行 production 值那組再帶 `baseline`（例 `s1-threads8-baseline-s11`）；勝負直接讀 `-summary.csv` 的 `Wins` / `Losses`（跟現行值逐 seed 比，§4.2），NEVER 自己比。

**判定**：

| 觀察 | 結論 |
| --- | --- |
| 有候選 3 個 seed 全贏現行值（`Losses = 0` 且 `Wins = 3`） | 取之，凍結；多個都全贏時取 threads 最低的 |
| 沒有候選全贏 | 維持現行值，凍結 |
| 記憶體警告或 node file 溢寫 | 降 threads，或先處理 `MemoryLimitMb` / `NodeFileStrategy` 的順序雷，再重跑 sizing |

**為什麼多個全贏時取最低者**：threads 越高，同一設定重跑的結果差越多，之後逐 seed 比大小越容易出現隨機的輸贏。**效能相當時選低 threads = 之後的比較更穩。** production 要不要開更高是 promotion 後的獨立決定。

**定版後**把 `Threads` / `ParallelMode` / `MemoryLimitMb` / `NodeFileStrategy` 的確切值寫進 `TuningHistory.md` 契約區塊。之後任一項變動 → **重跑 sizing 與 R0**。

### 2.3.1 CPLEX 專屬前提：dynamic search MUST 全程啟用

CPLEX 的 **dynamic search** 是預設求解演算法，有一個會被靜默關閉的條件：

> **只要應用程式存在 control callback，CPLEX 就關閉 dynamic search、發出 warning、改用 static branch and cut。**

退回 traditional B&C 是**數量級的效能事件**，不是微調。含意：

- **informational callback（`MIPInfoCallback`）與 dynamic search 相容**（不會退回 traditional B&C）→ 框架的 `ITrajectorySource.EnableTrajectory()` 屬這一類，§2.1 的軌跡診斷可用。**但它仍會改變搜尋路徑、傾向變慢**（2026-09-28 實測：5 個 seed 中 5/5 較慢，多數 +1%～+8%，個別 +25%～+45%）→ 要和正式求解對照的 promotion / hold-out 驗證 MUST `.CaptureTrajectory(false)`
- 附錄 B 列的三個 callback 缺口（Heuristic / Lazy constraint / User cut）**一旦補上就是 control callback** → 補上那天 dynamic search 被關閉，**所有歷史 tuning 數據作廢**，MUST 重跑 §2.3 與 §3.0
- 每輪 MUST 用正面證據確認：solver log 中 `MIP search method: dynamic search.` 出現次數 = 本輪 MIP trial 數，且 `traditional branch-and-cut` 出現 0 次——NEVER 以「沒看到 warning」當作通過（log 沒讀完、grep 抓錯都會看起來像沒有）

此機制為 CPLEX 獨有，**NEVER 從其他 solver 的文件類推**。

### 2.4 模型層手段不屬本階段（列出來是為了辨識）

下列都是**真正有效**但**只適用於模型尚未定版時**（Phase 1）的手段。進了 Phase 3 模型已凍結，看到這類訴求 → 依 §1 退回 Phase 1，NEVER 在本階段做：

| 手段 | 為什麼有效 | 屬於 |
| --- | --- | --- |
| 變數型態降級（BV → IV → CV） | 求解難度 CV ≪ IV < BV；網路流 / 指派問題具 totally unimodular 結構時 LP 鬆弛即得整數解 | Phase 1 |
| 聚合限制式、移除 dominated 限制式 | 縮小搜尋空間 | Phase 1 |
| 收緊變數上下界 | 直接收斂 LP 鬆弛、減少分支 | Phase 1（且界限一律寫成獨立 constraint） |
| Big-M 取最小可行值 | M 過大 → LP 鬆弛鬆散、節點爆增 | Phase 1 |
| 對稱性消除（排序限制式 / lexicographic） | 避免探索大量等價分支 | Phase 1 |
| Warm start / MIP start | 快速建立 incumbent、提早剪枝 | Phase 3 可透過 `OptModel.AddMIPStart(...)` 或 `OptEngine.AddMIPStart(...)` 注入前一輪解；不得藉此改變模型內容 |
| Lazy constraints / user cuts / heuristic callback | 延遲生成大量潛在限制式 | Phase 1（且框架**未提供** callback 註冊點，見附錄 B） |

**模型已定版後只剩旋鈕一條路。** 旋鈕連調 3 輪無改善 → 依 §7 停止並回報，要不要回頭改模型結構是**使用者的決定**，不是 tuning 的下一步。

---

## §3 實驗設計

### 3.0 R0 校準輪 —— 不調任何東西（硬 gate）

**R0 沒跑完不准進 R1。** 它確認 baseline 在這組 seed 上的表現、進場情境與瓶頸剖面——之後每一輪都拿同一組 seed 跟 baseline 比大小，R0 就是先看清楚對照組長什麼樣子。

**exp 分支的形狀由 Phase 2 交付**（coding guide §8.4 的 R0-ready 契約）：experiment 名已是 `tuning-r0`（不含專案名）、label 已帶 `r0-` 前綴、`// R0 —` marker 已就位、5 個 seed 已列好。**Phase 3 進場不重寫 code**，直接 `dotnet run --project <project.csproj> -- exp`。

★ 同名 experiment 是**整組覆寫**（§3.3）：Phase 2 驗證管線若已跑過 `-- exp`（實驗名 `tuning-r0`），bin 會有 `<Project>-tuning-r0-*.csv`；R0 正式執行時 `Save()` 會整組覆寫這些檔（留 `[實驗紀錄覆寫]` WARN），**不必先刪**（Phase 2 不 archive，所以沒有衝突）。

形狀不符契約（缺前綴、名稱不對、混掃旋鈕）→ 這是 Phase 2 的交付缺陷：**記成 finding 並就地補正 exp 分支**（它在白名單內），流程照常往下跑，不必退回 Phase 2。

**variant 池**：只有 baseline 一個 config，跑 `K` 個 seed。`K = 5`（MIPLIB / SCIP 實務慣例）。另指定 **3 個 holdout seeds 全程不參與調參**（§4.6 用）。

R0 **不計入 `tuningRound`**，它是校準不是輪次。

#### 產出 A · baseline 對照組與情境確認

看 R0 主表 K 個 seed 的 `Status`、`SolveTimeMs`、`Gap`，確認跟 §0.0.1 判的進場情境一致，並記下 baseline 每個 seed 的表現：

| 情境 | R0 的 K 個 seed 呈現 | 之後比大小多半比到 |
| --- | --- | --- |
| **A**（`Optimal`） | 都在時限內解完 | **runtime** |
| **B**（`Feasible`） | **全部跑滿 `TimeLimit`**，有 incumbent | **endGap**（兩邊都撞時限就比 gap） |
| **C**（`TimeLimit`） | 全部跑滿且**無 incumbent**，數值欄全 `NaN` | **有沒有找到 incumbent** |

★ **之後每一輪怎麼判勝負**：同一個 seed 的 variant 直接跟 baseline 比大小（§4.2）。框架已經把結果寫在主表的 `VsBaseline` 欄（`win` / `lose` / `tie` / `n/a`）與 `-summary.csv` 的 `Wins` / `Losses` / `Ties` / `NotCompared`——**直接讀，NEVER 自己比**。勝出規則：一個 seed 都不能輸，而且至少贏 3 個（§4.4）。不算平均，也不算任何統計指標。

label 規則因此是硬性的：`r<N>-<config>-s<seed>`，baseline 那組 MUST 含 `baseline`，暖機 MUST 含 `warmup`——框架靠 label 找 baseline、配對同一個 seed、排除暖機。`n/a` 代表無法比較（暖機、同一個 seed 沒有 baseline trial、baseline 本身求解失敗）——**查原因、補正實驗設計，NEVER 自己補比**。

★ **情境 B 的 runtime 不是沒用，是不拿來比大小**。它退居「隱藏代價」檢查：某 variant gap 收得更好卻提早結束（runtime < `TimeLimit`）→ 通常代表它撞到別的停止條件，要查清楚而不是直接當勝者。

#### 產出 B · 瓶頸剖面

依 §2.1 讀主表的 `FirstSolutionMs` / `BoundChange` / `LastBoundChangeMs` / `SolveTimeMs`（`FirstSolutionMs ÷ SolveTimeMs` 為找到第一個解花掉的時間佔比），分類成 No-incumbent / Dual-bound / Primal-search / Node-cost / 數值不穩。

**分類結果決定後續每一輪的候選來源**（§2.2），這是「不盲搜」的機制。

進場情境 C（`TimeLimit`）**直接判為 No-incumbent 剖面**，不必再算 `FirstSolutionMs ÷ SolveTimeMs` / `BoundChange`——沒有 incumbent，那兩個量算不出來。

#### 產出 C · 契約健檢探針

跑一個 `MipGap = 0` 的**探針**（不是 variant，不進排名、不參與 champion 判定），取得：

- 真最佳目標值
- 現行契約下的品質損失 = `(obj_contract − obj_true) / obj_true`
- 取得真最佳所需的額外時間

★ **情境 B / C 的探針要放寬時限才有意義**：`MipGap = 0` 但 `TimeLimit` 不變的話，探針只會再撞一次時限、拿不到真最佳值。**探針（且僅探針）允許放大 `TimeLimit`**——它不參與排名，不影響 variant 之間的可比性。放大倍數與結果 MUST 記進 `TuningHistory.md`。放大後仍拿不到 `Optimal` → 記「真最佳值未知」，流程照常往下跑，NEVER 因此中斷。

**這是發現，不是決策**：結果寫進 `TuningHistory.md` 當 finding 回報使用者，**NEVER 因此自行變更契約**（§2.0.1），也**NEVER 中斷流程** —— 流程繼續用現行契約往下跑。

#### R0 出口 gate（機械可判）

| 檢查 | 通過條件 |
| --- | --- |
| **情境已確認** | R0 的 K 個 seed 呈現符合 §0.0.1 判的情境（A / B / C），寫進 `TuningHistory.md` 契約區塊 |
| 剖面已分類 | 落在 §2.1 五類之一；`ModelType = LP` 記「LP」（§0.0.2） |
| 結果不變式 | 依 `phase2Status` 套 §0.1.1 對應那一套（情境 A 在 `MipGap` 容差內一致；情境 B / C 不退步 + bound 不越線） |
| dynamic search | `MIP search method: dynamic search.` 次數 = trial 數且 `traditional branch-and-cut` 0 次（§2.3.1）；`ModelType = LP` 記 N/A |
| 契約與環境已定版 | 已寫進 `TuningHistory.md` 契約區塊 |

★ 情境 C 有一個專屬的 R0 結果：**K 個 seed 全都沒找到 incumbent**。這**不是**早停條件——它正是本輪要打的目標（之後 variant 在哪個 seed 找到 baseline 找不到的解，那個 seed 就算贏）。照常進 R1 掃 No-incumbent 候選。

#### 3.0.1 每輪的證據 → 目標 → 設定 gate

**R<N> 開跑前 MUST 先完成一份「本輪目標與設定理由」；沒有它，不得建立 variant 或執行 `OptExperiment`。**它必須先直接寫進 `TuningHistory.md` 的 R<N> 節，不能在看到結果後補寫。

每輪只讀**可追溯的摘要欄位**，NEVER 將整份 solver log 或實驗 CSV 明細全文讀入。證據來源與最低讀取內容固定如下：

| 必讀來源 | 擷取內容 | 本輪如何使用 |
| --- | --- | --- |
| `TuningHistory.md` 的契約區塊與**所有既有 R 節** | 現行 baseline provenance、進場情境、已否證方向、歷史目標／裁決／失敗原因 | 排除已否證或重複方向；確認哪些設定已試過、為何不再試；判定本輪能改的唯一搜尋策略旋鈕 |
| 前一輪與現行 baseline 的 `Experiments/<Project>-tuning-r<N>-trial.csv`（主表，逐欄抽取） | `TrialId`／`Model`／`ModelType`／`TrialLabel`／`ConfigChanges`／`Seed`／`VsBaseline`／`Status`／`ObjectiveValue`／`BestBound`／`Gap`／`BuildAndSolveTimeMs`／`SolveTimeMs`／`FirstSolutionMs`／`LastBoundChangeMs`／`BoundChange`／`NodeCount`／`IterationCount`（共 18 欄；每格都有值：沒有數字時寫標記 `off`（軌跡沒開）／`none`（有收集但沒發生）／`n/a`（求解器不提供，或無法跟 baseline 比較）／`baseline`（基準列的 `ConfigChanges` 與 `VsBaseline`），定義在 `-meta.csv` 的 `legend.*`；同一個實驗每列的基準是誰，看 `-meta.csv` 的 `baseline.label`；`Seed` 是實際使用的種子，沒明設時是 CPLEX 預設值） | 對照 baseline 與歷史 variant 的結果；**`ConfigChanges` 就是「這筆跟 baseline 差在哪」、`VsBaseline` 就是「這筆跟同一個 seed 的 baseline 比，贏、輸還是平手」，框架已經判好，直接抄不要自己比**；確認本輪不是在重跑相同 config |
| `Experiments/<Project>-tuning-r<N>-meta.csv`（說明檔，`Section,Key,Value` 長格式） | `experiment.description`、`run.*` 開始時間與 trial 數、`model.*` 模型類型與各類變數／限制式數量（取自 CPLEX 模型）、`environment.*`、`baseline.*` 的完整設定、`legend.*` 缺值標記定義 | 取 baseline 的有效設定與模型規模；**沒列到的旋鈕就是沒設**（= 用 solver 預設） |
| 前一輪與現行 baseline 的 `Experiments/<Project>-tuning-r<N>-summary.csv`（彙總，一列一組設定） | `IsBaseline`、`Trials`、`Optimal`／`Feasible`／`NoSolution`／`Failed`、`FoundSolution`、`Wins`／`Losses`／`Ties`／`NotCompared` | 每組設定贏幾個、輸幾個一律直接讀這份，NEVER 自己比（§4.2） |
| 前一輪與現行 baseline 的主表 `<Project>-tuning-r<N>-trial.csv` 的 `FirstSolutionMs`／`BoundChange`／`LastBoundChangeMs` 欄，加上同輪 `-trajectory.csv`（只用 label 抽需要的 trial） | `FirstSolutionMs`、`BoundChange`、`LastBoundChangeMs`（框架算好，不重算）；endGap／bound 的改善或停滯型態（看軌跡） | 依 §2.1 重判／確認瓶頸剖面，並只從 §2.2 對應列選候選旋鈕 |
| R0 與最近一次 holdout 結果 | baseline 各 seed 的表現、holdout 有沒有輸 | 把本輪目標寫成可判的條件，而不是「希望更快」 |

**本輪目標 MUST 寫成一個可被當輪數據證偽的句子**，包含：進場情境、baseline 各 seed 的表現、要比贏的那一項、勝出條件、不可犧牲的品質／不變式。例如：

- 情境 A：`在同一停止契約下，variant 在 5 個 seed 上都不比 baseline 慢、至少快 3 個（baseline 各 seed 38–47s），且 objective 在 MipGap 容差內維持 Phase 2 基線。`
- 情境 B：`在同一 TimeLimit 下，variant 在 5 個 seed 上的 endGap 都不比 baseline 大、至少小 3 個（baseline 各 seed 5.9–7.1%），且 incumbent 不退步、BestBound 不越線。`
- 情境 C：`variant 在 5 個 seed 中至少 3 個找到 baseline 找不到的 incumbent，其餘 seed 不輸。`

**設定理由 MUST 對每一個 variant 各寫一條因果鏈**：`哪一個歷史／trajectory 觀測 → 判定的瓶頸 → §2.2 的候選旋鈕 → 此 round 要測的唯一值 → 預期在哪一項贏過 baseline`。只寫「試試看」「CPLEX 常用」「上一輪沒贏所以換一個」不是理由。若重試已否證設定，MUST 明列哪個資料、契約、環境或剖面已變；沒有變化就禁止重試。

本輪結束後，analysis MUST 逐項回填「目標是否達成、預測是否被支持、trajectory 是否支持原先瓶頸判定、下一輪應排除／保留哪個方向」。這份回填就是下一輪的歷史輸入，形成**歷史證據 → 本輪目標 → config 理由 → experiment 結果 → 下一輪歷史證據**的閉環。

### 3.1 一輪只改一個旋鈕

一次改三個然後變快了，你學不到任何可複用的知識，下一輪只能重新亂試。要比較兩個旋鈕就開兩個 variant。

### 3.2 variant 一律從 production baseline `Clone()`

```csharp
// Program.cs 材料段：整個檔只有這一顆具名 baseline
using var project = new OptProject("<Project>");
var productionBaseline = new CplexConfig
{
    MipGap = 0.03,
    TimeLimit = 300,
    Threads = 8,
    ParallelMode = 1,
    Seed = 42,
};

// exp 分支：每個 variant 從 baseline Clone 後只改一顆搜尋策略參數
// MipGap / TimeLimit（停止契約）與 Threads（環境）NEVER 當 variant（§2.0）
int[] seeds = { 11, 22, 33, 44, 55 };
var exp = project.Experiment("tuning-r1", "R1：Emphasis=3 與 GomoryCuts=2 各跟 baseline 比 5 個 seed")
    .AddModel(model)
    .AddConfig("r1-warmup-exclude", productionBaseline.Clone()); // 暖機：不計入、不當 baseline

for (int k = 0; k < seeds.Length; k++)
{
    var baseline = productionBaseline.Clone();
    baseline.Seed = seeds[k];

    var emphasis = baseline.Clone();
    emphasis.Emphasis = 3;

    var gomory = baseline.Clone();
    gomory.GomoryCuts = 2;

    // 順序輪替（§3.4）：第 k 個 seed 從第 k 個設定開始跑，不固定讓 baseline 承擔 cold-start
    var cells = new (string Name, CplexConfig Config)[] { ("baseline", baseline), ("emphasis3", emphasis), ("gomory2", gomory) };
    for (int i = 0; i < cells.Length; i++)
    {
        var (name, config) = cells[(i + k) % cells.Length];
        exp.AddConfig($"r1-{name}-s{seeds[k]}", config);
    }
}

var result = exp.Run();
```

- **NEVER 用 tune delegate 突變共用 config**——一律 `Clone()` 產具體物件
- **NEVER 另立一份「實驗 baseline」**與 production config 平行維護，兩份一定會漂移

### 3.3 experiment 命名

**MUST `tuning-r<N>`（不含專案名），每輪 N 遞增。**

Why: 同名實驗再跑一次是**整組覆寫**。`Run()` 內的 `Save()` 把這次跑的 trials 寫成 `{專案名}-{實驗名}-trial.csv` / `-meta.csv` / `-summary.csv`（有軌跡再加 `-trajectory.csv`）一組檔，同名會整組覆寫並留 `[實驗紀錄覆寫]` WARN，這次沒寫到的舊檔（例：上次有軌跡這次沒有）一併刪掉，`result.Trials` 只有這次跑的內容。檔被 Excel 開著寫不進去時改寫 `-locked-<時間>.csv` 並留 WARN。**一個已 archive 到 `Experiments/` 的 `r<N>` 視為永遠不可再執行**（§3.3.1）——同名重跑會把舊結果整組蓋掉。遞增 `r<N>` 命名是為了讓每一輪的策略方向有自己獨立的一組紀錄檔，要留住結果就換實驗名（`tuning-r1` → `tuning-r2`）。

#### 3.3.1 每輪 exp 設定保存契約

**每個已執行的 R<N> 都 MUST 在 `Program.cs` 的 exp 分支保留一個具名、可辨識的設定區塊**（例如 `// R2 — <Project>-tuning-r2`）。後續 round 只能新增新的 R<N> 區塊；**NEVER 把舊 round 的 variant 改成新 round 的設定，NEVER 刪除舊區塊。**當次 `-- exp` 只註冊／執行目前 round 的 `OptExperiment`，避免把歷史 config 重新跑進本輪；保留舊區塊的用途是重跑與稽核，不是混入本輪 trial。

保存的是**完整的有效設定快照**，不是「當時從 baseline Clone 後改一個欄位」的口頭描述。已 promotion 後 baseline 會改，舊區塊若仍只寫 `baseline.Clone()` 就會隨 baseline 漂移，失去歷史意義。因此，round 結束後 MUST 把每個 baseline／variant 實際生效的 `CplexConfig` 值 materialize 在該 R<N> 區塊（或以明確 snapshot initializer 表達），並保留：experiment name、config label、每個非預設／非 null 旋鈕值、seeds、停止與環境契約版本。每個 config label 仍 MUST 有 `r<N>-` 前綴。

`OptExperiment` 在 `bin/.../Experiment/` 寫出本輪一組檔 `<Project>-tuning-r<N>-trial.csv`（主表）、`-meta.csv`（說明檔）、`-summary.csv`（每組設定的彙總統計），以及 `-trajectory.csv`（**有收集到軌跡才有**）；這組檔合起來是該 round 的完整 Trial／config／統計／convergence 原始證據。**成功後立即複製到專案根 `Experiments/`（§0.1）；前三者缺一不可。**同名 `OptExperiment` 再跑是**整組覆寫**：bin 的舊紀錄會被蓋掉；**一個已 archive 的 r<N> 視為永遠不可再執行**（目標已存在就拒絕複製）；要重做實驗 MUST 使用新的 r<N+1>，並在 History 說明是 replication。

#### 3.3.2 跨輪有效 config 去重

每個 candidate 的「有效 config」= 完整 `CplexConfig` snapshot，**忽略 measurement seed、但包含所有停止契約、環境與策略欄位**。在建立 R<N> plan 前，MUST 對照 `TuningHistory.md` 中所有已 archive round 的 materialized config 區塊，並以主表與 `-meta.csv` 交叉驗證：

- `r<N>-baseline` 是必要對照組，允許與前一輪 baseline 相同。
- 任一 **candidate** 的有效 config 若與任一舊 round candidate 完全相同，MUST 視為重複實驗，**不得執行**。
- 只有使用者明確要求 replication／回歸驗證時才可例外；該 candidate label MUST 加 `-replica-of-r<M>`，History 必須寫明原 round、重跑理由與「不參與新方向的 champion 選擇」。

這條規則防止「看起來換了 round，實際又掃同一組參數」。單純 config label、變數名稱、seed 或輸出檔名不同，**不構成新設定**。

### 3.4 降噪：這一段不做，結論就是雜訊

MIP 有 **performance variability**：換機器、置換 row/column 順序、換 random seed 都可能讓求解時間差數倍，根因是 branch-and-cut 的 imperfect tie-breaking。

| 措施 | 做法 | 為什麼 |
| --- | --- | --- |
| **多 seed** | 每個 variant 跑**同一組 K 個 tuning seeds** | 單 seed 的差異多半是噪音 |
| **seed 是共同因子** | seed 是所有 variant 共用的重複量測，**NEVER 當成一個 variant** | 固定 seed 調參會 over-tune 到那個 seed；把 seed 放進 variant 池排名則是類別錯誤 |
| **warm-up 排除** | 第一個 solve 當 warm-up，**不計入** | cold-start 會固定懲罰第一個跑的 variant |
| **順序輪替** | variants 跨 seed 輪替或隨機化執行順序 | 避免固定讓 baseline 承擔 cold-start |
| **hold-out** | 留 3 個 seed 全程不參與調參（§4.6）；有多 instance 時升級為 train / test instance 分離 | 只在調參用的樣本上漂亮 = over-tuning |
| **共用一份 data** | 所有 cell 引用同一份 `OptData.Load` 結果，載入後視為唯讀 | 兩份資料會中途漂移，實驗結果就不能回答 production 的問題 |
| **決定論設定** | `ParallelMode = 1` + 固定 `Seed` + `DeterministicTimeLimit` | 否則多執行緒計時不可比 |

**雜訊靠「同一個 seed 比、而且一個都不能輸」擋掉**（§4.4）：MIP 換 seed 結果差好幾倍很常見，單看一個 seed 的輸贏多半是運氣；要求 5 個 seed 都不輸、至少贏 3 個，兩個設定其實一樣好時靠運氣過關的機率約 3%，hold-out 再擋一次。所以 NEVER 用單一 seed、也 NEVER 拿不同 seed 的結果互比。

### 3.5 實驗 runner 的行為

**runner 行為的唯一權威是 coding guide [§8.1](../coding/optimfoundation-api-guide.md)** —— 笛卡兒積展開、label 組成、預設安靜、`Clone()` 建 variant、共用一份 data、label 重複丟哪個例外、輸出哪幾個檔、log 檔名時機，一律查那一節。**本節 NEVER 複製它**，改一邊忘另一邊就是規範漂移。

Phase 3 在其上額外要求：

| 規則 | 說明 |
| --- | --- |
| archive | bin 產物成功後**立即**複製到專案根 `Experiments/`；`-trial.csv` / `-meta.csv` / `-summary.csv` 缺一不可，`-trajectory.csv` 有就一併搬；archive 不可變，目標已存在就拒絕、不覆寫（§3.3.1），每輪的檔永久保留 |
| `onSolved` 邊界 | 只有 `OptProject.Solve()` 有這個參數，`OptExperiment.Run()` 沒有——掃描中不要大量寫 solution |
| 一輪一顆 | 一輪只改一個搜尋策略旋鈕（§3.1） |
| 輪次前綴 | config label MUST 帶 `r<N>-`，experiment 名 MUST `tuning-r<N>`（不含專案名，§3.3） |

要覆寫 project-level defaults 時用 `.LoadConfig(projectConfig)`；預設 `ProjectConfig.Quiet()`（不洗 solver log、不匯出）。一般 solver 掃描保留預設即可。

---

### 3.6 CPLEX 內建 tuning tool 基準（零成本，不改程式）

在 R0 之後、R1 之前跑一次。CPLEX 自帶 tuning tool，**所有 API 與 Interactive Optimizer 皆可用**。框架未封裝 `TuneParam`（附錄 B 缺口），但這條路**繞得過去**：

`ProjectConfig.ExportLP = true` 已在 `bin/.../Model/` 產出 `.lp` 檔 → 拿它到 CPLEX Interactive Optimizer 跑 `tools tune`，**一行程式都不用改**。

```text
read <ProjectName>_LP_<timestamp>.lp
set timelimit <契約值>
set mip tolerances mipgap <契約值>
set tune repeat <N>
tools tune
display settings changed
```

**為什麼單一 instance 也值得跑**：調校單一模型時，CPLEX 可**排列（permute）模型後重新 tune** 以取得更穩健的結果（`TuningRepeat`）。model permutation 正是 performance variability 的來源之一，CPLEX 用它人工製造多樣本，正好補上單 instance 缺樣本的洞。`TuningMeasure` 另可選「最差情況最佳化」或「平均最佳化」。

**定位：候選來源與參考基準，NEVER 直接 promote**

| 為什麼不能直接採用 | 處理 |
| --- | --- |
| tune 可能一次改多個參數 → 歸因不了 | 把它建議的每個參數**拆成獨立 variant**，走 §4 逐顆驗證 |
| tune 的內部評分未必等於你的契約與勝出規則 | 用 §4.2 的 lexicographic gate 重新裁決 |
| tune 在自己的執行環境下量測 | 用 §2.3 定版的環境重跑 |

**工具不可用時（無授權 / 不在 PATH）**：**跳過，不中斷流程**，在 `TuningHistory.md` 記一行理由。NEVER 因為這一步失敗就停止 tuning。

## §4 champion 判定

### 4.1 Step 1 · eligibility gate（先淘汰，再排名）

一律淘汰（**三情境共通**）：

- 執行錯誤（`Error` / `NotSolved`）
- `Infeasible` / `Unbounded` —— 同一個凍結模型不該有 trial 變成無解，出現就代表越界，MUST 查明
- 違反 §0.1.1 的**結果不變式**：情境 A 是 objective 偏離基線超過 `MipGap` 容差；情境 B / C 是 objective 比基線差、或 `BestBound` 越過已知最佳 incumbent
- dynamic search 沒有全程啟用：solver log 中 `MIP search method: dynamic search.` 次數少於 trial 數，或出現 `traditional branch-and-cut`（§2.3.1）

**依情境追加的淘汰條件**：

| 情境 | 追加淘汰 | 明確**不**淘汰 |
| --- | --- | --- |
| A（`Optimal`） | 未達專案要求的 objective 或 gap 品質門檻 | — |
| B（`Feasible`） | — | **`Status = Feasible` 本身不是淘汰理由**——它是本情境的常態；gap 比 baseline 大，會在 §4.2 比大小時算輸 |
| C（`TimeLimit`） | — | **「無可行解」不是淘汰理由**——它是 baseline 的起點。無解的 trial 照常逐 seed 比：baseline 也沒解就平手，baseline 有解就算輸 |

★ **情境 C 若把「無可行解」當淘汰理由，會把所有 trial 連同 baseline 一起淘汰光**，本輪得不出任何結論——那不是嚴謹，是判準套錯情境。

**每個被淘汰的 candidate 都要寫淘汰理由。**

### 4.2 Step 2 · 逐 seed 比大小（lexicographic，NEVER 算平均或混成單一分數）

通過 eligibility 的 variant，**每個 seed 各跟同一個 seed 的 baseline 比一次**。依序比，前一項分出勝負就不看後面：

| # | 比較項 | 誰贏 |
| --- | --- | --- |
| 1 | 有沒有找到解（incumbent） | 有解的贏 |
| 2 | 都有解：有沒有證明最佳（`Status = Optimal`） | 證明最佳的贏 |
| 3 | 都證明最佳 | `SolveTimeMs` 短的贏 |
| 4 | 都沒證明（都撞停止條件） | `Gap` 小的贏 |
| — | 都沒找到解 | 平手 |

variant 本身求解失敗（`Infeasible` / `Unbounded` / `Error` / `NotSolved`）算輸；baseline 求解失敗、或那個 seed 沒有 baseline trial → 無法比較（`n/a`）。

**框架已經把結果寫好，NEVER 自己比**：主表每列的 `VsBaseline`（`win` / `lose` / `tie` / `n/a`），`-summary.csv` 每組設定的 `Wins` / `Losses` / `Ties` / `NotCompared`。引用時寫明檔名（`<Project>-tuning-r<N>-summary.csv`）、Config、欄名。

★ **`NodeCount` / `IterationCount` 自 2026-08-25 起會填實際值**，但**不能單獨看**：node 少不等於快——實測 `VariableSelect=3`（強分支）node 更多且慢 4 倍，而 `Emphasis=3` node 更少卻也慢 2.4 倍。要搭配 runtime 一起判讀；`IterationCount / NodeCount` 才看得出每個節點貴不貴。仍 NEVER 只憑它們就下結論。

**NEVER 讓一個比較快但解較差的 trial 勝出**；情境 B 同理——**NEVER 讓一個 gap 收得漂亮但 incumbent 反而變差的 trial 勝出**，§4.1 的結果不變式就該把它擋掉。

### 4.3 Step 3 · 不算平均、不算指標

同一個設定換 seed 可能差好幾倍，平均值會被一兩個特別慢（或撞時限）的 seed 拉走，還得另外決定逾時怎麼算、gap 怎麼平均——算法一複雜就沒人看得懂，也容易算錯。逐 seed 比大小的每一個結果都能直接對回主表那兩列，誰都驗得了。

要描述「快了多少」時，引用主表同一個 seed 的兩個原始數字（例：`s11` baseline 42.1s → variant 30.5s），NEVER 自己算平均或百分比當結論。

比較順序固定，不因情境改變。情境本身變了（例如情境 C 首度找到 incumbent 而升級成情境 B）時，MUST 重跑 R0 取得新的對照組，並在 `TuningHistory.md` 記一筆「情境轉換」分隔線。

### 4.4 Step 4 · 勝出規則：一個都不能輸，至少贏 3 個

**variant 在 K = 5 個 tuning seed 上 `Losses = 0` 且 `Wins ≥ 3` 才算勝出。** 平手可以，但不能輸任何一個。

Why：MIP 換 seed 結果差很多，單看一兩個 seed 的輸贏多半是運氣。兩個設定其實一樣好時，5 個 seed 全部不輸的機率約 3%，hold-out 再擋一次（§4.6）。

- `NotCompared` 不是 0 → 先查原因（baseline 求解失敗？seed 沒對上？），補正後重跑，NEVER 帶著 `n/a` 下結論
- 多個 variant 都勝出 → 選 `Wins` 最多的；還一樣就選改動風險較低、較好解釋的那個，另一個留到下一輪以新 baseline 再比。一次只 promote 一個
- 沒有 variant 勝出 = **無人勝出、保留 baseline**。「無人勝出」是**合法結論**，NEVER 為了有結果硬挑本輪牆鐘時間最小的那一個

### 4.5 職責分離（走 multi-agent 時尤其重要）

**設計 variant 的人不判定 champion。** 設計者對自己的 variant 有偏好，會不自覺放寬 eligibility gate。§8 的拓樸把 T2（設計）與 T4（分析）拆開就是為了這件事。

### 4.6 Step 5 · Hold-out 驗證（promotion 前必經）

champion 用 §3.0 保留的 **3 個未參與調參的 holdout seeds** 重跑。

**協定鐵則：holdout 只能用來估計，NEVER 用來選 config。** 拿 holdout 挑贏家等於偷看測試集，估計值即失去意義。

| 結果 | 動作 |
| --- | --- |
| holdout 的 3 個 seed 都不輸 baseline（`Losses = 0`） | 進 §5 promotion |
| holdout 有任何一個 seed 輸 | **over-tuning** —— 退回 retain，記錄證據，該旋鈕值進「已否證」清單 |
| holdout 上違反 §0.1.1 結果不變式 | **調壞了** —— 直接淘汰，NEVER promotion |

hold-out 輪一樣要跑 baseline（同樣 3 個 holdout seed），框架才有同一個 seed 的對手可比。

多 instance 可用時，同一協定升級成 train / test instance 分離。

---

## §5 promotion 閉環

```text
productionBaseline
  └─ Clone variants
       └─ OptExperiment.Run()
            └─ Trial.Config + Trial.Metrics
                 └─ AI promotion gate（§4）
                      └─ 更新 productionBaseline（§5.1）
                           └─ build + production 重跑 + ValidateRules（§5.3）
```

`OptExperiment` 本身只負責執行與保存證據——**它不會改傳入的 config，也不會替 production 選 winner**。閉環的責任在 AI workflow。

### 5.1 寫回 baseline

1. 從 champion 的 `Trial.Config.SolverSpecific` 取得完整設定快照
2. 把對應值**明確寫成字面值**填進 `Program.cs` 的 `productionBaseline` initializer
3. 更新 initializer 上方 provenance 註解

```csharp
// baseline provenance:
//   來源 experiment: HospitalRostering-tuning-r2
//   champion Trial : Canonical | r2-emphasis=optimal
//   promotion 日期 : 2026-08-08
//   diff           : Emphasis null → 2（其餘不變）
var productionBaseline = new CplexConfig
{
    MipGap = 0.03,
    TimeLimit = 300,
    Threads = 8,
    ParallelMode = 1,
    Seed = 42,
    Emphasis = 2,
};
```

- **NEVER 讓執行中的程式修改 source**
- **NEVER 讓 prod 在 runtime 動態挑「歷史上最快的一列」**
- **NEVER 同時維護「實驗 baseline」與「production config」兩份設定**

### 5.2 先記錄，再驗證

在跑 production 驗證**之前**，先在專案根 `TuningHistory.md` 新增本輪紀錄（§6），production 驗證結果之後補回同一筆。

Why: 驗證失敗時你需要那筆記錄來說明「試過什麼、為什麼 rejected」，事後補寫容易漏。

### 5.3 production 驗證（promotion 的出口 gate）

1. `dotnet build`
2. `dotnet run`（**無參數，走 production 路徑，NOT exp**）
3. 確認 `Status`、objective、`Gap` 與預期一致
4. `onSolved` 回呼 / `ValidateRules` 全數通過
5. 與 promotion 前的 production 結果對照，**依情境套 §0.1.1 對應那一套**：

   | 情境 | PASS 條件 |
   | --- | --- |
   | A | objective 在 `MipGap` 容差內與基線一致（§0.1.1） |
   | B | objective 不差於基線、`BestBound` 未越線、endGap 確實改善 |
   | C | 確實產出了 incumbent（`Status` 從 `TimeLimit` 變成 `Feasible` / `Optimal`），且該解通過 `ValidateRules` |

   ★ 情境 C 的 promotion 一旦 PASS，專案**就此升級為情境 B**：下一輪 MUST 重跑 R0 取得新的對照組並重判剖面（§4.3），並在 `TuningHistory.md` 記一筆「情境轉換」分隔線。這不是額外開銷——瓶頸真的換了一種，沿用舊的判讀會看錯方向。

| 結果 | 動作 |
| --- | --- |
| **PASS** | 該設定成為下一輪 baseline；把驗證結果補回 `TuningHistory.md` 同一筆 |
| **FAIL / 回退** | **撤銷本次 promotion**（還原 `productionBaseline`），在 `TuningHistory.md` 標記 `rejected` 並寫明失敗現象 |

Why 要驗：tuning 改的是 solver 行為，**不該改變解的正確性**。promotion 後 `ValidateRules` 掛了或目標值變了，代表這顆設定動到了不該動的東西。

### 5.4 status.json

只更新下列欄位，**NEVER 整檔覆寫**掉其他階段的欄位：

```json
{
  "phase": "tuning",
  "tuningRound": 1,
  "productionBaseline": "r1-emphasis=optimal",
  "baselineSourceExperiment": "tuning-r1",
  "baselineSourceTrial": "Canonical | r1-emphasis=optimal",
  "promotionVerified": true,
  "updated": "YYYY-MM-DD"
}
```

沒有 promotion 的輪次：`productionBaseline` 保持原值（初次為 `"initial"`），`promotionVerified` 不設或維持前值。

---

## §6 `TuningHistory.md`

專案根，**MUST 納入 source control**。

Why: `bin/.../Experiment/*.csv` 會被 `dotnet clean` 或換 configuration 清掉，不能當唯一 provenance。沒寫進這裡的決策，下一輪就會有人重做同一組實驗。

檔案分三段：**契約區塊**（開頭，整期固定）→ **R0 校準**（一節）→ **各輪決策日誌**（每輪一節，追加）。

### 6.1 契約區塊（檔案開頭，只寫一次）

```markdown
## 契約區塊（整期固定，變更即重跑 §2.3 與 §3.0）

### 停止契約
| 項目 | 值 | 備註 |
| `MipGap` / `TimeLimit` / … | … | 使用者定案日期 |

### 環境契約（§2.3 sizing 定版）
| `Threads` / `ParallelMode` / `MemoryLimitMb` / `NodeFileStrategy` | … | sizing 結果與理由 |

### 量測契約
| 進場情境（§0.0.1） | A `Optimal` / B `Feasible` / C `TimeLimit` |
| 比大小規則（§4.2、§4.4） | 逐 seed 跟 baseline 比；勝出 = `Losses = 0` 且 `Wins ≥ 3`；hold-out 通過 = `Losses = 0` |
| export（experiment 期間） | 全關 |
| 解正確性驗證 | ValidateRules 有 / 無 |
| dynamic search | 每輪確認 `MIP search method: dynamic search.` 次數 = MIP trial 數、`traditional branch-and-cut` 0 次 |

### Phase 2 結果基線（§0.1.1 不變式）
phase2Status / phase2Objective / phase2Bound / phase2Gap / verifiedOn / modelType（§0.0.2）
```

**情境轉換**（C → B、或 B → A）發生時：在該處追加一筆「情境轉換」分隔線，寫明轉換的輪次與重跑 R0 的結果。轉換前後的勝負**不可跨線比較**。

### 6.2 每輪分析報告與決策日誌（目標與證據先行，**預測必須在跑實驗之前寫**）

**每一個已執行 round MUST 在 `TuningHistory.md` 留下完整分析報告，不得只留一行 champion／retain 結論或連到其他草稿。**round 結束、promotion 前，MUST 直接將可稽核結論寫進下列 R<N> 節。History 的該節至少要讓下一位執行者不用重讀 solver log／完整 archive CSV，也能回答：「為何選這些旋鈕、跑了什麼設定、數據怎麼比較、為何 promote／retain／rejected」。

```markdown
## R<N> — YYYY-MM-DD

**本輪目標（跑之前）**：情境、baseline 各 seed 的表現、勝出條件（一個 seed 都不輸、至少贏 3 個）、品質／不變式條件
**決策依據（跑之前）**：
  - 歷史：已讀 R0、R{…}；已否證／不可重試方向：{…}
  - experiment 摘要：`<前輪 experiment>` 的 label／config／metrics；現行 baseline：{…}
  - 收斂軌跡：主表 `FirstSolutionMs`／`BoundChange`／`LastBoundChangeMs` + `<Project>-tuning-r<N>-trajectory.csv` 的 endGap 型態 → 剖面：{…}
**假設**：R0 剖面為 Dual-bound、`BoundChange` 僅 0.300 且跨 seed 一致 → bound 是唯一瓶頸，
        加強切割應能推高 bound_final
**預測**：bound_final > 3.60；5 個 seed 都不輸、至少贏 3 個 ← 跑之前寫死
**每個設定的理由（跑之前）**：
  | config label | 唯一改動 | 證據 → 瓶頸 → §2.2 候選 → 預期效果 |
  | --- | --- | --- |
**實測**：
  - experiment：`tuning-r<N>`
  - exp 設定快照：`Program.cs` 的 `R<N>` 區塊；archive：`Experiments/<Project>-tuning-r<N>-trial.csv`、`Experiments/<Project>-tuning-r<N>-meta.csv`、`Experiments/<Project>-tuning-r<N>-summary.csv`（有軌跡再加 `Experiments/<Project>-tuning-r<N>-trajectory.csv`）
  - machine facts：§6.2.2 的 `TUNING-FACTS R<N>` block（數字只能由此 block 或明列公式導出）
  - seeds：{…}；勝負：抄自 `<Project>-tuning-r<N>-summary.csv` 的列（`Wins`、`Losses`、`Ties`、`NotCompared`）

  | Trial | Status | objective | Gap | solveTimeMs | VsBaseline | BoundChange |
  | --- | --- | --- | --- | --- | --- | --- |

**分析報告**：
  - 剖面／瓶頸證據與本輪候選旋鈕的對應
  - eligibility 淘汰名單與理由；每個 config 贏 / 輸 / 平手幾個 seed，是否符合勝出條件
  - 本輪每個 config label 的有效設定（完整值在 exp snapshot；此處列 baseline → variant diff）
  - 本輪目標、預測與每個設定理由是否被數據／trajectory 支持；下一輪可試與必須排除的方向
  - 整體實驗結果敘述、收斂敘述與分析者見解（§6.2.1；**表格不得取代這三段**）
**裁決**：promote / retain / rejected + 理由（含贏幾個、輸幾個）
**已否證**（累積，後續輪次不重試）：
  | 方向 | 證據 | 否證於 |
```

**各區塊的意義**：

| 段 | 防什麼 |
| --- | --- |
| 本輪目標 + 決策依據 | 先把歷史 experiment、trajectory 與已否證方向轉成可驗證目標，避免重跑舊方向或「希望更快」式盲搜 |
| 假設 + 每個設定的理由 | 逼出「為什麼是這顆、為什麼是這個值」的因果推理，避免盲搜 |
| **預測（跑之前）** | **防事後合理化** —— 看到結果再解釋，永遠編得出理由；先押注才驗得出推理對錯 |
| 實測 + 分析報告 | 設定、數據與判讀直接保存於正式 History，不依賴對話記憶或會被清掉的 bin |
| 裁決 + 已否證 | 讓決策可被下一輪繼承 |

#### 6.2.1 每輪必填的敘事分析（不是表格重述）

每個 R<N> 的 `TuningHistory.md` 節在 Trial 表格之後，**MUST 以完整段落寫出下列三段；缺任一段即視為本輪報告不完整。**數字表格是證據索引，不能代替推理。不得把 solver log 逐行貼上、不得只寫「A 比 B 快／gap 較低」；每個結論都要指回本輪表格或 trajectory 指標。

1. **整體實驗收斂與結果**：交代本輪測了哪些方向、共有多少 variant × seed、哪些通過／未通過 eligibility、每個 config 跟 baseline 逐 seed 比的贏 / 輸 / 平手，以及有沒有 config 符合勝出條件（一個都不輸、至少贏 3 個）。要回答「這輪實驗整體告訴我們什麼」，不能只逐列念結果。
2. **收斂軌跡解讀**：以 `FirstSolutionMs`、`BoundChange`、`LastBoundChangeMs`、endGap、incumbent／BestBound 的變化，描述 baseline 與關鍵 candidate 的求解過程差異。例如：首解是否提早、bound 是否持續推進或早停、gap 為何縮小／不縮小、瓶頸判定是否被支持或推翻。軌跡只含 CPLEX 呼叫 callback 時觀察到的點，框架不補點；正式 objective／gap 與 eligibility 以 experiment Trial metrics 為準。
3. **分析者見解與下一步**：提出從「設定 → solver 行為 → 結果」得到的因果解釋，明確區分事實、推論與不確定性；說明為何 promote／retain／rejected、哪些方向已被否證、下一輪該試什麼或為何該停止。不可把相關性直接寫成確定因果；若證據不足，必須明說「尚無法判定」。

建議寫作順序是：**觀測事實**（表格／trajectory）→ **解讀**（瓶頸或 solver 行為）→ **結論**（本輪目標是否達成）→ **行動**（promotion、retain、下一輪或停止）。每段至少包含一個具體數值或 trajectory 證據與一個清楚判斷。

#### 6.2.2 `TUNING-FACTS` 機械事實區塊（防止亂填數字）

每個 R<N> 節 MUST 在敘事分析前放入一個**逐欄抄自 archive `<Project>-tuning-r<N>-trial.csv` + `-meta.csv`（同一個實驗；用 CSV 解析器讀，例如 PowerShell `Import-Csv`，NEVER 用逗號切字串）** 的 machine facts block；它是 History 中所有 Trial 原始數值的唯一來源，**不得手寫估計值或事後編輯數字**。格式固定如下（欄位來源見 [`optimfoundation-api-guide.md`](../coding/optimfoundation-api-guide.md) §9.2 `OptExperiment` 的輸出說明，markdown 表不是 JSON）：

```text
<!-- TUNING-FACTS:R<N>:BEGIN -->
- experiment：`tuning-r<N>`
- archive：`Experiments/<Project>-tuning-r<N>-trial.csv`、`Experiments/<Project>-tuning-r<N>-meta.csv`、`Experiments/<Project>-tuning-r<N>-summary.csv`（有軌跡再加 `Experiments/<Project>-tuning-r<N>-trajectory.csv`）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| <Model> | r<N>-baseline | 11 | Optimal | 0.0 | 0.0 | 0.0 | 0.0 | baseline | baseline |
| <Model> | r<N>-<config> | 11 | Optimal | 0.0 | 0.0 | 0.0 | 0.0 | win | Emphasis=2 |
<!-- TUNING-FACTS:R<N>:END -->
```

`instance` 填 `Model.Name`（單一模型的專案每列都一樣）；`label` 是主表 `TrialLabel` 去掉 seed 後綴的 config label。**`configDiffFromBaseline` 只列與本輪 baseline 真正不同的欄位，`Seed` 除外**——seed 是重複量測條件，不是策略差異；直接抄主表的 `ConfigChanges` 欄（框架已經算好，NEVER 自己重算）。它同時是跨輪去重的依據：兩輪的 diff 字串相同 = 同一個 candidate 被重跑。`vsBaseline` 直接抄主表的 `VsBaseline` 欄（框架逐 seed 比好的 `win` / `lose` / `tie` / `n/a`）。

敘事或彙總表引用 Trial 數字時，MUST 指向 `TUNING-FACTS` 的 label／欄位；勝負（`Wins`、`Losses`、`Ties`、`NotCompared`）一律引用 `<Project>-tuning-r<N>-summary.csv` 的列（檔名、Config、欄名），NEVER 用 facts 自己重比。沒有出處的數字視為未驗證主張。

**怎麼驗**：本階段**不使用任何外部腳本**。每輪 archive 完成後，逐項執行 [`checklist.md`](checklist.md) 的「每輪 archive 逐項驗收」A–E——開檔、讀值、與 archive `<Project>-tuning-r<N>-trial.csv` 逐欄比對。任何一格對不上（trial 欄位、archive 路徑、label 前綴、跨輪重複）都是 FAIL；**先修 facts 或重出 archive**，NEVER 用敘事文字掩蓋，也 NEVER 手改 archive 檔。

### 6.3 規則

- 決策欄只有三種值：`promote` / `retain` / `rejected`
- **即使本輪沒有勝者也要記 `retain` 與證據** —— 不記等於下一輪重做同一組實驗
- **「已否證」清單跨輪累積，NEVER 重置** —— 這是自動連續執行時避免繞圈的機制
- 歷史節 NEVER 改寫，只追加
- History 是**每輪分析與決策的永久報告**；完整逐 Trial 數值與完整 config 仍由該輪 experiment archive（`-trial.csv` + `-meta.csv` + `-summary.csv`）保存，但 archive 不是唯一紀錄（§3.3.1）
- **History 報告 MUST 有分析者自己的敘事見解**（§6.2.1）；只留 Trial 表、config diff 或一句裁決，均不算完成的 round report
- 每輪 archive 與 `TUNING-FACTS` MUST 通過 `checklist.md` 的「每輪 archive 逐項驗收」A–E；未通過時不得 promotion、不得開始下一輪

---

## §7 停損與退場

停止條件 MUST 機械可判 —— 本階段設計為**啟動後連續執行到停止**，中途不向使用者要指示。

### 7.1 停止條件表（每輪結束時依序檢查，命中即停）

| # | 條件 | 判準 | 動作 |
| --- | --- | --- | --- |
| **B** | **早停 · 整類否證** | 某輪所有 variant 的 bound 軌跡起訖與 baseline 一致 | 該旋鈕類標記「已否證」，**跳過該類剩餘候選**；若剖面對應的類已全數否證 → 停止 |
| **C** | **候選耗盡** | §2.2 中該剖面的候選已全部試過或否證 | 停止 |
| **D** | **連續 3 輪無人勝出** | 連續 3 輪沒有任何 variant 符合勝出條件（一個 seed 都不輸、至少贏 3 個，§4.4） | 停止。把「可能要改模型結構」當**建議**交還使用者（那是新一輪 Phase 1），NEVER 自行升級去動模型 |
| **E** | **promotion 驗證連續 2 輪 FAIL** | production 重跑不通過 | 停止——代表 promotion 流程本身有問題，不是 config 的問題 |
| **F** | **總預算耗盡** | 累計求解時間超過啟動時設定的上限 | 停止並回報已完成的輪次 |
| **G** | **結果不變式破裂且無法定位** | 多個 variant 同時違反 §0.1.1（情境 A 偏離 `phase2Objective` 超過 `MipGap` 容差；情境 B / C `BestBound` 越線） | **立即停止**並回報——這通常代表環境或框架層出了問題，不是旋鈕問題 |
| **H** | §1 判定非 tuning | 進場即判定 | 立即停止，告知退回哪個 phase、為什麼 |
| **I** | **情境 C 候選耗盡仍無 incumbent** | No-incumbent 候選全試過，K 個 seed 仍全部無解 | 停止。結論是「**現行契約下這個規模找不到可行解**」——把「放寬 `TimeLimit` 契約」或「改模型結構」當**建議**交還使用者，兩者都不是本階段能自行決定的 |

**B–G、I 都是正常收尾**，一律走 §5 的交付流程（有 champion 就 promotion，沒有就 retain + 證據），NEVER 半途丟下不寫紀錄。

（原條件 A「雜訊主導早停」已移除：雜訊大時沒有設定能每個 seed 都不輸，D 會自然停下。其餘代號不變。）

★ **情境 C 命中 I 時 NEVER 自行加大 `TimeLimit` 交差**——那是動停止契約（§2.0.1），要使用者拍板。「試遍旋鈕仍找不到可行解」本身就是有價值的交付結論。

### 7.2 總預算

啟動時 MUST 估算並記錄總預算，避免 `TimeLimit` 設得寬時失控：

```text
預估總時間 ≈ (sizing 3×3 + R0 5 + 每輪 (variants + baseline)×5 + 每次 hold-out 2×3) × 單次求解時間
```

單次求解時間取 R0 主表 baseline 各 seed 的 `SolveTimeMs` 最長那次；情境 B / C 就是 `TimeLimit`。**超過預算就停在當前輪次**（條件 F），已完成的輪次照常交付。

### 7.3 不屬本階段的退場

| 情況 | 說明 |
| --- | --- |
| 手動掃描已無方向 | 可考慮自動調參工具（附錄 C.2），但 **NEVER 一開始就丟幾十個參數給 configurator**；單模型情境優先用 §3.6 的 CPLEX 內建 tune |
| 想改模型結構 | 那是新一輪 Phase 1，由**使用者**決定要不要走 |

---

## §8 multi-agent 執行層

**何時用**（不符合就單線跑完，NEVER 為了派工而派工）：

| 判準 | 單線 | multi-agent |
| --- | --- | --- |
| 單次求解時間 | < 60s | ≥ 60s |
| 預估總輪次 | ≤ 5 | > 5 |
| 使用者要求最高保真度 | — | ✅ |

★ **小模型一律單線。** 單次求解 10 秒的模型，派 8 個 agent 的調度開銷遠大於求解本身，而且會把「一鍵跑完」變成一堆等待。單線執行時，§8.4 各 agent 的職責由同一個執行者依序完成，計畫、事實與結論仍直接追加於 `TuningHistory.md`。

★ **職責分離仍要維持**：即使單線，§4.5 的「設計 variant 的人不判定 champion」也要靠**先寫預測再看結果**（§6.2 四段日誌）來達成——預測寫死之後才准跑實驗。

**Why 要拆**：tuning 最常見的失敗不是算錯，是**同一個 agent 既設計又評分**，於是「我設計的 variant 贏了」。

### 8.1 Context 鐵則

- NEVER 把 solver log / 實驗 CSV / Trial 明細**全文**讀進任何 context —— ALWAYS `grep` 抽欄位（Status、objective、Gap、time、nodes）
- NEVER 讀 `.lp` / `.mps` / `.sav` 判斷模型類型或變數組成 —— ALWAYS 看實驗紀錄的 `ModelType` 與數量欄（天條「讀檔」，`../AGENTS.md`）
  Why: 大檔讀取會被截斷，`.lp` 的整數 / 二元變數宣告在檔尾，只看前段會把 MILP 誤判成 LP——實際發生過，半小時 tuning 全建立在錯的前提上
  Why: 單次 MIP 的 solver log 可以上萬行，讀一次就把整個視窗吃光，而你要的只有五個數字
- NEVER 把每輪分析結果留在對話裡累積 —— ALWAYS 直接寫入 `TuningHistory.md` 的 R<N> 節，orchestrator 只持有一張跨輪摘要表
  Why: tuning 是多輪迭代，第 4 輪時前 3 輪的原始數據還留在 context，判斷力已經被稀釋
- MUST 單一 agent 輸入預算 ≤ 800 行；回報上限：執行類 ≤ 15 行、分析類 ≤ 30 行
- MUST orchestrator 只持有：**輪次摘要表、baseline 現值、promotion 狀態**
- MUST 平行 fan-out 一次 ≤ 6 個 agent

### 8.2 拓樸

```text
T0 orchestrator（主對話，不下場調參）
 │
 ├─ T1 scope-guard ─────────► TuningHistory.md 的 R<N> 計畫段    （§1 判定 + 本輪量化目標）
 │      ├─ 要動資料 / 結構 → 停止 Phase 3，退回對應 phase
 │      └─ infeasible → 先派 T1b 取證再退回
 ├─ T1b iis-analyst ────────► 直接回報退回依據                   （§1.1，僅 infeasible 時）
 ├─ T2 variant-designer ────► TuningHistory.md 的 R<N> 計畫段    （§3，一次一旋鈕）
 ├─ T3 experiment-runner ───► archive + TuningHistory facts       （執行 + 抽數，不判優劣）
 ├─ T4 analyst ─────────────► TuningHistory.md 的 R<N> 分析段    （§4 判 champion）
 ├─ T5 promotion-judge ─────► TuningHistory.md 的 R<N> 裁決段    （second-opinion 裁決）
 ├─ T6 promoter ────────────► Program.cs + TuningHistory.md      （§5.1 + §6）
 └─ T7 promotion-verifier ──► status.json + History 驗證結論      （§5.3）
```

| Agent | subagent_type | model | 職責邊界 |
| --- | --- | --- | --- |
| T1 scope-guard | `general-purpose` | `sonnet` | 只判範圍，不動手 |
| T1b iis-analyst | `general-purpose` | `opus` | 讀 `.ilp` 找最小衝突集（取證用，不修） |
| T2 variant-designer | `general-purpose` | `opus` | 只產 config plan，不執行 |
| T3 experiment-runner | `general-purpose` | `sonnet` | 只執行與抽數，不判優劣 |
| T4 analyst | `general-purpose` | `opus` | 只判優劣，不寫回 code |
| T5 promotion-judge | `second-opinion` | （內建） | 只裁決，不動手 |
| T6 promoter | `general-purpose` | `opus` | 只寫回，不重新判定 |
| T7 promotion-verifier | `verifier` | （內建） | 只驗收，不修東西 |

T5 用 `second-opinion` 的理由：promotion 會改寫 production baseline，是長期沿用且事後難察覺的變更——改壞了要到下次 production 求解才發現。所以由不參與實驗設計與分析的角色裁決。

### 8.3 工作區

```text
<repo 根>/
└── Projects/<Project>/
    ├── TuningHistory.md       ← 永久分析與決策報告（每輪一節 + TUNING-FACTS）
    ├── Program.cs             ← 唯一 productionBaseline 與 R<N> config archive 所在
    ├── Experiments/           ← source-controlled Phase 3 archive，每輪永久保留
    │   └── <Project>-tuning-r<N>[-holdout]-{trial,meta,summary,trajectory}.csv（每輪一組檔，只新增不修改）
    └── bin/Experiment/ ← framework 暫存輸出；archive 後可被 clean 清掉
```

`TuningHistory.md` 與 `Experiments/` 是本檔明定的唯一 Phase 3 extension：前者保存計畫、分析與裁決，後者保存不可變原始證據。除這個 extension 外，AI 不得在專案內增加資料夾或中間文件。

### 8.4 派工 prompt（可直接複製，`{{}}` 處替換）

路徑一律**相對 repo 根**，NEVER 用絕對路徑。

#### T0 · orchestrator（主對話）

1. 確認正確性 gate：`status.json` 的 `solveVerified == true`，**並實跑一次確認**；否則停止並要求先完成 Phase 2
   —— 同時讀 `solveStatus` 與 `verifiedOn`，依 §0.0.1 定出**進場情境 A / B / C**，這決定結果不變式怎麼驗、剖面怎麼判。情境 C 且 `verifiedOn` 不是 `small-instance:*` → 停止退回 Phase 2
2. 讀本檔 §0 – §1
3. 讀 `Program.cs` 的 `productionBaseline` 現值（**只讀該 initializer 區塊**，不讀全檔）
4. 依序派 T1 → T2 → T3 → T4 → T5；判定 promote 才派 T6 → T7
5. 每輪結束更新輪次摘要表（orchestrator 唯一持有的跨輪視圖）：

| 輪 | 目標 | variants | champion | 決策 | production 驗證 |
| --- | --- | --- | --- | --- | --- |
| r1 | gap 8% → 3% | 3 | r1-emphasis=optimal | promote | PASS |

#### T1 · scope-guard

```text
目標：判定「這件事是不是 tuning」、定出進場情境，並輸出本輪要打的量化目標。
動機：Phase 3 只動 CplexConfig，前提是模型與資料凍結、正確性已驗。前提不成立卻硬調，會燒掉好幾輪實驗才發現方向從一開始就錯。

輸入：
- 使用者的訴求原文：{{貼在這裡}}
- Projects/{{Project}}/status.json 的 solveVerified / solveStatus / verifiedOn
- `status.json` 與 Phase 2 正式驗證結果（若有）
- Program.cs 的 productionBaseline 現值（只讀該區塊）
規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §0 與 §1（兩節）

先定進場情境（§0.0.1）：
- Optimal → 情境 A，比大小多半比到 runtime
- Feasible（撞限制但有 incumbent）→ 情境 B，比大小多半比到 endGap。**這是可進場的，NEVER 因為「不是 Optimal」就退回 Phase 2**
- TimeLimit（無任何可用解）→ 情境 C，比大小先比有沒有找到解；
  且 MUST 確認 verifiedOn 是 small-instance:*，否則停止退回 Phase 2（模型從未被任何 instance 驗過）
- Infeasible / Unbounded / Error → 不是 tuning，退回對應 phase

再依 §1 的表判定範圍。判不出來 → 明說判不出來並列出你需要的資訊，NEVER 猜一個。

另外輸出本輪的目標，**寫成逐 seed 比大小可以驗收的句子**（§4.4）：
- A：「5 個 seed 都不比 baseline 慢，至少快 3 個」
- B：「timeLimit 不變，5 個 seed 的 endGap 都不比 baseline 大，至少小 3 個」
- C：「5 個 seed 中至少 3 個找到 baseline 找不到的 incumbent，其餘不輸」
目標寫不成這種句子 → 回報卡住，NEVER 用「更快」這種無法驗收的目標。

輸出：直接寫入 `TuningHistory.md` 的 R{{N}} 計畫段。

回報格式：判定（是 tuning / 退回哪個 phase）、**進場情境 A/B/C**、依據（≤3 行）、
本輪量化目標、建議先動的旋鈕方向（不要給具體值，那是 T2 的事）。總長 ≤15 行。
```

#### T1b · iis-analyst（僅 infeasible 時）

```text
目標：讀 IIS 輸出，找出最小衝突約束集合，判斷是模型錯還是資料錯。
動機：Infeasible 幾乎都是模型或資料的錯，不是 solver 的錯。你產出的是「該退回哪裡、退回去要修什麼」的證據，不是修法本身。

輸入：solver log 的 `[衝突限制式摘要] 數量=N 名稱=…` 那一行（名稱數 = N）；要看式子內容才用名稱 grep Projects/{{Project}}/bin/Debug/net8.0/IIS/*.ilp。
NEVER 整檔讀 .ilp；拿到名稱後回 Constraint/ 找對應 .cs 與 Model.md 條目。
規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §1.1（只讀這節）

回報格式：IIS 約束清單（名稱 | Model.md 條目）、根因判定、該退回哪個 phase、
退回後要修什麼（資料 / 結構 / 界限，各附一句後果）。總長 ≤20 行。
NEVER 建議改成 soft constraint。NEVER 自己動手修任何檔案。
```

#### T2 · variant-designer

```text
目標：設計本輪的 config variants 與實驗計畫，寫成可直接貼進 Program.cs 的 code 片段。
動機：一次只改一個旋鈕，才知道是哪個旋鈕起作用。一次改三個然後變快了，你學不到任何可複用的知識。

輸入：`TuningHistory.md` 的 R{{N}} 計畫段、Program.cs 的 productionBaseline 現值、History 契約區塊與全部既有 R 節、前一輪／現行 baseline 的 experiment 摘要與 trajectory 摘要
規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §2 與 §3（兩節）。
旋鈕名稱與可設值 MUST 查 .claude/skills/tuning/cplex-parameter-reference.md，NEVER 憑記憶寫欄位名——那份是查表用，grep 需要的那幾顆即可，不要整份讀進 context。

輸出：補齊 `TuningHistory.md` 的 R{{N}} 計畫段，含：
- 本輪目標：baseline 各 seed 的表現、勝出條件（一個 seed 都不輸、至少贏 3 個）、品質／不變式條件
- 證據清單：讀過的 History round、experiment label／config／metrics、trajectory 指標與判定剖面
- variants 表：| label | 改動的旋鈕 | 值 | 預期效果 | 依據（§2 哪一列） |
- 每個 variant 的因果理由：歷史／trajectory 證據 → 瓶頸 → 候選旋鈕 → 此值 → 預期在哪一項贏過 baseline
- 可直接貼用的 C# 片段
- seeds / warm-up / 執行順序輪替的具體安排

過關條件：
1. 每個 variant 相對 baseline 只有一處差異（逐項比對，寫在表上）
2. builder 的 experiment name 為 `tuning-r{{N}}`，與歷史不重名；`OptExperiment.FullName` 與檔名前綴才是 `{{Project}}-tuning-r{{N}}`
3. baseline 來自 productionBaseline.Clone()
4. 每個旋鈕先由 cplex-parameter-reference.md 分類，再到 sibling `OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs` 核對實際 property、型別與語意
5. 每個 variant 都有可追溯的 evidence-to-config 理由；沒有重試已否證方向，除非明列剖面／契約／環境的變化
6. 本輪目標可由當輪 Trial 與 trajectory 機械判定達成或失敗

回報格式：variants 表（≤5 列）、experiment name、seeds 設定。總長 ≤15 行。
```

#### T3 · experiment-runner

```text
目標：執行本輪 OptExperiment，把結果抽成一張精簡的 Trial 表。
動機：你只負責跑與抽數，NEVER 判斷誰贏——判優劣是另一個 agent 的事，你先下結論會影響它。

輸入：`TuningHistory.md` 的 R{{N}} 計畫段。
做法：
1. 把 plan 的 C# 片段套進 Program.cs 的 exp 分支（只動 exp 分支，NEVER 動 productionBaseline）
2. dotnet build → dotnet run --project <csproj> -- exp
3. archive 前先確認：`Projects/<Project>/Experiments/` 若已有這一輪任何一個檔（`<Project>-tuning-r<N>-*.csv`）= 這輪已 archive 過，FAIL，停止並改用新 round 名稱 r<N+1>；否則把 bin 的 `<Project>-tuning-r<N>-trial.csv`、`-meta.csv`、`-summary.csv`、`-trajectory.csv`（有才搬）複製到 `Projects/<Project>/Experiments/`——目標已存在就拒絕、不覆寫，停下回報
4. 從 archive 主表 `<Project>-tuning-r<N>-trial.csv` 抽每個 Trial 的：label、`ConfigChanges`、Status、objective、BestBound、Gap、SolveTimeMs、seed、`VsBaseline`；基準完整設定查同一個實驗的 `-meta.csv`；各組贏 / 輸 / 平手讀同一個實驗的 `-summary.csv`，NEVER 自己比
   NEVER 整份 CSV 讀進 context——用 grep / 逐欄抽取
5. 從 archive 主表抽 baseline 與各 candidate 的 `FirstSolutionMs`、`BoundChange`、`LastBoundChangeMs`（框架已從軌跡算好，NEVER 自己重算）；要看 endGap／bound 停滯型態才用 label grep `<Project>-tuning-r<N>-trajectory.csv`
6. 依 archive `<Project>-tuning-r<N>-trial.csv` + `-meta.csv`（用 `Import-Csv` 解析）逐欄抄出本輪 `TUNING-FACTS` block（markdown 表，§6.2.2），填入 History；逐項跑 `checklist.md` 的「每輪 archive 逐項驗收」A–E，全 PASS 才交給 analyst
7. solver log 同理：只 grep Status、objective、gap、time 幾行

輸出：將下列 Trial 摘要與 archive 參照直接寫入 `TuningHistory.md` 的 R{{N}} 節：
| Trial label | seed | Status | objective | Gap | solveTime(s) | nodes |

另附 trajectory 摘要：| Trial label | FirstSolutionMs | BoundChange | LastBoundChangeMs | endGap | 與 baseline 的差異 |

過關條件：
1. plan 的每個 variant × seed 都有對應列
2. warm-up 那次已標記排除
3. 未修改 productionBaseline（附 git diff 摘要佐證）
4. 每個 Trial 能回連到本輪的 config label／snapshot，且 baseline 與 candidate 的收斂摘要已抽出
5. archive 完整（`-trial.csv` / `-meta.csv` / `-summary.csv` 必備）、TUNING-FACTS 與 archive 一致、沒有跨輪重複 candidate config

回報格式：Trial 表（≤15 列）、執行總時間、異常（crash / 無解 / 逾時）清單。總長 ≤20 行。
NEVER 下「哪個比較好」的結論。
```

#### T4 · analyst

```text
目標：依評分規則從本輪 Trial 選出 champion，或判定「無人勝出，保留 baseline」。
動機：只在一兩個 seed 上快一點不是改善——MIP 換 seed 結果本來就差很多。一個 seed 都不能輸、至少贏 3 個才算勝出，否則你 promote 的是運氣。

輸入：`TuningHistory.md` 的 R{{N}} 節（本輪目標、進場情境與 Trial 摘要）。
規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §4 與 §3.0（兩節）

先確認本輪的**進場情境**（triage 已定），再照 §4 的五個 Step 依序做。勝負直接讀：
- 主表每列的 `VsBaseline`（win / lose / tie / n/a）與 `-summary.csv` 的 `Wins` / `Losses` / `Ties` / `NotCompared`，NEVER 自己比、NEVER 算平均
- 情境 C 的 objective / Gap 全是 NaN，NEVER 把 NaN 當 0 參與任何計算——框架比大小時先看有沒有找到解
- 情境 C「無可行解」不是淘汰理由（§4.1），當成淘汰會把 baseline 一起淘汰光

輸出：直接補齊 `TuningHistory.md` 的 R{{N}} 分析段，含：eligibility 淘汰名單 + 理由、
每個 config 的贏 / 輸 / 平手、champion（或「無人勝出」）+ 理由、**本輪目標／預測／各設定理由是否被 Trial 與 trajectory 支持**、下一輪建議保留／排除方向；另寫出可移入 History 的三段敘事：**整體實驗結果、收斂軌跡解讀、分析者見解與下一步**（§6.2.1）

過關條件：
1. 每個被淘汰的 candidate 都寫了淘汰理由
2. 勝負引用 `<Project>-tuning-r<N>-summary.csv` 的 `Wins` / `Losses`（檔名、Config、欄名），沒有自己比、沒有算平均
3. champion 符合勝出條件（`Losses = 0` 且 `Wins ≥ 3`）；`NotCompared` 不是 0 時已查明原因
4. 「無人勝出」是合法結論，NEVER 為了有結果硬選一個
5. 逐項回覆本輪目標與設定理由是否成立；不能只說「變快／沒變快」
6. 三段敘事都有具體數據／trajectory 證據與清楚判斷；不是表格逐列改寫

回報格式：champion（或 retain）、每個 config 的贏 / 輸 / 平手、淘汰名單一行。總長 ≤30 行。
```

#### T5 · promotion-judge（second-opinion）

```text
目標：獨立裁決本輪該 promote champion 還是 retain 現有 baseline。
動機：promotion 會改寫 Program.cs 的 production baseline，是長期沿用且事後難察覺的變更。所以由不參與實驗設計與分析的你來裁決。

輸入（只給這三份，不給實驗過程）：
- `TuningHistory.md` 的 R{{N}} 計畫、Trial facts 與分析段

裁決依據：
1. analysis 的 eligibility gate 有沒有放水（拿 trials 原始數據覆核，不要只看結論）
2. champion 是否真的一個 seed 都沒輸、至少贏 3 個（拿主表 `VsBaseline` 逐列覆核）
3. 是否只在單一 instance / 少數 seed 上贏——那不足以 promote
4. 有沒有隱藏代價（runtime 變快但 gap 變差、記憶體用量暴增）
5. 本輪量化目標是否真的達成

輸出：直接寫入 `TuningHistory.md` 的 R{{N}} 裁決段。

回報格式：
- 裁決：PROMOTE <champion label> / RETAIN baseline
- 理由（≤5 行）
- 你不同意 analysis 的地方（無則寫「無」）
- 若 PROMOTE：promotion 後要特別盯的風險（≤2 條）
總長 ≤20 行。NEVER 修改任何檔案。
```

#### T6 · promoter

```text
目標：把 champion 的設定寫回 Program.cs 的 productionBaseline，並在 TuningHistory.md 留下永久 provenance。
動機：bin/.../Experiment/*.csv 會被 clean build 清掉。沒寫進 TuningHistory.md 的決策，下一輪就會有人重做同一組實驗。

前置：`TuningHistory.md` 的 R{{N}} 裁決必須是 PROMOTE；
RETAIN 則跳過寫回，只做 TuningHistory 記錄。

規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §5.1、§5.2 與 §6（三節）

過關條件：
1. Program.cs 只有一顆具名 productionBaseline，且值與 champion 完全一致
2. initializer 上方 provenance 註解含 experiment + Trial label + 日期 + diff
3. `Program.cs` exp 分支已新增／保留本輪 `R<N>` 的完整有效 config snapshot；舊 round 區塊仍存在且未被覆寫
4. TuningHistory.md 該節已納入本輪 analysis 報告、experiment／archive CSV 參照與每個 variant 的 baseline → variant diff；before/after production diff 逐項可讀
5. 全部 archive 通過 `checklist.md` 的「每輪 archive 逐項驗收」A–E
6. git diff 只動了 Program.cs、TuningHistory.md 與本輪的 Experiment archive

回報格式：before/after config diff（逐項）、TuningHistory 節標題、git diff 檔案清單。總長 ≤15 行。
```

#### T7 · promotion-verifier

```text
目標：驗證 promotion 後的 production 路徑仍然正確，並把結果補回 TuningHistory.md。
動機：tuning 改的是 solver 行為，不該改變解的正確性。如果 promotion 後 ValidateRules 掛了或目標值變了，代表這顆設定動到了不該動的東西。

規範：讀 .claude/skills/tuning/solver-tuning-guide.md 的 §5.3（只讀這節）

照 §5.3 的五個步驟做，並依該節的表判定 PASS / FAIL。

回報格式：build 結果、Status/objective/gap、ValidateRules 結果、before/after 對照、
最終判定（PASS/FAIL）。總長 ≤20 行。
```

### 8.5 每輪完成條件（T0 執行）

一輪完成 = 下列**全部**成立：

1. 開跑前已在 History／plan 寫明本輪量化目標、證據來源、剖面與每個 config 的 evidence-to-config 理由
2. T4 有明確結論（champion 或「無人勝出」），且以 Trial + trajectory 回覆本輪目標與預測是否成立
3. T5 裁決完成
4. PROMOTE → T6 寫回 + T7 production 驗證 PASS；RETAIN → `TuningHistory.md` 有 retain 記錄與證據
5. `Program.cs` exp 分支保留該 R<N> 的完整有效 config snapshot，且本輪 `OptExperiment` archive（`<Project>-tuning-r<N>-trial.csv` + `-meta.csv` + `-summary.csv`）可依檔名找到
6. 專案根 `Experiments/` 已 archive 本輪 `<Project>-tuning-r<N>-trial.csv`、`-meta.csv`、`-summary.csv`、`-trajectory.csv`（有收集到軌跡才有），且**所有** round 都通過 `checklist.md` 的「每輪 archive 逐項驗收」A–E
7. `TuningHistory.md` 該輪包含完整 analysis 報告、前輪證據參照、收斂軌跡敘事、分析者見解與下一輪保留／排除方向
8. `status.json` 已更新（§5.4）

---

## §9 常見錯誤與反模式

| 症狀 | 真正原因 | 修法 |
| --- | --- | --- |
| 撞時限的專案被擋在 Phase 2 出不來，說「要 Optimal 才能進 Phase 3」 | 把 `Feasible` 當成 Phase 2 的 FAIL | `Feasible` 是合法交付狀態，也是 Phase 3 情境 B 的典型進場（§0.0.1）。Phase 2 手上沒有任何合法工具能修「太慢」 |
| 情境 B 掃了一輪，自己拿 runtime 比得出「全部平手」 | 自己比、而且比錯欄——每個 trial 都跑滿 `TimeLimit` | 不要自己比：讀 `VsBaseline` / `Wins` / `Losses`，框架兩邊都撞時限時會比 gap（§4.2） |
| 情境 C 一開跑就「所有 trial 都被淘汰、包含 baseline」 | 把「無可行解」當 eligibility 淘汰理由 | §4.1：情境 C 下無解不是淘汰，照常逐 seed 比（baseline 也沒解就平手） |
| 情境 C 算出一堆 `NaN` 或詭異的 0 | 自己拿主表手算，把 `NaN` 的 objective / Gap 當 0 參與平均 | 不要手算：讀 `-summary.csv` 的 `FoundSolution` 與 `Wins` / `Losses`，框架比大小時先看有沒有找到解 |
| 調了三輪都「好像有變快」但說不出哪個旋鈕有效 | 一輪改了多個旋鈕 | 回 §3.1，一次一個 |
| 同名重跑後 bin 的舊紀錄被蓋掉 | 同名 experiment 是**整組覆寫** | 改用 `tuning-r<N>` 遞增命名區分輪次（§3.3），已 archive 的輪次 NEVER 重跑 |
| promote 之後 production 反而變慢 | 只跑一個 seed，贏的是運氣 | 每個 variant 跑同一組 5 個 seed，照 §4.4 一個都不能輸、至少贏 3 個，再過 §4.6 hold-out |
| baseline 每次跑的時間都不一樣 | 沒固定 `Seed` / `ParallelMode` | `ParallelMode = 1` + 固定 seed + `DeterministicTimeLimit` |
| `NodeFileStrategy` 設了但沒作用 | `MemoryLimitMb` 會強制 `MIP.Strategy.File = 0` | 設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy`（附錄 A ★） |
| `CS1061` 找不到 `epGap` / `workThreads` / `mipEmphasis` 等欄位 | 用了已廢止的 camelCase 舊名 | 對照 `cplex-parameter-reference.md` 改用 PascalCase（附錄 A ★★） |
| 調到 timeout 都還在 gap 5% | 已到旋鈕的極限 | §7 停損，把「改模型結構」當建議交還使用者 |
| 交付後 `git diff` 有 `.csv` / `Constraint_*.cs` | 越界改了凍結範圍的檔 | 全部還原，該訴求依 §1 退回對應 phase |
| `TuningHistory.md` 只有 promote 的輪次 | retain 的輪次沒記 | 補記——不記等於下一輪重做同一組實驗 |
| 下一輪 exp 設定覆寫了上一輪 | 只把 `baseline.Clone()` 當歷史、沒有 materialize snapshot | 依 §3.3.1 保留每個 R<N> 的完整有效 config 區塊；歷史 archive（CSV）只能輔助佐證，不能是唯一紀錄 |

### 反模式

❌ **主動建議 tuning** —— 使用者提出才做
❌ **未過正確性 gate 就調** —— 更快地算出錯答案
❌ **跳過 R0 直接掃 variants** —— 沒先看清楚 baseline 在這組 seed 上的表現、情境與剖面，候選旋鈕只能盲選（§3.0）
❌ **把 `MipGap` / `TimeLimit` 當速度旋鈕掃** —— 它們是停止條件，兩個不同契約的 trial 比 runtime 沒有意義（§2.0）
❌ **把 `Seed` 放進 variant 池排名** —— 它是重複量測的自變數，「贏」只證明變異大（§2.0）
❌ **在 threads 未定版時就開始比** —— threads 改變重跑結果的差異，比出來的輸贏說不清是誰造成的（§2.0.2）
★ **`NodeCount` / `IterationCount` 自 2026-08-25 起會填實際值**，但**不能單獨看**：node 少不等於快——實測 `VariableSelect=3`（強分支）node 更多且慢 4 倍，而 `Emphasis=3` node 更少卻也慢 2.4 倍。要搭配 runtime 一起判讀；`IterationCount / NodeCount` 才看得出每個節點貴不貴。仍 NEVER 只憑它們就下結論。
❌ **`Emphasis = 2` 當成推 bound 的手段** —— 推 bound 是 `3`（BESTBOUND）（§2.2）
❌ **看到 objective 變好就採用** —— 同契約下 objective 應恆等於 Phase 2 基線，變了代表越界（§0.1.1）
❌ **跑完實驗才補寫「預測」** —— 事後永遠編得出理由，預測必須在跑之前寫死（§6.2）
❌ **一開始就塞滿參數** —— Phase 3 分不出是哪個旋鈕造成差異；其餘旋鈕一律留 `null` 用 CPLEX 預設
❌ **用 soft constraint 讓 infeasible「有解」** —— 改語意，屬 Phase 1
❌ **只跑 experiment 就宣稱完成** —— 沒 promotion 等於白跑
❌ **同一個 agent 既設計 variant 又判定 champion** —— 設計者會不自覺放寬 gate
❌ **只留 `bin/.../Experiment/*.csv` 當證據** —— clean build 就沒了
❌ **讀 solver log / experiment CSV 全文** —— 上萬行，要的只有五個數字
❌ **拿單次牆鐘時間排序** —— 同一個 seed 跟 baseline 比，5 個 seed 一個都不能輸、至少贏 3 個（直接讀 `-summary.csv` 的 `Wins` / `Losses`）
❌ **在 train instance 上的改善數字當交付結論** —— 那是 over-tuning
❌ **改 OptimFoundation 框架本體** —— `dlls/` 唯讀；缺旋鈕見附錄 B

---

## 附錄 A · 旋鈕查表（已移出本檔）

[`cplex-parameter-reference.md`](cplex-parameter-reference.md) 只提供調校分類導航。實際 property、型別與語意一律查 sibling `OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs` 或 developer guide；NEVER 從本文件推測欄位。

分類與「能不能進 variant 池」的判準仍在 §2.0；搜尋策略的全池清單在 §2.2.1。

下列四條註記是**規範不是查表資料**，所以留在本檔（§2.2、§9 都引用它們）：

> ★ **`MemoryLimitMb` 與 `NodeFileStrategy` 的順序雷**：框架的 `LoadConfig()` 在設定 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`。要做「記憶體爆 → 溢寫節點檔」，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy = 2/3`，否則被覆蓋成 0。
>
> ★ **而且 `File = 0` 比 CPLEX 預設更糟**：CPLEX 的 `MIP.Strategy.File` 預設是 `1`（節點壓縮後仍留在記憶體）。框架強制成 `0` 等於**把壓縮策略關掉**——樹超過 `MemoryLimitMb` 時沒有任何緩衝，直接吃記憶體到 OOM。另注意 `MemoryLimitMb` 管的是 **live tree 大小**，不是行程總記憶體；CPLEX 官方建議把它設得明顯低於可用記憶體，好讓壓縮提早啟動。
>
> ★★ **命名已統一為單一套 PascalCase**（框架 2026-08 起）。過去的「抽象旋鈕（`ITunableConfig`）＋ camelCase CPLEX 欄位」雙軌制**已取消**，`epGap` / `workThreads` / `mipEmphasis` / `varSel` / `randomSeed` 這類舊名**不再存在**，寫了直接 compile error。
>
> `CplexConfig` 實作 `ISolverConfig`（`ITunableConfig` 已併入），跨 solver 共通的那組（`Emphasis` / `Seed` / `FeasibilityTol` / `OptimalityTol` / `RootAlgorithm` / `Presolve` / `HeuristicEffort` / `MemoryLimitMb` / `TimeLimit` / `MipGap` / `Threads`）與 CPLEX 專屬欄位現在是**同一套名字**，不再有「兩邊都寫」的風險。
>
> 舊 code 或舊文件出現 camelCase 名 → 視為待遷移，以本表為準。
>
> ★★★ **`ProjectConfig` 不是 tuning 的對象**。它現在只剩輸出開關（`EnableSolverLog` / `ExportLP` / `ExportMPS` / `ExportSol` / `ExportIIS`），專案身分交給 `new OptProject(name, retentionDays)`，`CplexConfig` 才管 solver 怎麼解。實驗快照只擷取 solver 那層，所以輸出開關不會混進 tuning 記錄。

### A.1 建立變數的正確 API（旋鈕表的常見誤用）

調參時若需要改動 `Program.cs`，**變數建立一律 `engine.BuildVars<T>(sets...)`**，型別由類別名前綴決定（`VariableB_` Binary / `VariableC_` Continuous / `VariableI_` Integer）。

- 一般 code 使用 `BuildVars<T>`；`BuildBVs` / `BuildCVs` / `BuildIVs` 仍是有效公開 API，只在自訂 bounds 或維護既有明確型別建構時使用（權威在 Phase 2 guide §3）
- **Phase 3 不該動到變數層**：需要改變數宣告或 bounds = 你在改模型，依 §1 退回 Phase 1 / 2

---

## 附錄 B · 框架尚未提供的接口

動 `CplexConfig` / `OptEngine` 屬**改 OptimFoundation 框架本體** → Core + Cplex DLL 必須一起 rebuild 並依 `dlls/README.md` 回填 `dlls/` 與 `VERSION.txt`。這**不在 Phase 3 範圍內**，列出來是為了在遇到瓶頸時知道「這條路目前走不通、要走得先做框架維護」。

**2026-08 更新：原本列在這裡的參數缺口已全部補上。** ZeroHalf / Disjunctive / Implied 切割、進階 presolve
（`AggregatorLimit` `PresolvePasses` `PresolveReduce`）、記憶體 emphasis（`MemoryEmphasis`）、內建 tune 的
量測設定（`TuningMeasure` `TuningRepeat` `TuningTimeLimit`）現在都是 `CplexConfig` 的正式欄位。
分支優先級也有了部分替代：`PriorityOrderType` 可以讓 CPLEX **自動產生**一份優先序（依成本遞減／bound range／
cost per coefficient count），搭配 `UsePriorityOrder` 開關使用。

**剩下的 API 缺口**都不是「加一個欄位」能解決的，要暴露新的方法：

| 缺口 | CPLEX 對應 | 影響哪個手段 | 建議補法 |
| --- | --- | --- | --- |
| 逐變數分支優先級 | `Cplex.SetPriority` / order file | 手動指定某些變數先分支（`PriorityOrderType` 只能自動產生） | `OptEngine.SetBranchPriority(var, p)` |
| 自動調參方法 | `Cplex.TuneParam` | 附錄 C.2 的 baseline（`Param.Tune.*` 的設定欄位已有，但沒有觸發 tune 的方法） | `OptEngine.AutoTune()` |
| Heuristic callback | `Cplex.HeuristicCallback` | 自訂啟發式 | 暴露 callback 註冊點 ⚠️ |
| Lazy / user cut callback | `LazyConstraintCallback` / `UserCutCallback` | 延遲生成限制式 | 暴露 callback 註冊點 ⚠️ |

框架內部目前只有私有的 `MIPInfoCallback`（收斂軌跡擷取，經 `ITrajectorySource.EnableTrajectory()` 開啟），沒有公開的 heuristic / lazy / start hook。

> ⚠️ **補上這三個 callback 缺口會關閉 dynamic search（§2.3.1）。**
> `MIPInfoCallback` 是 **informational** callback，與 dynamic search 相容（不會退回 traditional B&C），所以目前的軌跡擷取不會觸發這個事件；但它仍會改變搜尋路徑、傾向變慢，要和正式求解對照的驗證要關軌跡（§2.3.1）。
> 但 heuristic / lazy constraint / user cut 全都是 **control** callback：只要存在，CPLEX 就關閉 dynamic search、發出 warning、退回 static branch and cut。那是**數量級的效能事件**。
> 因此若日後補上任一個：**所有既有 tuning 數據作廢**，MUST 重跑 §2.3 sizing 與 §3.0 R0，並在 `TuningHistory.md` 記一筆「求解演算法變更」分隔線。

---

## 附錄 C · 研究地圖（已移出本檔）

Algorithm Configuration 的領域名稱、自動調參工具（irace / SMAC3）與引用清單移到
[`../../reference/tuning-research-map.md`](../../reference/tuning-research-map.md)。

**那是文獻地圖，不是規則。** 何時讀：§2 的手動旋鈕掃完仍不達標、要導入自動調參工具、要寫報告引用文獻時。
