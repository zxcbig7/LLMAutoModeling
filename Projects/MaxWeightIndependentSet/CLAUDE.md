# MaxWeightIndependentSet（⚠ 非標準研究變體）

## 這是什麼

NP-hard 的 **Max Weighted Independent Set**（IP）範例 + 一份 **solver-層 tuning 研究**。狀態：done（見 `status.json`）。
實例：G(n,p) 250 節點 / 9275 邊，最佳目標值 1332；8 組 `CplexConfig` 旋鈕掃描結果見 [experiments/RESULTS.md](experiments/RESULTS.md)。

## ⚠ 為何長得跟其他專案不一樣

本專案**刻意不遵循**框架的標準六資料夾 + 雙模式 Program 慣例——它是一個探索「分階段 NL→模型→程式→調校」流程的研究產物，早於目前的標準化。**請勿把它的版面當成框架慣例。**

| 路徑 | 內容 |
|------|------|
| `csharp/` | 實際 C# 專案（`Program.cs` + `Project/` + `Model/`）；code 單一來源 |
| `csharp/Project/ExperimentRunner.cs` | tuning sweep（`mipEmphasis` / `cliqueCuts` / `probe` / 決定論） |
| `stages/` | NL→模型的分階段產物（classify → standard → keyinfo → aml） |
| `experiments/` | tuning 落地（`mwis-tuning.csv`+`.json` + `RESULTS.md`） |

## 框架的標準慣例在哪

要學/套用框架標準寫法，**不要**參考本資料夾，請看：

- 起始範本：`Template_CPLEX/`（generator + OptModel 雙模式）
- 雙架構參考：`Projects/HospitalRostering_Generator`（預設）、`Projects/HospitalRostering_Manual`（手寫後路）
- 資料夾規則單一來源：`claudemdTemplate/`
- 端到端教學：`tutorial/`

> 若日後要把本專案標準化：依 spec `specs/2026-06-21-claudeai-spec-refresh.md` 的「MWIS 重構為標準六資料夾 + 雙模式」項，把 `csharp/` 攤平成 `Model/ Set/ Parameter/ Variable/ Objective/ Constraint/` + 頂層 `Program.cs`/`ExperimentRunner.cs`，`stages/` 的數學文件歸入 `Model/`。
