# OptimFoundation CPLEX Framework

## 開發兩階段原則（天條）

### 第一階段：數學模型

在 `Model/ProjectName_Model.md` 完整定義 Sets、Parameters、Variables、Objective、Constraints。
數學模型不考慮任何程式細節。

### 第二階段：程式實作

程式開發是**純粹將數學模型轉譯為程式碼**，沒有任何創意發揮。

**★ 禁止 Hardcode（天條）：所有數值一律在 Dataload 的 Parameter 中定義，Constraint / Objective 只能透過 dataload 查詢係數，不得直接寫死任何數字。**

---

## 概述

OptimFoundation 是封裝 IBM ILOG CPLEX 的 C# 框架，用於建構**整數線性規劃（ILP / MIP）**模型。
.NET 8.0；DLL 統一放於 `ClaudeAIAssistant\dlls\`（見根目錄 CLAUDE.md 天條）。

---

## 專案結構（5-component）

```
MyProject/
├── Program.cs
├── MyProblem.cs                 # 主問題類別（IDisposable）
├── MyProject.csproj
│
├── Set\
│   └── Dataload.cs              # Sets、Parameters、罰分權重
│
├── Parameter\
│   └── Parameter_Xxx.cs         # 繼承 ParameterBase
│
├── Variable\
│   ├── VariableB_Xxx.cs         # Binary（繼承 VariableBase）
│   ├── VariableX_Xxx.cs         # Continuous（繼承 VariableBase）
│   └── VariableCreate.cs        # BuildBVs / BuildCVs 呼叫
│
├── Objective\
│   └── ObjectiveFunction.cs
│
└── Constraint\
    ├── BuildModel.cs
    └── Constraint_Xxx.cs        # 繼承 ConstraintBase
```

---

## 核心元件

### CplexConfig — 求解器設定

```csharp
CplexConfig config = new CplexConfig
{
    epGap       = 0.03,    // MIP gap 容忍度（3%）
    timeLimit   = 300,     // 求解時間上限（秒）
    workThreads = 8,       // 平行執行緒數
    enableLog   = true,    // 顯示 CPLEX log
    exportSol   = true,    // 匯出 .sol 檔
    exportLP    = true,    // 匯出 .lp 檔（可讀模型）
    exportMPS   = true     // 匯出 .mps 檔
};
```

### OptEngine — 模型引擎

```csharp
optEngine = new OptEngine(config);
optEngine.Build();              // 初始化 CPLEX 環境
bool isOK = optEngine.Solve();  // 執行求解，回傳是否找到可行解
int  vars  = optEngine.varCount; // 目前變數數量
```

---

## 變數

### 定義（預設：source generator）

本範本**預設**用 `AutoSetsGenerator` 宣告變數——一行 attribute，編譯期自動補完整 class，樣板最省、最不易錯（AI 協作首選）。屬性順序對應 `BuildBVs` 傳入 sets 的順序。

```csharp
using OptimModeling;

// Binary：三維。生成 Item/Machine/Date 屬性
[OptVar(VarType.Binary, "Item", "Machine", "Date:DateTime")]
public partial class VariableB_Assign { }

// Continuous：一維
[OptVar(VarType.Continuous, "Item")]
public partial class VariableX_Slack { }
```

> set 字串：`"Name"`＝string；`"Name:DateTime"` / `:int` / `:double` 指定型別。csproj 需以 analyzer 掛入 `OptimModeling.Generators`（本範本已掛）。

### 定義（後路：手寫）

generator 不適用時（特殊型別、需逐行 debug 生成碼）才手寫，繼承 `VariableBase`、string 屬性加 `= string.Empty;` 避免 CS8618。完整手寫示範見 `Projects/HospitalRostering_Manual`。

```csharp
public class VariableB_Assign : VariableBase
{
    public string   Item    { get; set; } = string.Empty;
    public string   Machine { get; set; } = string.Empty;
    public DateTime Date    { get; set; }
}
```

### 建立

```csharp
// VariableCreate.Build() 內
optEngine.BuildBVs<VariableB_Assign>(dataload.Items, dataload.Machines, dataload.Dates);
optEngine.BuildCVs<VariableX_Slack>(dataload.Items);
// 選用：有界 Continuous
optEngine.BuildCVs<VariableX_Flow>(lb: 0, ub: 1000, dataload.Items);
```

---

## 限制式建構語法

每個限制式類別繼承 `ConstraintBase`，提供：
- `ConstraintName` — 自動取得類別名稱
- `ConstraintCount` — 建立數量計數器

### LHS / RHS 累加模式

```csharp
// 加入 LHS
optEngine.AddLHS(coefficient, new VariableB_Assign { Item = i, Machine = m, Date = d });

// RHS 常數
optEngine.AddRHS(10);

// RHS 含變數（移項用）
optEngine.AddRHS(1.0, new VariableB_OtherVar { Item = i });
optEngine.AddRHS(-1); // 負常數（繼續累加）

