---
title: 以 Tutorial 為 AI 開發模範架構 — 組裝集中 + Constraint 顯式依賴
status: draft
created: 2026-07-22
updated: 2026-07-22
modules: [projects, claudemdTemplate, governance]
---

# 以 Tutorial 為模範架構

## Summary

把 OptimFoundation `Templates/Tutorial` 定為 AI-Modeling 的**開發模範架構**，吸收它真正好管理的兩點——(D) 組裝集中在一個 `Model/<Name>Model.cs`、(E) Constraint ctor 顯式傳需要的積木——同時**保留** AI-Modeling 更清楚的部分（六段對映的資料夾命名、獨立 Objective/、一 Set 一檔）。

決策已於 2026-07-22 與 Vic 逐項確認，覆蓋 2026-07-15 wave2「資料夾不跟 template」的部分範圍（僅組裝方式與 Model/ 用途，資料夾命名維持不變）。

## Motivation / Why

Tutorial「比較好管理」的來源不是資料夾名稱，是**組裝集中**與**依賴顯式**：

- 現況 AI-Modeling 的模型組裝散在三處：`Variable/VariableCreate.cs`（建變數）+ `Constraint/BuildModel.cs`（建目標式+限制式）+ `Program.cs`（串接）。要看懂「這個模型由什麼組成」得跳三個檔。
- 現況每條 Constraint ctor 都收整包 `(Dataload, OptEngine)`，從裡面挖 `dataload.SET_A`——依賴看不見、無法單獨測、改 Dataload 形狀會牽動全部 constraint。

Tutorial 兩者都解掉：`Model/TutorialModel.cs` 一個類看完整組裝；Constraint ctor 只收它真正用到的積木。

## 定案（逐項，2026-07-22 Vic 確認）

| # | 面向 | 決定 | 動作 |
|---|---|---|---|
| D | 組裝方式 | **採 Tutorial**：每專案一個 `Model/<Name>Model.cs`，含 `CreateVariables(engine)` + `CreateModel(engine)` + `Build(engine)`，plug 進 `OptModel` | `VariableCreate.cs` 內容 → `CreateVariables`；`BuildModel.cs` 內容 → `CreateModel`；刪這兩檔；`Program.cs` 改用組裝類 |
| E | Constraint ctor | **採 Tutorial**：顯式傳積木 `Constraint_X(Set_A a, List<Parameter_Y> y, OptEngine e)`，不傳整包 Dataload | 改每條 Constraint 的 ctor + `CreateModel` 內的 call site |
| C | `Model/` 用途 | `Model/` 同時放 `<Name>_Model.md`（數學規格）+ `<Name>Model.cs`（組裝類） | 新增 .cs，.md 原地不動 |
| F | Set 檔案 | **維持一 Set 一檔**（不採 Tutorial 合併 `Sets.cs`） | 不動 |
| A | 資料夾命名 | **維持 `Set/Parameter/Variable/Constraint/Objective/`**（不改 `SetClass/…`） | 不動 |
| B | Objective 位置 | **維持獨立 `Objective/`**（不併進 Constraint/） | 不動 |

一致、不動的：`[OptSet<T>]` / `[OptVar]`+`[OptDim]` / `partial Dataload : DataContext` / `OptData.Load` / Set 欄位 ALL_CAPS / `parameter_x` / `Numeric.SafeRatio`。

## Scope

### In Scope

1. `claudemdTemplate/` 治理文件：新增 `Model/CLAUDE.md` 的組裝類規範（D）；改 `Constraint/CLAUDE.md` 的 ctor 規範（E）；`Root/CLAUDE.md` 的 Program 骨架改用組裝類；廢 `Variable/CLAUDE.md` 的 `VariableCreate` 段、`Constraint/CLAUDE.md` 的 `BuildModel` 串接段（併入 Model 組裝類）
2. `Projects/CLAUDE.md`：標準結構描述改為「組裝在 `Model/<Name>Model.cs`」
3. 8 個實作專案逐一遷移（D + E），每個 MUST build 綠 + 實跑目標值不變
4. `Template_CPLEX`：同步遷移為模範（它是新專案的複製種子）
5. `tutorial/` 與 `CPLEX_API_REFERENCE.md`：組裝範例對齊

### Out of Scope

- 資料夾改名（A）、Objective 併入（B）、Set 合併（F）——已定案維持現狀
- `MaxWeightIndependentSet` 的 Model/+Project/ 兩夾結構（獨立議題，本規格不碰其非標準結構，只在它遷 D/E 時順帶對齊）
- 數學語意——全程只改組裝/依賴形狀，NEVER 動移項、係數、方向、目標值

## Data Model / 目標結構（每專案遷移後）

```text
<Project>/
├── Program.cs              ← OptData.Load + OptModel，plug 組裝類
├── Model/
│   ├── <Name>_Model.md     ← 數學規格（不動）
│   └── <Name>Model.cs      ← 新增：組裝類（CreateVariables + CreateModel + Build）
├── Set/                    ← Set_* 積木（一檔一個）+ Dataload.cs
├── Parameter/              ← Parameter_*
├── Variable/               ← Variable_*（刪 VariableCreate.cs，內容進組裝類）
├── Objective/             ← ObjectiveFunction.cs
└── Constraint/            ← Constraint_*（刪 BuildModel.cs，內容進組裝類；ctor 改顯式積木）
```

