---
title: 用兩章教學對比「手動架構」與「Generator + 注入 Action 架構」，並以同一份醫院排班模型各建一個可跑專案 + 同組 tuning
status: approved
created: 2026-06-22
updated: 2026-06-22
modules: [tutorial-docs, csharp-projects, tuning-harness, docs-claudemd]
scope: CPLEX only
supersedes_decision: 2026-06-21-claudeai-spec-refresh（部分推翻：由「唯一 OptModel pattern」改為「雙 pattern 並存，AI 預設偏好 Generator 版」）
---

# 雙架構教學：同一份醫院排班模型，手動 vs Generator+注入 Action

## Summary

把現有醫院排班教學（`tutorial/index.html`）擴成**兩章並排對比兩種建構方式**：
（A）**手動架構**——手寫 `XxxProblem.Execute()` 自接 `OptEngine`、手寫每個 Variable/Parameter class；
（B）**Generator + 注入 Action 架構**——`[OptVar]`/`[OptParam]` 由 `AutoSetsGenerator` 編譯期生成 class，Fluent `OptModel` 用注入的 `Action<OptEngine>` 組裝。
兩者用**完全相同的數學模型**（醫院排班完整版，~10 條限制式 + 加權懲罰），在 `Projects/` 各建一個**從頭到尾可跑**的專案、跑**同一組 tuning 實驗**，最後讓 `CLAUDE.md` **兩種建構方式都支援**，但明示 **AI 預設偏好 B（較簡單）**。

## Motivation / Why

1. **教學只有單一寫法、看不出取捨**：現行教學只示範一種組裝法，讀者無法理解「為什麼要 generator / OptModel」。並排同一模型最能凸顯差異。
2. **框架實際同時存在兩套**：`AutoSetsGenerator`（source generator）與手寫 class、`OptModel`（Fluent）與手寫 `Problem.Execute()` 都在 repo 內。與其假裝只有一套，不如教清楚兩套的邊界與取捨。
3. **2026-06-21 spec-refresh 過度收斂**：該份 spec 把手動 `XxxProblem` 列為唯一要淘汰的 pattern。實務上手動版仍是理解框架的最佳教材、也是 generator 不適用時的後路。本案改為**雙 pattern 並存**，但規範層面引導 AI 預設走 B。

## Scope

### In Scope

- **教學（`tutorial/`）**：新增/重構出**兩章**，各自用同一份醫院排班模型，端到端走完架構 A 與架構 B；外加一節並排對照（含 tuning 結果對比）。
- **程式（`Projects/`）**：建立兩個可跑專案
  - `HospitalRostering_Manual`（架構 A）
  - `HospitalRostering_Generator`（架構 B）
  - 兩者共用同一份 `Model/HospitalRostering_Model.md`（數學內容字字相同）。
- **Tuning**：兩專案**都**雙模式（`dotnet run` 求解 / `dotnet run -- experiment` 掃描），跑**同一組** `CplexConfig` variants，各自輸出 `Experiments/*.csv + .json`。
- **規範（`CLAUDE.md`）**：`Projects/CLAUDE.md`（必要時含 `Template_CPLEX/CLAUDE.md`、`claudemdTemplate/Root`）說明**兩種建構方式**，並標注 **AI 預設偏好 B**、何時才退回 A。

### In Scope（2026-06-22 review 後追加 — 修掉「新人上手即撞牆」的入口問題）