// 建立約束（名稱格式：ConstraintName@idx1@idx2）
optEngine.CreateEqual      ($"{ConstraintName}@{i}@{d:yyyy_MM_dd}");
optEngine.CreateLessEqual  ($"{ConstraintName}@{i}@{m}");
optEngine.CreateGreatEqual ($"{ConstraintName}@{i}");

ConstraintCount++;
```

> **重要**：`AddLHS`/`AddRHS` 為累加；呼叫 `CreateXxx` 後清空，開始下一條。

### 時間滑動視窗

```csharp
var window = dataload.Dates
    .Where(sd => d.AddDays(-windowSize) < sd && sd <= d)
    .ToList();
if (window.Count < windowSize) return; // 資料不足跳過
window.ForEach(wDate =>
    optEngine.AddLHS(1, new VariableB_AC { A = a, C = wDate }));
optEngine.AddRHS(windowMax);
optEngine.CreateLessEqual($"{ConstraintName}@{a}@{d:yyyy_MM_dd}");
ConstraintCount++;
```

---

## 目標函數

```csharp
// ObjectiveFunction.Build() 內
dataload.Items.ForEach(i =>
    optEngine.AddLHS(penaltyWeight, new VariableX_Slack { Item = i }));

optEngine.CreateMinimize();   // 或 CreateMaximize()
```

---

## 解取得 API

```csharp
// ① 目標函數值
double obj = engine.GetObjectiveValue();

// ② 指定型別全部解值 → Dictionary<varName, double>
var sol = engine.GetSetVarValues<VariableX_Slack>();
foreach (var kvp in sol)
    Logging.Info($"{kvp.Key} = {kvp.Value}");

// ③ 依型別名稱取解（key 為 set 值，不含型別前綴）
var sol2 = engine.GetSolution("VariableX_Slack");
double val = sol2.TryGetValue("Item1", out var v) ? v : 0;

// ④ 依完整變數名稱取值
double v2 = engine.GetVariableValue("VariableX_Slack|Item1");

// ⑤ 取所有變數名稱
string[] allNames = engine.GetAllVarNames();
string[] setNames = engine.GetSetVarNames<VariableX_Slack>();

// ⑥ 存 CSV
CsvCtrl.SaveSolutionToCSV<VariableB_Assign>(engine, dataId: "V1", userId: "USER");
```

---

## 參數類別

### 預設：source generator

```csharp
using OptimModeling;

// 含值參數（生成 Item/Date/QTY + 兩個建構子）
[OptParam("Item", "Date:DateTime")]
public partial class Parameter_Demand { }

// 純 key 參數（無 QTY）→ HasValue = false
[OptParam("Employee", "Group", HasValue = false)]
public partial class Parameter_PreAssign { }
```

> generator 自動補 `QTY`（除非 `HasValue = false`）、無參數建構子（object initializer / `CsvCtrl.BuildParameter<T>` 用）與 `params object[]` 建構子（動態建構）。

### 後路：手寫

```csharp
public class Parameter_Demand : ParameterBase
{
    public string   Item  { get; set; } = string.Empty;
    public DateTime Date  { get; set; }
    public double   QTY   { get; set; }

    public Parameter_Demand() { }
    public Parameter_Demand(params object[] sets) => InitClassBySets(sets);  // 動態建構需要時
}
```

CSV 讀取：

```csharp
SetA         = CsvCtrl.ReadStrSet   ("Set_A.csv");
SetC         = CsvCtrl.ReadDateSet  ("Set_C.csv");
paramList    = CsvCtrl.BuildParameter<Parameter_Demand>("Param_Demand");
```

---

## Logging

```csharp
Logging.SetLogFileName("ProjectName"); // 建構子內呼叫
Logging.Info("訊息");
Logging.Info("訊息含計時:", stopwatch);
```

---

## 新增限制式 Checklist

1. `Variable/` 建立 `VariableB_Xxx.cs` 或 `VariableX_Xxx.cs`
2. `Variable/VariableCreate.Build()` 加 `BuildBVs<>()` / `BuildCVs<>()`
3. `Constraint/` 建立 `Constraint_Xxx.cs`（繼承 `ConstraintBase`）
4. `Constraint/BuildModel.Build()` 加 `new Constraint_Xxx(dataload, engine).Build()`
5. 若有罰分，`Objective/ObjectiveFunction.Build()` 加 `AddLHS(penalty, var)`
6. `Set/Dataload` 加 penalty 權重與 parameter

---

## 依賴套件

| 套件 | 說明 |
|------|------|
| `ILOG.CPLEX` / `ILOG.Concert` | IBM CPLEX 求解器核心 |
| `OptimFoundation.Core` | VariableBase、ParameterBase、ConstraintBase、Logging、CsvCtrl |
| `OptimFoundation.Cplex` | OptEngine、CplexConfig |
| `NLog` | 日誌 |

> **★ 天條**：所有 DLL 統一放在 `ClaudeAIAssistant\dlls\`。  
> DLL HintPath（Template_CPLEX）：`..\dlls\Xxx.dll`；Projects：`..\..\dlls\Xxx.dll`
