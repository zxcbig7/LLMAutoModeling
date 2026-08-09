# Tuning SOP 設計稿（待審，尚未生效）

> 狀態：**設計稿**。不是規範。審過後才決定要不要併進 `.claude/rules/Ph3_Tuning/solver-tuning-guide.md`。
> 前提：不改任何 `.cs`、不改 OptimFoundation 框架、不要求既有專案先遷移成八資料夾結構。全部在文件與流程層。
> 文獻依據集中在 §7，本文各處以 `[E5]`、`[LT]` 這類標記回指。

## §0 這份 SOP 要解決什麼

現行流程的三個實際失效點（由 `HospitalRostering_Generator` 2026-08-02 那輪實測暴露）：

| 失效 | 現象 | 根因 |
| --- | --- | --- |
| 把雜訊當改善 | `seed=20260622` 快 45%，會被選為 champion | 沒有先量變異就開始比較 `[LT]` |
| 混淆契約與策略 | `MipGap=0.01` 與 `Emphasis=2` 在同一輪比 runtime | 停止條件被當成效能旋鈕 |
| 診斷不可執行 | 規範要求看 `NodeCount` 判斷瓶頸，但框架不填該欄位 | 診斷依據與實際可得資料脫節 |

核心轉變：**從「症狀 → 旋鈕查表」的開環，改成「量化剖面 → 假設 → 預測 → 證偽」的閉環**。

## §1 四層旋鈕分類（整份 SOP 的地基）

任何旋鈕動手前先歸類。歸錯類，後面所有比較都無效。

| 類別 | 成員 | 實驗中的角色 |
| --- | --- | --- |
| **契約旋鈕**<br>定義「什麼叫解出來了」 | `MipGap` `AbsoluteMipGap` `TimeLimit` `DeterministicTimeLimit` `NodeLimit` `IntegerSolutionLimit`；容差 `IntegralityTolerance` `OptimalityTol` `FeasibilityTol` | 整個 tuning 週期**固定共用**。變更 = 週期重啟（§2.3） |
| **環境旋鈕**<br>改變執行環境與量測基準 | `Threads` `ParallelMode` `MemoryLimitMb` `TreeMemoryLimitMb` `NodeFileStrategy` | **R0 之前先單獨定死**（§2.4 sizing），之後整期凍結。NEVER 與策略旋鈕同輪比較 |
| **策略旋鈕**<br>改求解路徑，終點不變 | `Emphasis` `VariableSelect` `NodeSelect` `BranchDirection` `DiveType` `MipSearch` `Probe` `RinsHeuristicFrequency` `HeuristicEffort` 各 cuts `CutsFactor` `CutPasses` `RootAlgorithm` `NodeAlgorithm` `Symmetry` `Presolve` `NumericalEmphasis` | **唯一的 variant 池** |
| **量測器材**<br>不是 candidate | `Seed` `ClockType` | `ClockType` 整期固定；`Seed` 是**自變數**，NEVER 當 champion `[E5]` |

判準一句話：**這顆旋鈕改了之後，兩次求解還算不算在做同一件事？** 不算 → 契約類。**改了之後 θ 還算不算同一把尺？** 不算 → 環境類。

### 1.1 為什麼 `Threads` 是環境層而不是策略層

它同時改變三件與量測有關的東西 `[GRB]`：

1. **記憶體預算**——平行 B&B 每個 worker thread 需要一份完整 model 複本加多個大型資料結構，記憶體隨 threads 成長 → 間接改變 `NodeFileStrategy` / `TreeMemoryLimitMb` 的作用點
2. **同步成本結構**——`ParallelMode` 決定論模式的同步代價隨 threads 上升
3. **θ 本身**——threads 越高，執行緒時序造成的路徑分歧越大 `[CPX]`，雜訊地板跟著漂移

第 3 點是關鍵：θ 是本 SOP 所有判定的門檻。threads 浮動時量 θ，等於用會伸縮的尺量東西。

### 1.2 threads 與其他旋鈕的已知交互（CPLEX 機制）

