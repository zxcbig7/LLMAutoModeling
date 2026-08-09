# OptimFoundation API — Phase 2 開發規範

> 同步日期：2026-08-09
>
> 適用版本：目前 `OptimFoundation.Core`、`OptimFoundation.Generators` 與 `OptimFoundation.Cplex` source。Gurobi 不在本階段開發範圍。
> 本文件是 Phase 2 的 API 與寫法權威；若範例專案與本文衝突，以 source 與本文為準。

## §0 心智模型

Phase 2 的任務是把已確認的 `Model/<Project>_Model.md` 機械式轉成程式。模型未確認、符號未宣告、限制式方向不明或資料形狀未定時，先回到 Phase 1 補齊，不在 coding 階段自行猜測。

資料到求解的單向流程如下：

```text
Data/*.csv 或 DB query
  → IDataSource.Load<T>()
  → Dataload : DataContext
  → OptData.Load(...)
  → Engine.BuildVars<T>()
  → Objective / Constraint
  → OptModel
  → OptProject.Execute()
  → ISolutionSink / GetSolution
```

核心原則：

- `Set_*` 與 `Parameter_*` 都是「一列資料」的型別，載入後都是 `List<T>`。
- Set 有一到多個維度，沒有數值欄。
- Parameter 有零到多個維度，最後固定有 `double QTY`。
- Set、Parameter 與 Variable 的維度都用相同的 `[OptDim<T>("Name")]` 宣告。
- `OptDim<T>` 的 `T` 是 C# 資料型別，不是另一個模型類別。
- Variable 類別前綴是型別的單一真相：`VariableB_`、`VariableC_`、`VariableI_`。
- 新限制式以 `CreateXxx(this, dims...)` 交給框架統一命名。

## §1 專案結構與命名

Phase 2 模型專案使用以下八個職責資料夾；不要另造平行的資料或模型管線：

```text
<Project>/
├── Model/<Project>_Model.md
├── Set/Set_*.cs
├── Parameter/Parameter_*.cs
├── Variable/Variable[B|C|I]_*.cs
├── Objective/ObjectiveFunction.cs
├── Constraint/Constraint_*.cs
├── Solution/<Project>Solution.cs
├── Data/Dataload.cs
├── Data/*.csv                 # 使用 CsvDataSource 時
├── Program.cs
└── <Project>.csproj
```

命名規則：

| 類別 | 格式 | 範例 |
| --- | --- | --- |
| Set row | `Set_<語意>` | `Set_Product` |
| Parameter row | `Parameter_<語意>` | `Parameter_Demand` |
| Binary Variable | `VariableB_<語意>` | `VariableB_Assign` |
| Continuous Variable | `VariableC_<語意>` | `VariableC_Start` |
| Integer Variable | `VariableI_<語意>` | `VariableI_Batch` |
| Constraint | `Constraint_<語意>` | `Constraint_Capacity` |

generator 類別必須是 `partial`；專案一律使用 attribute 產碼，不手寫 framework base class 或 generator 會建立的 property。

## §2 資料層 — Set / Parameter / Dataload

### 2.0 統一宣告

一維 Set：

```csharp
using OptimFoundation.Modeling;

[OptSet]
[OptDim<string>("Product")]
public sealed partial class Set_Product { }
```

多維 Set：

```csharp
[OptSet]
[OptDim<string>("From")]
[OptDim<string>("To")]
public sealed partial class Set_Arc { }
```

多維 Parameter：

```csharp
[OptParam]
[OptDim<string>("From")]
[OptDim<string>("To")]
public sealed partial class Parameter_ArcCost { }
```

零維 scalar Parameter：

```csharp
[OptParam]
public sealed partial class Parameter_Alpha { }
```

規則：

- Set 至少要有一個 `OptDim`；零維 Set 會產生 compile error `OPTF008`。
- Parameter 可有零到多個 `OptDim`；generator 一律在最後加入 `double QTY`。
- 支援的維度型別是 `string`、`DateTime`、`int`、`long`、`double`、`decimal`；其他型別會產生 `OPTF007`。
- `OptDim` 的名稱同時是生成的 property 名、CSV/DB 欄名與 key 順序。
- Set、Parameter、Variable 之間沒有型別引用；要表示相同語意時，使用相同的維度名與 CLR 型別。

generator 產物的基底如下：

