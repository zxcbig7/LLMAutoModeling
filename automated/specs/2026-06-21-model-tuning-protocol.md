---
title: Model Tuning Protocol — AI 必讀的最佳化模型調校協定
status: draft
created: 2026-06-21
updated: 2026-06-21
modules: [prompts, claude-code-workflow, api, docs]
---

# Model Tuning Protocol

## Summary

一份給 **AI（Claude Code + Web API LLM pipeline）必讀**的「模型調校協定」文件。
整體 AI Modeling 框架分為 **Phase 1 Modeling**（Stage 0–4b）與 **Phase 2 Coding**（Stage 5–14）；本協定定義在「任何涉及最佳化模型調整」時，AI **必須先讀此文件**，再依固定流程**跑測試（先正確性、後效能）**、依**實驗回饋做 tuning**，並決定回饋要回到 Modeling 或停在 solver 層。

主要產物：`Prompts/15_ModelTuning.md`（協定本體）+ `CLAUDE.md`「Model Tuning」段落改為指向它的 MUST-read 規則。

## Motivation / Why

- 現況：[CLAUDE.md](../CLAUDE.md) 只有極簡的「Model Tuning」三行（改數值 / 改結構），**沒有測試與效能調校的標準流程**，AI 每次調模型行為不一致、容易跳過驗證。
- 痛點：調整後常見三類錯誤無人把關 —— ①改壞數學結構（移項/改號）②數值失真 ③solve 太慢 / timeout / gap 過大。
- OptimFoundation 已內建**為 LLM tuning 設計的 Exp API**（`OptimFoundation.Core`）：`Trial.Capture()` 一次求解就抓 `ConfigSnapshot` + `SolveMetrics`，`Experiment.Save()` 落地 `experiments/<Name>.csv`+`.json`。`ConfigSnapshot` 註解明寫「供之後重現與 LLM tuning 參考」——應把這條**實驗回饋接進 tuning 迴圈**，讓調校有客觀指標（`MipGap`/`WallTimeMs`/`NodeCount`/`Status`）而非憑感覺。參考架構 `Experiments.py` 為論文端的前身（CSV 紀錄 token/runtime/正確性），契約以 C# Exp API 為準。
- 需要一個**單一可信來源（single source of truth）**：只要碰模型，AI 先讀它、照它做。

## Scope

### In Scope

- 撰寫 `Prompts/15_ModelTuning.md`：含觸發規則、兩階段定位、兩種 tuning 模式、測試流程、實驗回饋對應動作、邊界規則。
- 定義 **MUST-read 觸發條件**（Q3）：數學結構變更 + 數值變更 + solver 參數變更，**任一**都先讀本文件。
- 定義 **測試流程**（Q2）：build → solve → 正確性 gate → 效能 tuning → before/after 比較。
- 定義 **實驗回饋 → tuning 動作** 的決策樹（接 OptimFoundation.Core Exp API 的 `SolveMetrics` 指標）。
- 定義 **兩個 tuning 入口**：(A) Modeling 階段介入優化模型；(B) Coding 結束後跑實驗驅動 tuning。
- 更新 [CLAUDE.md](../CLAUDE.md)「Model Tuning」段落 → 指向本協定的 MUST-read 規則。

### Out of Scope

- 不實作 Web API 的實驗 endpoint / 批次 sweep 本身（僅在協定中**引用** OptimFoundation.Core Exp API 的契約；端點實作另開規格）。
- 不重寫既有 Stage 0–14 Prompt 模板（本協定只新增 Stage 15 層級的調校流程）。
- 不調整 OptimFoundation DLL / CPLEX 本體。
- 不做自動化排程跑實驗（人或 orchestrator 觸發，本協定只定義「該怎麼跑、怎麼讀結果」）。

## User Stories / Use Cases

1. As **Claude Code（互動建模工作流）**, 當使用者說「把這個約束改成 ≤ 並重調」時，我先讀 `15_ModelTuning.md`，照協定 build→solve→驗解→（必要時）調 CplexConfig，最後回報 before/after。
2. As **Web API LLM pipeline**, 在 Stage 14 之後若需要效能調校，我依協定讀實驗 CSV 的回饋指標，決定回 Stage 4 重表述或只調 solver 參數。
3. As **使用者**, 我只要說「跑實驗 tuning」，AI 就有一致、可驗收、可回溯的調校行為，不會默默改壞模型。
4. As **建模者（Modeling 階段）**, 在進 Coding 前，AI 依協定的「Mode A」對 AML 數學模型做可解性 / 正確性優化（例如收緊 big-M、加 valid inequalities）。

