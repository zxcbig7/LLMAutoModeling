# OptimFoundation CPLEX Project 開發說明書

> 依據 `Templates/Template_CPLEX` 實作，搭配 `OptimFoundation.Core` / `OptimFoundation.Cplex`。

---

## 1. 專案目錄結構

```
ProjectName/
├── ProjectName.csproj
├── Program.cs                          進入點
├── ProblemName.cs                      Execute() 主流程
├── Data/
│   ├── Dataload.cs                     Sets、Parameters、資料初始化
│   └── Parameter_Xxx.cs                一個 Parameter 一個檔案
├── VariablesClass/
│   ├── VariableCreate.cs               BuildBVs / BuildIVs / BuildCVs
│   ├── VariableB_Xxx.cs                Binary 變數
│   ├── VariableI_Xxx.cs                Integer 變數
│   └── VariableX_Xxx.cs                Continuous 變數
└── Constraints/
    ├── BuildModel.cs                   依序呼叫所有 Build()
    ├── ObjectiveFunction.cs            目標式
    └── Constraint_Xxx.cs               各限制式，一個一個檔案
```

---

## 2. csproj 設定

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- OptimFoundation 參考 -->
  <ItemGroup>
    <ProjectReference Include="..\..\OptimFoundation\src\OptimFoundation.Core\OptimFoundation.Core.csproj" />
    <ProjectReference Include="..\..\OptimFoundation\src\OptimFoundation.Cplex\OptimFoundation.Cplex.csproj" />
  </ItemGroup>

  <!-- IBM CPLEX DLL：由 repo 根 dlls/ 提供（相對路徑，層數依專案位置調整）。設置見 dlls/README.md -->
  <ItemGroup>
    <Reference Include="ILOG.Concert">
      <HintPath>..\..\dlls\ILOG.Concert.dll</HintPath>
    </Reference>
    <Reference Include="ILOG.CPLEX">
      <HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath>
    </Reference>
  </ItemGroup>

  <!-- 選用：Oracle 資料來源 -->
  <ItemGroup>
    <PackageReference Include="Oracle.ManagedDataAccess.Core" Version="23.6.1" />
  </ItemGroup>
</Project>
```

---

## 3. Using 宣告

每個 .cs 檔案的標準 using：

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using ProjectNamespace.Data;
using ProjectNamespace.VariableClass;
```

---

## 4. Variable 類別

### 4.1 規則

| 前綴 | 類型 | 對應 BuildXxx |
|------|------|---------------|
| `VariableB_` | Binary（0/1） | `BuildBVs<>()` |
| `VariableI_` | Integer | `BuildIVs<>()` |
| `VariableX_` | Continuous | `BuildCVs<>()` |

- 繼承 `VariableBase`
- **只宣告 properties，不寫任何建構子**
- Property 宣告順序 = 變數 key 中 `@` 的順序
- 使用 object initializer 建立實例

### 4.2 範本

```csharp
// VariableB_ShiftAssign.cs
public class VariableB_ShiftAssign : VariableBase
{
    public DateTime Date     { get; set; }
    public string   Employee { get; set; }
    public string   Group    { get; set; }
}

// VariableX_BelowAVG.cs
public class VariableX_BelowAVG : VariableBase
{
    public string Employee { get; set; }
}
```

### 4.3 使用方式

```csharp
// AddLHS / AddRHS 傳入 object initializer
engine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = g });
engine.AddRHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = "O" });
```

Variable key 格式：`VariableB_ShiftAssign@2026-01-01@E1@D`（DateTime 固定 `yyyy-MM-dd`）

---

## 5. Parameter 類別

### 5.1 規則

- 繼承 `ParameterBase`
- **只宣告 properties，不寫建構子**
- `QTY` 永遠放最後一個 property（數值欄位）
- 其他 property 為 Index / Key 欄位

### 5.2 範本

```csharp
// Parameter_ShiftDemand.cs
public class Parameter_ShiftDemand : ParameterBase
{
    public DateTime Date  { get; set; }
    public string   Group { get; set; }
    public double   QTY   { get; set; }
}

// Parameter_CrossGroup.cs
public class Parameter_CrossGroup : ParameterBase
{
    public string Employee { get; set; }
    public string Group    { get; set; }
    public double QTY      { get; set; }
}
```

