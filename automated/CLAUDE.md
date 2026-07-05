# AI Modeling — Claude Code 開發指引

## 專案概覽

ASP.NET Core Web API，接收自然語言最佳化問題描述，透過多階段 LLM 推理生成符合 OptimFoundation CPLEX 框架的完整 C# 專案。

- 規格文件：[specs/2026-05-20-ai-modeling-optim-cplex-env.md](specs/2026-05-20-ai-modeling-optim-cplex-env.md)
- 參考架構：研究來源 paper「OMG_LLM Reasoning Module」（外部，非本 repo 相依）
- 目標框架：[`../../OptimFoundation/`](../../OptimFoundation/)（sibling repo，相對路徑）
- 互動有 gate 版：見同 repo [`../interactive/`](../interactive/)（預設路線，本 automated/ 為全自動量產路線）

---

## 多階段推理流程（16 個階段）

```
Stage 0:  Classify        → ProblemType (LP/IP/MILP)
Stage 1:  SimpleModel     → 結構化自然語言
Stage 2:  StandardModel   → 標準化描述 + 約束分類
Stage 3:  KeyInfo         → JSON（Sets/Params/Vars/Obj/Constraints）
Stage 4:  AMLModel        → AMPL-style Markdown 數學模型
Stage 4b: AMLVerify       → 驗證 & 修正 AMLModel（必要）
Stage 5:  ParamCode       → Parameters.cs
Stage 6:  VarCode         → Variables.cs
Stage 7:  DataloadCode    → Dataload.cs
Stage 7b: DataloadVerify  → 驗證 & 修正 Dataload.cs（必要）
Stage 8:  ConstraintCode  → Constraints.cs
Stage 9:  ObjCode         → ObjectiveFunction.cs
Stage 10: VarCreateCode   → VariableCreate.cs
Stage 11: BuildConstraints→ BuildConstraints.cs
Stage 12: ProjectCode     → Project.cs
Stage 13: ProgramCode     → Program.cs
Stage 14: FixCode         → 自動修復（build 失敗時循環最多 5 次）
          WriteRagData    → 成功後將 AMLModel 寫回 docs/<ProjectName>.md
```

Prompt 模板放在 `Prompts/` 資料夾，一個階段一個 `.md` 檔。實作時由 `PromptLibrary.cs` 讀取並填入變數。

### Stage 0 — 問題分類影響 CplexConfig

| ProblemType | mipEmphasis | timeLimit |
|---|---|---|
| LP | 0 | 300 |
| IP | 1 | 1800 |
| MILP | 2 | 3600 |

---

## 生成的 C# 專案結構

```
<ProjectName>/
├── Model/
│   ├── Parameters.cs        # Param_XXX : ParameterBase
│   ├── Variables.cs         # VariableX_XXX / VariableY_XXX : VariableBase
│   ├── Constraints.cs       # Constraint_XXX : ConstraintBase
│   └── ObjectiveFunction.cs # ObjectiveFunction
├── Project/
│   ├── Dataload.cs          # Dataload（Sets + Params 資料）
│   ├── VariableCreate.cs    # 呼叫 BuildCVs<>/BuildIVs<>/BuildBVs<>
│   ├── BuildConstraints.cs  # 呼叫各 Constraint_XXX.Build()
│   └── Project.cs           # 設定目標函數、呼叫 Solve()、輸出結果
└── Program.cs
```

---

## OptimFoundation CPLEX API 速查