| 組合 | CPLEX 機制 | 後果 |
| --- | --- | --- |
| threads × `MemoryLimitMb`（WorkMem） | WorkMem 管的是 **live tree 的大小**，不是總記憶體。tree 超過它就換儲存策略 `[CPX-MEM]` | threads 多 → 樹長得快 → 更早觸發切換 |
| threads × `NodeFileStrategy`（MIP.Strategy.File） | **框架的雷**：`Configuration()` 設 `MemoryLimitMb` 時強制 `File = 0`。而 CPLEX 預設是 `1`（壓縮後留在記憶體） | 框架把壓縮策略**關掉了**，比預設更糟；threads 越高越早 OOM |
| threads × `TreeMemoryLimitMb`（TreLim） | TreLim 管總樹大小，防止工作儲存塞爆磁碟 | threads 高時需同步調高，否則提前中止 |
| threads × `ParallelMode` | `1`（決定論）同步點多；opportunistic 同步少、通常較快但**路徑與計時不可重現** `[CPX-PAR]` | 決定論代價隨 threads 上升 |
| threads × `RootAlgorithm = 6`（concurrent） | concurrent optimizer 讓多個 LP 演算法各佔一 thread | root 吃掉 threads，B&B 平行度下降 |
| threads × **首解幾乎即最佳的模型** | root/ramp-up 佔比高，樹未長大即解完 | 純同步開銷、零收益 |
| threads × cuts × heuristics | cut pass 同時牽動 heuristic 與 probing 的平行配置 | 三方耦合，OFAT 掃不出來 |

`HospitalRostering_Generator` 落在倒數第二格：incumbent 在 0.66s 到位、其後全程證 bound，實測 `threads=10`（10.16s）與 `threads=4`（10.26s）差 1%。threads 對它**沒有作用空間**，留在 variant 池只是浪費預算並抬高 θ。

### 1.3 CPLEX 專屬：dynamic search 必須全程保持啟用

CPLEX 的 **dynamic search** 是預設的求解演算法。它有一個會被靜默關閉的條件 `[CPX-CB]`：

> **只要應用程式中存在 control callback，CPLEX 就會關閉 dynamic search、發出 warning，改用 static branch and cut。**

退回 traditional B&C 是**數量級的效能事件**，不是微調。含意：

- **informational callback（`MIPInfoCallback`）與 dynamic search 相容**，不影響效能、不干擾搜尋空間 → 框架的 `ITrajectorySource.EnableTrajectory()` 走的是這一類，**量測本身安全**，§3.2 的軌跡剖面可放心使用
- 但框架附錄 B 列的三個缺口（Heuristic / Lazy constraint / User cut callback）**一旦補上就是 control callback** → 補上的那天，dynamic search 被關閉，**所有歷史 tuning 數據作廢，必須重跑 S1–S2**
- 量測契約（§2.2）因此多一條：**每次 R0 都確認 solver log 沒有 dynamic search 被停用的 warning**

這是 CPLEX 獨有的機制，其他 solver 沒有等價物，不可從別家文件類推。

## §2 層 0 · 契約凍結（進場前，一次性）

三份契約寫進 `TuningHistory.md` 開頭，整期不變。

### 2.1 停止契約

`MipGap` / `TimeLimit` 等的**確切值**。所有 variant 共用。

### 2.2 量測契約

| 項目 | 設定 | 依據 |
| --- | --- | --- |
| `ParallelMode` | `1`（決定論） | 非決定論下多執行緒計時不可比 |
| export 開關 | experiment 期間 LP / MPS / Sol **全關** | `[E3]` 檔案 I/O 會污染計時，實測可達 100–1000x 開銷 |
| 計時 | 固定 `ClockType`，或改用 `DeterministicTimeLimit` | 牆鐘受機器負載影響 |
| 解正確性 | **每個 trial 的解都要能過 `ValidateRules`** | `[E1]` 不可預設 solver 在任何參數組合下都給對的解 |
| 資源上限 | `TimeLimit` 必設，NEVER `null` | `[E2]` 未終止的 run 會拖垮整批實驗 |
| **dynamic search** | 每輪確認 solver log **無 dynamic search 停用 warning** | `[CPX-CB]` control callback 會關掉它，退回 static B&C 是數量級事件（§1.3） |

`ParallelMode` 明設 `1` 而不是留 `null`：CPLEX 預設值的實際行為隨版本演進（舊版 `Threads > 1` 即轉 opportunistic；現代版本預設 auto 會盡量維持決定論）`[CPX-PAR]`。依賴預設等於讓可重現性隨 DLL 版本浮動。

