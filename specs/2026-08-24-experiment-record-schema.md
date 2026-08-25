---
title: Experiment 紀錄格式 v1 —— baseline 全量一次、每個 trial 只記 delta，並補齊調參決策所需的診斷指標
status: approved
created: 2026-08-24
updated: 2026-08-24
modules: [framework-core, framework-cplex, skills-docs, projects]
---

# Experiment Record Schema v1

## Summary

把 `OptExperiment` 的輸出從「每個 trial 重抄一份 183 顆設定的巢狀 JSON + 一張混了 11 顆設定的寬 CSV」改成 **3 份扁平 CSV**：整期不變的東西（模型身分、環境契約、baseline 全量設定）在 `-meta.csv` 存一次，每個 trial 只記「基於哪個 baseline、額外改了什麼、結果如何」，收斂軌跡獨立成檔。同時補齊 Phase 3 判瓶頸所需但目前缺席的診斷指標（`NodeCount`／`IterationCount`／`TFeasMs`／`TStallMs`／`DeltaBound`）與收斂軌跡本身。

服務兩個讀者：**人**（Excel 打開就看得懂誰改了什麼、結果如何）與 **AI**（整份塞進 context 也只有幾 KB）。

## Motivation / Why

以 `Projects/CandyBlending` 的 R0 實測為證據：

| 現況問題 | 量測值 |
| --- | --- |
| 每個 trial 重抄全量設定 | JSON **113,311 bytes / 15 trials = 7,554 bytes per trial** |
| 絕大多數是 null | `solverSpecific` 183 顆，**非 null 只有 9 顆 → 95.1% 是 null** |
| 真正的資訊量 | trial 之間實際差異 = **1 顆**（`Seed: 11 → 22`） |
| CSV 看不出差異 | CSV 只攤平 11 顆設定；改 `GomoryCuts` 時兩列**完全一樣** |
| 診斷指標永遠是空的 | CSV 有 `NodeCount`／`IterationCount` 欄但值為空——**5008 變數的真 MIP 跑 10 秒也一樣空** |
| 分不出是哪次跑的 | 同名 experiment 會 append（刻意設計，用 `RunAt`+`Label` 去重），但 CSV 上不易一眼分辨批次 |
| 缺模型身分 | trial 頂層只有 `label / runAt / config / metrics`，`Model` 與 config 黏成 `"Canonical \| r0-baseline-s11"` 一個字串 |
| 紀錄虛胖的第二個來源 | `CplexConfig` 有 **6 個 property initializer**（`Threads` `RowRead` `MemoryLimitMb` `MipGap` `OptimalityTol` `FeasibilityTol`）。`CandyBlending` 只設 5 顆，snapshot 卻有 9 顆非 null——**4 顆是框架自己塞的** |

**`CplexConfig` 的 initializer 不只讓紀錄變胖，還有行為副作用。** 該檔第 12 行已把契約寫成「**`null` = 不設**，`Configuration()` 不呼叫 `SetParam`，沿用 CPLEX 預設」，那 6 個 initializer 是對自身契約的例外：

- `Threads = 32` **偏離** CPLEX 官方預設（官方是 `0` = automatic）——在 8 核機器上等於預設超額訂閱
- `MemoryLimitMb = 2048` 的值雖與官方相同，但 `Configuration()` **套用它時會強制 `MIP.Strategy.File = 0`**（同檔第 27 行自述），而 CPLEX 官方預設是 `1`（壓縮後留記憶體，更安全）。等於**每一個從未要求過此事的專案，node file 策略都被靜默降級**

**收斂軌跡本身沒有問題**（2026-08-24 實測更正）：`Trial.Capture` 內部已自動呼叫 `EnableTrajectory()`，`OptEngine` 以 `MIPInfoCallback` 收點，`Experiment.SaveCore` 在有點時寫出 `-trajectory.csv`。用 `HospitalRostering_Generator`（5008 變數）實測產出 **453 列軌跡、baseline 36 個點**，欄位與 `solver-tuning-guide.md` §2.1 宣稱的完全一致。

`CandyBlending` 的軌跡是空的，只是因為它是**純 LP**——沒有分支定界過程，MIP callback 不會觸發。**本規格因此不動軌跡的任何部分。**