| 宣告 | 生成基底 | 生成內容 |
| --- | --- | --- |
| `[OptSet]` | `SetRowBase` | 依序生成各維 property |
| `[OptParam]` | `ParameterBase` | 各維 property + 最後的 `QTY` |
| `[OptVar]` | `VariableBase` | 依序生成各維 property |

新程式建立資料列一律使用 object initializer，避免維度順序變更後位置式值靜默錯位：

```csharp
new Parameter_ArcCost { From = "A", To = "B", QTY = 12.5 };
```

### 2.1 CSV 形狀

所有模型輸入 CSV 都必須有表頭，表頭依 property 名稱對位，大小寫不敏感。每列欄數必須與表頭一致。

一維 Set：

```csv
Product
Desk
Chair
```

多維 Set：

```csv
From,To
A,B
B,C
```

多維 Parameter：

```csv
From,To,QTY
A,B,12.5
B,C,8
```

scalar Parameter：

```csv
QTY
0.95
```

資料契約：

- Set CSV 欄位恰好對應各 `OptDim`，不含 `QTY`。
- Parameter CSV 欄位對應各 `OptDim`，最後包含 `QTY`。
- scalar Parameter 的 CSV 只有 `QTY`；應由專案邏輯確認恰好一列。
- Template 日期一律使用 `yyyy-MM-dd`；模型內名稱會統一序列化成 `yyyy_MM_dd`。
- 目前 mapper 以 invariant culture 的 `DateTime.Parse` 轉型，因此可能額外接受其他 invariant 日期表示；這只是 parser 容忍度，不是受支援的 Template 契約，新資料與教學不可依賴。
- 數值使用 invariant culture，小數點為 `.`，不加千分位。
- 多餘欄位會被 mapper 忽略；缺少必要 property 欄位或轉型失敗會拋例外並留下 Error Log。

### 2.2 單一讀取 API

`IDataSource` 的模型列讀取入口只有一個：

```csharp
List<T> Load<T>(string sourceName = null)
    where T : ModelElementBase, new();
```

Set 與 Parameter 的差別由生成型別的 property 決定，不由 reader 分流：

```csharp
set_Arc = source.Load<Set_Arc>("network-arcs.csv");
parameter_ArcCost = source.Load<Parameter_ArcCost>("arc-costs-2026.csv");
```

`sourceName` 不必等於 class 名稱。對 `CsvDataSource` 而言，它是執行檔目錄（`AppDomain.BaseDirectory`）下 `Data/` 資料夾內的檔名，可傳有或沒有 `.csv`；只有省略時才以 `typeof(T).Name` 為預設。

完整 `Dataload` 範例：

```csharp
using OptimFoundation.Core;
using OptimFoundation.Core.IO;

public sealed partial class Dataload : DataContext
{
    public List<Set_Arc> set_Arc = new();
    public List<Parameter_ArcCost> parameter_ArcCost = new();

    public Dataload() : this(new CsvDataSource()) { }

    public Dataload(IDataSource source)
    {
        set_Arc = source.Load<Set_Arc>("network-arcs.csv");
        parameter_ArcCost = source.Load<Parameter_ArcCost>("arc-costs-2026.csv");
    }
}

var data = OptData.Load(() => new Dataload());
```

`public Dataload() : this(new CsvDataSource())` 是 constructor chaining：先建立 `CsvDataSource`，再呼叫 `Dataload(IDataSource source)`。同一個類別不能同時宣告兩個簽名完全相同的無參數 constructor；要換來源就顯式傳入另一個 `IDataSource`。

來源行為：

| 來源 | `Load<T>` 第一引數的語意 |
| --- | --- |
| `CsvDataSource` | `Data/{sourceName}`；可省略 `.csv`，省略引數時使用 `typeof(T).Name` |
| `InMemoryDataSource` | 已由 `AddRows` 註冊的表格名稱 |
| `DbDataSource` | 完整 `SELECT` SQL，不猜表名 |

DB 例：

```csharp
IDataSource source = new DbDataSource(configuredDbCtrl);
var arcs = source.Load<Set_Arc>(
    "SELECT from_node AS From, to_node AS To FROM arc");
```

需要 bind parameters 時，使用具體 `DbDataSource` overload：

```csharp
var rows = dbSource.Load<Parameter_ArcCost>(
    "SELECT from_node AS From, to_node AS To, cost AS QTY " +
    "FROM arc_cost WHERE data_id = :id",
    (":id", dataId));
```

### 2.3 記憶體資料與輸出

