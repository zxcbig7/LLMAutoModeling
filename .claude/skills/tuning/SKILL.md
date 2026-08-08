---
name: tuning
description: Phase 3 調校 orchestrator——模型與資料凍結、已 feasible 的前提下，只調 CplexConfig solver 旋鈕，用 OptExperiment 留證據，champion 寫回 production baseline 並留下 provenance。當使用者說「太慢」「timeout」「gap 下不去」「調參數」「tuning」「效能」時使用。使用者提出才做，NEVER 主動建議。
argument-hint: <專案名> [調校方向或症狀]
---

# tuning — Phase 3 調校調度

三階段 phase gate：`modeling` → `coding` → **`tuning`（本 skill）**。

你是第三棒，而且是**選配的一棒**：使用者提出效能問題才啟動，NEVER 主動建議調校。核心信念是**先驗正確，再調效能——調快一個錯模型沒有價值**。

**本 skill 只動一樣東西：`CplexConfig` 的 solver 旋鈕。** 進場時模型與資料已凍結、已 feasible；`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 在本階段一律唯讀。要動它們就不是 tuning，退回對應 phase。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.claude/skills/tuning/SKILL.md`）。
> 規則單一來源：`.claude/rules/AGENTS.md`（天條）+ `.claude/workflows/interactive/phase-3-tuning.md`（範圍界線、solver 決策表、Experiment paved path、退場條件）+ `.claude/rules/Ph2_Coding/optimfoundation-api-guide.md` §8–§9（Experiment API 與框架簽名）。
> 例外：Step 4 的 **promotion 閉環與 provenance 產出**是本 skill 定義的交付要求（工作流層），數學與 API 規則一律回讀 `.claude/workflows/interactive/`。
> 本 skill 只做調度與 gate 把關，**NEVER 在此複製規則**。

## 輸入（`$ARGUMENTS`）

`/tuning <專案> <調校方向>`——第一段指出調哪個專案，其餘是使用者觀察到的症狀或指定方向。

| 參數形式 | 動作 |
| --- | --- |
| 專案名 + 敘述（`HospitalRostering gap 卡在 8% 下不去`） | 專案取第一段；敘述帶進 Step 1 triage 當症狀證據 |
| 只有專案名 | 進 Step 0 實跑一次，拿 Status / gap / 時間自行 triage |
| 只有敘述、沒有專案名 | 掃 `Projects/*/status.json` 找 `solveVerified: true` 的專案；恰好一個就用它並回報，否則停下問 |
| 空 | 停下問使用者：哪個專案、什麼症狀 —— NEVER 自己挑一個專案來調 |

使用者指定的方向（「試試 emphasis」「加 cuts」）是 Step 2 的 variant 候選，**不是 Step 0 gate 的豁免**：正確性沒過就先回 `coding`，並說清楚為什麼還不能調。指定方向也不解除「一輪只改一個旋鈕」。

## Step 0 · 進場 gate（不通過就不准調）

依序確認，任一項不成立就停下回報，先回 `coding` 修：

1. `dotnet build` 通過
2. `Status == Optimal`（或該題目預期的合法狀態）—— **feasible 是本階段的前提，不是本階段要解決的問題**
3. `coding` 的解驗證協定四步已過（代回可行 + 單位一致 + LP bound sanity + 對得上 Model.md 小例）

`status.json` 的 `solveVerified: true` 是必要條件，但不能只看它——**實跑一次確認**。

## Step 1 · 範圍界線：先確認這真的是 tuning

本 skill 只有**一條**路：調 `CplexConfig` 旋鈕。使用者的訴求落在下表哪一格，決定你要不要繼續：

