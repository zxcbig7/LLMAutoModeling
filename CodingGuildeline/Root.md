# [ProjectName] 專案概述

> 本規範對應 `AI-Modeling/Template/` canonical scaffold。`OptimFoundation/OptimFoundation/Templates/` 是 framework integration／相容性案例，不是新專案結構來源。

## 問題類型

（LP / MIP / ILP — 填寫）

## 開發兩階段原則

**第一階段：數學模型** — 在 `Model/ProjectName_Model.md` 完整定義 Sets、Parameters、Variables、Objective、Constraints。

**第二階段：程式實作** — 程式碼是數學模型的純轉譯。所有係數從 Parameter 的 `QTY` 取得，NEVER 自行移項、改號、翻轉方向或加入裸數字。

## 專案結構

```text
ProjectName/
├── Program.cs           ← 唯一入口與唯一組裝點（solve / experiment）
├── Model/               ← 數學模型文件
├── Parameter/           ← Parameter 積木，數值放 QTY
├── Set/                 ← Set 積木 + Dataload : DataContext
├── Variable/            ← VariableB_/X_/I_ 宣告
├── Objective/           ← ObjectiveFunction
└── Constraint/          ← 一條或一組邏輯相關限制式一個 class
```

不建立只負責轉呼叫的 composition facade、變數註冊 class、模型組裝 class 或獨立實驗 runner。所有層級關係直接列在 `Program.cs`。

## 預設建模方式

| 軸 | paved path | 後路 |
| --- | --- | --- |
| 變數／參數 class | `[OptVar]` / `[OptParam]` + `[OptDim<TSet>]` source generator | 手寫 `VariableBase` / `ParameterBase` |
| 執行 | `OptModel` 定義模型，`OptProject` / `OptExperiment` 執行 | 需要逐行掌控 native engine 時手寫 `XxxProblem.Execute()` |

新題目用 paved path。後路只在明確需要時採用，不能混入 paved-path 骨架。

## Program.cs 骨架

```csharp
var data = OptData.Load(() => new Dataload());

var projectConfig = new ProjectConfig
{
    ProjectName = "ProjectName",
    EnableSolverLog = true,
    ExportSol = true,
    ExportLP = true,
    DataId = "ProjectName",
    UserId = "SYSTEM",
};
var solverConfig = new CplexConfig
{
    epGap = 0.0,
    timeLimit = 60,
    workThreads = 4,
};

var model = new OptModel("Canonical") // 模型定義；執行屬 runner
    .AddVariables(e => e.BuildVars<VariableB_Assign>(data.EMPLOYEE, data.DATE))
    .AddObjective(e => new ObjectiveFunction(e, data.EMPLOYEE, data.parameter_Cost).Build())
    .AddConstraints(e => new Constraint_Coverage(e, data.EMPLOYEE, data.DATE, data.parameter_Demand).Build());

if (args.Contains("experiment"))
{
    var baseline = solverConfig.Clone();
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;

    new OptExperiment("projectname-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", emphasis)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(e => data.WriteToCSV(e));

bool ok = project.Execute();
```

## 分工與順序

- `ProjectConfig`：專案名、保留期、solver log、LP/MPS/Sol 輸出。
- `CplexConfig`：gap、time limit、threads 等 solver 旋鈕。
- `OptModel`：只記錄 variables、objective、constraints；同一份可交給任一 runner。
- `OptProject`：一次正式求解；`OnSolved` 只存在於這個邊界。
- `OptExperiment`：模型 × solver config；預設 solver log OFF、輸出 OFF、不做 housekeeping，並自動儲存 CSV/JSON。
- `Experiment.Save()`：同名採 append，`Run()` 回傳的 `Trials` 包含歷史 + 本輪；只處理本輪時使用唯一 experiment name。
- 每種變數、目標式、每條限制式各占一個 fluent call。框架固定依 variables → objective → constraints 套用。
- Objective / Constraint 建構子只收實際使用的 Set、Parameter、scalar 和 engine，NEVER 收整包 `Dataload`。

`OptData.Load` 是唯一資料建構路徑。載入後把資料視為唯讀；`Freeze()` 只攔截框架受控的 mutation API，直接寫 public field 或 mutable `List` 不保證立即攔截，因此專案 code 一律不得這樣做。

## Tuning 模式

`dotnet run -- experiment` 直接在 `Program.cs` 使用 `OptExperiment`。用 `Clone()` 產生具體 variant；所有 cell 共用同一份已載入資料。規範見 `Experiment/CLAUDE.md`。