```csharp
IDataSource source = new InMemoryDataSource()
    .AddRows(new[]
    {
        new Set_Arc { From = "A", To = "B" }
    })
    .AddRows(new[]
    {
        new Parameter_ArcCost { From = "A", To = "B", QTY = 12.5 }
    });
```

Set 與 Parameter 共用同一個 Template CSV 輸出 API：

```csharp
CsvCtrl.WriteRows(set_Arc, "network-arcs.csv");
CsvCtrl.WriteRows(parameter_ArcCost, "arc-costs-2026.csv");
```

`WriteRows<T>` 以 `typeof(T).GetProperties()` 產生表頭，輸出到執行檔目錄下的 `Data/{fileName}`；未提供 `.csv` 時會自動補上，可由 `Load<T>` round-trip 讀回。

### 2.4 DataContext 的實際驗證範圍

`OptData.Load` 會建立 `Dataload`、執行 generator 產生的 Set／Parameter 註冊、驗證後 freeze。現行自動驗證涵蓋：

- Set 維度 key 重複。
- Parameter 維度 key 重複。
- Parameter 的 `double` 數值為 `NaN`、Infinity 或絕對值超過 `1e15`。

Parameter→Set 關聯與完整笛卡兒積是專案資料語意，framework 不做 schema 猜測。Set-driven loop 查找 Parameter 時使用：

```csharp
var row = parameters.FindParameterOrLog(
    p => p.Product == product && p.Date == date,
    product, date);
double qty = row?.QTY ?? 0.0;
```

找不到時只寫 `PARAMETER_NOT_FOUND` Warning 並回傳 null；採 0、跳過或其他預設值由開發者決定。

### 2.5 使用 Set row

一維 Set row 可直接隱式轉成其維度型別：

```csharp
foreach (var product in data.set_Product)
{
    string productName = product;
}
```

多維 Set row 可讀 property 或 deconstruct：

```csharp
foreach (var arc in data.set_Arc)
{
    var from = arc.From;
    var to = arc.To;
}

foreach (var (from, to) in data.set_Arc) { }
```

## §3 變數層

宣告：

```csharp
[OptVar]
[OptDim<string>("From")]
[OptDim<string>("To")]
public sealed partial class VariableB_UseArc { }
```

一般建立入口：

```csharp
engine.BuildVars<VariableB_UseArc>(data.set_Arc);
```

多維 Set row 會展開成多個 key token，因此 `Set_Arc` 的兩欄正好對應 `VariableB_UseArc` 的兩個 `OptDim`。若要完整笛卡兒積，傳入多個一維 Set list：

```csharp
engine.BuildVars<VariableC_Produce>(data.set_Product, data.set_Date, data.set_Shift);
```

`BuildVars<T>` 依類別名前綴委派：

| 前綴 | 型別 | 預設 bounds |
| --- | --- | --- |
| `VariableB_` | Binary | `[0, 1]` |
| `VariableC_` | Continuous | `[0, 1E100]` |
| `VariableI_` | Integer | `[0, 1E100]` |

型別專用 API 仍是公開且有效的底層入口：

```csharp
engine.BuildBVs<VariableB_Assign>(sets);
engine.BuildCVs<VariableC_Amount>(sets);
engine.BuildCVs<VariableC_Amount>(lb, ub, sets);
engine.BuildIVs<VariableI_Count>(sets);
engine.BuildIVs<VariableI_Count>(lb, ub, sets);
```

新專案預設使用 `BuildVars<T>`。只有需要自訂 bounds 或維護明確型別建構程式時才直接使用型別專用 API；正式 B/C/I 前綴與明確 builder 不一致會記錄 `VARIABLE_TYPE_MISMATCH` 後拋例外。

零維 Variable 只掛 `[OptVar]`，以 `BuildVars<T>()` 建立一個 scalar variable。

## §4 模型層 — Objective / Constraint

### 4.0 Objective

```csharp
public sealed class ObjectiveFunction
{
    public void Build(OptEngine engine)
    {
        engine.AddLHS(coefficient, variableSpec);
        engine.CreateMinimize(); // 或 CreateMaximize()
    }
}
```

目標式與限制式共用 pool。每次建立完成後 framework 會清空 pool；不要手動累加 `ConstraintCount`。

### 4.1 Constraint paved path

保留 Model.md 原本的左右側，不自行移項：