### Using 宣告

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
```

### 繼承結構

```csharp
public class MyModel : OptimFoundation.Cplex.OptEngine
{
    public MyModel(CplexConfig config) : base(config) { }
    public override void Build() { base.Build(); /* ... */ }
}
```

### 建立變數

```csharp
BuildCVs<VariableX_Name>(set1, set2);           // 連續
BuildIVs<VariableY_Name>(set1, set2);           // 整數
BuildBVs<VariableY_Name>(set1, set2);           // 二元
BuildCVs<VariableX_Name>(lb, ub, set1, set2);   // 自訂界限
```

### 建立約束（Pool 機制）

```csharp
// LHS 用 AddLHS，RHS 用 AddRHS
// 嚴禁左右兩側互換或改變係數正負號
foreach (var s1 in dataload.Set1)
{
    foreach (var s2 in dataload.Set2)
    {
        engine.AddLHS(coef, new VariableX_Name(s1, s2));
    }
    engine.AddRHS(rhsValue);
    engine.CreateLessEqual($"ConstraintName@{s1}");   // <= 
    // 或 CreateGreatEqual / CreateEqual
    ConstraintCount++;
}
```

### 建立目標函數

```csharp
foreach (var s in dataload.Set1)
    engine.AddLHS(cost[s], new VariableX_Name(s));
engine.CreateMinimize();  // 或 CreateMaximize()
```

### 求解與取得結果

```csharp
engine.Build();
bool ok = engine.Solve();
if (engine.Status == SolveStatus.Optimal)
{
    double obj = engine.GetObjectiveValue();
    var sol = engine.GetSolution("VariableX_Name");
}
engine.Dispose();
```

### CplexConfig

```csharp
var config = new CplexConfig
{
    workThreads = 8,
    timeLimit = 3600,
    epGap = 1e-4,
    enableLog = true,
    exportLP = true,
    exportSol = true
};
```

### 可調 Solver 旋鈕全表（tuning 用）

`CplexConfig` 的所有欄位皆 `null` = 用 CPLEX 預設；tuning 時只設要動的。全部已接線進 `OptEngine.Configuration()`。**改 solver 參數屬「solver 層」tuning，不改數學結構、不改數值。**

| 分類 | 欄位 | CPLEX 參數 | 用途 / 取值 |
|---|---|---|---|
| **核心** | `workThreads` | `Param.Threads` | 工作執行緒數 |
| | `timeLimit` | `Param.TimeLimit` | 牆鐘逾時秒數 |
| | `epGap` | `Param.MIP.Tolerances.MIPGap` | 相對 MIP gap |
| | `mipEmphasis` | `Param.Emphasis.MIP` | 0 平衡 / 1 可行 / 2 最佳 / 3 bound / 4 隱藏解 |
| | `algorithm` | `Param.RootAlgorithm` | root LP 演算法（0 自動…4 barrier） |
| | `randomSeed` | `Param.RandomSeed` | 隨機種子（可重現） |
| **決定論/計時** | `parallelMode` | `Param.Parallel` | -1 機會式 / 0 自動 / 1 決定論 |
| | `detTimeLimit` | `Param.DetTimeLimit` | 決定論時間上限 (ticks)，實驗可重現首選 |
| | `clockType` | `Param.ClockType` | 1 CPU / 2 wall |
| | `numericalEmphasis` | `Param.Emphasis.Numerical` | `true` = 數值穩定優先 |
| **容差** | `epOpt` | `Param.Simplex.Tolerances.Optimality` | 最佳性容差 |
| | `epRHS` | `Param.Simplex.Tolerances.Feasibility` | 可行性容差 |
| | `epInt` | `Param.MIP.Tolerances.Integrality` | 整數容差 |
| | `epAGap` | `Param.MIP.Tolerances.AbsMIPGap` | 絕對 MIP gap |
| **Limits** | `nodeLimit` | `Param.MIP.Limits.Nodes` | B&B 節點上限 |
| | `treeMemoryLimit` | `Param.MIP.Limits.TreeMemory` | 搜尋樹記憶體上限 (MB) |
| | `intSolLimit` | `Param.MIP.Limits.Solutions` | 找到 N 個整數解即停 |
| | `workMemory` | `Param.WorkMem` | 工作記憶體 (MB) |
| | `nodeFileInd` | `Param.MIP.Strategy.File` | 節點檔策略 |
| **搜尋策略** | `varSel` | `Param.MIP.Strategy.VariableSelect` | 分支變數選擇 |
| | `nodeSelect` | `Param.MIP.Strategy.NodeSelect` | 節點選擇 |
| | `branchDir` | `Param.MIP.Strategy.Branch` | -1 向下 / 0 自動 / 1 向上 |
| | `probe` | `Param.MIP.Strategy.Probe` | -1..3 探測強度 |
| | `mipSearch` | `Param.MIP.Strategy.Search` | 0 自動 / 1 傳統 / 2 動態 B&C |
| | `diveType` | `Param.MIP.Strategy.Dive` | 潛降策略 |
| | `rinsHeur` | `Param.MIP.Strategy.RINSHeur` | RINS 啟發式頻率 |
| | `HeuristicEffort` | `Param.MIP.Strategy.HeuristicEffort` | 啟發式投入程度 |
| | `PreIndicator` | `Param.Preprocessing.Presolve` | presolve 開關 |
| | `NodeAlgorithm` | `IntParam.NodeAlg` | 節點 LP 演算法 |
| **Cuts** | `cutsFactor` | `Param.MIP.Limits.CutsFactor` | cut 數量上限倍數 |
| | `cutPasses` | `Param.MIP.Limits.CutPasses` | cut 生成回合數 |
| | `gomoryCuts` / `coverCuts` / `cliqueCuts` / `mirCuts` / `flowCoverCuts` | `Param.MIP.Cuts.*` | 每族 -1 關 / 0 自動 / 1..3 漸積極 |
| **純 LP** | `simplexIterLimit` | `Param.Simplex.Limits.Iterations` | simplex 迭代上限 |
| | `barrierAlgorithm` | `Param.Barrier.Algorithm` | barrier 演算法 |

> 動 `CplexConfig` / `OptEngine` 屬改 OptimFoundation DLL → **Core + Cplex DLL 必須一起 rebuild 並更新到本專案**。

---

## 生成 Code 的命名規則

以下規則必須注入到每個 Prompt 中，確保 LLM 生成的 code 符合 OptimFoundation 慣例。

### Parameter 類別

```csharp
public class Param_<AMLParamName> : ParameterBase
{
    public string SET_NAME { get; set; }   // 大寫 + 底線，對應 AML Set 名稱
    public double QTY { get; set; }        // 資料值欄位，永遠放最後

