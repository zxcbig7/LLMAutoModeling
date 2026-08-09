# Solver Tuning — Phase 3 端到端調校規範

> **這份文件是什麼**：模型與資料已凍結、正確性已驗的專案跑太慢或收斂不了時，怎麼系統性地調 CPLEX solver 旋鈕，從「進場判斷」到「champion 寫回 production baseline 並驗證」的完整標準流程。
> **給誰看**：要替一個既有 OptimFoundation 專案調效能的人，以及要執行這件事的 AI。
> **怎麼用**：從 §0 的進場 gate 開始，**gate 沒過就不要往下讀**。gate 的第一件事是分 §0.0.1 的三個進場情境——**情境決定主指標，主指標選錯整輪實驗白跑**。旋鈕名稱與可設值查附錄 A，NEVER 憑記憶寫欄位名。
> **前置**：專案已通過 Phase 2 的解驗證協定四步（`status.json` 的 `solveVerified: true`），且**使用者主動提出**效能問題。求解**沒有**收斂到 `Optimal`（撞時限停在 `Feasible`，甚至沒找到任何解）**不是進場的阻礙，而是最典型的進場理由**——見 §0.0.1。
> **本檔自足**：讀這一份就能從症狀走到 promotion 完成，不需要開任何其他文件。天條全文在 [`../AGENTS.md`](../AGENTS.md)，Experiment / Config 的 API 簽名在 [`../Ph2_Coding/optimfoundation-api-guide.md`](../Ph2_Coding/optimfoundation-api-guide.md) §8–§9。

### Canonical 邊界（AI MUST 先判斷）

- **本檔是 Phase 3 的唯一權威**。進場條件、可動範圍、實驗設計、評分規則、promotion 流程一律以本檔為準。
- **權威順序**：[`../AGENTS.md`](../AGENTS.md) 的天條 → 本檔 → [`../Ph2_Coding/optimfoundation-api-guide.md`](../Ph2_Coding/optimfoundation-api-guide.md) §9 的框架簽名 → 任何既有專案 code。
- **只有一條路線**：調 `CplexConfig` 的 solver 旋鈕。沒有第二種手段。落在 §1 判定表其他格的訴求一律退回對應 phase。

---

## §0 心智模型

### 0.0 進場條件 — 先驗正確，再調效能

**依序確認，任一項不成立就停下回報，先回 Phase 2 修**：

1. `dotnet build` 通過
2. **Status 落在可進場的三態之一**（見下表）——**NEVER 要求 `Optimal`**
3. Phase 2 的解驗證協定四步已過：① Status 五態診斷 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity

`status.json` 的 `solveVerified: true` 是**必要條件但不充分**——MUST **實跑一次確認**，不能只信 JSON。

Why: 對錯的模型調參數只會**更快地得到錯答案**，而且會讓那個錯答案看起來更可信（「我們還做了效能調校」）。

#### 0.0.1 Status 決定進場情境（本階段的第一個分岔）

**「沒在時限內收斂到 `Optimal`」正是本階段最典型的進場理由，不是進場的阻礙。** 要求 `Optimal` 才准進 Phase 3，等於把最需要調校的專案永遠擋在門外——而 Phase 2 手上沒有任何合法工具能修「太慢」。

| 框架 `SolveStatus` | 進場情境 | 可否進場 | 本階段的主指標（§3.0 產出 A 決定） |
| --- | --- | --- | --- |
| `Optimal` | **情境 A · 收斂已達成** | ✅ | **runtime**（把同一個答案更快解出來） |
| `Feasible` | **情境 B · 撞限制、有 incumbent、gap 未收** | ✅ **最典型** | **同時間預算下的 endGap** |
| `TimeLimit`（中止且無任何可用解） | **情境 C · 連第一個可行解都找不到** | ✅ **但有附加前提** | **是否找到 incumbent + `t_feas`** |
| `Infeasible` | 前提破裂 | ❌ | 走 §1.1 取證，退回 Phase 1 / 2 |
| `Unbounded` | 前提破裂 | ❌ | 退回 Phase 2 補界限 constraint |
| `Error` / `NotSolved` | 執行面問題 | ❌ | 先修到跑得起來 |

**情境 C 的附加前提**：`TimeLimit` 代表沒有任何解，②③ 兩步驗不了——這時 `solveVerified` MUST 是在**小 instance** 上取得的（`status.json` 的 `verifiedOn` 為 `small-instance:*`）。沒有任何 instance 驗過模型就進場 = 你在對一個從未被驗證過的模型調參數，**停下退回 Phase 2**。

**NEVER 為了「進場好看」而放寬契約**：把 `MipGap` 調鬆或 `TimeLimit` 調大讓 `Feasible` 變成 `Optimal`，是動停止契約（§2.0.1），會讓整個 before/after 失去共同基準。情境 B 就用情境 B 的量法。

Why 要在進場就分情境：三個情境的**主指標不同**。情境 B 的每個 trial 都會跑滿 `TimeLimit`，runtime 完全沒有鑑別力——沿用情境 A 的 runtime 判準，會得到「所有 variant 全部平手、θ = 0」這種毫無資訊的結論，而真正的差異全在 gap 上。主指標選錯，整輪實驗白跑。

**另一個前置**：資料驗證。`OptData.Load` 自動檢查 Set／Parameter 重複 key 與 Parameter 數值 sanity；Parameter→Set 關聯由開發者掌握，Set-driven lookup 使用 `FindParameterOrLog` 留下缺值 Warning。資料契約未驗清楚就別調參數，否則只會更快得到錯答案。

Gate 順序固定：**資料驗證（載入時自動）→ 解驗證協定四步（Phase 2）→ 才進本檔**。

### 0.1 凍結範圍 — 可寫區域白名單

進場時模型與資料已凍結、正確性已驗（§0.0.1 的三個進場情境之一）。本階段的可寫區域是**白名單**（只有名單上的可以動，其餘一律不行）：

| # | 可寫區域 | 用途 | 限制 |
| --- | --- | --- | --- |
| 1 | `Program.cs` 的具名 `CplexConfig` production baseline | promotion 唯一寫回點 | 只改欄位值與其上方 provenance 註解 |
| 2 | `Program.cs` **exp 分支內**的 variant 定義 | 每輪實驗的 `Clone()` 與旋鈕設定 | 只在 exp 分支內；一律 `baseline.Clone()` 起手 |
| 3 | 專案根 `TuningHistory.md` | 決策紀錄 | 每輪追加，NEVER 改寫歷史節 |
| 4 | `status.json` 的 Phase 3 欄位 | 進度 | 只更新自己負責的欄位 |
| 5 | repo 根 `_wip/<Project>/t<N>-*.md` | 中間產物 | 草稿區 |

**白名單之外一律唯讀，包含**：model 組裝 chain、`Dataload`、`Set/` `Parameter/` `Variable/` `Constraint/` `Objective/` `Solution/`、`Data/*.csv`、`Model.md`、`ProjectConfig`、csproj、OptimFoundation 框架與 `dlls/`。

#### 0.1.1 Phase 2 結果不變式（本階段最硬的驗收點）

**tuning NEVER 改變 solver 該回答的那個答案。** 進場時 MUST 先記錄 Phase 2 的求解結果當作基線，寫進 `TuningHistory.md` 契約區塊：

```text
phase2Status     = Optimal | Feasible | TimeLimit
phase2Objective  = <值>（TimeLimit 時為 n/a）
phase2Bound      = <值>（TimeLimit 時為 n/a）
phase2Gap        = <值>（TimeLimit 時為 n/a）
```

不變式**依 `phase2Status` 分兩套，套錯會把正常結果誤判成越界、或把越界放行**：

**情境 A（`phase2Status = Optimal`）— 嚴格相等**

| 情況 | 判定 | 動作 |
| --- | --- | --- |
| 同契約下 objective 與基線一致 | 正常 | 繼續 |
| 某 trial 的 objective 偏離基線 | **異常 trial** | 該 trial 淘汰並記錄，NEVER 當成「解更好」直接採用 |
| promotion 後 production objective 偏離基線 | **調壞了** | **立即回退 baseline**，記 `rejected` |

Why: 同一個模型、同一份資料、同一個停止契約下，最佳解就是那一個。objective 變了只有三種可能：契約被動到、數值容差被放寬（`IntegralityTolerance` 之類）、或框架/模型被誤改——三種都是本階段的越界。

**情境 B / C（`phase2Status = Feasible` 或 `TimeLimit`）— 單調不退步 + bound 不矛盾**

基線只是一個 incumbent，不是最佳解。**要求 objective 嚴格相等在這裡是錯的**——incumbent 變好本來就是 tuning 該有的產出。改判三條，全部機械可驗：

| # | 規則 | 違反代表什麼 |
| --- | --- | --- |
| 1 | **不得變差**：min 問題 objective 不得高於 `phase2Objective`；max 反之（容差取 `MipGap` 量級） | 該 trial 更差，正常淘汰（不是越界） |
| 2 | **bound 不得越線**：min 問題任一 trial 的 `BestBound` 不得高於全期已知最佳 incumbent；max 反之 | **越界**——bound 越過已知可行解在數學上不可能，代表容差被放寬或模型被誤改 |
| 3 | **Optimal 錨點**：任一 trial 若回報 `Optimal`，其 objective 即為真最佳值；此後任何 trial 的 incumbent **優於**它 → 越界 | 同上 |

規則 2 與 3 是情境 B / C 唯一能偵測「調壞了」的手段——**MUST 每輪執行**。放棄了它們，情境 B 就變成沒有任何越界防線的裸奔。

★ 專案若無 `Solution/` 與 `ValidateRules`（解驗證協定未實作），上述不變式就是唯一能自動檢查「解沒被調壞」的手段，不可省略。

#### 0.1.2 交付時的 diff 檢查（機械可驗）

**`git diff --name-only` MUST 只出現這兩個檔**：

```text
Projects/<Project>/Program.cs
Projects/<Project>/TuningHistory.md
```

（`status.json` 若已納管則為第三個；`_wip/` 屬草稿區不計。）

多出任何 `.csv` / `Constraint_*.cs` / `Objective/*.cs` / `Model.md` / csproj 的改動 = **本階段越界，MUST 全部還原**。

`Program.cs` 內部的 diff 也要逐行檢查，**只允許兩種**：baseline 的欄位值與其 provenance 註解、exp 分支內的 variant 定義。model 組裝 chain 出現在 diff 裡 = 越界。

Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，該輪實驗證據**整批作廢**。而 §0.1.1 的結果不變式是同一件事的另一面：**結構不變**（diff 檢查）且**結果不變**（objective 對照），兩者都過才算沒有越界。

### 0.2 完成的定義

**完成 ≠ 跑出 experiment CSV。**

完成 = champion 已 promotion 到 production baseline **且** production 重跑驗證通過（或有證據支持保留原 baseline）。只產出 experiment 報表而 production 仍跑舊 config，**不算完成**——下一輪會有人重做同一組實驗。

沒有可靠勝者時，「**retain + 證據**」也是合法交付，但一樣要寫進 `TuningHistory.md`。

### 0.3 本階段不做的事

- NEVER **主動建議** tuning——使用者提出才做
- NEVER 用 soft constraint / penalty 讓 infeasible 模型「有解」——旋鈕不會把 infeasible 變 feasible，只會讓你更快確認它 infeasible
- NEVER 以 tuning 名義移項 / 改號 / 翻方向 / 四捨五入或修改輸入精度
- NEVER 沒有 `OptExperiment` 記錄就宣稱改善
- NEVER 自行升級去改模型結構——那是新一輪 Phase 1，由使用者決定要不要走

---

## §1 範圍界線 — 先確認這真的是 tuning

使用者的訴求落在下表哪一格，決定你要不要繼續：

| 使用者要的 | 是不是 tuning | 動作 |
| --- | --- | --- |
| 太慢 / gap 收不下來，**已 `Optimal` 但想更快** | ✅ **是**（情境 A） | 進 §2 |
| 撞 `TimeLimit` 停下、**有 incumbent 但 gap 未收**（`Feasible`） | ✅ **是**（情境 B，最典型） | 進 §2；主指標是 endGap 不是 runtime |
| 跑滿時限**連第一個可行解都沒有**（`TimeLimit`） | ✅ **是**（情境 C） | **先確認 `verifiedOn` 是 `small-instance:*`**——模型從未被任何 instance 驗過 → 退回 Phase 2；驗過才進 §2，剖面強制 Primal-search |
| 換參數值、換一批資料 | ❌ 否 | 換 `Data/*.csv` 重跑即可，本階段不介入 |
| 加刪約束、改 Big-M、reformulation、換 formulation | ❌ 否 | 退回 Phase 1 改 `Model.md`，確認後由 Phase 2 重走轉譯 |
| 要放鬆某條限制（soft constraint / penalty） | ❌ 否 | 那是**建模決定**：回 Phase 1 寫進 Model.md（含 penalty 的具名 PARAM 與被放鬆的條目），確認後由 Phase 2 照常轉譯 |
| `Infeasible` | ❌ 否 | **前提破裂**：跑 IIS 拿最小衝突集當**證據**，退回 Phase 1 / 2（見 §1.1） |
| `Unbounded` | ❌ 否 | **前提破裂**：某方向漏了界，退回 Phase 2 補該變數的上限 constraint |