- **修好 `Template_CPLEX`（P0）**：目前用了 `[OptVar]`/`[OptParam]` 卻沒掛 generator analyzer → 編不過。改成**一致的 generator canonical 範本**（掛 analyzer + 所有變數/參數改 `[OptVar]`/`[OptParam]`）。
- **generator 正名（P0）**：`claudemdTemplate/{Variable,Parameter,Root}`、root `CLAUDE.md`、`README.md` 補上 `[OptVar]`/`[OptParam]` 用法，標為**預設**、手寫為後路。
- **root `CLAUDE.md` 改新版（P0）**：由舊手動 `MyProblem.Execute()` + `Data\` 改為 OptModel + `Set\` + 雙模式 + generator 偏好。
- **`README.md` 翻新（P1）**：OptModel/雙模式/ExperimentRunner/generator；修專案表；移除不存在的 `Foundation/` 連結（改指 `CPLEX_API_REFERENCE.md` + `dlls/`）。
- **`scripts/run.ps1`（P1）**：排除非可執行專案（analyzer `OptimModeling.Generators`、stub）。
- **root `CodeMap.md`（P2）**：標註雙架構並指向本 spec 的 dual-arch CodeMap。
- **小細節（P3）**：CPLEX 版本對齊、`dataset/NLP4P.csv` 用途註記、`Template_CPLEX` 死 `Generated/` 設定收尾、`_new` 移植後列待刪。

### Out of Scope

- 修改 OptimFoundation 框架 DLL（`OptModel` / `OptEngine` / `Experiment` / `Trial` / `AutoSetsGenerator` 皆唯讀引用，不擴充）。
- 數學模型內容變更（沿用現行醫院排班完整版的 Sets/Params/Vars/Obj/Constraints，不增刪約束數學）。
- 本環境 build / 跑 CPLEX 驗證（無 DLL / msbuild）→ 交付 pattern 對齊 + 每檔 diff，**build 由使用者本機跑**。
- Gurobi / 其他求解器。
- 其餘 7 個既有專案的重構（本案只動 Hospital 兩專案 + 教學 + CLAUDE.md）。

## User Stories / Use Cases

1. As 教學讀者, 我要在同一個問題上看到「手寫 vs generator」逐行對照, so that 我懂兩種寫法的差異與各自成本。
2. As 開發者, 我要兩個可 `dotnet run` 的專案, so that 我能親手跑出兩版的解與 tuning 結果並比較。
3. As 框架使用者（含 AI）, 我要 `CLAUDE.md` 清楚說「兩套都行、預設走 B、何時退回 A」, so that 產新題目時有一致且有理由的預設。

## Acceptance Criteria

**教學（tutorial）**
- [ ] `tutorial/index.html` 內新增兩章：〈架構 A：手動組裝〉與〈架構 B：Generator + 注入 Action〉，各自端到端建出同一份醫院排班模型（Sets→Params→Vars→Obj→Constraints→solve→讀解）。
- [ ] 新增一節〈兩架構並排對照〉：composition root、變數/參數來源、檔案數、tuning 寫法、各自取捨（含「AI 為何預設 B」）。
- [ ] 兩章引用**同一份** `Model/HospitalRostering_Model.md`，數學符號一致。
- [ ] 章節導覽（TOC/sidebar）更新，可跳轉到兩新章。

**程式（兩專案）**
- [ ] `Projects/HospitalRostering_Manual/` 可跑：`Program.cs` → `HospitalRosteringProblem.cs : IDisposable` 手寫 `Execute()`（手接 `OptEngine`、手呼叫建變數/建模/求解/寫 CSV）；Variable/Parameter class **全手寫**。
- [ ] `Projects/HospitalRostering_Generator/` 可跑：`Program.cs` 用 Fluent `OptModel`（`.AddVariables(e=>…).AddModel(e=>…).OnSolved(…)` 注入 Action）；Variable/Parameter 以 `[OptVar]`/`[OptParam]` + `AutoSetsGenerator` 生成（class 只留 `partial` 宣告 + attribute）。
- [ ] 兩專案 namespace 乾淨統一（`HospitalRostering_Manual.*` / `HospitalRostering_Generator.*`），**不得**殘留 `SandBox`/`MyApp`/`VariableClass`/`OptimModeling` 舊命名。
- [ ] 兩專案資料夾統一：`Model/ Parameter/ Set/ Variable/ Objective/ Constraint/` + 頂層 `Program.cs`、`ExperimentRunner.cs`。
- [ ] 兩專案**建模邏輯抽成共用 build-step**（A：`Problem` 內部步驟方法或 `VariableCreate`/`BuildModel`；B：`VariableCreate`/`BuildModel`），確保 solve 與 experiment 兩模式模型一致。
- [ ] 兩專案的數學模型一致：相同 Sets/Vars/限制式集合（FullfillDemand / OneGroup / PreAssign / SixDayWork / NightToDay / OffOneDay / CrossGroup / BelowAVG / WeekendLT4 / DoubleOffLT2 + ObjectiveFunction）。
- [ ] 禁止 Hardcode 天條維持：所有數值經 `Parameter.QTY` / Dataload。

**Tuning**
- [ ] 兩專案皆支援 `dotnet run -- experiment`，跑**同一組** variants（baseline / emphasis=optimal / varsel=strong / nodesel=bestbound / gap=0.01 / threads=4 / seed=固定）。
- [ ] 兩專案各輸出 `Experiments/<name>.csv + .json`（用框架 `Experiment` / `Trial.Capture`）。
- [ ] 教學對照節呈現兩版 tuning 的可比較欄位（Status/Obj/Gap/Time/Nodes/Vars/Cons）。

**規範（CLAUDE.md）**
- [ ] `Projects/CLAUDE.md` 明列「兩種建構方式」各自的資料夾/composition/變數來源，並標注 **AI 預設偏好 B（generator，較簡單）**、**退回 A 的時機**（如：generator 不支援的特殊變數型別、需要逐行 debug 生成碼時）。
- [ ] 連結到兩個示範專案與教學兩章作為 few-shot。
- [ ] 必要時 `Template_CPLEX/CLAUDE.md` 與 `claudemdTemplate/Root/CLAUDE.md` 同步加註雙 pattern（至少互相連結，不互相矛盾）。

**交付**
- [ ] 每個改動/新增檔附 before/after 或新增重點說明（人工 review 驗收）。
- [ ] 「需手動刪除舊資料」清單（舊 `HospitalRosteringProblem_new` 等被取代物，列出供你拍板）。

## Module Interactions

- **tutorial/**（docs）：`index.html` 主檔加兩章 + 對照節；可能更新 `development-workflow.md` / `ai-modeling-framework-tutorial.md` 的交叉連結。
- **Projects/HospitalRostering_Manual/**（C#）：手動 pattern 端到端。
- **Projects/HospitalRostering_Generator/**（C#）：generator + OptModel pattern 端到端。
- **Projects/OptimModeling.Generators/**（C#，唯讀引用）：`AutoSetsGenerator`（`[OptVar]`/`[OptParam]`）— B 專案以 analyzer/ProjectReference 方式掛入。
- **OptimFoundation.Core / .Cplex**（外部框架，唯讀）：`OptModel`、`OptEngine`、`CplexConfig`、`Experiment`、`Trial.Capture`、`ITunableConfig`。
- **CLAUDE.md 群**：`Projects/CLAUDE.md`（主）、`Template_CPLEX/CLAUDE.md`、`claudemdTemplate/Root/CLAUDE.md`（同步雙 pattern）。

## API Design（本案的「契約」= 兩種架構的樣板）

### 架構 A — 手動組裝（HospitalRostering_Manual）

```csharp
// Program.cs
if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }
using var problem = new HospitalRosteringProblem();   // 手寫 IDisposable
problem.Execute();