    public Param_<AMLParamName>(params object[] sets) => InitClassBySets(sets);
    public Param_<AMLParamName>() { }
}
```

### Variable 類別

```csharp
// 連續變數
public class VariableX_<AMLVarName> : VariableBase
{
    public string SET_NAME { get; set; }   // 大寫 + 底線
    public VariableX_<AMLVarName>(params object[] sets) => InitClassBySets(sets);
    public VariableX_<AMLVarName>() { }
}

// 整數或二元變數
public class VariableY_<AMLVarName> : VariableBase { ... }
```

### Constraint 類別

```csharp
public class Constraint_<AMLConstraintName> : ConstraintBase
{
    private Dataload dataload;
    private OptEngine engine;

    public Constraint_<AMLConstraintName>(Dataload dataload, OptEngine engine)
    {
        this.dataload = dataload;
        this.engine = engine;
    }

    public override void Build()
    {
        // LHS → AddLHS, RHS → AddRHS
        // 嚴禁跨邊移項或改號
    }
}
```

### Set 成員字串命名

- CamelCase，首字母大寫：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌
- 複數概念用複合詞：`"TruckFleet"` ✅、`"VehicleGroup"` ✅

### 識別符命名

- Set 屬性：全大寫 + 底線（`PRODUCT_INDEX`、`CROP_TYPE`）
- 類別名：`Param_`、`VariableX_`、`VariableY_`、`Constraint_` 前綴 + AML 原名

---

## Constraint 分類系統

LLM 在 StandardModel / AMLModel 階段需將每個約束分類：

| 類型 | 描述 | 語言線索 |
|------|------|---------|
| UB / LB | 上下界限 | "at most", "at least", "no more than" |
| Balance | 等量平衡 | "must equal", "total input = total output" |
| Proportional | 比例範圍（不等式） | "at least X times", "in proportion to" |
| Conjunction | 同時滿足 | "only if all", "must all be active" |
| Disjunction | 至少滿足部分 | "at least one of", "a minimum of" |
| Exclusive XOR | 恰好選一 | "only one of", "exactly one must be selected" |
| Implication | 若A則B | "if... then...", "implies that" |
| Conditional Activation | Big-M 條件啟動 | "only if", "can be used when", "governed by trigger" |

---

## Dataload 類別規則

```csharp
public class Dataload
{
    // Sets → List<string>
    public List<string> PRODUCT_INDEX = new List<string>();

