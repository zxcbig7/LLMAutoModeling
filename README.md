# AI Modeling — OptimFoundation CPLEX

自然語言最佳化題目 → 可求解的 **OptimFoundation**（封裝 IBM ILOG CPLEX）C# 專案。
建模預設走 **source generator（`[OptVar]`/`[OptParam]`）+ Fluent `OptModel` 雙模式**，需逐行掌控時可退回手寫。

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
├── Template_CPLEX/              ← 新專案起始範本（generator + OptModel 雙模式）
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

1. **複製範本**：把 `Template_CPLEX/` 複製到 `Projects/MyProject/`
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
├── Program.cs                  ← 唯一進入點（solve / experiment 雙模式）
├── ExperimentRunner.cs         ← 參數掃描（tuning）
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
│   └── VariableCreate.cs       ← BuildBVs / BuildCVs（兩模式共用）
│
├── Objective/
│   └── ObjectiveFunction.cs
│
└── Constraint/
    ├── BuildModel.cs           ← 只呼叫各 Constraint.Build()（兩模式共用）
    └── Constraint_Xxx.cs
```

### 執行流程（預設：Fluent OptModel）

```csharp
// Program.cs
if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }

var dataload = new Dataload();
using (var m = new OptModel("MyProject")
    .UseConfig(() => new CplexConfig { epGap = 0.03, timeLimit = 300, workThreads = 8 })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

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
| [Template_CPLEX/CLAUDE.md](Template_CPLEX/CLAUDE.md) | 框架語法詳細範例（generator 預設） |
| [tutorial/](tutorial/) | 端到端教學（醫院排班 + 兩架構對照） |
| [tuning/CLAUDE.md](tuning/CLAUDE.md) | CPLEX tuning 策略與旋鈕對照 |
