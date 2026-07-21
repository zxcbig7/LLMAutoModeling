---
title: AI-Modeling — CPLEX 建模專案架構地圖
scope: 一個標準 CPLEX 題目專案 + 其依賴的 OptimFoundation pipeline
updated: 2026-07-21
related_spec: ROADMAP.md（原 specs/2026-06-21-claudeai-spec-refresh.md 已併入 ROADMAP §2、§3）
---

# CodeMap：CPLEX 建模專案（新版 Fluent OptModel + Tuning）

> **雙架構更新（2026-06-22）**：框架現支援兩種建構方式——預設 generator（`[OptVar]`/`[OptParam]`）+ Fluent `OptModel`，後路手寫 class + `XxxProblem.Execute()`。本圖描述的是**預設**的 OptModel pipeline；兩架構並排對照（含 Manual 版）見 [`ROADMAP.md`](ROADMAP.md) §2（架構全景）與 §3（決策轉折）——原 dual-architecture CodeMap/spec 已於 2026-07-15 刪除，內容併入 ROADMAP。
>
> **資料防護層更新（2026-07-18/19）**：所有專案的 `Dataload` 建構一律走 `OptData.Load(() => new Dataload())`，NEVER 裸 `new Dataload()`——後者仍可編譯但完全跳過驗證。細節見下方 Dependency Graph 最上游那段鏈路，與 [`CPLEX_API_REFERENCE.md`](CPLEX_API_REFERENCE.md) §7.6。

[TOC]