    // Parameters → 一律 List<Param_XXX>，包括 scalar（只有一筆）
    public List<Param_Budget> param_Budget = new List<Param_Budget>();

    public Dataload()
    {
        PRODUCT_INDEX.AddRange(new[] { "Condos", "DetachedHouse" });

        param_Budget.Add(new Param_Budget(760000.0));
        // 1D: new Param_Profit("Condos", 0.5)
        // 2D: new Param_Cost("CityA", "CityB", 10.0)
    }
}
```

**數值保真規則**：所有數值必須與原始問題描述完全一致，不得四捨五入、推算或填佔位符。

---

## Prompt 實作指引

### CoT 觸發

每個 Prompt 必須包含：
- "Think step by step"
- 明確的 Step 1 → Step N 推理結構
- 輸出格式要求（JSON / code block / Markdown）

### Prompt Context 傳入規則

| Stage | 傳入 context |
|---|---|
| 09_ObjCode | AMLModel + ParamCode + VarCode + DataloadCode + ConstraintCode |
| 10_VarCreateCode | AMLModel + VarCode + DataloadCode |
| 08_ConstraintCode | AMLModel + ParamCode + VarCode + DataloadCode |
| 07_DataloadCode | StandardModel + AMLModel + ParamCode |
| 07b_DataloadVerify | ProblemDescription + AMLModel + ParamCode + DataloadCode |
| 04b_AMLVerify | ProblemDescription + AMLModel |

### LHS/RHS 嚴格規則

生成 Constraint code 時：
- AML 左側的項 → `AddLHS(coef, variable)`
- AML 右側的項 → `AddRHS(value)`
- **絕對禁止**：移項、改號、合併化簡

### 參數讀取方式

```csharp
// 先 LINQ 查詢存進變數，再傳入 AddLHS/AddRHS
var cost = dataload.param_Cost.FirstOrDefault(x => x.PRODUCT == p)?.QTY ?? 0.0;
engine.AddLHS(cost, new VariableX_Amount(p));
```

**禁止**將 LINQ 直接嵌入 `AddLHS(...)` 內。

### Constraint 方向

AML `>=` → `CreateGreatEqual()`
AML `<=` → `CreateLessEqual()`
AML `=` → `CreateEqual()`
**不得猜測或翻轉比較方向。**

---

## 自動修復循環（Build & Run）

```
dotnet build
  ↓ 失敗
擷取 compiler error → GetFixPrompt(error, failedCode) → LLM 修正
  ↓ 最多 5 次
全失敗 → Job 狀態 = Failed，保留 error log
```

---

## RAG 架構（Microsoft Semantic Kernel）

- **Embedding Model**：`text-embedding-3-small`（Azure OpenAI / OpenAI）
- **Vector Store**：`VolatileMemoryStore`（in-memory，隨 API 啟動載入）
- **Collection**：`"optimization-docs"`

```csharp
// 套件：Microsoft.SemanticKernel
// 啟動時建立記憶體索引（載入 docs/*.md）
var memory = new SemanticTextMemory(
    new VolatileMemoryStore(),
    embeddingService  // text-embedding-3-small via Azure OpenAI
);