## 遷移 pattern（executor 照抄）

### D：組裝集中

```csharp
// Model/<Name>Model.cs（新增）
public class WeeniesBunsModel
{
    private readonly Dataload _d;
    public WeeniesBunsModel(Dataload d) => _d = d;

    public void CreateVariables(OptEngine engine) // ← 原 VariableCreate.Build() 內容
    {
        engine.BuildVars<VariableX_Production>(_d.PRODUCTTYPE);
        Logging.Info($"Variables created: {engine.varCount}");
    }

    public void CreateModel(OptEngine engine) // ← 原 BuildModel.Build() 內容
    {
        new ObjectiveFunction(_d.parameter_ProductSpec, engine).Build();
        new Constraint_Flour(_d.parameter_ProductSpec, _d.FlourCapacity, engine).Build(); // [C1] ≤
        new Constraint_Pork(_d.parameter_ProductSpec, _d.PorkSupply, engine).Build();     // [C2] ≤
        new Constraint_Labor(_d.parameter_ProductSpec, _d.LaborHours, engine).Build();    // [C3] ≤
    }

    public void Build(OptEngine engine) { CreateVariables(engine); CreateModel(engine); }
}
```

```csharp
// Program.cs
var dataload = OptData.Load(() => new Dataload());
var def = new WeeniesBunsModel(dataload);
using var model = new OptModel("WeeniesBuns")
    .UseConfig(() => new CplexConfig { ... })
    .AddVariables(def.CreateVariables)
    .AddModel(def.CreateModel)
    .OnSolved(e => dataload.WriteToCSV(e));
bool ok = model.Execute();
```

### E：Constraint 顯式依賴

```csharp
// ✓ Tutorial 式：只收用到的積木
public class Constraint_Flour : ConstraintBase
{
    private readonly List<Parameter_ProductSpec> _spec;
    private readonly double _flourCap;
    private readonly OptEngine _engine;

    public Constraint_Flour(List<Parameter_ProductSpec> spec, double flourCap, OptEngine engine)
    {
        _spec = spec;
        _flourCap = flourCap;
        _engine = engine;
    }
    // Build() 用 _spec / _flourCap，不再 dataload.xxx
}
```

```csharp
// ✗ 現況：收整包 Dataload，從裡面挖
public Constraint_Flour(WeeniesBunsDataload dataload, OptEngine engine) { ... }
```

## Acceptance Criteria

1. 每個遷移後專案：`Model/<Name>Model.cs` 存在，`Variable/VariableCreate.cs` 與 `Constraint/BuildModel.cs` 已刪
2. 每條 Constraint ctor 不再出現 `Dataload` 型別參數（grep `Constraint_.*Dataload` 於各 constraint ctor 零命中）
3. `Program.cs` 用 `new <Name>Model(dataload)` + `.AddVariables(def.CreateVariables).AddModel(def.CreateModel)`
4. 每個專案 `dotnet build` 0 error
5. 每個專案 `dotnet run` 目標值與遷移前**逐一比對相同**（LP 類 + MWIS=1332 + Template_CPLEX=158；有 baseline 的先記錄再改）
6. `claudemdTemplate/` 與 `Projects/CLAUDE.md`、`CPLEX_API_REFERENCE.md`、`tutorial/` 的組裝教法一致指向「Model 組裝類」，無殘留 `VariableCreate`/`BuildModel 串接` 舊教法
7. 無欄位對齊、無 BOM

## 執行計畫（phase）

- **Phase 0 — Pilot**：先遷 `WeeniesBuns`（最小，3 constraint）完整走完 D+E，build+run 驗目標值不變。確認 pattern 可行、無語意陷阱，才往下。
- **Phase 1 — 治理文件**：pilot 成功後，把定版 pattern 寫進 `claudemdTemplate/` + `Projects/CLAUDE.md`（先立規範，後續專案照抄）
- **Phase 2 — 其餘專案批次**：ClinicVitamin / SandwichProduction / GlassFactory / FactorioOptimization / HospitalRostering_Generator / HospitalRostering_Manual / MaxWeightIndependentSet + Template_CPLEX，逐專案 build+run 驗。派 subagent 做，每個結尾 verifier。
- **Phase 3 — 文件對齊**：tutorial / CPLEX_API_REFERENCE 組裝範例同步

## Edge Cases & Risks

- **E 是最高成本項**：每條 constraint 都要改 ctor + call site，跨 8 專案數十檔。Pilot 先驗值得不值得
- **lambda return→continue 陷阱**：若順帶把 `.ForEach` 改 `foreach`（非必要）要小心（Template_CPLEX 遷移踩過）——本規格**不**要求動 `.ForEach`（List 上合法），減少風險面
- **目標值回歸**：無測試網，靠人工比對 run 輸出。建議先對每專案跑一次記下 baseline 目標值再動手（呼應 `2026-07-21-newcomer-onboarding.md` 的 `expected.json` 構想——兩份規格可合流）
- **HospitalRostering 雙版**：Generator 與 Manual 共用同一數學模型，遷移後仍須保持兩版對照關係

## 通用鐵則

- NEVER 動數學語意（移項/係數/方向/目標值）
- NEVER 欄位對齊（含 code fence 內）、NEVER BOM
- 每 phase 結尾派 verifier fresh-context 驗收後才進下一 phase
