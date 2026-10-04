# tuning · 交付前自檢

> 規則本體在同層 `solver-tuning-guide.md` 與上層 `../AGENTS.md`。
> **本 skill 不依賴任何外部腳本**：所有驗證都是這份清單裡「開檔、讀值、比對」的動作，沒有 `.ps1` / `.py` 要跑。
> 本檔只是交付前的勾選面。**沒有實驗證據的項目不准打勾。**
> ★ = 錯了不會報錯，但結論整批作廢。

## 進場 gate

- [ ] 使用者主動提出調校需求（不是我自己起意）
- [ ] `dotnet build` 通過
- [ ] Status 落在可進場的三態（`Optimal` / `Feasible` / `TimeLimit`）—— **沒有因為「不是 `Optimal`」就退回 `coding`**
- [ ] ★ **進場情境已判定**（A / B / C）並寫進 `TuningHistory.md` 契約區塊
- [ ] ★ **已讀 `ModelType`**（正式求解紀錄 `<P>-solve-trial.csv` 的 `ModelType` 欄，或任一輪 `<P>-tuning-r<N>-trial.csv` 的 `ModelType` 欄，框架讀 CPLEX 模型判定，未自行推論）並寫進契約區塊；`LP` 走規範 §0.0.2 分支
- [ ] ★ **模型結構來源正確**：類型取自主表 `ModelType` 欄，變數 / 限制式數量取自 `-meta.csv` 的 `model.<Model>.*`，**沒有讀 `.lp` / `.mps` 推論**；且 `ModelType = LP` ⇔ `model.<Model>.binaryVarCount = integerVarCount = semiContinuousVarCount = semiIntegerVarCount = sosCount = 0`，兩邊一致
- [ ] ★ **進場情境已判定**（A `Optimal` / B `Feasible` / C `TimeLimit`），決定結果不變式怎麼驗；勝負一律逐 seed 比大小（§4.2），不另選指標
- [ ] 情境 C 時，`status.json` 的 `verifiedOn` 是 `small-instance:*`（模型至少被某個 instance 驗過）
- [ ] `coding` 的解驗證協定四步已**實跑**確認過（不只是看 `status.json`）
- [ ] ★ 已記錄 Phase 2 結果基線 `phase2Status` / `phase2Objective` / `phase2Bound` / `phase2Gap` / `verifiedOn`
- [ ] exp 分支已是 Phase 2 交付的 R0-ready 形狀（名稱 / `r0-` label / marker / baseline × 5 seeds），**未重寫**；不符處已記成 finding 並就地補正

## 範圍界線

- [ ] 這輪確實是「模型與資料固定、只嫌慢或收斂不了」的情境
- [ ] 要換資料 / 改結構的訴求已被擋在門外並退回對應 phase
- [ ] `Infeasible` / `Unbounded` 沒有被當成 tuning 題目處理（已附 IIS 或缺界證據退回）
- [ ] 沒有為了「讓它變 `Optimal`」而放寬 `MipGap` 或加大 `TimeLimit`（那是動停止契約，要使用者拍板）

## 契約與環境（S0 / S1）

- [ ] 契約區塊已寫進 `TuningHistory.md` 開頭：停止契約 / 環境契約 / 量測契約 / Phase 2 基線
- [ ] ★ **停止條件**（`MipGap` `TimeLimit` `NodeLimit` `IntegerSolutionLimit` 容差，共 27 顆）整期固定，**未進 variant 池**
- [ ] ★ **執行資源**（`Threads` `ParallelMode` `MemoryLimitMb` `NodeFileStrategy`，共 9 顆）已定版並凍結——同一台實機**沿用 Phase 2 的值**，只有換機 / 換 CPLEX 版本 / Phase 2 未明設才重跑 S1 sizing
- [ ] `ParallelMode = 1` 已明設（不依賴 CPLEX 預設）
- [ ] experiment 期間 LP / MPS / Sol export 全關（計時不含檔案 I/O）
- [ ] ★ 每輪用**正面證據**確認 dynamic search：solver log 中 `MIP search method: dynamic search.` 次數 = MIP trial 數，`traditional branch-and-cut` 0 次（不是「沒看到 warning」）
- [ ] 發現契約可能訂錯時，只記成 finding 回報，**未自行變更**

## R0 校準（S2，硬 gate）

