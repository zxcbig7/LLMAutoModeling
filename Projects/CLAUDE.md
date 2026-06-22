# Projects 資料夾

此資料夾包含所有 OptimFoundation CPLEX 題目專案。

## 每個專案的標準架構（Fluent OptModel + 雙模式）

```text
ProjectName/
├── CLAUDE.md            ← 專案概述、問題類型
├── ProjectName.csproj
├── Program.cs           ← 唯一進入點（solve / experiment 雙模式）
├── ExperimentRunner.cs  ← 參數掃描（tuning）
├── Model/               ← 數學模型文件（Markdown）
├── Parameter/           ← ParameterBase 子類別，QTY 欄位
├── Set/                 ← Dataload（Sets 由 Parameters 衍生）
├── Variable/            ← VariableB_/X_/I_ + VariableCreate
├── Objective/           ← ObjectiveFunction
└── Constraint/          ← ConstraintBase 子類別 + BuildModel
```

## 通用規則（適用所有專案）

- **唯一 composition root**：`Program.cs` 用框架內建的 Fluent `OptModel`（`UseConfig → AddVariables → AddModel → OnSolved → Execute`），**不再手寫 `XxxProblem.Execute()`**
- **雙模式**：`dotnet run` 求解；`dotnet run -- experiment` 跑 tuning 掃描（`ExperimentRunner`）
- **build/solve 共用** `VariableCreate` / `BuildModel`（兩模式同一份建模邏輯）
- **禁止 Hardcode**：所有數值透過 `Parameter.QTY` 取得
- **兩階段開發**：先完成數學模型（`Model/`），再實作程式碼
- **資料夾規則以 `claudemdTemplate/` 為單一來源**：各子資料夾規則見 `claudemdTemplate/{Set,Variable,Parameter,Objective,Constraint,Experiment,Root,Model}/CLAUDE.md`，專案內不再各自維護重複副本
- canonical code 範本：`Template_CPLEX/`；tuning 策略與旋鈕對照：`truning/CLAUDE.md`
- 端到端開發流程：`tutorial/`
```