規範自己也在繞過格式缺陷：§8.1 寫著「NEVER 整份 JSON 讀進 context，grep 抽欄位」——把紀錄改稀疏，那條規則就可以刪掉。

## Scope

### In Scope

- **A · `OptimFoundation.Core`**：`Trial` 新增 `RunId` / `TrialId` / `Model` 欄位；`SolveMetrics` 新增導出指標；writer 改為 3 份 CSV；停產 per-trial 全量 JSON
- **B · `OptimFoundation.Cplex`**：補 `NodeCount` / `IterationCount` 取值；同一 experiment 多次執行以 `RunId` 分隔（**軌跡擷取已可用，不動**）
- **B2 · `CplexConfig` 回歸純接口**：移除全部 6 個 property initializer，`null` 一律代表「不設、不動 CPLEX、不記錄」。連帶消除 `MemoryLimitMb` 觸發的 node-file 降級副作用
- **C · AI-Modeling 規範**：`solver-tuning-guide.md`（§2.1 剖面資料來源、§3.3 archive 三件、§6.2.2 `TUNING-FACTS` schema、Fatal 的 NodeCount 條款）、`tuning/checklist.md`（每輪 archive 逐項驗收 A–E）、`AGENTS.md` Phase 3 出口契約
- **D · 專案端**：**零 code 改動的驗證**——`Template/` 與 `Projects/CandyBlending` 只換 DLL 重編，不動任何 `.cs`，直接驗證新格式產出正確

### Out of Scope

- 改變 tuning 方法論本身（一輪一顆、θ 門檻、champion 判定、promotion 流程一律不動）
- 改變 `OptModel` 的建模 API 或 Phase 2 的四段 `Program.cs` 模板
- 舊格式的向後相容轉換工具（**確認移除舊格式**，見 Edge Cases）
- 自動化的剖面判定（本規格只負責**產出剖面所需的資料**，判定仍由 Phase 3 依 §2.1 人／AI 執行）
- **任何需要專案端呼叫或設定的開關**——所有行為在框架內部決定，消費專案的 `Program.cs`（四段模板）一個字都不改
- 舊格式與新格式並存的切換選項（一次換掉，不做雙軌）
- **收斂軌跡的擷取、開關與 `-trajectory.csv` 的格式**——實測證實已正常運作，維持現狀
- 移除 JSON——`Experiment.SaveCore` 用它做 append 去重、`Experiment.Load` 讀它，**改為稀疏化而非停產**

## User Stories / Use Cases

1. As a **調參人員**, I want to 用 Excel 打開一份 CSV 就看出「這輪跑了哪些 candidate、各自在 baseline 上改了什麼、結果誰比較好」, so that 不必開 JSON 或寫程式就能做初步判讀。
2. As a **AI orchestrator**, I want to 把整輪紀錄讀進 context 而不爆量, so that 我能直接比較 trial、算 sgm 與 θ，而不是靠 grep 片段拼湊。
3. As a **調參人員**, I want to 從紀錄看出瓶頸屬於哪一類（首解太慢／bound 推不動／node 太貴／數值不穩）, so that 下一輪的候選旋鈕有依據而不是盲掃。
4. As a **稽核者**, I want to 確認報告裡的每個數字都能回溯到某一次執行的某個 trial, so that 「數字是編的」這件事查得出來。
5. As a **調參人員**, I want to 同一個 experiment 重跑時舊紀錄不被污染也不混淆, so that 我能比較「同樣設定在不同時間跑的結果」。

## Acceptance Criteria

### 檔案結構

- [ ] **消費專案零 code 改動**：`Template/` 與 `CandyBlending` 的 `.cs` 一個字不改，只換 DLL 重編即產出新格式
- [ ] 跑完一輪 exp，`bin/.../Experiments/` 出現**恰好 3 份**檔：`<Exp>.csv`、`<Exp>-trajectory.csv`、`<Exp>-meta.csv`
- [ ] **不再產出** per-trial 全量設定的 `.json`
- [ ] 三份檔都能用 `(RunId, TrialId)` join；`-meta.csv` 用 `RunId` 對應

### 紀錄程度（各檔的責任邊界）