- [ ] Phase 2 驗證管線跑過的 `-- exp`（實驗名 `tuning-r0`）不必先刪：Phase 3 正式跑 R0 會整組覆寫 bin 的 `<Project>-tuning-r0-*.csv`（Phase 2 不 archive，沒有衝突）
- [ ] ★ **R0 已完成**：baseline 跑完 K = 5 個 seed，主表每列 `VsBaseline = baseline`，R0 呈現與判定的情境一致
- [ ] 瓶頸剖面已依主表 `FirstSolutionMs` / `BoundChange` / `LastBoundChangeMs` / `SolveTimeMs` 分類，沒有自己從 `-trajectory.csv` 重算（No-incumbent / Dual-bound / Primal-search / Node-cost / 數值不穩）
- [ ] R0 的 K 個 seed 通過 §0.1.1 對應那一套不變式（A 在 `MipGap` 容差內一致；B / C 不退步 + bound 不越線）
- [ ] 情境 C 的「K 個 seed 全部無解」**沒有被誤當成早停條件**（那是本輪要打的目標）
- [ ] 已指定 3 個 holdout seeds，且全程未參與調參
- [ ] 契約健檢探針（`MipGap = 0`）已跑並記成 finding，未進排名；情境 B / C 若放大了 `TimeLimit`，倍數已記錄
- [ ] `ModelType = LP` 時：剖面記「LP」、未跑 `MipGap = 0` 探針、dynamic search 記 N/A、候選只用 §2.2 的 LP 列（§0.0.2）
- [ ] 總預算已估算並記錄

## 實驗設計（S3）

- [ ] 以現行 production baseline 為起點，variants 全部用 `Clone()` 產生
- [ ] ★ **一輪只改一個旋鈕**
- [ ] ★ 候選**只從剖面對應那一類**取，沒有查全表盲掃
- [ ] ★ `Seed` 是共同因子，**未當成 variant 排名**
- [ ] 所有 trial 共用同一份已載入的 `data`，載入後未被修改
- [ ] experiment 名帶輪次（`tuning-r<N>`，不含專案名），且 `Experiments/` 尚無 `<Project>-tuning-r<N>-*.csv`（與歷史不重名，已 archive 的輪次 NEVER 重跑）
- [ ] 有 warm-up 排除 / 順序輪替；label 符合 `r<N>-<config>-s<seed>`，baseline 那組含 `baseline`、暖機含 `warmup`（`-summary.csv` 依此分組與排除）
- [ ] TUNING-FACTS 是用 CSV 解析器（例如 `Import-Csv`）讀主表抄出，不是用逗號切字串

## 每輪 archive 逐項驗收（S3 每輪結束、進入裁決前）

> **沒有腳本可跑——逐項自己開檔比對。**
> 每一列都寫了「怎麼查」與「期望值」；查不到、對不上一律 FAIL，FAIL 未修正前**不得 promotion、不得開始下一輪**。
> `<P>` = 專案名，`<N>` = 本輪輪次；每輪一組檔，檔名固定是 `<P>-tuning-r<N>[-holdout]-trial.csv` / `-meta.csv` / `-summary.csv` / `-trajectory.csv`，整個檔就是本輪的紀錄，不必篩列。

**A. 檔案齊備**

- [ ] 專案根有 `Experiments/`、`TuningHistory.md`、`Program.cs` 三者。
- [ ] ★ 本輪從 bin 的 `Experiment/` 複製到 `Experiments/` 時，目標已存在就拒絕、不覆寫（archive 不可變）。
- [ ] `Experiments/` 下本輪檔案齊全且檔名逐字正確：`<P>-tuning-r<N>[-holdout]-trial.csv`（主表，一列一 trial，18 欄）、`-meta.csv`（說明檔，放模型大小/環境/基準完整設定）、`-summary.csv`（彙總，14 欄，每組設定一列，跟 baseline 逐 seed 比的贏 / 輸 / 平手由框架判好）三者缺一不可。`-trajectory.csv` **只有在真的收集到收斂軌跡時才會產生**；軌跡只含 CPLEX 實際呼叫 callback 時觀察到的點，框架不補點；純 LP、或 presolve / root 就解完時 callback 不會被呼叫，就不會有這個檔（關閉收集時也沒有）。
- [ ] 本輪的實驗名逐字符合 `tuning-r<數字>`（檔名前綴 `<P>-tuning-r<數字>`）；沒有 `test`、`exp1`、`tmp` 這類命名。
- [ ] `Experiments/` 若已有這一輪任何一個檔（`<P>-tuning-r<N>[-holdout]-{trial,meta,summary,trajectory}.csv`）→ 這輪已 archive 過，NEVER 重跑同名，要重做改開 `r<N+1>`。
- [ ] archive 時已確認「只能新增」：複製的目標檔在 `Experiments/` 都不存在；已存在就拒絕、停下回報，NEVER 覆寫。
- [ ] 歷史輪次的 archive 仍在（每輪永久保留、不可變，NEVER 因為「舊的沒用了」刪除或覆寫舊檔）。

**B. 三處交叉引用對得上**

