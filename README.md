# ClaudeAIAssistant — OptimFoundation CPLEX 練習專案

以 **OptimFoundation** 框架（封裝 IBM ILOG CPLEX）建構整數線性規劃（ILP / MIP）模型的 C# 練習集。
建模預設走 **source generator（`[OptVar]`/`[OptParam]`）+ Fluent `OptModel` 雙模式**，需逐行掌控時可退回手寫。

---

## 目錄結構

```text
ClaudeAIAssistant/
├── dlls/                        ← ★ 所有 DLL 唯一來源（編譯版 OptimFoundation，唯讀）
│   ├── ILOG.Concert.dll
│   ├── ILOG.CPLEX.dll
│   ├── NLog.dll
│   ├── OptimFoundation.Core.dll
│   └── OptimFoundation.Cplex.dll
│
├── CLAUDE.md                    ← AI 操作規範（天條）
├── CPLEX_API_REFERENCE.md       ← OptimFoundation 完整 API 參考（含速查卡）
├── claudemdTemplate/            ← 各資料夾規則的單一來源（few-shot 範本）
│
├── Template_CPLEX/              ← 新專案起始範本（generator + OptModel 雙模式）
├── Projects/                    ← 所有練習題目
│   ├── OptimModeling.Generators/    ← AutoSetsGenerator（source generator，analyzer）
│   ├── HospitalRostering_Generator/ ← 雙架構教學：generator + 注入 Action（預設）
│   ├── HospitalRostering_Manual/    ← 雙架構教學：手寫 class + Problem.Execute（後路）
│   └── …（GlassFactory / SandwichProduction / ClinicVitamin / WeeniesBuns / …）
│
├── tutorial/                    ← 端到端教學（醫院排班，含兩架構對照）
├── truning/                     ← CPLEX tuning 策略與旋鈕對照
└── specs/                       ← SDD 規格文件
```

---

## 環境需求

| 項目      | 版本                       |
| --------- | -------------------------- |
| .NET      | 8.0                        |
| IBM CPLEX | 22.1.1                     |
| IDE       | Visual Studio 2022 / Rider / VS Code |

> DLL 已預置於 `dlls/`，不需另行安裝 NuGet 套件。框架本體唯讀，API 對照 `CPLEX_API_REFERENCE.md`。

---

## 快速開始

1. **複製範本**：把 `Template_CPLEX/` 複製到 `Projects/MyProject/`
2. **改 `csproj` 兩處相對路徑**（複製後都會多一層，不改會編不過）：DLL HintPath `..\dlls\` → `..\..\dlls\`；generator 參考 `..\Projects\OptimModeling.Generators\` → `..\OptimModeling.Generators\`（範本 csproj 開頭也有此提示）
3. **先寫數學模型**：在 `Model/MyProject_Model.md` 完成 Sets/Parameters/Variables/Objective/Constraints（確認前不寫 `.cs`）
4. **再翻譯成 code**：變數/參數預設用 `[OptVar]`/`[OptParam]` 宣告
5. **執行**：
   - `dotnet run` — 求解
   - `dotnet run -- experiment` — 參數掃描（tuning）

### csproj 參考（標準寫法）

```xml
<ItemGroup>
  <!-- source generator：以 analyzer 掛入 -->
  <ProjectReference Include="..\OptimModeling.Generators\OptimModeling.Generators.csproj"
                    OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
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

1. **DLL 唯一來源**：所有 `csproj` HintPath 指向 `ClaudeAIAssistant\dlls\`
2. **框架唯讀**：OptimFoundation 為編譯版 DLL，需擴充在專案端寫 helper，不改框架
3. **Parameter 資料夾必須存在**：Sets 由 Parameters 衍生
4. **禁止 Hardcode**：所有數值放 `Parameter.QTY`，Constraint / Objective 不得出現裸數字
5. **先模型後實作**：數學模型（`Model/`）確認前不寫 `.cs`
6. **SaveSolutionToCSV 前先 CreateFolder**：`FolderDir.Solution.CreateFolder();`

---

## 參考文件

| 文件 | 說明 |
| ---- | ---- |
| [CLAUDE.md](CLAUDE.md) | AI 操作規範與天條 |
| [CPLEX_API_REFERENCE.md](CPLEX_API_REFERENCE.md) | OptimFoundation 完整 API 參考（含速查卡） |
| [claudemdTemplate/](claudemdTemplate/) | 各資料夾規則的單一來源（few-shot 範本） |
| [Template_CPLEX/CLAUDE.md](Template_CPLEX/CLAUDE.md) | 框架語法詳細範例（generator 預設） |
| [tutorial/](tutorial/) | 端到端教學（醫院排班 + 兩架構對照） |
| [truning/CLAUDE.md](truning/CLAUDE.md) | CPLEX tuning 策略與旋鈕對照 |
