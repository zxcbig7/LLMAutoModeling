# OptimFoundation CPLEX Framework

> 本目錄是 AI-Modeling 新專案的 canonical scaffold。sibling `OptimFoundation/OptimFoundation/Templates/` 僅供 framework integration／相容性驗證，不得拿來覆蓋本文件的結構與寫法。若本目錄現有 `.cs` 與本規範衝突，代表 code 待遷移，規範仍為權威。

## 開發兩階段原則（天條）

### 第一階段：數學模型

在唯一模型文件 `Model/ProjectName_Model.md` 依序完整定義問題描述、Terminology Mapping Table、Sets、Parameters、Variables、Constraints、Objective 與已套用假設；NEVER 另建 `Glossary.md`。
數學模型不考慮任何程式細節。

### 第二階段：程式實作

程式開發是**純粹將數學模型轉譯為程式碼**，沒有任何創意發揮。

**★ 禁止 Hardcode（天條）：所有數值一律在 Dataload 的 Parameter 中定義；`Program.cs` 再把每個 Constraint / Objective 真正使用的 Set、Parameter、scalar 與界限值顯式傳入，不得直接寫死任何數字。**

---

## 概述

OptimFoundation 是封裝 IBM ILOG CPLEX 的 C# 框架，用於建構**整數線性規劃（ILP / MIP）**模型。
.NET 8.0；DLL 統一放於 repo 根 `dlls/`（設置見 `dlls/README.md`）。

---

## 專案結構（六資料夾）

```
MyProject/
├── Program.cs # 唯一入口：solve / experiment 雙模式 + 全部層級組裝
├── MyProject.csproj
│
├── Model\
│   └── ProjectName_Model.md # 含術語表的唯一模型文件
│
├── Set\
│   ├── Set_Xxx.cs # [OptSet<T>] 維度積木，一顆一個檔
│   └── Dataload.cs # Sets、Parameters、罰分權重、WriteToCSV
│
├── Parameter\
│   └── Parameter_Xxx.cs # [OptParam] + [OptDim<Set_X>]
│
├── Variable\
│   ├── VariableB_Xxx.cs # Binary
│   ├── VariableX_Xxx.cs # Continuous
│   └── VariableI_Xxx.cs # Integer
│
├── Objective\
│   └── ObjectiveFunction.cs
│
└── Constraint\
    └── Constraint_Xxx.cs # 繼承 ConstraintBase
```

> 不另開只負責轉呼叫的變數註冊、模型組裝、local helper 或實驗 facade。`Program.cs` 是單一組裝點，依「材料 → `OptModel` 三階段 → runner」排列；每個 variable family、Objective、每條 Constraint 各占一個 fluent call，solve 與 experiment 共用同一個模型定義。

---

## 核心元件

### ProjectConfig / CplexConfig — 專案與求解器設定

```csharp
var projectConfig = new ProjectConfig
{
    ProjectName = "ProjectName",
    EnableSolverLog = true, // 預設即為 true
    ExportSol = true,
    ExportLP = true,
    ExportMPS = true,
    DataId = "ProjectName",
    UserId = "SYSTEM",
};
var solverConfig = new CplexConfig
{
    epGap = 0.03, // MIP gap 容忍度（3%）
    timeLimit = 300, // 求解時間上限（秒）
    workThreads = 8, // 平行執行緒數
};
```

`ProjectConfig` 管專案名、保留期、solver log、LP/MPS/Sol 輸出及 solution metadata `DataId` / `UserId`；`CplexConfig` 只放 gap、time limit、threads 等 solver 旋鈕。`CsvCtrl.WriteSolution` 目前仍由呼叫端明確傳入 metadata。完整欄位與 tuning 策略見 [`../tuning/CLAUDE.md`](../tuning/CLAUDE.md)。

### OptEngine — 模型引擎

```csharp
optEngine = new OptEngine(solverConfig, projectConfig);
optEngine.Build(); // 初始化 CPLEX 環境
bool isOK = optEngine.Solve(); // 執行求解，回傳是否找到可行解
int vars = optEngine.varCount; // 目前變數數量
```