| 訴求 | 是不是 tuning | 動作 |
| --- | --- | --- |
| timeout / gap 收不下來 / 太慢，模型正確且有解 | ✅ 是 | 進 Step 2 |
| 要換參數值、換一批資料 | ❌ 否 | 換 `Data/*.csv` 重跑即可，本 skill 不介入 |
| 要加刪約束 / 改 Big-M / reformulation | ❌ 否 | 停止，退回 `modeling` 改 Model.md，確認後由 `coding` 重走轉譯 |
| `Infeasible` / `Unbounded` | ❌ 否 | 前提破裂，停止調校：`Infeasible` 跑 IIS 拿最小衝突集合當**證據**回報並退回 `modeling` / `coding`；`Unbounded` 退回 `coding` 補漏掉的界限 constraint。NEVER 在本階段建 soft variant 或加 penalty 讓它「有解」 |

Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，這一輪的實驗證據整批作廢。

**停損**：連續 3 輪無實質改善 → 停止 tuning 並回報，把「可能要改模型結構」當**建議**交還使用者（那是新一輪 Phase 1），NEVER 自己升級去動模型。

## Step 2 · 設計本輪 variants

- 以 `Program.cs` 現行的 production baseline 為起點，用 `Clone()` 產生具體 variant——**NEVER 用 tune delegate 突變共用 config**
- **一輪只改一個旋鈕**，要比較兩個旋鈕就開兩個 variant
- 共用同一份已載入的 `data`，載入後視為唯讀

## Step 3 · 跑 OptExperiment 留證據

- experiment 名帶輪次：`<project>-tuning-r<N>`
- 每個 Trial 記錄 `Status`、objective、`MipGap`、時間、節點數
- 想降低雜訊就加 warm-up trial + 多 seed + 輪替 variant 順序（performance variability 是真的，單次跑贏不算贏）
- `OptExperiment` 無 `OnSolved`；掃描中不要大量輸出 solution

## Step 4 · Champion 判定與 promotion 閉環

只產 experiment 報表、production 仍跑舊 config，**不算完成**。完整閉環：

1. 讀本輪 Trial 的 config + metrics，選出 champion（贏得夠明確才算贏；差距在 variability 範圍內視同平手）
2. champion 的**完整設定明確寫回** `Program.cs` 的 production baseline
3. 同步 provenance：baseline 上方註解（來源 experiment、Trial label、before/after diff）+ 專案根 `TuningHistory.md`
4. **promotion 後 MUST 重新 build + 跑無參數 production**，通過才算數
5. 沒有可靠勝者 → 保留原 baseline，一樣把「retain + 證據」記進 `TuningHistory.md`

`bin/Experiments` 會被清掉，NEVER 只依賴它當紀錄。

## Step 5 · 交付 + 更新 status.json

交付內容：本輪 variants 清單、before/after 對照表、champion 或 retain 決策 + 理由、production 驗證結果、`TuningHistory.md` 位置。
git diff MUST 只有 `Program.cs`（那顆 baseline）與 `TuningHistory.md` 兩個檔——多出任何 `.csv` / `Constraint_*.cs` / `Model.md` 的改動，就是本階段越界了。

`Projects/<Project>/status.json` **只更新下列欄位**（完整 schema 見 `modeling` skill，NEVER 整檔覆寫掉其他欄位）：

```json
{ "phase": "tuning", "tuningRound": 1, "productionBaseline": "r1-<label> | initial", "promotionVerified": true, "updated": "YYYY-MM-DD" }
```

## 交付前

逐條對照同資料夾的 `checklist.md`，全過才交付。

## Fatal

- NEVER 未過進場 gate 就調效能
- NEVER 在本階段改資料（`Data/*.csv`、`Dataload`）或模型（`Constraint_*`、`Objective`、`Model.md`）—— 要改就不是 tuning，退回對應 phase
- NEVER 用 soft constraint / penalty 繞過 infeasible
- NEVER 以 tuning 名義移項 / 改號 / 翻轉方向 / 四捨五入或修改輸入精度
- NEVER 不留 Experiment 紀錄就宣稱改善
- NEVER 一輪同時改多個旋鈕（歸因不了就等於沒調）
- NEVER 主動發起 tuning（使用者提出才做）
- NEVER 用絕對路徑
