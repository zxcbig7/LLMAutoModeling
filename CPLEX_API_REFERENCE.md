# OptimFoundation CPLEX 接口參考文件

> **用途**：本文件是給 AI 在生成 / 修改 / 驗證 OptimFoundation CPLEX 程式碼時的權威參考。
> **權威來源**：`dlls/` 內編譯版 OptimFoundation 的公開簽名；原始碼在 sibling 資料夾 `../OptimFoundation/`（本 repo 外，非硬相依）。
> **天條**：所有 API 呼叫必須能在編譯版 OptimFoundation 找到對應定義；本文件未列出的方法視為「不存在」。
> **NEVER**：禁止修改 OptimFoundation 框架本體（唯讀）。
> **鏡像同步**：源頭 = `../OptimFoundation/specs/developer-guide.md`；本檔為鏡像。最近同步 **2026-07-19**（修正 §7.5 OptDim/泛型定位、新增 §7.6 `DataContext`/`OptData` 資料防護層、§8 補 `ISolutionSink` batch/transaction、§5.2 修正 `Build()`/`Solve()` 為 template method）。
> 註：`developer-guide.md` 截至本次同步**尚未**跟進框架 2026-07-18「資料防護」規格（`DataContext`/`OptData`/`SafeRatio`/`FullGrid`/`OPTF006`），故本次新增內容改以框架原始碼（`OptimFoundation.Core`）+ `../OptimFoundation/specs/2026-07-18-framework-data-guard.md` 為準；待來源文件補齊後兩邊再對齊。改框架 public API 時 MUST 兩邊一起更新。

---

## 目錄