// HospitalRosteringProblem.cs（手寫 composition root）
public sealed class HospitalRosteringProblem : IDisposable
{
    private readonly Dataload _data = new();
    private OptEngine _engine = null!;
    public void Execute()
    {
        var config = new CplexConfig { epGap = 0.03, timeLimit = 100, workThreads = 10, /*…*/ };
        _engine = new OptEngine(config);
        _engine.Build();
        new VariableCreate(_data, _engine).Build();   // 手寫 class
        new BuildModel(_data, _engine).Build();        // 手寫 class
        bool ok = _engine.Solve();
        if (ok) _data.WriteToCSV(_engine);
    }
    public void Dispose() => _engine?.Dispose();
}

// Variable/VariableB_ShiftAssign.cs — 全手寫
public class VariableB_ShiftAssign : VariableBase
{
    public DateTime Date { get; set; }
    public string Employee { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
}
```

### 架構 B — Generator + 注入 Action（HospitalRostering_Generator）

```csharp
// Program.cs
if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }
var data = new Dataload();
using var m = new OptModel("HospitalRostering_Generator")
    .UseConfig(() => new CplexConfig { epGap = 0.03, timeLimit = 100, workThreads = 10, /*…*/ })
    .AddVariables(e => new VariableCreate(data, e).Build())   // 注入 Action<OptEngine>
    .AddModel(e => new BuildModel(data, e).Build())
    .OnSolved(e => data.WriteToCSV(e));
m.Execute();