- [ ] `-meta.csv` **只記整期不變的東西**：schema 版本、experiment 身分與開始時間、模型身分、環境契約、**baseline 的全量設定（只列非 null，實測應為 9 列量級）**
- [ ] `<Exp>.csv` **一列一 trial**，內容是「身分 + 基於哪個 baseline + 額外改了什麼 + 結果 + 診斷指標」；**不重複 baseline 已有的設定**
- [ ] `<Exp>-trajectory.csv` **一列一收斂點**，只放時間序列
- [ ] baseline trial 的 `DiffKnobs` 欄為空；candidate 的 `DiffKnobs` 只列**與 baseline 不同**的旋鈕（`Seed` 除外，它另有專欄）
- [ ] **`-meta.csv` 的 `baseline` 段只列專案真正設定過的旋鈕**：移除 initializer 後，`CandyBlending` 應為 **5 列**（`MipGap` `TimeLimit` `Threads` `ParallelMode` `Seed`），不再出現 `RowRead` / `MemoryLimitMb` / `FeasibilityTol` / `OptimalityTol` 這些沒人設過的欄位
- [ ] `CplexConfig` 任一 property 未被明確賦值時，`Configuration()` **不對 CPLEX 呼叫該顆的 `SetParam`**，且該顆**不出現在任何紀錄檔**
- [ ] 任一份 CSV 中，**null 佔比 < 20%**（現況 JSON 為 95.1%）

### 體積

- [ ] `<Exp>.csv` 每列 **< 300 bytes**（現況 JSON 7,554 bytes/trial）
- [ ] 一輪 5 seeds 的 R0，三份檔合計（不含 trajectory）**< 5 KB**

### 診斷指標可用

- [ ] `NodeCount` / `IterationCount` 有實際數值（非空），MIP 模型上 > 0
- [ ] `TFeasMs`（首個 incumbent 時間）、`TStallMs`（bound 最後改善時間）、`DeltaBound`（bound 淨推進）三欄有值；模型無 incumbent 時 `TFeasMs` 為空（**不是 0**）
- [ ] `TrajectoryPoints > 0`（軌跡由框架內部固定開啟，**呼叫端不必也無法設定**），且 `-trajectory.csv` 的該 trial 列數等於此值
- [ ] 依這些欄位可判出 §2.1 的六類剖面，**不必開啟或解析任何其他檔案**

### 執行分隔

- [ ] 每一列（三份檔皆同）帶 `RunId`，值為該次 experiment 執行的開始時間戳
- [ ] 同一個 experiment 連跑三次，三次的列可用 `RunId` 完全分開；同一 `RunId` 內 `TrialId` 唯一
- [ ] `-meta.csv` 記錄該 `RunId` 的開始時間
- [ ] **每輪自足**：連續四輪（R0 → promotion → R1 → promotion → R2 → R3）之後，**只讀 R3 的三份檔**即可完整重建 R3 當時的全部設定，不需要 R0–R2 的任何檔案

### 模型身分

- [ ] `Model` 是獨立欄位，**不再與 config label 黏成單一字串**
- [ ] 多模型 experiment（`AddTrial(model, label, config)`）產出的列，可用 `Model` 欄直接分組比較
- [ ] `-meta.csv` 記錄每個 Model 的 `VarCount` / `ConstraintCount`，且主表每列重複這兩個值作為「模型未變」的逐 trial 斷言

### 人與 AI 都讀得懂

- [ ] Excel 直接開啟三份 CSV：中文不亂碼、數字不被誤判、日期正確解析
- [ ] `NaN` / `Infinity` 以字面值寫入，**與「空 = 未設定」語意分開**
- [ ] 浮點數 round-trip 不失精度（`5450.000000000001` 不得被截成 `5450`）
- [ ] 一位沒讀過本規格的人，只看 `<Exp>.csv` 的表頭就能說出每欄的意思

### 軌跡成本驗證（安全閥）

- [ ] R0 以「開軌跡」與「關軌跡」各跑一次 5 seeds，runtime 的 `sgm` 差異落在該專案 θ 以內
- [ ] 若差異超過 θ → 判定軌跡擷取會污染量測，**本規格的軌跡部分退回重新設計**（例如只在專門的診斷輪開啟）

### 規範同步

