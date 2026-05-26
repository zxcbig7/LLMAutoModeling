# OptimFoundation CPLEX Framework

## 概述

OptimFoundation 是封裝 IBM ILOG CPLEX 的 C# 框架，用於建構**整數線性規劃（ILP / MIP）**模型。
專案採用 .NET 8.0，DLL 參考路徑為 `IBM\ILOG\CPLEX_Studio2211\cplex\bin\x64_win64\`。

---

## 專案結構

```
MyProject/
├── Program.cs                  # 進入點
├── MyProject.cs                # 主問題類別（IDisposable）
├── MyProject.csproj
│
├── Data/
│   ├── Dataload.cs             # 所有 Sets、Parameters、罰分權重
│   └── Parameter_Xxx.cs        # 參數資料類別（繼承 ParameterBase）
│
├── VariablesClass/
│   ├── VariableCreate.cs       # 統一建立所有變數
│   ├── VariableB_Xxx.cs        # Binary 變數（繼承 VariableBase）
│   └── VariableX_Xxx.cs        # Continuous 變數（繼承 VariableBase）
│
└── Constraints/
    ├── BuildModel.cs           # 統一呼叫所有限制式 + 目標式
    ├── ObjectiveFunction.cs    # 目標函數（最小化 / 最大化）
    └── Constraint_Xxx.cs       # 各限制式類別（繼承 ConstraintBase）
```

---

## 核心元件

### CplexConfig — 求解器設定

```csharp
CplexConfig config = new CplexConfig
{
    epGap       = 0.03,    // MIP gap 容忍度（3%）
    timeLimit   = 100,     // 求解時間上限（秒）
    workThreads = 10,      // 平行執行緒數
    enableLog   = true,    // 顯示 CPLEX log
    exportSol   = true,    // 匯出 .sol 檔
    exportLP    = true,    // 匯出 .lp 檔（可讀模型）
    exportMPS   = true     // 匯出 .mps 檔
};
```

### OptEngine — 模型引擎

```csharp
optEngine = new OptEngine(config);
optEngine.Build();          // 初始化 CPLEX 環境
bool isOK = optEngine.Solve();  // 執行求解，回傳是否找到可行解
```

---

## 變數命名規則

| 前綴 | 類型 | 建立方法 | 說明 |
|------|------|----------|------|
| `VariableB_` | `bool` (0/1) | `BuildBVs<T>(sets...)` | Binary 決策變數 |
| `VariableX_` | `double` | `BuildCVs<T>(sets...)` | Continuous 決策變數 |

### 定義變數類別

```csharp
// Binary 變數 — 繼承 VariableBase，屬性即為 index sets
public class VariableB_Assign : VariableBase
{
    public string Item    { get; set; }
    public string Machine { get; set; }
    public DateTime Date  { get; set; }
}

// Continuous 變數
public class VariableX_Slack : VariableBase
{
    public string Item { get; set; }
}
```

> **注意**：屬性順序對應 `BuildBVs<T>()` 傳入 sets 的順序。

### 建立變數

```csharp
// VariableCreate.Build() 內
optEngine.BuildBVs<VariableB_Assign>(dataload.Items, dataload.Machines, dataload.Dates);
optEngine.BuildCVs<VariableX_Slack>(dataload.Items);
```

---

## 限制式建構語法

每個限制式類別繼承 `ConstraintBase`，提供：
- `ConstraintName` — 自動取得類別名稱
- `ConstraintCount` — 建立數量計數器

### LHS / RHS 累加模式

```csharp
// 加入 LHS（左側，決策變數端）
optEngine.AddLHS(coefficient, new VariableB_Assign { Item = i, Machine = m, Date = d });

// 加入 RHS（右側，右端值）
optEngine.AddRHS(10);                                          // 常數
optEngine.AddRHS(coefficient, new VariableB_OtherVar { ... }); // 含變數（移項用）

// 建立約束（每次 AddLHS/AddRHS 後呼叫一次，名稱需唯一）
optEngine.CreateEqual($"{ConstraintName}@{i}@{d:yyyy_MM_dd}");        // LHS = RHS
optEngine.CreateLessEqual($"{ConstraintName}@{i}@{m}");               // LHS ≤ RHS
optEngine.CreateGreatEqual($"{ConstraintName}@{i}");                   // LHS ≥ RHS

ConstraintCount++;  // 每建一條就 ++
```

> **重要**：`AddLHS` / `AddRHS` 是**累加**的，呼叫 `CreateXxx` 後才會清空，開始下一條。  
> 限制式名稱加 `@` 分隔索引，方便除錯時識別。

### 常見限制式型態

```csharp
// ① 等式：∑ x[i][d] = demand[d]
dataload.Items.ForEach(i =>
{
    optEngine.AddLHS(1, new VariableB_Assign { Item = i, Date = d });
});
optEngine.AddRHS(demand);
optEngine.CreateEqual($"{ConstraintName}@{d:yyyy_MM_dd}");
ConstraintCount++;