### 5.3 Scalar Parameter（只有一筆）

```csharp
public class Parameter_Budget : ParameterBase
{
    public double QTY { get; set; }
}
// 使用
dataload.param_Budget.Add(new Parameter_Budget { QTY = 760000.0 });
```

---

## 6. Dataload 類別

### 6.1 職責

- 宣告所有 Sets（`List<string>`、`List<DateTime>`、…）
- 宣告所有 Parameters（`List<Parameter_Xxx>`）
- 在建構子內初始化資料（或從 CSV/Oracle 讀取）
- 提供 `WriteToCSV(OptEngine)` 方法輸出解值

### 6.2 範本

```csharp
public class Dataload
{
    // Sets
    public List<string>   Employee = new List<string>();
    public List<string>   Group    = new List<string>();
    public List<DateTime> Date     = new List<DateTime>();

    // Parameters
    public List<Parameter_ShiftDemand> parameter_ShiftDemand = new List<Parameter_ShiftDemand>();
    public List<Parameter_CrossGroup>  parameter_CrossGroup  = new List<Parameter_CrossGroup>();

    // 罰分或設定常數（scalar，直接用 double）
    public double Penalty_SixDay = 1.0;

    public Dataload()
    {
        // 初始化 Sets
        Group.AddRange(new[] { "O", "D", "E", "N", "C" });
        for (int i = 1; i <= 16; i++) Employee.Add($"E{i}");

        int year = 2026, month = 1;
        for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
            Date.Add(new DateTime(year, month, d));

        // 初始化 Parameters
        Date.ForEach(d =>
        {
            parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "D", QTY = 5 });
            parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "N", QTY = 2 });
        });
    }

    public void WriteToCSV(OptEngine engine)
    {
        CSVCtrl.SaveToCSV<VariableB_ShiftAssign>(
            engine.GetSetVarSol<VariableB_ShiftAssign>(), DATA_ID: "V1", USER_ID: "USER");
    }
}
```

### 6.3 從 CSV 讀取（選用）

```csharp
// 在建構子中替換 inline 資料
this.Employee  = CSVCtrl.ReadStrSet("Set_Employee.csv");
this.parameter_ShiftDemand = CSVCtrl.BuildParameter<Parameter_ShiftDemand>("Param_ShiftDemand");
```

---

## 7. VariableCreate 類別

### 7.1 職責

呼叫 `OptEngine` 的 `Build*Vs<>()` 建立所有決策變數。

### 7.2 範本

```csharp
public class VariableCreate
{
    private OptEngine optEngine;
    private Dataload  dataload;

    public VariableCreate(Dataload dataload, OptEngine engine)
    {
        this.dataload  = dataload;
        this.optEngine = engine;
    }

    public void Build()
    {
        // Binary
        optEngine.BuildBVs<VariableB_ShiftAssign>(dataload.Date, dataload.Employee, dataload.Group);
        optEngine.BuildBVs<VariableB_GroupMismatch>(dataload.Date, dataload.Employee);

        // Continuous
        optEngine.BuildCVs<VariableX_BelowAVG>(dataload.Employee);

        // Integer（如需要）
        // optEngine.BuildIVs<VariableI_Xxx>(dataload.SetA, dataload.SetB);

        Logging.Info($"Variables created: {optEngine.varCount}");
    }
}
```

### 7.3 BuildXxx 方法對應

| 方法 | 說明 | 預設界限 |
|------|------|---------|
| `BuildBVs<T>(sets…)` | Binary variables | [0, 1] |
| `BuildIVs<T>(sets…)` | Integer variables | [0, ∞) |
| `BuildCVs<T>(sets…)` | Continuous variables | [0, ∞) |
| `BuildCVs<T>(lb, ub, sets…)` | Continuous，自訂上下界 | [lb, ub] |

Sets 傳入順序必須對應 Variable class 的 property 宣告順序。

---

## 8. Constraint 類別

### 8.1 規則

