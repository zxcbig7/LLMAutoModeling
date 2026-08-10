# 舊版寫法 → 新版寫法 對照表（盤點基準）

權威來源：`.claude/rules/Ph2_Coding/optimfoundation-api-guide.md`（2026-08-09 同步版）與 `.claude/rules/AGENTS.md`。
本表是盤點用的指紋清單；判定有疑義時以上述兩份為準。

`.claude/rules/Ph2_Coding/optimfoundation-api-guide-0810.md` 與 `optimfoundation-api-guide-0810 bkp.md` 是**舊版全文備份**，整份都是舊寫法，盤點時只需標記「整檔舊版」+ 段落分佈，不必逐行列舉。

## L1 · 資料層（改動最大）

| # | 舊寫法 | 新寫法 |
| --- | --- | --- |
| L1-1 | `Set_*` 是積木／集合物件，繼承 `SetBase<T>`、`ISetBrick` | `Set_*` 是 **row class**，繼承 `SetRowBase`，載入後是 `List<Set_*>` |
| L1-2 | `public Set_Item set_Item = new();` | `public List<Set_Item> set_Item = new();` |
| L1-3 | `set_X.Load(source, "Set_X");` | `set_X = source.Load<Set_X>("<sourceName>");` |
| L1-4 | `source.LoadParam<Parameter_X>("Parameter_X")` | `source.Load<Parameter_X>("<sourceName>")` |
| L1-5 | `set.LoadFrom(...)` / `LoadSetRows` / `IDataSource.LoadSet` | 已移除，一律 `Load<T>` |
| L1-6 | `CsvCtrl.WriteSet(...)` / `CsvCtrl.WriteParam(...)` | `CsvCtrl.WriteRows(rows, fileName)` |
| L1-7 | CSV 檔名 MUST 等於類別名／`Set_<名>.csv` 固定 | `sourceName` **不必**等於 class 名；省略時才用 `typeof(T).Name` |
| L1-8 | `DbDataSource.LoadParam<T>(sql, params)` | `DbDataSource.Load<T>(sql, params)` |
| L1-9 | `IDataSource` 有 `LoadSet` / `LoadData` / `LoadParam` 三支 | 只有 `LoadData(string)` + `Load<T>(string = null)` |
| L1-10 | `[FullGrid]` 全格覆蓋檢查 / `MissingCell` issue kind | 新版 `DataContext` 驗證**不含**此項（見 L1-11） |
| L1-11 | 「四類驗證」（index 參照 Dangling / TypeMismatch / MissingSet、DuplicateKey、數值 sanity、MissingCell） | 實際只有三項：Set 維度 key 重複、Parameter 維度 key 重複、Parameter 數值 sanity（NaN/Inf/>1e15）。Parameter→Set 關聯**不自動驗** |
| L1-12 | `FirstOrDefault(p => ...)?.QTY ?? 0.0` 直接查 Parameter | `rows.FindParameterOrLog(predicate, keyValues)?.QTY ?? 0.0` |
| L1-13 | 一維 Set 成員直接當 string 用（`foreach (var item in set_Item)` 得到 `string`） | row class 有隱式轉換；多維 row 用 property 或 deconstruct（`foreach (var (from,to) in set_Arc)`） |
| L1-14 | `Dataload` 欄位型別 `Set_X` | `List<Set_X>` |

## L2 · 變數層

| # | 舊寫法 | 新寫法 |
| --- | --- | --- |
| L2-1 | **`VariableX_`**（Continuous 前綴） | **`VariableC_`** |
| L2-2 | `BuildBVs` / `BuildCVs` / `BuildIVs` 一律禁用（黑名單） | 仍是**有效公開 API**，只在自訂 bounds 或維護既有明確型別建構時使用；一般走 `BuildVars<T>` |
| L2-3 | `BuildCVs<T>(lb, ub, sets)` 標 ❌ 禁用 | 合法（自訂 bounds 時） |
| L2-4 | 集中式 `VariableCreate.cs` / `Sets.cs` 集中檔 | 一型別一檔 |
| L2-5 | `engine.varCount` / `engine.TotalVarCount` | `engine.VariableCount` / `engine.RegisteredVariableCount` |
| L2-6 | 診斷碼只列 `OPTF001/002/004/006` | 另有 `OPTF007`（OptDim 型別不合法）、`OPTF008`（Set 零維） |

## L3 · 限制式層