`[E1]` 這條在 `OptExperiment` 沒有 `OnSolved` 的限制下，做法是：champion 進 promotion 時必過 production `ValidateRules`；掃描期間至少確認 `Status` 與 `ObjectiveValue` 在同契約下一致，出現偏移即視為異常 trial 並淘汰。

### 2.3 契約變更協定

tuning 過程中發現契約可能訂錯（例：`MipGap=0.03` 導致 production 常態交付次佳解）→

1. **回報，不自行變更**——這是品質決策，使用者拍板
2. 契約一改 → **歷史 runtime 全部作廢**（終點線移了），重跑 R0
3. `TuningHistory.md` 記一筆「契約變更」分隔線

## §2.4 環境定版（sizing，R0 之前）

環境旋鈕改變 θ（§1.1），所以必須**先定死再量 θ**。這一步只做一次，不進入輪次計數。

### 做法

固定 `ParallelMode = 1`（決定論，換取可比性），只掃 `Threads`，候選三個：**實體核心數 / −1 / −2**。NEVER 用邏輯核心數 `[GRB]`。

每個候選跑 3 個 seed，比 `sgm(runtime)`。

### 判定

| 觀察 | 結論 |
| --- | --- |
| 三個候選差距 < 10% | threads **對本模型無作用空間**，取最低者（省資源、壓低 θ），凍結 |
| 有明顯勝者 | 取之，凍結 |
| 記憶體警告或 node file 溢寫 | 降 threads 或先處理 `MemoryLimitMb` / `NodeFileStrategy` 的設定順序雷（§1.2），再重跑 sizing |

### 為什麼取最低者是合理預設

threads 高會抬高 θ（執行緒時序分歧），θ 抬高就更難偵測策略旋鈕的真實改善。**在效能相當時，選低 threads 等於買到更靈敏的量測**。生產環境要不要用更高 threads 是 promotion 後的獨立決定，不影響 tuning 期間的可比性。

### 定版後寫進契約區塊

`Threads` / `ParallelMode` / `MemoryLimitMb` / `NodeFileStrategy` 的確切值進 `TuningHistory.md` 開頭，與停止契約並列。之後任一項變動 → **重跑 sizing 與 R0**。

## §3 層 1 · R0 校準輪（不調任何東西）

**R0 沒跑完不准進 R1。** 它產出的三個常數是後續每一輪的判定依據。

variant 池：只有 baseline 一個 config，跑 `K` 個 seed。`K = 5`（文獻慣例 `[MIPLIB]`）；另留 3 個 **holdout seeds** 全程不參與調參 `[E5]`。

### 3.1 產出 A · 雜訊地板 θ

對 K 個 seed 的 runtime 計算：

- **shifted geometric mean**（`sgm`），shift 取與典型求解時間同量級 `[ACH]`
  - 秒級題目 → shift = 1s；分鐘級 → shift = 10s
- `max / min` 比值
- 逾時者以 **PAR10** 計入（罰 10 倍 `TimeLimit`）

**θ = 後續所有輪次的勝出門檻**。改善幅度 ≤ θ 一律視同平手。

> 保守取法：θ = `(max − min) / sgm`。實測 `HospitalRostering_Generator` 的兩個樣本已顯示 θ ≥ 45%。

### 3.2 產出 B · 瓶頸剖面

從 `Experiments/<name>-trajectory.csv`（框架已產出，欄位 `TimeMs, Objective, Bound, Gap`）計算四個量：

| 量 | 定義 |
| --- | --- |
| `t_feas` | 首個可行 incumbent 出現時間 |
| `r_primal` | `t_feas / t_total` — primal 側佔全程比例 |
| `Δbound` | `bound_final − bound_root` — dual 側淨移動 |
| `t_stall` | bound 最後一次改善的時點 |

分類 → 決定 R1 的候選旋鈕類。**不查全表盲掃**：