## Acceptance Criteria

可驗收、可測試的條件。

- [ ] `Prompts/15_ModelTuning.md` 存在，且含下列必備 section：**Trigger Rules、兩階段定位、Tuning Modes (A/B)、Test Procedure、Experiment Feedback Mapping、Boundary Rules、Stop Conditions**。
- [ ] 協定明確寫出 **MUST-read 觸發三類**（結構 / 數值 / solver 參數），且每類附判斷線索。
- [ ] 協定的 Test Procedure 為**正確性優先**：`dotnet build` → `Solve()` → `Status == Optimal` → **正確性 gate（解/目標值 vs 問題描述）** → 才進效能 tuning。
- [ ] 協定列出 **tuning 旋鈕對照**：抽象層 `ITunableConfig`（`Emphasis`/`Seed`/`OptimalityTol`/`FeasibilityTol`/`RootAlgorithm`/`Presolve`/`HeuristicEffort`/`MemoryLimitMb`）+ 共用 `ISolverConfig`（`TimeLimit`/`MipGap`/`Threads`），並標注對 `CplexConfig`（`mipEmphasis`/`epGap`/`timeLimit`/`workThreads`）的映射與 ProblemType（LP/IP/MILP）預設值表。
- [ ] 協定含 **Experiment Feedback Mapping 決策樹**：把 `SolveMetrics`（`Status`/`MipGap`/`WallTimeMs`/`NodeCount`/`ObjectiveValue`/`BestBound`）+ `Code Gen Attempt` 映射到「回 Modeling」或「調 solver」或「修 Dataload 數值」。
- [ ] 協定指明用 `Trial.Capture(engine, label, solveAction)` 逐輪擷取、`Experiment.Save()` 落地 `experiments/<Name>.csv`+`.json` 作為 tuning 紀錄（內建，不另造 log）。
- [ ] 協定重申 **Boundary Rules**：LHS/RHS 不得移項改號、數值不得四捨五入、比較方向不得翻轉、模型 .md 數學式用 LaTeX（[[feedback_model_latex]]）、動 OptimFoundation 時 Core+Cplex DLL 一起更新（[[project_dll_coupdate]]）。
- [ ] 協定含 **Stop Conditions**：fix loop ≤ 5、tuning 迭代上限、何時宣告「無法在不改結構下達標」。
- [ ] [CLAUDE.md](../CLAUDE.md) 的「Model Tuning」段落改為**一句話 MUST-read 指向** `Prompts/15_ModelTuning.md`，不再重複內容。
- [ ] 協定本身用繁體中文撰寫（technical terms 保留英文），格式與既有 `Prompts/*.md` 一致。

## Module Interactions

涉及的模組 / 層 / 流程，以及彼此關係。

- **Prompts（新增）**：`Prompts/15_ModelTuning.md` —— 協定本體，可被 Claude Code 直接讀、也可由 `PromptLibrary.cs` 載入給 API。
- **Claude Code 互動建模工作流**：`projects/<ProjectName>/stages/` + `status.json`；tuning 會改 `07b_dataload_verified.cs`（數值）或從 `04_aml.md` 重跑（結構）。
- **Web API LLM pipeline**：Stage 14（FixCode）之後的調校階段引用本協定；對齊參考架構 `OMG_OptLLM.GenModel()`/`GenCode()` 兩段。
- **OptimFoundation.Core Exp API（引用，不在本規格實作）**：`Experiment`/`Trial.Capture`/`SolveMetrics`/`ConfigSnapshot`/`ITunableConfig`/`ITrajectorySource`；落地 `experiments/<Name>.csv`+`.json`。為回饋與紀錄的權威來源。
- **OptimFoundation CPLEX**：`CplexConfig`（mipEmphasis/epGap/timeLimit/workThreads，對映 `ITunableConfig`）+ `OptEngine.Solve()`/`Status`/`LastMetrics`/`GetObjectiveValue()` 為效能 tuning 的操作面。
- **docs/ RAG**：tuning 後若成功，沿用既有 WriteRagData 把最終 AMLModel 回寫 `docs/<ProjectName>.md`。