| # | 舊寫法 | 新寫法 |
| --- | --- | --- |
| L3-1 | `engine.CreateLessEqual($"{ConstraintName}@{item}")` 手工組名 | `engine.CreateLessEqual(this, item)` owner overload |
| L3-2 | 手工串 `ConstraintName + "@" + ...`、手工格式化日期 `{date:yyyy_MM_dd}` | 傳原始維度值給 owner overload，框架組名 |
| L3-3 | `CreateXxx(double rhs, string name)` 三個 overload 標「存在但 NEVER 使用」 | 仍受支援的明確介面；只是會**覆蓋**已累積 RHS 常數，一般路徑仍是 `AddRHS(v)` + owner overload |
| L3-4 | `CreateRange(lb, ub, name)` | `CreateRange(lb, ub, this, dims)` |
| L3-5 | soft：`CreateLeSoft(rhs, penalty, name)` | 另有 owner overload `CreateLeSoft(rhs, penalty, this, dims)` |
| L3-6 | 建構子收 `Set_X items`（積木型別） | 收 `List<Set_X> items` |

## L4 · Runner / Config / Solution

| # | 舊寫法 | 新寫法 |
| --- | --- | --- |
| L4-1 | `project.optEngine` | `project.Engine` |
| L4-2 | `project.totalTimeSpan` / `buildModelTimer` / `totalTimer` | `project.TotalElapsed` / `project.BuildModelElapsed` |
| L4-3 | `CplexConfig` camelCase：`epGap` `timeLimit` `workThreads` `mipEmphasis` `randomSeed` `parallelMode` `workMemory` `nodeSelect` `varSel` `algorithm` `nodeFileInd` `epOpt` `epRHS` `polishAfterTime` `rowRead` `treeMemoryLimit` `detTimeLimit` `clockType` `nodeLimit` `intSolLimit` `epAGap` `epInt` `numericalEmphasis` `cutsFactor` `cutPasses` `gomoryCuts` `coverCuts` `cliqueCuts` `mirCuts` `flowCoverCuts` `probe` `rinsHeur` `branchDir` `diveType` `mipSearch` `symmetry` `simplexIterLimit` `barrierAlgorithm` | **全部 PascalCase**：`MipGap` `TimeLimit` `Threads` `Emphasis` `Seed` `ParallelMode` `MemoryLimitMb` `NodeSelect` `VariableSelect` `RootAlgorithm` `NodeFileStrategy` `OptimalityTol` `FeasibilityTol` `PolishAfterTime` `RowRead` `TreeMemoryLimitMb` `DeterministicTimeLimit` `ClockType` `NodeLimit` `IntegerSolutionLimit` `AbsoluteMipGap` `IntegralityTolerance` `NumericalEmphasis` `CutsFactor` `CutPasses` `GomoryCuts` `CoverCuts` `CliqueCuts` `MirCuts` `FlowCoverCuts` `Probe` `RinsHeuristicFrequency` `BranchDirection` `DiveType` `MipSearch` `Symmetry` `SimplexIterationLimit` `BarrierAlgorithm`（權威：Ph3 guide 附錄 A） |
| L4-4 | 「抽象旋鈕 `ITunableConfig` + camelCase 欄位」雙軌，同一設定 NEVER 兩邊都寫 | 雙軌制**已取消**，單一套 PascalCase |
| L4-5 | `CsvCtrl.WriteSolution` 前 MUST `FolderDir.Solution.CreateFolder()` | `CsvSolutionSink` / `CsvCtrl.WriteSolution` **自行建立** `Solution/` |
| L4-6 | 只有 `CsvCtrl.WriteSolution` 一條輸出路 | 另有 `ISolutionSink` / `BeginBatch` / `Commit` |
| L4-7 | `engine.BestObjValue` / `engine.MIPGap` | `engine.LastMetrics.BestBound` / `.MipGap`（Phase 3 判定用 `LastMetrics`） |

## L5 · Status 與驗收

| # | 舊寫法 | 新寫法 |
| --- | --- | --- |
| L5-1 | 「Status **三分**診斷」（Optimal / Infeasible / Unbounded） | **七態**：`Optimal` `Feasible` `TimeLimit` `Infeasible` `Unbounded` `Error` `NotSolved` |
| L5-2 | 「五態」（AGENTS.md 舊描述） | 七態；`Feasible` 是合法交付狀態，`TimeLimit` = 中止且無解 |
| L5-3 | 「LP bound sanity」只講 relaxation | 核對 `engine.LastMetrics.BestBound`：max 時 `Obj <= BestBound`、min 時 `Obj >= BestBound` |

## L6 · 結構／路徑（**禁止改動，只標記不修**）

八資料夾結構、csproj HintPath、Analyzer、namespace 寫法、`Model/` 只放 `.md` 等**架構規則不在本次盤點的可改範圍**。若發現與新 guide 不一致，只記錄在「架構差異（不修）」段落。

## 判定注意

- 「新版**放寬**」的項目（L2-2、L2-3、L3-3）：舊文件寫「NEVER 使用／黑名單」是**過時的禁令**，屬舊寫法，要標出來，但改法是改敘述（放寬），不是刪 API。
- 只出現在**歷史備份檔**（`*-0810*.md`）的舊寫法，標記為 `BACKUP` 類，與活躍規範檔分開統計。
- 註解、`<summary>`、文件散文中的舊 API 名稱同樣算舊寫法，要列。