- [ ] `Program.cs` 的 exp 分支有本輪 marker，**整行逐字**是 `// R<N> — <P>-tuning-r<N>`（破折號是 `—`，不是 `-`）。
- [ ] `TuningHistory.md` 有 `## R<N>` 標題（行首、`##` 一級）。
- [ ] 該節內文有引用 archive 路徑字串 `Experiments/<P>-tuning-r<N>-trial.csv`，並標明本輪的實驗名（`tuning-r<N>`）。

**C. `TUNING-FACTS` 與 archive 檔逐欄比對**（§6.2.2）

- [ ] `TuningHistory.md` 的 R<N> 節有成對的 `<!-- TUNING-FACTS:R<N>:BEGIN -->` / `:END -->`，中間是 `- experiment：`／`- archive：` 兩行加一張 markdown 表（不是 ```json``` 區塊）。
- [ ] facts 的 `experiment` 值**逐字**等於 `tuning-r<N>`（大小寫敏感，不含專案名；facts 沒有 `RunId`）。
- [ ] facts 的 `archive：` 那行列出的 `<P>-tuning-r<N>-trial.csv` / `-meta.csv` / `-summary.csv`（有軌跡再加 `-trajectory.csv`）路徑（`Experiments/<P>-tuning-r<N>-*.csv`）指向 A 節那些實際存在的檔。
- [ ] facts 表格的資料列數 **等於** 本輪 `<P>-tuning-r<N>-trial.csv` 的資料列數。
- [ ] ★ **逐列、逐欄對照** 本輪 `<P>-tuning-r<N>-trial.csv` 的每一列（順序相同，一個都不能跳）：

  | facts 欄位 | 在主表 `<P>-tuning-r<N>-trial.csv` 的來源 | 比對方式 |
  | --- | --- | --- |
  | `instance` | `Model` | 逐字相同 |
  | `label` | `TrialLabel` 去掉 `-s<seed>` 後綴 | 逐字相同 |
  | `seed` | `Seed` | 數值相同 |
  | `status` | `Status` | 逐字相同 |
  | `objectiveValue` | `ObjectiveValue` | 逐字相同（含 `NaN`，NEVER 改寫成 0） |
  | `bestBound` | `BestBound` | 同上 |
  | `gap` | `Gap` | 同上 |
  | `solveTimeMs` | `SolveTimeMs` | 同上 |
  | `vsBaseline` | `VsBaseline`（`baseline` / `win` / `lose` / `tie` / `n/a`） | 逐字相同，照抄不改 |
  | `configDiffFromBaseline` | `ConfigChanges`（基準列寫 `baseline`、與基準無差異寫 `none`，照抄不改；查基準完整設定要看同一個實驗的 `<P>-tuning-r<N>-meta.csv` 的 `baseline.*`） | 只列真正不同的欄位，**Seed 除外**（seed 是重複量測條件，不是策略差異） |

- [ ] 對照時發現任何一格不符 → **先修 facts 或重出 archive**，NEVER 用敘事文字掩蓋，也 NEVER 手改 archive 檔。

**D. label 與跨輪去重**

- [ ] 本輪 `<P>-tuning-r<N>-trial.csv` 每一列的 `TrialLabel` 都以 `r<N>-` 開頭的 config 段結尾（本輪前綴正確，沒有沿用上一輪的 `r<N-1>-`）。
- [ ] ★ 把本輪每個 **非 baseline、非 replica** candidate 的 `configDiffFromBaseline` 拿去比對 `TuningHistory.md` 的**已否證清單**與歷史各輪的 facts：**不得與任何歷史 candidate 的有效設定完全相同**（忽略 Seed 後仍相同 = 重跑舊實驗）。
- [ ] label 含 `-replica-of-r<M>` 的 replica candidate，History 本輪節**明確寫了為什麼要重跑**（replication 理由）；沒寫理由的 replica 視為重複實驗。

**E. 數字只能從 facts 來**

- [ ] 本輪敘事、彙總表與裁決引用的每個 Trial 數字，都能指回 `TUNING-FACTS` 的某個 label／欄位。
- [ ] 勝負（`Wins`、`Losses`、`Ties`、`NotCompared`）一律引用 `<P>-tuning-r<N>-summary.csv` 的列（檔名、Config、欄名），**沒有自己比**。
- [ ] 沒有任何數字是憑印象、憑對話記憶或憑 `bin/` 裡已被清掉的檔寫出來的。

## 判定與證據