- [ ] `solver-tuning-guide.md` §2.1 的資料來源與欄位描述與實際產出一致
- [ ] Fatal 的「NEVER 憑 `NodeCount` / `IterationCount` 判斷瓶頸（框架不填）」改寫為補值後的正確用法
- [ ] `tuning/checklist.md` 的 archive 驗收 A–E 對齊 3 份檔
- [ ] `Template/` 跑一次 exp 產出的檔案符合上述全部條件

## Module Interactions

- **framework-core（`OptimFoundation.Core`）**
  - `Trial`：新增 `RunId`、`TrialId`、`Model`；`Label` 只留 config label（不再含 model 前綴）
  - `SolveMetrics`：新增 `TFeasMs`、`TStallMs`、`DeltaBound`、`TrajectoryPoints`；`NodeCount` / `IterationCount` 由 engine 實際填值
  - `Experiment`：新增 `RunId`、`StartedAt`；`Save()` 改寫 3 份 CSV
  - writers：`CsvExperimentWriter`（主表）、`TrajectoryCsvWriter`（軌跡）、新增 `MetaCsvWriter`；**移除** `JsonExperimentWriter` 的預設輸出
  - `Experiment.Load` 改由 `CsvExperimentWriter.Read` 支撐
- **framework-cplex（`OptimFoundation.Cplex`）**
  - `OptExperiment`：簽名不變；`Run()` 內部產生 `RunId`，並在建 engine 後、`Solve()` 前**固定**呼叫 `engine.EnableTrajectory()`
  - `OptEngine`：求解後取 `NodeCount` / `IterationCount` 填入 `LastMetrics`；由 `Trajectory` 計算三個導出指標
  - `CplexConfig`：移除 6 個 property initializer（`Threads` `RowRead` `MemoryLimitMb` `MipGap` `OptimalityTol` `FeasibilityTol`），全部改為 `null`；`Configuration()` 的套用邏輯不變（本來就是 null 即跳過）
- **skills-docs（AI-Modeling `.claude/skills/tuning/`）**
  - `solver-tuning-guide.md` §2.1 / §3.3 / §6.2.2 / §9 Fatal
  - `tuning/checklist.md` 每輪 archive 逐項驗收
  - `.claude/skills/AGENTS.md` Phase 3 出口契約的 archive 件數
- **projects**（**不改任何 `.cs`**）
  - `Template/`：換 DLL 重編，跑一次 exp 驗證 3 份檔符合 Acceptance Criteria
  - `Projects/CandyBlending`：同上，重跑 R0 交叉驗證

## API Design

### 新增／變更的 public API

**硬約束：public API 的「新增」只發生在資料結構上，不新增任何要呼叫端使用的方法。** 軌跡擷取由 `OptExperiment.Run()` 內部固定開啟。

```csharp
// OptimFoundation.Cplex.OptExperiment —— 簽名不變
// Run() 內部：建 engine 後、Solve() 前固定呼叫 engine.EnableTrajectory()，呼叫端無感

// OptimFoundation.Core.Trial
public string RunId  { get; set; }      // 新增：該次 experiment 執行的識別（時間戳）
public int    TrialId { get; set; }     // 新增：RunId 內唯一的序號
public string Model  { get; set; }      // 新增：模型名，與 Label 分離
public string Label  { get; set; }      // 變更語意：只放 config label（原本是 "{model} | {config}"）

// OptimFoundation.Core.SolveMetrics
public double? TFeasMs          { get; set; }   // 新增：首個 incumbent 出現時間；無解為 null
public double? TStallMs         { get; set; }   // 新增：bound 最後一次改善的時間
public double? DeltaBound       { get; set; }   // 新增：bound 首點到末點的淨變化
public int     TrajectoryPoints { get; set; }   // 新增：軌跡點數；0 = 未開啟
public long?   NodeCount        { get; set; }   // 既有欄位，改為實際填值
public long?   IterationCount   { get; set; }   // 既有欄位，改為實際填值

// OptimFoundation.Core.Experiment
public string   RunId     { get; set; }   // 新增
public DateTime StartedAt { get; set; }   // 新增

// OptimFoundation.Cplex.CplexConfig —— 簽名不變，移除 initializer（behavioral breaking change）
public int?    Threads        { get; set; }   // 原 = 32   → null
public int?    RowRead        { get; set; }   // 原 = 30000 → null
public double? MemoryLimitMb  { get; set; }   // 原 = 2048  → null
public double? MipGap         { get; set; }   // 原 = 1e-4  → null
public double? OptimalityTol  { get; set; }   // 原 = 1e-06 → null
public double? FeasibilityTol { get; set; }   // 原 = 1e-06 → null
```