// ② 不等式（移項）：LHS ≤ A + B  →  LHS - B ≤ A
optEngine.AddLHS(1, new VariableB_Flag { Item = i });
optEngine.AddRHS(1, new VariableB_Assign { Item = i, Date = d });  // 正值移到 RHS
optEngine.AddRHS(-1);  // 常數
optEngine.CreateLessEqual($"{ConstraintName}@{i}@{d:yyyy_MM_dd}");
ConstraintCount++;

// ③ 時間視窗（滑動窗口）
var window = dataload.Dates
    .Where(sd => d.AddDays(-duration) < sd && sd <= d)
    .ToList();
if (window.Count < duration) return; // 資料不足跳過
```

---

## 目標函數

```csharp
// ObjectiveFunction.Build() 內

// 最小化（加入各罰分項）
dataload.Items.ForEach(i =>
{
    optEngine.AddLHS(penaltyWeight, new VariableB_Violation { Item = i });
    optEngine.AddLHS(penaltyWeight2, new VariableX_Slack { Item = i });
});
optEngine.CreateMinimize();

// 或最大化
optEngine.CreateMaximize();
```

---

## 參數資料類別

```csharp
// 簡單參數（不需建構子）
public class Parameter_Demand : ParameterBase
{
    public DateTime Date  { get; set; }
    public string   Group { get; set; }
    public double   QTY   { get; set; }
}

// 多 Set 複合鍵參數（需建構子）
public class Parameter_Template : ParameterBase
{
    public string   Set1 { get; set; }
    public double   Set2 { get; set; }
    public int      Set3 { get; set; }
    public DateTime Set4 { get; set; }
    public double   QTY  { get; set; }

    public Parameter_Template(params object[] Sets)
    {
        InitClassBySets(Sets); // 不要修改
    }
}
```

---

## Logging

```csharp
Logging.SetLogFileName("ProjectName"); // 設定 log 檔名（在建構子呼叫）
Logging.Info("訊息");
Logging.Info("訊息含計時:", stopwatch);
```

---

## 主問題類別範本

```csharp
public class MyProblem : IDisposable
{
    public OptEngine optEngine;
    public Dataload  dataload;
    public Stopwatch totalTimer     = new Stopwatch();
    public Stopwatch buildModelTimer = new Stopwatch();

    public MyProblem()
    {
        dataload = new Dataload();
        Logging.SetLogFileName(GetType().Name);
    }

    public bool Execute()
    {
        totalTimer.Restart();

        CplexConfig config = new CplexConfig
        {
            epGap       = 0.03,
            timeLimit   = 100,
            workThreads = 10,
            enableLog   = true,
            exportSol   = true,
            exportLP    = true,
            exportMPS   = true
        };

        optEngine = new OptEngine(config);
        optEngine.Build();

        buildModelTimer.Restart();
        new VariableCreate(dataload, optEngine).Build();
        Logging.Info("【建構變數完成】", buildModelTimer);

        new BuildModel(dataload, optEngine).Build();
        Logging.Info("【建構模型完成】", buildModelTimer);
        buildModelTimer.Stop();

        bool isSuccess = optEngine.Solve();
        if (isSuccess) dataload.WriteToCSV(optEngine);

        totalTimer.Stop();
        return isSuccess;
    }

    public void Dispose() => optEngine?.Dispose();
}
```

---

## 新增限制式 Checklist

1. 在 `VariablesClass/` 建立 `VariableB_Xxx.cs` 或 `VariableX_Xxx.cs`
2. 在 `VariableCreate.Build()` 加上 `BuildBVs<>()` 或 `BuildCVs<>()`
3. 在 `Constraints/` 建立 `Constraint_Xxx.cs`（繼承 `ConstraintBase`）
4. 在 `Constraints/BuildModel.Build()` 加上 `new Constraint_Xxx(dataload, engine).Build()`
5. 若有罰分，在 `ObjectiveFunction.Build()` 加 `AddLHS(penalty, var)`
6. 在 `Dataload` 加對應的 penalty 權重與 parameter

---

## 依賴套件

| 套件 | 說明 |
|------|------|
| `ILOG.CPLEX` / `ILOG.Concert` | IBM CPLEX 求解器核心 |
| `OptimFoundation.Core` | 基礎類別（VariableBase、ParameterBase、ConstraintBase、Logging） |
| `OptimFoundation.Cplex` | OptEngine、CplexConfig |
| `NLog` | 日誌（隨 OptimFoundation.Core 附帶） |
| `Oracle.ManagedDataAccess.Core` | Oracle DB 連線（選用，NuGet） |

> **★ 天條**：所有 DLL 統一放在 `ClaudeAIAssistant\dlls\`，csproj HintPath 一律指向該目錄。
> 詳見根目錄 `CLAUDE.md`。

DLL HintPath（Template_CPLEX 相對路徑）：

```xml
<HintPath>..\dlls\ILOG.Concert.dll</HintPath>
<HintPath>..\dlls\ILOG.CPLEX.dll</HintPath>
<HintPath>..\dlls\NLog.dll</HintPath>
<HintPath>..\dlls\OptimFoundation.Core.dll</HintPath>
<HintPath>..\dlls\OptimFoundation.Cplex.dll</HintPath>
```