```csharp
public sealed class Constraint_Demand : ConstraintBase
{
    private readonly List<Set_Product> products;
    private readonly List<Parameter_Demand> demand;

    public Constraint_Demand(
        List<Set_Product> products,
        List<Parameter_Demand> demand)
    {
        this.products = products;
        this.demand = demand;
    }

    public void Build(OptEngine engine)
    {
        foreach (var product in products)
        {
            engine.AddLHS(1.0, new VariableC_Produce { Product = product });
            double required = demand.FindParameterOrLog(
                row => row.Product == product,
                product)?.QTY ?? 0.0;
            engine.AddRHS(required);
            engine.CreateGreatEqual(this, product);
        }
    }
}
```

新程式使用：

```csharp
engine.CreateEqual(this, dims);
engine.CreateLessEqual(this, dims);
engine.CreateGreatEqual(this, dims);
engine.CreateRange(lb, ub, this, dims);
```

框架會以 owner 類別名和原始維度值組成 `Constraint_Class@dim1@dim2`。變數、參數與限制式的模型名稱都共用 `@` 分隔與 `yyyy_MM_dd` 日期格式；資料 CSV 的日期仍是 `yyyy-MM-dd`。

### 4.2 保留的公開 overload

以下介面仍然有效：

```csharp
CreateEqual(string name)
CreateLessEqual(string name)
CreateGreatEqual(string name)
CreateEqual(double rhs, string name)
CreateLessEqual(double rhs, string name)
CreateGreatEqual(double rhs, string name)
CreateRange(double lb, double ub, string name)
CreateLeSoft(double rhs, double penalty)
CreateLeSoft(double rhs, double penalty, string name)
CreateGeSoft(double rhs, double penalty)
CreateGeSoft(double rhs, double penalty, string name)
CreateEqSoft(double rhs, double penalty, string name)
```

它們是仍受支援的明確字串命名與直接 RHS 介面。`CreateXxx(double rhs, string name)` 會覆蓋已累積的 RHS 常數；新模型的一般路徑仍是 `AddRHS(value)` 後接 owner overload，避免混用造成語意不清。

Soft constraint 也有 owner overload：

```csharp
CreateLeSoft(rhs, penalty, this, dims)
CreateGeSoft(rhs, penalty, this, dims)
CreateEqSoft(rhs, penalty, this, dims)
```

### 4.3 名稱與錯誤

模型名稱 token 不可為空，不可含空白、`@` 或 `+ - * / ^ < > = : , \\`。DateTime 不可含時分秒。框架主動判定錯誤時會在 throw 前留下 Error Log；公開 API 邊界會記錄未預期例外後原樣 rethrow，且同一例外只記一次。

## §5 Program.cs 組裝

```csharp
var data = OptData.Load(() => new Dataload());

var projectConfig = new ProjectConfig
{
    ProjectName = "Example",
    EnableSolverLog = true,
};

var solverConfig = new CplexConfig
{
    MipGap = 1e-4,
    TimeLimit = 120,
    Threads = 8,
};

var model = new OptModel("ExampleModel")
    .AddVariables(e => e.BuildVars<VariableB_UseArc>(data.set_Arc))
    .AddObjective(e => new ObjectiveFunction(/* dependencies */).Build(e))
    .AddConstraints(e => new Constraint_Flow(/* dependencies */).Build(e));

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => solutionSink.WriteSolution<VariableB_UseArc>(engine));

bool solved = project.Execute();
```

每個 Objective/Constraint constructor 只接收實際使用的 Set row list、Parameter row list 與常數，不接收整包 `Dataload`。`Program.cs` 是唯一組裝點。

公開執行狀態只使用 PascalCase API：最近一次 engine 是 `project.Engine`，成功旗標是 `project.IsSuccess`，時間是 `project.TotalElapsed` / `project.BuildModelElapsed`；變數數量是 `engine.VariableCount` / `engine.RegisteredVariableCount`。`CplexConfig` 的公開 property 也全部使用 PascalCase。

## §6 解答讀取與輸出

```csharp
IReadOnlyDictionary<string, double> all = engine.GetSolution();
Dictionary<string, double> oneType = engine.GetSetVarValues<VariableB_UseArc>();

ISolutionSink sink = new CsvSolutionSink();
sink.WriteSolution<VariableB_UseArc>(engine, dataId, userId);
```

多型別輸出可使用 batch：

