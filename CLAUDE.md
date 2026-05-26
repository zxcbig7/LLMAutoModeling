# ClaudeAIAssistant — OptimFoundation CPLEX 專案

## 專案結構

```text
ClaudeAIAssistant/
├── dlls/                           ← ★ 所有 DLL 唯一來源（天條）
│   ├── ILOG.Concert.dll
│   ├── ILOG.CPLEX.dll
│   ├── NLog.dll
│   ├── OptimFoundation.Core.dll
│   └── OptimFoundation.Cplex.dll
│
├── Template_CPLEX/                 ← 框架使用範本（參考用）
│   └── ...
│
└── Projects/                       ← 所有實際題目專案放這裡
    └── GlassFactory/
        └── ...
```

---

## ★ DLL 參考規則（天條）

> **所有專案的 DLL 一律參考 `ClaudeAIAssistant\dlls\`，禁止使用其他路徑。**

### csproj HintPath 計算方式

| 專案位置 | 到 `dlls\` 的相對路徑 |
| --- | --- |
| `ClaudeAIAssistant\Template_CPLEX\` | `..\dlls\Xxx.dll` |
| `ClaudeAIAssistant\Projects\MyProject\` | `..\..\dlls\Xxx.dll` |

### csproj 標準寫法

**Template_CPLEX（`..\dlls\`）：**

```xml
<ItemGroup>
  <Reference Include="ILOG.Concert">
    <HintPath>..\dlls\ILOG.Concert.dll</HintPath>
  </Reference>
  <Reference Include="ILOG.CPLEX">
    <HintPath>..\dlls\ILOG.CPLEX.dll</HintPath>
  </Reference>
  <Reference Include="NLog">
    <HintPath>..\dlls\NLog.dll</HintPath>
  </Reference>
  <Reference Include="OptimFoundation.Core">
    <HintPath>..\dlls\OptimFoundation.Core.dll</HintPath>
  </Reference>
  <Reference Include="OptimFoundation.Cplex">
    <HintPath>..\dlls\OptimFoundation.Cplex.dll</HintPath>
  </Reference>
</ItemGroup>
```

**Projects\MyProject（`..\..\dlls\`）：**

```xml
<ItemGroup>
  <Reference Include="ILOG.Concert">
    <HintPath>..\..\dlls\ILOG.Concert.dll</HintPath>
  </Reference>
  <Reference Include="ILOG.CPLEX">
    <HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath>
  </Reference>
  <Reference Include="NLog">
    <HintPath>..\..\dlls\NLog.dll</HintPath>
  </Reference>
  <Reference Include="OptimFoundation.Core">
    <HintPath>..\..\dlls\OptimFoundation.Core.dll</HintPath>
  </Reference>
  <Reference Include="OptimFoundation.Cplex">
    <HintPath>..\..\dlls\OptimFoundation.Cplex.dll</HintPath>
  </Reference>
</ItemGroup>
```

---

## 框架概述

OptimFoundation 是封裝 IBM ILOG CPLEX 的 C# 框架，用於建構**整數線性規劃（ILP / MIP）**模型。
詳細語法見 `Template_CPLEX\CLAUDE.md`。

### 每個 Projects\MyProject 的標準結構

```text
Projects\MyProject\
├── MyProject.csproj
├── Program.cs
├── MyProblem.cs                    ← 主類別（一行注解指向 Model/）
│
├── Model\                          ← 純數學模型（Markdown，無程式碼）
│   └── MyProject_Model.md          ← Sets / Parameters / Variables / Objective / Constraints
│
├── Set\                            ← Sets 定義（由 Parameters 衍生）+ Dataload
│   └── Dataload.cs
│
├── Parameter\                      ← ★ 天條：必須存在，Parameter 類別（ParameterBase 子類別）
│   └── Parameter_Xxx.cs
│
├── Variable\                       ← Variable 類別定義 + VariableCreate
│   ├── VariableB_Xxx.cs            ← Binary 變數（VariableBase 子類別）
│   ├── VariableX_Xxx.cs            ← Continuous 變數
│   └── VariableCreate.cs           ← BuildBVs / BuildCVs 呼叫
│
├── Objective\                      ← 目標函數
│   └── ObjectiveFunction.cs
│
└── Constraint\                     ← 限制式 + 模型建構入口
    ├── BuildModel.cs
    └── Constraint_Xxx.cs
