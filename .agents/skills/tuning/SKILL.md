---
name: tuning
description: Phase 3 調校 orchestrator——模型與資料凍結、正確性已驗的前提下，只調 CplexConfig solver 旋鈕。啟動後連續自動執行到停損：環境定版 → R0 校準 → 策略輪迭代 → hold-out → promotion，全程落檔 TuningHistory.md。當使用者說「太慢」「timeout」「跑不完」「gap 下不去」「找不到可行解」「調參數」「tuning」「效能」時使用。使用者提出才做，NEVER 主動建議。
---

# tuning — Phase 3 調校調度

三階段 phase gate：`modeling` → `coding` → **`tuning`（本 skill）**。

你是第三棒，而且是**選配的一棒**：使用者提出效能問題才啟動，NEVER 主動建議調校。核心信念是**先驗正確，再調效能——調快一個錯模型沒有價值**；以及**先證明量得準，再開始比**。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.Codex/skills/tuning/SKILL.md`）。
> 規則單一來源：`../AGENTS.md`（天條）+ `solver-tuning-guide.md`（**Phase 3 唯一規範**）。

## 文件遵循 gate（不可跳過）

在讀取專案、規劃實驗、修改 baseline 或宣告結果前，MUST 逐一讀取並遵守完整文件集：`../AGENTS.md`、`solver-tuning-guide.md`、`ph3_tuning.md`、`checklist.md`、`Test-TuningRoundArchive.ps1`。任何檔案缺失、無法讀取、內容相互矛盾，或無法證明交付物符合其中所有適用要求時，MUST 停下並回報檔名與衝突；NEVER 猜測、挑選較方便的規則，或先實驗再補讀。

權威順序僅用於判定衝突，不會免除閱讀：`../AGENTS.md` → `solver-tuning-guide.md` → `ph3_tuning.md` → `checklist.md` → `Test-TuningRoundArchive.ps1`。交付前 MUST 完成 `checklist.md` 的所有適用項目；每輪建立 archive 時 MUST 執行 `Test-TuningRoundArchive.ps1` 並將失敗視為不可 promotion。
> 本 skill 只做調度與 gate 把關，**NEVER 在此複製規則**——每次執行都實際讀規範檔，不憑記憶。

## 執行模式：一鍵啟動，連續跑到停損

**啟動後不中途向使用者要指示。** 從 S0 一路跑到停止條件命中，中間每輪自動決定「繼續 or 停」。

唯二會中途停下的情況：**S0 進場 gate 不通過**、或**規範 §7.1 的停止條件 G/H 命中**（結果不變式破裂、判定非 tuning）。

發現契約可能訂錯（例 `MipGap` 太鬆）→ **記成 finding 繼續跑**，NEVER 中斷、NEVER 自行變更契約。

## 可寫區域白名單（規範 §0.1）

**只有四處可寫，其餘一律唯讀**：

1. `Program.cs` 的具名 production baseline `CplexConfig`（欄位值 + 上方 provenance 註解）
2. `Program.cs` **exp 分支內**的 variant 定義（一律 `baseline.Clone()` 起手）
3. 專案根 `TuningHistory.md`（追加，NEVER 改寫歷史節）
4. `status.json` 的 Phase 3 欄位

**白名單外全部唯讀**：model 組裝 chain、`Dataload`、`Set/` `Parameter/` `Variable/` `Constraint/` `Objective/` `Solution/`、`Data/*.csv`、`Model.md`、`ProjectConfig`、csproj、`dlls/`。

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

**S0-1 正確性 gate**（任一不成立即停止，回報並退回 `coding`）：

1. `dotnet build` 通過
2. **Status 落在可進場的三態之一**（規範 §0.0.1）—— **NEVER 要求 `Optimal`**
3. `coding` 的解驗證協定四步已過

`status.json` 的 `solveVerified: true` 是必要條件但不充分，**MUST 實跑一次確認**。專案若無 `Solution/` 與 `ValidateRules`，回報此缺口並改用規範 §0.1.1 的不變式作為唯一自動驗證手段。

**S0-1b 定進場情境**（規範 §0.0.1）——**這一步決定後面每一輪的主指標，選錯整輪實驗白跑**：

| `solveStatus` | 情境 | 主指標 | 備註 |
| --- | --- | --- | --- |
| `Optimal` | **A** | runtime `sgm` | 已解完，想更快 |
| `Feasible` | **B** | **endGap**（同時間預算） | 撞時限、有 incumbent、gap 未收——**最典型的進場**，NEVER 因為「不是 Optimal」就退回 `coding` |
| `TimeLimit` | **C** | 找到解的 seed 數 → `t_feas` | 連可行解都沒有；**MUST 先確認 `verifiedOn` 是 `small-instance:*`**，否則模型從未被任何 instance 驗過 → 退回 `coding` |
| `Infeasible` / `Unbounded` / `Error` | — | — | 前提破裂，退回對應 phase |

情境 B / C **NEVER 用 runtime 當主指標**——每個 trial 都跑滿 `TimeLimit`，離散度為 0、θ = 0，會得到「所有 variant 全部平手」這種零資訊結論。

**S0-2 範圍界線**（規範 §1）：判定是否真的是 tuning。要動資料 / 結構 / soft constraint → 停止並退回對應 phase。`Infeasible` 先取 IIS 證據再退回。

**S0-3 記錄 Phase 2 結果基線**（規範 §0.1.1）：實跑一次記下 `phase2Status` / `phase2Objective` / `phase2Bound` / `phase2Gap` / `verifiedOn`，寫進 `TuningHistory.md` 契約區塊。**不變式依情境分兩套**：情境 A 是 objective 嚴格相等；情境 B / C 是「不得變差 + `BestBound` 不得越過已知最佳 incumbent」。

**S0-4 契約凍結**（規範 §2.0、§6.1）：停止契約用現行值、量測契約（experiment 期間 export 全關、`ParallelMode = 1`、每輪確認無 dynamic search 停用 warning）寫進契約區塊。

**S0-5 總預算**（規範 §7.2）：估算並記錄，供停止條件 F 使用。

## S1 · 環境定版 sizing（規範 §2.3）

只掃 `Threads`：實體核心數 / −1 / −2，各 3 seeds，`ParallelMode = 1`。差距 < 10% → **取最低者**（壓低 θ，換取更靈敏的量測）。定版後寫進契約區塊並凍結。

**不計入輪次。**

## S2 · R0 校準（規範 §3.0，硬 gate）

baseline × 5 seeds（另指定 3 個 holdout seeds 全程不參與）。產出：

- **主指標 + θ**（雜訊地板）= 後續所有輪次的勝出門檻。**θ 的單位隨主指標變**：runtime 與 `t_feas` 是比值 `(max−min)/sgm`；endGap 是絕對百分點 `max−min`
- **瓶頸剖面**（從 `-trajectory.csv` 算四個量）= 後續候選的唯一來源。情境 C 直接判 **No-incumbent** 剖面
- **契約健檢探針**（`MipGap = 0`）= finding，不進排名、不中斷流程。情境 B / C 的探針**允許放大 `TimeLimit`**（僅探針，記進 history）

**R0 沒跑完不准進 S3。** 剖面 = Variability-dominated → 停止條件 A，直接收尾。

★ 情境 C 的 R0「5 個 seed 全部沒找到解」**不是**早停條件——那正是本輪要打的目標，baseline 得 0 分，照常進 S3。

**不計入輪次。**

## S2.5 · CPLEX 內建 tune 基準（規範 §3.6）

拿 `bin/.../Models/*.lp` 到 CPLEX Interactive Optimizer 跑 `tune`，零成本、不改程式。建議值**拆成獨立 variant** 進 S3 驗證，NEVER 直接 promote。

工具不可用 → **跳過不中斷**，記一行理由。

## S3 · 策略輪循環 R1..RN

每輪固定形狀（規範 §3、§4）：

1. **寫假設與預測**（規範 §6.2）—— **MUST 在跑實驗之前寫死**，防事後合理化
2. 候選**只從剖面對應那一類取**（規範 §2.2），**一輪一顆**
3. 改 `Program.cs` exp 分支的 variant 定義（`baseline.Clone()`），build，跑 `-- exp`
4. 每個 variant × 同一組 5 個 tuning seeds；seed 是共同因子不是 variant
5. 抽數：**NEVER 整份 JSON / log 讀進 context**，grep 抽 Status / objective / gap / time，軌跡另算四個量。`Status = TimeLimit` 時數值欄全是 `NaN`，**NEVER 當 0 參與彙總**
6. 裁決（規範 §4）：eligibility gate → lexicographic → **主指標**改善 > θ。情境 C 下「無可行解」**不是淘汰理由**（當成淘汰會把 baseline 一起淘汰光）
7. 寫實測與裁決，更新**已否證清單**（跨輪累積）

**每輪結束依規範 §7.1 檢查停止條件 A–I，命中即進 S4/S5 收尾；否則自動進下一輪。**

★ 情境 B / C **每輪重判剖面**——找到 incumbent 之後瓶頸會換一種。情境 C 首度產出 incumbent 即**升級為情境 B**：重跑 R0 重新定主指標與 θ，`TuningHistory.md` 記一筆「情境轉換」分隔線，轉換前後的數字不可跨線比較。

## S4 · Hold-out（規範 §4.6）

champion 用 3 個未參與調參的 seed 重跑。改善消失 → over-tuning，退回 retain。**holdout 只能估計，NEVER 用來選 config。**

## S5 · Promotion 閉環（規範 §5）

1. champion 完整設定寫回 `Program.cs` baseline + provenance 註解
2. 先寫 `TuningHistory.md`，再驗證
3. **重新 build + 跑無參數 production**（export 開回來）
4. 驗 `Status` / objective / gap，**依情境套不同 PASS 條件**（規範 §5.3）：A = objective 等於 `phase2Objective`；B = objective 不差於基線且 endGap 確實改善；C = 確實產出了 incumbent。有 `ValidateRules` 就一併驗
5. FAIL → 撤銷 promotion，記 `rejected`

無可靠勝者 → **retain + 證據**，一樣是合法交付。

## 交付

**diff 檢查**（規範 §0.1.2）：`git diff --name-only` MUST 只有 `Program.cs`、`TuningHistory.md`（+ `status.json`）。`Program.cs` 內部 diff 只允許 baseline 值、provenance 註解、exp 分支 variant 定義——model chain 出現在 diff 裡 = 越界，全部還原。

**回報**：進場情境與主指標、跑了幾輪、每輪一行（假設 → 裁決）、θ 與剖面、champion 或 retain + 理由、停止原因（§7.1 哪一條）、production 驗證結果、`TuningHistory.md` 路徑。

`status.json` **只更新下列欄位**（NEVER 整檔覆寫）：

```json
{ "phase": "tuning", "tuningRound": 0, "productionBaseline": "initial", "baselineSourceExperiment": "", "baselineSourceTrial": "", "promotionVerified": false, "updated": "YYYY-MM-DD" }
```

交付前逐條對照同資料夾的 `checklist.md`。

## Fatal

- NEVER 未過進場 gate 就調效能
- NEVER 因為「Status 不是 `Optimal`」就把專案退回 `coding` —— `Feasible` / `TimeLimit` 是情境 B / C 的進場條件，不是 FAIL
- NEVER 在情境 B / C 用 runtime 當主指標（全部跑滿時限，θ = 0，結論零資訊）
- NEVER 在情境 C 把「無可行解」當 eligibility 淘汰理由（會連 baseline 一起淘汰光）
- NEVER 把 `NaN` 的 objective / MipGap 當 0 參與彙總
- NEVER 為了「讓它變 Optimal」而放寬 `MipGap` 或加大 `TimeLimit` —— 那是動停止契約，要使用者拍板
- NEVER 跳過 S1 / S2 直接掃 variants（沒有 θ 就沒有判定門檻）
- NEVER 動白名單以外的任何檔案或 `Program.cs` 的其他部分
- NEVER 讓違反 §0.1.1 結果不變式的 trial 勝出，更 NEVER promotion
- NEVER 把契約旋鈕（`MipGap` / `TimeLimit` …）或 `Seed` 放進 variant 池
- NEVER 憑 `NodeCount` / `IterationCount` 判斷瓶頸（框架不填）
- NEVER 一輪同時改多個旋鈕
- NEVER 跑完實驗才補寫「預測」
- NEVER 用 soft constraint / penalty 繞過 infeasible
- NEVER 不留 Experiment 紀錄就宣稱改善
- NEVER 主動發起 tuning
- NEVER 用絕對路徑