**這是 behavioral breaking change**，但衝擊可控：Phase 2 的 §8.4 交棒契約本來就要求 `productionBaseline` 明設 `Threads` / `ParallelMode` / `Seed`，合規專案（`Template`、`CandyBlending`）行為不變。不合規的專案會從「隱形的 32 執行緒」變成 CPLEX automatic——那正好讓它一直沒遵守契約這件事浮出來。

### 呼叫端變化

**零。** 既有的四段 `Program.cs` 實驗段完全不動：

```csharp
var exp = new OptExperiment("<Project>-tuning-r0", "R0 校準：baseline × 5 seeds");
exp.AddModel(model);
foreach (var seed in new[] { 11, 22, 33, 44, 55 })
{
    var config = productionBaseline.Clone();
    config.Seed = seed;
    exp.AddConfig($"r0-baseline-s{seed}", config);
}
exp.Run();
```

換上新 DLL 重編後，同一段程式碼即產出 3 份新格式檔案。

## Data Model

三份 CSV 的完整 schema。**欄位順序凍結，NEVER 重排**（消費端會寫依欄位位置的公式）。

### 1 · `<Experiment>.csv` —— trial 主表（一列一 trial）

| # | 欄位 | 型別 | 空值語意 | 說明 |
| --- | --- | --- | --- | --- |
| 1 | `RunId` | string | 不可空 | 該次執行的開始時間戳，格式 `yyyyMMdd-HHmmss` |
| 2 | `TrialId` | int | 不可空 | `RunId` 內的序號，從 1 起 |
| 3 | `Model` | string | 不可空 | 模型名（`OptModel.Name`） |
| 4 | `TrialLabel` | string | 不可空 | config label，例 `r0-baseline-s11`、`r1-GomoryCuts=2` |
| 5 | `BasedOn` | string | 不可空 | candidate 填該輪 baseline 的 `TrialLabel`（例 `r1-baseline`）；**baseline 自己填 `production`**，表示它就是現行 production baseline，避免自我指涉 |
| 6 | `DiffKnobs` | string | 空 = 就是 baseline | 與 baseline 不同的設定，格式 `Knob=Value`，多顆以 `;` 分隔（`Seed` 不列入） |
| 7 | `Seed` | int | 不可空 | 重複量測的軸，獨立成欄以便 group by |
| 8 | `Status` | string | 不可空 | `SolveStatus` 列舉值 |
| 9 | `ObjectiveValue` | double | `NaN` = solver 無值 | round-trip 精度 |
| 10 | `BestBound` | double | `NaN` = solver 無值 | 純 LP 時為佔位值 |
| 11 | `MipGap` | double | `NaN` = solver 無值 | 純 LP 時為佔位值 |
| 12 | `RunTimeMs` | double | 不可空 | |
| 13 | `TFeasMs` | double | **空 = 從未找到 incumbent** | 情境 C 主指標 |
| 14 | `TStallMs` | double | 空 = 無軌跡 | bound 最後改善時間 |
| 15 | `DeltaBound` | double | 空 = 無軌跡 | bound 淨推進 |
| 16 | `NodeCount` | long | 空 = solver 未回報 | 判 Node-cost 剖面 |
| 17 | `IterationCount` | long | 空 = solver 未回報 | 搭 `NodeCount` 算每 node 迭代數 |
| 18 | `TrajectoryPoints` | int | 不可空 | **0 = 軌跡未開啟**（自檢欄） |
| 19 | `VarCount` | int | 不可空 | 逐 trial 斷言模型未變 |
| 20 | `ConstraintCount` | int | 不可空 | 同上 |
| 21 | `Note` | string | 可空 | |

### 2 · `<Experiment>-trajectory.csv` —— 收斂點（一列一點）

