# MaxWeightIndependentSet

## 這是什麼

NP-hard 的 **Max Weighted Independent Set**（IP）範例 + 一份 **solver-層 tuning 研究**。狀態：done（見 `status.json`）。
實例：G(n,p) 250 節點 / 9275 邊，最佳目標值 1332；8 組 `CplexConfig` 旋鈕掃描結果見 [experiments/RESULTS.md](experiments/RESULTS.md)。

## 結構（已標準化為扁平版）

2026-07-14 已從舊的 `stages/` + `csharp/` 兩層結構遷至框架標準扁平版（`csharp/` 上提、`stages/` 數學模型精華併入 `Model/`）。

| 路徑 | 內容 |
|------|------|
| `Model/` | 數學模型（`MaxWeightIndependentSet_Model.md`）+ Parameters/Variables/Constraints/ObjectiveFunction |
| `Project/` | `Dataload` / `VariableCreate` / `BuildConstraints` / `Project` / `ExperimentRunner`（tuning sweep） |
| `Program.cs` | 進入點（solve / experiment 雙模式） |
| `experiments/` | tuning 落地（`mwis-tuning.csv`+`.json` + `RESULTS.md`） |
| `status.json` | 進度記錄 |

## 框架標準慣例參考

- 起始範本：`Template_CPLEX/`（generator + OptModel 雙模式）
- 雙架構參考：`Projects/HospitalRostering_Generator`（預設）、`Projects/HospitalRostering_Manual`（手寫後路）
- 資料夾規則單一來源：`../CLAUDE.md` 與 `claudemdTemplate/`
- 天條唯一權威：[`../../AGENTS.md`](../../AGENTS.md)