```csharp
using var batch = sink.BeginBatch(dataId, userId);
batch.Write<VariableB_UseArc>(engine);
batch.Write<VariableC_Load>(engine);
batch.Commit();
```

CSV batch 是立即寫出；DB sink 可實作真正 transaction。未求解或 adapter 回報錯誤時不要把空解當成功結果。

`CsvSolutionSink` 與 `CsvCtrl.WriteSolution` 會自行建立執行檔目錄下的 `Solution/`，呼叫端不需要預先建立資料夾。

## §7 驗收

完成前至少執行：

1. Model.md 的 SET/PARAM/VAR/CONSTRAINT/OBJ 與程式逐項對照。
2. 依資料來源檢查 schema：CSV 核對表頭、欄數、型別與檔名；DB 核對 SQL 欄位／alias 與型別。
3. `dotnet build`。
4. 跑 unit tests；資料與命名 edge case 必須涵蓋 Error Log。
5. 用小型已知答案資料求解，核對可行性、目標值與輸出列數。
6. 搜尋文件與專案是否仍引用不存在的介面或錯誤資料形狀。

### 7.1 `SolveStatus` 七態與 gate 分流

`EngineBase.Status` 是 `SolveStatus` 列舉，**NEVER 只判 `== Optimal`**：

| 值 | 語意 | `Solve()` 回傳 | `ObjectiveValue` / `MipGap` | Phase 2 gate |
| --- | --- | --- | --- | --- |
| `Optimal` | 找到並證明最佳 | `true` | 有值 | 驗證協定照跑 → 過 |
| `Feasible` | 有可行解、未證明最佳（可能撞 `TimeLimit` / `NodeLimit` / `IntegerSolutionLimit`） | `true` | 有值 | 對 incumbent 照跑 → 過；`MipGap` / `BestBound` 記入交付 |
| `TimeLimit` | 中止且**無任何可用解**（CPLEX `Unknown` 映射而來，**名稱不代表只有時間會觸發**） | `false` | `NaN` | 無解可驗 → 換小 instance 求到 `Optimal` 再過 |
| `Infeasible` | 無可行解（含 `InfeasibleOrUnbounded`）；框架自動跑 conflict 分析寫出 `IISs/*.ilp` | `false` | `NaN` | ✗ 退回 Phase 1 / 2 |
| `Unbounded` | 目標式無界 | `false` | `NaN` | ✗ 補界限 constraint |
| `Error` | 求解器或 adapter 執行異常 | `false` | `NaN` | ✗ 修執行面，不得當作無解 |
| `NotSolved` | 尚未完成 `Solve()` | 不適用 | 無有效 metrics | ✗ 不得進入解驗證 |

`Feasible` 是**合法交付狀態**：轉譯忠實與否用 incumbent 就驗得出來，與有沒有證明最佳無關。收斂不足屬效能問題，唯一合法處置是 Phase 3，NEVER 在 Phase 2 放寬 `CplexConfig.MipGap` 或加大 `CplexConfig.TimeLimit` 來「讓它變 Optimal」——那是變更停止契約，會讓 Phase 3 的 before/after 失去基準。

### 7.2 解驗證四步

1. 依 §7.1 對 `engine.Status` 分流；只有 `Optimal` 或 `Feasible` 有 incumbent 可繼續驗證。
2. 把 incumbent 代回 Model.md 的每條限制式，逐條確認 `LHS op RHS`。
3. 核對目標值、關鍵變數的單位與數量級。
4. 核對 `engine.LastMetrics.BestBound`：最大化問題應有 `ObjectiveValue <= BestBound`，最小化問題應有 `ObjectiveValue >= BestBound`（容許 solver 數值誤差）。

## §8 Experiment

實驗必須用同一個 `OptModel` 與可重現資料，只更換 solver config。不要在 tuning 階段偷偷改模型或資料。

```csharp
var result = new OptExperiment("run-id", "比較說明")
    .AddModel(model)
    .AddConfig("baseline", baseline)
    .AddConfig("candidate", candidate)
    .Run();
```

production baseline 與實驗候選組態分開；promotion 需保留 metrics 與判準。

## §9 API 速查

### 資料來源

