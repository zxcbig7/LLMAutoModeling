# OptimFoundation CPLEX 開發流程（端到端 Playbook）

把「自然語言最佳化問題」變成「可執行、可調校的 OptimFoundation CPLEX 專案」的完整流程。
本檔是**頂層導覽**：講清楚各階段做什麼、產物是什麼、用哪份規範、怎麼銜接。
細節下鑽到 `claudemdTemplate/`（資料夾規範）、`tuning/`（調校）、`ai-modeling-framework-tutorial.md`（Modeling 階段）。

---

## 0. 心智模型：兩個 repo + 一組 consumer DLL

```
OptimFoundation/                      ← 框架（求解引擎、pipeline、tuning harness）
  src/OptimFoundation.Core   → OptimFoundation.Core.dll
  src/OptimFoundation.Cplex  → OptimFoundation.Cplex.dll
        │  build (Release)
        ▼  複製
AI-Modeling/dlls/                     ← 各專案用 HintPath 引用框架與 solver DLL
AI-Modeling/                          ← 建模端（題目專案、範本、規範、教學）
  ├── Template/            canonical code 範本（雙 runner）
  ├── claudemdTemplate/    各資料夾開發規範（單一來源）
  ├── tuning/             CPLEX 調校策略
  ├── tutorial/            本流程 + Modeling 教學
  └── Projects/            題目專案
```

`AI-Modeling/Template/` 是新專案唯一 canonical scaffold。`OptimFoundation/OptimFoundation/Templates/` 是 framework integration／compatibility examples，可能保留 `ProjectReference`、舊資料夾或手寫入口，不能拿來當生成規格。generator 是唯一路線；手寫 `VariableBase` / `ParameterBase` 已廢止，`Projects/HospitalRostering_Manual` 僅供理解舊 code，NEVER 照抄其宣告方式。

> **改了框架就要更新 DLL**：`dotnet build src/OptimFoundation.Cplex -c Release` → 複製
> `OptimFoundation.Core.dll` + `OptimFoundation.Cplex.dll` + `OptimFoundation.Generators.dll` 到 `AI-Modeling/dlls/`。
> AI-Modeling 是消費端，`csproj` 必須保留 repo-local `<Reference>` / `<Analyzer>` 的 `HintPath`，不可跨 repo 改成 `ProjectReference`。否則專案會看不到新的型別或拿到不同版本。

---

## 1. 流程總覽

```
自然語言問題
   │
   ▼  ① 規格 + 地圖
   │   /sdd 產 specs/*.md（approve 後動工）→ CodeMap.md（依賴地圖）
   ▼  ② Modeling（數學模型）
   │   問題分類 → 結構化 → KeyInfo(JSON) → AML(LaTeX) → 驗證
   ▼  ③ Coding（程式實作，新 pattern）
   │   Parameter → Set(Dataload) → Variable → Constraint/Objective
   │   → Program(OptModel 三階段 + 兩種 runner) → build
   ▼  ④ 正確性 gate
   │   解 / 目標值對得上問題描述？沒過不進 tuning
   ▼  ⑤ Tuning
   │   模型優化（優先）→ OptExperiment 掃 solver 旋鈕 → Experiments/*.csv
   ▼
最佳解 + 解值輸出（Solution/*.csv）
```

**逐層收斂、前一層沒過不進下一層**：Modeling 保證數學對、Coding 保證能跑、Tuning 保證快且收斂。

---

## 2. 階段 ①：規格與地圖（動工前）

| 步驟 | 觸發 | 產物 |
|------|------|------|
| `/sdd <一句話>` | 非 trivial 新功能 / 新題目 | `specs/YYYY-MM-DD-<slug>.md`，approve 後才動 code |
| `.claude/reference/CodeMap.md` | 規格確認後、stub 前；或進陌生模組前 | 依賴圖 + File/Symbol Index |

> 規範衝突時：專案自身 `CLAUDE.md` > `claudemdTemplate/` > 框架。

---

## 3. 階段 ②：Modeling

目標：自然語言 → 無歧義、可機器解析的數學模型。詳見 `ai-modeling-framework-tutorial.md` Part 1。