**使用者指定的方向不是 gate 的豁免。** 「試試 emphasis」「加 cuts」是 §3 的 variant 候選；正確性沒過就先回 Phase 2，並說清楚為什麼還不能調。指定方向也不解除「一輪只改一個旋鈕」。

### 1.1 Infeasible 的取證流程

Infeasible 幾乎都是模型或資料的錯，不是 solver 的錯。在這裡調旋鈕沒有任何意義——你要產出的是「該退回哪裡、退回去要修什麼」的**證據**，不是修法本身。

1. 讀 `bin/Debug/net8.0/IISs/*.ilp`（`Solve()` 遇到 Infeasible 會自動 `RefineConflict` 寫出），或呼叫 `engine.GetConflictConstraints()`
   **NEVER 整檔讀進 context**——先 grep 出約束名稱清單，再針對命中的名稱回 `Constraint/` 找對應 `.cs` 與 Model.md 條目
2. 對每個名稱，回 Model.md 找該條的語意
3. 判定根因：

| 根因 | 徵狀 | 退回哪裡 |
| --- | --- | --- |
| 資料值矛盾 | 需求總量 > 總產能 | Phase 1（資料前提）或換 CSV |
| 模型結構矛盾 | 兩條 hard constraint 互斥 | Phase 1 改 Model.md |
| Big-M 太小 | 砍掉了合法解 | Phase 1 重推 M 的上界 |
| 界限設錯 | LB > UB | Phase 1 / 2 |

4. **NEVER 建議改成 soft constraint** 讓它有解——放鬆模型是改語意，屬 Phase 1 由使用者決定
5. **NEVER 自己動手修**——本階段只出證據

---

## §2 旋鈕分類與瓶頸診斷

先分類旋鈕，再從**收斂軌跡**量化瓶頸，最後才對應候選。**不是盲搜，也不是憑印象讀 log。**

### 2.0 旋鈕四分類（本階段的地基）

任何旋鈕動手前先歸類。**歸錯類，後面所有比較都無效。**

| 類別 | 成員 | 實驗中的角色 |
| --- | --- | --- |
| **契約旋鈕**<br>定義「什麼叫解出來了」 | `MipGap` `AbsoluteMipGap` `TimeLimit` `DeterministicTimeLimit` `NodeLimit` `IntegerSolutionLimit`；容差 `IntegralityTolerance` `OptimalityTol` `FeasibilityTol` | 整個 tuning 週期**固定共用**，NEVER 進 variant 池。變更 = 週期重啟（§2.0.1） |
| **環境旋鈕**<br>改變執行環境與量測基準 | `Threads` `ParallelMode` `MemoryLimitMb` `TreeMemoryLimitMb` `NodeFileStrategy` | **R0 之前先單獨定版**（§2.3），之後整期凍結。NEVER 與策略旋鈕同輪比較 |
| **策略旋鈕**<br>改求解路徑，終點不變 | `Emphasis` `VariableSelect` `NodeSelect` `BranchDirection` `DiveType` `MipSearch` `Probe` `RinsHeuristicFrequency` `HeuristicEffort` 各 cuts `CutsFactor` `CutPasses` `RootAlgorithm` `NodeAlgorithm` `Symmetry` `Presolve` `NumericalEmphasis` | **唯一的 variant 池** |
| **量測器材**<br>不是 candidate | `Seed` `ClockType` | `ClockType` 整期固定；`Seed` 是 §3.0 的**自變數**，NEVER 當 champion |

兩句判準：

- **這顆旋鈕改了之後，兩次求解還算不算在做同一件事？** 不算 → 契約類
- **改了之後 θ 還算不算同一把尺？** 不算 → 環境類

❌ 最常見的兩個歸類錯誤：

- 把 `MipGap` 當速度旋鈕掃 —— 它是停止條件。兩個不同 `MipGap` 的 trial 比 runtime，等於兩個跑者跑不同長度的賽道比秒數
- 把 `Seed` 當 candidate 排名 —— 它不是策略，是同一個策略的第二次抽樣。它「贏」只證明變異大，不證明它比較好

#### 2.0.1 契約變更協定

tuning 過程中發現契約可能訂錯（例：`MipGap` 過鬆導致 production 常態交付次佳解）→

1. **回報，NEVER 自行變更** —— 這是品質決策，使用者拍板
2. 契約一改 → **歷史 runtime 全部作廢**（終點線移了），重跑 §2.3 sizing 與 §3.0 R0
3. `TuningHistory.md` 記一筆「契約變更」分隔線

#### 2.0.2 `Threads` 為什麼是環境層而不是策略層

它同時改變三件與量測有關的東西：

1. **記憶體預算** —— CPLEX 的 `MemoryLimitMb`（WorkMem）管的是 **live tree 大小**，不是總記憶體；threads 多 → 樹長得快 → 更早觸發儲存策略切換
2. **同步成本結構** —— `ParallelMode` 決定論模式的同步代價隨 threads 上升
3. **θ 本身** —— threads 越高，執行緒時序造成的路徑分歧越大，雜訊地板跟著漂移

第 3 點是關鍵：θ 是本階段所有判定的門檻。**threads 浮動時量 θ，等於用會伸縮的尺量東西。**

已知交互（歸類依據）：

| 組合 | CPLEX 機制 | 後果 |
| --- | --- | --- |
| threads × `MemoryLimitMb` | WorkMem 管 live tree 大小，超過即換儲存策略 | threads 多 → 更早觸發切換 |
| threads × `NodeFileStrategy` | **框架的雷**：`Configuration()` 設 `MemoryLimitMb` 時強制 `MIP.Strategy.File = 0`；CPLEX 預設是 `1`（壓縮後留記憶體） | 框架把壓縮策略**關掉了**，比預設更糟；threads 越高越早 OOM |
| threads × `RootAlgorithm = 6`（concurrent） | concurrent optimizer 讓多個 LP 演算法各佔一 thread | root 吃掉 threads，B&B 平行度下降 |
| threads × **首解幾乎即最佳的模型** | root / ramp-up 佔比高，樹未長大即解完 | 純同步開銷、零收益 |
| threads × cuts × heuristics | cut pass 同時牽動 heuristic 與 probing 的平行配置 | 三方耦合，一輪一顆的設計掃不出來（§9 反模式） |

### 2.1 收斂軌跡剖面 → 瓶頸判斷

**資料來源**：`bin/.../Experiments/<name>-trajectory.csv`（框架自動產出，欄位 `RunAt, Label, PointIndex, TimeMs, Objective, Bound, Gap`）。

★ **NEVER 用 `NodeCount` / `IterationCount` 做診斷** —— 框架目前不填這兩個欄位（experiment CSV 中為空）。憑它們判斷「node 數暴衝」「node throughput 低」是不可執行的指引。

從軌跡計算四個量：

| 量 | 定義 | 怎麼算 |
| --- | --- | --- |
| `t_feas` | 首個可行 incumbent 出現時間 | 第一個 `Objective` 落入合理量級的點的 `TimeMs` |
| `r_primal` | primal 側佔全程比例 | `t_feas / t_total` |
| `Δbound` | dual 側淨移動 | `Bound` 末點 − 首點 |
| `t_stall` | bound 最後一次改善的時點 | `Bound` 不再變化的起始 `TimeMs` |

分類 → 決定候選旋鈕類。**只掃對應那一類，NEVER 查全表盲掃**：

| 剖面 | 判準 | 瓶頸 | 候選 |
| --- | --- | --- | --- |
| **No-incumbent** | `t_feas` **不存在**（整條軌跡無 `Objective` 點，`Status = TimeLimit`） | 連一個可行解都生不出來 | 同 Primal-search，但**優先度最高且順序不同**：`Emphasis = 4` → `1` → `NodeSelect = 0`（DFS）→ `DiveType = 2/3` → `RinsHeuristicFrequency` / `HeuristicEffort` |
| **Dual-bound** | `r_primal` 小、`Δbound` 小、`t_stall` 早 | 證明 bound | `Emphasis = 3`（**BESTBOUND**）、`GomoryCuts` `MirCuts` `CoverCuts` `CliqueCuts` `FlowCoverCuts` `CutsFactor` `CutPasses` `Probe` `Symmetry` |
| **Primal-search** | `r_primal` 大 | 找可行解 | `Emphasis = 1`（FEASIBILITY）或 `4`（HIDDENFEAS）、`RinsHeuristicFrequency` `HeuristicEffort` `NodeSelect = 0` `DiveType` |
| **Node-cost** | 兩者皆不明顯、軌跡點稀疏而總時長 | 單 node 太貴 | 減 cuts、`RootAlgorithm` `NodeAlgorithm` `Presolve`（threads 已在 §2.3 定版，不在此掃） |
| **數值不穩** | log 有數值警告、解不穩定 | ill-conditioning | `NumericalEmphasis = true`（係數量級問題要回 Phase 1） |
| **Variability-dominated** | `max/min > 2` 且無明顯瓶頸 | 雜訊主導 | **停止**，回報「此規模量不出可靠改善」（§7 早停 A） |

★ **判讀陷阱**：**軌跡末點 ≠ 最終解**。它是 callback 觀測快照，最終值一律以 `<name>.csv` 的 `ObjectiveValue` 為準（實測曾出現末點 3.8 而最終值 3.7）。

★ **No-incumbent 的資料形狀**：`Status = TimeLimit` 時 `ObjectiveValue` / `BestBound` / `MipGap` 全是 `NaN`，`<name>.csv` 那幾欄是空的。**NEVER 把 `NaN` 當 0 參與彙總**——診斷與排名一律改用軌跡的 `t_feas` 是否存在（§3.0 產出 A 情境 C）。

★ **剖面在情境 B / C 下 MUST 每輪重判**：找到 incumbent 之後瓶頸會從 No-incumbent 轉成 Primal-search 或 Dual-bound，沿用 R0 的剖面會繼續掃已經不是瓶頸的那一類。情境 A 沿用 R0 剖面即可。

★ **跨 variant 的 bound 軌跡若起訖完全一致** → 該類旋鈕對 dual 側無效，整類標記「已否證」（§7 早停 B）。

### 2.2 剖面 → 候選旋鈕（只列策略旋鈕）

本表只含**策略旋鈕**。契約旋鈕（`MipGap` `TimeLimit` `NodeLimit` `IntegerSolutionLimit`）與環境旋鈕（`Threads` 等）不在此掃 —— 見 §2.0。

| 剖面 | 候選（依序試，一輪一顆） |
| --- | --- |
| **No-incumbent** | `Emphasis = 4`（HIDDENFEAS）→ `Emphasis = 1` → `NodeSelect = 0`（DFS，最快下潛到葉節點）→ `DiveType = 2/3` → `RinsHeuristicFrequency` → `HeuristicEffort`。**NEVER 先加 cuts**——cut 推的是 bound，這裡連可行解都沒有，加 cuts 只會讓每個 node 更貴 |
| **Dual-bound** | `Emphasis = 3` → 加切割（`GomoryCuts` / `MirCuts` / `CoverCuts` / `CliqueCuts` / `FlowCoverCuts`）→ `CutsFactor` / `CutPasses` → `Probe` → `Symmetry` |
| **Primal-search** | `Emphasis = 1` → `RinsHeuristicFrequency` → `HeuristicEffort` → `NodeSelect = 0`（DFS）→ `DiveType` → `Emphasis = 4`（前者無效時的後備） |
| **Node-cost** | 減 cuts（各 cut 設 `-1`）→ `RootAlgorithm` / `NodeAlgorithm` → `Presolve` |
| **數值不穩** | `NumericalEmphasis = true` |

#### `Emphasis` 五個值的官方語意 —— 選錯等於掃錯方向

| 值 | 名稱 | 行為 | 適用剖面 |
| --- | --- | --- | --- |
| 0 | BALANCED（預設） | 平衡「快速證明最佳」與「早期找到高品質可行解」 | — |
| 1 | FEASIBILITY | 更頻繁產出可行解，**犧牲證明最佳的速度** | Primal-search |
| 2 | OPTIMALITY | 較少心力找早期可行解 | 兩者之間，**不是 bound 專用** |
| 3 | **BESTBOUND** | **更大力度推動 best bound，找可行解變成幾乎附帶** | **Dual-bound** |
| 4 | HIDDENFEAS | 全力找「很難找到」的高品質可行解；`1` 找不到好解時才用 | Primal-search 後備 |