- [Dependency Graph](#dependency-graph)
- [File Index](#file-index)
- [Symbol Index](#symbol-index)

## Dependency Graph

```mermaid
graph TD
    Program["Program.cs（雙模式入口）"]
    Program -->|"無參數"| Solve["Solve 模式"]
    Program -->|"-- experiment"| Exp["Experiment 模式"]

    Solve -->|"OptData.Load(() => new Dataload())"| Load["OptData.Load&lt;T&gt;（唯一建構入口）"]
    Load --> DL["Dataload : DataContext（Set/）"]
    DL -->|"ctor 內 LoadParam/Set.Load(source)"| Read["讀 Set/Parameter 資料"]
    Load -->|"RegisterAll()+DataValidator.Validate()"| Gate{"驗證通過？"}
    Gate -->|"否"| Fail["DataValidationException（聚合列出全部問題，fail-fast，NEVER 吞掉）"]
    Gate -->|"是"| OptModel["OptModel（框架 composition root）"]

    OptModel -->|".AddVariables"| VC["VariableCreate.Build()"]
    OptModel -->|".AddModel"| BM["BuildModel.Build()"]
    OptModel -->|".OnSolved"| WCSV["Dataload.WriteToCSV()"]
    OptModel -->|"內部持有"| Engine["OptEngine（CPLEX）"]

    Exp --> ER["ExperimentRunner.Run()"]
    ER -->|"每 variant 重新呼叫"| Load
    ER -->|"每 variant fresh"| Engine
    ER -->|"手動呼叫（共用）"| VC
    ER -->|"手動呼叫（共用）"| BM
    ER -->|"Trial.Capture"| Trial["Trial（ConfigSnapshot+SolveMetrics）"]
    Trial --> Exp2["Experiment.Save() → Experiments/*.csv+json"]

    VC -->|"BuildBVs/CVs/IVs"| Engine
    BM --> OBJ["ObjectiveFunction.Build()"]
    BM --> CONS["Constraint_*.Build()"]
    OBJ -->|"AddLHS/CreateMax|Min"| Engine
    CONS -->|"AddLHS/AddRHS/CreateLe|Ge|Eq|Range"| Engine
    VC -.->|"Sets/Parameters"| DL
    BM -.-> DL
```

要點：**資料防護主幹道**（2026-07-18/19 落地，8/8 專案採用）——`Dataload` 一律 `: DataContext`，唯一建構入口是 `OptData.Load(() => new Dataload())`；裸 `new Dataload()` 仍可編譯但完全跳過 `RegisterAll()`+`DataValidator.Validate()`，NEVER 這樣寫。驗證通過的 `dataload` 才往下遊。`VariableCreate` 與 `BuildModel` 是**兩模式共用**的 build-step；solve 模式由 `OptModel` 驅動、experiment 模式由 `ExperimentRunner` 驅動（且每個 variant 各自重新 `OptData.Load` 一次，取得獨立 fresh dataload），建模邏輯不重複。

## File Index

| 路徑 | 角色 | 新版動作 |
|------|------|----------|
| `Program.cs` | 唯一入口，solve/experiment 分派 | 重寫為雙模式 |
| `ExperimentRunner.cs` | tuning 掃描器 | 新增（每專案標配） |
| `Set/<Proj>Dataload.cs` | Sets/Parameters/WriteToCSV | namespace→`Proj.Set` |
| `Variable/VariableCreate.cs` | 建立所有決策變數 | 共用，不變邏輯 |
| `Variable/VariableB_*/X_*/I_*.cs` | 變數型別宣告 | namespace 對齊 |
| `Constraint/BuildModel.cs` | 目標式+限制式組裝入口 | 共用 |
| `Constraint/Constraint_*.cs` | 各條限制式 | 可用 `CreateRange` |
| `Objective/ObjectiveFunction.cs` | 目標式 | 不變 |
| `Parameter/Parameter_*.cs` | `ParameterBase`（QTY） | namespace 對齊 |
| `Model/<Proj>_Model.md` | 數學模型 | 不變 |
| `<Proj>Problem.cs`（手寫 Execute） | Manual 後路架構的問題入口 | **本圖（Generator 預設路線）不用**；官方保留為並存後路，範例 `Projects/HospitalRostering_Manual/HospitalRosteringProblem.cs`（`public sealed class HospitalRosteringProblem : IDisposable`），NEVER 誤刪——見 ROADMAP.md §2 雙 pattern 並存決策 |

## Symbol Index

| Symbol | 模組 | Export / 簽名 | 角色 |
|--------|------|---------------|------|
| `OptModel` | OptimFoundation.Cplex | `.UseConfig(Func<CplexConfig>).AddVariables(Action<OptEngine>).AddModel(...).OnSolved(...).Execute()` | solve composition root |
| `OptEngine` | OptimFoundation.Cplex | `BuildBVs/CVs/IVs<T>`、`AddLHS/AddRHS`、`CreateLessEqual/GreatEqual/Equal/Range`、`CreateLeSoft/GeSoft/EqSoft`、`CreateMaximize/Minimize`、`Solve`、`GetSetVarValues<T>`、`EnableTrajectory` | 求解引擎窗口 |
| `Experiment` | OptimFoundation.Core | `new(name,desc)`、`.AddTrial(Trial)`、`.Save()` | tuning 結果累積/輸出 |
| `Trial` | OptimFoundation.Core | `static Capture(ISolverEngine, label, Func<bool>, note=null)` | 單次求解快照 |
| `ITunableConfig` | OptimFoundation.Core | `Seed/Emphasis/FeasibilityTol/OptimalityTol/RootAlgorithm/Presolve/HeuristicEffort/MemoryLimitMb` | 跨引擎 tuning 抽象旋鈕 |
| `CplexConfig` | OptimFoundation.Cplex | `ITunableConfig` + CPLEX 專屬欄位（`workThreads/varSel/nodeSelect/epGap/...`） | CPLEX 設定（tuning 單一來源） |
| `VariableCreate` | `<Proj>.Variable` | `(Dataload,OptEngine)` → `.Build()` | 建變數（兩模式共用） |
| `BuildModel` | `<Proj>.Constraint` | `(Dataload,OptEngine)` → `.Build()` | 建目標式+限制式（兩模式共用） |
| `DataContext` | OptimFoundation.Core | `abstract class DataContext`（2026-07-18 新增，paved path） | 資料防護層基底；子類（如 `Dataload`）ctor 內讀資料，欄位漏掛 attribute → 編譯期 `OPTF006` |
| `OptData` | OptimFoundation.Core | `static T Load<T>(Func<T> factory) where T : DataContext`（唯一多載，無 `IDataSource` 多載） | 唯一建構入口：觸發 `RegisterAll()`+`DataValidator.Validate()`；裸 `new Dataload()` 仍可編譯但跳過驗證 |
| `Dataload` | `<Proj>.Set` | `public partial class Dataload : DataContext`；欄位含 Sets/Parameters + `WriteToCSV(OptEngine)` | 資料載入；建構唯一入口 `OptData.Load(() => new Dataload())`，失敗丟 `DataValidationException` |