- **天條**：模型中所有數值（係數、上下限、比例）都對應到某個 `Parameter.QTY`，**禁止裸數字**。
- 產物 `Model/ProjectName_Model.md`：問題描述 → Sets → Parameters → Variables → Objective → Constraints（`[C1][C2]…` 與程式一一對應）。
- **暫停確認關卡**：AML 數學結構 + Dataload 數值，是最易錯處，先讓人確認再往下。

---

## 4. 階段 ③：Coding（模型定義 + 對稱 runner）

### 4.1 標準專案結構

```text
ProjectName/
├── Program.cs           ← 唯一組裝點（solve / experiment）
├── Model/   Parameter/   Set/   Variable/   Objective/   Constraint/
```

各資料夾職責與規範以 `claudemdTemplate/<Folder>/CLAUDE.md` 為單一來源：

| 資料夾 | 內容 | namespace | 規範 |
|--------|------|-----------|------|
| `Parameter/` | `ParameterBase` 子類別（`QTY`） | `Proj.Parameter` | `claudemdTemplate/Parameter` |
| `Set/` | `Dataload`（Sets 由 Parameters 衍生、WriteToCSV） | `Proj.Set` | `claudemdTemplate/Set` |
| `Variable/` | `VariableB_/X_/I_` | `Proj.Variable` | `claudemdTemplate/Variable` |
| `Objective/` | `ObjectiveFunction` | `Proj.Objective` | `claudemdTemplate/Objective` |
| `Constraint/` | `Constraint_*` | `Proj.Constraint` | `claudemdTemplate/Constraint` |
| `Model/` | 數學模型 `.md` | — | `claudemdTemplate/Model` |

### 4.2 composition root：Program.cs（唯一組裝點；手寫 XxxProblem 已廢止）

```csharp
using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ProjectName;
using ProjectName.Set;
using ProjectName.Variable;
using ProjectName.Constraint;

var data = OptData.Load(() => new ProjectNameDataload());
var projectConfig = new ProjectConfig
{
    ProjectName = "ProjectName",
    EnableSolverLog = true,
    ExportSol = true,
    DataId = "ProjectName",
    UserId = "SYSTEM",
};
var baseline = new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4 };

var model = new OptModel("baseline-model")
    .AddVariables(e => e.BuildBVs<VariableB_Assign>(data.ITEM, data.DATE))
    .AddVariables(e => e.BuildCVs<VariableX_Shortage>(data.ITEM))
    .AddObjective(e => new ObjectiveFunction(e, data.ITEM, data.parameter_Cost).Build())
    .AddConstraints(e => new Constraint_Capacity(e, data.ITEM, data.parameter_Capacity).Build())
    .AddConstraints(e => new Constraint_Demand(e, data.ITEM, data.DATE, data.parameter_Demand).Build());

if (args.Contains("experiment"))
{
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;
    new OptExperiment("project-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis", emphasis)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => baseline)
    .OnSolved(e => data.WriteToCSV(e));
bool ok = project.Execute();
```

`OptModel` 只註冊定義，框架固定 variables → objective → constraints；`OptProject` 負責單次求解與成功 callback，`OptExperiment` 負責 m×n 序列實驗且預設 log / 匯出 OFF。`ProjectConfig` 只放專案身分與輸出策略，`CplexConfig` 只放 solver 旋鈕。

每個 variable family、Objective、每條 Constraint 各占一個 fluent call。NEVER 用純轉呼叫 `CreateVariables` / `BuildObjective` / `BuildConstraints` helper 或 block lambda 隱藏組成。Objective / Constraint constructor 統一以 `OptEngine` 為第一個參數。

### 4.3 建模窗口（engine API）

- **變數**：`BuildBVs/CVs/IVs<T>(sets…)`（Binary/Continuous/Integer）
- **限制式 Pool**：`AddLHS(coef,var)` / `AddRHS(...)` 累加 → `CreateLessEqual/GreatEqual/Equal(name)`；區間用 `CreateRange(lb,ub,name)`；軟性用 `CreateLeSoft/GeSoft/EqSoft`
- **目標式**：累加後 `CreateMaximize()` / `CreateMinimize()`
- **不變式**：AML 左側→`AddLHS`、右側→`AddRHS`；**嚴禁移項 / 改號 / 化簡 / 翻轉比較方向**；係數先 LINQ 查進區域變數再傳入
- **取解**：`GetSetVarValues<T>()` / `GetVariableValue(name)` / `GetObjectiveValue()`