★ **推 bound 是 `3` 不是 `2`。** 這是實測踩過的坑：Dual-bound 剖面下試 `Emphasis = 2` 完全沒有效果。

#### 記憶體相關（屬環境層，在 §2.3 處理，不進 variant 池）

`TreeMemoryLimitMb` + `NodeFileStrategy = 2/3`，**順序有雷**：框架設 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy`（附錄 A ★）。

### 2.3 環境定版（sizing）—— R0 之前的一次性步驟

環境旋鈕改變 θ（§2.0.2），所以 **MUST 先定死再量 θ**。這一步只做一次，**不計入輪次**。

**做法**：固定 `ParallelMode = 1`（決定論，換取可比性），只掃 `Threads`，候選三個：**實體核心數 / −1 / −2**。NEVER 用邏輯核心數（超執行緒下多 thread 爭用記憶體存取，常比實體核心數更慢）。每個候選跑 3 個 seed，比 `sgm(runtime)`。

**判定**：

| 觀察 | 結論 |
| --- | --- |
| 三個候選差距 < 10% | threads **對本模型無作用空間**，**取最低者**凍結 |
| 有明顯勝者 | 取之，凍結 |
| 記憶體警告或 node file 溢寫 | 降 threads，或先處理 `MemoryLimitMb` / `NodeFileStrategy` 的順序雷，再重跑 sizing |

**為什麼平手時取最低者**：threads 高會抬高 θ（執行緒時序分歧），θ 抬高就更難偵測策略旋鈕的真實改善。**效能相當時選低 threads = 買到更靈敏的量測。** production 要不要開更高是 promotion 後的獨立決定。

**定版後**把 `Threads` / `ParallelMode` / `MemoryLimitMb` / `NodeFileStrategy` 的確切值寫進 `TuningHistory.md` 契約區塊。之後任一項變動 → **重跑 sizing 與 R0**。

### 2.3.1 CPLEX 專屬前提：dynamic search MUST 全程啟用

CPLEX 的 **dynamic search** 是預設求解演算法，有一個會被靜默關閉的條件：

> **只要應用程式存在 control callback，CPLEX 就關閉 dynamic search、發出 warning、改用 static branch and cut。**

退回 traditional B&C 是**數量級的效能事件**，不是微調。含意：

- **informational callback（`MIPInfoCallback`）與 dynamic search 相容**，不影響效能、不干擾搜尋空間 → 框架的 `ITrajectorySource.EnableTrajectory()` 屬這一類，**§2.1 的軌跡診斷安全可用**
- 附錄 B 列的三個 callback 缺口（Heuristic / Lazy constraint / User cut）**一旦補上就是 control callback** → 補上那天 dynamic search 被關閉，**所有歷史 tuning 數據作廢**，MUST 重跑 §2.3 與 §3.0
- 每輪 MUST 確認 solver log 無 dynamic search 停用 warning

此機制為 CPLEX 獨有，**NEVER 從其他 solver 的文件類推**。

### 2.4 規模預警

`EngineBase.Solve()` 在求解前跑 `PreSolveGuard()`：`RegisteredVariableCount` 超過 `ScaleWarnThreshold`（預設 `10,000,000`）就 `Logging.Warn`（`[MODEL_SCALE_WARNING]`），**只警告不中止**。

看到這個警告 → **不是調旋鈕的時機**。模型太大是結構問題（該縮 set / 拆問題），回報使用者並建議退回 Phase 1，NEVER 靠 `Threads` 硬扛。

### 2.5 模型層手段不屬本階段（列出來是為了辨識）

下列都是**真正有效**但**只適用於模型尚未定版時**（Phase 1）的手段。進了 Phase 3 模型已凍結，看到這類訴求 → 依 §1 退回 Phase 1，NEVER 在本階段做：

| 手段 | 為什麼有效 | 屬於 |
| --- | --- | --- |
| 變數型態降級（BV → IV → CV） | 求解難度 CV ≪ IV < BV；網路流 / 指派問題具 totally unimodular 結構時 LP 鬆弛即得整數解 | Phase 1 |
| 聚合限制式、移除 dominated 限制式 | 縮小搜尋空間 | Phase 1 |
| 收緊變數上下界 | 直接收斂 LP 鬆弛、減少分支 | Phase 1（且界限一律寫成獨立 constraint） |
| Big-M 取最小可行值 | M 過大 → LP 鬆弛鬆散、節點爆增 | Phase 1 |
| 對稱性消除（排序限制式 / lexicographic） | 避免探索大量等價分支 | Phase 1 |
| Warm start / MIP start | 快速建立 incumbent、提早剪枝 | Phase 1（且框架目前**未提供**注入 API，見附錄 B） |
| Lazy constraints / user cuts / heuristic callback | 延遲生成大量潛在限制式 | Phase 1（且框架**未提供** callback 註冊點，見附錄 B） |

**模型已定版後只剩旋鈕一條路。** 旋鈕連調 3 輪無改善 → 依 §7 停止並回報，要不要回頭改模型結構是**使用者的決定**，不是 tuning 的下一步。

---

## §3 實驗設計

### 3.0 R0 校準輪 —— 不調任何東西（硬 gate）

**R0 沒跑完不准進 R1。** 它產出的常數是後續每一輪的判定依據；沒有它，任何「改善」都無法與雜訊區分。

**variant 池**：只有 baseline 一個 config，跑 `K` 個 seed。`K = 5`（MIPLIB / SCIP 實務慣例）。另指定 **3 個 holdout seeds 全程不參與調參**（§4.6 用）。

R0 **不計入 `tuningRound`**，它是校準不是輪次。

#### 產出 A · 主指標與雜訊地板 θ（後續所有輪次的勝出門檻）

**先定主指標，再量 θ。** 主指標由 §0.0.1 的進場情境決定，**NEVER 一律用 runtime**：

| 情境 | R0 的 K 個 seed 呈現 | 主指標 | 為什麼不能用 runtime |
| --- | --- | --- | --- |
| **A**（`Optimal`） | 都在時限內解完，runtime 有差異 | **runtime** | — |
| **B**（`Feasible`） | **全部跑滿 `TimeLimit`** | **endGap**（時限到期時的 `MipGap`）；輔助指標 `Δbound`、`t_feas` | 每個 trial 的 runtime 都等於 `TimeLimit`，離散度為 0 → θ = 0 → 所有 variant 全部「平手」，整輪實驗零資訊 |
| **C**（`TimeLimit`） | 全部跑滿且**無 incumbent**，數值欄全 `NaN` | **① 找到 incumbent 的 seed 數（主）② 這些 seed 的 `t_feas`（次）** | 連 objective 與 gap 都是 `NaN`，沒有任何連續量可比 |

彙總方法（依主指標套用）：

| 主指標 | 中心值 | 離散度 | 特殊值處理 |
| --- | --- | --- | --- |
| runtime（A） | **shifted geometric mean**（`sgm`），shift 與典型求解時間同量級：秒級 → 1s；分鐘級 → 10s | `max / min` | 逾時以 **PAR10** 計入（罰 10 × `TimeLimit`），NEVER 當成「剛好等於時限」 |
| endGap（B） | **算術平均**（gap 已是比值，不再取幾何平均） | `max − min`（絕對百分點） | 某 seed 無 incumbent → 該 seed 的 gap 記為 **100%**，並在報告註明有幾個 |
| t_feas（C） | 對**有找到解**的 seed 取 `sgm` | `max / min` | 沒找到解的 seed 以 **PAR10**（10 × `TimeLimit`）計入，NEVER 直接剔除——剔除等於獎勵「只在少數 seed 上碰運氣」的 variant |

**θ 的定義（依主指標）**：

- 情境 A：`θ = (max − min) / sgm`（runtime，比值）
- 情境 B：`θ = max − min`（endGap，**絕對百分點**）
- 情境 C：`θ = (max − min) / sgm`（t_feas，比值）；另**找到解的 seed 數差 ≤ 1 一律視同平手**

**θ 的用法（三情境相同）**：任一輪次主指標的改善幅度 ≤ θ 一律**視同平手**，不得 promote。

★ **情境 B 的 runtime 不是沒用，是不能當主指標**。它退居「隱藏代價」檢查：某 variant gap 收得更好卻提早結束（runtime < `TimeLimit`）→ 通常代表它撞到別的停止條件，要查清楚而不是直接當勝者。

#### 產出 B · 瓶頸剖面

依 §2.1 從 `-trajectory.csv` 計算 `t_feas` / `r_primal` / `Δbound` / `t_stall`，分類成 No-incumbent / Dual-bound / Primal-search / Node-cost / 數值不穩 / Variability-dominated。

**分類結果決定後續每一輪的候選來源**（§2.2），這是「不盲搜」的機制。

進場情境 C（`TimeLimit`）**直接判為 No-incumbent 剖面**，不必再算 `r_primal` / `Δbound`——沒有 incumbent，那兩個量算不出來。

#### 產出 C · 契約健檢探針

跑一個 `MipGap = 0` 的**探針**（不是 variant，不進排名、不參與 champion 判定），取得：

- 真最佳目標值
- 現行契約下的品質損失 = `(obj_contract − obj_true) / obj_true`
- 取得真最佳所需的額外時間

★ **情境 B / C 的探針要放寬時限才有意義**：`MipGap = 0` 但 `TimeLimit` 不變的話，探針只會再撞一次時限、拿不到真最佳值。**探針（且僅探針）允許放大 `TimeLimit`**——它不參與排名，不影響 variant 之間的可比性。放大倍數與結果 MUST 記進 `TuningHistory.md`。放大後仍拿不到 `Optimal` → 記「真最佳值未知」，流程照常往下跑，NEVER 因此中斷。

**這是發現，不是決策**：結果寫進 `TuningHistory.md` 當 finding 回報使用者，**NEVER 因此自行變更契約**（§2.0.1），也**NEVER 中斷流程** —— 流程繼續用現行契約往下跑。

#### R0 出口 gate（機械可判）

| 檢查 | 通過條件 |
| --- | --- |
| **主指標已定** | 依 §0.0.1 情境選定 runtime / endGap / t_feas 之一，寫進 `TuningHistory.md` 契約區塊 |
| θ 已算出 | 有確切數值，非「≥ 某值」的下限；單位與主指標一致 |
| 剖面已分類 | 落在 §2.1 六類之一 |
| 結果不變式 | 依 `phase2Status` 套 §0.1.1 對應那一套（情境 A 嚴格相等；情境 B / C 不退步 + bound 不越線） |
| dynamic search | solver log 無停用 warning（§2.3.1） |
| 契約與環境已定版 | 已寫進 `TuningHistory.md` 契約區塊 |

**剖面 = Variability-dominated → 不進 R1**，直接依 §7 早停 A 收尾回報。

★ 情境 C 有一個專屬的 R0 結果：**K 個 seed 全都沒找到 incumbent**。這**不是**早停條件——它正是本輪要打的目標（主指標 = 找到解的 seed 數，baseline 得 0 分）。照常進 R1 掃 No-incumbent 候選。

### 3.1 一輪只改一個旋鈕

一次改三個然後變快了，你學不到任何可複用的知識，下一輪只能重新亂試。要比較兩個旋鈕就開兩個 variant。

### 3.2 variant 一律從 production baseline `Clone()`

```csharp
// Program.cs 材料段：整個檔只有這一顆具名 baseline
var productionBaseline = new CplexConfig
{
    epGap = 0.03,
    timeLimit = 300,
    workThreads = 8,
    parallelMode = 1,
    randomSeed = 42,
};

// exp 分支：每個 variant 從 baseline Clone 後只改一個旋鈕
var baseline = productionBaseline.Clone();

var emphasis = baseline.Clone();
emphasis.Emphasis = 2;

var tighterGap = baseline.Clone();
tighterGap.epGap = 0.01;

var threads4 = baseline.Clone();
threads4.workThreads = 4;

var result = new OptExperiment("<Project>-tuning-r1", "一次只改一個 solver 旋鈕")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=optimal", emphasis)
    .AddConfig("r1-gap=0.01", tighterGap)
    .AddConfig("r1-threads=4", threads4)
    .Run();