| # | 欄位 | 型別 | 說明 |
| --- | --- | --- | --- |
| 1 | `RunId` | string | join key |
| 2 | `TrialId` | int | join key |
| 3 | `TrialLabel` | string | 冗餘但讓此檔單獨可讀 |
| 4 | `PointIndex` | int | 從 0 起 |
| 5 | `TimeMs` | double | 該點的經過時間 |
| 6 | `Objective` | double | 當下 incumbent；尚無解為 `NaN` |
| 7 | `Bound` | double | 當下 best bound |
| 8 | `Gap` | double | 當下 gap；尚無解為 `NaN` |

### 3 · `<Experiment>-meta.csv` —— 整期不變的東西（key-value 長格式）

| 欄位 | 說明 |
| --- | --- |
| `Section` | `schema` / `experiment` / `model` / `environment` / `baseline` |
| `Key` | 該 section 內的鍵 |
| `Value` | 值 |

範例內容（實測 baseline 非 null 為 9 顆，故 `baseline` 段約 9 列）：

```csv
Section,Key,Value
schema,version,1
experiment,name,CandyBlending-tuning-r0
experiment,runId,20260824-080123
experiment,startedAt,2026-08-24 08:01:23
experiment,description,R0 校準：baseline × 5 seeds
experiment,trajectoryEnabled,true
model,Canonical.varCount,12
model,Canonical.constraintCount,11
environment,solver,Cplex
environment,solverVersion,22.1.1
environment,threads,8
environment,parallelMode,1
environment,machine,DESKTOP-XXXX
baseline,label,r0-baseline
baseline,MipGap,1e-06
baseline,TimeLimit,300
baseline,Threads,8
baseline,ParallelMode,1
baseline,Seed,11
baseline,MemoryLimitMb,2048
baseline,FeasibilityTol,1e-06
baseline,OptimalityTol,1e-06
baseline,RowRead,30000
```

### baseline 的跨輪演進：全量快照，刻意重複

promotion 會改寫 `Program.cs` 的 `productionBaseline`，所以 baseline 會沿著輪次累積：

| 輪次 | 事件 | 該輪 baseline 的實際內容 |
| --- | --- | --- |
| R0 | Phase 2 交棒 | prod：專案明設的那幾顆 |
| R1 | `GomoryCuts=2` 勝出 → promotion | base1 = prod **+ GomoryCuts=2** |
| R2 | `NodeSelect=1` 勝出 → promotion | base2 = base1 **+ NodeSelect=1** |
| R3 | 測 `Probe=2` | baseline 仍是 base2；candidate = base2 + Probe=2 |

**規則：`-meta.csv` 的 `baseline` 段一律寫「累積後的全量快照」（只列非 null），`DiffKnobs` 只寫「這一輪多動了什麼」。**

R3 的 `-meta.csv`：

```csv
baseline,label,r3-baseline
baseline,basedOn,r2-NodeSelect=1@20260824-131002
baseline,MipGap,1e-06
baseline,TimeLimit,300
baseline,Threads,8
baseline,ParallelMode,1
baseline,Seed,11
baseline,GomoryCuts,2      ← R1 累積進來，對 R3 而言已是基準的一部分
baseline,NodeSelect,1      ← R2 累積進來
```

R3 主表：`r3-baseline` 的 `DiffKnobs` 為空、`r3-Probe=2` 的 `DiffKnobs` = `Probe=2`。**`GomoryCuts` / `NodeSelect` 不出現在 `DiffKnobs`**——它們不是這輪的變因。

**NEVER 用鏈式增量（只記「base2 + Probe=2」）**，三個理由：

1. §3.3.1 已明訂「config snapshot MUST materialize，不能只記『從當時 baseline Clone』」——理由正是 promotion 會改寫 `productionBaseline`，**鏈式的起點本身會漂移**
2. 每份 experiment 檔 MUST 自足。鏈式時要知道 R3 跑了什麼就得回讀 R2 → R1 → R0；少一個檔、改一次名、或某輪 promotion 被撤銷成 `rejected`，整條鏈就斷——而且**斷掉不會報錯**，只會安靜地重建出錯誤設定
3. 重複的成本是零：移除 initializer 後每輪 `baseline` 段約 5–12 列，四輪加起來不到 1 KB