1. [模組架構與相依關係](#1-模組架構與相依關係)
2. [型別 `ModelElementBase` — 變數名稱規則](#2-型別-modelelementbase--變數名稱規則)
3. [型別 `VariableBase` / `ParameterBase` / `ConstraintBase`](#3-型別-variablebase--parameterbase--constraintbase)
4. [`CplexConfig` — 求解器設定](#4-cplexconfig--求解器設定)
5. [`OptEngine` — 求解引擎完整 API](#5-optengine--求解引擎完整-api)
6. [`EngineBase` 繼承 API — 變數 / Pool / 限制式](#6-enginebase-繼承-api--變數--pool--限制式)
7. [`VariableBuilder` — 變數名稱生成規則](#7-variablebuilder--變數名稱生成規則)
8. [`CsvCtrl` — CSV I/O 工具](#8-csvctrl--csv-io-工具)
9. [`FolderDir` — 專案目錄結構](#9-folderdir--專案目錄結構)
10. [`Logging` — 日誌工具](#10-logging--日誌工具)
11. [`Enums` 與 `ISolverEngine` 介面](#11-enums-與-isolverengine-介面)
12. [標準執行流程](#12-標準執行流程)
13. [常見模式與反模式](#13-常見模式與反模式)
14. [完整最小範例](#14-完整最小範例)
15. [API 黑名單（不存在的方法）](#15-api-黑名單不存在的方法)
16. [Experiment 套件 — Tuning 實驗記錄](#16-experiment-套件--tuning-實驗記錄)

---

## 1. 模組架構與相依關係

```text
OptimFoundation.Core (沒有 CPLEX 相依)
├── DesignBases.cs        ModelElementBase / VariableBase / ParameterBase / ConstraintBase
├── EngineBase.cs         EngineBase<TModel,TVar,TExpr,TConstr> : ISolverEngine
├── Enums.cs              VarType / ConstraintSense / ObjectiveSense
├── ISolverEngine.cs      ISolverEngine 介面、SolveStatus enum
├── ISolverConfig.cs      ISolverConfig 介面
├── VariableBuilder.cs    靜態：產生變數名稱組合
├── Csv/CsvCtrl.cs        CSV 讀寫
├── Infrastructure/
│   ├── ClassInfo.cs      反射取得型別欄位資訊
│   ├── FolderDir.cs      Data/Solution/Log/Model/IIS/Sol 六個資料夾
│   └── ReflectionHelper.cs
└── Logging/Logging.cs    Console + 檔案雙寫

OptimFoundation.Cplex (相依 Core + IBM ILOG.Concert + ILOG.CPLEX)
├── CplexConfig.cs        : ISolverConfig
└── OptEngine.cs          : EngineBase<Cplex, INumVar, ILinearNumExpr, IRange>
```

**Namespace**：
- `OptimFoundation.Core` — 抽象 + 共用工具
- `OptimFoundation.Cplex` — CPLEX 實作

**必要 using**：

```csharp
using OptimFoundation.Cplex;     // OptEngine, CplexConfig
using OptimFoundation.Core;      // VariableBase, ParameterBase, Logging, FolderDir, CsvCtrl
```

---

## 2. 型別 `ModelElementBase` — 變數名稱規則

**檔案**：`Foundation\src\OptimFoundation.Core\DesignBases.cs`

```csharp
public abstract class ModelElementBase
{
    protected ModelElementBase() { }
    protected ModelElementBase(params object[] sets);  // 自動呼叫 InitClassBySets
    public void InitClassBySets(params object[] sets); // 按屬性順序賦值
    public override string ToString();                 // ClassName@p1@p2@...
}
```

### ★ `ToString()` 規則（變數名稱來源）

```text
變數名稱 = ClassName@{p1}@{p2}@...@{pn}
按 GetProperties() 順序串接，DateTime 強制格式 "yyyy-MM-dd"
```

**範例**：

| 子類別定義 | 物件 | `ToString()` 結果 |
|---|---|---|
| `class VariableX_Production : VariableBase { public string GlassType {get;set;} }` | `new VariableX_Production { GlassType = "Regular" }` | `"VariableX_Production@Regular"` |
| `class VariableB_Assign : VariableBase { public string Employee {get;set;} public DateTime Date {get;set;} }` | `new VariableB_Assign { Employee = "E1", Date = new DateTime(2026,1,1) }` | `"VariableB_Assign@E1@2026-01-01"` |
| `class Parameter_AB : ParameterBase { public string A {get;set;} public string B {get;set;} public double QTY {get;set;} }` | `new Parameter_AB { A = "A1", B = "B1", QTY = 3 }` | `"Parameter_AB@A1@B1@3"` |

### ★ 屬性順序就是變數索引順序

`GetProperties()` 回傳順序 = 程式碼宣告順序。**因此屬性宣告順序必須與 `BuildBVs` / `BuildCVs` 傳入的 sets 順序一致**。

```csharp
public class VariableB_Assign : VariableBase
{
    public DateTime Date     { get; set; }   // 第1個 set
    public string   Employee { get; set; }   // 第2個 set
    public string   Group    { get; set; }   // 第3個 set
}

// 呼叫端必須對應順序：
optEngine.BuildBVs<VariableB_Assign>(dataload.Date, dataload.Employee, dataload.Group);
// 結果變數名稱：VariableB_Assign@2026-01-01@E1@D
```

### `InitClassBySets(params object[])` 強制型別轉換

- 自動 `Convert.ChangeType` 字串 → `int` / `double` / `DateTime` 等。
- 失敗丟 `InvalidCastException`。
- 屬性數 ≠ 參數數時丟 `ArgumentException`。

---

## 3. 型別 `VariableBase` / `ParameterBase` / `ConstraintBase`

**檔案**：`Foundation\src\OptimFoundation.Core\DesignBases.cs`

```csharp
public abstract class VariableBase : ModelElementBase { protected string VariableName => ElemName; }
public abstract class ParameterBase : ModelElementBase { protected string ParameterName => ElemName; }
public abstract class ConstraintBase : ModelElementBase
{
    protected int ConstraintCount { get; set; }
    protected string ConstraintName => ElemName;
}
```

**三者差異**：

| 類別 | 用途 | 命名慣例 |
|---|---|---|
| `VariableBase` | 決策變數的索引描述 | `VariableX_Xxx`（連續）、`VariableI_Xxx`（整數）、`VariableB_Xxx`（Binary） |
| `ParameterBase` | 模型參數的索引描述（通常含 `QTY` 欄位） | `Parameter_Xxx` |
| `ConstraintBase` | 限制式類別的 base，提供 `ConstraintName` / `ConstraintCount` | `Constraint_Xxx` |

### 子類別寫法（無建構子，全用 property initializer）

```csharp
public class VariableX_Sandwich : VariableBase
{
    public string SandwichType { get; set; } = string.Empty;
}

public class Parameter_ShiftDemand : ParameterBase
{
    public DateTime Date  { get; set; }
    public string   Group { get; set; } = string.Empty;
    public double   QTY   { get; set; }
}
```

> ★ **string 屬性務必初始化為 `= string.Empty;`** — 否則 nullable C# (`<Nullable>enable</Nullable>`) 會出 CS8618 警告。

💡 **預設改用 source generator**：上面的手寫 class 是「後路」。框架附 `AutoSetsGenerator`，可用一行 attribute 取代整段樣板（編譯期生成相同的 `: VariableBase`/`: ParameterBase` + 屬性 + QTY + 建構子）：

```csharp
using OptimFoundation.Modeling;   // generator 注入的 attribute namespace

[OptVar("SandwichType")]                            public partial class VariableX_Sandwich { }   // 字串式：型別由前綴 VariableX_ 決定
[OptParam("Date:DateTime", "Group")]                public partial class Parameter_ShiftDemand { }
[OptParam("Employee", "Group", HasValue = false)]   public partial class Parameter_PreAssign { }   // 純 key，無 QTY
```

上為**字串式**（逃生口，永久保留、仍受支援）；**paved path** 是 Set 積木 + 逐維具名宣告見 §7.5（`[OptSet<T>]` + 光桿 `[OptVar]`/`[OptParam]` + `[OptDim<TSet>("Name")]`）；同節也列了多參數泛型 `[OptVar<Set_X>]` 這條**同樣受支援**的逃生口。`[OptVar]` 型別由類別名前綴決定（`VariableB_/X_/I_`），前綴非法 → `OPTF001`；`OptParam` 非 `Parameter_` 前綴 → `OPTF002`。csproj 以 analyzer 掛入 `..\dlls\OptimFoundation.Generators.dll`。可運作範例 `Projects/HospitalRostering_Generator`（vs 手寫版 `Projects/HospitalRostering_Manual`，註：該範例建於 OptDim 定案之前，仍用舊字串式）。

### 為何不需要寫建構子

`VariableBuilder.GetCtor()` 自動偵測（優先序）：

1. 無參數建構子 → 透過 `InitClassBySets(parts)` 賦值
2. `public Foo(params object[] sets) : base(sets) {}`
3. `public Foo(string[] parts)`（舊寫法）

→ **預設用 property initializer + 物件初始化器即可，不需要寫任何建構子。**

---

## 4. `CplexConfig` — 求解器設定

**檔案**：`Foundation\src\OptimFoundation.Cplex\CplexConfig.cs`

```csharp
public sealed class CplexConfig : ISolverConfig, ITunableConfig
{
    public int? workThreads = 32; // 工作執行緒上限
    public bool enableLog = false; // 啟用 CPLEX log
    public bool exportLP = false; // 輸出 .lp 模型檔
    public bool exportSol = false; // 輸出 .sol 解檔
    public bool exportMPS = false; // 輸出 .mps 模型檔
    public int? rowRead = 30000; // 限制式上限
    public double? workMemory = 2048; // 工作記憶體 MB
    public double? epGap = 1e-4; // MIP gap 終止條件
    public int? nodeSelect = null; // MIP.Strategy.NodeSelect
    public int? randomSeed = null; // Param.RandomSeed
    public double? epOpt = 1e-06; // Optimality tolerance
    public double? epRHS = 1e-06; // Feasibility tolerance
    public double? timeLimit = null; // 求解秒數上限（null=無限）
    public double? polishAfterTime = null; // Solution polishing 起始秒數
    public int? mipEmphasis = null; // 1=feasibility/2=optimality/3=bestBound/4=hidden
    public int? varSel = null; // 分支變數選擇策略
    public int? algorithm = null; // RootAlgorithm 1~6
    public int? nodeFileInd = null; // 0/2/3 節點檔策略

    // ISolverConfig adapter
    public double? TimeLimit { get; set; }
    public double? MipGap { get; set; }
    public int? Threads { get; set; }
    public bool LogToConsole { get; set; }
    public string LogFilePath { get; set; }
    public int? RootAlgorithm { get; set; }
    public int? NodeAlgorithm { get; set; }
    public bool? PreIndicator { get; set; }

    // ITunableConfig — 跨引擎抽象 tuning 旋鈕，delegate 到上方既有欄位（null=用 solver 預設）
    public int? Seed { get; set; } // → randomSeed
    public int? Emphasis { get; set; } // → mipEmphasis
    public double? FeasibilityTol { get; set; } // → epRHS
    public double? OptimalityTol { get; set; } // → epOpt
    public int? Presolve { get; set; } // ↔ PreIndicator（0=off/非0=on）
    public double? HeuristicEffort { get; set; } // CPLEX 無直接對應，僅供快照記錄
    public double? MemoryLimitMb { get; set; } // → workMemory（MB）
    // RootAlgorithm（→ algorithm）由上方滿足
}
```

> ★ **抽象旋鈕 vs camelCase 欄位**：兩者指向同一設定（`config.Seed = 7` 等同 `config.randomSeed = 7`）。
> 用抽象旋鈕（`ITunableConfig`）寫的 tuning code 可跨 Cplex / Gurobi / Solver；用 camelCase 欄位則是 CPLEX 專屬。
>
> ★ **上面是常用欄位的代表性子集，非全部**。CPLEX 專屬的切割族（`gomoryCuts`/`coverCuts`/`cliqueCuts`/`mirCuts`/`flowCoverCuts`）、`probe`、`parallelMode`、`numericalEmphasis`、`cutsFactor`、`treeMemoryLimit`、`detTimeLimit`、`epAGap`、`epInt` 等，連同各自的 CPLEX `Param.*` 路徑與取值範圍，**完整對照表見 [`tuning/CLAUDE.md`](tuning/CLAUDE.md) §6.2**。

**標準寫法**：

```csharp
CplexConfig config = new CplexConfig
{
    epGap = 0.0, // 求最佳解
    timeLimit = 60,
    workThreads = 4,
    enableLog = true,
    exportSol = true,
    exportLP = true,
    exportMPS = false
};
```

> ★ 欄位是 **camelCase** + 公開 **field**（不是 property），直接 `=` 賦值即可。

---

## 5. `OptEngine` — 求解引擎完整 API

**檔案**：`Foundation\src\OptimFoundation.Cplex\OptEngine.cs`

```csharp
public class OptEngine : EngineBase<ILOG.CPLEX.Cplex, INumVar, ILinearNumExpr, IRange>
```

### 5.1 建構子

```csharp
public OptEngine(CplexConfig config);
public OptEngine();   // 用預設 CplexConfig
```

### 5.2 生命週期

```csharp
public void Build();                   // EngineBase 具體方法（非 virtual）：呼叫 BuildCore()
public bool Solve();                   // EngineBase 具體方法（非 virtual）：先 PreSolveGuard() 再呼叫 SolveCore()
public void Dispose();                 // 釋放 native resources
```

> ★ **2026-07 起 `Build()`/`Solve()` 是 `EngineBase` 的 template method、已改為非 virtual**——`OptEngine` 內部只實作 `protected override void BuildCore()` / `protected override bool SolveCore()`。消費端呼叫方式不變（`engine.Build()`/`engine.Solve()`）；但**若要繼承 `OptEngine` 自訂行為，NEVER `override Build()`/`override Solve()`（已非 virtual，直接編譯錯誤）——改覆寫 `BuildCore()`/`SolveCore()`**（`developer-guide.md` 第 13 節「進階：繼承 OptEngine」尚未更新此節，以本檔與框架原始碼為準）。

`Solve()` 行為：

1. **`PreSolveGuard()`**（新增）：`TotalVarCount` 超過 `Config.ScaleWarnThreshold`（`ISolverConfig` 的 default interface member，預設 **10,000,000**，可由具體 config 覆寫）→ `Logging.Warn` 一則（含實際變數數與門檻），**只警告、不阻擋、不中止求解**
2. 若 `exportLP=true` → 寫 `Models/{ProjectName}_LP_{timestamp}.lp`
3. 若 `exportMPS=true` → 寫 `Models/{ProjectName}_MPS_{timestamp}.mps`
4. 呼叫 `Model.Solve()`
5. 設定 `Status`（Optimal / Feasible / Infeasible / Unbounded / Error）
6. 若解可行且 `exportSol=true` → 寫 `Sols/{ProjectName}_Solution_{timestamp}.sol`
7. 若 Infeasible → 自動執行 `RefineConflict` 寫 IIS `IISs/{ProjectName}_IIS_{timestamp}.ilp`
8. 印出 `ObjVal / BestBound / MIPGap`
9. 回填 `LastMetrics`（`SolveMetrics`）：wall time / status / 目標值 / bound / gap / node·iter 數 / var·constraint 數（+ 收斂軌跡，若有啟用）

```csharp
public SolveMetrics LastMetrics { get; }   // EngineBase；Solve() 後填入，未求解為 null
```

> 詳見第 16 節「Experiment 套件」——`SolveMetrics` 是 tuning 實驗記錄的核心 telemetry。

### 5.3 取解

```csharp
public override double GetObjectiveValue();
public override double GetVariableValue(string name);  // name = 完整變數名稱含 @
public          Dictionary<string, double> GetSetVarValues<T>();  // EngineBase
public          IReadOnlyDictionary<string, double> GetSolution(string varTypeName = null);
```

| 方法 | 輸入 | 輸出 | 範例 |
|---|---|---|---|
| `GetObjectiveValue()` | — | `double` | `60.0` |
| `GetVariableValue(name)` | `"VariableX_Sandwich@Regular"` | `double` | `20.0` |
| `GetSetVarValues<T>()` | 泛型 T | `Dictionary<string,double>` key = 完整名稱 | `{"VariableX_Sandwich@Regular":20, "VariableX_Sandwich@Special":0}` |
| `GetSolution(typeName)` | `"VariableX_Sandwich"` 或 `null` | `IReadOnlyDictionary` | 同上 |
| `GetSolution(null)` | — | 所有變數 | 含所有 type |

### 5.4 變數查詢（EngineBase 繼承）

```csharp
public int      varCount       { get; }           // 全部變數數
public int      TotalVarCount  { get; }           // 跨 VariableSets 加總
public string[] GetAllVarNames();
public string[] GetSetVarNames<T>();
public void     VarSetsReset();                   // 清空所有變數
```

### 5.5 模型命名

```csharp
public void SetModelName(string name);   // 影響 LP/MPS/Sol 檔名前綴
```

### 5.6 OptEngine 額外便捷方法（protected — 子類別專用）

```csharp
protected INumVar     CreateVar(string name, double lb = 0, double ub = double.MaxValue, VarType type = VarType.Continuous);
protected ILinearNumExpr Expr(IEnumerable<(double coef, INumVar var)> terms);
protected IRange      AddLE(string name, ILinearNumExpr lhs, double rhs);
protected IRange      AddGE(string name, ILinearNumExpr lhs, double rhs);
protected IRange      AddEQ(string name, ILinearNumExpr lhs, double rhs);
protected void        Minimize(ILinearNumExpr expr);
protected void        Maximize(ILinearNumExpr expr);
```

> 一般使用情境**不會**用到這些 — 都用下方 Pool API 即可。

---

## 6. `EngineBase` 繼承 API — 變數 / Pool / 限制式

**檔案**：`Foundation\src\OptimFoundation.Core\EngineBase.cs`
所有 `OptEngine` 物件都繼承以下 public 方法。

### 6.1 批次建立變數（BuildVars / BuildBVs / BuildCVs / BuildIVs）

```csharp
public virtual void BuildCVs<T>(params object[] sets);                    // 連續變數 [0, 1e100]
public virtual void BuildCVs<T>(double lb, double ub, params object[] sets);
public virtual void BuildIVs<T>(params object[] sets);                    // 整數變數 [0, 1e100]
public virtual void BuildIVs<T>(double lb, double ub, params object[] sets);
public virtual void BuildBVs<T>(params object[] sets);                    // Binary [0, 1]
```

**T 必須**：繼承 `VariableBase`，且**屬性順序 = sets 順序**。

**sets 支援型別**：任何 `IEnumerable<T>`（`List<T>`、`T[]` 皆可），T = `string` / `int` / `long` / `double` / `decimal` / `DateTime` / enum。單獨傳一個 `string[]` 也安全——framework 會自動還原 C# params 共變的誤攤平。

```csharp
// 一維
optEngine.BuildCVs<VariableX_Production>(dataload.GlassTypes);
// → 生成：VariableX_Production@Regular, VariableX_Production@Tempered

// 三維
optEngine.BuildBVs<VariableB_Assign>(dataload.Date, dataload.Employee, dataload.Group);
// → 生成：VariableB_Assign@2026-01-01@E1@D, ...

// 自訂 lb / ub
optEngine.BuildCVs<VariableX_Slack>(0.0, 100.0, dataload.Items);
```

### 6.2 Pool API — 累加 LHS / RHS

`AddLHS` / `AddRHS` 是「進池子」操作。同一限制式內可反覆呼叫，最後用 `CreateXxx` 一次出池。

```csharp
public bool AddLHS(double coeff, object varSpec);   // 變數項
public bool AddLHS(double constant);                // 常數項
public bool AddRHS(double coeff, object varSpec);   // RHS 含變數（自動移項：-coeff·var 到 LHS）
public bool AddRHS(double constant);                // RHS 常數
public bool HasPool { get; }
public void ClearPool();
```

`varSpec` 可以是：

- `new VariableX_Sandwich { SandwichType = "Regular" }`（最常用，物件初始化器）
- 任何 `override ToString()` 後等於完整變數名稱的物件

**自動移項邏輯**（EngineBase.cs:206-212）：

```text
最終形式  =  (Σ lhsTerms - Σ rhsTerms)  sense  (rhsConst - lhsConst)
```

### 6.3 建立限制式

```csharp
public bool CreateLessEqual(string name);                // <=
public bool CreateLessEqual(double rhs, string name);    // 直接給 RHS 常數
public bool CreateGreatEqual(string name);               // >=
public bool CreateGreatEqual(double rhs, string name);
public bool CreateEqual(string name);                    // ==
public bool CreateEqual(double rhs, string name);
public bool CreateRange(double lb, double ub, string name);  // lb <= expr <= ub
```

**命名格式（強制）**：`ConstraintName@index1@index2@...`

```csharp
optEngine.CreateLessEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
```

> ★ `CreateXxx` 會自動 `ClearPool()`，下一條限制式不需手動清。

### 6.4 目標函數

```csharp
public void CreateMinimize();   // 將 pool LHS 設為目標式 (min)
public void CreateMaximize();   // (max)
```

### 6.5 軟限制式（彈性變數法 — 通用實作於 EngineBase，三引擎共用）

```csharp
public virtual bool SupportsSoftConstraints { get; }   // 預設 true
public virtual bool CreateLeSoft(double rhs, double penalty);
public virtual bool CreateGeSoft(double rhs, double penalty);
public virtual bool CreateEqSoft(double rhs, double penalty, string name);
```

> ★ **機制**：在變數池加一個彈性變數 + 建限制式 + 在目標式加對應 penalty（min→正號、max→負號）。
> 用法與 hard 版相同：先 `AddLHS(...)` 累加 LHS，再呼叫 `CreateXxSoft(rhs, penalty)`。

| 方法 | 彈性變數 | 建立的限制式 | 目標式加項 |
|---|---|---|---|
| `CreateLeSoft` | `Surplus_{name}` ≥ 0 | `LHS − surplus <= rhs` | `+penalty·surplus` |
| `CreateGeSoft` | `Deficit_{name}` ≥ 0 | `LHS + deficit >= rhs` | `+penalty·deficit` |
| `CreateEqSoft` | `Delta_Neg_{name}`、`Delta_Pos_{name}` ≥ 0 | `LHS + dn − dp == rhs` | `+penalty·(dn+dp)` |

- 彈性變數會註冊進 `Variables`（解值可查），名稱依上表；Le/Ge 未指定 name 時自動命名 `Soft_{Sense}_{n}`。
- 彈性變數量 = 求解後的「違反量」，可用 `GetVariableValue("Deficit_...")` 取得。
- Phase 3 tuning 把某條 hard constraint 改 soft 時，**必須同步更新 `Model.md`**（見 CLAUDE.md Phase 3）。

### 6.6 變數界限調整（protected）

```csharp
protected void SetVarLB(object searchData, double lb);
protected void SetVarUB(object searchData, double ub);
protected void SetVarRange(object searchData, double lb, double ub);
```

需要在繼承 `OptEngine` 的子類別才能呼叫；一般專案不用，建議在 `BuildCVs` 直接指定。

---

## 7. `VariableBuilder` — 變數名稱生成規則

**檔案**：`Foundation\src\OptimFoundation.Core\VariableBuilder.cs`

```csharp
public static class VariableBuilder
{
    public static IEnumerable<string>   GetVarNames<T>(object[] sets);                  // 主入口
    public static IEnumerable<string>   GenVarCombinations(params List<string>[] lists); // "@a@b"
    public static List<string>[]        ConvertSetsToStringLists(params object[] lists);
    public static void                  BuildVars<T>(Action<object> createVarMethod, object[] sets);
}
```

### Set → 字串轉換規則（`ConvertSetsToStringLists`）

| Set 型別 | 轉字串方式 |
|---|---|
| `List<string>` | 原樣 |
| `List<int>` | `n.ToString()` |
| `List<double>` | `n.ToString("0.##########")` |
| `List<DateTime>` | `d.ToString("yyyy-MM-dd")` |
| 其他 | 丟 `ArgumentException` |

### 笛卡兒積展開

`BuildBVs<T>(setA, setB, setC)` → 變數總數 = `|setA| × |setB| × |setC|`。

---

## 7.5 `SetBase` 積木 + 逐維具名宣告（OptSet + OptDim，2026-07 新增，**2026-07-15 定版**）

**檔案**：`Foundation\src\OptimFoundation.Core\SetBase.cs` + `AutoSetsGenerator.cs`

### paved path：`[OptDim<TSet>("Name")]` 逐維具名宣告

**唯一 paved path** = Set 積木 + 光桿 `[OptVar]`/`[OptParam]` + 每維一個 `[OptDim<TSet>("Name")]`。`TSet` = 引用哪顆 Set 積木（決定型別），字串參數 = 這一維在本 Variable/Parameter 裡的角色名（PascalCase，供同一顆 Set 積木在同一個類別上取多個角色，如 `LotA`/`LotB`）：

```csharp
[OptSet<DateTime>] public partial class Set_Date { }      // → : SetBase<DateTime>
[OptSet<string>] public partial class Set_Employee { }    // 元素型別一律顯式寫出

[OptParam]
[OptDim<Set_Date>("Date")]
[OptDim<Set_Employee>("Employee")]
public partial class Parameter_ShiftDemand { }
// → ParameterBase + Date(DateTime) + Employee(string) + QTY + 兩個 ctor

[OptVar]
[OptDim<Set_Date>("Date")]
[OptDim<Set_Employee>("Employee")]
public partial class VariableB_ShiftAssign { }
// → VariableBase + Date + Employee（型別由前綴 B/X/I 決定）
```

- property 名 = `[OptDim<TSet>("Name")]` 傳入的字串（PascalCase）；型別從 `TSet` 的 `[OptSet<T>]` 自動抓；attribute 順序 = key 組成順序
- NEVER 寫裸 `[OptSet]`（無泛型參數版）—— ALWAYS 顯式 `[OptSet<string>]` —— Why: 兩者 codegen 完全相同，但顯式讓元素型別在宣告處一眼可見。裸版仍受支援（逃生口 / 舊 code），只是不寫新的
- 引用非積木 → CS0311（`where T : ISetBrick`）；元素型別非法 → OPTF004；缺 `[OptSet<T>]` → OPTF005
- 合法元素型別：`string / DateTime / int / long / double / decimal`
- 光桿 `[OptVar]`/`[OptParam]` 不加任何 `[OptDim]` = 0 維純量，key = 類名、無 `@` 索引

### 逃生口（仍受支援，NEVER 標成「已淘汰／已移除／錯誤」——generator 持續產碼，只是新專案不首選）

```csharp
// 多參數泛型（arity 1..6）：維度名固定 = 積木類名去 Set_ 前綴，無法像 [OptDim] 一樣同一顆 Set 取多個角色名
[OptParam<Set_Date, Set_Employee>] public partial class Parameter_ShiftDemand2 { }
[OptVar<Set_Date, Set_Employee>]   public partial class VariableB_ShiftAssign2 { }

// 字串式（遷移用）：永久保留
[OptParam("Date:DateTime", "Group")]
```

兩者**合法、可編譯、可用於簡單情境**——差別只在「同一顆 Set 積木要在同一個類別取多個角色名」這種需求做不到（固定綁積木類名）。治理文件與新專案一律優先教 `[OptDim<TSet>("Name")]`，但既有程式碼用泛型式/字串式**不算錯誤，不需要遷移**。

### `SetBase<T>` API

```csharp
public abstract class SetBase<T> : ISetBrick, IEnumerable<T>
{
    public string SetName { get; }              // 去 Set_ 前綴的積木名
    public int Count { get; }                   // 未載入 → InvalidOperationException
    public bool Contains(T item);
    public void LoadInline(params T[] items);
    public void LoadFrom(IEnumerable<T> items);
    public void LoadCsv(string fileName);       // 依 T dispatch CsvCtrl.Read*Set
    public void LoadDb(DbDataSource source);    // set 名取自類名
}
```

四道防呆（全丟例外）：未載入即用 / 載入後為空 / 二次載入 / 重複成員。`SetBase<T>` 實作 `IEnumerable<T>`，可直接傳入 `BuildBVs/BuildIVs/BuildCVs(params object[])`。

---

## 7.6 `DataContext` / `OptData` — 資料防護層（2026-07-18 新增）

**檔案**：`OptimFoundation.Core/DataContext.cs`、`OptData.cs`、`DataValidator.cs`、`Numeric.cs`（權威範本：`../OptimFoundation/OptimFoundation/Templates/Tutorial/Data/Dataload.cs`）

### `Dataload` 唯一建構路徑

```csharp
public partial class Dataload : DataContext   // partial + 繼承缺一不可，否則 generator 註冊碼不會產生
{
    public Set_Product PRODUCT = new();
    public List<Parameter_Demand> parameter_Demand = new();

    public Dataload() : this(new CsvDataSource()) { }   // 預設來源 = CSV
    public Dataload(IDataSource source)
    {
        // 每行一句、顯式讀檔——這幾行 MUST 不變
        PRODUCT.Load(source, "Set_Product");
        parameter_Demand = source.LoadParam<Parameter_Demand>("Parameter_Demand");
    }
}

// 唯一建構入口：new + generator 註冊 + 聚合驗證一次到位
var dataload = OptData.Load(() => new Dataload());
```

```csharp
public static class OptData
{
    public static T Load<T>(Func<T> factory) where T : DataContext;   // 唯一多載——無 IDataSource 多載
}
```

- `OptData.Load<T>(Func<T> factory)` 是**唯一多載**；`OptData.Load<T>(IDataSource)` **不存在**——多來源／自訂 ctor 照樣支援，factory 內部想怎麼 `new Dataload(...)` 都可以，只是要包一層 lambda
- 裸 `new Dataload()` / `new Dataload(source)` 仍可編譯，**但跳過驗證**——NEVER 在文件或範例把它當成建構終點
- **`Set_*`/`Parameter_*` 忘記掛 `[OptSet<T>]`/`[OptParam]`**：一旦被 `Dataload : DataContext` 的欄位引用 → **compile error `OPTF006`**（否則會靜默不註冊、永遠不受驗證）

### 建構時自動驗證（`DataValidationException`）

```csharp
public sealed class DataValidationException : Exception
{
    public IReadOnlyList<DataIssue> Issues { get; }   // 一次列出全部問題，不是遇到第一個就停
}
public enum DataIssueKind { MissingSet, TypeMismatch, Dangling, DuplicateKey, MissingCell, Numeric }
```

四類檢查（`OptData.Load` 建構時聚合跑）：**參照完整性**（parameter 值不在對應 Set 內 → `Dangling`；型別不符 → `TypeMismatch`；set 名打錯 → `MissingSet`）、**index key 唯一性**（`DuplicateKey`）、**`[FullGrid]` 完整性**（見下）、**數值 sanity**（`double`/`QTY` 欄為 `NaN`/`±Infinity`/超過量級門檻 **1e15** → `Numeric`）。專案端**永遠不用手寫**這類檢查（如舊式 `ValidateSetsCoverParameters()`）——邏輯集中框架、零複製。

### `[FullGrid]` — opt-in 完整性檢查

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class FullGridAttribute : Attribute { }
```

只標在語意上必須全格覆蓋的 parameter（例：每機每日每班都要有產能值）；**預設 NEVER 加**，MILP 資料多半稀疏、稀疏且未標的 parameter 不會被誤報：

```csharp
[FullGrid]
[OptParam]
[OptDim<Set_Machine>("Machine")]
[OptDim<Set_Date>("Date")]
public partial class Parameter_Capacity { }   // 缺任一 (Machine,Date) 組合 → 建構時報 MissingCell
```

### `Numeric.SafeRatio` — 由數據推導的比值防呆

```csharp
public static double SafeRatio(double numerator, double denominator,
    double magnitudeCeiling = 1e9, string context = null);
// den==0 / 結果非有限（NaN/Infinity）/ |結果|>magnitudeCeiling → throw 明確例外（含 context）；否則回傳比值
```

```csharp
// BigM 由數據推導，NEVER 手寫裸除法
public double BigM => Numeric.SafeRatio(
    parameter_Capacity.Max(c => c.QTY),
    parameter_MachineHours.Where(h => h.QTY > 0).Min(h => h.QTY),
    context: "BigM");
```

> `SafeRatio` 的量級門檻（預設 1e9）與 `DataValidator` 數值 sanity 的門檻（1e15）刻意不同——前者是「衍生值」的較嚴防呆，後者是「原始資料」的寬鬆防呆，勿混用同一門檻。

---

## 8. `CsvCtrl` — CSV I/O 工具

**檔案**：`Foundation\src\OptimFoundation.Core\Csv\CsvCtrl.cs`

```csharp
public static class CsvCtrl
{
    // 讀 Set（從 FolderDir.Data 讀，一行一筆）
    public static List<int>      ReadIntSet(string fileName);
    public static List<double>   ReadDoubleSet(string fileName);
    public static List<string>   ReadStrSet(string fileName);
    public static List<DateTime> ReadDateSet(string fileName);

    // 讀 Parameter
    public static Dictionary<string, double> ReadParameter(string fileName);  // key = "@p1@p2"
    public static List<T>                    BuildParameter<T>(string fileName = null);

    // 矩陣
    public static double[,] ReadMatrixCsv(string fileName);

    // 寫
    public static void CreateParamTable<T>();
    public static void ClearData(string fileName);

    // ★ 輸出解答 → Solution/{TypeName}.csv
    public static void WriteSolution<T>(ISolverEngine engine, string dataId, string userId);
}
```

### `WriteSolution` 細節（CsvCtrl.cs:91-110）

**輸出格式**：

```text
DATA_ID,VAR_TYPE,{Set1},{Set2},...,QTY,USER
{dataId},VariableX_Sandwich,Regular,...,20.0,{userId}
```

**目錄**：`{ProjectPath}/Solution/{TypeName}.csv`，**會自動 `TryCreateFile`** 但**不會自動 `CreateFolder`**。

> ★ **必須先呼叫 `FolderDir.Solution.CreateFolder()`**，否則 `StreamWriter` 拋 `DirectoryNotFoundException`。

**正確用法**：

```csharp
FolderDir.Solution.CreateFolder();
CsvCtrl.WriteSolution<VariableX_Sandwich>(engine, "SandwichProduction", "USER");
```

### `ISolutionSink` — 多變數型別批次輸出 / transaction（2026-07-18 新增）

**檔案**：`OptimFoundation.Core/IO/IDataSource.cs`（介面）、`CsvSolutionSink`、`OracleSolutionSink`

```csharp
public interface ISolutionSink
{
    void WriteSolution<TVariableClass>(ISolverEngine engine, string dataId = null, string userId = null);
    ISolutionBatch BeginBatch(string dataId = null, string userId = null);   // 多型別原子寫入
}
public interface ISolutionBatch : IDisposable
{
    void Write<TVariableClass>(ISolverEngine engine);
    void Commit();   // 未 Commit 即 Dispose = rollback
}
```

- `CsvSolutionSink.BeginBatch`：no-op batch（逐檔寫，行為與直接呼叫 `WriteSolution` 逐字相同）
- `OracleSolutionSink.BeginBatch`：真 transaction——單一 `IDbCtrl.ExecuteInTransaction` 內用 `IDbCtrl.ExecuteBatch`（array-bind，非逐列 `Execute`）寫多個變數型別；任一步失敗全 rollback，DB 無殘留

```csharp
using var batch = sink.BeginBatch(dataId: "V1", userId: "USER");
batch.Write<VariableB_Assign>(engine);
batch.Write<VariableX_Makespan>(engine);
batch.Commit();   // 忘記呼叫 = 整批 rollback
```

---

## 9. `FolderDir` — 專案目錄結構

**檔案**：`Foundation\src\OptimFoundation.Core\Infrastructure\FolderDir.cs`

```csharp
public class FolderDir
{
    public static ProjFolder Data = new ProjFolder("Data"); // 輸入 CSV
    public static ProjFolder Solution = new ProjFolder("Solution"); // 解 CSV 輸出
    public static ProjFolder Log = new ProjFolder("Logs"); // 日誌
    public static ProjFolder Model = new ProjFolder("Models"); // LP/MPS
    public static ProjFolder IIS = new ProjFolder("IISs"); // 不可行衝突
    public static ProjFolder Sol = new ProjFolder("Sols"); // CPLEX .sol
    public static ProjFolder Experiment = new ProjFolder("Experiments"); // Experiment CSV/JSON

    public class ProjFolder
    {
        public static string ProjectPath { get; }   // AppDomain.BaseDirectory（即 bin/Debug/net8.0）
        public ProjFolder(string folderName);

        public void   CreateFolder();                              // 不存在才建
        public string GetPath();                                   // {ProjectPath}/{folderName}
        public string GetFilePath(string fileName);
        public bool   TryCreateFile(string fileName);              // 不存在才建空檔
    }

    public static void TryCreateFolder(string path);
}
```

> ★ **天條**：`new ProjFolder(...)` 建構子**不會**自動 `CreateFolder()`（IL 確認 16 bytes — 只 base ctor + 設欄位 + ret）。任何寫檔前必須手動 `xxx.CreateFolder()`。

| 操作 | 必須先 CreateFolder? |
|---|---|
| `CsvCtrl.WriteSolution` | ✅ `FolderDir.Solution.CreateFolder()` |
| `Logging.Info` 等 | ❌（Logging 內部 `Directory.CreateDirectory`） |
| `Solve()` 寫 LP/MPS/Sol | ❌（`Configuration()` 內已呼叫） |

### 路徑根目錄

`ProjectPath = AppDomain.CurrentDomain.BaseDirectory` = **執行檔目錄**（通常是 `bin/Debug/net8.0/`），**不是專案根**。所以 `Solution/` 會在 `bin/Debug/net8.0/Solution/`。

---

## 10. `Logging` — 日誌工具

**檔案**：`Foundation\src\OptimFoundation.Core\Logging\Logging.cs`

```csharp
public static class Logging
{
    public static void Info(string message);
    public static void Debug(string message);
    public static void Warn(string message);
    public static void Error(string message);
    public static void Info(string message, Stopwatch sw);   // 印完自動 sw.Restart()
    public static void SetLogFileName(string name);          // 之後寫到 Logs/{name}_{timestamp}.txt
    public static void ClearLogs();
}
```

**輸出格式**：

```text
2026-05-26 23:33:51.4433 | INFO  | [Namespace.Of.Caller] message
```

**位置**：`Logs/Log_{startupTimestamp}.txt`（若呼叫 `SetLogFileName("Foo")` 則為 `Foo_{timestamp}.txt`）。

**典型使用**：在 `MyProblem` 建構子呼叫 `Logging.SetLogFileName(GetType().Name);`

---

## 11. `Enums` 與 `ISolverEngine` 介面

**檔案**：`Foundation\src\OptimFoundation.Core\Enums.cs`、`ISolverEngine.cs`

```csharp
public enum VarType         { Continuous, Integer, Binary }
public enum ConstraintSense { LessEqual, Equal, GreaterEqual }
public enum ObjectiveSense  { Minimize, Maximize }

public enum SolveStatus { NotSolved, Optimal, Feasible, Infeasible, Unbounded, TimeLimit, Error }

public interface ISolverEngine : IDisposable
{
    ISolverConfig Config { get; }
    SolveStatus   Status { get; }
    SolveMetrics  LastMetrics { get; }   // 最近一次 Solve() 的統一 telemetry（未求解 null）
    void   Build();
    bool   Solve();
    double GetObjectiveValue();
    double GetVariableValue(string name);
    IReadOnlyDictionary<string, double> GetSolution(string varTypeName = null);
}

// 跨引擎抽象 tuning 旋鈕（CplexConfig 已實作，見第 4 節）
public interface ITunableConfig
{
    int? Seed; int? Emphasis; double? FeasibilityTol; double? OptimalityTol;
    int? RootAlgorithm; int? Presolve; double? HeuristicEffort; double? MemoryLimitMb;
}

// 收斂軌跡來源（CPLEX 支援；EngineBase 預設不支援）
public interface ITrajectorySource
{
    bool SupportsTrajectory { get; }
    void EnableTrajectory();
    IReadOnlyList<ConvergencePoint> Trajectory { get; }
}
```

`Status` 取值順序（`OptEngine.Solve()`）：

```text
CPLEX.Status.Optimal             → SolveStatus.Optimal
CPLEX.Status.Feasible            → SolveStatus.Feasible
CPLEX.Status.Infeasible          → SolveStatus.Infeasible
CPLEX.Status.InfeasibleOrUnbounded → SolveStatus.Infeasible
CPLEX.Status.Unbounded           → SolveStatus.Unbounded
其他                              → SolveStatus.Error
```

`Solve()` 回傳 `true` 的條件：`Status == Optimal || Status == Feasible`。

---

## 12. 標準執行流程

### 12.1 目錄結構（必須）

```text
Projects/MyProject/
├── MyProject.csproj
├── Program.cs
├── MyProblem.cs              ← 主類別（含數學模型注解）
├── Set/Dataload.cs           ← Sets + Parameters + Penalty + WriteToCSV
├── Parameter/Parameter_Xxx.cs
├── Variable/
│   ├── VariableB_Xxx.cs
│   ├── VariableX_Xxx.cs
│   └── VariableCreate.cs
├── Objective/ObjectiveFunction.cs
└── Constraint/
    ├── BuildModel.cs
    └── Constraint_Xxx.cs
```

### 12.2 csproj DLL 參考（天條）

```xml
<ItemGroup>
  <Reference Include="ILOG.Concert"><HintPath>..\..\dlls\ILOG.Concert.dll</HintPath></Reference>
  <Reference Include="ILOG.CPLEX"><HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath></Reference>
  <Reference Include="NLog"><HintPath>..\..\dlls\NLog.dll</HintPath></Reference>
  <Reference Include="OptimFoundation.Core"><HintPath>..\..\dlls\OptimFoundation.Core.dll</HintPath></Reference>
  <Reference Include="OptimFoundation.Cplex"><HintPath>..\..\dlls\OptimFoundation.Cplex.dll</HintPath></Reference>
</ItemGroup>
```

### 12.3 執行序列

```csharp
// Program.cs
using (var problem = new MyProblem())
    problem.Execute();

// MyProblem.Execute()
optEngine = new OptEngine(config);
optEngine.Build();                                       // 1. 初始化 CPLEX
new VariableCreate(dataload, optEngine).Build();         // 2. 建變數（BuildBVs/BuildCVs）
new BuildModel(dataload, optEngine).Build();             // 3. 建模型（目標 + 限制）
bool ok = optEngine.Solve();                             // 4. 求解
if (ok) dataload.WriteToCSV(optEngine);                  // 5. 輸出 CSV
```

---

## 13. 常見模式與反模式

### 13.1 限制式撰寫範式

**範式 A：簡單上限**（`Σ x[i] ≤ Capacity`）

```csharp
foreach (var i in dataload.Items)
    optEngine.AddLHS(1, new VariableX_Production { Item = i });
optEngine.AddRHS(dataload.Capacity);
optEngine.CreateLessEqual($"{ConstraintName}");
ConstraintCount++;
```

**範式 B：等式需求**（`Σ_i x[i,j] = Demand[j]`）

```csharp
foreach (var j in dataload.Days)
{
    foreach (var i in dataload.Employees)
        optEngine.AddLHS(1, new VariableB_Assign { Employee = i, Day = j });
    optEngine.AddRHS(dataload.Demand[j]);
    optEngine.CreateEqual($"{ConstraintName}@{j:yyyy_MM_dd}");
    ConstraintCount++;
}
```

**範式 C：含 RHS 變數（自動移項）**（`x[d] ≥ x[d-1] + x[d] - 1`）

```csharp
optEngine.AddLHS(1, new VariableB_NightToDay { Date = d, Employee = e });
optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = prevD, Employee = e, Group = "N" });
optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = "D" });
optEngine.AddRHS(-1);
optEngine.CreateGreatEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
ConstraintCount++;
```

### 13.2 目標式（penalty 法）

```csharp
foreach (var e in dataload.Employees)
{
    optEngine.AddLHS(dataload.Penalty_BelowAVG, new VariableX_BelowAVG { Employee = e });
    optEngine.AddLHS(dataload.Penalty_OffOneDay, new VariableB_Off1Day { Employee = e });
}
optEngine.CreateMinimize();
```

### 13.3 反模式

**❌ 用 try/catch 吞例外**：

```csharp
// 反模式（Foundation Template 留下的舊風格，請勿沿用）
try { ... }
catch (Exception) { throw; }
```

**❌ 在 Variable / Parameter 寫建構子**：

```csharp
// 反模式 — VariableBuilder 已自動處理，多寫只會干擾
public class VariableX_Foo : VariableBase
{
    public VariableX_Foo(params object[] s) : base(s) { }  // ← 不需要
}
```

**❌ 在 csproj 用錯路徑**：

```xml
<!-- 反模式 — Projects 層級必須 ..\..\ -->
<HintPath>..\dlls\OptimFoundation.Core.dll</HintPath>
```

**❌ 忘記 CreateFolder**：

```csharp
// 反模式 — 會在執行時拋 DirectoryNotFoundException
CsvCtrl.WriteSolution<VariableX_Foo>(engine, "X", "USER");

// 正確
FolderDir.Solution.CreateFolder();
CsvCtrl.WriteSolution<VariableX_Foo>(engine, "X", "USER");
```

**❌ String 屬性沒初始化**：

```csharp
// 反模式 — CS8618
public string Employee { get; set; }

// 正確
public string Employee { get; set; } = string.Empty;
```

---

## 14. 完整最小範例

**問題**：玻璃工廠 LP。決策：`x[Regular], x[Tempered] ≥ 0`，最大化 `8·x[R] + 10·x[T]`，限制 `3·x[R] + 5·x[T] ≤ 300`、`5·x[R] + 8·x[T] ≤ 300`。

> 本節示範**現行建構路徑**：`DataContext` + `OptData.Load`（§7.6）+ `OptModel` fluent；Parameter/Variable 用 §7.5 的 paved path `[OptDim<TSet>]`。與 §7.6 一致，無矛盾。

### `GlassFactory.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="ILOG.Concert"><HintPath>..\..\dlls\ILOG.Concert.dll</HintPath></Reference>
    <Reference Include="ILOG.CPLEX"><HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath></Reference>
    <Reference Include="NLog"><HintPath>..\..\dlls\NLog.dll</HintPath></Reference>
    <Reference Include="OptimFoundation.Core"><HintPath>..\..\dlls\OptimFoundation.Core.dll</HintPath></Reference>
    <Reference Include="OptimFoundation.Cplex"><HintPath>..\..\dlls\OptimFoundation.Cplex.dll</HintPath></Reference>
    <Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />
  </ItemGroup>
</Project>
```

### `Set/Set_GlassType.cs`

```csharp
using OptimFoundation.Modeling;

namespace GlassFactory.Set
{
    [OptSet<string>] public partial class Set_GlassType { }   // 元素型別一律顯式寫出
}
```

### `Parameter/Parameter_HeatingTime.cs`（`Parameter_CoolingTime`/`Parameter_Profit` 同構，省略）

```csharp
using OptimFoundation.Modeling;
using GlassFactory.Set;

namespace GlassFactory.Parameter
{
    [OptParam]
    [OptDim<Set_GlassType>("GlassType")]
    public partial class Parameter_HeatingTime { }
}
```

### `Set/GlassDataload.cs`

```csharp
using OptimFoundation.Core;
using OptimFoundation.Core.IO;
using OptimFoundation.Cplex;
using GlassFactory.Parameter;
using GlassFactory.Variable;

namespace GlassFactory.Set
{
    // 資料層唯一入口：ctor 就是「寫讀檔的家」，每行顯式讀檔
    public partial class GlassDataload : DataContext
    {
        public Set_GlassType GLASSTYPE = new();

        public List<Parameter_HeatingTime> parameter_HeatingTime = new();
        public List<Parameter_CoolingTime> parameter_CoolingTime = new();
        public List<Parameter_Profit> parameter_Profit = new();

        public double HeatingCapacity = 300;
        public double CoolingCapacity = 300;

        public GlassDataload() : this(new CsvDataSource()) { }

        public GlassDataload(IDataSource source)
        {
            GLASSTYPE.Load(source, "Set_GlassType");
            parameter_HeatingTime = source.LoadParam<Parameter_HeatingTime>("Parameter_HeatingTime");
            parameter_CoolingTime = source.LoadParam<Parameter_CoolingTime>("Parameter_CoolingTime");
            parameter_Profit = source.LoadParam<Parameter_Profit>("Parameter_Profit");
        }

        public void WriteSolution(OptEngine engine)
        {
            var produce = engine.GetSetVarValues<VariableX_Production>();
            foreach (var kvp in produce)
                Logging.Info($"  {kvp.Key.Split('@').Last()}: {kvp.Value:F1}");
            Logging.Info($"  Profit = ${engine.GetObjectiveValue():F2}");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.WriteSolution<VariableX_Production>(engine, "GlassFactory", "USER");
        }
    }
}
```

### `Variable/VariableX_Production.cs`

```csharp
using OptimFoundation.Modeling;
using GlassFactory.Set;

namespace GlassFactory.Variable
{
    [OptVar]
    [OptDim<Set_GlassType>("GlassType")]
    public partial class VariableX_Production { }
}
```

### `Model/GlassFactoryModel.cs`（模型積木：組裝順序，plug 進 `OptModel`）

```csharp
using GlassFactory.Set;
using GlassFactory.Variable;
using GlassFactory.Objective;
using GlassFactory.Constraint;
using OptimFoundation.Cplex;

namespace GlassFactory.Model
{
    public class GlassFactoryModel
    {
        private readonly GlassDataload _d;
        public GlassFactoryModel(GlassDataload data) => _d = data;

        public void CreateVariables(OptEngine engine)
            => engine.BuildVars<VariableX_Production>(_d.GLASSTYPE);

        public void CreateModel(OptEngine engine)
        {
            new ObjectiveFunction(_d, engine).Build();
            new Constraint_Heating(_d, engine).Build();
            new Constraint_Cooling(_d, engine).Build();
        }

        // 一次組全（experiment / 手動建 engine 用）
        public void Build(OptEngine engine)
        {
            CreateVariables(engine);
            CreateModel(engine);
        }
    }
}
```

### `Objective/ObjectiveFunction.cs`

```csharp
using GlassFactory.Set;
using GlassFactory.Variable;
using OptimFoundation.Cplex;

namespace GlassFactory.Objective
{
    public class ObjectiveFunction
    {
        private readonly GlassDataload _d;
        private readonly OptEngine _engine;
        public ObjectiveFunction(GlassDataload d, OptEngine e) { _d = d; _engine = e; }

        public void Build()
        {
            foreach (var p in _d.parameter_Profit)
                _engine.AddLHS(p.QTY, new VariableX_Production { GlassType = p.GlassType });
            _engine.CreateMaximize();
        }
    }
}
```

### `Constraint/Constraint_Heating.cs`

```csharp
using GlassFactory.Set;
using GlassFactory.Variable;
using OptimFoundation.Cplex;

namespace GlassFactory.Constraint
{
    public class Constraint_Heating
    {
        private const string Name = "C1_Heating";
        private readonly GlassDataload _d;
        private readonly OptEngine _engine;
        public Constraint_Heating(GlassDataload d, OptEngine e) { _d = d; _engine = e; }

        public void Build()
        {
            foreach (var h in _d.parameter_HeatingTime)
                _engine.AddLHS(h.QTY, new VariableX_Production { GlassType = h.GlassType });
            _engine.AddRHS(_d.HeatingCapacity);
            _engine.CreateLessEqual(Name);
        }
    }
}
```

> `Constraint_Cooling`（`parameter_CoolingTime` / `CoolingCapacity`）同構，省略。

### `Program.cs`

```csharp
using GlassFactory.Set;
using GlassFactory.Model;
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace GlassFactory
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var dataload = OptData.Load(() => new GlassDataload());   // 唯一建構入口：註冊 + 聚合驗證
            var def = new GlassFactoryModel(dataload);

            using var model = new OptModel("GlassFactory")
                .UseConfig(() => new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4, enableLog = true, exportSol = true, exportLP = true })
                .AddVariables(def.CreateVariables)
                .AddModel(def.CreateModel)
                .OnSolved(engine => dataload.WriteSolution(engine));

            bool ok = model.Execute();
            Logging.Info($"[GlassFactory] success={ok}, status={model.optEngine.Status}");
        }
    }
}
```

---

## 15. API 黑名單（不存在的方法）

**呼叫以下方法會編譯失敗或執行時錯誤，AI 生成程式碼絕對不可使用：**

| ❌ 錯誤呼叫 | ✅ 正確替代 |
|---|---|
| `engine.GetVarSol(name)` | `engine.GetVariableValue(name)` |
| `engine.GetSetVarSol<T>()` | `engine.GetSetVarValues<T>()` |
| `CSVCtrl.SaveToCSV<T>(engine.GetSetVarSol<T>(), DATA_ID:, USER_ID:)` | `CsvCtrl.WriteSolution<T>(engine, dataId, userId)` |
| `CSVCtrl.xxx` （大寫 V） | `CsvCtrl.xxx` |
| `FolderDir.Result` | `FolderDir.Solution` |
| `new OptEngineConfig { ... }` | `new CplexConfig { ... }` |
| `optEngine.AddPool / AddPoolRHS` | `optEngine.AddLHS / AddRHS` |
| `optEngine.addPool(...)` | `optEngine.AddLHS(...)` |
| 假設 `ProjFolder` 建構子會 `CreateFolder` | **必須**手動 `FolderDir.Solution.CreateFolder()` |
| 假設變數名分隔符是 `|` | 是 `@`（`ModelElementBase.ToString()`） |
| `BuildBVs(typeof(T), sets)` | `BuildBVs<T>(sets)` — 用泛型 |

### 不可動的檔案

```text
OptimFoundation 框架本體（dlls/ 內編譯版 + sibling ../OptimFoundation/ 原始碼）  ← 唯讀，禁止修改
```

任何「Foundation 缺方法」的需求 → 在 **Project 端寫 helper / extension** 解決，**不可改 Foundation**。

---

## 16. Experiment 套件 — Tuning 實驗記錄

> **用途**：套件化的「單次求解記錄器」。套進任何求解一次的 project，跑完自動抓**這一 run 的完整設定 + 收斂數據**，
> 累積成 `Experiment` 後輸出 `Experiments/<name>.csv` + `.json`。CSV 給人做 tuning 對照，JSON 給後續 LLM tuning 參考。
> Phase 3 tuning 用它系統化比較不同 solver 設定（取代人工翻 log）。

**檔案**：`Foundation\src\OptimFoundation.Core\Experiments\`、`ITunableConfig.cs`、`ITrajectorySource.cs`

### 16.1 核心型別

```csharp
// 單次求解的統一 telemetry（EngineBase.LastMetrics）
public sealed class SolveMetrics
{
    public SolveStatus Status;          public double ObjectiveValue;
    public double BestBound;            public double MipGap;
    public double WallTimeMs;           public long?  NodeCount;       // 取不到為 null
    public long?  IterationCount;       public int    VarCount;
    public int    ConstraintCount;
    public List<ConvergencePoint> Convergence;   // 收斂軌跡（CPLEX，選用）
}

public sealed class ConvergencePoint { public double TimeMs, Objective, Bound, Gap; }

// 設定快照：抽象旋鈕 + reflection 抓的 concrete 專屬欄位（不漏設定）
public sealed class ConfigSnapshot
{
    public string Solver;                              // "Cplex"
    public Dictionary<string, object> Tunable;         // 抽象旋鈕值
    public Dictionary<string, object> SolverSpecific;  // concrete 欄位
    public static ConfigSnapshot From(ISolverConfig config);
}

public sealed class Trial
{
    public string Label; public DateTime RunAt;
    public ConfigSnapshot Config; public SolveMetrics Metrics; public string Note;

    // 套件化單次擷取：讀 engine.Config → 跑 solveAction（一次求解）→ 讀 engine.LastMetrics。
    // 不接管也不 Dispose engine；非 Optimal（TimeLimit/Feasible）仍照記錄。
    public static Trial Capture(ISolverEngine engine, string label,
                                Func<bool> solveAction, string note = null);
}

public class Experiment
{
    public Experiment(string name, string description);
    public List<Trial> Trials;
    public void AddTrial(Trial trial);
    public void Save();                        // → Experiments/<Name>.csv + .json（跨 run append）
    public static Experiment Load(string name);
}
```

### 16.2 標準用法（tuning 掃描）

> 資料建構走 §7.6 的 `OptData.Load` 唯一入口（各 Trial 共用同一份 dataload）；模型組裝沿用 §14 的 `GlassFactoryModel`（`CreateVariables`/`CreateModel`/`Build`），與 §7.6、§14 一致。

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using GlassFactory.Set;
using GlassFactory.Model;

var dataload = OptData.Load(() => new GlassDataload());
var exp = new Experiment("glass-tuning", "比較 mipEmphasis 對求解時間與 gap 的影響");

// 要掃的設定（用 ITunableConfig 抽象旋鈕，跨引擎一致）
foreach (var (label, tune) in new (string, Action<CplexConfig>)[]
{
    ("baseline", _ => { }),
    ("emphasis=feasible", c => c.Emphasis = 1),
    ("emphasis=optimal", c => { c.Emphasis = 2; c.Seed = 7; }),
})
{
    var config = new CplexConfig { epGap = 0.03, timeLimit = 60, enableLog = false };
    tune(config);

    using var engine = new OptEngine(config);   // 每個 Trial 用全新 engine
    engine.Build();
    new GlassFactoryModel(dataload).Build(engine);   // CreateVariables + CreateModel 一次組全

    // 一行擷取：完整設定 + 收斂數據（CPLEX 自動含收斂軌跡）
    var trial = Trial.Capture(engine, label, () => engine.Solve());
    exp.AddTrial(trial);
}

exp.Save();   // → bin/Debug/net8.0/Experiments/glass-tuning.csv + .json
```

> ★ `Trial.Capture` 會自動偵測 `ITrajectorySource`（CPLEX 支援）並啟用軌跡，**使用者無須額外動作**。
> ★ 收斂軌跡用 CPLEX `MIPInfoCallback`，**會關閉 dynamic search、可能影響求解時間**，故為 opt-in（只在 Capture 時啟用）。
> ★ `Experiment.Save()` 跨 run 累積（同名實驗以 RunAt+Label 去重）；net8 專案的 `System.Text.Json` 由 shared framework 提供，dlls/ 無須額外放。

### 16.3 輸出格式

- **CSV**：每個 Trial 一列，欄位 = label + 抽象旋鈕 + 指標（給人 / Excel / pandas）。
- **JSON**：巢狀含 `config`（tunable + solverSpecific）、`metrics`、`convergence[]`，中文直出（給 LLM）。

---

## 附錄：API 速查卡

```csharp
// === 建構 ===
var config = new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4,
                                enableLog = true, exportSol = true, exportLP = true };
var engine = new OptEngine(config);
engine.Build();

// === 建變數 ===
engine.BuildCVs<VarX>(setA);                       // 連續 [0, 1e100]
engine.BuildCVs<VarX>(0, 100, setA);               // 連續 [0, 100]
engine.BuildIVs<VarI>(setA, setB);                 // 整數
engine.BuildBVs<VarB>(setA, setB, setC);           // Binary

// === 建限制式 ===
engine.AddLHS(coef, new VarX { A = a });           // 加 LHS 變數項
engine.AddLHS(constant);                            // 加 LHS 常數
engine.AddRHS(coef, new VarX { A = a });           // 加 RHS 變數項（自動移項）
engine.AddRHS(constant);                            // 加 RHS 常數
engine.CreateLessEqual ($"{name}@{idx}");          // <=
engine.CreateGreatEqual($"{name}@{idx}");          // >=
engine.CreateEqual     ($"{name}@{idx}");          // ==
engine.CreateRange(lb, ub, $"{name}@{idx}");       // lb <= expr <= ub
engine.CreateLeSoft(rhs, penalty);                 // soft <=
engine.CreateGeSoft(rhs, penalty);                 // soft >=
engine.CreateEqSoft(rhs, penalty, name);           // soft ==

// === 目標式 ===
engine.AddLHS(coef, new VarX { ... });
engine.CreateMinimize();   // 或 CreateMaximize()

// === 求解 ===
bool ok = engine.Solve();

// === 取解 ===
double obj = engine.GetObjectiveValue();
double v = engine.GetVariableValue("VarX@a@b");
var dict = engine.GetSetVarValues<VarX>(); // Dictionary<string,double>
var dict2 = engine.GetSolution("VarX"); // IReadOnlyDictionary
string[] names = engine.GetSetVarNames<VarX>();

// === 輸出 CSV ===
FolderDir.Solution.CreateFolder();                              // ★ 必須
CsvCtrl.WriteSolution<VarX>(engine, "DataId", "User");

// === 日誌 ===
Logging.Info("...");                                            // INFO 級別
Logging.SetLogFileName("ProjectName");                          // 改 log 檔名

// === 釋放 ===
engine.Dispose();

// === Tuning 實驗記錄（第 16 節）===
var exp = new Experiment("my-tuning", "說明");
var trial = Trial.Capture(engine, "label", () => engine.Solve());  // 抓設定+收斂數據，不 Dispose engine
exp.AddTrial(trial);
exp.Save();                                        // → Experiments/my-tuning.csv + .json
var m = engine.LastMetrics;                         // wall time / gap / bound / node·iter / 軌跡
```