### 4.4 build

```
dotnet build  →  失敗則擷取 compiler error 修對應 .cs  →  重試（≤5）
```

---

## 5. 階段 ④：正確性 gate

求解後先驗「解 / 目標值對得上問題描述」。**沒過禁止進 tuning**——先回 Modeling / Coding 修正。

---

## 6. 階段 ⑤：Tuning（可執行架構）

**黃金順序：先模型優化，再調 solver 旋鈕。** 模型一刀的效益通常 > 調十個參數。

每個專案在 `Program.cs` 以 `OptExperiment` 建構掃描；底層 `Experiment` / `Trial.Capture` 由 runner 管理：

```
dotnet run -- experiment      →  掃多組 CplexConfig  →  Experiments/<name>.csv + .json
```

- 掃描單位是 baseline 的 `Clone()`：每個 clone 只動一個旋鈕，再以 `.AddConfig(label, config)` 加入。
- 抽象旋鈕（跨引擎）：`Emphasis / Seed / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / MemoryLimitMb`。
- CPLEX 專屬欄位 + 完整 ✅/❌ 對照：`../.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md`。
- 一份已驗證且凍結 framework-controlled mutation API 的 data 可供所有 cell 共用；每個 cell 使用 fresh engine。直接 public field / 可變 `List` 寫入不保證立即攔截，因此 callback MUST 視 data 為唯讀。
- `OptExperiment` 預設 solver log OFF、匯出 OFF、不做 housekeeping；`timeLimit` 確保每個 cell 收斂。
- 同名 experiment 採 append：`Save()` 會把既有 JSON 歷史合併回 `result.Trials`。只想處理本輪時使用唯一名稱；沿用同名時將回傳值視為累積資料。

詳見 `claudemdTemplate/Experiment/CLAUDE.md` 與 `../.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md`。

---

## 7. 慣例速查（天條）

- **QTY 天條**：所有數值來自 `Parameter.QTY`，任何地方不得裸數字。
- **命名**：★ 變數型別**由類名前綴單一決定**（`VariableB_`→Binary、`VariableI_`→Integer、`VariableX_`→Continuous），一律用 `BuildVars<T>(sets...)` 建立——`BuildBVs`/`BuildIVs`/`BuildCVs` 已禁用。前綴不是約定，是型別宣告。Parameter/Variable 只宣告 properties、無建構子、用 object initializer；數值欄位固定 `public double QTY` 放最後。
- **建模一條路**：class 用 `[OptVar]`/`[OptParam]` + `[OptDim]` source generator，composition 用 Fluent `OptModel`。手寫 class + `XxxProblem.Execute()` **已廢止**；`Projects/HospitalRostering_Generator` 是起手參考，`Projects/HospitalRostering_Manual` 僅供理解舊架構（並排見 `tutorial/index.html` §5.8）。
- **資料夾/namespace**：八資料夾 `Model/Set/Parameter/Variable/Objective/Constraint/Solution/Data`（NEVER 增減）+ 全專案單一 namespace `<Project>`，NEVER 子 namespace（廢 `Data/` 舊義、`Constraints/`、`VariablesClass/`、`SandBox`）。
- **模型定義共用**：同一個 `OptModel` 同時供 `OptProject` 與 `OptExperiment`。

---

## 8. 參考檔索引

| 需求 | 看這裡 |
|------|--------|
| 新建專案範本（code） | `Template/` |
| 各資料夾規範（單一來源） | `claudemdTemplate/<Folder>/CLAUDE.md` |
| Modeling 階段細節 | `tutorial/ai-modeling-framework-tutorial.md` Part 1 |
| Coding 細節（Pool / Dataload / 命名） | 同上 Part 2 |
| 調校策略 + 旋鈕對照 | `../.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md` |
| tuning 可執行架構 | `claudemdTemplate/Experiment/CLAUDE.md` |
| 框架窗口 / pipeline | `OptimFoundation`（`OptModel` / `OptProject` / `OptExperiment` / `EngineBase`） |