```

`Model\MyProject_Model.md` 標準章節：**問題描述 → Sets → Parameters → Decision Variables → Objective → Constraints**

> ★ **天條**：`Parameter\` 資料夾必須存在，Sets 由 Parameters 衍生（`=> parameter_Xxx.Select(...).ToList()`）。

### Namespace 規則

| 資料夾 | Namespace |
| -------- | ----------- |
| `Set\` | `MyProject.Set` |
| `Parameter\` | `MyProject.Parameter` |
| `Variable\` | `MyProject.Variable` |
| `Objective\` | `MyProject.Objective` |
| `Constraint\` | `MyProject.Constraint` |
| 根目錄 | `MyProject` |

### 執行流程

```csharp
// Program.cs
using (var problem = new MyProblem())
{
    bool ok = problem.Execute();   // ★ 天條：必須接收 bool 回傳值
}

// MyProblem.Execute() → 必須回傳 bool
public bool Execute()
{
    optEngine = new OptEngine(config);
    optEngine.Build();
    new VariableCreate(dataload, optEngine).Build();   // 1. 建變數
    new BuildModel(dataload, optEngine).Build();        // 2. 建模型（目標 + 限制）
    bool ok = optEngine.Solve();                        // 3. 求解 ★ 必須接收回傳值
    if (ok) dataload.WriteToCSV(optEngine);
    return ok;
}
```

---

## 限制式語法速查

```csharp
// LHS 累加變數
optEngine.AddLHS(coefficient, new VariableB_Xxx { Set1 = s1, Set2 = s2 });

// RHS 設定（常數 or 含變數）
optEngine.AddRHS(value);
optEngine.AddRHS(coefficient, new VariableB_Xxx { ... });  // 移項用

// 建立約束（名稱格式：ConstraintName@index1@index2）
optEngine.CreateEqual($"{ConstraintName}@{s1}@{s2}");
optEngine.CreateLessEqual($"{ConstraintName}@{s1}");
optEngine.CreateGreatEqual($"{ConstraintName}@{s1}");
ConstraintCount++;
```

---

## 目標函數

```csharp
optEngine.AddLHS(penaltyWeight, new VariableX_Slack { Item = i });
optEngine.CreateMinimize();  // 或 CreateMaximize()
```

---

## ★ 解取得 API（基於 Foundation 原始碼）

> **天條**：API 呼叫永遠基於 `Foundation\` 原始碼定義，禁止動到 Foundation。

### 變數名稱格式（`ModelElementBase.ToString()`）

```text
ClassName@prop1@prop2@...
DateTime 屬性格式：@yyyy-MM-dd
```

範例：

- `VariableX_Production { GlassType = "Regular" }` → `"VariableX_Production@Regular"`
- `VariableB_Assign { Employee = "E1", Date = 2026-01-01 }` → `"VariableB_Assign@E1@2026-01-01"`

### 取解方法

```csharp
// ① 目標函數值
double obj = engine.GetObjectiveValue();

// ② 所有變數解值（Dictionary<string, double>，key = 完整變數名稱）
var sol = engine.GetSetVarValues<VariableX_Production>();
// → {"VariableX_Production@Regular": 60.0, "VariableX_Production@Tempered": 0.0}

// ③ 依型別名稱取解（同 GetSetVarValues，但用字串型別名）
var sol2 = engine.GetSolution("VariableX_Production");

// ④ 解出特定變數值（傳完整變數名稱）
double v = engine.GetVariableValue("VariableX_Production@Regular");

// ⑤ 從 dictionary 解析 label
foreach (var kvp in sol)
{
    string label = kvp.Key.Split('@').Last();  // "Regular"
    double value = kvp.Value;
}

// ⑥ 存 CSV → Solution/VariableName.csv
// ★ 必須先 CreateFolder()！ProjFolder 建構子在編譯版 DLL 不自動建目錄。
FolderDir.Solution.CreateFolder();
CsvCtrl.SaveSolutionToCSV<VariableX_Production>(engine, "GlassFactory", "USER");
```

### 禁止使用的方法（不存在於 Foundation）

```csharp
// ✗ engine.GetVarSol(...)       → 不存在
// ✗ engine.GetSetVarSol<T>()    → 不存在
// ✗ CsvCtrl.SaveToCSV<T>(...)   → 不存在（正確：SaveSolutionToCSV）
```
