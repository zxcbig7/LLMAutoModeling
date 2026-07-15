# Phase 2 · Foundation Coding — 轉譯實作規範

> 天條（LHS/RHS 鐵則、禁 Hardcode、命名對應）見 [`README.md`](README.md)；本檔放結構與 API 細節。
> API 簽名的權威來源：[`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md)（repo 內主要）；`developer-guide.md` 在 sibling 資料夾 `../../OptimFoundation/`（進階補充，非硬相依）。

## 系統脈絡

三階段的第二階段：把已確認的 `Model/<Project>_Model.md` **逐條機械轉譯**成 OptimFoundation CPLEX C# 專案。

## 硬規則

- MUST 專案建在 [`../Projects/`](../Projects/)`<Project>/`，DLL 一律參考 [`../dlls/`](../dlls/)（csproj HintPath `..\..\dlls\Xxx.dll`）—— Why: DLL 唯一來源天條，路徑亂掉 build 就靠運氣
- MUST `Parameter\` 資料夾必須存在；Sets 由 Parameters 衍生（`=> parameter_Xxx.Select(...).Distinct().ToList()`）—— Why: 資料只進一次，Set 與 Parameter 永不失同步
- MUST 建模方式預設 source generator（`[OptVar]` / `[OptParam]`）+ Fluent `OptModel`；需逐行掌控引擎生命週期才退回手寫 `: VariableBase` / `XxxProblem.Execute()` —— 範例見 `../Projects/HospitalRostering_Generator` 與 `../Projects/HospitalRostering_Manual`
- MUST 參數讀取先 LINQ 存局部變數再傳入 `AddLHS` / `AddRHS`，NEVER 把 LINQ 內嵌在呼叫裡 —— Why: 可 debug、可驗值，內嵌讀不出中間值
- MUST `BuildModel.cs` 只呼叫各 `Constraint_Xxx.Build()` 與 `ObjectiveFunction`，NEVER 在裡面直接寫 `AddLHS` / `AddRHS`
- MUST Variable class 只放 properties、不寫 constructor（框架用 reflection 組 key）
- MUST build 失敗走 fix loop：擷取 compiler error → 修正 → 重 build，**至多 5 次**；仍失敗 → 停下回報，NEVER 硬掰

## 專案結構（五資料夾，逐條對應 Model.md）

```text
Projects/<Project>/
├── <Project>.csproj # DLL HintPath ..\..\dlls\
├── Program.cs # 唯一進入點（solve / experiment 雙模式）
├── ExperimentRunner.cs # Phase 3 參數掃描用
├── Model/ # <Project>_Model.md + Glossary.md（Phase 1 產物）
├── Set/ # Dataload.cs（Sets 由 Parameters 衍生 + WriteToCSV）
├── Parameter/ # Parameter_Xxx.cs（[OptParam] 生成，QTY 欄位）
├── Variable/ # VariableB_/X_/I_Xxx.cs + VariableCreate.cs
├── Objective/ # ObjectiveFunction.cs
└── Constraint/ # Constraint_Xxx.cs + BuildModel.cs
```

Namespace = `<Project>.<資料夾名>`（根目錄 = `<Project>`）。

## Program.cs 骨架（預設：Fluent OptModel 雙模式）

```csharp
if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }

var dataload = new Dataload();
using (var m = new OptModel("<Project>")
    .UseConfig(() => new CplexConfig { epGap = 1e-4, timeLimit = 300, workThreads = 8, enableLog = true, exportSol = true })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

`VariableCreate` / `BuildModel` 被 solve 與 experiment 兩模式共用，模型不重複。

## 轉譯順序

1. Parameter（Model.md Parameters 表逐列）→ 2. Dataload（數值保真）→ 3. Variable + VariableCreate → 4. Constraint 逐條（一條/一組邏輯相關 = 一個 `Constraint_Xxx.cs`）→ 5. ObjectiveFunction → 6. BuildModel → 7. Program.cs → 8. `dotnet build` → fix loop ≤5 → `dotnet run` → **解驗證協定**（見下）

## Pool API（限制式）

```csharp
foreach (var e in dataload.Employees)
{
    foreach (var d in dataload.Dates)
        engine.AddLHS(1.0, new VariableB_Assign { EMPLOYEE = e, DATE = d });
    var maxDays = dataload.parameter_MaxWorkDays.FirstOrDefault(p => p.EMPLOYEE == e)?.QTY ?? 0.0;
    engine.AddRHS(maxDays);
    engine.CreateLessEqual($"MaxWorkDays@{e}");
    ConstraintCount++;
}
```

- 約束名格式：`ConstraintName@index1@index2`
- RHS 含變數（移項需求）→ `AddRHS(coef, variable)`，但 Model 原式怎麼寫就怎麼放，NEVER 自行移項

## 取解 API（存在的才用）

```csharp
double obj = engine.GetObjectiveValue();
var sol = engine.GetSetVarValues<VariableX_Production>(); // {"VariableX_Production@Regular": 60.0}
double v = engine.GetVariableValue("VariableB_Assign@E1@2026-01-01"); // DateTime 格式 @yyyy-MM-dd
FolderDir.Solution.CreateFolder(); // ★ WriteSolution 前必呼叫
CsvCtrl.WriteSolution<VariableX_Production>(engine, "<Project>", "USER");
```

## 禁止使用（Foundation 不存在這些方法）

```csharp
// ✗ engine.GetVarSol(...) → 不存在
// ✗ engine.GetSetVarSol<T>() → 不存在
// ✗ CsvCtrl.SaveToCSV<T>(...) → 不存在（正確：WriteSolution）
```

簽名有疑慮 → 查 [`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md)，NEVER 憑記憶發明 API。

## good/bad：參數讀取

✅ Good
```csharp
var profit = dataload.parameter_Profit.FirstOrDefault(p => p.GLASS_TYPE == g)?.QTY ?? 0.0;
engine.AddLHS(profit, new VariableX_Production { GLASS_TYPE = g });
```

❌ Bad
```csharp
engine.AddLHS(dataload.parameter_Profit.First(p => p.GLASS_TYPE == g).QTY, new VariableX_Production { GLASS_TYPE = g }); // LINQ 內嵌
engine.AddLHS(300.0, new VariableX_Production { GLASS_TYPE = g }); // 裸數字
```

## 解驗證協定（`dotnet run` 後必跑，全過才算「會動」）

MUST 依序四步，任一不過 → 停下回報，NEVER 宣稱完成：

1. **Status 三分診斷**
   - `Optimal` → 進第 2 步
   - `Infeasible` → 回報並走 [`phase-3-tuning.md`](phase-3-tuning.md) 的 IIS 流程；先自查 big-M 是否太小、有無互斥硬約束
   - `Unbounded` → 某方向漏了界；查該變數 UB 或漏掉的上限約束
2. **可行性代回**：取解值代回**每一條** constraint，確認 LHS op RHS 成立（含 soft/big-M）—— Why: solver 回 Optimal 只保證它解的模型可行，不保證那模型 = 題目
3. **單位一致性**：目標值與關鍵變數的單位跟題目一致（利潤=錢、產量=件、工時=時），量級合理
4. **LP bound sanity**：目標值落在 LP relaxation bound 對的一側（max 問題：整數解 ≤ LP bound；min 問題：整數解 ≥ LP bound）；差太離譜 → 疑 big-M / 係數錯

✅ Good：四步都過 + 目標值對照 Model.md 手算小例
❌ Bad：看到 `Status == Optimal` 就回報「解出來了」（沒代回、沒對單位）

## 常見任務

- 新專案起手 → 依 [`../claudemdTemplate/`](../claudemdTemplate/) 各資料夾模板 + 參考 `../Projects/HospitalRostering_Generator`
- csproj DLL 區塊 → 抄範例專案的 `<ItemGroup>`（五個 Reference：ILOG.Concert / ILOG.CPLEX / NLog / OptimFoundation.Core / OptimFoundation.Cplex）
- build 錯 CS0246（找不到型別）→ 先檢查 HintPath 相對層數（Projects 下是 `..\..\dlls\`）

## Fatal
- NEVER 自行詮釋 Model.md（歧義 → 回 Phase 1）
- NEVER 裸數字進 Constraint / Objective
- NEVER 用別的 DLL 路徑
- NEVER fix loop 超過 5 次還繼續硬修
- NEVER 改 OptimFoundation 框架本體
</content>
