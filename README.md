# AI Modeling — OptimFoundation CPLEX

自然語言最佳化題目 → 可求解的 **OptimFoundation**（封裝 IBM ILOG CPLEX）C# 專案。
建模預設走 **source generator（`[OptVar]`/`[OptParam]`）+ `OptModel` 模型定義**；同一模型可交給 `OptProject` 單次求解或 `OptExperiment` 做交叉實驗。需逐行掌控時可退回手寫。

## 入口與路線

任何 AI agent 先讀 [`AGENTS.md`](AGENTS.md)（通用入口）；規範單一來源在 `interactive/` 與 `automated/`。

| 路線 | 位置 | 何時用 |
| --- | --- | --- |
| **interactive**（有 gate，預設） | [`interactive/`](interactive/) | 一般開發：Modeling → Coding → Tuning 三階段 phase gate |
| **automated**（全自動 16-stage） | [`automated/`](automated/) | 大量量產 / 不需人在迴路：`00 → 14` 一路生到底 |

## 換機器設置（clone 後唯一要做的事）

```powershell
powershell -File scripts/setup-dlls.ps1
```

DLL 不進版控（商用 CPLEX + 建置產物）。上面這行自動偵測本機 CPLEX 安裝與 sibling `../OptimFoundation/` 建置輸出，把 6 個 DLL 放進 `dlls/`。手動步驟與執行期 PATH 注意事項見 [`dlls/README.md`](dlls/README.md)。

---

## 目錄結構

```text
AI-Modeling/
├── AGENTS.md                    ← 通用 AI agent 入口（任何 agent 先讀這）
├── CLAUDE.md                    ← Claude Code 入口 router（指向 AGENTS.md / interactive / automated）
│
├── interactive/                 ← ★ 路線一（預設）：三階段 phase gate 規範單一來源
│   ├── README.md                ←   天條層 + 三階段總綱
│   ├── phase-1-model-design.md  ←   建模（4 階段降維）
│   ├── phase-2-coding.md        ←   轉譯實作（解驗證協定）
│   ├── phase-3-tuning.md        ←   調校（solver / IIS / structure 決策）
│   └── linearization-patterns.md ←  8 類 constraint linearization 手法
├── automated/                   ← 路線二：全自動 16-stage pipeline（Prompts + specs + CLAUDE.md）
│
├── dlls/                        ← ★ 所有 DLL 唯一來源（不進版控；見 dlls/README.md）
│   └── README.md                ←   6 個 DLL 清單 + 佈置說明
├── scripts/
│   ├── setup-dlls.ps1           ← 一鍵佈置 dlls/（偵測 CPLEX + sibling OptimFoundation）
│   └── run.ps1                  ← 批次 build + run 所有 Projects/
│
├── CPLEX_API_REFERENCE.md       ← OptimFoundation 完整 API 參考（含速查卡）
├── claudemdTemplate/            ← 各資料夾規則的單一來源（few-shot 範本）
├── Template/                    ← 新專案起始範本（generator + 對稱 runner API）
├── Projects/                    ← 所有練習題目
│   ├── HospitalRostering_Generator/ ← 雙架構教學：generator + 注入 Action（預設）
│   ├── HospitalRostering_Manual/    ← 雙架構教學：手寫 class + Problem.Execute（後路）
│   └── …（GlassFactory / SandwichProduction / ClinicVitamin / …）
│
├── tutorial/                    ← 端到端教學（醫院排班，含兩架構對照）
├── tuning/                     ← CPLEX tuning 策略與旋鈕對照
└── specs/                       ← SDD 規格文件
```

---

## 環境需求

| 項目      | 版本                       |
| --------- | -------------------------- |
| .NET      | 8.0                        |
| IBM CPLEX | 22.1.1                     |
| IDE       | Visual Studio 2022 / Rider / VS Code |

> clone 後先跑 `scripts/setup-dlls.ps1` 佈置 `dlls/`（見上「換機器設置」）；不需另行安裝 NuGet 套件。框架本體唯讀，API 對照 `CPLEX_API_REFERENCE.md`。

---

## 快速開始

1. **複製範本**：把 `Template/` 複製到 `Projects/MyProject/`
2. **改 `csproj` 兩處相對路徑**（複製後都會多一層，不改會編不過）：DLL HintPath `..\dlls\` → `..\..\dlls\`；generator Analyzer `..\dlls\` → `..\..\dlls\`（範本 csproj 開頭也有此提示）
3. **先寫數學模型**：在 `Model/MyProject_Model.md` 完成 Sets/Parameters/Variables/Objective/Constraints（確認前不寫 `.cs`）
4. **再翻譯成 code**：變數/參數預設用 `[OptVar]`/`[OptParam]` 宣告
5. **執行**：
   - `dotnet run` — 求解
   - `dotnet run -- experiment` — 參數掃描（tuning）

### csproj 參考（標準寫法）

```xml
<ItemGroup>
  <!-- source generator：以 analyzer DLL 掛入（repo 根 dlls/） -->
  <Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />
</ItemGroup>
<ItemGroup>
  <Reference Include="ILOG.Concert"><HintPath>..\..\dlls\ILOG.Concert.dll</HintPath></Reference>
  <Reference Include="ILOG.CPLEX"><HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath></Reference>
  <Reference Include="NLog"><HintPath>..\..\dlls\NLog.dll</HintPath></Reference>
  <Reference Include="OptimFoundation.Core"><HintPath>..\..\dlls\OptimFoundation.Core.dll</HintPath></Reference>
  <Reference Include="OptimFoundation.Cplex"><HintPath>..\..\dlls\OptimFoundation.Cplex.dll</HintPath></Reference>
</ItemGroup>
```

---

## 專案標準結構

```text
Projects/MyProject/
├── MyProject.csproj
├── Program.cs                  ← 唯一組裝點（模型、單次求解、實驗）
│
├── Model/
│   └── MyProject_Model.md      ← 數學模型文件
│
├── Set/
│   └── Dataload.cs             ← Sets / Parameters / WriteToCSV
│
├── Parameter/
│   └── Parameter_Xxx.cs        ← [OptParam] 宣告（後路手寫 : ParameterBase）
│
├── Variable/
│   ├── VariableB_Xxx.cs        ← [OptVar] 宣告（後路手寫 : VariableBase）
│   ├── VariableX_Xxx.cs
│
├── Objective/
│   └── ObjectiveFunction.cs
│
└── Constraint/
    └── Constraint_Xxx.cs
```

### 執行流程（paved path）

```csharp
// Program.cs
var data = OptData.Load(() => new Dataload());

var projectConfig = new ProjectConfig
{
    ProjectName = "MyProject",
    EnableSolverLog = true,
    ExportSol = true,
};
var baseline = new CplexConfig { epGap = 0.03, timeLimit = 300, workThreads = 8 };

var model = new OptModel("baseline")
    .AddVariables(e =>
    {
        e.BuildBVs<VariableB_Xxx>(data.Items);
        e.BuildCVs<VariableX_Xxx>(data.Items);
    })
    .AddObjective(e => new ObjectiveFunction(data.Items, data.Profit, e).Build())
    .AddConstraints(e =>
    {
        new Constraint_Capacity(data.Items, data.Capacity, e).Build();
    });

if (args.Contains("experiment"))
{
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;
    new OptExperiment("my-project-tuning", "baseline vs emphasis")
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

> `Dataload` MUST `public partial class Dataload : DataContext`；NEVER 裸 `new Dataload()` 當建構終點——仍可編譯但跳過框架的參照完整性 / 重複 key / 數值 sanity 驗證。`OptData.Load` 驗證後會凍結框架受控的註冊 mutation API；既有 public fields 與可變 `List` 不保證在直接寫入當下攔截。細節見 [`CPLEX_API_REFERENCE.md`](CPLEX_API_REFERENCE.md) §7.6。
>
> 需逐行掌控引擎生命週期時，可退回手寫 `MyProblem : IDisposable` 的 `Execute()`（完整示範見 `Projects/HospitalRostering_Manual`）。

---

## 重點練習題目

| 專案 | 類型 | 說明 |
| ---- | ---- | ---- |
| [GlassFactory](Projects/GlassFactory/) | LP | 玻璃工廠 — 產能限制下最大化生產利潤 |
| [SandwichProduction](Projects/SandwichProduction/) | LP | 早餐店 — 有限食材下最大化三明治收益 |
| [ClinicVitamin](Projects/ClinicVitamin/) | MIP | 診所 — 維生素原料限制下最大化藥品利潤 |
| [HospitalRostering_Generator](Projects/HospitalRostering_Generator/) | MIP | 醫院排班 — **generator + 注入 Action（預設架構）** |
| [HospitalRostering_Manual](Projects/HospitalRostering_Manual/) | MIP | 醫院排班 — **手寫 class + Problem.Execute（後路架構）** |

> 兩個 HospitalRostering 專案用**同一份數學模型**、跑同一組 tuning 實驗，專門對照兩種建構方式。其餘練習見 `Projects/`，端到端教學見 `tutorial/`。

---

## 關鍵規則（天條）

1. **DLL 唯一來源**：所有 `csproj` HintPath 指向 repo 根 `dlls/`（佈置見 `dlls/README.md`）
2. **框架唯讀**：OptimFoundation 為編譯版 DLL，需擴充在專案端寫 helper，不改框架
3. **Parameter 資料夾必須存在**：Sets 由 Parameters 衍生
4. **禁止 Hardcode**：所有數值放 `Parameter.QTY`，Constraint / Objective 不得出現裸數字
5. **先模型後實作**：數學模型（`Model/`）確認前不寫 `.cs`
6. **WriteSolution 前先 CreateFolder**：`FolderDir.Solution.CreateFolder();`

---

## 參考文件

| 文件 | 說明 |
| ---- | ---- |
| [CLAUDE.md](CLAUDE.md) | AI 操作規範與天條 |
| [CPLEX_API_REFERENCE.md](CPLEX_API_REFERENCE.md) | OptimFoundation 完整 API 參考（含速查卡） |
| [claudemdTemplate/](claudemdTemplate/) | 各資料夾規則的單一來源（few-shot 範本） |
| [Template/CLAUDE.md](Template/CLAUDE.md) | 框架語法詳細範例（generator 預設） |
| [tutorial/](tutorial/) | 端到端教學（醫院排班 + 兩架構對照） |
| [tuning/CLAUDE.md](tuning/CLAUDE.md) | CPLEX tuning 策略與旋鈕對照 |