## API Design

> 本規格**不實作** endpoint，僅定義協定引用的 OptimFoundation.Core Exp API 契約，供 Stage 15 呼叫與讀取。型別均位於 `namespace OptimFoundation.Core`。

### 一輪 tuning 的標準呼叫（協定要照這個 pattern 走）

```csharp
var exp = Experiment.Load("MyProject") ?? new Experiment("MyProject", "tuning sweep");

// 每換一組旋鈕 → 重設 config → Build → Capture 一輪
engine.Config.Emphasis = 2;        // ITunableConfig 旋鈕
engine.Config.MipGap   = 1e-4;     // ISolverConfig 共用旋鈕
engine.Build();
exp.AddTrial(Trial.Capture(engine, label: "emphasis=2,gap=1e-4", solveAction: () => engine.Solve()));

exp.Save();   // → experiments/MyProject.csv + .json（append、以 RunAt+Label 去重）
```

### `SolveMetrics`（協定讀取的回饋指標，由 `engine.LastMetrics` / `Trial.Metrics` 取得）

| 欄位 | 型別 | tuning 用途 |
| --- | --- | --- |
| `Status` | `SolveStatus` | **正確性/可行性 gate**（Optimal/Feasible/Infeasible/…） |
| `ObjectiveValue` | `double` | 正確性比對（vs 期望解） |
| `BestBound` | `double` | 配合 gap 判斷收斂品質 |
| `MipGap` | `double` | gap 過大 → 調 epGap/Emphasis 或回 Mode A |
| `WallTimeMs` | `double` | 求解耗時，效能主指標 |
| `NodeCount` / `IterationCount` | `long?` | 搜尋規模，定位瓶頸 |
| `VarCount` / `ConstraintCount` | `int` | 模型規模，判斷是否該 reformulation |
| `Convergence[]` | `ConvergencePoint` | 逐點軌跡（需 `ITrajectorySource.EnableTrajectory()`） |

### `ITunableConfig` 旋鈕（跨引擎抽象，協定的效能調校面）

`Emphasis`、`Seed`、`OptimalityTol`、`FeasibilityTol`、`RootAlgorithm`、`Presolve`、`HeuristicEffort`、`MemoryLimitMb`；
共用 `ISolverConfig`：`TimeLimit`、`MipGap`、`Threads`。`null` = 用 solver 預設。`ConfigSnapshot.From(config)` 會把這些 + reflection 抓到的 solver 專屬欄位一起存進每個 `Trial`。

### 觸發語意（概念，非 HTTP 定案）

`triggerType ∈ { "structure" | "data" | "solver" }`（Q3 三類）→ 對應 Mode A（回 Modeling）/ 修 Dataload / 調 `ITunableConfig`。

## Data Model

> 無 DB。tuning 紀錄由 OptimFoundation.Core 的 `Experiment.Save()` 內建落地，Q2 選「先正確性後效能」，故**不另建**自製 log——`Trial`（含 `ConfigSnapshot` + `SolveMetrics`）即為可回溯紀錄。

```text
experiments/<ProjectName>.csv    # Exp API 落地：每個 Trial 一列（CsvExperimentWriter）
experiments/<ProjectName>.json   # 權威來源，append/去重（JsonExperimentWriter），供 Experiment.Load()
projects/<ProjectName>/stages/04_aml.md                  # Mode A（結構）介入點
projects/<ProjectName>/stages/07b_dataload_verified.cs   # 數值 tuning 點
projects/<ProjectName>/csharp/Project/Project.cs         # CplexConfig / ITunableConfig 調整點
```

`Trial` 結構：`{ Label, RunAt, Config: ConfigSnapshot, Metrics: SolveMetrics, Note }`；同名實驗 `Save()` 以 `RunAt+Label` 去重後 append。

## Edge Cases & Error Handling