### Dataload — 資料層（資料防護，2026-07-18 起）

```csharp
// Program.cs：唯一建構路徑
var data = OptData.Load(() => new Dataload());

// Set/Dataload.cs
public partial class Dataload : DataContext { }
```

- `Dataload` MUST 是 `public partial class Dataload : DataContext`（`partial` + 繼承缺一不可）
- 建構永遠走 `OptData.Load(() => new Dataload())`，**NEVER** 裸 `new Dataload()`——後者仍可編譯，但完全跳過參照完整性 / 重複 key / 數值 sanity 驗證，壞資料會直接進 solver 產出「看起來最佳」的錯答案
- 驗證失敗丟 `DataValidationException`（聚合列出所有問題），這是刻意的 fail-fast，NEVER 用 try/catch 吞掉
- NEVER 在 Dataload 手寫驗證邏輯——機械檢查集中在框架 `DataContext`
- `OptData.Load` 回傳後把資料視為唯讀。`Freeze()` 只攔截框架受控的 mutation API；直接改 public field 或 mutable `List` 不保證立即攔截，專案 code 仍一律不得修改
- 細節見 [`Set/CLAUDE.md`](Set/CLAUDE.md)

### OptModel / OptProject / OptExperiment — 模型與執行環境

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.Items, data.Machines))
    .AddObjective(engine => new ObjectiveFunction(engine, data.Items, data.parameter_Profit).Build())
    .AddConstraints(engine => new Constraint_Capacity(engine, data.Items, data.parameter_Capacity).Build());

if (args.Contains("experiment"))
{
    var baseline = solverConfig.Clone();
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;

    new OptExperiment("project-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", emphasis)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => data.WriteToCSV(engine));

bool ok = project.Execute();
```

- `OptModel` 只記錄模型定義，不持有 engine；框架固定依 variables → objective → constraints 執行，與 fluent 註冊順序無關。
- 每個 variable family、目標式、每條限制式各自註冊；NEVER 用 `CreateVariables` / `BuildObjective` / `BuildConstraints` local helper 或 block lambda 隱藏組成與順序。
- `OptProject` 執行一次正式求解，只有它提供 `OnSolved`；infeasible 時可從 `project.optEngine.GetConflictConstraints()` 取 IIS。
- `OptExperiment` 依 model × solver config 展開實驗，預設 solver log OFF、LP/MPS/Sol 輸出 OFF、housekeeping OFF，並自動儲存 CSV/JSON。
- `Experiment.Save()` 對同名實驗採 append；`Run()` 回傳的 `result.Trials` 已合併歷史。只處理本輪時使用唯一 experiment name，不要重印整份累積清單。

---

## 變數

### 定義（預設：source generator）

本範本**預設**用 `AutoSetsGenerator` 宣告變數——一行 attribute，編譯期自動補完整 class，樣板最省、最不易錯（AI 協作首選）。屬性順序對應 `BuildBVs` 傳入 sets 的順序。

```csharp
using OptimFoundation.Modeling;

// Binary：三維。光桿 [OptVar] + 逐維 [OptDim]；型別由前綴 VariableB_ 決定
[OptVar]
[OptDim<Set_Item>("Item")]
[OptDim<Set_Machine>("Machine")]
[OptDim<Set_Date>("Date")]
public partial class VariableB_Assign { }

