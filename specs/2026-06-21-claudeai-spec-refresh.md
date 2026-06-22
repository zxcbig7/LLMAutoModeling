---
title: 以 Fluent OptModel 為基準，刷新 ClaudeAIAssistant 開發規範與專案（含可跑 tuning 架構）
status: approved
created: 2026-06-21
updated: 2026-06-21
modules: [docs-claudemd, csharp-projects, truning, tuning-harness]
scope: CPLEX only
---

# ClaudeAIAssistant 開發規範刷新（Fluent OptModel + 可跑 Tuning 架構）

## Summary

把 ClaudeAIAssistant 的開發規範與**全部** 8 個題目專案，對齊現在的 OptimFoundation 框架：
① composition root 統一成框架內建的 **Fluent `OptModel`**；
② 每個專案標配**可實際跑的 tuning 架構**（`ExperimentRunner` + `Program` 模式切換，
建構於框架既有的 `Experiment` / `Trial.Capture` / `ITunableConfig` harness）。
範圍限 CPLEX。交付：刷新各 CLAUDE.md、完善 `truning/`、8 專案全面套新版、待刪清單。

## Motivation / Why

1. **兩套 composition pattern 並存**：框架的 [OptModel.cs](../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptModel.cs) 閒置，專案各自手寫 `XxxProblem.Execute()`。
2. **命名分歧**：`Set/` vs `Data/`（Factorio 重複）、`Constraint/` vs `Constraints/`、`Variable/` vs `VariablesClass/`、`ProjectName.*` vs `SandBox.*`/`MyApp`。
3. **claudemdTemplate 落後框架**：Root 教手寫 pattern；Constraint 缺 `CreateRange`；`Data/` 命名與實際 `Set/` 不符。
4. **tuning 能力未標準化**：框架已有完整 experiment harness（`Experiment`/`Trial`/`SolveMetrics`/`ITunableConfig`），且 [ExperimentRunner.cs](../Projects/HospitalRosteringProblem/ExperimentRunner.cs) 已是可用範例，但只此一專案有、且用舊 namespace；其餘專案不能跑 tuning。

## Scope

### In Scope

- claudemdTemplate（master 範本）對齊新 pattern + 命名標準 + **新增 tuning 架構規範**
- `truning/CLAUDE.md` 完善（對齊現行 `CplexConfig`、串接可跑的 `ExperimentRunner` 架構）
- `Projects/` **全部 8 個**專案 .cs 套新版：Fluent OptModel + 標準資料夾/namespace + `ExperimentRunner`
- `Template_CPLEX` 同步成 canonical 範本（solve 模式 + experiment 模式）
- 各專案 per-folder CLAUDE.md 校正
- 產出「需手動刪除舊資料」清單

### Out of Scope