`baseline,basedOn` 是 **provenance pointer，不是重建依據**——用來回答「這個 baseline 怎麼來的」，讓人能追回當初為何 promote。舊檔全數遺失時，本輪紀錄仍自足可重現。

### 格式細則（三份檔共用）

| 項目 | 規定 | 理由 |
| --- | --- | --- |
| 編碼 | UTF-8 **with BOM** | 沒 BOM 時 Excel 會把中文顯示成亂碼 |
| 換行 | CRLF | Excel 與 repo 政策一致 |
| 分隔符 | `,`，RFC 4180 引號規則 | label 規定不得含逗號，實務上不會觸發引號 |
| 數值 | invariant culture、`.` 小數點、無千分位 | |
| 精度 | round-trip（`R` 或 17 位有效數字） | `TUNING-FACTS` 要求與 archive 逐字相同 |
| 非數值 | 字面 `NaN` / `Infinity` / `-Infinity` | **與「空 = 未設定」語意分開** |
| 布林 | 小寫 `true` / `false` | |
| 時間 | `yyyy-MM-dd HH:mm:ss`（`RunId` 用 `yyyyMMdd-HHmmss`） | Excel 直接解析成日期 |
| 欄位順序 | 凍結 | |

## Edge Cases & Error Handling

- **同名 experiment 重複執行** → 不再是靜默 append 的陷阱：每次 `Run()` 產生新 `RunId`，三份檔以 append 寫入但每列都帶 `RunId`，用它即可完全分離。`-meta.csv` 每個 `RunId` 一組列。
- **軌跡未開啟** → `TrajectoryPoints = 0`、`TFeasMs` / `TStallMs` / `DeltaBound` 為空、`-trajectory.csv` 該 trial 無列。**不得靜默**：`TrajectoryPoints` 就是給人一眼看出來的自檢欄。
- **純 LP（無整數變數）** → `BestBound` / `MipGap` 是 CPLEX 佔位值（`-1E+75` / `1E+75`），`NodeCount` 可能為 0 或空。紀錄照實寫，判讀責任在 Phase 3（api-guide §7 已有此說明）。
- **`TimeLimit` 無可用解** → `ObjectiveValue` / `MipGap` 為 `NaN`、`TFeasMs` 為**空**。天條：NEVER 把 `NaN` 或空當 0 參與彙總。
- **多顆旋鈕的 candidate**（條件耦合，例 `RootAlgorithm=4` + `BarrierAlgorithm=2`）→ `DiffKnobs` 以 `;` 串接；一輪一顆是天條，出現多顆時 `TuningHistory.md` MUST 寫明為何必須綁在一起。
- **多模型 experiment** → `Model` 欄分組；`-meta.csv` 的 `model` 段每個模型一組 `varCount` / `constraintCount`。跨模型比較時，主指標只在**同一個 Model 內**有意義，跨模型只能比趨勢。
- **舊格式紀錄** → **確認移除，不做轉換工具**。既有 `Projects/HospitalRostering_Generator` 的舊 archive 保留原檔不動、不再產生新的舊格式；新舊混存時以 `-meta.csv` 是否存在判斷格式版本。
- **`DiffKnobs` 與實際 config 不一致** → `DiffKnobs` MUST 由 framework 在 capture 當下**從實際 config snapshot 算出**，NEVER 由呼叫端手寫 label 推導（label 會說謊）。
- **promotion 後 baseline 演進**（`prod → base1 → base2 → base2+test`）→ 見 Data Model「baseline 的跨輪演進」：全量快照、刻意重複、不做鏈式增量。
- **promotion 被撤銷（`rejected`）** → 下一輪的 baseline 段照實寫「撤銷後」的實際設定，`basedOn` 指向真正生效的那一輪；NEVER 沿用已撤銷 champion 的標籤。
- **專案未設定任何 solver 旋鈕** → 移除 initializer 後 `baseline` 段可能只有 `label` / `basedOn` 兩列。這是合法狀態（全部沿用 CPLEX 預設），但 Phase 2 §8.4 要求至少明設 `ParallelMode` / `Seed` / `Threads`，所以正常交棒的專案不會落在這裡；若真的落在這裡，代表交棒契約沒被遵守。

## Non-Functional Requirements

