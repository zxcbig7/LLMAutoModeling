# Phase 3 · Foundation Tuning — 調校規範

> 天條見 [`README.md`](README.md)。**使用者提出才做，不主動建議。**
> 任何涉及模型調整（結構 / 數值 / solver 參數任一）MUST 先讀本檔再動手。
> Solver 旋鈕全表在 [`../automated/specs/2026-06-21-model-tuning-protocol.md`](../automated/specs/2026-06-21-model-tuning-protocol.md)（進階補充，本檔已列常用旋鈕）。

## 硬規則

- MUST 測試流程正確性優先：先過 [`phase-2-coding.md`](phase-2-coding.md) 的**解驗證協定**（Status 三分診斷 → 可行性代回 → 單位一致 → LP bound sanity）才進效能 tuning —— Why: 調快一個錯的模型毫無意義
- MUST 依觸發類型走對應入口（見三類表），NEVER 混路（例如嫌慢就順手改約束結構）
- MUST 每輪 tuning 用 Experiment API 記錄（`Trial.Capture` + `Experiment.Save`），對照 `MipGap` / `WallTimeMs` / `NodeCount` / `Status` 客觀指標 —— Why: 沒有指標的調校是憑感覺，不可回溯
- MUST tuning 前後 before/after 對照回報（目標值 + 指標）
- MUST 影響模型語意的變更（加 slack、加/刪約束、Hard→Soft）同步更新 Model.md
- Stop conditions：solver 層連調 3 輪無改善 → 升級到下一方向；結構層改 2 輪仍不達標 → 停下回報「無法在不改需求下達標」，NEVER 無限迴圈

## 三類觸發 → 入口

| 觸發 | 線索 | 動作入口 |
| --- | --- | --- |
| **solver**（求解參數） | timeout / gap 過大 / 太慢，模型正確 | 只調 `CplexConfig` 旋鈕；不動數學結構、不動數值 |
| **data**（數值） | 使用者改參數值 | 只改 `Set/Dataload.cs`（數值保真照舊）|
| **structure**（數學結構） | 加/刪約束、改 big-M、infeasible、reformulation | 回 Model.md 改模型 → 重走 Coding 轉譯該部分 |

## Solver 層決策（正確性 gate 通過後）

| 症狀 | 動作 |
| --- | --- |
| timeout 但解正確 | 提 `timeLimit` / `mipEmphasis = 1`（重可行解）/ 開平行；連續放寬仍 timeout → 回 structure（reformulation）|
| gap 過大 | 收 `epGap` 或 `mipEmphasis = 2`；無效再開 cuts（`gomoryCuts` / `mirCuts`...）或回 structure |
| 記憶體爆 | `treeMemoryLimit` + `nodeFileInd` |
| 要可重現實驗 | `parallelMode = 1` + 固定 `randomSeed` + `detTimeLimit` |
| 數值不穩 | `numericalEmphasis = true` |

ProblemType 預設起點：LP → `mipEmphasis 0 / timeLimit 300`；IP → `1 / 1800`；MILP → `2 / 3600`。

## Infeasible 標準流程（IIS → Soft Constraint）

1. 跑 CPLEX IIS 找最小衝突 constraint 集合
2. 該 constraint 改用框架軟限制式：先 `AddLHS(...)` 再 `CreateLeSoft(rhs, penalty)` / `CreateGeSoft` / `CreateEqSoft`（用法見 [`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md) 6.5）
3. 違反量 = 彈性變數解值：`GetVariableValue("Deficit_...")`
4. Model.md 同步標記 Hard → Soft，penalty 值寫成 Parameter

## 結構層手段（效能差異大時才動，動了就要同步 Model.md）

| 手段 | 時機 |
| --- | --- |
| Symmetry breaking | 存在多個等價解（如員工可互換）|
| Tighten Big-M | LP relaxation gap 過大 |
| Valid inequalities | 不改 feasible region、收緊 LP bound |
| Warm start | 有已知可行解可給 B&B 起點 |

## Experiment API（每輪 tuning 必記錄）

```csharp
var exp = new Experiment("<Project>-tuning", "調校說明");
foreach (var (label, tune) in variants)
{
    var config = new CplexConfig { timeLimit = 300 };
    tune(config);
    using var engine = new OptEngine(config);
    engine.Build();
    new VariableCreate(dataload, engine).Build();
    new BuildModel(dataload, engine).Build();
    exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
}
exp.Save(); // → Experiments/<Project>-tuning.csv + .json
```

CSV 給人對照、JSON 給後續 LLM tuning。跑法：`dotnet run -- experiment`（`ExperimentRunner.Run()`）。

## good/bad：嫌慢的第一反應

✅ Good：正確性 gate → 確認 solver 觸發 → 掃 `mipEmphasis` / `epGap` 兩三組 variant → 附 before/after 指標
❌ Bad：直接把 `<=` 改成 `=`「讓解空間變小」（改壞數學結構）、把 0.375 改成 0.4「比較好算」（數值失真）

## 常見任務
- 「太慢 / timeout」→ solver 路線（上表）
- 「infeasible」→ IIS → Soft Constraint 流程
- 「改需求 / 加規則」→ structure：回 Model.md（Phase 1 格式）→ 使用者確認 → 轉譯
- 「跑實驗」→ ExperimentRunner + Experiment API，報 CSV 路徑與結論

## Fatal
- NEVER 未過正確性 gate 就調效能
- NEVER 以 tuning 之名移項 / 改號 / 翻轉方向 / 動數值精度
- NEVER 改了模型語意不更新 Model.md
- NEVER 不記 Experiment 就宣稱「有變快」
</content>
