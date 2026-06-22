# OptimFoundation CPLEX 開發流程（端到端 Playbook）

把「自然語言最佳化問題」變成「可執行、可調校的 OptimFoundation CPLEX 專案」的完整流程。
本檔是**頂層導覽**：講清楚各階段做什麼、產物是什麼、用哪份規範、怎麼銜接。
細節下鑽到 `claudemdTemplate/`（資料夾規範）、`truning/`（調校）、`ai-modeling-framework-tutorial.md`（Modeling 階段）。

---

## 0. 心智模型：兩個 repo + 一份 DLL

```
OptimFoundation/                      ← 框架（求解引擎、pipeline、tuning harness）
  src/OptimFoundation.Core   → OptimFoundation.Core.dll
  src/OptimFoundation.Cplex  → OptimFoundation.Cplex.dll
        │  build (Release)
        ▼  複製
ClaudeAIAssistant/dlls/               ← 各專案用 HintPath 引用這兩個 DLL
ClaudeAIAssistant/                     ← 建模端（題目專案、範本、規範、教學）
  ├── Template_CPLEX/      canonical code 範本（雙模式）
  ├── claudemdTemplate/    各資料夾開發規範（單一來源）
  ├── truning/             CPLEX 調校策略
  ├── tutorial/            本流程 + Modeling 教學
  └── Projects/            題目專案
```

> **改了框架就要更新 DLL**：`dotnet build src/OptimFoundation.Cplex -c Release` → 複製
> `OptimFoundation.Core.dll` + `OptimFoundation.Cplex.dll` 到 `ClaudeAIAssistant/dlls/`。
> 否則專案編譯時看不到新的型別（OptModel / Experiment / 新窗口）。

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
   │   → VariableCreate / BuildModel → Program(Fluent OptModel 雙模式) → build
   ▼  ④ 正確性 gate
   │   解 / 目標值對得上問題描述？沒過不進 tuning
   ▼  ⑤ Tuning
   │   模型優化（優先）→ ExperimentRunner 掃 solver 旋鈕 → Experiments/*.csv
   ▼
最佳解 + 解值輸出（Solution/*.csv）
```

**逐層收斂、前一層沒過不進下一層**：Modeling 保證數學對、Coding 保證能跑、Tuning 保證快且收斂。

---

## 2. 階段 ①：規格與地圖（動工前）

| 步驟 | 觸發 | 產物 |
|------|------|------|
| `/sdd <一句話>` | 非 trivial 新功能 / 新題目 | `specs/YYYY-MM-DD-<slug>.md`，approve 後才動 code |
| `CodeMap.md` | 規格確認後、stub 前；或進陌生模組前 | 依賴圖 + File/Symbol Index |

> 規範衝突時：專案自身 `CLAUDE.md` > `claudemdTemplate/` > 框架。

---

## 3. 階段 ②：Modeling

目標：自然語言 → 無歧義、可機器解析的數學模型。詳見 `ai-modeling-framework-tutorial.md` Part 1。

- **天條**：模型中所有數值（係數、上下限、比例）都對應到某個 `Parameter.QTY`，**禁止裸數字**。
- 產物 `Model/ProjectName_Model.md`：問題描述 → Sets → Parameters → Variables → Objective → Constraints（`[C1][C2]…` 與程式一一對應）。
- **暫停確認關卡**：AML 數學結構 + Dataload 數值，是最易錯處，先讓人確認再往下。

---

## 4. 階段 ③：Coding（Fluent OptModel + 雙模式）

### 4.1 標準專案結構

```text
ProjectName/
├── Program.cs           ← 唯一進入點（solve / experiment 雙模式）
├── ExperimentRunner.cs  ← 參數掃描
├── Model/   Parameter/   Set/   Variable/   Objective/   Constraint/
```

各資料夾職責與規範以 `claudemdTemplate/<Folder>/CLAUDE.md` 為單一來源：

| 資料夾 | 內容 | namespace | 規範 |
|--------|------|-----------|------|
| `Parameter/` | `ParameterBase` 子類別（`QTY`） | `Proj.Parameter` | `claudemdTemplate/Parameter` |
| `Set/` | `Dataload`（Sets 由 Parameters 衍生、WriteToCSV） | `Proj.Set` | `claudemdTemplate/Set` |
| `Variable/` | `VariableB_/X_/I_` + `VariableCreate` | `Proj.Variable` | `claudemdTemplate/Variable` |
| `Objective/` | `ObjectiveFunction` | `Proj.Objective` | `claudemdTemplate/Objective` |
| `Constraint/` | `Constraint_*` + `BuildModel` | `Proj.Constraint` | `claudemdTemplate/Constraint` |
| `Model/` | 數學模型 `.md` | — | `claudemdTemplate/Model` |

### 4.2 composition root：Program.cs（唯一，不手寫 XxxProblem）

```csharp
using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ProjectName;
using ProjectName.Set;
using ProjectName.Variable;
using ProjectName.Constraint;

if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }

var dataload = new ProjectNameDataload();
using (var m = new OptModel("ProjectName")
    .UseConfig(() => new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4, enableLog = true, exportSol = true })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

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

每個專案標配 `ExperimentRunner.cs`，建構於框架 harness（`Experiment` / `Trial.Capture` / `ITunableConfig`）：

```
dotnet run -- experiment      →  掃多組 CplexConfig  →  Experiments/<name>.csv + .json
```

- 掃描單位 `(label, Action<CplexConfig> tune)`：baseline + 每次只動一個旋鈕。
- 抽象旋鈕（跨引擎）：`Emphasis / Seed / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / MemoryLimitMb`。
- CPLEX 專屬欄位 + 完整 ✅/❌ 對照：`truning/CLAUDE.md`。
- 每 Trial fresh Dataload + engine；掃描時關 log / export 加速；`timeLimit` 確保收斂。

詳見 `claudemdTemplate/Experiment/CLAUDE.md` 與 `truning/CLAUDE.md`。

---

## 7. 慣例速查（天條）

- **QTY 天條**：所有數值來自 `Parameter.QTY`，任何地方不得裸數字。
- **命名**：變數型別由 `Build*Vs<T>` 決定，前綴只是約定（`B`→BVs、`I`→IVs、`X`→CVs）；Parameter/Variable 只宣告 properties、無建構子、用 object initializer；數值欄位固定 `public double QTY` 放最後。
- **composition 唯一**：Fluent `OptModel`，不手寫 `XxxProblem.Execute()`。
- **資料夾/namespace**：`Set/Variable/Parameter/Objective/Constraint/Model` + `ProjectName.*`（廢 `Data/`、`Constraints/`、`VariablesClass/`、`SandBox`）。
- **建模 step 共用**：`VariableCreate` / `BuildModel` 同時供 solve 與 experiment。

---

## 8. 參考檔索引

| 需求 | 看這裡 |
|------|--------|
| 新建專案範本（code） | `Template_CPLEX/` |
| 各資料夾規範（單一來源） | `claudemdTemplate/<Folder>/CLAUDE.md` |
| Modeling 階段細節 | `tutorial/ai-modeling-framework-tutorial.md` Part 1 |
| Coding 細節（Pool / Dataload / 命名） | 同上 Part 2 |
| 調校策略 + 旋鈕對照 | `truning/CLAUDE.md` |
| tuning 可執行架構 | `claudemdTemplate/Experiment/CLAUDE.md` |
| 框架窗口 / pipeline | `OptimFoundation`（`OptModel` / `EngineBase` / `Experiment` / `Trial`） |
