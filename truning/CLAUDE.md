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

## ★ 開發流程天條（Phase Gate）

> 框架精神：**(Modeling ──強力綁定──▶ Coding) ──有需要才做──▶ Tuning**
>
> **禁止在使用者明確確認數學模型前產生任何 `.cs` 檔。**
> **任何不清楚的術語或題目描述，禁止猜測，必須追問後才繼續。**

### 三階段流程

#### Phase 1 — Modeling（只輸出 Model.md）

- 收到新題目 → 只產出 `Model\MyProject_Model.md`，禁止同時建立任何 `.cs`
- 模型必須完整涵蓋：問題描述 → Sets → Parameters → Decision Variables → Objective → Constraints
- 專有名詞參照 `Model\Glossary.md`；有不清楚的術語 → 追問，不得自行詮釋
- 所有歧義必須在此階段釐清（見下方確認清單）
- 等待使用者明確說「**開始實作**」或「**模型確認**」

#### Phase 2 — Coding（純機械轉譯，強力綁定 Phase 1）

- Coding 是將 Model.md 逐條翻譯為程式碼，不允許任何自行詮釋
- 若發現 Model.md 有任何歧義 → 立即停止，回 Phase 1 補充模型，禁止自行假設後繼續
- 命名、架構、係數來源全部依 CLAUDE.md 規則，無例外

#### Phase 3 — Tuning（有需要才做，使用者明確指示）

- 僅在使用者提出後才進行，不主動建議
- Tuning 變更若影響模型語意（新增 slack variable、新增 constraint）→ 必須同步更新 Model.md

**方向一：Solver 設定**（不動模型，調 CPLEX 參數）

| 參數 | 用途 |
| --- | --- |
| `EpGap` | MIP 最優性誤差容忍，調大換速度 |
| `TiLim` | 時間上限，強制停止取目前最佳解 |
| `MIPEmphasis` | 切換策略：找可行解 / 證明最優 / 最佳 bound |
| Thread count | 平行化 |
| Presolve / Cuts | 開關 Gomory、clique 等 cut generation |

**方向二：IIS → Soft Constraint**（模型 infeasible 時的標準流程）

1. 執行 CPLEX IIS，找出最小衝突 constraint 集合
2. 把該 constraint 改用框架內建的軟限制式 API（自動加彈性變數 + penalty）：
   - `engine.CreateLeSoft(rhs, penalty)` / `CreateGeSoft(rhs, penalty)` / `CreateEqSoft(rhs, penalty, name)`
   - 用法同 hard 版：先 `AddLHS(...)` 再 `CreateXxSoft(...)`（見 CPLEX_API_REFERENCE 6.5）
3. 違反量 = 彈性變數解值，可 `GetVariableValue("Deficit_...")` 查
4. Model.md 同步標記該 constraint 從 Hard 改為 Soft（penalty 值寫進 Parameter）

**方向三：模型結構優化**（效能差異大時才動）

| 手段 | 時機 |
| --- | --- |
| Symmetry breaking | 多個等價解存在（如員工互換），大幅縮減 B&B 搜索空間 |
| Tighten Big-M | LP relaxation gap 過大，找更緊的 M 上界 |
| Valid inequalities | 加入不改 feasible region 但收緊 LP bound 的補強 constraint |
| Warm start | 提供初始可行解給 CPLEX 作為 B&B 起點 |

**Tuning 記錄（方向一掃描多組設定時用）**：用 `Experiment` 套件系統化記錄、比較各組設定，取代人工翻 log。

```csharp
var exp = new Experiment("proj-tuning", "說明");
foreach (var (label, tune) in variants) {
    var config = new CplexConfig { ... }; tune(config);   // 用抽象旋鈕：config.Emphasis / config.Seed ...
    using var engine = new OptEngine(config); engine.Build();
    new VariableCreate(d, engine).Build(); new BuildModel(d, engine).Build();
    exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
}
exp.Save();   // → Experiments/proj-tuning.csv + .json（指標：time/gap/bound/node + 收斂軌跡）
```