- 繼承 `ConstraintBase`（提供 `ConstraintName`、`ConstraintCount`）
- 建構子接受 `(Dataload dataload, OptEngine engine)`
- `Build()` 負責用 Pool API 建立限制式
- 每條限制式建完後呼叫 `ConstraintCount++`
- 最後一行 `Logging.Info($"[{ConstraintName}] {ConstraintCount}")`

### 8.2 Pool API 建構流程

```
AddLHS(coef, varObj)         ← LHS 變數項
AddLHS(constant)             ← LHS 常數項（若有）
AddRHS(value)                ← RHS 常數值
AddRHS(coef, varObj)         ← RHS 變數項（若有）
Create[Equal|LessEqual|GreatEqual](name)  ← 建立並清空 Pool
```

**絕對規則：**
- AML 左側的項 → `AddLHS`，右側的項 → `AddRHS`
- **禁止移項、改號、合併化簡**
- `CreateEqual` = `==`，`CreateLessEqual` = `<=`，`CreateGreatEqual` = `>=`

### 8.3 範本（等式限制）

```csharp
public class Constraint_FullfillDemand : ConstraintBase
{
    private OptEngine optEngine;
    private Dataload  dataload;

    public Constraint_FullfillDemand(Dataload dataload, OptEngine engine)
    {
        this.optEngine = engine;
        this.dataload  = dataload;
    }

    public void Build()
    {
        dataload.Date.ForEach(d =>
        {
            dataload.Group.Where(g => g != "O").ToList().ForEach(g =>
            {
                dataload.Employee.ForEach(e =>
                    optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = g }));

                double demand = dataload.parameter_ShiftDemand
                    .FirstOrDefault(x => x.Date == d && x.Group == g)?.QTY ?? 0;
                optEngine.AddRHS(demand);
                optEngine.CreateEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{g}");
                ConstraintCount++;
            });
        });
        Logging.Info($"[{ConstraintName}] {ConstraintCount}");
    }
}
```

### 8.4 範本（不等式限制，RHS 含變數）

```csharp
public void Build()
{
    dataload.Date.ForEach(d =>
    {
        dataload.Employee.ForEach(e =>
        {
            var preD = d.AddDays(-1);
            dataload.parameter_NightToDay.ForEach(rule =>
            {
                // LHS: violation flag
                optEngine.AddLHS(1, new VariableB_NightToDay { Date = d, Employee = e });
                // RHS: x[preD,e,preGroup] + x[d,e,group] - 1
                optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = preD, Employee = e, Group = rule.PreGroup });
                optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = d,    Employee = e, Group = rule.Group });
                optEngine.AddRHS(-1);
                optEngine.CreateGreatEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
                ConstraintCount++;
            });
        });
    });
    Logging.Info($"[{ConstraintName}] {ConstraintCount}");
}
```

### 8.5 LINQ 讀取 Parameter 的正確寫法

```csharp
// 先存成變數，再傳入 AddLHS/AddRHS
var cost = dataload.parameter_Cost
    .FirstOrDefault(x => x.PRODUCT == p)?.QTY ?? 0.0;
engine.AddLHS(cost, new VariableX_Amount { PRODUCT = p });

// 禁止：直接把 LINQ 嵌入 AddLHS 參數內
engine.AddLHS(dataload.parameter_Cost.FirstOrDefault(...)?.QTY ?? 0, ...); // ❌
```

---

## 9. ObjectiveFunction 類別

### 9.1 規則

- 與 Constraint 相同的建構子模式
- 只呼叫 `AddLHS`，不呼叫 `AddRHS`
- 最後呼叫 `engine.CreateMinimize()` 或 `engine.CreateMaximize()`

### 9.2 範本

```csharp
public class ObjectiveFunction
{
    private OptEngine optEngine;
    private Dataload  dataload;

    public ObjectiveFunction(Dataload dataload, OptEngine engine)
    {
        this.optEngine = engine;
        this.dataload  = dataload;
    }

    public void Build()
    {
        dataload.Date.ForEach(d =>
        {
            dataload.Employee.ForEach(e =>
            {
                optEngine.AddLHS(dataload.Penalty_SixDay,       new VariableB_SixDayWork    { Date = d, Employee = e });
                optEngine.AddLHS(dataload.Penalty_GroupMismatch, new VariableB_GroupMismatch { Date = d, Employee = e });
            });
        });
        dataload.Employee.ForEach(e =>
            optEngine.AddLHS(dataload.Penalty_BelowAVG, new VariableX_BelowAVG { Employee = e }));

        optEngine.CreateMinimize(); // 或 CreateMaximize()
    }
}
```