// 啟動時載入 docs/
foreach (var file in Directory.GetFiles("docs", "*.md"))
{
    var text = File.ReadAllText(file);
    var id = Path.GetFileNameWithoutExtension(file);
    await memory.SaveInformationAsync("optimization-docs", text, id);
}

// RAG 查詢（取 top-5 相似文件）
var results = memory.SearchAsync("optimization-docs", prompt, limit: 5);
var context = string.Join("\n\n", await results.Select(r => r.Metadata.Text).ToListAsync());
```

`IRagService` 的兩個方法：
- `QueryAsync(prompt)` → 純 LLM（不帶 context）
- `QueryRagAsync(prompt)` → 帶 top-5 retrieved context 的 LLM 呼叫

### WriteRagData — 成功後回寫 AML 知識

求解成功後，將生成的 AMLModel Markdown 寫回 `docs/<ProjectName>.md`，並 reload 至記憶體索引：

```csharp
// IProjectRunner.RunAsync() 末尾，Status == Optimal 時執行
var amlPath = Path.Combine("docs", $"{projectName}.md");
await File.WriteAllTextAsync(amlPath, amlModelMarkdown);
await memory.SaveInformationAsync("optimization-docs", amlModelMarkdown, projectName);
```

---

## Framework 1：Claude Code 互動建模工作流

### 啟動新專案

使用者說「幫我建模：[描述]」或「新建專案 [ProjectName]」時，Claude Code：

1. 在 `projects/<ProjectName>/stages/` 建立工作資料夾
2. 建立 `projects/<ProjectName>/status.json`（初始為空）
3. 依序執行 Stage 0 → 13，每個 stage 完成立刻寫入對應檔案
4. Stage 4（AMLModel）和 Stage 7b（Dataload 驗證）完成後**暫停確認**

### Stage 輸出檔命名

| Stage | 檔案 |
|---|---|
| 00 | `stages/00_classify.json` |
| 01 | `stages/01_simple.md` |
| 02 | `stages/02_standard.md` |
| 03 | `stages/03_keyinfo.json` |
| 04 | `stages/04_aml.md` |
| 04b | `stages/04b_aml_verified.md` |
| 05 | `stages/05_parameters.cs` |
| 06 | `stages/06_variables.cs` |
| 07 | `stages/07_dataload.cs` |
| 07b | `stages/07b_dataload_verified.cs` |
| 08 | `stages/08_constraints.cs` |
| 09 | `stages/09_objective.cs` |
| 10 | `stages/10_varcreate.cs` |
| 11 | `stages/11_buildconstraints.cs` |
| 12 | `stages/12_project.cs` |
| 13 | `stages/13_program.cs` |

`status.json` 格式：
```json
{ "completed": ["00","01","02"], "current": "03", "projectType": "MILP" }
```

### Resume（context 重置後繼續）

使用者說「繼續」時，Claude Code：
1. 讀 `status.json` 確認已完成的 stage 清單
2. 讀取 `current` stage 所需的前置檔案作為 context
3. 從 `current` stage 繼續，**不重跑**已完成的 stage

### 組裝 C# 專案

所有 stage 完成後，將 `stages/05-13` 的 .cs 檔複製到 `csharp/` 目錄：
```
csharp/
├── Model/Parameters.cs、Variables.cs、Constraints.cs、ObjectiveFunction.cs
├── Project/Dataload.cs、VariableCreate.cs、BuildConstraints.cs、Project.cs
├── Program.cs
└── <ProjectName>.csproj
```

### Build 失敗修復

使用者貼上 `dotnet build` error 時：
1. 讀取相關 .cs 檔案（錯誤出現的檔案 + 依賴的 Param/Var 類別）
2. 執行 Stage 14（Fix Prompt）
3. 寫回修正後的 .cs 檔案到 `stages/` 和 `csharp/`
4. 告訴使用者重新 build，最多允許 5 次

### Model Tuning（Stage 15 協定）

> 詳細協定見 [specs/2026-06-21-model-tuning-protocol.md](specs/2026-06-21-model-tuning-protocol.md)（產出 `Prompts/15_ModelTuning.md`）。任何涉及模型調整**先讀協定**，再依「正確性 → 效能」順序跑測試。

三類觸發，對應不同入口（**禁止**移項 / 改號 / 翻轉比較方向 / 四捨五入）：

| 觸發類型 | 線索 | 動作入口 |
|---|---|---|
| **structure**（數學結構） | 加/刪約束、改 big-M、加 valid inequalities | 從 Stage 4（`stages/04_aml.md`）重跑，`status.json` 中 04 之後標 pending |
| **data**（數值） | 改參數值 | 直接改 `stages/07b_dataload_verified.cs` → 更新 `csharp/Project/Dataload.cs` |
| **solver**（求解參數） | timeout / gap 過大 / 太慢，但模型正確 | 調 `csharp/Project/Project.cs` 的 `CplexConfig` 旋鈕（見上方「可調 Solver 旋鈕全表」） |

solver 層 tuning 決策（**正確性 gate 通過後才做**）：

- **timeout 但正確** → 提 `timeLimit` / 調 `mipEmphasis=1`（重可行解）/ 開 `parallelMode`；連續放寬仍 timeout → 回 structure（reformulation）。
- **gap 過大** → 收 `epGap` 或 `mipEmphasis=2`；無效再考慮 cuts（`gomoryCuts`/`mirCuts`…）或回 structure。
- **記憶體爆** → `treeMemoryLimit` + `nodeFileInd`。
- **要可重現實驗** → `parallelMode=1`（決定論）+ 固定 `randomSeed` + `detTimeLimit`。
- **數值不穩** → `numericalEmphasis=true`。

ProblemType 預設（Stage 0 決定）：

| ProblemType | mipEmphasis | timeLimit |
|---|---|---|
| LP | 0 | 300 |
| IP | 1 | 1800 |
| MILP | 2 | 3600 |

---

## 目錄結構（本 repo）

```
AI Modeling - Claude Code/
├── CLAUDE.md                  # 本檔
├── specs/                     # SDD 規格文件
├── Prompts/                   # 各階段 Prompt 模板（.md）
│   ├── 00_ProblemClassify.md  # Stage 0：LP/IP/MILP 分類
│   ├── 01_SimpleModel.md
│   ├── 02_StandardModel.md
│   ├── 03_KeyInfo.md
│   ├── 04_AMLModel.md
│   ├── 04b_AMLModelVerify.md  # Stage 4b：AML 驗證（必要）
│   ├── 05_ParamCode.md
│   ├── 06_VarCode.md
│   ├── 07_DataloadCode.md
│   ├── 07b_DataloadVerify.md  # Stage 7b：Dataload 驗證（必要）
│   ├── 08_ConstraintCode.md
│   ├── 09_ObjCode.md
│   ├── 10_VarCreateCode.md
│   ├── 11_BuildConstraintsCode.md
│   ├── 12_ProjectCode.md
│   ├── 13_ProgramCode.md
│   └── 14_FixCode.md
├── docs/                      # RAG 參考文件
│   ├── Basic_*.md             # 基礎模型範例（CPLEX 版）
│   └── CodeTemplate_*.md      # OptimFoundation CPLEX code 樣板
├── src/
│   └── AIModeling.Api/        # ASP.NET Core Web API
│       ├── Controllers/
│       ├── Services/
│       ├── Prompts/           # PromptLibrary.cs（讀取 Prompts/*.md）
│       └── Models/            # DTOs
└── projects/                  # 生成的 C# 專案輸出
```

---

## 重要注意事項

- OptimFoundation 用 `AddLHS` / `AddRHS`（不是 `addPool` / `addPoolRHS`）
- Namespace 用 `OptimFoundation.Cplex`（不是 `OMG.GurobiEngine`）
- 生成的 Constraint 類別接受 `OptEngine`（不是 Gurobi 的 `GRBModel`）
- 參考架構 `Prompts.py` 是 Gurobi 版本，改寫時必須換掉所有 Gurobi API
