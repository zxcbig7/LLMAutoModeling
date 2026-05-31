# [ProjectName] 專案概述

## 問題類型

（LP / MIP / ILP — 填寫）

## 開發兩階段原則

**第一階段：數學模型**
在 `Model/ProjectName_Model.md` 完整定義 Sets、Parameters、Variables、Objective、Constraints。

**第二階段：程式實作**
程式碼是數學模型的純轉譯，無創意發揮。所有係數透過 `Dataload` 從 `Parameter.QTY` 取得，任何地方不得出現裸數字。

## 專案結構

```
ProjectName/
├── Model/           ← 數學模型文件（Markdown）
├── Parameter/       ← ParameterBase 子類別，數量欄位用 QTY
├── Data/            ← Dataload（Sets 由 Parameters 衍生）
├── Variable/        ← VariableB_ / VariableX_ / VariableI_
├── Objective/       ← ObjectiveFunction
└── Constraint/      ← ConstraintBase 子類別 + BuildModel
```

## 執行流程

```csharp
// Program.cs
using (var problem = new ProjectNameProblem())
{
    bool ok = problem.Execute();
}

// Execute()
optEngine.Build();
new VariableCreate(dataload, optEngine).Build();
new BuildModel(dataload, optEngine).Build();
bool ok = optEngine.Solve();
if (ok) dataload.WriteToCSV(optEngine);
```