// Continuous：一維；型別由前綴 VariableX_ 決定
[OptVar]
[OptDim<Set_Item>("Item")]
public partial class VariableX_Slack { }
```

> `[OptVar]` 光桿宣告不帶型別，型別由類別名前綴決定（`VariableB_/X_/I_`）；前綴非法直接 compile error `OPTF001`（訊息教正確取名）。`[OptDim<TSet>("Name")]` 的泛型參數 MUST 是已掛 `[OptSet<T>]` 的 `Set_<Name>` 積木；attribute 順序 = property 順序 = `BuildVars`/`Build*Vs` 傳入 sets 的順序，property 名 = 字串參數（PascalCase）。csproj 需以 `<Analyzer Include="..\dlls\OptimFoundation.Generators.dll" />` 掛入（本範本已掛）。

**逃生口（遷移用，新 code NEVER 照抄）**：字串式 `[OptVar("Item", "Date:DateTime")]` 與多參數泛型 `[OptVar<Set_A, Set_B>]` 仍受框架支援、不算錯誤，但同一顆 Set 要取多個角色名（`LotA`/`LotB`）時只有 `[OptDim]` 做得到。本範本 `Variable/` 底下的實碼仍是字串式，屬待遷移狀態，勿當成教學範例。

### 定義（後路：手寫）

generator 不適用時（特殊型別、需逐行 debug 生成碼）才手寫，繼承 `VariableBase`、string 屬性加 `= string.Empty;` 避免 CS8618。完整手寫示範見 `Projects/HospitalRostering_Manual`。

```csharp
public class VariableB_Assign : VariableBase
{
    public string Item { get; set; } = string.Empty;
    public string Machine { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
```

### 建立

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildBVs<VariableB_Assign>(data.Items, data.Machines, data.Dates))
    .AddVariables(engine => engine.BuildCVs<VariableX_Slack>(data.Items))
    // 選用：有界 Continuous
    .AddVariables(engine => engine.BuildCVs<VariableX_Flow>(0, 1000, data.Items));
```

變數種類多時仍維持每個 variable family 一行 `.AddVariables(...)`，讓 review 能直接看完整清單。

---

## 限制式建構語法

每個限制式類別繼承 `ConstraintBase`，以 `ConstraintName` 自動取得類別名稱。`ConstraintCount` 已 obsolete；每次 `CreateXxx` 的群組與數量由 `EngineBase` 自動統計。

### LHS / RHS 累加模式

```csharp
// 加入 LHS
optEngine.AddLHS(coefficient, new VariableB_Assign { Item = i, Machine = m, Date = d });

// RHS 常數（MUST 由建構子顯式注入，NEVER 裸數字——見上方天條）
optEngine.AddRHS(someBound);

// RHS 含變數（移項用）
optEngine.AddRHS(1.0, new VariableB_OtherVar { Item = i });
optEngine.AddRHS(-1); // 負常數（繼續累加）

// 建立約束（名稱格式：ConstraintName@idx1@idx2）
optEngine.CreateEqual ($"{ConstraintName}@{i}@{d:yyyy_MM_dd}");
optEngine.CreateLessEqual ($"{ConstraintName}@{i}@{m}");
optEngine.CreateGreatEqual ($"{ConstraintName}@{i}");
```

> **重要**：`AddLHS`/`AddRHS` 為累加；呼叫 `CreateXxx` 後清空，開始下一條。

### 時間滑動視窗

```csharp
var window = dates
    .Where(sd => d.AddDays(-windowSize) < sd && sd <= d)
    .ToList();
if (window.Count < windowSize) return; // 資料不足跳過
window.ForEach(wDate =>
    optEngine.AddLHS(1, new VariableB_AC { A = a, C = wDate }));
optEngine.AddRHS(windowMax);
optEngine.CreateLessEqual($"{ConstraintName}@{a}@{d:yyyy_MM_dd}");
```

---

## 目標函數

```csharp
// ObjectiveFunction.Build() 內；MUST 在所有 Constraint 之前建（soft constraint 的 penalty 要掛進來）
foreach (var i in items)
    optEngine.AddLHS(penaltyWeight, new VariableX_Slack { Item = i });

optEngine.CreateMinimize(); // 或 CreateMaximize()
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
double v2 = engine.GetVariableValue("VariableX_Slack@Item1"); // 分隔符是 @，NEVER 用 |

// ⑤ 取所有變數名稱
string[] allNames = engine.GetAllVarNames();
string[] setNames = engine.GetSetVarNames<VariableX_Slack>();

// ⑥ 存 CSV
CsvCtrl.WriteSolution<VariableB_Assign>(engine, dataId: "V1", userId: "USER");
```

---

## 參數類別

### 預設：source generator（paved path = `[OptParam]` + 每維一個 `[OptDim<TSet>("Name")]`）

```csharp
using OptimFoundation.Modeling;

[OptSet<string>] public partial class Set_Item { }   // 元素型別一律顯式寫出
[OptSet<DateTime>] public partial class Set_Date { }
[OptSet<string>] public partial class Set_Employee { }
[OptSet<string>] public partial class Set_Group { }

// 含值參數（生成 Item/Date/QTY + 兩個建構子）；TSet 決定該維型別
[OptParam]
[OptDim<Set_Item>("Item")]
[OptDim<Set_Date>("Date")]
public partial class Parameter_Demand { }

// 純 key 參數（無 QTY）→ HasValue = false
[OptParam(HasValue = false)]
[OptDim<Set_Employee>("Employee")]
[OptDim<Set_Group>("Group")]
public partial class Parameter_PreAssign { }
```

> generator 自動補 `QTY`（除非 `HasValue = false`）、無參數建構子（object initializer / `CsvCtrl.BuildParameter<T>` 用）與 `params object[]` 建構子（動態建構）。`Set_*` 須另掛 `[OptSet<T>]`，元素型別 MUST 顯式寫出、NEVER 用裸 `[OptSet]`（見 `CPLEX_API_REFERENCE.md` §7.5）。

### 逃生口：多參數泛型 / 字串式（仍受支援，NEVER 標成已淘汰／已移除／錯誤）

```csharp
// 多參數泛型（arity 1..6）：維度名固定 = 積木類名去 Set_ 前綴，無法像 [OptDim] 同顆 Set 取多個角色名
[OptParam<Set_Item, Set_Date>] public partial class Parameter_Demand2 { }

// 字串式（遷移用途，永久保留）：型別用 "Name:Type" 表示，省略型別＝string
[OptParam("Item", "Date:DateTime")]
public partial class Parameter_Demand3 { }

[OptParam("Employee", "Group", HasValue = false)]
public partial class Parameter_PreAssign2 { }
```

> 兩者合法、可編譯、可用於簡單情境；新專案優先教 `[OptDim<TSet>("Name")]`，既有程式碼用這兩種寫法不需遷移。

### 後路：手寫

```csharp
public class Parameter_Demand : ParameterBase
{
    public string Item { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public double QTY { get; set; }

    // NEVER 寫建構子——框架用 reflection 讀屬性順序組 key，物件一律用 object-initializer 建立
}
```

CSV 讀取：

```csharp
SetA = CsvCtrl.ReadStrSet ("Set_A.csv");
SetC = CsvCtrl.ReadDateSet ("Set_C.csv");
paramList = CsvCtrl.BuildParameter<Parameter_Demand>("Param_Demand");
```

---

## Logging

```csharp
// OptProject.Execute() 會依有效 ProjectConfig 設定 log 檔名。
Logging.Info("訊息");
Logging.Info("訊息含計時:", stopwatch);
```

---

## 新增限制式 Checklist

1. `Variable/` 建立 `VariableB_Xxx.cs` 或 `VariableX_Xxx.cs`
2. `Program.cs` 的 `OptModel` variables 階段直接新增一行 `.AddVariables(...)`
3. `Constraint/` 建立 `Constraint_Xxx.cs`（繼承 `ConstraintBase`）
4. `Program.cs` 的 constraints 階段直接新增一行 `.AddConstraints(engine => new Constraint_Xxx(engine, 明確依賴...).Build())`
5. 若有罰分，`Objective/ObjectiveFunction.Build()` 加 `AddLHS(penalty, var)`
6. `Set/Dataload` 加 penalty 權重與 parameter

---

## 依賴套件

| 套件 | 說明 |
|------|------|
| `ILOG.CPLEX` / `ILOG.Concert` | IBM CPLEX 求解器核心 |
| `OptimFoundation.Core` | VariableBase、ParameterBase、ConstraintBase、Logging、CsvCtrl |
| `OptimFoundation.Cplex` | OptEngine、CplexConfig、OptModel、OptProject、OptExperiment |
| `NLog` | 日誌 |

> **★ 天條**：所有 DLL 統一放在 repo 根 `dlls/`（設置見 `dlls/README.md`）。  
> DLL HintPath（Template_CPLEX）：`..\dlls\Xxx.dll`；Projects：`..\..\dlls\Xxx.dll`