// Variable/VariableB_ShiftAssign.cs — 只留宣告，class body 由 generator 生成
[OptVar(VarType.Binary, "Date:DateTime", "Employee", "Group")]
public partial class VariableB_ShiftAssign { }
```

### ExperimentRunner（兩版共用形狀，雙模式的 experiment 端）

```csharp
public static class ExperimentRunner
{
    public static void Run()
    {
        var exp = new Experiment("<proj>-tuning", "掃 emphasis/varSel/nodeSelect/gap/threads/seed");
        var variants = new (string label, Action<CplexConfig> tune)[]
        {
            ("baseline", _ => {}), ("emphasis=optimal", c => c.Emphasis = 2),
            ("varsel=strong", c => c.varSel = 3), ("nodesel=bestbound", c => c.nodeSelect = 1),
            ("gap=0.01", c => c.epGap = 0.01), ("threads=4", c => c.workThreads = 4),
            ("seed=20260622", c => c.Seed = 20260622),
        };
        foreach (var (label, tune) in variants)
        {
            var config = new CplexConfig { epGap = 0.03, timeLimit = 100, workThreads = 10, enableLog = false };
            tune(config);
            var data = new Dataload();
            using var engine = new OptEngine(config);
            engine.Build();
            new VariableCreate(data, engine).Build();   // ← 與 solve 模式共用同一 build-step
            new BuildModel(data, engine).Build();
            exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
        }
        exp.Save();
    }
}
```

### 兩架構差異對照（教學對照節的骨幹）

| 面向 | 架構 A 手動 | 架構 B Generator+注入 |
|------|-------------|------------------------|
| composition root | 手寫 `Problem.Execute()` | Fluent `OptModel` + 注入 Action |
| 變數/參數 class | 全手寫 properties | `[OptVar]`/`[OptParam]` 生成 |
| 生命週期管理 | 自己 `IDisposable`/`Dispose` | `OptModel` 內建 build/solve/dispose |
| 樣板量 | 多 | 少（AI 預設選此） |
| 可控/可 debug 生成碼 | 高（全在眼前） | 生成碼需看 `obj/.../*.g.cs` |
| tuning（experiment） | 同形狀 `ExperimentRunner` | 同形狀 `ExperimentRunner` |

## Data Model（專案盤點：現狀 → 動作）

| 項目 | 現狀 | 動作 | 風險 |
|------|------|------|------|
| `Projects/HospitalRostering_Manual` | 不存在 | 新建（架構 A，手寫 class + Problem.Execute + 雙模式） | 中 |
| `Projects/HospitalRostering_Generator` | 不存在 | 新建（架構 B，[OptVar]/[OptParam] + OptModel + 雙模式）；模型碼可由 `_new` 移植 | 中 |
| `Projects/HospitalRosteringProblem_new` | OptModel + ModelComposer + 雙模式，但 namespace 亂（SandBox/MyApp/VariableClass/Constraints/Data） | 作為 B 的程式來源移植；移植後列入**待刪清單**供你拍板 | 中 |
| `Model/HospitalRostering_Model.md` | 散在 tutorial + `_new/README`，無 canonical .md | 萃取成單一 canonical `.md`，兩專案共用（複製或連結） | 低 |
| `tutorial/index.html` | 10 章單一寫法 | 加兩章 + 對照節 + TOC | 中 |
| `Projects/CLAUDE.md` | 只述 OptModel 單一 pattern | 改為雙 pattern + 偏好 B 標注 | 低 |

## Edge Cases & Error Handling

- **「同一份模型」如何保證不漂移**：兩專案共用同一 `HospitalRostering_Model.md`；建模碼以相同限制式集合 + 相同 QTY 來源。對照節需明確聲明「只有 composition / class 來源不同，數學零差異」。
- **Generator 掛載**：B 專案需正確以 analyzer 形式引用 `OptimModeling.Generators`（`OutputItemType="Analyzer"`），否則 `[OptVar]` 不生成 → build 失敗。本環境不可 build，需在規範與 csproj 註明，使用者本機驗證。
- **手動版的雙模式重複**：A 版 solve 走 `Problem.Execute()`、experiment 走 `ExperimentRunner` 手接 engine；兩者**必須共用** `VariableCreate`/`BuildModel`，避免模型在兩模式間漂移（與 B 同原則）。
- **OptModel 一次性 vs tuning 重建**：tuning 不能用 OptModel（自建/Dispose engine），故兩版 experiment 都手接 `OptEngine`——這點剛好讓 A/B 的 experiment 形狀一致。
- **既有 `_new` 移植污染**：移植時務必清掉 `SandBox`/`MyApp` namespace 與 `VariableClass`/`Constraints`/`Data` 舊資料夾名，否則兩專案命名不一致。
- **build 不可驗**：本環境無 CPLEX DLL / msbuild → 人工 review + 每檔 diff，使用者本機 build/run。

## Non-Functional Requirements

- **數學等價**：A、B 兩專案對同一資料集求解，模型（變數數/限制式數/目標式）需邏輯等價（差異只在 composition 與 class 來源）。
- **一致性**：兩專案資料夾、namespace、ExperimentRunner variants 形狀一致；CLAUDE.md 與兩專案、教學三者不矛盾。
- **可回溯**：每改動/新增檔附重點 diff。
- **教學可讀**：兩章「同骨架、逐段對照」，讀者能一眼看出哪段是架構差異、哪段是共用模型。

## Open Questions

- [ ] 兩專案命名採 `HospitalRostering_Manual` / `HospitalRostering_Generator`（本規格暫定，若你偏好 `_v1/_v2` 或其他，stub 前告知）。
- [ ] 教學兩章插入位置：建議插在現行 Phase 2（翻譯成 C#）之後，作為「同一模型的兩種落地」；若你要獨立成教學末段比較章亦可。
- [ ] canonical `Model/HospitalRostering_Model.md` 是「兩專案各一份複製」還是「共用一份、專案以連結引用」（暫定各放一份複製，內容字字相同）。

## Implementation Plan

### Stub 階段（approve 後第一步）

- [ ] 先產 `CodeMap.md`（兩專案骨架 + generator 掛載 + tuning 流向）作為實作地圖。
- [ ] 萃取 canonical `HospitalRostering_Model.md`（從 tutorial Phase 1 + `_new`）。
- [ ] 建 `HospitalRostering_Generator` **骨架**：資料夾 + `.csproj`（掛 `OptimModeling.Generators` analyzer + dll HintPath）+ `Program.cs`（OptModel 形狀，body TODO）+ 空 `VariableCreate`/`BuildModel`/`ExperimentRunner` 簽名 + `[OptVar]`/`[OptParam]` 宣告殼。
- [ ] 建 `HospitalRostering_Manual` **骨架**：資料夾 + `.csproj` + `Program.cs` + `HospitalRosteringProblem.cs`（`Execute()` 留 TODO/`NotImplementedException`）+ 手寫 Variable/Parameter class 殼 + 空 `VariableCreate`/`BuildModel`/`ExperimentRunner`。
- [ ] （骨架不寫業務邏輯；因無 build 環境，stub 完以人工結構 review 取代 build 驗證。）

### 逐步實作（stub 確認後）

- [ ] B 專案：移植 `_new` 模型碼 → 清 namespace → 改用 `[OptVar]`/`[OptParam]` → 補 `VariableCreate`/`BuildModel`/`ExperimentRunner` → solve/experiment 雙模式完成。
- [ ] A 專案：以同一模型手寫全部 Variable/Parameter class + `Problem.Execute()` + `ExperimentRunner`（共用 build-step）。
- [ ] 教學：寫〈架構 A〉〈架構 B〉兩章 + 〈並排對照〉節 + 更新 TOC/sidebar。
- [ ] CLAUDE.md：`Projects/CLAUDE.md` 雙 pattern + 偏好 B；`Template_CPLEX`/`claudemdTemplate/Root` 同步註記。
- [ ] 彙整「需手動刪除舊資料」清單（`HospitalRosteringProblem_new` 等）。

## References

- 前一份（被本案部分推翻）：`specs/2026-06-21-claudeai-spec-refresh.md`
- 架構地圖：`CodeMap.md`
- 新版 composition root：`Template_CPLEX/Program.cs`（OptModel 雙模式）
- source generator：`Projects/OptimModeling.Generators/AutoSetsGenerator.cs`（`[OptVar]`/`[OptParam]`）
- B 專案程式來源：`Projects/HospitalRosteringProblem_new/`（ModelComposer + ExperimentRunner，待清 namespace）
- 數學模型現址：`tutorial/index.html` Phase 1（§4 Sets/Params/Vars/Obj/Constraints）
- tuning harness：OptimFoundation `Experiment` / `Trial.Capture` / `ITunableConfig`