---

## 10. BuildModel 類別

### 10.1 職責

依照固定順序呼叫 ObjectiveFunction 和所有 Constraint 的 `Build()`。

### 10.2 範本

```csharp
public class BuildModel
{
    private OptEngine engine;
    private Dataload  dataload;

    public BuildModel(Dataload dataload, OptEngine engine)
    {
        this.engine   = engine;
        this.dataload = dataload;
    }

    public void Build()
    {
        Logging.Info("【建構目標式】");
        new ObjectiveFunction(dataload, engine).Build();

        Logging.Info("【建構限制式】");
        new Constraint_FullfillDemand(dataload, engine).Build();
        new Constraint_OneGroup(dataload, engine).Build();
        new Constraint_PreAssign(dataload, engine).Build();
        new Constraint_SixDayWork(dataload, engine).Build();
        new Constraint_NightToDay(dataload, engine).Build();
        // ... 其他限制式
    }
}
```

---

## 11. 主專案類別（ProblemName.cs）

### 11.1 職責

組裝 `CplexConfig`，按順序呼叫 Build → VariableCreate → BuildModel → Solve → Output。

### 11.2 完整範本

```csharp
public class ProblemName : IDisposable
{
    public OptEngine optEngine;
    public Dataload  dataload;

    public Stopwatch buildModelTimer = new Stopwatch();
    public Stopwatch totalTimer      = new Stopwatch();
    public TimeSpan  totalTimeSpan   = new TimeSpan();

    private bool   _isSuccess;
    private string _projectName => GetType().Name;

    public ProblemName()
    {
        dataload   = new Dataload();
        _isSuccess = false;
        Logging.SetLogFileName(_projectName);
    }

    public bool Execute()
    {
        totalTimer.Restart();

        CplexConfig config = new CplexConfig
        {
            epGap       = 0.01,
            timeLimit   = 300,
            workThreads = 8,
            enableLog   = true,
            exportLP    = true,
            exportMPS   = true,
            exportSol   = true
        };

        optEngine = new OptEngine(config);
        optEngine.Build();

        buildModelTimer.Restart();

        new VariableCreate(dataload, optEngine).Build();
        Logging.Info("【建構變數完成】", buildModelTimer);

        new BuildModel(dataload, optEngine).Build();
        Logging.Info("【建構模型完成】", buildModelTimer);

        buildModelTimer.Stop();

        _isSuccess = optEngine.Solve();

        if (_isSuccess)
            dataload.WriteToCSV(optEngine);

        totalTimeSpan = totalTimer.Elapsed;
        totalTimer.Stop();
        return _isSuccess;
    }

    public void Dispose()
    {
        optEngine?.Dispose();
    }
}
```

---

## 12. Program.cs

```csharp
using OptimFoundation.Core;
using ProjectNamespace;

internal class Program
{
    static void Main(string[] args)
    {
        using (ProblemName project = new ProblemName())
        {
            project.Execute();
            Logging.Info("整體運作時間:", project.totalTimer);
        }
    }
}
```

---

## 13. CplexConfig 參數說明

| 參數 | 型別 | 說明 | 建議值 |
|------|------|------|--------|
| `epGap` | `double` | MIP gap 容忍度 | LP: 0 / MILP: 0.01~0.05 |
| `timeLimit` | `int` | 求解時間上限（秒） | LP: 300 / IP: 1800 / MILP: 3600 |
| `workThreads` | `int` | 平行執行緒數 | 8~16 |
| `enableLog` | `bool` | 是否輸出 CPLEX log | `true` |
| `exportLP` | `bool` | 輸出 `.lp` 模型檔 | `true`（debug 用）|
| `exportMPS` | `bool` | 輸出 `.mps` 模型檔 | 選用 |
| `exportSol` | `bool` | 輸出 `.sol` 解值檔 | `true` |
| `projectName` | `string` | 檔案命名用（選填） | 預設用類別名稱 |

### 依問題類型的建議設定

