# AI Modeling — OptimFoundation CPLEX

自然語言最佳化題目 → 可求解的 **OptimFoundation**（封裝 IBM ILOG CPLEX）C# 專案。
建模預設走 **source generator（`[OptVar]`/`[OptParam]`）+ `OptModel` 模型定義**；同一模型可交給 `OptProject` 單次求解或 `OptExperiment` 做交叉實驗。需逐行掌控時可退回手寫。

## 入口與路線

任何 AI agent 先讀 [`.claude/rules/AGENTS.md`](.claude/rules/AGENTS.md)（通用入口）；規範單一來源在 `.claude/rules/` 與 [`.claude/workflows/interactive/`](.claude/workflows/interactive/)。

**唯一路線是三階段 phase gate**，依序推進、每階段之間有 gate，NEVER 跳階或走免 gate 的全自動路線：

| 階段 | skill | 產物 |
| --- | --- | --- |
| Phase 1 Modeling | `/modeling` | `Model/<Project>_Model.md`，停在使用者確認 |
| Phase 2 Coding | `/coding` | 八資料夾專案，build 綠 + 解已驗證 |
| Phase 3 Tuning | `/tuning` | promotion 後的 baseline + `TuningHistory.md`（使用者提出才做） |

整體任務、三階段 I/O 契約與 `status.json` schema 見 [`.claude/rules/MILP DevPipeline/README.md`](.claude/rules/MILP%20DevPipeline/README.md)。

## 換機器設置（clone 後唯一要做的事）

DLL 不進版控（商用 CPLEX + 建置產物），所以 clone 後 `dlls/` 是空的，要放好 6 個 DLL 才能 build：CPLEX 兩顆從本機安裝複製，OptimFoundation 四顆先建 sibling `../OptimFoundation/` 再複製 `Release` 輸出。逐步指令與執行期 PATH 注意事項見 [`dlls/README.md`](dlls/README.md)。

---

## 目錄結構

```text
AI-Modeling/
├── CLAUDE.md                    ← Claude Code 入口 router（指向 .claude/）
├── README.md                    ← 本檔
├── ROADMAP.md                   ← 路線圖與待辦
│
├── .claude/                     ← ★ 所有 AI 規範、工作流與工具集中於此
│   ├── rules/                   ←   規範層（該怎麼做）
│   │   ├── AGENTS.md            ←     天條唯一權威（任何 AI agent 先讀這）
│   │   ├── milp-domain-rules.md ←     MILP domain 天條（跨三 phase）
│   │   ├── Ph1_Modeling/        ←     建模規範 + linearization 手法 + multi-agent 拓樸
│   │   ├── Ph2_Coding/          ←     API guide（唯一標準）+ 人工驗收 checklist + multi-agent 拓樸
│   │   └── Ph3_Tuning/          ←     調校規範 + CPLEX 旋鈕策略 + 文獻地圖
│   ├── workflows/               ←   開發流程
│   │   ├── interactive/         ←     ★ 唯一路線：三階段 phase gate
│   │   └── automated/           ←     已停用（16-stage 量產線），僅供歷史查閱
│   ├── reference/               ←   查閱資料
│   │   ├── CodeMap.md              ← 模型定義與 runner 架構地圖
│   │   └── baseline/               ← 遷移前基準值（驗收依據）
│   ├── skills/                  ←   /modeling · /coding · /tuning 三個 phase orchestrator
│   ├── commands/                ←   slash command
│   ├── agents/                  ←   subagent 定義
│   ├── hooks/                   ←   dotnet build 摘要 hook
│   └── evals/                   ←   behavioral eval cases
│
├── dlls/                        ← ★ 所有 DLL 唯一來源（不進版控；見 dlls/README.md）
│   └── README.md                ←   6 個 DLL 清單 + 佈置說明
├── Template/                    ← 新專案起始範本（generator + 對稱 runner API）
├── Projects/                    ← 所有練習題目
│   ├── HospitalRostering_Generator/ ← 雙架構教學：generator + 注入 Action（預設）
│   ├── HospitalRostering_Manual/    ← 歷史對照：手寫 class + Problem.Execute（已廢止，NEVER 照抄）
│   └── …（GlassFactory / SandwichProduction / ClinicVitamin / …）
│
├── tutorial(for developer)/     ← 端到端教學（醫院排班，含兩架構對照）
├── dataset/                     ← 題庫與資料集
└── specs/                       ← SDD 規格文件
```

---

## 環境需求

| 項目      | 版本                       |
| --------- | -------------------------- |
| .NET      | 8.0                        |
| IBM CPLEX | 22.1.1                     |
| IDE       | Visual Studio 2022 / Rider / VS Code |

> clone 後先照 [`dlls/README.md`](dlls/README.md) 佈置 `dlls/`（見上「換機器設置」）；不需另行安裝 NuGet 套件。框架本體唯讀，API 對照 `.claude/rules/Ph2_Coding/optimfoundation-api-guide.md` §9。

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
│   └── Parameter_Xxx.cs        ← [OptParam] + [OptDim] 宣告（generator 唯一路線）
│
├── Variable/
│   ├── VariableB_Xxx.cs        ← [OptVar] + [OptDim] 宣告（generator 唯一路線）
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

> `Dataload` MUST `public partial class Dataload : DataContext`；NEVER 裸 `new Dataload()` 當建構終點——仍可編譯但跳過框架的參照完整性 / 重複 key / 數值 sanity 驗證。`OptData.Load` 驗證後會凍結框架受控的註冊 mutation API；既有 public fields 與可變 `List` 不保證在直接寫入當下攔截。細節見 [`.claude/rules/Ph2_Coding/optimfoundation-api-guide.md`](.claude/rules/Ph2_Coding/optimfoundation-api-guide.md) §2.4 與 §9.2.4。
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
| [HospitalRostering_Manual](Projects/HospitalRostering_Manual/) | MIP | 醫院排班 — 手寫 class + Problem.Execute，**已廢止寫法、僅歷史對照** |

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
| [optimfoundation-api-guide.md](.claude/rules/Ph2_Coding/optimfoundation-api-guide.md) | Phase 2 唯一標準：端到端轉譯規範 + §9 完整 API 簽名／速查卡／黑名單 |
| [claudemdTemplate/](claudemdTemplate/) | 各資料夾規則的單一來源（few-shot 範本） |
| [Template/CLAUDE.md](Template/CLAUDE.md) | 框架語法詳細範例（generator 預設） |
| [tutorial(for developer)/](tutorial%28for%20developer%29/) | 端到端教學（醫院排班 + 兩架構對照） |
| [.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md](.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md) | CPLEX tuning 策略與旋鈕對照 |