| 剖面 | 判準 | 瓶頸 | R1 候選 |
| --- | --- | --- | --- |
| **Dual-bound** | `r_primal` 小、`Δbound` 小、`t_stall` 早 | 證明 bound | `Emphasis = 3`（**BESTBOUND**）、`GomoryCuts` `MirCuts` `CoverCuts` `CliqueCuts` `FlowCoverCuts` `CutsFactor` `CutPasses` `Probe` `Symmetry` |
| **Primal-search** | `r_primal` 大 | 找可行解 | `Emphasis = 1`（FEASIBILITY）或 `4`（HIDDENFEAS）、`RinsHeuristicFrequency` `HeuristicEffort` `NodeSelect = 0` `DiveType` |
| **Node-cost** | 兩者皆不明顯、軌跡點稀疏而總時長 | 單 node 太貴 | 減 cuts、`RootAlgorithm` `NodeAlgorithm` `Presolve`（threads 已在 §2.4 定版，不在此掃） |
| **Variability-dominated** | `max/min > 2` 且無明顯瓶頸 | 雜訊主導 | **停止**，回報「此規模量不出可靠改善」 |

#### `Emphasis` 五個值的官方語意 `[CPX-EMP]`——選錯等於掃錯方向

| 值 | 名稱 | 行為 | 適用剖面 |
| --- | --- | --- | --- |
| 0 | BALANCED（預設） | 平衡「快速證明最佳」與「早期找到高品質可行解」 | — |
| 1 | FEASIBILITY | 更頻繁產出可行解，**犧牲證明最佳的速度** | Primal-search |
| 2 | OPTIMALITY | 較少心力找早期可行解 | 兩者之間，**不是 bound 專用** |
| 3 | **BESTBOUND** | **更大力度推動 best bound，找可行解變成幾乎附帶** | **Dual-bound** |
| 4 | HIDDENFEAS | 全力找「很難找到」的高品質可行解；FEASIBILITY 找不到好解時才用 | Primal-search 的後備 |

★ 現行規範 §2.2 的「gap 大 → `Emphasis = 2`」指向錯誤——**推 bound 是 `3`，不是 `2`**。`HospitalRostering_Generator` 那輪試的正是 2，這是它在 Dual-bound 瓶頸下沒有效果的原因之一。

判讀陷阱：**trajectory 末點 ≠ 最終解**。它是 callback 觀測快照，最終值以 `<name>.csv` 的 `ObjectiveValue` 為準（實測 `Emphasis=optimal` 兩者為 3.8 vs 3.7）。

### 3.3 產出 C · 契約健檢探針

跑一個 `MipGap = 0` 的**探針**（不是 variant，不進排名），得到：

- 真最佳目標值
- 現行契約下的品質損失 = `(obj_contract − obj_true) / obj_true`
- 取得真最佳所需的額外時間

這個數字回報使用者，走 §2.3。

### 3.4 R0 出口 gate

θ 已算出 + 剖面分類明確 + 契約已確認 → 進 R1。分類為 Variability-dominated → 不進 R1。

## §3.5 CPLEX 內建 tuning tool 基準（零成本，不改程式）

CPLEX 自帶 tuning tool，**所有 API 與 Interactive Optimizer 都有** `[CPX-TUNE]`。框架未封裝 `TuneParam`（附錄 B 缺口），但這條路**完全繞得過去**：

`ProjectConfig.ExportLP = true` 已經在 `bin/.../Models/` 產出 `.lp` 檔 → 拿它到 CPLEX Interactive Optimizer 跑 `tune`，**一行程式都不用改**。

```text
read <ProjectName>_LP_<timestamp>.lp
set timelimit <契約值>
set mip tolerances mipgap <契約值>
set tune repeat <N>
tune
display settings changed
```

### 為什麼這一步對單一 instance 特別重要

我原本的判斷是「單一 instance 沒有統計基礎，不上自動調參」。CPLEX 的 tuning tool **有一個為此設計的機制**：

> 調校單一模型時，可以要求 CPLEX **排列（permute）模型後重新 tune**，以取得更穩健的結果（`TuningRepeat`）`[CPX-TUNE]`

model permutation 正是 `[LT]` 所講的 variability 來源之一。CPLEX 用它人工製造多樣本——這正好補上單 instance 缺樣本的洞。`TuningMeasure` 另可選「最差情況最佳化」或「平均最佳化」。

### 定位：候選來源與參考基準，NEVER 直接 promote

| 為什麼不能直接採用 | 處理 |
| --- | --- |
| tune 可能一次改多個參數 → 歸因不了 | 把它建議的每個參數拆成獨立 variant，走 §4 逐顆驗證 |
| tune 的內部評分未必等於你的契約與 θ | 用 §4.3 的 lexicographic gate 重新裁決 |
| tune 在自己的執行環境下量測 | 用 §2.4 定版的環境重跑 |

