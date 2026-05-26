# ClaudeAIAssistant — OptimFoundation CPLEX 練習專案

以 **OptimFoundation** 框架（封裝 IBM ILOG CPLEX）建構整數線性規劃（ILP / MIP）模型的 C# 練習集。

---

## 目錄結構

```
ClaudeAIAssistant/
├── dlls/                        ← ★ 所有 DLL 唯一來源
│   ├── ILOG.Concert.dll
│   ├── ILOG.CPLEX.dll
│   ├── NLog.dll
│   ├── OptimFoundation.Core.dll
│   └── OptimFoundation.Cplex.dll
│
├── Foundation/                  ← OptimFoundation 框架原始碼（禁止修改）
│
├── Template_CPLEX/              ← 新專案起始範本
│
├── Projects/                    ← 所有練習題目
│   ├── GlassFactory/
│   ├── SandwichProduction/
│   ├── ClinicVitamin/
│   └── HospitalRosteringProblem/
│
├── CLAUDE.md                    ← AI 操作規範
└── CPLEX_API_REFERENCE.md       ← API 完整參考文件
```

---

## 環境需求

| 項目 | 版本 |
|------|------|
| .NET | 8.0 |
| IBM CPLEX | 22.1.1 |
| IDE | Visual Studio 2022 / Rider |

> DLL 已預置於 `dlls/`，不需另行安裝 NuGet 套件。

---

## 快速開始

1. **複製範本**：把 `Template_CPLEX/` 整個資料夾複製到 `Projects/MyProject/`
2. **確認 HintPath**：`csproj` 中的 DLL 路徑需指向 `..\..\dlls\`
3. **建立模型**：依照下方專案結構填寫各層
4. **執行**：`dotnet run` 或 Visual Studio 直接啟動

### csproj DLL 參考（標準寫法）

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

## 專案標準結構

```
Projects/MyProject/
├── MyProject.csproj
├── Program.cs
├── MyProblem.cs                 ← 主類別（IDisposable）
│
├── Model/
│   └── MyProject_Model.md       ← 數學模型文件
│
├── Set/
│   └── Dataload.cs              ← Sets / Parameters / WriteToCSV
│
├── Parameter/
│   └── Parameter_Xxx.cs         ← ParameterBase 子類別
│
├── Variable/
│   ├── VariableB_Xxx.cs         ← Binary 變數
│   ├── VariableX_Xxx.cs         ← Continuous 變數
│   └── VariableCreate.cs
│
├── Objective/
│   └── ObjectiveFunction.cs
│
└── Constraint/
    ├── BuildModel.cs
    └── Constraint_Xxx.cs
```

### 執行流程

```csharp
// Program.cs
using (var problem = new MyProblem())
    problem.Execute();

// MyProblem.Execute()
optEngine = new OptEngine(config);
optEngine.Build();                                    // 1. 初始化
new VariableCreate(dataload, optEngine).Build();      // 2. 建變數
new BuildModel(dataload, optEngine).Build();          // 3. 建模型
bool ok = optEngine.Solve();                          // 4. 求解
if (ok) dataload.WriteToCSV(optEngine);               // 5. 輸出
return ok;
```

---

## 練習題目

| 專案 | 類型 | 問題描述 |
|------|------|---------|
| [GlassFactory](Projects/GlassFactory/) | LP | 玻璃工廠 — 在加熱 / 冷卻機產能限制下，最大化生產利潤 |
| [SandwichProduction](Projects/SandwichProduction/) | LP | 早餐店 — 在有限食材庫存下，最大化三明治生產收益 |
| [ClinicVitamin](Projects/ClinicVitamin/) | MIP | 診所 — 在維生素原料限制下，最大化藥品生產利潤 |
| [HospitalRosteringProblem](Projects/HospitalRosteringProblem/) | MIP | 醫院排班 — 滿足每日人力需求，最小化排班違規懲罰 |

---

## 關鍵規則（天條）

1. **DLL 唯一來源**：所有 `csproj` HintPath 必須指向 `ClaudeAIAssistant\dlls\`
2. **禁止修改 Foundation**：`Foundation/` 目錄為唯讀，需要擴充在 Project 端寫 helper
3. **Parameter 資料夾必須存在**：每個專案都要有 `Parameter/` 層
4. **SaveSolutionToCSV 前必須 CreateFolder**：
   ```csharp
   FolderDir.Solution.CreateFolder();
   CsvCtrl.SaveSolutionToCSV<VarX>(engine, "DataId", "User");
   ```
5. **Execute 必須接收 bool 回傳值**：`bool ok = optEngine.Solve();`

---

## 參考文件

| 文件 | 說明 |
|------|------|
| [CPLEX_API_REFERENCE.md](CPLEX_API_REFERENCE.md) | OptimFoundation 完整 API 參考（含速查卡） |
| [CLAUDE.md](CLAUDE.md) | AI 操作規範與限制 |
| [Foundation/README.md](Foundation/README.md) | OptimFoundation 框架說明 |
| [Template_CPLEX/CLAUDE.md](Template_CPLEX/CLAUDE.md) | 框架語法詳細範例 |