| ProblemType | `epGap` | `timeLimit` |
|-------------|---------|-------------|
| LP | `0` | `300` |
| IP | `0` | `1800` |
| MILP | `0.01`~`0.05` | `3600` |

---

## 14. 解值取得

```csharp
// 取得目標函數值
double objVal = optEngine.GetObjectiveValue();

// 取得特定變數集合的解值（回傳 List<T>，只包含值 > 0 的變數）
var sol = optEngine.GetSetVarSol<VariableB_ShiftAssign>();
foreach (var v in sol)
{
    Console.WriteLine($"{v.Date:yyyy-MM-dd} {v.Employee} {v.Group}");
}

// 存到 CSV
CSVCtrl.SaveToCSV<VariableB_ShiftAssign>(sol, DATA_ID: "V1", USER_ID: "USER");
```

---

## 15. 命名規則總覽

### Variable / Parameter 命名

| 類別 | 前綴 | Set properties | 數值 property |
|------|------|----------------|---------------|
| Binary variable | `VariableB_` | 大寫 + 語義名（`Date`、`Employee`） | 無 |
| Integer variable | `VariableI_` | 同上 | 無 |
| Continuous variable | `VariableX_` | 同上 | 無 |
| Parameter | `Parameter_` | 大寫 + 語義名 | `QTY`（最後一個）|

### Constraint / Objective 命名

- Constraint 類別：`Constraint_<AML約束名稱>`
- Objective 類別：`ObjectiveFunction`（固定名稱）

### Constraint name（`Create*` 的第一個參數）

格式：`ConstraintName@key1@key2@...`，方便 CPLEX log / IIS debug。

範例：`"Constraint_FullfillDemand@2026_01_15@D"`

---

## 16. 常見錯誤與解法

| 錯誤訊息 | 原因 | 解法 |
|----------|------|------|
| `KeyNotFoundException: 找不到變數 'xxx'` | `AddLHS/AddRHS` 傳入的 `varObj.ToString()` key 與 `BuildBVs` 建立時的 key 不符 | 確認 property 宣告順序與 `Build*Vs` 傳入 set 順序一致 |
| `NullReferenceException` 在 LINQ 查 Parameter | `FirstOrDefault` 返回 `null` | 確認資料有初始化，或使用 `?.QTY ?? 0.0` |
| `InvalidOperationException: 缺少建構子` | Variable/Parameter class 有非預設建構子 | 移除所有建構子，只留 properties |
| CPLEX 1217 No solution | 模型 Infeasible | 檢查 LHS/RHS 方向是否移項錯誤；先拿掉部分限制式確認基礎模型 |
| CPLEX Error 1424 Invalid filetype | 嘗試用 `ExportModel` 輸出 `.sol` | 框架會自動呼叫 `WriteSolution`，不需手動操作 |
| Build 後 `varCount == 0` | `BuildBVs<>` 傳入空 List | 確認 Dataload 建構子內 Set 有正確 `AddRange` / `Add` |

---

## 17. 開發步驟 Checklist

```
□ 1. 新建 .csproj，加入 OptimFoundation.Core、OptimFoundation.Cplex 參考
□ 2. 建立 Data/Parameter_Xxx.cs（properties only，最後一個 QTY）
□ 3. 建立 Data/Dataload.cs（Sets、Parameters、建構子初始化資料）
□ 4. 建立 VariablesClass/Variable[B|I|X]_Xxx.cs（properties only）
□ 5. 建立 VariablesClass/VariableCreate.cs（BuildBVs / BuildCVs / BuildIVs）
□ 6. 建立 Constraints/ObjectiveFunction.cs（AddLHS + CreateMinimize/Maximize）
□ 7. 建立 Constraints/Constraint_Xxx.cs（AddLHS + AddRHS + Create* + ConstraintCount++）
□ 8. 建立 Constraints/BuildModel.cs（依序呼叫所有 Build()）
□ 9. 建立 ProblemName.cs（Execute：Config → Build → VariableCreate → BuildModel → Solve）
□ 10. 建立 Program.cs（using 呼叫 Execute）
□ 11. dotnet build → 確認 0 errors
□ 12. 執行，確認 CPLEX log 顯示 Optimal
```