```

- **NEVER 用 tune delegate 突變共用 config**——一律 `Clone()` 產具體物件
- **NEVER 另立一份「實驗 baseline」**與 production config 平行維護，兩份一定會漂移

### 3.3 experiment 命名

**MUST `<Project>-tuning-r<N>`，每輪 N 遞增。**

Why: 同名實驗是 **append 不是覆寫**。`Run()` 內的 `Save()` 會先讀既有 JSON，把歷史 trials 合併進回傳值的 `result.Trials`。重跑同名 experiment 後直接 `foreach (result.Trials)`，會再次看到歷史資料並把舊 trial 誤報成本輪結果。

### 3.4 降噪：這一段不做，結論就是雜訊

MIP 有 **performance variability**：換機器、置換 row/column 順序、換 random seed 都可能讓求解時間差數倍，根因是 branch-and-cut 的 imperfect tie-breaking。

| 措施 | 做法 | 為什麼 |
| --- | --- | --- |
| **多 seed** | 每個 variant 跑**同一組 K 個 tuning seeds** | 單 seed 的差異多半是噪音 |
| **seed 是共同因子** | seed 是所有 variant 共用的重複量測，**NEVER 當成一個 variant** | 固定 seed 調參會 over-tune 到那個 seed；把 seed 放進 variant 池排名則是類別錯誤 |
| **warm-up 排除** | 第一個 solve 當 warm-up，**不計入** | cold-start 會固定懲罰第一個跑的 variant |
| **順序輪替** | variants 跨 seed 輪替或隨機化執行順序 | 避免固定讓 baseline 承擔 cold-start |
| **hold-out** | 留 3 個 seed 全程不參與調參（§4.6）；有多 instance 時升級為 train / test instance 分離 | 只在調參用的樣本上漂亮 = over-tuning |
| **共用一份 data** | 所有 cell 引用同一份 `OptData.Load` 結果，載入後視為唯讀 | 兩份資料會中途漂移，實驗結果就不能回答 production 的問題 |
| **決定論設定** | `ParallelMode = 1` + 固定 `Seed` + `DeterministicTimeLimit` | 否則多執行緒計時不可比 |

**θ 的量測已升格為 §3.0 的 R0 校準輪，是硬 gate。** 沒有 θ 就沒有判定門檻——baseline 自己的 seed 之間若差 2x，任何小於 2x 的「改善」都不算數。這一步直接決定後面判不判得出勝負，NEVER 跳過或用估計值代替。

### 3.5 實驗 runner 的行為

| 規則 | 說明 |
| --- | --- |
| experiment 預設安靜 | solver log、LP/MPS/Sol export、housekeeping 預設全 OFF |
| 笛卡兒積 | `.AddModel` × `.AddConfig` 自動展開；單一 cell 用 `.AddTrial(model, label, config)` |
| label | 自動成為 `ModelName \| config-label`；輪次前綴（`r1-`）寫進 config label |
| `OnSolved` 邊界 | **只屬 `OptProject`**，`OptExperiment` 沒有——掃描中不要大量寫 solution |
| label 重複 | `Run()` 在建任何 engine 前丟 `InvalidOperationException`；重複的 `AddConfig` label 在加入當下丟 `ArgumentException` |
| 輸出 | `Experiments/<name>.csv`（一列一 Trial，給人 / Excel）+ `.json`（巢狀含 `config` / `metrics` / `convergence[]`，給 LLM） |
| log 檔名 | exp 模式 MUST 在 `OptData.Load` **之前** `Logging.SetLogFileName("<Project>_exp")`，整次執行才收在同一包 |

要覆寫 project-level defaults 時用 `.UseConfig(() => projectConfig)`；factory 每個 cell 都會執行。一般 solver 掃描保留預設即可。

---

### 3.6 CPLEX 內建 tuning tool 基準（零成本，不改程式）

在 R0 之後、R1 之前跑一次。CPLEX 自帶 tuning tool，**所有 API 與 Interactive Optimizer 皆可用**。框架未封裝 `TuneParam`（附錄 B 缺口），但這條路**繞得過去**：

`ProjectConfig.ExportLP = true` 已在 `bin/.../Models/` 產出 `.lp` 檔 → 拿它到 CPLEX Interactive Optimizer 跑 `tune`，**一行程式都不用改**。

```text
read <ProjectName>_LP_<timestamp>.lp
set timelimit <契約值>
set mip tolerances mipgap <契約值>
set tune repeat <N>
tune
display settings changed
```

**為什麼單一 instance 也值得跑**：調校單一模型時，CPLEX 可**排列（permute）模型後重新 tune** 以取得更穩健的結果（`TuningRepeat`）。model permutation 正是 performance variability 的來源之一，CPLEX 用它人工製造多樣本，正好補上單 instance 缺樣本的洞。`TuningMeasure` 另可選「最差情況最佳化」或「平均最佳化」。

**定位：候選來源與參考基準，NEVER 直接 promote**

| 為什麼不能直接採用 | 處理 |
| --- | --- |
| tune 可能一次改多個參數 → 歸因不了 | 把它建議的每個參數**拆成獨立 variant**，走 §4 逐顆驗證 |
| tune 的內部評分未必等於你的契約與 θ | 用 §4.2 的 lexicographic gate 重新裁決 |
| tune 在自己的執行環境下量測 | 用 §2.3 定版的環境重跑 |

**工具不可用時（無授權 / 不在 PATH）**：**跳過，不中斷流程**，在 `TuningHistory.md` 記一行理由。NEVER 因為這一步失敗就停止 tuning。

## §4 champion 判定

### 4.1 Step 1 · eligibility gate（先淘汰，再排名）

一律淘汰（**三情境共通**）：

- 執行錯誤（`Error` / `NotSolved`）
- `Infeasible` / `Unbounded` —— 同一個凍結模型不該有 trial 變成無解，出現就代表越界，MUST 查明
- 違反 §0.1.1 的**結果不變式**：情境 A 是 objective 偏離基線；情境 B / C 是 objective 比基線差、或 `BestBound` 越過已知最佳 incumbent
- solver log 出現 **dynamic search 停用 warning**（§2.3.1）

**依情境追加的淘汰條件**：

| 情境 | 追加淘汰 | 明確**不**淘汰 |
| --- | --- | --- |
| A（`Optimal`） | 未達專案要求的 objective 或 gap 品質門檻 | — |
| B（`Feasible`） | endGap 比 baseline 差且超出 θ | **`Status = Feasible` 本身不是淘汰理由**——它是本情境的常態 |
| C（`TimeLimit`） | — | **「無可行解」不是淘汰理由**——它是 baseline 的起點。無解的 trial 以主指標的最差值（找到 0 個 incumbent、`t_feas` 記 PAR10）進入排名 |

★ **情境 C 若把「無可行解」當淘汰理由，會把所有 trial 連同 baseline 一起淘汰光**，本輪得不出任何結論——那不是嚴謹，是判準套錯情境。

**每個被淘汰的 candidate 都要寫淘汰理由。**

### 4.2 Step 2 · lexicographic 比較（NEVER 只用單一數字混排）

依序比較，前一項分出勝負就不看後面。**第 3 項的比較對象是 §3.0 定的主指標，不是無條件的 runtime**：

| # | 比較項 | 情境 A | 情境 B | 情境 C |
| --- | --- | --- | --- | --- |
| 1 | 結果不變式（§0.1.1） | objective 等於基線 | objective 不差於基線 + bound 不越線 | 同 B |
| 2 | 專案要求的品質門檻 | objective 與 `MipGap` 均達標 | endGap 是否落進要求區間 | 是否**存在** incumbent |
| 3 | **主指標**（穩健彙總後，§4.3） | runtime `sgm` | **endGap 平均** | **找到解的 seed 數 → `t_feas` `sgm`** |
| 4 | 改善 MUST **大於 θ**（§4.4） | 否則視同平手 | 否則視同平手 | seed 數差 ≤ 1 亦視同平手 |

★ **NEVER 用 `NodeCount` / `IterationCount` 做判定或 tie-break** —— 框架目前不填這兩個欄位（experiment CSV 中為空）。需要 tie-break 時改用 §2.1 的 `Δbound` 與 `t_stall`。

**NEVER 讓一個比較快但解較差的 trial 勝出**；情境 B 同理——**NEVER 讓一個 gap 收得漂亮但 incumbent 反而變差的 trial 勝出**，第 1 項就該把它擋掉。

### 4.3 Step 3 · 主指標怎麼彙總

| 主指標 | 方法 | 為什麼 |
| --- | --- | --- |
| runtime（A） | **shifted geometric mean**（shift 常用 1s 或 10s）；timeout 以 **PAR10** 計入 | 算術平均被少數慢 instance 綁架，快 instance 的改善看不見；NEVER 把 timeout 當成「剛好等於時限」 |
| endGap（B） | **算術平均**；無 incumbent 的 seed 記 100% | gap 已是比值，再取幾何平均會低估差異 |
| 找到解的 seed 數 / `t_feas`（C） | 先比 seed 數（整數，直接比）；平手才比有解 seed 的 `t_feas` `sgm`，無解 seed 以 PAR10 計入 | 剔除無解 seed 等於獎勵「碰運氣」的 variant |

MUST 寫明用了哪個主指標、哪種彙總與計算值。**主指標在同一個 tuning 週期內固定，中途換 = 前面所有輪次的排名作廢**（除非情境本身變了——例如情境 C 首度找到 incumbent 而升級成情境 B，此時 MUST 重跑 R0 重新定 θ，並在 `TuningHistory.md` 記一筆「情境轉換」分隔線）。

### 4.4 Step 4 · 改善要大於 variability

**改善幅度 MUST 大於 θ（§3.0 R0 校準量出來的雜訊地板）。**

差距落在雜訊範圍內 = **視同平手**，結論是「無人勝出、保留 baseline」。

「無人勝出」是**合法結論**，NEVER 為了有結果硬挑本輪牆鐘時間最小的那一個。

### 4.5 職責分離（走 multi-agent 時尤其重要）

**設計 variant 的人不判定 champion。** 設計者對自己的 variant 有偏好，會不自覺放寬 eligibility gate。§8 的拓樸把 T2（設計）與 T4（分析）拆開就是為了這件事。

### 4.6 Step 5 · Hold-out 驗證（promotion 前必經）

champion 用 §3.0 保留的 **3 個未參與調參的 holdout seeds** 重跑。

**協定鐵則：holdout 只能用來估計，NEVER 用來選 config。** 拿 holdout 挑贏家等於偷看測試集，估計值即失去意義。

| 結果 | 動作 |
| --- | --- |
| holdout 上**主指標**改善仍 > θ | 進 §5 promotion |
| holdout 上改善消失或縮到 θ 以內 | **over-tuning** —— 退回 retain，記錄證據，該旋鈕值進「已否證」清單 |
| holdout 上違反 §0.1.1 結果不變式 | **調壞了** —— 直接淘汰，NEVER promotion |

★ 情境 C 的 holdout 判準是「在 3 個 holdout seed 上**至少各找到一個 incumbent**」。只在 tuning seed 上找得到解、holdout 上找不到 = 典型 over-tuning，退回 retain。

多 instance 可用時，同一協定升級成 train / test instance 分離。

---

## §5 promotion 閉環

```text
productionBaseline
  └─ Clone variants
       └─ OptExperiment.Run()
            └─ Trial.Config + Trial.Metrics
                 └─ AI promotion gate（§4）
                      └─ 更新 productionBaseline（§5.1）
                           └─ build + production 重跑 + ValidateRules（§5.3）
