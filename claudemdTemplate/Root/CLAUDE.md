# [ProjectName] 專案概述

## 問題類型

（LP / MIP / ILP — 填寫）

## 開發兩階段原則

**第一階段：數學模型** — 在 `Model/ProjectName_Model.md` 完整定義 Sets、Parameters、Variables、Objective、Constraints。

**第二階段：程式實作** — 程式碼是數學模型的純轉譯，無創意發揮。所有係數透過 `Dataload` 從 `Parameter.QTY` 取得，任何地方不得出現裸數字。

## 專案結構

```text
ProjectName/
├── Program.cs           ← 唯一入口（solve / experiment 雙模式）
├── ExperimentRunner.cs  ← 參數掃描（tuning）
├── Model/               ← 數學模型文件（Markdown）
├── Parameter/           ← ParameterBase 子類別，QTY 欄位
├── Set/                 ← Dataload（Sets 由 Parameters 衍生）
├── Variable/            ← VariableB_/X_/I_ + VariableCreate
├── Objective/           ← ObjectiveFunction
└── Constraint/          ← ConstraintBase 子類別 + BuildModel
```

## 建模方式（兩種，AI 預設用 generator + OptModel）

| 軸 | 預設（首選） | 後路（需要時） |
|----|------|------|
| 變數/參數 class | `[OptVar]`/`[OptParam]` source generator（樣板最省、最不易錯） | 手寫 `: VariableBase` / `: ParameterBase` |
| composition root | Fluent `OptModel`（注入 Action） | 手寫 `XxxProblem.Execute()`（`: IDisposable`） |

預設組合的可運作範例見 `Projects/HospitalRostering_Generator`；完整手寫組合見 `Projects/HospitalRostering_Manual`。**新題目用預設即可**，下面是預設流程。

## 執行流程（預設：Fluent OptModel composition root）

solve 模式由框架內建的 `OptModel` 註冊式 pipeline 驅動（注入各建構步驟的 Action）；手寫 `XxxProblem.Execute()` 僅在需逐行掌控引擎生命週期時作為後路：

```csharp
// Program.cs
using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ProjectName;
using ProjectName.Set;
using ProjectName.Variable;
using ProjectName.Constraint;

if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }

var dataload = new ProjectNameDataload();

using (var m = new OptModel("ProjectName")
    .UseConfig(() => new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4, enableLog = true, exportSol = true })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

- `OptModel` 保證 `AddVariables` 先於 `AddModel`；內建 build/solve 計時與「整體運作時間」log。
- 專案名由 `new OptModel("ProjectName")` 帶入（log 檔命名），**不需要 `XxxProblem` 類別**。
- `VariableCreate` / `BuildModel` 同時被 solve 與 experiment 兩模式共用。

## Tuning 模式

`dotnet run -- experiment` → `ExperimentRunner.Run()` 掃描多組 `CplexConfig`，
記錄完整設定 + 收斂數據到 `Experiments/<name>.csv + .json`。
規範見 `Experiment/CLAUDE.md`，旋鈕對照見 `truning/CLAUDE.md`。