Hutter 2010 `[HUT]` 顯示 configurator 能打贏 CPLEX 內建 tune——但那是**有 instance set** 的前提。單模型情境下內建 tune 反而是最合身的工具，且成本近乎為零。

**跑不跑都要記錄**：不跑要寫理由（例：無 CPLEX Interactive Optimizer 授權），否則下一輪會有人重問一次。

## §4 層 2 · 策略輪 R1..RN

### 4.1 每輪的固定形狀

- 候選**只從 R0 剖面對應的那一類**取
- **一輪一顆旋鈕**，多顆比較 = 多個 variant
- 每個 variant 跑**同一組 K 個 tuning seeds**——seed 是共同因子，不是 variant `[E5]`
- experiment 名 `<Project>-tuning-r<N>`，N 遞增（同名是 append 不是覆寫）

### 4.2 Racing 早停

`[IRACE]` 的精神，小規模簡化版（樣本太少不做 Friedman 檢定）：

某 variant 在**前 3 個 seed 上都劣於 baseline 且差距 > θ** → 當場淘汰，不跑完剩餘 seed。省下的預算給下一顆旋鈕。

### 4.3 裁決（lexicographic，前項分勝負就不看後項）

1. **解正確 + Status 合法**——異常一律淘汰並寫理由
2. **objective 落在契約門檻內**——同契約下應相同；不同即為異常，查數值問題，NEVER 直接當「解更好」
3. **`sgm(runtime)` 改善 > θ**

`NodeCount` / `IterationCount` 在本框架為空，**不列入判準**（現行規範的 §2.1 此處要修）。

### 4.4 早停 · 整類否證

**某輪所有 variant 的 bound 軌跡起訖與 baseline 一致 → 該類旋鈕對 dual 側無效，整類標記「已否證」，不必湊滿三輪。**

實測依據：`HospitalRostering_Generator` 七個 trial 的 bound 全部 `3.300 → 3.600`，第 1 輪即可結案。

## §5 層 3 · Hold-out 驗證（promotion 前必經）

champion 用**未參與調參的 3 個 holdout seeds** 重跑。

**協定鐵則 `[E6]`：holdout 只能用來估計，NEVER 用來選 config。** 拿 holdout 挑贏家等於偷看測試集，估計值即失去意義。

| 結果 | 動作 |
| --- | --- |
| holdout 上改善仍 > θ | 進 promotion |
| holdout 上改善消失 | **over-tuning**，退回 retain，記錄證據 |

多 instance 可用時，同一協定升級成 train / test instance 分離 `[E6]`。

## §6 層 4 · Promotion 與停損

沿用現行 §5 閉環，補兩條：

- **在 production 設定與機器上驗證** `[E7]`——R0 期間關掉的 export 要開回來，確認開啟後時間仍可接受
- promotion 後 production 的 `ValidateRules` 必過，objective 與 promotion 前一致

### 停損三條

| 條件 | 動作 |
| --- | --- |
| R0 判為 Variability-dominated | 不進 R1，回報 |
| 某類旋鈕整類已否證（§4.4） | 跳過該類，不重試 |
| 連續 3 輪無 > θ 的改善 | 停止，建議回 Phase 1 改模型結構（那是使用者的決定） |

### 決策日誌（每輪，取代現行只記結果的格式）

四段，**預測必須在跑之前寫**——事後才寫的預測沒有證偽能力：

```markdown
## R<N> — YYYY-MM-DD

**假設**：（R0 剖面為 Dual-bound，故推測加強 Gomory cuts 能提升 bound）
**預測**：（bound_final 由 3.60 上升；runtime sgm 改善 > θ=45%）  ← 跑之前寫
**實測**：（表格：variant | seeds | Status | obj | sgm | Δbound）
**裁決**：promote / retain / rejected + 理由
**已否證**：（累積清單，供後續輪次不重試）
```

## §6.5 OFAT 的限制與交互作用（誠實聲明）

本 SOP 的策略輪是 **OFAT（one-factor-at-a-time）**：一輪一顆旋鈕。它的隱含假設是**主效應可加、交互作用可忽略**——而這個假設在 MIP solver 上並不完全成立（§1.2 的三方耦合就是反例）。

DoE 文獻對 OFAT 的批評是明確的：真實系統有多顆旋鈕且效能取決於多因子交互，資料珍貴時**一次改多個因子往往更有效率**。

