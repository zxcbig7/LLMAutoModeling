# Phase 3 · automated 路線不做調校

本資料夾**刻意沒有 stage prompt**。automated pipeline 的終點是 Stage 14 build 綠，交付一個 tuning-ready 的專案；solver 調校不在本路線內。

## 為什麼不放進 pipeline

- tuning 的前提是**正確性 gate 已通過**（`Status == Optimal` + 解代回每條 constraint 驗過）。automated 路線沒有人在迴路做這件事，自動調參等於「更快地算出錯答案」。
- champion promotion 會改寫 `Program.cs` 的 production baseline，是一次性高後果且事後難察覺的變更，MUST 有 second-opinion 裁決與 production 驗證，不適合無人監督地跑。
- 效能改善必須大於 baseline 自身的 performance variability，需要 3–5 seeds 與 hold-out instances。單跑一輪就寫回 baseline 得到的是雜訊。

## pipeline 已經替 tuning 準備好的東西

| 來源 | 內容 |
| --- | --- |
| [`../Ph1_Modeling/00_ProblemClassify.md`](../Ph1_Modeling/00_ProblemClassify.md) | 依 ProblemType（LP / IP / MILP）給 `mipEmphasis` 與 `timeLimit` 起始值，即第一顆 `productionBaseline` |
| [`../Ph2_Coding/12_ProjectCode.md`](../Ph2_Coding/12_ProjectCode.md) | 產出 `OptModel` + `OptProject` / `OptExperiment` 雙 runner，variant 一律 `baseline.Clone()` |
| [`../Ph2_Coding/13_ProgramCode.md`](../Ph2_Coding/13_ProgramCode.md) | 具名 `CplexConfig` 材料段，是 promotion 的唯一寫回點 |

## 要調校時去哪

| 需求 | 讀 |
| --- | --- |
| 流程與 gate | [`../../interactive/phase-3-tuning.md`](../../interactive/phase-3-tuning.md) |
| 規範（範圍界線、閉環、`TuningHistory.md`） | [`../../../rules/Ph3_Tuning/foundation-tuning-rules.md`](../../../rules/Ph3_Tuning/foundation-tuning-rules.md) |
| multi-agent 拓樸（T1–T7） | [`../../../rules/Ph3_Tuning/agent-workflow-prompts.md`](../../../rules/Ph3_Tuning/agent-workflow-prompts.md) |
| solver 旋鈕全表 | [`../../../rules/Ph3_Tuning/cplex-tuning-strategy.md`](../../../rules/Ph3_Tuning/cplex-tuning-strategy.md) |

新增本階段 stage prompt 前先確認：這個 stage 能不能在無人監督下判定「改善是真的」。答不出來就不該加。
