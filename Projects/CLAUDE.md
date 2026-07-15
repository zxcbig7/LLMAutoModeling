# Projects 資料夾

此資料夾包含所有 OptimFoundation CPLEX 題目專案。**interactive 與 automated 兩條路線統一產出下列扁平結構**（不再有 `stages/` + `csharp/` 兩層中繼）。

## 每個專案的標準架構（統一扁平，D3）

```text
ProjectName/
├── CLAUDE.md ← 專案概述、問題類型
├── ProjectName.csproj ← 複製 Template_CPLEX；DLL/Analyzer 相對 repo 根 dlls/
├── Program.cs ← 唯一進入點（solve / experiment 雙模式）
├── ExperimentRunner.cs ← 參數掃描（tuning）
├── status.json ← 進度追蹤（automated 斷點續跑用）
├── Model/ ← 數學模型文件（Markdown，Model/<Name>_Model.md）
├── Set/ ← Set 積木（[OptSet<T>]）+ Dataload 組合器
├── Parameter/ ← Parameter 積木（[OptParam<Set_X>]，含 QTY）
├── Variable/ ← Variable 積木（[OptVar<Set_X>]，前綴 B/X/I）+ VariableCreate
├── Objective/ ← ObjectiveFunction
└── Constraint/ ← ConstraintBase 子類別 + BuildModel
```

## resume（automated 斷點續跑，D6）

`status.json`：`{ "completed": ["00","01",...], "current": "05", "projectType": "MILP" }`
每個 stage 產物直接寫入上面的最終扁平資料夾（直寫終點）；續跑時讀 `status.json` + 已完成檔當 context，從 `current` 續跑不重跑。

## 通用規則（適用所有專案）

- **天條唯一權威在 [`../AGENTS.md`](../AGENTS.md)**（數值保真、API 白名單、框架唯讀、相對路徑、DLL 引用）
- **唯一 composition root**：`Program.cs` 用框架內建 Fluent `OptModel`（`UseConfig → AddVariables → AddModel → OnSolved → Execute`）
- **雙模式**：`dotnet run` 求解；`dotnet run -- experiment` 跑 tuning 掃描（`ExperimentRunner`）
- **build/solve 共用** `VariableCreate` / `BuildModel`（兩模式同一份建模邏輯）
- **禁止 Hardcode**：所有數值透過 `Parameter.QTY` 取得
- **兩階段開發**：先完成數學模型（`Model/`），再實作程式碼
- **資料夾規則以 `claudemdTemplate/` 為單一來源**：各子資料夾規則見 `claudemdTemplate/{Set,Variable,Parameter,Objective,Constraint,Experiment,Root,Model}/CLAUDE.md`
- canonical code 範本：`Template_CPLEX/`；tuning 策略與旋鈕對照：`../tuning/CLAUDE.md`；端到端流程：`../tutorial/`
- 建模基本物件（Set 積木 / SetBase / 三檔位讀取）→ OptimFoundation `specs/2026-07-13-optset-basic-objects.md`