- **改結構卻沒回 Stage 4**：協定強制——`triggerType == structure` 必須從 `04_aml.md` 重跑，`status.json` 中 04 之後標 pending。
- **solve = Infeasible**：先懷疑數值/方向（回正確性 gate），**不可**靠放寬 solver 參數掩蓋。
- **timeout 但模型正確**：才進 solver tuning（mipEmphasis/timeLimit/epGap）；連續放寬到上限仍 timeout → 宣告需 reformulation（回 Mode A）。
- **gap 過大**：先 `epGap`/`mipEmphasis`，再考慮 valid inequalities（結構層，回 Mode A）。
- **動到 OptimFoundation**：Core + Cplex DLL 必須一起更新（[[project_dll_coupdate]]）。
- **模型 .md 數學式**：一律 LaTeX（[[feedback_model_latex]]），tuning 改寫 AML 時不得退化成純文字。
- **fix loop 撞 5 次上限**：依既有規則 Job = Failed，保留 error log，協定不無限重試。

## Non-Functional Requirements

- **一致性**：任何 AI（Claude Code / API）對同一觸發都走相同步驟，行為可預測。
- **可回溯**：每輪 tuning 的 before/after（status、目標值、solve time、gap）可被人讀懂。
- **保真**：數值與比較方向零失真（沿用 CLAUDE.md 既有硬規則）。
- **正確性優先於效能**：未過正確性 gate，禁止進效能 tuning。

## Open Questions

- [ ] 協定檔名：`Prompts/15_ModelTuning.md`（與 pipeline 編號一致）vs 根目錄 `TUNING.md`（更醒目）？草稿暫定前者。
- [ ] 「正確性 gate」的期望解來源：問題描述自帶答案？還是人工標註？（`SolveMetrics.ObjectiveValue` 需對照 ground truth）<TODO: 待確認>
- [x] ~~Web API 實驗 endpoint 來源~~ → **已定**：用 OptimFoundation.Core Exp API（`Experiment`/`Trial.Capture`/`SolveMetrics`/`ITunableConfig`）。

## Implementation Plan

### Stub 階段（先做，approve 後）

- [ ] 建 `Prompts/15_ModelTuning.md` 骨架：所有 section 標題 + 一行說明 + `<!-- TODO -->`（不填完整內容）。
- [ ] 在 [CLAUDE.md](../CLAUDE.md)「Model Tuning」段落加 MUST-read 指向（一行）。
- [ ] 確認 Markdown 結構與既有 `Prompts/*.md` 一致（人工 review，無 build step）。

### 逐段填肉

- [ ] Trigger Rules（三類 + 線索）
- [ ] 兩階段定位 + 兩種 Tuning Mode (A/B)
- [ ] Test Procedure（正確性優先五步）
- [ ] tuning 旋鈕對照表：`ITunableConfig`/`ISolverConfig` → `CplexConfig`（對齊 ProblemType）
- [ ] Experiment Feedback Mapping 決策樹
- [ ] Boundary Rules + Stop Conditions
- [ ] 與 CLAUDE.md / status.json 工作流的整合說明

## References

- 本 repo 設計總綱：[CLAUDE.md](../CLAUDE.md)（多階段推理流程、Model Tuning、CplexConfig 對照）
- 主規格：[specs/2026-05-20-ai-modeling-optim-cplex-env.md](2026-05-20-ai-modeling-optim-cplex-env.md)
- **OptimFoundation.Core Exp API（權威契約）**：
  - `OptimFoundation.Core/Experiment.cs`（`Experiment`/`Save`/`Load`）
  - `OptimFoundation.Core/ITunableConfig.cs`（跨引擎 tuning 旋鈕）
  - `OptimFoundation.Core/ITrajectorySource.cs`（收斂軌跡 hook）
  - `OptimFoundation.Core/Experiments/Trial.cs`（`Trial.Capture`）
  - `OptimFoundation.Core/Experiments/SolveMetrics.cs`（`SolveMetrics`/`ConvergencePoint`）
  - `OptimFoundation.Core/Experiments/ConfigSnapshot.cs`（設定快照）
- 參考架構（論文端前身，非權威）：`OMG_LLM Reasoning Module/Experiments.py`、`main.py`、`OMG_Foundation/OMG_OptLLM.py`
- 相關 memory：[[feedback_model_latex]]、[[project_dll_coupdate]]