```csharp
public interface IDataSource
{
    DataTable LoadData(string sourceName);
    List<T> Load<T>(string sourceName = null) where T : ModelElementBase, new();
}

public sealed class CsvDataSource : IDataSource
{
    public DataTable LoadData(string fileName); // AppDomain.BaseDirectory/Data/{fileName}
}

public sealed class DbDataSource : IDataSource
{
    public DbDataSource(IDbCtrl db);
    public DataTable LoadData(string sql);
    public DataTable LoadData(string sql, params (string name, object value)[] parameters);
    public List<T> Load<T>(string sql, params (string name, object value)[] parameters)
        where T : ModelElementBase, new();
}

public static class CsvCtrl
{
    public static void WriteRows<T>(IReadOnlyList<T> rows, string fileName = null)
        where T : ModelElementBase;
    public static void WriteSolution<TVariable>(ISolverEngine engine, string dataId, string userId);
}

public static class ParameterLookupExtensions
{
    public static T FindParameterOrLog<T>(
        this IEnumerable<T> rows,
        Func<T, bool> predicate,
        params object[] keyValues)
        where T : ParameterBase;
}
```

### 變數

```csharp
BuildVars<T>(params object[] sets)
BuildBVs<T>(params object[] sets)
BuildCVs<T>(params object[] sets)
BuildCVs<T>(double lb, double ub, params object[] sets)
BuildIVs<T>(params object[] sets)
BuildIVs<T>(double lb, double ub, params object[] sets)
```

### Pool 與限制式

```csharp
AddLHS(double coefficient, object variable)
AddLHS(double constant)
AddRHS(double coefficient, object variable)
AddRHS(double constant)

CreateEqual(ConstraintBase owner, params object[] dims)
CreateLessEqual(ConstraintBase owner, params object[] dims)
CreateGreatEqual(ConstraintBase owner, params object[] dims)
CreateRange(double lb, double ub, ConstraintBase owner, params object[] dims)

CreateMinimize()
CreateMaximize()
```

### Runner、觀測與常用設定

```csharp
// OptProject
OptEngine Engine { get; }
bool IsSuccess { get; }
TimeSpan TotalElapsed { get; }
TimeSpan BuildModelElapsed { get; }
bool Execute()

// EngineBase
int VariableCount { get; }
int RegisteredVariableCount { get; }
SolveStatus Status { get; }
SolveMetrics LastMetrics { get; }

// CplexConfig 常用公開設定；全部使用 PascalCase
double? TimeLimit { get; set; }
double? MipGap { get; set; }
int? Threads { get; set; }
int? Seed { get; set; }
int? Emphasis { get; set; }
double? FeasibilityTol { get; set; }
double? OptimalityTol { get; set; }
double? MemoryLimitMb { get; set; }
long? NodeLimit { get; set; }
long? IntegerSolutionLimit { get; set; }
```

## §10 常見錯誤

| 症狀 | 原因 | 修正 |
| --- | --- | --- |
| Set 類別出現 `OPTF008` | 沒有宣告維度 | 至少加一個使用支援基礎型別的 `OptDim` |
| CSV 第一筆被當表頭 | 檔案沒有表頭 | 第一列補齊 property 名稱 |
| `Load<T>` 缺欄 | CSV/SQL alias 與 property 不一致 | 改表頭或用 SQL `AS` |
| `VARIABLE_ARITY_MISMATCH` | 傳入維度總寬度與 Variable property 數不同 | 對齊 `OptDim` 順序與 Set row 欄數 |
| `VARIABLE_TYPE_MISMATCH` | 類別前綴與明確 builder 不一致 | 改用 `BuildVars<T>` 或修正前綴/builder |
| `VARIABLE_NOT_FOUND` | 查詢 key、property 順序或建立 domain 不一致 | 對照 Variable 宣告與 `BuildVars` 引數 |
| 限制式名稱重複 | owner+dims 沒包含唯一索引 | 把完整索引傳給 `CreateXxx(this, ...)` |
| RHS 值被替換 | 混用了 RHS 常數 overload | 改為 `AddRHS(value)` + owner overload |
| 日期名稱不一致 | 手工使用 CSV 日期格式組模型名 | 傳原始 DateTime 給框架組名 |

## 附錄 A — 限制式轉譯原則

- Model.md 左側項用 `AddLHS`，右側項用 `AddRHS`。
- `=`、`≤`、`≥` 分別用 `CreateEqual`、`CreateLessEqual`、`CreateGreatEqual`。
- 不在 code 端自行移項、翻號或改方向。
- 資料常數來自 Parameter；式子結構常數才可直接寫 literal。
- 變數 index 與 Set/Parameter 維度名稱、型別、順序逐項核對。