- Gurobi / Solver 端（本次只做 CPLEX）
- **修改 OptimFoundation 框架 DLL**：tuning 架構只「使用」既有 `Experiment`/`Trial`/`ITunableConfig`，不擴充框架；truning §5 列的框架缺口（MIP start、callback 等）不在此次補
- 本環境 build / 跑 CPLEX 驗證（決議：pattern 對齊 + 人工 review，build 由使用者本機跑）
- 既有數學模型（Model/*.md）數學內容變更

## User Stories / Use Cases

1. As 框架維護者, 我要 AI 產新題目時只有一種 composition pattern + 一種 tuning 架構, so that 產出一致、可直接套框架。
2. As 開發者, 我要每個專案 `dotnet run` 求解、`dotnet run -- experiment` 掃參數, so that 不必為每題重寫 tuning glue。
3. As 使用者, 我要明確「待手動刪除」清單, so that 安全清掉舊 code / 重複資料。

## Acceptance Criteria

**規範文件**
- [ ] claudemdTemplate/Root 改用 Fluent OptModel 為唯一 solve composition root，並說明 `Program` 的 solve / experiment 雙模式
- [ ] claudemdTemplate 新增 **Experiment/CLAUDE.md**：規範 `ExperimentRunner`（variants 掃描 + `Trial.Capture` + `Experiment.Save`）
- [ ] claudemdTemplate/Constraint 補 `CreateRange(lb, ub, name)` 窗口
- [ ] claudemdTemplate `Data/` 更名 `Set/`，namespace `ProjectName.Set`，Dataload 歸 `Set/`
- [ ] claudemdTemplate 其餘（Variable/Parameter/Objective/Model）與框架 API 校正無誤
- [ ] `truning/CLAUDE.md`：✅/❌ 表逐欄對齊現行 `CplexConfig`；新增「`UseConfig()` 內套 tuning」與「`ExperimentRunner` 掃描旋鈕」範例段，連結 experiment harness

**程式（全部 8 專案）**
- [ ] 每個專案 `Program.cs` = 唯一 composition root，支援 `dotnet run`（solve via OptModel）與 `dotnet run -- experiment`（ExperimentRunner）
- [ ] 對應 `XxxProblem.cs` 刪除（其職責拆入 Program + 共用 build-step class）
- [ ] 每個專案具備 `ExperimentRunner.cs`，掃描至少 baseline + 數組 `CplexConfig` 變體，輸出 `Experiments/<name>.csv + .json`
- [ ] 8 專案資料夾統一 `Model/ Parameter/ Set/ Variable/ Objective/ Constraint/`，namespace 統一 `ProjectName.*`
- [ ] `VariableCreate` / `BuildModel` 為 solve 與 experiment 兩模式共用（不重複建模邏輯）
- [ ] `Template_CPLEX` 同步成 canonical 範本（雙模式）
- [ ] 每個改動檔附 before/after 重點 diff 說明（人工 review 驗收）

**交付**
- [ ] 「需手動刪除舊資料」清單（重複 `Data/`、build 產物、廢棄專案 / 檔）

## Module Interactions

- **claudemdTemplate/**（docs）：master few-shot 範本 → 改 Root/Constraint、Data→Set 更名、**新增 Experiment/**
- **truning/**（docs）：CPLEX tuning 策略 → 對齊 `CplexConfig` + 串 `ExperimentRunner`
- **Projects/**（C# code）：8 專案 → Program 雙模式 + 刪 XxxProblem + ExperimentRunner + 命名對齊
- **Template_CPLEX/**（C# code）：canonical 範本 → 雙模式同步
- **OptimFoundation.Core / .Cplex**（外部框架，唯讀引用）：
  - `OptModel`（solve composition root）
  - `Experiment` / `Trial.Capture` / `SolveMetrics` / `ConfigSnapshot` / `ITunableConfig`（tuning harness）

## API Design（本案的「契約」= 新 pattern 雙模式樣板）

### Program.cs — 雙模式 composition root

```csharp
// Program.cs — 唯一進入點：solve / experiment 兩模式
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;
using WeeniesBuns.Constraint;

if (args.Contains("experiment"))
{
    ExperimentRunner.Run();              // dotnet run -- experiment
    return;
}

// 一般求解：Fluent OptModel
var dataload = new WeeniesBunsDataload();
using (var m = new OptModel("WeeniesBuns")
    .UseConfig(() => new CplexConfig { epGap = 0.0, timeLimit = 60, workThreads = 4, enableLog = true, exportSol = true })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

### ExperimentRunner.cs — 可跑的 tuning 架構（建構於框架 harness）

```csharp
// 掃描多組 CplexConfig，每組一次 Trial.Capture，累積成 Experiment 輸出 csv+json
public static class ExperimentRunner
{
    public static void Run()
    {
        Logging.SetLogFileName("WeeniesBuns_Experiment");
        var exp = new Experiment("weeniesbuns-tuning", "掃 emphasis/varSel/nodeSelect/gap/threads/seed");

        var variants = new (string label, Action<CplexConfig> tune)[]
        {
            ("baseline",           _ => { }),
            ("emphasis=optimal",   c => c.Emphasis   = 2),   // ITunableConfig 抽象旋鈕
            ("varsel=strong",      c => c.varSel     = 3),   // CPLEX 專屬欄位
            ("nodesel=bestbound",  c => c.nodeSelect = 1),
            ("seed=20260621",      c => c.Seed       = 20260621),
        };

        foreach (var (label, tune) in variants)
        {
            var config = new CplexConfig { epGap = 0.03, timeLimit = 60, workThreads = 8, enableLog = false };
            tune(config);

            var dataload = new WeeniesBunsDataload();      // 每 Trial fresh，避免狀態污染
            using var engine = new OptEngine(config);
            engine.Build();
            new VariableCreate(dataload, engine).Build();  // ← 與 solve 模式共用
            new BuildModel(dataload, engine).Build();      // ← 與 solve 模式共用

            exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
        }

        exp.Save();   // → Experiments/weeniesbuns-tuning.csv + .json
    }
}
```

### 標準資料夾 / namespace

| 資料夾 | 內容 | namespace | 廢棄別名 |
|--------|------|-----------|----------|
| `Model/` | 數學模型 `.md` | — | — |
| `Parameter/` | `ParameterBase` 子類別（QTY） | `ProjectName.Parameter` | — |
| `Set/` | `XxxDataload`（Sets 由 Parameters 衍生） | `ProjectName.Set` | `Data/` |
| `Variable/` | `VariableB_/X_/I_` + `VariableCreate` | `ProjectName.Variable` | `VariablesClass/` |
| `Objective/` | `ObjectiveFunction` | `ProjectName.Objective` | — |
| `Constraint/` | `ConstraintBase` 子類別 + `BuildModel` | `ProjectName.Constraint` | `Constraints/` |

頂層：`Program.cs`（雙模式）、`ExperimentRunner.cs`。廢棄 `XxxProblem.cs`、`SandBox.*`/`MyApp`/`OptimModeling` namespace。

## Data Model（專案盤點：現狀 → 動作；全部套新版）

| 專案 | 現狀 | 動作 | 風險 |
|------|------|------|------|
| WeeniesBuns | 結構新、手寫 Execute | Program 雙模式；刪 Problem；加 ExperimentRunner | 低 |
| SandwichProduction | 同上 | 同上 | 低 |
| ClinicVitamin | 同上 | 同上 | 低 |
| GlassFactory | 同上 | 同上 | 低 |
| FactorioOptimization | 同上 + **`Set/`/`Data/` 重複** | 同上 + 留 `Set/`、刪 `Data/`、修引用 | 中 |
| HospitalRosteringProblem | 最舊：`SandBox.*`、`Constraints/`、`VariablesClass/`、多 Runner；**已有 ExperimentRunner** | 全面改名 + OptModel + 整理 Runner 為標準 | 高 |
| HospitalRosteringProblem_new | 舊 namespace + `Generated/` codegen | 全面改名 + OptModel + ExperimentRunner | 高 |
| MaxWeightIndependentSet | 非標準：`stages/ csharp/ experiments/` | 重構為標準六資料夾 + 雙模式（保留 stages 數學文件入 Model/） | 高 |

## Edge Cases & Error Handling

- **OptModel 一次性 vs tuning 需重建**：OptModel 內部自建/Dispose engine，不適合掃參數；故 tuning 用 `ExperimentRunner` 手動持有 engine。兩者**共用** `VariableCreate`/`BuildModel`，建模邏輯不重複。
- **每 Trial fresh Dataload + engine**：避免 pool / 變數狀態跨 Trial 污染（沿用既有 ExperimentRunner 做法）。
- **掃描時關 log / exports**：`enableLog=false`、不輸出 LP/Sol 以加速；`timeLimit` 確保每 Trial 收斂。
- **Factorio 重複 Dataload**：先 `diff Set/Dataload.cs Data/Dataload.cs` 確認留哪份再刪。
- **MWIS 結構落差大**：stages/ 內若有非建模產物（中間檔、實驗 log），歸入待刪清單而非硬塞標準資料夾。
- **build 不可驗**：本環境無 CPLEX DLL / msbuild → 人工 review + 明確 diff 交付，使用者本機 build。

## Non-Functional Requirements

- **一致性**：8 專案 + Template_CPLEX + claudemdTemplate 三者 pattern / 命名 / tuning 架構零分歧
- **可回溯**：每改動檔附 before/after 重點
- **不破壞數學語意**：只動 composition / 命名 / 新增 tuning glue，不動約束數學與係數來源（QTY 天條不變）

## Open Questions（已於釐清回合拍板）

- ✅ Hospital×2 與 MWIS：**全部套新版**（不再保留舊結構）
- ✅ tuning：**要可實際跑的架構**（ExperimentRunner + Program 雙模式），非僅文件

## Implementation Plan

### Stub 階段（approve 後第一步）

- [ ] 產 `CodeMap.md`（claudemdTemplate + Template_CPLEX）作為重構地圖
- [ ] **Template_CPLEX 改成 canonical 雙模式範本**：Program（solve/experiment）+ 新增 `ExperimentRunner.cs` + `Set/` 命名；刪 `TemplateProblem.cs`
- [ ] claudemdTemplate：改 **Root**（雙模式 composition）、**Data→Set 更名**、**新增 Experiment/CLAUDE.md**；其餘標 TODO
- [ ] 以 **WeeniesBuns 當 pilot** 走完整雙模式重構，回報 diff 確認 pattern 無誤（不一次改 8 個）

### 逐步實作（pilot 確認後）

- [ ] claudemdTemplate 其餘 CLAUDE.md（Constraint 補 CreateRange、Variable/Parameter/Objective/Model 校正）
- [ ] `truning/CLAUDE.md` 完善（CplexConfig 對齊 + ExperimentRunner 範例 + harness 連結）
- [ ] 低風險 4 專案（SandwichProduction/ClinicVitamin/GlassFactory/Factorio）
- [ ] 高風險 3 專案（Hospital×2 + MWIS）逐案重構 + 回報
- [ ] 各專案 per-folder CLAUDE.md 校正
- [ ] 彙整「需手動刪除舊資料」清單交付

## 需手動刪除舊資料（初版，Step 4 逐案補證據）

> 我會在重構時刪掉「明確由新 pattern 取代」的檔（如 `XxxProblem.cs`、重複 `Data/Dataload.cs`）。
> 下列**留給你手動拍板刪除**（風險高 / 屬你的決定 / 大批產物）：

- **重複死碼**：`Projects/FactorioOptimization/Data/`（與 `Set/` 重複，確認後）
- **MWIS 非建模殘留**：`MaxWeightIndependentSet/stages|csharp|experiments` 內的中間檔 / 舊實驗產物（重構後逐項列）
- **build 產物（會重生）**：各專案 `bin/ obj/ .vs/ packages/`
- **舊 namespace 殘檔**：Hospital 系列重構後遺留的 `SandBox`/`MyApp` 舊檔（列出供確認）

## References

- solve composition root：[OptModel.cs](../../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptModel.cs)
- tuning harness：[Experiment.cs](../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiment.cs)、[Trial.cs](../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/Trial.cs)、[ITunableConfig.cs](../../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/ITunableConfig.cs)
- tuning 可跑範例（待汰換 namespace）：[ExperimentRunner.cs](../Projects/HospitalRosteringProblem/ExperimentRunner.cs)
- 待汰換手寫 pattern：[Template_CPLEX/TemplateProblem.cs](../Template_CPLEX/TemplateProblem.cs)、[WeeniesBunsProblem.cs](../Projects/WeeniesBuns/WeeniesBunsProblem.cs)