- **Performance**：軌跡擷取對 runtime 的影響 MUST 落在該專案 θ 以內（見 Acceptance Criteria 的安全閥）；寫檔在所有 trial 跑完後一次完成，不進入計時區間。
- **可讀性（本規格的第一公民）**：人不看規格、只看表頭就能懂欄位；AI 讀完整輪紀錄的 context 成本 < 5 KB。
- **Observability**：`Run()` 結束時 log 一行摘要：`RunId`、trial 數、軌跡是否開啟、三份檔的路徑。
- **相容性**：CSV 欄位順序凍結；新增欄位一律追加在最後並升 `schema.version`。
- **零侵入**：所有行為在框架內完成。消費專案不新增設定、不新增呼叫、不改四段模板——換 DLL 即生效。
- **Security**：無敏感資料；`environment,machine` 若涉及隱私可由 `ProjectConfig` 關閉。

## Open Questions

- [ ] `Experiment.Load(name)` 在移除 JSON 後改讀哪一份？（建議：主表 + meta 重建 `Trial`，軌跡按需載入）
- [ ] 軌跡點數上限？CPLEX callback 觸發頻率不可控，跑滿 300 秒可能產生大量點。建議先不設限、實測後再決定，避免預先過度設計。

## Implementation Plan

### Stub 階段（先做）

- [ ] `OptimFoundation.Core`：`Trial` / `SolveMetrics` / `Experiment` 加上新 property（僅型別與簽名，不含計算）
- [ ] `OptimFoundation.Core`：`MetaCsvWriter` 建類別與 `Write` 簽名，body `throw new NotImplementedException()`
- [ ] 兩個組件 build 綠
- [ ] AI-Modeling 端：`Template/` **不動任何 `.cs`**，僅換 DLL 重編確認仍 build 綠

### 逐層實作

- [ ] `CplexConfig` 移除 6 個 property initializer，確認 `Configuration()` 對 `null` 一律跳過
- [ ] 迴歸驗證：`Template` / `CandyBlending` 移除 initializer 後 solve 結果不變（兩者都已明設 `Threads` / `ParallelMode` / `Seed`）
- [ ] `OptEngine` 求解後填 `NodeCount` / `IterationCount`
- [ ] `OptExperiment.Run()` 內部在建 engine 後、`Solve()` 前固定呼叫 `engine.EnableTrajectory()`
- [ ] 由 `Trajectory` 計算 `TFeasMs` / `TStallMs` / `DeltaBound` / `TrajectoryPoints`
- [ ] `RunId` / `TrialId` / `Model` 生成與傳遞；`Label` 去掉 model 前綴
- [ ] `DiffKnobs` 由 config snapshot 對 baseline 差集算出
- [ ] 三個 writer 落地（主表 / 軌跡 / meta），移除 JSON 預設輸出
- [ ] 依 `dlls/README.md` 回填 `dlls/` 並更新 `VERSION.txt`
- [ ] `Template/` 實跑 exp，逐條對照 Acceptance Criteria
- [ ] 軌跡成本安全閥實測（開／關各一輪 R0，比 runtime sgm）
- [ ] 更新 `solver-tuning-guide.md`（§2.1 / §3.3 / §6.2.2 / Fatal）
- [ ] 更新 `tuning/checklist.md` 的 archive 驗收 A–E
- [ ] 更新 `.claude/skills/AGENTS.md` Phase 3 出口契約
- [ ] `Projects/CandyBlending` 重跑 R0 驗證

## References

- 實作 repo：`../OptimFoundation/`（本規格驅動該 repo 的變更；DLL 回填流程見 `dlls/README.md`）
- 現況實測證據：`Projects/CandyBlending/bin/Debug/net8.0/Experiments/CandyBlending-tuning-r0.{csv,json}`
- 受影響的既有規格：
  - `.claude/skills/tuning/solver-tuning-guide.md`（§2.1 剖面、§3.3 archive、§6.2.2 TUNING-FACTS、§9 Fatal）
  - `.claude/skills/tuning/checklist.md`（每輪 archive 逐項驗收 A–E）
  - `.claude/skills/AGENTS.md`（Phase 3 出口契約）
  - `.claude/skills/coding/optimfoundation-api-guide.md`（§8.1 實驗 runner、§9.2.8 SolveMetrics 簽名）