本 SOP 仍選 OFAT，理由與代價說清楚：

| 面向 | OFAT | 代價 / 補償 |
| --- | --- | --- |
| 可歸因 | 每個改善都指得出是哪顆旋鈕造成 | — |
| 決策可稽核 | 符合本 repo「每一步能反向對回上一步」的不變式 | — |
| 交互作用 | **抓不到** | 用 §2.4 分層固定把交互最強的環境層先剝離 |
| 最優性 | 只保證找到座標方向的局部最優 | 接受；本階段目標是「顯著且可解釋的改善」，不是全域最優 |

三個補償手段，依成本排序：

1. **分層固定**（本 SOP 已採用）——把 threads 這類改變執行環境的旋鈕先定版，消掉最大的一組交互
2. **針對性 2×2 factorial**——只對**已知強耦合**的兩顆做四格全組合（例：`CutsFactor` × `HeuristicEffort`、`RootAlgorithm` × `Threads`）。四個 cell × K seeds 仍在可負擔範圍，且能直接讀出交互項
3. **model-based configurator（SMAC）**——隨機森林代理模型天然捕捉交互，這是自動 AC 相對 OFAT 的**真正優勢**（不只是省時間）

第 3 點與 §7 末「不採用自動 AC」的結論存在張力，處理方式是：**交互作用是採用 SMAC 的正當理由，但單一 instance 仍使它缺乏統計基礎**。累積出 instance family 後，這個張力自然解除——屆時應優先考慮 SMAC 而非繼續加深 OFAT 輪次。

## §7 文獻依據

| 標記 | 出處 | 本 SOP 用它做什麼 |
| --- | --- | --- |
| `[LT]` | Lodi & Tramontani, *Performance Variability in Mixed-Integer Programming*, INFORMS TutORials, 2013 | seed / 變數順序 / 平台這類「看似效能中性」的改動造成劇烈時間差異 → §3.1 先量 θ 再談改善 |
| `[E1]`–`[E7]` | Eggensperger, Lindauer & Hutter, *Pitfalls and Best Practices in Algorithm Configuration*, JAIR 2019（arXiv:1705.06058）七個 pitfall | E1 不信任 target algorithm → 驗解；E2 正確終止 → `TimeLimit` 必設；E3 檔案 I/O 污染計時 → 關 export；E5 over-tune 到 seed → seed 當共同因子 + holdout；E6 over-tune 到 training instance → holdout 只估不選；E7 over-tune 到機器 → production 機器驗證 |
| `[ACH]` | Achterberg（經 MIPLIB 2017 引用）；MIPLIB 2017 用 shift = 1s，Achterberg 2007 用 100s（時間）/ 10（節點） | §3.1 shift 取與典型求解時間同量級 |
| `[MIPLIB]` | MIPLIB 2017 / SCIP 實務：5 seeds `{0,1,2,3,4}`；部分研究用 10 seeds | §3 的 `K = 5` |
| `[IRACE]` | López-Ibáñez et al., *The irace package: Iterated racing*, ORP 2016；F-race 用 Friedman rank test，預設首次淘汰前 5 個 instance | §4.2 早停淘汰的精神來源（小規模簡化，不做檢定） |
| `[HUT]` | Hutter, Hoos & Leyton-Brown, *Automated Configuration of MIP Solvers*, CPAIOR 2010 | 自動 AC 的適用條件；見下方「不採用」說明 |
| `[KN]` | Klotz & Newman, *Practical guidelines for solving difficult MILPs*, Surveys in OR & Mgmt Sci 18(1), 2013 | 診斷導向而非盲搜；模型層手段才有數量級效果 → §6 停損指向 Phase 1 |
| `[CPX-CB]` | IBM CPLEX — *Control Callbacks and Dynamic Search*；`MIPInfoCallback` 類別文件 | **control callback 存在即關閉 dynamic search**、發 warning、退回 static B&C；informational callback（`MIPInfoCallback`）與 dynamic search 相容且不影響效能 → §1.3、§2.2 |
| `[CPX-MEM]` | IBM CPLEX — WorkMem / MIP.Strategy.File / TreLim；*Running Out of Memory* | WorkMem 管 live tree 大小（非總記憶體）；超過即依 `MIP.Strategy.File` 換儲存策略，預設 `1` 為記憶體壓縮；建議 WorkMem 明顯低於可用記憶體以提早壓縮 → §1.2、§2.4 |
| `[CPX-PAR]` | IBM CPLEX — Threads 參數、Parallel mode switch、Determinism of Results | opportunistic 同步較少通常較快但路徑與計時不可重現；預設行為隨版本演進 → §1.2、§2.2 明設 `ParallelMode = 1` |
| `[CPX-EMP]` | IBM CPLEX — MIP emphasis switch（`MIPEmphasis`） | 0 BALANCED／1 FEASIBILITY／2 OPTIMALITY／3 BESTBOUND／4 HIDDENFEAS 的官方語意 → §3.2 剖面對應；修正現行規範指向 `2` 的錯誤 |
| `[CPX-TUNE]` | IBM CPLEX — Tuning Tool（Invoking the Tuning Tool、Interactive Optimizer `tune` 指令、`TuningRepeat` / `TuningMeasure`） | 所有 API 與 Interactive Optimizer 皆可用；**單模型可用 permutation 重複 tune 取得穩健結果** → §3.5 |
| `[GRB]` | Gurobi Optimizer Reference — Parameter Guidelines；Managing Threads and Memory Usage | 僅用於**跨 solver 共通的經驗法則**：threads 不應超過虛擬核心、超執行緒可能傷效能、軟上限 32、**首解幾乎即最佳時 more threads often not better** → §2.4。CPLEX 專屬機制一律以上列 `[CPX-*]` 為準 |
| `[OFAT]` | One-factor-at-a-time method（DoE 通論）；Morris method（elementary effects screening） | §6.5 OFAT 的限制與補償手段 |