- [ ] 每個 Trial 都有 `Status`、objective、`Gap`、`SolveTimeMs`（`TimeLimit` 的 trial 數值欄為 `NaN` 是正常的）
- [ ] ★ **未「只憑」`NodeCount` / `IterationCount` 做判定** —— 自 2026-08-25 起框架會填實際值，但 node 少不等於快（實測 `VariableSelect=3` node 更多且慢 4 倍、`Emphasis=3` node 更少也慢 2.4 倍），MUST 與 runtime 一起判讀
- [ ] ★ **未把 `NaN` 的 objective / `Gap` 當 0 參與任何計算**
- [ ] ★ 勝負直接讀主表 `VsBaseline` 與 `-summary.csv` 的 `Wins` / `Losses`，**沒有自己比、沒有算平均或任何統計指標**（sgm、PAR10、θ、gap 平均、改善量都不用；§4.2、§4.3）
- [ ] ★ 情境 B / C **沒有拿 runtime 排名**（全部跑滿時限，排出來必然全部平手）
- [ ] ★ 情境 C **沒有把「無可行解」當淘汰理由**（那會連 baseline 一起淘汰光）
- [ ] ★ champion **一個 seed 都沒輸、至少贏 3 個**（`Losses = 0` 且 `Wins ≥ 3`）；`NotCompared` 不是 0 時已查明原因
- [ ] ★ 每個 trial 都通過 §0.1.1 不變式；違反者已淘汰並記錄理由
- [ ] 每個被淘汰的 candidate 都寫了淘汰理由
- [ ] 紀錄不只存在 `bin/.../Experiment/`（會被清掉）

## Hold-out（S4）

- [ ] ★ champion 已用 3 個未參與調參的 seed 重跑
- [ ] ★ holdout **只用來估計，未用來挑選 config**
- [ ] hold-out 有任何一個 seed 輸 baseline 者已退回 retain 並記為 over-tuning
- [ ] 情境 C：holdout 的 3 個 seed **各自都找到 incumbent**（只在 tuning seed 上找得到 = over-tuning）

## Promotion 閉環（S5）

- [ ] champion 的**完整設定**已明確寫回 `Program.cs` 的 production baseline
- [ ] baseline 上方 provenance 註解已更新（來源 experiment、Trial label、日期、config diff）
- [ ] `TuningHistory.md` 已追加本輪紀錄（**先寫紀錄再驗證**）
- [ ] promotion 後**重新 build + 跑無參數 production**（export 開回來），通過驗收
- [ ] ★ production 通過該情境的 PASS 條件（A = objective 在 `MipGap` 容差內等於 `phase2Objective`；B = objective 不差於基線且 endGap 改善；C = 確實產出 incumbent）；不通過則已撤銷 promotion 並記 `rejected`
- [ ] 情境 C promotion PASS 後已升級為情境 B：重跑 R0 取得新的對照組，並記「情境轉換」分隔線
- [ ] 若結論是保留原 baseline：`TuningHistory.md` 一樣記了 retain 決策與證據

## 紀錄格式

- [ ] 每輪四段齊全：假設 / 預測 / 實測 / 裁決
- [ ] ★ **「預測」是在跑實驗之前寫的**，不是事後補的
- [ ] 「已否證」清單跨輪累積，未重置
- [ ] 歷史節未被改寫，只有追加

## 一致性（越界檢查）

- [ ] ★ `git diff --name-only` 只有 `Program.cs`、`TuningHistory.md`、本輪新增的 `Experiments/<Project>-tuning-r<N>[-holdout]-trial.csv` / `-meta.csv` / `-summary.csv`（+ `-trajectory.csv` 有才有、+ `status.json`）；已 archive 的檔不得出現在 diff
- [ ] ★ `Program.cs` 內部 diff **只有** baseline 值、provenance 註解、exp 分支 variant 定義 —— model chain 未出現在 diff
- [ ] ★ 已 archive 的 `Experiments/` 檔只能新增、不能修改：diff 中它們只能是新增檔（狀態 A），任何修改（M）都是越界
- [ ] `Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Solution/`、`Model.md`、csproj 全程未被改動
- [ ] 沒有以 tuning 名義移項 / 改號 / 翻轉方向 / 四捨五入 / 改輸入精度
- [ ] 沒有為了「讓它有解」而加 soft constraint 或 penalty

## 停損與交付

- [ ] 停止原因明確對應規範 §7.1 的 A–I 其中一條，並已寫進回報
- [ ] 連續 3 輪**無人勝出**時已停止，沒有自行升級去動模型結構
- [ ] 情境 C 候選耗盡仍無 incumbent（條件 I）時，**沒有自行加大 `TimeLimit` 交差**——已把放寬契約 / 改模型當建議交還使用者
- [ ] 回報含：進場情境、輪次數、每輪一行摘要（含贏 / 輸 / 平手）、剖面、champion 或 retain + 理由、停止原因、production 驗證結果
- [ ] `status.json` 已更新（`tuningRound` / `productionBaseline` / `promotionVerified` 等），未覆寫其他階段欄位