```

`OptExperiment` 本身只負責執行與保存證據——**它不會改傳入的 config，也不會替 production 選 winner**。閉環的責任在 AI workflow。

### 5.1 寫回 baseline

1. 從 champion 的 `Trial.Config.SolverSpecific` 取得完整設定快照
2. 把對應值**明確寫成字面值**填進 `Program.cs` 的 `productionBaseline` initializer
3. 更新 initializer 上方 provenance 註解

```csharp
// baseline provenance:
//   來源 experiment: HospitalRostering-tuning-r2
//   champion Trial : Canonical | r2-emphasis=optimal
//   promotion 日期 : 2026-08-08
//   diff           : mipEmphasis null → 2（其餘不變）
var productionBaseline = new CplexConfig
{
    epGap = 0.03,
    timeLimit = 300,
    workThreads = 8,
    parallelMode = 1,
    randomSeed = 42,
    mipEmphasis = 2,
};
```

- **NEVER 讓執行中的程式修改 source**
- **NEVER 讓 prod 在 runtime 動態挑「歷史上最快的一列」**
- **NEVER 同時維護「實驗 baseline」與「production config」兩份設定**

### 5.2 先記錄，再驗證

在跑 production 驗證**之前**，先在專案根 `TuningHistory.md` 新增本輪紀錄（§6），production 驗證結果之後補回同一筆。

Why: 驗證失敗時你需要那筆記錄來說明「試過什麼、為什麼 rejected」，事後補寫容易漏。

### 5.3 production 驗證（promotion 的出口 gate）

1. `dotnet build`
2. `dotnet run`（**無參數，走 production 路徑，NOT exp**）
3. 確認 `Status`、objective、`MipGap` 與預期一致
4. `OnSolved` / `ValidateRules` 全數通過
5. 與 promotion 前的 production 結果對照，**依情境套 §0.1.1 對應那一套**：

   | 情境 | PASS 條件 |
   | --- | --- |
   | A | objective 相同或在可接受品質門檻內 |
   | B | objective 不差於基線、`BestBound` 未越線、endGap 確實改善 |
   | C | 確實產出了 incumbent（`Status` 從 `TimeLimit` 變成 `Feasible` / `Optimal`），且該解通過 `ValidateRules` |

   ★ 情境 C 的 promotion 一旦 PASS，專案**就此升級為情境 B**：下一輪 MUST 重跑 R0 重新定主指標與 θ（§4.3），並在 `TuningHistory.md` 記一筆「情境轉換」分隔線。這不是額外開銷——瓶頸真的換了一種，沿用舊尺會量錯。

| 結果 | 動作 |
| --- | --- |
| **PASS** | 該設定成為下一輪 baseline；把驗證結果補回 `TuningHistory.md` 同一筆 |
| **FAIL / 回退** | **撤銷本次 promotion**（還原 `productionBaseline`），在 `TuningHistory.md` 標記 `rejected` 並寫明失敗現象 |

Why 要驗：tuning 改的是 solver 行為，**不該改變解的正確性**。promotion 後 `ValidateRules` 掛了或目標值變了，代表這顆設定動到了不該動的東西。

### 5.4 status.json

只更新下列欄位，**NEVER 整檔覆寫**掉其他階段的欄位：

```json
{
  "phase": "tuning",
  "tuningRound": 1,
  "productionBaseline": "r1-emphasis=optimal",
  "baselineSourceExperiment": "<Project>-tuning-r1",
  "baselineSourceTrial": "Canonical | r1-emphasis=optimal",
  "promotionVerified": true,
  "updated": "YYYY-MM-DD"
}
```

沒有 promotion 的輪次：`productionBaseline` 保持原值（初次為 `"initial"`），`promotionVerified` 不設或維持前值。

---

## §6 `TuningHistory.md`

專案根，**MUST 納入 source control**。

Why: `bin/Experiments/*.json` 會被 `dotnet clean` 或換 configuration 清掉，不能當唯一 provenance。沒寫進這裡的決策，下一輪就會有人重做同一組實驗。

檔案分三段：**契約區塊**（開頭，整期固定）→ **R0 校準**（一節）→ **各輪決策日誌**（每輪一節，追加）。

### 6.1 契約區塊（檔案開頭，只寫一次）

```markdown
## 契約區塊（整期固定，變更即重跑 §2.3 與 §3.0）

### 停止契約
| 項目 | 值 | 備註 |
| `MipGap` / `TimeLimit` / … | … | 使用者定案日期 |

### 環境契約（§2.3 sizing 定版）
| `Threads` / `ParallelMode` / `MemoryLimitMb` / `NodeFileStrategy` | … | sizing 結果與理由 |

### 量測契約
| 進場情境（§0.0.1） | A `Optimal` / B `Feasible` / C `TimeLimit` |
| 主指標（§3.0 產出 A） | runtime `sgm` / endGap 平均 / 找到解的 seed 數 + `t_feas` |
| θ | 值 + 單位（比值或絕對百分點） |
| export（experiment 期間） | 全關 |
| 解正確性驗證 | ValidateRules 有 / 無 |
| dynamic search | 每輪確認無停用 warning |

### Phase 2 結果基線（§0.1.1 不變式）
phase2Status / phase2Objective / phase2Bound / phase2Gap / verifiedOn
```

**情境轉換**（C → B、或 B → A）發生時：在該處追加一筆「情境轉換」分隔線，寫明轉換的輪次、新主指標與重跑後的新 θ。轉換前後的排名數字**不可跨線比較**。

### 6.2 每輪決策日誌（四段，**預測必須在跑實驗之前寫**）

```markdown
## R<N> — YYYY-MM-DD

**假設**：R0 剖面為 Dual-bound、`Δbound` 僅 0.300 且跨 seed 一致 → bound 是唯一瓶頸，
        加強切割應能推高 bound_final
**預測**：bound_final > 3.60；runtime sgm 改善 > θ            ← 跑之前寫死
**實測**：
  - experiment：`<Project>-tuning-r<N>`
  - seeds：{…}；彙總：sgm(shift 1s)、timeout PAR10

  | Trial | Status | objective | MipGap | sgm(runtime) | Δbound |
  | --- | --- | --- | --- | --- | --- |

**裁決**：promote / retain / rejected + 理由（含改善幅度與 θ 的對照）
**已否證**（累積，後續輪次不重試）：
  | 方向 | 證據 | 否證於 |
```

**四段的意義**：

| 段 | 防什麼 |
| --- | --- |
| 假設 | 逼出「為什麼試這顆」的推理，避免盲搜 |
| **預測（跑之前）** | **防事後合理化** —— 看到結果再解釋，永遠編得出理由；先押注才驗得出推理對錯 |
| 實測 | 數據落檔，不依賴對話記憶 |
| 裁決 + 已否證 | 讓決策可被下一輪繼承 |

### 6.3 規則

- 決策欄只有三種值：`promote` / `retain` / `rejected`
- **即使本輪沒有勝者也要記 `retain` 與證據** —— 不記等於下一輪重做同一組實驗
- **「已否證」清單跨輪累積，NEVER 重置** —— 這是自動連續執行時避免繞圈的機制
- 歷史節 NEVER 改寫，只追加
- 這是**決策索引**；完整逐 Trial 數值仍由 experiment JSON 保存

---

## §7 停損與退場

停止條件 MUST 機械可判 —— 本階段設計為**啟動後連續執行到停止**，中途不向使用者要指示。

### 7.1 停止條件表（每輪結束時依序檢查，命中即停）

| # | 條件 | 判準 | 動作 |
| --- | --- | --- | --- |
| **A** | **早停 · 雜訊主導** | R0 剖面 = Variability-dominated（`max/min > 2` 且無明顯瓶頸） | 不進 R1，直接收尾回報「此規模量不出可靠改善」 |
| **B** | **早停 · 整類否證** | 某輪所有 variant 的 bound 軌跡起訖與 baseline 一致 | 該旋鈕類標記「已否證」，**跳過該類剩餘候選**；若剖面對應的類已全數否證 → 停止 |
| **C** | **候選耗盡** | §2.2 中該剖面的候選已全部試過或否證 | 停止 |
| **D** | **連續 3 輪無實質改善** | 連續 3 輪無任何 variant 的**主指標**改善 > θ（主指標依 §3.0 情境而定，NEVER 一律看 runtime） | 停止。把「可能要改模型結構」當**建議**交還使用者（那是新一輪 Phase 1），NEVER 自行升級去動模型 |
| **E** | **promotion 驗證連續 2 輪 FAIL** | production 重跑不通過 | 停止——代表 promotion 流程本身有問題，不是 config 的問題 |
| **F** | **總預算耗盡** | 累計求解時間超過啟動時設定的上限 | 停止並回報已完成的輪次 |
| **G** | **結果不變式破裂且無法定位** | 多個 variant 同時違反 §0.1.1（情境 A 偏離 `phase2Objective`；情境 B / C `BestBound` 越線） | **立即停止**並回報——這通常代表環境或框架層出了問題，不是旋鈕問題 |
| **H** | §1 判定非 tuning | 進場即判定 | 立即停止，告知退回哪個 phase、為什麼 |
| **I** | **情境 C 候選耗盡仍無 incumbent** | No-incumbent 候選全試過，K 個 seed 仍全部無解 | 停止。結論是「**現行契約下這個規模找不到可行解**」——把「放寬 `TimeLimit` 契約」或「改模型結構」當**建議**交還使用者，兩者都不是本階段能自行決定的 |

**A–G、I 都是正常收尾**，一律走 §5 的交付流程（有 champion 就 promotion，沒有就 retain + 證據），NEVER 半途丟下不寫紀錄。

★ **情境 C 命中 I 時 NEVER 自行加大 `TimeLimit` 交差**——那是動停止契約（§2.0.1），要使用者拍板。「試遍旋鈕仍找不到可行解」本身就是有價值的交付結論。

### 7.2 總預算

啟動時 MUST 估算並記錄總預算，避免 `TimeLimit` 設得寬時失控：

```text
預估總時間 ≈ (sizing 3×3 + R0 5 + 每輪 variants×5) × 單次求解時間
```

單次求解時間以 R0 的 `sgm` 為準。**超過預算就停在當前輪次**（條件 F），已完成的輪次照常交付。

### 7.3 不屬本階段的退場

| 情況 | 說明 |
| --- | --- |
| 手動掃描已無方向 | 可考慮自動調參工具（附錄 C.2），但 **NEVER 一開始就丟幾十個參數給 configurator**；單模型情境優先用 §3.6 的 CPLEX 內建 tune |
| 想改模型結構 | 那是新一輪 Phase 1，由**使用者**決定要不要走 |

---

## §8 multi-agent 執行層

**何時用**（不符合就單線跑完，NEVER 為了派工而派工）：

| 判準 | 單線 | multi-agent |
| --- | --- | --- |
| 單次求解時間 | < 60s | ≥ 60s |
| 預估總輪次 | ≤ 5 | > 5 |
| 使用者要求最高保真度 | — | ✅ |

★ **小模型一律單線。** 單次求解 10 秒的模型，派 8 個 agent 的調度開銷遠大於求解本身，而且會把「一鍵跑完」變成一堆等待。單線執行時，§8.4 各 agent 的職責由同一個執行者依序完成，**產物落檔的要求不變**（`_wip/<Project>/t<N>-*.md` 仍要寫，那是跨 session resume 的依據）。

★ **職責分離仍要維持**：即使單線，§4.5 的「設計 variant 的人不判定 champion」也要靠**先寫預測再看結果**（§6.2 四段日誌）來達成——預測寫死之後才准跑實驗。

**Why 要拆**：tuning 最常見的失敗不是算錯，是**同一個 agent 既設計又評分**，於是「我設計的 variant 贏了」。

### 8.1 Context 鐵則

- NEVER 把 solver log / experiment JSON / Trial 明細**全文**讀進任何 context —— ALWAYS `grep` 抽欄位（Status、objective、MipGap、time、nodes）
  Why: 單次 MIP 的 solver log 可以上萬行，讀一次就把整個視窗吃光，而你要的只有五個數字
- NEVER 把每輪分析結果留在對話裡累積 —— ALWAYS 落檔 `_wip/<Project>/t<N>-*.md`，orchestrator 只持有一張跨輪摘要表
  Why: tuning 是多輪迭代，第 4 輪時前 3 輪的原始數據還留在 context，判斷力已經被稀釋
- MUST 單一 agent 輸入預算 ≤ 800 行；回報上限：執行類 ≤ 15 行、分析類 ≤ 30 行
- MUST orchestrator 只持有：**輪次摘要表、baseline 現值、promotion 狀態**
- MUST 平行 fan-out 一次 ≤ 6 個 agent

### 8.2 拓樸

```text
T0 orchestrator（主對話，不下場調參）
 │
 ├─ T1 scope-guard ─────────► _wip/<Project>/t<N>-triage.md      （§1 判定 + 本輪量化目標）
 │      ├─ 要動資料 / 結構 → 停止 Phase 3，退回對應 phase
 │      └─ infeasible → 先派 T1b 取證再退回
 ├─ T1b iis-analyst ────────► _wip/<Project>/t<N>-iis.md         （§1.1，僅 infeasible 時）
 ├─ T2 variant-designer ────► _wip/<Project>/t<N>-plan.md        （§3，一次一旋鈕）
 ├─ T3 experiment-runner ───► _wip/<Project>/t<N>-trials.md      （執行 + 抽數，不判優劣）
 ├─ T4 analyst ─────────────► _wip/<Project>/t<N>-analysis.md    （§4 判 champion）
 ├─ T5 promotion-judge ─────► _wip/<Project>/t<N>-verdict.md     （second-opinion 裁決）
 ├─ T6 promoter ────────────► Program.cs + TuningHistory.md      （§5.1 + §6）
 └─ T7 promotion-verifier ──► _wip/<Project>/t<N>-prodverify.md  （§5.3）
```

| Agent | subagent_type | model | 職責邊界 |
| --- | --- | --- | --- |
| T1 scope-guard | `general-purpose` | `sonnet` | 只判範圍，不動手 |
| T1b iis-analyst | `general-purpose` | `opus` | 讀 `.ilp` 找最小衝突集（取證用，不修） |
| T2 variant-designer | `general-purpose` | `opus` | 只產 config plan，不執行 |
| T3 experiment-runner | `general-purpose` | `sonnet` | 只執行與抽數，不判優劣 |
| T4 analyst | `general-purpose` | `opus` | 只判優劣，不寫回 code |
| T5 promotion-judge | `second-opinion` | （內建） | 只裁決，不動手 |
| T6 promoter | `general-purpose` | `opus` | 只寫回，不重新判定 |
| T7 promotion-verifier | `verifier` | （內建） | 只驗收，不修東西 |

T5 用 `second-opinion` 的理由：promotion 會改寫 production baseline，是長期沿用且事後難察覺的變更——改壞了要到下次 production 求解才發現。所以由不參與實驗設計與分析的角色裁決。

### 8.3 工作區

```text
<repo 根>/
├── _wip/<Project>/t<N>-*.md   ← 每輪中間產物（N = 輪次）；repo 層，NEVER 放進專案
└── Projects/<Project>/
    ├── TuningHistory.md       ← 永久 provenance（每輪一節）——這份 MUST 在專案根
    ├── Program.cs             ← 唯一 productionBaseline 所在
    └── bin/Experiments/*.json ← 框架產出；NEVER 全文讀，用 grep 抽欄位
```

`_wip/` 與 `TuningHistory.md` 的差別是刻意的：前者是本輪拋棄式草稿，放 repo 層才不會撞到「八資料夾 NEVER 增減」天條；後者是規範明文要求的專案根永久記錄——天條管的是資料夾，專案根放檔案不受限。

### 8.4 派工 prompt（可直接複製，`{{}}` 處替換）

路徑一律**相對 repo 根**，NEVER 用絕對路徑。

#### T0 · orchestrator（主對話）

1. 確認正確性 gate：`status.json` 的 `solveVerified == true`，**並實跑一次確認**；否則停止並要求先完成 Phase 2
   —— 同時讀 `solveStatus` 與 `verifiedOn`，依 §0.0.1 定出**進場情境 A / B / C**，這決定後面每一輪的主指標。情境 C 且 `verifiedOn` 不是 `small-instance:*` → 停止退回 Phase 2
2. 讀本檔 §0 – §1
3. 讀 `Program.cs` 的 `productionBaseline` 現值（**只讀該 initializer 區塊**，不讀全檔）
4. 依序派 T1 → T2 → T3 → T4 → T5；判定 promote 才派 T6 → T7
5. 每輪結束更新輪次摘要表（orchestrator 唯一持有的跨輪視圖）：

| 輪 | 目標 | variants | champion | 決策 | production 驗證 |
| --- | --- | --- | --- | --- | --- |
| r1 | gap 8% → 3% | 3 | r1-emphasis=optimal | promote | PASS |

#### T1 · scope-guard

```text
目標：判定「這件事是不是 tuning」、定出進場情境，並輸出本輪要打的量化目標。
動機：Phase 3 只動 CplexConfig，前提是模型與資料凍結、正確性已驗。前提不成立卻硬調，會燒掉好幾輪實驗才發現方向從一開始就錯。

輸入：
- 使用者的訴求原文：{{貼在這裡}}
- Projects/{{Project}}/status.json 的 solveVerified / solveStatus / verifiedOn
- _wip/{{Project}}/v3-solve.md（Phase 2 的解驗證結果，若有）
- Program.cs 的 productionBaseline 現值（只讀該區塊）
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §0 與 §1（兩節）

先定進場情境（§0.0.1）：
- Optimal → 情境 A，主指標 runtime
- Feasible（撞限制但有 incumbent）→ 情境 B，主指標 endGap。**這是可進場的，NEVER 因為「不是 Optimal」就退回 Phase 2**
- TimeLimit（無任何可用解）→ 情境 C，主指標「找到解的 seed 數 + t_feas」；
  且 MUST 確認 verifiedOn 是 small-instance:*，否則停止退回 Phase 2（模型從未被任何 instance 驗過）
- Infeasible / Unbounded / Error → 不是 tuning，退回對應 phase

再依 §1 的表判定範圍。判不出來 → 明說判不出來並列出你需要的資訊，NEVER 猜一個。

另外輸出本輪的量化目標，**單位要與該情境的主指標一致**：
- A：「runtime sgm 從 480s 降到 300s 以內」
- B：「endGap 從 8% 降到 3% 以內，timeLimit 不變」
- C：「5 個 seed 中至少 3 個找到 incumbent」
目標寫不出數字 → 回報卡住，NEVER 用「更快」這種無法驗收的目標。

輸出：寫入 _wip/{{Project}}/t{{N}}-triage.md

回報格式：判定（是 tuning / 退回哪個 phase）、**進場情境 A/B/C 與主指標**、依據（≤3 行）、
本輪量化目標、建議先動的旋鈕方向（不要給具體值，那是 T2 的事）。總長 ≤15 行。
```

#### T1b · iis-analyst（僅 infeasible 時）

```text
目標：讀 IIS 輸出，找出最小衝突約束集合，判斷是模型錯還是資料錯。
動機：Infeasible 幾乎都是模型或資料的錯，不是 solver 的錯。你產出的是「該退回哪裡、退回去要修什麼」的證據，不是修法本身。

輸入：Projects/{{Project}}/bin/Debug/net8.0/IISs/*.ilp
NEVER 整檔讀——先 grep 出約束名稱清單，再針對命中的名稱回 Constraint/ 找對應 .cs 與 Model.md 條目。
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §1.1（只讀這節）

回報格式：IIS 約束清單（名稱 | Model.md 條目）、根因判定、該退回哪個 phase、
退回後要修什麼（資料 / 結構 / 界限，各附一句後果）。總長 ≤20 行。
NEVER 建議改成 soft constraint。NEVER 自己動手修任何檔案。
```

#### T2 · variant-designer

```text
目標：設計本輪的 config variants 與實驗計畫，寫成可直接貼進 Program.cs 的 code 片段。
動機：一次只改一個旋鈕，才知道是哪個旋鈕起作用。一次改三個然後變快了，你學不到任何可複用的知識。

輸入：_wip/{{Project}}/t{{N}}-triage.md、Program.cs 的 productionBaseline 現值
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §2、§3 與附錄 A（旋鈕名稱與可設值 MUST 查附錄 A，NEVER 憑記憶寫欄位名）

輸出：寫入 _wip/{{Project}}/t{{N}}-plan.md，含：
- variants 表：| label | 改動的旋鈕 | 值 | 預期效果 | 依據（§2 哪一列） |
- 可直接貼用的 C# 片段
- seeds / warm-up / 執行順序輪替的具體安排

過關條件：
1. 每個 variant 相對 baseline 只有一處差異（逐項比對，寫在表上）
2. experiment name 為 {{Project}}-tuning-r{{N}}，與歷史不重名
3. baseline 來自 productionBaseline.Clone()
4. 每個旋鈕名都在附錄 A 查得到且標 ✅

回報格式：variants 表（≤5 列）、experiment name、seeds 設定。總長 ≤15 行。
```

#### T3 · experiment-runner

```text
目標：執行本輪 OptExperiment，把結果抽成一張精簡的 Trial 表。
動機：你只負責跑與抽數，NEVER 判斷誰贏——判優劣是另一個 agent 的事，你先下結論會影響它。

輸入：_wip/{{Project}}/t{{N}}-plan.md
做法：
1. 把 plan 的 C# 片段套進 Program.cs 的 exp 分支（只動 exp 分支，NEVER 動 productionBaseline）
2. dotnet build → dotnet run --project <csproj> -- exp
3. 從 bin/.../Experiments/<name>.json 抽每個 Trial 的：label、Status、objective、MipGap、runtime、nodes、seed
   NEVER 整份 JSON 讀進 context——用 grep / 逐欄抽取
4. solver log 同理：只 grep Status、objective、gap、time 幾行

輸出：寫入 _wip/{{Project}}/t{{N}}-trials.md：
| Trial label | seed | Status | objective | MipGap | runtime(s) | nodes |

過關條件：
1. plan 的每個 variant × seed 都有對應列
2. warm-up 那次已標記排除
3. 未修改 productionBaseline（附 git diff 摘要佐證）

回報格式：Trial 表（≤15 列）、執行總時間、異常（crash / 無解 / 逾時）清單。總長 ≤20 行。
NEVER 下「哪個比較好」的結論。
```

#### T4 · analyst

```text
目標：依評分規則從本輪 Trial 選出 champion，或判定「無人勝出，保留 baseline」。
動機：主指標快幾毫秒 / 好零點幾個百分點不是改善——MIP 的 performance variability 本來就有數個百分點。改善必須大於 θ，否則你 promote 的是雜訊。

輸入：_wip/{{Project}}/t{{N}}-trials.md、_wip/{{Project}}/t{{N}}-triage.md（本輪量化目標 + 進場情境）
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §4 與 §3.0（兩節）

先確認本輪的**進場情境與主指標**（triage 已定），再照 §4 的五個 Step 依序做。
主指標套錯情境是本崗位最常見的失效：
- 情境 B 的每個 trial 都跑滿 timeLimit，拿 runtime 排名會得到「全部平手」——要比 endGap
- 情境 C 的 objective / MipGap 全是 NaN，NEVER 把 NaN 當 0 參與彙總——要比「找到解的 seed 數」再比 t_feas
- 情境 C「無可行解」不是淘汰理由（§4.1），當成淘汰會把 baseline 一起淘汰光

輸出：寫入 _wip/{{Project}}/t{{N}}-analysis.md，含：eligibility 淘汰名單 + 理由、
彙總後比較表、champion（或「無人勝出」）+ 理由、θ 對照

過關條件：
1. 每個被淘汰的 candidate 都寫了淘汰理由
2. 主指標與情境相符，且用了 §4.3 該情境指定的彙總法（寫明方法與計算值），非單次數字
3. 結論明確標示改善幅度與 θ 的關係
4. 「無人勝出」是合法結論，NEVER 為了有結果硬選一個

回報格式：champion（或 retain）、改善幅度、variability 對照、淘汰名單一行。總長 ≤30 行。
```

#### T5 · promotion-judge（second-opinion）

```text
目標：獨立裁決本輪該 promote champion 還是 retain 現有 baseline。
動機：promotion 會改寫 Program.cs 的 production baseline，是長期沿用且事後難察覺的變更。所以由不參與實驗設計與分析的你來裁決。

輸入（只給這三份，不給實驗過程）：
- _wip/{{Project}}/t{{N}}-triage.md（本輪目標）
- _wip/{{Project}}/t{{N}}-trials.md（原始 Trial 數據）
- _wip/{{Project}}/t{{N}}-analysis.md（分析結論）

裁決依據：
1. analysis 的 eligibility gate 有沒有放水（拿 trials 原始數據覆核，不要只看結論）
2. 改善幅度是否確實大於 variability，而非落在雜訊範圍
3. 是否只在單一 instance / 單一 seed 上贏——那不足以 promote
4. 有沒有隱藏代價（runtime 變快但 gap 變差、記憶體用量暴增）
5. 本輪量化目標是否真的達成

輸出：寫入 _wip/{{Project}}/t{{N}}-verdict.md

回報格式：
- 裁決：PROMOTE <champion label> / RETAIN baseline
- 理由（≤5 行）
- 你不同意 analysis 的地方（無則寫「無」）
- 若 PROMOTE：promotion 後要特別盯的風險（≤2 條）
總長 ≤20 行。NEVER 修改任何檔案。
```

#### T6 · promoter

```text
目標：把 champion 的設定寫回 Program.cs 的 productionBaseline，並在 TuningHistory.md 留下永久 provenance。
動機：bin/Experiments/*.json 會被 clean build 清掉。沒寫進 TuningHistory.md 的決策，下一輪就會有人重做同一組實驗。

前置：_wip/{{Project}}/t{{N}}-verdict.md 的裁決必須是 PROMOTE；
RETAIN 則跳過寫回，只做 TuningHistory 記錄。

規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §5.1、§5.2 與 §6（三節）

過關條件：
1. Program.cs 只有一顆具名 productionBaseline，且值與 champion 完全一致
2. initializer 上方 provenance 註解含 experiment + Trial label + 日期 + diff
3. TuningHistory.md 該節所有欄位齊全，before/after diff 逐項可讀
4. git diff 只動了 Program.cs 與 TuningHistory.md

回報格式：before/after config diff（逐項）、TuningHistory 節標題、git diff 檔案清單。總長 ≤15 行。
```

#### T7 · promotion-verifier

```text
目標：驗證 promotion 後的 production 路徑仍然正確，並把結果補回 TuningHistory.md。
動機：tuning 改的是 solver 行為，不該改變解的正確性。如果 promotion 後 ValidateRules 掛了或目標值變了，代表這顆設定動到了不該動的東西。

規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §5.3（只讀這節）

照 §5.3 的五個步驟做，並依該節的表判定 PASS / FAIL。

回報格式：build 結果、Status/objective/gap、ValidateRules 結果、before/after 對照、
最終判定（PASS/FAIL）。總長 ≤20 行。
```

### 8.5 每輪完成條件（T0 執行）

一輪完成 = 下列**全部**成立：

1. T4 有明確結論（champion 或「無人勝出」），且證據落檔
2. T5 裁決完成
3. PROMOTE → T6 寫回 + T7 production 驗證 PASS；RETAIN → `TuningHistory.md` 有 retain 記錄與證據
4. `TuningHistory.md` 該輪節位欄位齊全
5. `status.json` 已更新（§5.4）

---

## §9 常見錯誤與反模式

| 症狀 | 真正原因 | 修法 |
| --- | --- | --- |
| 撞時限的專案被擋在 Phase 2 出不來，說「要 Optimal 才能進 Phase 3」 | 把 `Feasible` 當成 Phase 2 的 FAIL | `Feasible` 是合法交付狀態，也是 Phase 3 情境 B 的典型進場（§0.0.1）。Phase 2 手上沒有任何合法工具能修「太慢」 |
| 情境 B 掃了一輪，所有 variant「runtime 完全一樣、全部平手」 | 主指標套錯——每個 trial 都跑滿 `TimeLimit` | 主指標改 endGap（§3.0 產出 A），θ 用絕對百分點 |
| 情境 C 一開跑就「所有 trial 都被淘汰、包含 baseline」 | 把「無可行解」當 eligibility 淘汰理由 | §4.1：情境 C 下無解不是淘汰，是主指標的最差值 |
| 情境 C 的彙總算出一堆 `NaN` 或詭異的 0 | 把 `NaN` 的 objective / MipGap 當 0 參與平均 | `Status = TimeLimit` 時數值欄全 `NaN`（§2.1 ★）；改比 seed 數與 `t_feas` |
| 調了三輪都「好像有變快」但說不出哪個旋鈕有效 | 一輪改了多個旋鈕 | 回 §3.1，一次一個 |
| 重跑實驗看到上一輪的 Trial | 同名 experiment 是 append | 改用 `<Project>-tuning-r<N>` 遞增命名（§3.3） |
| promote 之後 production 反而變慢 | 只跑一個 seed，贏的是雜訊 | 回 §3.0 量 θ + §4.4 |
| baseline 每次跑的時間都不一樣 | 沒固定 `Seed` / `ParallelMode` | `ParallelMode = 1` + 固定 seed + `DeterministicTimeLimit` |
| `NodeFileStrategy` 設了但沒作用 | `MemoryLimitMb` 會強制 `MIP.Strategy.File = 0` | 設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy`（附錄 A ★） |
| `CS1061` 找不到 `epGap` / `workThreads` / `mipEmphasis` 等欄位 | 用了已廢止的 camelCase 舊名 | 對照附錄 A 改用 PascalCase（附錄 A ★★） |
| 調到 timeout 都還在 gap 5% | 已到旋鈕的極限 | §7 停損，把「改模型結構」當建議交還使用者 |
| 交付後 `git diff` 有 `.csv` / `Constraint_*.cs` | 越界改了凍結範圍的檔 | 全部還原，該訴求依 §1 退回對應 phase |
| `TuningHistory.md` 只有 promote 的輪次 | retain 的輪次沒記 | 補記——不記等於下一輪重做同一組實驗 |

### 反模式

❌ **主動建議 tuning** —— 使用者提出才做
❌ **未過正確性 gate 就調** —— 更快地算出錯答案
❌ **跳過 R0 直接掃 variants** —— 沒有 θ 就沒有判定門檻，任何「改善」都無法與雜訊區分（§3.0）
❌ **把 `MipGap` / `TimeLimit` 當速度旋鈕掃** —— 它們是停止條件，兩個不同契約的 trial 比 runtime 沒有意義（§2.0）
❌ **把 `Seed` 放進 variant 池排名** —— 它是重複量測的自變數，「贏」只證明變異大（§2.0）
❌ **在 threads 未定版時量 θ** —— threads 改變雜訊幅度，等於用會伸縮的尺量東西（§2.0.2）
❌ **憑 `NodeCount` / `IterationCount` 判斷瓶頸** —— 框架不填這兩個欄位，改用軌跡剖面（§2.1）
❌ **`Emphasis = 2` 當成推 bound 的手段** —— 推 bound 是 `3`（BESTBOUND）（§2.2）
❌ **看到 objective 變好就採用** —— 同契約下 objective 應恆等於 Phase 2 基線，變了代表越界（§0.1.1）
❌ **跑完實驗才補寫「預測」** —— 事後永遠編得出理由，預測必須在跑之前寫死（§6.2）
❌ **一開始就塞滿參數** —— Phase 3 分不出是哪個旋鈕造成差異；其餘旋鈕一律留 `null` 用 CPLEX 預設
❌ **用 soft constraint 讓 infeasible「有解」** —— 改語意，屬 Phase 1
❌ **只跑 experiment 就宣稱完成** —— 沒 promotion 等於白跑
❌ **同一個 agent 既設計 variant 又判定 champion** —— 設計者會不自覺放寬 gate
❌ **只留 `bin/Experiments/*.json` 當證據** —— clean build 就沒了
❌ **讀 solver log / experiment JSON 全文** —— 上萬行，要的只有五個數字
❌ **拿單次牆鐘時間排序** —— 用 shifted geometric mean + PAR10
❌ **在 train instance 上的改善數字當交付結論** —— 那是 over-tuning
❌ **改 OptimFoundation 框架本體** —— `dlls/` 唯讀；缺旋鈕見附錄 B

---

## 附錄 A · `CplexConfig` 全旋鈕對照

✅ = 框架已提供且已接線；❌ = 框架尚未提供（要用得改框架本體並重編 Core + Cplex DLL，屬框架維護，不在本流程內，見附錄 B）。

欄位為 `null` 即採用 CPLEX 預設，tuning 時**只設要動的那一個**。

★ **查表前先查 §2.0 的四分類。** 本表只列「這顆旋鈕是什麼」，不代表它可以進 variant 池：

| 類別 | 本表中的成員 | 可否當 variant |
| --- | --- | --- |
| **契約** | `MipGap` `AbsoluteMipGap` `TimeLimit` `DeterministicTimeLimit` `NodeLimit` `IntegerSolutionLimit` `IntegralityTolerance` `OptimalityTol` `FeasibilityTol` | ❌ 整期固定 |
| **環境** | `Threads` `ParallelMode` `MemoryLimitMb` `TreeMemoryLimitMb` `NodeFileStrategy` | ❌ §2.3 sizing 定版後凍結 |
| **器材** | `Seed` `ClockType` | ❌ `Seed` 是重複量測的自變數 |
| **策略** | 其餘全部 | ✅ **唯一的 variant 池** |

| 用途 | CPLEX 參數 | `CplexConfig` 欄位 | 取值 / 預設 |
| --- | --- | --- | --- |
| 執行緒數 | `Param.Threads` | ✅ `Threads` | 整數，預設 32；實測比較核心數 / −1 / −2 |
| 平行模式 | `Param.Parallel` | ✅ `ParallelMode` | −1 機會式 / 0 自動 / 1 決定論 |
| 限制式讀取上限 | `Param.Read.Constraints` | ✅ `RowRead` | 整數，預設 30000 |
| 工作記憶體 | `IntParam.WorkMem` | ✅ `MemoryLimitMb` | MB，預設 2048 |
| 樹記憶體上限 | `Param.MIP.Limits.TreeMemory` | ✅ `TreeMemoryLimitMb` | MB |
| 節點檔策略 | `Param.MIP.Strategy.File` | ✅ `NodeFileStrategy` | 0 不存 / 1 記憶體壓縮（預設） / 2 磁碟 / 3 磁碟壓縮 |
| 節點選擇 | `Param.MIP.Strategy.NodeSelect` | ✅ `NodeSelect` | 0 DFS / 1 best-bound（預設） / 2 best-estimate / 3 交替 |
| 分支變數選擇 | `Param.MIP.Strategy.VariableSelect` | ✅ `VariableSelect` | −1 min-infeas / 0 自動 / 1 max-infeas / 2 pseudo cost / 3 strong branching / 4 pseudo reduced cost |
| 分支方向 | `Param.MIP.Strategy.Branch` | ✅ `BranchDirection` | −1 向下 / 0 自動 / 1 向上 |
| 潛降策略 | `Param.MIP.Strategy.Dive` | ✅ `DiveType` | 0 自動 / 1 傳統 / 2 探測 / 3 引導 |
| 搜尋模式 | `Param.MIP.Strategy.Search` | ✅ `MipSearch` | 0 自動 / 1 傳統 B&C / 2 動態 B&C |
| 探測強度 | `Param.MIP.Strategy.Probe` | ✅ `Probe` | −1..3 |
| RINS 頻率 | `Param.MIP.Strategy.RINSHeur` | ✅ `RinsHeuristicFrequency` | −1 關 / 0 自動 / N |
| 啟發式投入 | `Param.MIP.Strategy.HeuristicEffort` | ✅ `HeuristicEffort` | 倍率 |
| MIP emphasis | `Param.Emphasis.MIP` | ✅ `Emphasis` | 0 平衡 / 1 重可行解 / 2 重最佳性 / 3 best bound / 4 hidden |
| 數值穩定 | `Param.Emphasis.Numerical` | ✅ `NumericalEmphasis` | bool |
| Cut 數量倍數 | `Param.MIP.Limits.CutsFactor` | ✅ `CutsFactor` | 倍率 |
| Cut 回合數 | `Param.MIP.Limits.CutPasses` | ✅ `CutPasses` | −1 / 0 / N |
| Gomory 切割 | `Param.MIP.Cuts.Gomory` | ✅ `GomoryCuts` | −1 關 / 0 自動 / 1..3 漸積極 |
| 覆蓋切割 | `Param.MIP.Cuts.Covers` | ✅ `CoverCuts` | −1 / 0 / 1..3 |
| 團切割 | `Param.MIP.Cuts.Cliques` | ✅ `CliqueCuts` | −1 / 0 / 1..3 |
| MIR 切割 | `Param.MIP.Cuts.MIRCut` | ✅ `MirCuts` | −1 / 0 / 1..3 |
| Flow cover 切割 | `Param.MIP.Cuts.FlowCovers` | ✅ `FlowCoverCuts` | −1 / 0 / 1..3 |
| MIP gap（相對） | `Param.MIP.Tolerances.MIPGap` | ✅ `MipGap` | 預設 1e-4 |
| MIP gap（絕對） | `Param.MIP.Tolerances.AbsMIPGap` | ✅ `AbsoluteMipGap` | 數值 |
| 整數容差 | `Param.MIP.Tolerances.Integrality` | ✅ `IntegralityTolerance` | 數值 |
| 最佳性容差 | `Param.Simplex.Tolerances.Optimality` | ✅ `OptimalityTol` | 預設 1e-6 |
| 可行性容差 | `Param.Simplex.Tolerances.Feasibility` | ✅ `FeasibilityTol` | 預設 1e-6 |
| 時間限制（牆鐘） | `Param.TimeLimit` | ✅ `TimeLimit` | 秒，**MUST 明設，NEVER 留 `null`** |
| 決定論時間 | `Param.DetTimeLimit` | ✅ `DeterministicTimeLimit` | ticks，可重現實驗首選 |
| 計時方式 | `Param.ClockType` | ✅ `ClockType` | 1 CPU / 2 wall |
| 節點上限 | `Param.MIP.Limits.Nodes` | ✅ `NodeLimit` | 整數 |
| 整數解上限 | `Param.MIP.Limits.Solutions` | ✅ `IntegerSolutionLimit` | 找到 N 個整數解即停 |
| Solution polishing | `Param.MIP.PolishAfter.Time` | ✅ `PolishAfterTime` | 秒 |
| 隨機種子 | `Param.RandomSeed` | ✅ `Seed` | 整數 |
| 預處理 | `Param.Preprocessing.Presolve` | ✅ `PreIndicator` / `Presolve` | bool；一般保持開啟，只有 debug 才關 |
| 對稱性消除 | `Param.Preprocessing.Symmetry` | ✅ `Symmetry` | −1 auto / 0 off / 1..5 逐步提高強度；排班、指派這類同質資源的題目值得試 |
| Root 演算法 | `IntParam.RootAlgorithm` | ✅ `RootAlgorithm` | 0 自動 / 1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent |
| 節點 LP 演算法 | `IntParam.NodeAlg` | ✅ `NodeAlgorithm` | 0..6 |
| Simplex 迭代上限 | `Param.Simplex.Limits.Iterations` | ✅ `SimplexIterationLimit` | 整數 |
| Barrier 演算法 | `Param.Barrier.Algorithm` | ✅ `BarrierAlgorithm` | 0..3 |
| ZeroHalf / Disjunctive 切割 | `Param.MIP.Cuts.ZeroHalfCut` / `.Disjunctive` | ❌ | — |
| 進階 presolve | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | ❌ | — |
| 記憶體 emphasis | `Param.Emphasis.MemUsage` | ❌ | — |
| 分支優先級 | `Cplex.SetPriority` / order file | ❌ | 只能間接用 `VariableSelect` 影響 |
| MIP start（初始解注入） | `Cplex.AddMIPStart` / `SetVectors` | ❌ | 等效手段：`Emphasis = 1` + `RinsHeuristicFrequency` + `HeuristicEffort` |
| 自動調參 | `Cplex.TuneParam` | ❌ | — |
| Heuristic / Lazy / UserCut callback | 對應 callback | ❌ | — |

> ★ **`MemoryLimitMb` 與 `NodeFileStrategy` 的順序雷**：框架的 `Configuration()` 在設定 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`。要做「記憶體爆 → 溢寫節點檔」，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy = 2/3`，否則被覆蓋成 0。
>
> ★ **而且 `File = 0` 比 CPLEX 預設更糟**：CPLEX 的 `MIP.Strategy.File` 預設是 `1`（節點壓縮後仍留在記憶體）。框架強制成 `0` 等於**把壓縮策略關掉**——樹超過 `MemoryLimitMb` 時沒有任何緩衝，直接吃記憶體到 OOM。另注意 `MemoryLimitMb` 管的是 **live tree 大小**，不是行程總記憶體；CPLEX 官方建議把它設得明顯低於可用記憶體，好讓壓縮提早啟動。
>
> ★★ **命名已統一為單一套 PascalCase**（框架 2026-08 起）。過去的「抽象旋鈕（`ITunableConfig`）＋ camelCase CPLEX 欄位」雙軌制**已取消**，`epGap` / `workThreads` / `mipEmphasis` / `varSel` / `randomSeed` 這類舊名**不再存在**，寫了直接 compile error。
>
> `CplexConfig` 同時實作 `ISolverConfig` 與 `ITunableConfig`，跨 solver 共通的那組（`Emphasis` / `Seed` / `FeasibilityTol` / `OptimalityTol` / `RootAlgorithm` / `Presolve` / `HeuristicEffort` / `MemoryLimitMb` / `TimeLimit` / `MipGap` / `Threads`）與 CPLEX 專屬欄位現在是**同一套名字**，不再有「兩邊都寫」的風險。
>
> 舊 code 或舊文件出現 camelCase 名 → 視為待遷移，以本表為準。
>
> ★★★ **`ProjectConfig` 不是 tuning 的對象**。它管專案身分與輸出（`ProjectName` / `EnableSolverLog` / `ExportLP` / `DataId`…），`CplexConfig` 才管 solver 怎麼解。實驗快照只擷取 solver 那層，所以輸出開關不會混進 tuning 記錄。

### A.1 建立變數的正確 API（旋鈕表的常見誤用）

調參時若需要改動 `Program.cs`，**變數建立一律 `engine.BuildVars<T>(sets...)`**，型別由類別名前綴決定（`VariableB_` Binary / `VariableC_` Continuous / `VariableI_` Integer）。

- 一般 code 使用 `BuildVars<T>`；`BuildBVs` / `BuildCVs` / `BuildIVs` 仍是有效公開 API，只在自訂 bounds 或維護既有明確型別建構時使用（權威在 Phase 2 guide §3）
- **Phase 3 不該動到變數層**：需要改變數宣告或 bounds = 你在改模型，依 §1 退回 Phase 1 / 2

---

## 附錄 B · 框架尚未提供的接口

動 `CplexConfig` / `OptEngine` 屬**改 OptimFoundation 框架本體** → Core + Cplex DLL 必須一起 rebuild 並依 `dlls/README.md` 回填 `dlls/` 與 `VERSION.txt`。這**不在 Phase 3 範圍內**，列出來是為了在遇到瓶頸時知道「這條路目前走不通、要走得先做框架維護」。

| 缺口 | CPLEX 對應 | 影響哪個手段 | 建議補法 |
| --- | --- | --- | --- |
| MIP start / 初始解注入 | `Cplex.AddMIPStart` / `SetVectors` | warm start | `OptEngine.SetMIPStart(dict)` |
| 分支優先級 | `Cplex.SetPriority` / order file | branching priority | `OptEngine.SetBranchPriority(var, p)` |
| ZeroHalf 切割 | `Param.MIP.Cuts.ZeroHalfCut` | 切割 | 加 `int? zeroHalfCuts` |
| Disjunctive / Implied 切割 | `Param.MIP.Cuts.Disjunctive` / `.Implied` | 切割 | 加對應欄位 |
| 進階 presolve | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | 預處理 | 加對應欄位 |
| 記憶體 emphasis | `Param.Emphasis.MemUsage` | 記憶體 | 加 `bool? memoryEmphasis` |
| 自動調參 | `Cplex.TuneParam` | 附錄 C.2 的 baseline | `OptEngine.AutoTune()` |
| Heuristic callback | `Cplex.HeuristicCallback` | 自訂啟發式 | 暴露 callback 註冊點 ⚠️ |
| Lazy / user cut callback | `LazyConstraintCallback` / `UserCutCallback` | 延遲生成限制式 | 暴露 callback 註冊點 ⚠️ |

框架內部目前只有私有的 `MIPInfoCallback`（收斂軌跡擷取，經 `ITrajectorySource.EnableTrajectory()` 開啟），沒有公開的 heuristic / lazy / start hook。

> ⚠️ **補上這三個 callback 缺口會關閉 dynamic search（§2.3.1）。**
> `MIPInfoCallback` 是 **informational** callback，與 dynamic search 相容，不影響效能、不干擾搜尋空間——所以目前的軌跡擷取是安全的。
> 但 heuristic / lazy constraint / user cut 全都是 **control** callback：只要存在，CPLEX 就關閉 dynamic search、發出 warning、退回 static branch and cut。那是**數量級的效能事件**。
> 因此若日後補上任一個：**所有既有 tuning 數據作廢**，MUST 重跑 §2.3 sizing 與 §3.0 R0，並在 `TuningHistory.md` 記一筆「求解演算法變更」分隔線。

---

## 附錄 C · 研究地圖（reference，非規範）

**這一節是文獻地圖，不是規則。** 何時讀：§2 的手動旋鈕掃完仍不達標、要導入自動調參工具、要寫報告引用文獻時。

### C.1 領域名稱

這件事在學界叫 **Algorithm Configuration (AC)**，不是 "parameter tuning"——搜文獻用 AC 才找得到主線。CPLEX 是 AC 領域的標準實驗對象（159 個 user-specifiable parameters），所以「CPLEX 專屬」的研究比想像中多。

| 線 | 問句 | 產出 | 可用度 |
| --- | --- | --- | --- |
| **A. 手動調參方法論** | 看 log 該動哪個旋鈕？ | 診斷 decision tree | ★★★ 直接可用（§2 就是這條線） |
| **B. 自動調參（offline）** | 給一組 instance，最佳參數組是什麼？ | configurator 工具 | ★★☆ 需 instance set |
| **C. Per-instance 配置** | 給「這個」instance 該用什麼參數？ | ML 模型 | ★☆☆ 需訓練資料 |
| **D. 學習取代 solver 內部決策** | branching / cut 規則能不能學？ | 研究原型 | ☆☆☆ 幾乎都綁 SCIP，CPLEX callback 開放度不足，**讀來理解方向、不是可導入的技術** |

### C.2 可用工具

| 工具 | 方法 | 介面 | 備註 |
| --- | --- | --- | --- |
| **CPLEX 內建 tune** | 內部啟發式 | **Interactive Optimizer 讀 `.lp`（§3.6）** | **零成本、不改程式、MUST 先跑**。框架未封裝 `TuneParam`，但 `ExportLP` 產出的 `.lp` 可直接餵給 Interactive Optimizer。**單模型可用 `TuningRepeat` 做 permutation 穩健化**，正好補上單 instance 缺樣本的洞 |
| **irace** | racing + F-race | R，包 CLI wrapper | 最好上手，統計上有 racing 早停 |
| **SMAC3** | Bayesian optimization | Python，包 CLI wrapper | 目前主流，樣本效率最好 |
| ParamILS | iterated local search | Ruby/Perl 老工具 | 歷史意義為主，新專案別選 |
| GGA / GGA+ | gender-based GA | — | 平行度高時有優勢 |
| MPILS | ILS + 統計剪枝 | 論文原型 | 方法可借鑑，未見公開釋出 |

這些 configurator 都是**黑箱包 solver**：只要能用命令列跑一次求解並吐出時間 / gap 就能接。本框架的 `dotnet run -- exp` 已具備這個介面形狀。

**NEVER 一開始就丟幾十個參數給 configurator**——搜索空間爆炸、樣本不夠、結論全是噪音。從小池子開始、動態擴充。

### C.3 落地順序

| 階段 | 做什麼 | 對應本檔 |
| --- | --- | --- |
| 0 | 過正確性 gate | §0.0 |
| 1 | 手動診斷 | §2 |
| 2 | baseline：default 各 3–5 seed，量出 variability | §3.4 |
| 3 | 手動掃 10–20 個旋鈕的少數 variant，一次一個 | §3 |
| 4 | 仍不達標才上 irace / SMAC3 包 `dotnet run -- exp` | 需新寫 CLI wrapper |
| 5 | hold-out instance 驗收，報 shifted geometric mean | §4.3 |

### C.4 引用清單

**手動方法論（A 線）**

- Klotz & Newman, *Practical guidelines for solving difficult mixed integer linear programs*, Surveys in OR & Mgmt Sci 18(1), 2013 — https://www.sciencedirect.com/science/article/abs/pii/S1876735413000020
  （Ed Klotz 是 CPLEX 開發者，這篇等於官方版調參 SOP；§2.1 的症狀表出自這裡）
- Klotz & Newman, *Practical guidelines for solving difficult linear programs*, 2012 — https://people.mines.edu/anewman/wp-content/uploads/sites/158/2019/11/27-LP_practice123112.pdf
- 同作者的 ill-conditioning / numerical instability 專篇，INFORMS TutORials 2014（係數量級跨度過大時必讀）

**自動調參（B 線）**

- Hutter, Hoos, Leyton-Brown, *Automated Configuration of MIP Solvers*, CPAIOR 2010 — https://ml.informatik.uni-freiburg.de/wp-content/uploads/papers/10-CPAIOR-MIP-Config.pdf
  （用 ParamILS 調 CPLEX 76 個參數，特定 instance family 最高 ~52x 加速，且打贏 CPLEX 內建 tuning tool）
- Himmich et al., *MPILS: An Automatic Tuner for MILP Solvers*, C&OR 2023 — https://www.sciencedirect.com/science/article/abs/pii/S0305054823002083
  （維持小參數池的 tuning → learning → evaluation 三步循環；最貼近「一組固定業務問題」的工業情境）

**Per-instance 配置（C 線）**

- Iommazzo et al., *Learning to Configure Mathematical Programming Solvers by Mathematical Programming* — https://arxiv.org/pdf/2401.05041
- *Instance-wise algorithm configuration with graph neural networks* — https://arxiv.org/pdf/2202.04910
- *Automatic MILP Solver Configuration By Learning Problem Similarities* — https://arxiv.org/pdf/2307.00670
- *The Algorithm Configuration Problem*（形式化 survey） — https://arxiv.org/pdf/2403.00898

**ML for solvers（D 線）**

- *Machine Learning Algorithms for Improving Exact Classical Solvers*（2012–2025 survey） — https://arxiv.org/pdf/2508.06906
- awesome-ml4co 論文清單 — https://github.com/Thinklab-SJTU/awesome-ml4co

**實驗方法論（必讀，§3.4 與 §4.3 的依據）**

- Lodi & Tramontani, *Performance Variability in Mixed-Integer Programming*, INFORMS TutORials 2013 — https://pubsonline.informs.org/doi/abs/10.1287/educ.2013.0112
- Danna, *Performance variability in mixed integer programming*, MIP 2008 — https://coral.ise.lehigh.edu/mip-2008/talks/danna.pdf
- Eggensperger et al., *Pitfalls and Best Practices in Algorithm Configuration* — https://arxiv.org/pdf/1705.06058

> LLM 相關研究（OR-LLM-Agent 等）目前集中在**自然語言 → 模型 / 程式碼生成**，不是 solver 參數配置；別把它誤當成本主題的解法。