### 明確不採用：irace / SMAC 自動調參

`[HUT]` 的 52x 加速來自 76 個參數 × 大 instance set × 數十 CPU 小時。適用條件是**同一 family 的多個 instance**。

本專案目前是**單一 instance、秒級求解**——自動 configurator 沒有統計基礎，只會把雜訊擬合進參數。等到累積出 instance family（同題型不同月份 / 不同院區的班表）再考慮，屆時 `dotnet run -- exp` 已具備 configurator 需要的 CLI 介面形狀。

## §8 對現行規範的具體修改點

| 檔案 | 改什麼 |
| --- | --- |
| `Ph3_Tuning/solver-tuning-guide.md` §2.1 | 症狀表改為 §3.2 的軌跡剖面表；刪除依賴 `NodeCount` 的兩列 |
| 同上 §2.2 | 「gap 大 → 收 `MipGap`」移出——`MipGap` 是契約不是旋鈕 |
| 同上 新增 §3.0 | R0 校準輪 |
| 同上 §3.4 | 「先量 baseline 變異」升格為硬 gate，並定義 θ 的算法 |
| 同上 §4 | 加 holdout 驗證層；判準移除 node/iteration |
| 同上 §6 | `TuningHistory.md` 模板改成四段決策日誌 + 契約區塊 |
| 同上 §7 | 加早停 A（Variability-dominated）與早停 B（整類否證） |
| 同上 §2.2 | **「gap 大 → `Emphasis = 2`」改為 `3`（BESTBOUND）** —— 現行指引把推 bound 指向錯誤的值（§3.2） |
| 同上 附錄 A | 旋鈕表加一欄「類別」（契約 / 環境 / 策略 / 器材）；`Threads` 現行歸在策略側，改歸環境層 |
| 同上 附錄 A ★註記 | `MemoryLimitMb` 強制 `File = 0` 的雷，補上「CPLEX 預設是 `1`（記憶體壓縮），框架這個行為比預設更糟」 |
| 同上 附錄 B | 三個 callback 缺口補上「**補上即為 control callback → 關閉 dynamic search → 歷史 tuning 數據全部作廢**」的警告（§1.3） |
| 同上 附錄 C.2 | CPLEX 內建 tune 由「框架未封裝故不可用」改為 **§3.5 的 `.lp` + Interactive Optimizer 路徑**（零成本、不改架構、單模型可用 permutation 穩健化） |
| 同上 §2.3 | 「執行緒怎麼選」由「實測選最快」改為 §2.4 的 sizing 流程（含平手時取最低者的理由） |
| `skills/tuning/checklist.md` | 加「環境已定版並寫進契約區塊」「R0 已完成且 θ 已記錄」「契約與環境旋鈕未進 variant 池」「champion 已過 holdout」四條 |
