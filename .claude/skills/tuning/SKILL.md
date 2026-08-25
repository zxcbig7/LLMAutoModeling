---
name: tuning
description: Phase 3 調校 orchestrator——模型與資料凍結、正確性已驗的前提下，只調 CplexConfig solver 旋鈕。啟動後連續自動執行到停損：環境定版 → R0 校準 → 策略輪迭代 → hold-out → promotion，全程落檔 TuningHistory.md。當使用者說「太慢」「timeout」「跑不完」「gap 下不去」「找不到可行解」「調參數」「tuning」「效能」時使用。使用者提出才做，NEVER 主動建議。
---

# tuning — Phase 3 調校調度

三階段 phase gate：`modeling` → `coding` → **`tuning`（本 skill）**。

你是第三棒，而且是**選配的一棒**：使用者提出效能問題才啟動，NEVER 主動建議調校。核心信念是**先驗正確，再調效能——調快一個錯模型沒有價值**；以及**先證明量得準，再開始比**。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.claude/skills/tuning/SKILL.md`）。

## 規則在哪（本 skill 只有調度，規則一條都不複製）

| 文件 | 管什麼 | 何時讀 |
| --- | --- | --- |
| [`../AGENTS.md`](../AGENTS.md) | 天條、三階段契約、`status.json` schema | 動手前 |
| [`solver-tuning-guide.md`](solver-tuning-guide.md) | **Phase 3 唯一規範**：進場情境、可寫白名單、旋鈕分類、實驗設計、champion 判定、promotion、停損 | 每個 S 步驟依標註的節次讀那一節 |
| [`checklist.md`](checklist.md) | 交付前自檢 | 宣告完成前逐條跑 |
| [`cplex-parameter-reference.md`](cplex-parameter-reference.md) | 旋鈕查表（欄位名、型別、值域、預設） | **查表用，不必通讀**；要寫欄位名或設值時搜尋它，NEVER 憑記憶寫 |

**本 skill 不依賴任何外部腳本**（沒有 `.ps1` / `.py` 要跑）：每輪的 archive 驗證是 `checklist.md`「每輪 archive 逐項驗收」那一節，逐項開檔比對。

衝突順序：`../AGENTS.md` → solver-tuning-guide → checklist。**兩份規則互相矛盾 → 停下回報檔名與衝突**，NEVER 挑方便的那條、NEVER 先實驗再補讀。

審查既有調校工作 → 用 `/review`（[`../../commands/Ph3_Tuning/review.md`](../../commands/Ph3_Tuning/review.md)）。

## 執行模式：一鍵啟動，連續跑到停損

**啟動後不中途向使用者要指示。** 從 S0 一路跑到停止條件命中，中間每輪自動決定「繼續 or 停」。

唯二會中途停下的情況：**S0 進場 gate 不通過**、或**規範 §7.1 的停止條件 G/H 命中**（結果不變式破裂、判定非 tuning）。

發現契約可能訂錯（例 `MipGap` 太鬆）→ **記成 finding 繼續跑**，NEVER 中斷、NEVER 自行變更契約。

## 可寫區域

**白名單五處以外全部唯讀**，逐項定義在規範 **§0.1**（`Program.cs` 的 production baseline、`Program.cs` exp 分支、`TuningHistory.md`、`Experiments/`、`status.json` 的 P3 欄位）。動任何檔案前先確認它在名單上；model 組裝 chain、`Dataload`、`Set/`…`Solution/`、`Data/*.csv`、`Model.md`、`ProjectConfig`、csproj、`dlls/` 一律不准碰。

## 輸入（`$ARGUMENTS`）

| 參數形式 | 動作 |
| --- | --- |
| 專案名 + 敘述 | 專案取第一段；敘述帶進 S0 當症狀證據 |
| 只有專案名 | 實跑一次自行 triage |
| 只有敘述 | 掃 `Projects/*/status.json` 找 `solveVerified: true`；恰好一個就用並回報，否則停下問 |
| 空 | 停下問使用者 —— NEVER 自己挑一個專案 |

使用者指定的方向（「試試 emphasis」「加 cuts」）是候選來源，**不是 gate 的豁免**，也不解除「一輪一顆」。

---

## S0 · 進場 gate + 契約凍結

| 步驟 | 做什麼 | 規範 |
| --- | --- | --- |
| S0-1 | 正確性 gate：`dotnet build` → Status 落在可進場的三態 → `coding` 解驗證協定四步已過。`status.json` 的 `solveVerified: true` 是必要不充分，**MUST 實跑一次確認** | §0.0 |
| S0-1b | **定進場情境 A / B / C，並依情境選定主指標** | §0.0.1 |
| S0-2 | 範圍界線：判定是否真的是 tuning；要動資料 / 結構 / soft constraint → 停止並退回對應 phase | §1（`Infeasible` 先取 IIS 證據，§1.1） |
| S0-3 | 記錄 Phase 2 結果基線（`phase2Status` / `phase2Objective` / `phase2Bound` / `phase2Gap` / `verifiedOn`）寫進契約區塊 | §0.1.1 |
| S0-4 | 契約凍結：停止契約用現行值；量測契約（export 全關、`ParallelMode = 1`、每輪確認無 dynamic search 停用 warning）寫進契約區塊 | §2.0、§6.1 |
| S0-5 | 估算並記錄總預算 | §7.2 |

★ **S0-1b 選錯，整輪實驗白跑**——三個情境的主指標不同，判準與情境 C 的附加前提一律讀 §0.0.1，NEVER 憑印象推斷。
★ 專案若無 `Solution/` 與 `ValidateRules`，回報此缺口並改用 §0.1.1 的不變式作為唯一自動驗證手段。

## S1 · 環境定版 sizing（規範 §2.3）

★ **同一台實機接手時這一步只是抄值。** Phase 2 已明設 `Threads` / `ParallelMode` / `Seed`（api-guide §8.4），直接抄進契約區塊、不重跑掃描。

只有換機器、換 CPLEX 版本、或 Phase 2 未明設（= 交付缺陷，記 finding）才實掃，掃法與判準見 §2.3。定版後寫進契約區塊並凍結。**不計入輪次。**

## S2 · R0 校準（規範 §3.0，硬 gate）

**exp 分支的形狀由 Phase 2 交付**（api-guide §8.4）：名稱、`r0-` label、marker、5 個 seed 都已就位。**NEVER 重寫 code**，直接 `dotnet run --project <project.csproj> -- exp`。形狀不符 → 記 finding 並就地補正（在白名單內），不退回 Phase 2。

★ 執行前 MUST 先刪 `bin/.../Experiments/<Project>-tuning-r0.*` —— Phase 2 驗證管線時跑過一次，同名 experiment 是 **append 不是覆寫**，不刪會把驗證 trial 混進 R0。

三個產出（主指標 + θ、瓶頸剖面、契約健檢探針）的算法一律照 §3.0。**R0 沒跑完不准進 S3。** 剖面 = Variability-dominated → 停止條件 A，直接收尾。

★ 情境 C 的 R0「5 個 seed 全部沒找到解」**不是**早停條件——那正是本輪要打的目標，baseline 得 0 分，照常進 S3。

**不計入輪次。**

## S2.5 · CPLEX 內建 tune 基準（規範 §3.6）

拿 `bin/.../Models/*.lp` 到 CPLEX Interactive Optimizer 跑 `tune`，零成本、不改程式。建議值**拆成獨立 variant** 進 S3 驗證，NEVER 直接 promote。工具不可用 → **跳過不中斷**，記一行理由。

## S3 · 策略輪循環 R1..RN

每輪固定形狀（規範 §3、§4）：

1. **寫假設與預測**（§6.2）—— **MUST 在跑實驗之前寫死**，防事後合理化
2. 候選**只從剖面對應那一類取**（§2.2；全池見 §2.2.1，**§2.2.2 兩顆會丟例外的旋鈕不可掃**），**一輪一顆**
3. 改 `Program.cs` exp 分支的 variant 定義（`baseline.Clone()`），build，跑 `-- exp`
4. 每個 variant × 同一組 5 個 tuning seeds；seed 是共同因子不是 variant
5. archive 本輪 artifact 到專案根 `Experiments/`（`.csv` / `-meta.csv` / `.json` 必備，`-trajectory.csv` 有才搬），產 `TUNING-FACTS` block，再逐項跑 `checklist.md` 的「每輪 archive 逐項驗收」A–E（§3.3.1、§6.2.2）
6. 抽數：**NEVER 整份 JSON / log 讀進 context**，grep 抽欄位（§8.1）
7. 裁決（§4 五個 Step）
8. 寫實測、分析報告與裁決，更新**已否證清單**（跨輪累積）

**每輪結束依 §7.1 檢查停止條件 A–I，命中即進 S4/S5 收尾；否則自動進下一輪。**

★ 情境 B / C **每輪重判剖面**——找到 incumbent 之後瓶頸會換一種。情境 C 首度產出 incumbent 即**升級為情境 B**（§5.3）。

## S4 · Hold-out（規範 §4.6）

champion 用 3 個未參與調參的 seed 重跑。改善消失 → over-tuning，退回 retain。**holdout 只能估計，NEVER 用來選 config。**

## S5 · Promotion 閉環（規範 §5）

1. champion 完整設定寫回 `Program.cs` baseline + provenance 註解（§5.1）
2. **先寫 `TuningHistory.md`，再驗證**（§5.2）
3. 重新 build + 跑無參數 production（export 開回來）
4. 依情境套 §5.3 的 PASS 條件驗收；有 `ValidateRules` 就一併驗
5. FAIL → 撤銷 promotion，記 `rejected`

無可靠勝者 → **retain + 證據**，一樣是合法交付。

## 交付

**diff 檢查**（規範 §0.1.2）：`git diff --name-only` MUST 只有 `Program.cs`、`TuningHistory.md`、本輪 `Experiments/` artifact（+ `status.json`）。`Program.cs` 內部 diff 只允許 baseline 值、provenance 註解、exp 分支 variant 定義——model chain 出現在 diff 裡 = 越界，全部還原。

**回報**：進場情境與主指標、跑了幾輪、每輪一行（假設 → 裁決）、θ 與剖面、champion 或 retain + 理由、停止原因（§7.1 哪一條）、production 驗證結果、`TuningHistory.md` 路徑。

`status.json` **只更新下列欄位**（完整 schema 在 `../AGENTS.md`，NEVER 整檔覆寫）：

```json
{ "phase": "tuning", "tuningRound": 0, "productionBaseline": "initial", "baselineSourceExperiment": "", "baselineSourceTrial": "", "promotionVerified": false, "updated": "YYYY-MM-DD" }
```

交付前逐條對照 `checklist.md`。

## Fatal（本階段特有；通用天條全在 `../AGENTS.md`，一樣適用）

- NEVER 主動發起 tuning
- NEVER 未過進場 gate 就調效能
- NEVER 因為「Status 不是 `Optimal`」就把專案退回 `coding` —— `Feasible` / `TimeLimit` 是情境 B / C 的進場條件，不是 FAIL
- NEVER 用錯情境的主指標（B / C 用 runtime 會全部平手、θ = 0，結論零資訊）
- NEVER 為了「讓它變 `Optimal`」而放寬 `MipGap` 或加大 `TimeLimit` —— 那是動停止契約，要使用者拍板
- NEVER 跳過 S1 / S2 直接掃 variants（沒有 θ 就沒有判定門檻）
- NEVER 動白名單（§0.1）以外的任何檔案或 `Program.cs` 的其他部分
- NEVER 一輪同時改多個旋鈕，NEVER 跑完實驗才補寫「預測」
- NEVER 重寫 Phase 2 交付的 exp 分支形狀（只補正不合契約處，並記成 finding）
- NEVER 沒刪 bin 同名檔就跑 R0（append 會混進 Phase 2 的驗證 trial）
- NEVER 不留 Experiment 紀錄就宣稱改善
- NEVER 跳過每輪的 archive 逐項驗收（A–E 任一 FAIL 未修就不得 promotion、不得進下一輪）