> 詳見 CPLEX_API_REFERENCE 第 16 節。每個 Trial 記錄完整設定 + 收斂數據；CSV 給人對照、JSON 給後續 LLM tuning。

### 預設慣例（不追問，直接按此處理）

| 項目 | 預設行為 |
| --- | --- |
| Index domain 邊界 | Dataset 已清洗，LINQ 直接篩選，不需額外確認 |
| 參數未定義的組合 | 填入不影響模型的合理預設值（通常為 0 或略過該 constraint） |
| Soft vs Hard constraint | 預設設計為 Hard constraint；放鬆為 penalty 屬 Phase 3，不在 Phase 1 討論 |
| Big-M 值 | 視模型需求自行判斷合理上界，不另行追問 |
| 目標函數方向 | 使用者描述模型時必然包含；若真的沒提才追問 |

### 仍需 Phase 1 明確確認的項目

| 歧義類型 | 需確認的問題 |
| --- | --- |
| Linearization 選擇 | OR/AND 邏輯約束有多種等價 formulation，效能差異大，需指定 |
| Time boundary | 時間序列是否有 wrap-around（最後一天連接第一天）？ |
| 任何不熟悉的術語 | 參照 Glossary.md；找不到定義 → 必須追問 |

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
│   ├── MyProject_Model.md          ← Sets / Parameters / Variables / Objective / Constraints
│   └── Glossary.md                 ← 專有名詞定義（Phase 1 建立，不清楚的術語追問後補入）
│
├── Data\                           ← Sets 定義（由 Parameters 衍生）+ Dataload
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
>
> ★ **天條：禁止 Hardcode**：模型所有數值（係數、容量、比例等）一律定義在 `Parameter` 類別的 `QTY` 欄位，透過 `Dataload` helper 取得。`Constraint` / `Objective` 程式碼中不得出現任何裸數字，即使題目只提到少數幾個數值也必須參數化。

### ★ 命名對應規則（天條）

**Model.md 符號命名：**

- 所有 Sets、Parameters、Variables、Constraints 必須使用語意名稱
- 禁止使用無意義符號：`i`、`j`、`k`、`x`、`y`、`z`、`t`（單一字母）
- 正確範例：`GlassType`、`Machine`、`ProductionQty`、`Assign_{Employee,Date}`

**程式碼命名必須直接對應 Model.md 符號：**

| Model.md 符號 | 程式碼類別名稱 |
| --- | --- |
| `Produce_{GlassType}` | `VariableX_Production` |
| `Assign_{Employee,Date}` | `VariableB_Assign` |
| `Capacity_{GlassType}` constraint | `Constraint_Capacity` |
| `DemandQty` parameter | `Parameter_Demand`（欄位 `QTY`） |

對應原則：取模型符號的語意核心，去掉 index 下標，轉 PascalCase。

### ★ BuildModel 架構規則（天條）

- 每一類 constraint 獨立一個 `Constraint_Xxx.cs`，對應 Model.md 的一條或一組邏輯相關的 constraints
- `BuildModel.cs` 只負責呼叫各 Constraint class 的 `Build()` 方法，**禁止**在 BuildModel.cs 內直接寫 `AddLHS` / `AddRHS`
- `ObjectiveFunction.cs` 同理，Objective 邏輯全部在此，BuildModel 只呼叫它

### Namespace 規則

| 資料夾 | Namespace |
| -------- | ----------- |
| `Data\` | `MyProject.Data` |
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
    // CSV 輸出預設不呼叫，使用者說「輸出 CSV」時才加入：
    // if (ok) { FolderDir.Solution.CreateFolder(); CsvCtrl.SaveSolutionToCSV<VariableX_Xxx>(optEngine, "ProjectName", "USER"); }
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
