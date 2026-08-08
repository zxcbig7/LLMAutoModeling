# Phase 2 · Foundation Coding — 轉譯實作規範

> 天條見 [`README.md`](README.md)；本階段唯一標準（含 API 簽名）是 [`../../rules/Ph2_Coding/optimfoundation-api-guide.md`](../../rules/Ph2_Coding/optimfoundation-api-guide.md)，簽名表在其 §9。

## 系統脈絡

把已確認的 `Model/<Project>_Model.md` 逐條機械轉譯成 OptimFoundation CPLEX C# 專案。發現模型歧義就回 Phase 1，NEVER 自行補假設。

新專案只從 `../../../Template/` 長出來。`OptimFoundation/OptimFoundation/Templates/` 是 framework integration／compatibility examples，不是 scaffold；其中的 `ProjectReference`、舊資料夾名、手寫入口或參數順序都不能覆蓋本文件。generator 是**唯一** paved path；手寫 `: VariableBase` / `: ParameterBase` 已廢止，`../../../Projects/HospitalRostering_Manual/` 僅供理解 API 行為，NEVER 照抄其宣告方式。

## 硬規則

- 專案建在 `../../../Projects/<Project>/`；DLL HintPath 一律 `..\..\dlls\`。
- `Dataload` MUST 是 `public partial class Dataload : DataContext`，且只透過 `OptData.Load(() => new Dataload())` 建構。
- `OptData.Load` 後把資料視為唯讀。`Freeze()` 只保護框架受控 mutation API；直接修改 public field 或 mutable `List` 不保證立即攔截，所以專案 code MUST 不做這些寫入。
- Set 使用 `[OptSet<T>]` 積木；Parameter / Variable 預設用 `[OptParam]` / `[OptVar]` + `[OptDim<TSet>]` source generator。
- Objective / Constraint 建構子只收實際使用的 Set、Parameter、scalar 與 `OptEngine`，NEVER 接整包 `Dataload`。
- `Program.cs` 是唯一組裝點。每種變數、目標式、每條限制式直接各占一個 fluent call；不建立純轉呼叫 class 或 local function。
- 參數查詢先存局部變數再傳入 `AddLHS` / `AddRHS`，NEVER 內嵌 LINQ。
- Objective MUST 先於所有 Constraint；框架的 phase 順序固定為 variables → objective → constraints。
- build fix loop 最多 5 次；仍失敗就停下回報。

## 專案結構

```text
Projects/<Project>/
├── <Project>.csproj
├── Program.cs                  # 唯一入口與組裝點
├── Model/<Project>_Model.md
├── Set/                        # Set_* + Dataload
├── Parameter/                  # Parameter_*
├── Variable/                   # VariableB_/X_/I_*
├── Objective/ObjectiveFunction.cs
└── Constraint/Constraint_*.cs
```

## Program.cs paved path

```csharp
var data = OptData.Load(() => new Dataload());
var penalty = data.parameter_Penalty.Single().QTY;

var projectConfig = new ProjectConfig
{
    ProjectName = "<Project>",
    EnableSolverLog = true,
    ExportSol = true,
    ExportLP = true,
    DataId = "<Project>",
    UserId = "SYSTEM",
};
var solverConfig = new CplexConfig
{
    epGap = 1e-4,
    timeLimit = 300,
    workThreads = 8,
};

var model = new OptModel("Canonical") // 模型定義；執行屬 runner
    .AddVariables(e => e.BuildVars<VariableB_Assign>(data.EMPLOYEE, data.DATE))
    .AddVariables(e => e.BuildVars<VariableX_Shortage>(data.DATE))
    .AddObjective(e => new ObjectiveFunction(e, data.DATE, penalty).Build())
    .AddConstraints(e => new Constraint_MaxWorkDays(e, data.EMPLOYEE, data.DATE, data.parameter_MaxWorkDays).Build())
    .AddConstraints(e => new Constraint_Coverage(e, data.DATE, data.parameter_Demand).Build());

if (args.Contains("experiment"))
{
    var baseline = solverConfig.Clone();
    var variant = baseline.Clone();
    variant.Emphasis = 2;

    new OptExperiment("<project>-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", variant)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(e => data.WriteToCSV(e));

bool ok = project.Execute();
```

`OptModel` 是模型定義，名稱用於 experiment trial label；`ProjectConfig.ProjectName` 才是 log / output 的專案身分。`OnSolved` 屬 `OptProject`，不屬模型，也不存在於 `OptExperiment`。

`ProjectConfig.DataId` / `UserId` 是 solution metadata 預設值；目前呼叫 `CsvCtrl.WriteSolution` 時仍明確傳入。Objective / Constraint constructor 的 canonical 參數順序是 `OptEngine` 第一個，其後才是 Set、Parameter、scalar。

## 轉譯順序

1. Set / Parameter。
2. Dataload：顯式載入，數值保真。
3. Variable 宣告。
4. Constraint 逐條轉譯，一條或一組邏輯相關一檔。
5. ObjectiveFunction。
6. Program.cs：materials → OptModel → runner。
7. `dotnet build` → fix loop ≤ 5。
8. `dotnet run` → 解驗證協定。

## Pool API

```csharp
foreach (var employee in employees)
{
    foreach (var date in dates)
        engine.AddLHS(1.0, new VariableB_Assign { Employee = employee, Date = date });

    var maxDays = maxWorkDays.FirstOrDefault(p => p.Employee == employee)?.QTY ?? 0.0;
    engine.AddRHS(maxDays);
    engine.CreateLessEqual($"{ConstraintName}@{employee}");
}
```

- Model 左邊的項進 `AddLHS`，右邊的項進 `AddRHS`。
- `>=` 用 `CreateGreatEqual`，`<=` 用 `CreateLessEqual`，`=` 用 `CreateEqual`。
- RHS 含變數可用 `AddRHS(coef, variable)`；NEVER 自行移項。
- `EngineBase` 自動統計限制式，不維護第二份 `ConstraintCount`。
- soft constraint 只在 **Model.md 本身就寫了 soft** 時才轉譯（那是 Phase 1 的建模決定），用具名 `CreateLeSoft(rhs, penalty, name)` / `CreateGeSoft(rhs, penalty, name)`，penalty 來自 `Parameter`；成功後框架自動記錄 name、sense、rhs、penalty。NEVER 自行把 hard 改成 soft。

## 取解 API

```csharp
double obj = engine.GetObjectiveValue();
var sol = engine.GetSetVarValues<VariableX_Production>();
double value = engine.GetVariableValue("VariableB_Assign@E1@2026-01-01");
FolderDir.Solution.CreateFolder();
CsvCtrl.WriteSolution<VariableX_Production>(engine, "<Project>", "USER");
```

不存在：`GetVarSol`、`GetSetVarSol<T>`、`SaveToCSV<T>`。簽名不確定就查 API reference。

## 解驗證協定

1. Status：Optimal 才往下；Infeasible 走 IIS；Unbounded 查漏界。
2. 將解代回每條 constraint，確認 LHS op RHS。
3. 檢查目標值、關鍵變數的單位與量級。
4. LP bound sanity：max 的整數解 ≤ LP bound；min 反之。

四步全過且對得上 Model.md 小例才算完成。看到 `Optimal` 就宣稱正確不合格。

## Fatal

- NEVER 自行詮釋 Model.md。
- NEVER 裸數字進 Objective / Constraint。
- NEVER 改 OptimFoundation 框架或換 DLL 來源。
- NEVER 用 helper 隱藏模型組裝。
- NEVER fix loop 超過 5 次。
