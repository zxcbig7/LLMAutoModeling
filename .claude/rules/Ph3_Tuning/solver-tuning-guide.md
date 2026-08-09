# Solver Tuning — Phase 3 端到端調校規範

> **這份文件是什麼**：模型與資料已凍結、已 feasible 的專案跑太慢時，怎麼系統性地調 CPLEX solver 旋鈕，從「進場判斷」到「champion 寫回 production baseline 並驗證」的完整標準流程。
> **給誰看**：要替一個既有 OptimFoundation 專案調效能的人，以及要執行這件事的 AI。
> **怎麼用**：從 §0 的進場 gate 開始，**gate 沒過就不要往下讀**。旋鈕名稱與可設值查附錄 A，NEVER 憑記憶寫欄位名。
> **前置**：專案已通過 Phase 2 的解驗證協定四步（`status.json` 的 `solveVerified: true`），且**使用者主動提出**效能問題。
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
2. `Status == Optimal`（或該題目預期的合法狀態）
3. Phase 2 的解驗證協定四步已過：① Status 三分診斷 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity

`status.json` 的 `solveVerified: true` 是**必要條件但不充分**——MUST **實跑一次確認**，不能只信 JSON。

Why: 對錯的模型調參數只會**更快地得到錯答案**，而且會讓那個錯答案看起來更可信（「我們還做了效能調校」）。

**另一個前置**：資料驗證。`OptData.Load` 載入時自動跑四類檢查（index 參照、key 唯一性、數值 sanity、`[FullGrid]` 全格覆蓋），任一違規丟 `DataValidationException`。資料錯了就別調參數——重複的 index key 會被 `FirstOrDefault` 靜默吃掉，模型拿到的是錯的係數。

Gate 順序固定：**資料驗證（載入時自動）→ 解驗證協定四步（Phase 2）→ 才進本檔**。

### 0.1 凍結範圍 — 本階段只動一顆東西

進場時模型與資料已凍結、已 feasible。**本階段唯一可寫的是 `Program.cs` 裡那顆具名 `CplexConfig productionBaseline`**（以及 exp 分支裡由它 `Clone()` 出來的 variants）。

| 檔案 | 本階段 |
| --- | --- |
| `Program.cs` 的 `productionBaseline` | ✅ **唯一寫入點** |
| 專案根 `TuningHistory.md` | ✅ 每輪追加 |
| `Model/<Project>_Model.md` | 🔒 唯讀 |
| `Data/*.csv`、`Data/Dataload.cs` | 🔒 唯讀 |
| `Constraint/*.cs`、`Objective/*.cs` | 🔒 唯讀 |
| `Variable/`、`Set/`、`Parameter/`、`Solution/` | 🔒 唯讀 |
| OptimFoundation 框架本體、`dlls/` | 🔒 唯讀（全流程） |

**交付時 `git diff` MUST 只有 `Program.cs` 與 `TuningHistory.md` 兩個檔。** 多出任何 `.csv` / `Constraint_*.cs` / `Model.md` 的改動，就是本階段越界。

Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，該輪實驗證據**整批作廢**。

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
| timeout / gap 收不下來 / 太慢，**模型正確且有解** | ✅ **是** | 進 §2 |
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

## §2 症狀 → 旋鈕 診斷

先從 solver log 判斷瓶頸在哪，再對應旋鈕。**不是盲搜。**

### 2.1 log 症狀 → 瓶頸判斷

| log 症狀 | 判斷 | 對應方向 |
| --- | --- | --- |
| LP relaxation 與最佳解差距大 | bound 太弱 | 加切割（`gomoryCuts` / `mirCuts` / `coverCuts`） |
| 一直找不到可行解 | primal 側弱 | `mipEmphasis = 1`、`rinsHeur`、`HeuristicEffort` |
| node 數暴衝但 gap 不動 | 搜索無方向 | `varSel`（branching）、`probe` |
| node throughput 低（每秒處理 node 少） | 單 node 太貴 | 關掉部分 cuts、`nodeSelect`、記憶體 / node file |
| 數值警告、解不穩定 | ill-conditioning | `numericalEmphasis = true`（係數量級問題要回 Phase 1） |

### 2.2 目標 → 動作

| 症狀 / 目標 | 動作（依序試，一次一個） |
| --- | --- |
| **timeout** | 提 `timeLimit` → `mipEmphasis = 1`（重可行解）→ 實測篩 `workThreads` |
| **gap 大** | 加切割（`gomoryCuts` / `mirCuts` / `coverCuts`）→ `nodeSelect = 1`（best-bound）→ 收 `epGap` / `mipEmphasis = 2` |
| **可行解難找** | `nodeSelect = 0`（DFS）→ `intSolLimit` / `nodeLimit` → 放寬 `epGap` → `mipEmphasis = 1` |
| **記憶體爆** | `treeMemoryLimit` + `nodeFileInd = 2/3`（**順序有雷**，見附錄 A 的 ★ 註記） |
| **數值不穩** | `numericalEmphasis = true` |
| **要可重現** | `parallelMode = 1` + 固定 `randomSeed` + 用 `detTimeLimit` 取代牆鐘 `timeLimit` |

### 2.3 執行緒怎麼選

`workThreads` 預設 32 通常過大。候選只有三個，**實測選最快**：核心數、核心數−1、核心數−2。NEVER 憑感覺設。

### 2.4 規模預警

`EngineBase.Solve()` 在求解前跑 `PreSolveGuard()`：`TotalVarCount` 超過 `ScaleWarnThreshold`（預設 `10,000,000`）就 `Logging.Warn`，**只警告不中止**。

看到這個警告 → **不是調旋鈕的時機**。模型太大是結構問題（該縮 set / 拆問題），回報使用者並建議退回 Phase 1，NEVER 靠 `workThreads` 硬扛。

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
| **多 seed** | 正式 promotion 用 3–5 seeds | 單 seed 的差異多半是噪音 |
| **warm-up 排除** | 第一個 solve 當 warm-up，**不計入** | cold-start 會固定懲罰第一個跑的 variant |
| **順序輪替** | variants 跨 seed 輪替或隨機化執行順序 | 避免固定讓 baseline 承擔 cold-start |
| **hold-out instances** | train / test instance 分開 | 只在調參用的 instance 上漂亮 = over-tuning |
| **共用一份 data** | 所有 cell 引用同一份 `OptData.Load` 結果，載入後視為唯讀 | 兩份資料會中途漂移，實驗結果就不能回答 production 的問題 |
| **決定論設定** | `parallelMode = 1` + 固定 `randomSeed` + `detTimeLimit` | 否則多執行緒計時不可比 |

**先量 baseline 自己的變異**：default 跑 3–5 個 seed。如果 baseline 自己的 seed 之間就差 2x，那任何小於 2x 的「改善」都不算數。這一步會直接決定你後面判不判得出勝負。

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

## §4 champion 判定

### 4.1 Step 1 · eligibility gate（先淘汰，再排名）

一律淘汰：

- 執行錯誤
- `Infeasible` / `Unbounded`
- 無可行解
- 未達專案要求的 objective 或 gap 品質門檻

**每個被淘汰的 candidate 都要寫淘汰理由。**

### 4.2 Step 2 · lexicographic 比較（NEVER 只用 runtime 混排）

依序比較，前一項分出勝負就不看後面：

1. **正確且可接受的 Status / 解品質**
2. **達成專案要求的 objective 與 MipGap**
3. 前兩者相同或都達標時，才比較**穩健彙總後的 runtime**

`NodeCount` / `IterationCount` **只作診斷與 tie-break**，NEVER 當主要排名依據。

**NEVER 讓一個比較快但解較差的 trial 勝出。**

### 4.3 Step 3 · runtime 怎麼彙總

| 指標 | 方法 | 為什麼 |
| --- | --- | --- |
| runtime | **shifted geometric mean**（shift 常用 1s 或 10s） | 算術平均被少數慢 instance 綁架，快 instance 的改善看不見 |
| timeout | **PAR10**（罰 10 倍時限） | NEVER 把 timeout 當成「剛好等於時限」 |

MUST 寫明用了哪種彙總與計算值。

### 4.4 Step 4 · 改善要大於 variability

**改善幅度 MUST 大於 baseline 自身的變異（§3.4 量出來的那個數）。**

差距落在雜訊範圍內 = **視同平手**，結論是「無人勝出、保留 baseline」。

「無人勝出」是**合法結論**，NEVER 為了有結果硬挑本輪牆鐘時間最小的那一個。

### 4.5 職責分離（走 multi-agent 時尤其重要）

**設計 variant 的人不判定 champion。** 設計者對自己的 variant 有偏好，會不自覺放寬 eligibility gate。§8 的拓樸把 T2（設計）與 T4（分析）拆開就是為了這件事。

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
5. 與 promotion 前的 production 結果對照：objective 應相同或在可接受品質門檻內

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

**每輪一節，固定欄位**：

```markdown
## Round 2 — 2026-08-08

- **experiment**：`HospitalRostering-tuning-r2`
- **baseline Trial**：`Canonical | r2-baseline`
- **champion Trial**：`Canonical | r2-emphasis=optimal`
- **instances / seeds**：canonical instance × seeds {42, 43, 44, 45, 46}
- **彙總方法**：runtime = shifted geometric mean (shift 10s)；timeout = PAR10
- **結果**：

  | Trial | Status | objective | MipGap | runtime (sgm) |
  | --- | --- | --- | --- | --- |
  | r2-baseline | Optimal | 128400 | 2.8% | 271s |
  | r2-emphasis=optimal | Optimal | 128400 | 1.1% | 198s |

- **baseline variability**：baseline 跨 seed 的 runtime 範圍 254–289s（±6%）
- **config diff**：`mipEmphasis` null → 2
- **決策**：**promote** —— gap 從 2.8% 收到 1.1% 且 objective 相同，runtime 改善 27% 遠大於 ±6% 的 variability
- **production 驗證**：`dotnet build` PASS；無參數 production `Status=Optimal`、objective 128400（與 promotion 前相同）、`ValidateRules` PASS
```

- 決策欄只有三種值：`promote` / `retain` / `rejected`
- **即使本輪沒有勝者也要記 `retain` 與證據**
- 這是**決策索引**；完整逐 Trial 數值仍由 experiment JSON 保存

---

## §7 停損與退場

| 條件 | 動作 |
| --- | --- |
| **連續 3 輪無實質改善** | 停止 tuning 並回報。把「可能要改模型結構」當**建議**交還使用者（那是新一輪 Phase 1），NEVER 自行升級去動模型 |
| §1 判定非 tuning | 立即停止，告知退回哪個 phase、為什麼 |
| production 驗證連續 2 輪 FAIL | 停止——代表 promotion 流程本身有問題，不是 config 的問題 |
| 手動掃描已無方向 | 可考慮自動調參工具（附錄 C.2），但 **NEVER 一開始就丟幾十個參數給 configurator** |
| 問題規模小到秒解 | 不要 tuning——變異數比訊號大 |
| 只有單一 instance（不是一個 family） | 自動調參沒有統計基礎，只走 §2 手動診斷 + Experiment 記錄 |

---

## §8 multi-agent 執行層

**何時用**：多輪迭代、需要嚴謹的 champion 判定、或使用者要求最高保真度。單輪小掃描單線做完即可。

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
2. 讀本檔 §0 – §1
3. 讀 `Program.cs` 的 `productionBaseline` 現值（**只讀該 initializer 區塊**，不讀全檔）
4. 依序派 T1 → T2 → T3 → T4 → T5；判定 promote 才派 T6 → T7
5. 每輪結束更新輪次摘要表（orchestrator 唯一持有的跨輪視圖）：

| 輪 | 目標 | variants | champion | 決策 | production 驗證 |
| --- | --- | --- | --- | --- | --- |
| r1 | gap 8% → 3% | 3 | r1-emphasis=optimal | promote | PASS |

#### T1 · scope-guard

```text
目標：判定「這件事是不是 tuning」，並輸出本輪要打的量化目標。
動機：Phase 3 只動 CplexConfig，前提是模型與資料凍結且已 feasible。前提不成立卻硬調，會燒掉好幾輪實驗才發現方向從一開始就錯。

輸入：
- 使用者的訴求原文：{{貼在這裡}}
- _wip/{{Project}}/v3-solve.md（Phase 2 的解驗證結果，若有）
- Program.cs 的 productionBaseline 現值（只讀該區塊）
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §0 與 §1（兩節）

判定依 §1 的表。判不出來 → 明說判不出來並列出你需要的資訊，NEVER 猜一個。

另外輸出本輪的量化目標（例：「gap 從 8% 降到 3% 以內，時間不超過 600s」）。
目標寫不出數字 → 回報卡住，NEVER 用「更快」這種無法驗收的目標。

輸出：寫入 _wip/{{Project}}/t{{N}}-triage.md

回報格式：判定（是 tuning / 退回哪個 phase）、依據（≤3 行）、本輪量化目標、
建議先動的旋鈕方向（不要給具體值，那是 T2 的事）。總長 ≤15 行。
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
動機：runtime 快幾毫秒不是改善——MIP 的 performance variability 本來就有數個百分點。改善必須大於 baseline 自身的變異，否則你 promote 的是雜訊。

輸入：_wip/{{Project}}/t{{N}}-trials.md、_wip/{{Project}}/t{{N}}-triage.md（本輪量化目標）
規範：讀 .claude/rules/Ph3_Tuning/solver-tuning-guide.md 的 §4 與 §3.4（兩節）

照 §4 的四個 Step 依序做。

輸出：寫入 _wip/{{Project}}/t{{N}}-analysis.md，含：eligibility 淘汰名單 + 理由、
彙總後比較表、champion（或「無人勝出」）+ 理由、baseline variability 估計

過關條件：
1. 每個被淘汰的 candidate 都寫了淘汰理由
2. runtime 用了穩健彙總（寫明方法與計算值），非單次時間
3. 結論明確標示改善幅度與 baseline variability 的關係
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
| 調了三輪都「好像有變快」但說不出哪個旋鈕有效 | 一輪改了多個旋鈕 | 回 §3.1，一次一個 |
| 重跑實驗看到上一輪的 Trial | 同名 experiment 是 append | 改用 `<Project>-tuning-r<N>` 遞增命名（§3.3） |
| promote 之後 production 反而變慢 | 只跑一個 seed，贏的是雜訊 | 回 §3.4 + §4.4 |
| baseline 每次跑的時間都不一樣 | 沒固定 `randomSeed` / `parallelMode` | `parallelMode = 1` + 固定 seed + `detTimeLimit` |
| `nodeFileInd` 設了但沒作用 | `workMemory` 會強制 `MIP.Strategy.File = 0` | 設 `workMemory` **之後**再設 `nodeFileInd`（附錄 A ★） |
| 同一設定寫了兩次，值還不一樣 | 抽象旋鈕與 camelCase 欄位指向同一項（`Seed` 與 `randomSeed`） | 只寫一邊（附錄 A ★★） |
| 調到 timeout 都還在 gap 5% | 已到旋鈕的極限 | §7 停損，把「改模型結構」當建議交還使用者 |
| 交付後 `git diff` 有 `.csv` / `Constraint_*.cs` | 越界改了凍結範圍的檔 | 全部還原，該訴求依 §1 退回對應 phase |
| `TuningHistory.md` 只有 promote 的輪次 | retain 的輪次沒記 | 補記——不記等於下一輪重做同一組實驗 |

### 反模式

❌ **主動建議 tuning** —— 使用者提出才做
❌ **未過正確性 gate 就調** —— 更快地算出錯答案
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

| 用途 | CPLEX 參數 | `CplexConfig` 欄位 | 取值 / 預設 |
| --- | --- | --- | --- |
| 執行緒數 | `Param.Threads` | ✅ `workThreads`（= `Threads`） | 整數，預設 32；實測比較核心數 / −1 / −2 |
| 平行模式 | `Param.Parallel` | ✅ `parallelMode` | −1 機會式 / 0 自動 / 1 決定論 |
| 限制式讀取上限 | `Param.Read.Constraints` | ✅ `rowRead` | 整數，預設 30000 |
| 工作記憶體 | `IntParam.WorkMem` | ✅ `workMemory`（= `MemoryLimitMb`） | MB，預設 2048 |
| 樹記憶體上限 | `Param.MIP.Limits.TreeMemory` | ✅ `treeMemoryLimit` | MB |
| 節點檔策略 | `Param.MIP.Strategy.File` | ✅ `nodeFileInd` | 0 不存 / 1 記憶體壓縮（預設） / 2 磁碟 / 3 磁碟壓縮 |
| 節點選擇 | `Param.MIP.Strategy.NodeSelect` | ✅ `nodeSelect` | 0 DFS / 1 best-bound（預設） / 2 best-estimate / 3 交替 |
| 分支變數選擇 | `Param.MIP.Strategy.VariableSelect` | ✅ `varSel` | −1 min-infeas / 0 自動 / 1 max-infeas / 2 pseudo cost / 3 strong branching / 4 pseudo reduced cost |
| 分支方向 | `Param.MIP.Strategy.Branch` | ✅ `branchDir` | −1 向下 / 0 自動 / 1 向上 |
| 潛降策略 | `Param.MIP.Strategy.Dive` | ✅ `diveType` | 0 自動 / 1 傳統 / 2 探測 / 3 引導 |
| 搜尋模式 | `Param.MIP.Strategy.Search` | ✅ `mipSearch` | 0 自動 / 1 傳統 B&C / 2 動態 B&C |
| 探測強度 | `Param.MIP.Strategy.Probe` | ✅ `probe` | −1..3 |
| RINS 頻率 | `Param.MIP.Strategy.RINSHeur` | ✅ `rinsHeur` | −1 關 / 0 自動 / N |
| 啟發式投入 | `Param.MIP.Strategy.HeuristicEffort` | ✅ `HeuristicEffort` | 倍率 |
| MIP emphasis | `Param.Emphasis.MIP` | ✅ `mipEmphasis`（= `Emphasis`） | 0 平衡 / 1 重可行解 / 2 重最佳性 / 3 best bound / 4 hidden |
| 數值穩定 | `Param.Emphasis.Numerical` | ✅ `numericalEmphasis` | bool |
| Cut 數量倍數 | `Param.MIP.Limits.CutsFactor` | ✅ `cutsFactor` | 倍率 |
| Cut 回合數 | `Param.MIP.Limits.CutPasses` | ✅ `cutPasses` | −1 / 0 / N |
| Gomory 切割 | `Param.MIP.Cuts.Gomory` | ✅ `gomoryCuts` | −1 關 / 0 自動 / 1..3 漸積極 |
| 覆蓋切割 | `Param.MIP.Cuts.Covers` | ✅ `coverCuts` | −1 / 0 / 1..3 |
| 團切割 | `Param.MIP.Cuts.Cliques` | ✅ `cliqueCuts` | −1 / 0 / 1..3 |
| MIR 切割 | `Param.MIP.Cuts.MIRCut` | ✅ `mirCuts` | −1 / 0 / 1..3 |
| Flow cover 切割 | `Param.MIP.Cuts.FlowCovers` | ✅ `flowCoverCuts` | −1 / 0 / 1..3 |
| MIP gap（相對） | `Param.MIP.Tolerances.MIPGap` | ✅ `epGap`（= `MipGap`） | 預設 1e-4 |
| MIP gap（絕對） | `Param.MIP.Tolerances.AbsMIPGap` | ✅ `epAGap` | 數值 |
| 整數容差 | `Param.MIP.Tolerances.Integrality` | ✅ `epInt` | 數值 |
| 最佳性容差 | `Param.Simplex.Tolerances.Optimality` | ✅ `epOpt`（= `OptimalityTol`） | 預設 1e-6 |
| 可行性容差 | `Param.Simplex.Tolerances.Feasibility` | ✅ `epRHS`（= `FeasibilityTol`） | 預設 1e-6 |
| 時間限制（牆鐘） | `Param.TimeLimit` | ✅ `timeLimit`（= `TimeLimit`） | 秒，**MUST 明設，NEVER 留 `null`** |
| 決定論時間 | `Param.DetTimeLimit` | ✅ `detTimeLimit` | ticks，可重現實驗首選 |
| 計時方式 | `Param.ClockType` | ✅ `clockType` | 1 CPU / 2 wall |
| 節點上限 | `Param.MIP.Limits.Nodes` | ✅ `nodeLimit` | 整數 |
| 整數解上限 | `Param.MIP.Limits.Solutions` | ✅ `intSolLimit` | 找到 N 個整數解即停 |
| Solution polishing | `Param.MIP.PolishAfter.Time` | ✅ `polishAfterTime` | 秒 |
| 隨機種子 | `Param.RandomSeed` | ✅ `randomSeed`（= `Seed`） | 整數 |
| 預處理 | `Param.Preprocessing.Presolve` | ✅ `PreIndicator` / `Presolve` | bool；一般保持開啟，只有 debug 才關 |
| 對稱性消除 | `Param.Preprocessing.Symmetry` | ✅ `symmetry` | −1 auto / 0 off / 1..5 逐步提高強度；排班、指派這類同質資源的題目值得試 |
| Root 演算法 | `IntParam.RootAlgorithm` | ✅ `algorithm`（= `RootAlgorithm`） | 0 自動 / 1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent |
| 節點 LP 演算法 | `IntParam.NodeAlg` | ✅ `NodeAlgorithm` | 0..6 |
| Simplex 迭代上限 | `Param.Simplex.Limits.Iterations` | ✅ `simplexIterLimit` | 整數 |
| Barrier 演算法 | `Param.Barrier.Algorithm` | ✅ `barrierAlgorithm` | 0..3 |
| ZeroHalf / Disjunctive 切割 | `Param.MIP.Cuts.ZeroHalfCut` / `.Disjunctive` | ❌ | — |
| 進階 presolve | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | ❌ | — |
| 記憶體 emphasis | `Param.Emphasis.MemUsage` | ❌ | — |
| 分支優先級 | `Cplex.SetPriority` / order file | ❌ | 只能間接用 `varSel` 影響 |
| MIP start（初始解注入） | `Cplex.AddMIPStart` / `SetVectors` | ❌ | 等效手段：`mipEmphasis = 1` + `rinsHeur` + `HeuristicEffort` |
| 自動調參 | `Cplex.TuneParam` | ❌ | — |
| Heuristic / Lazy / UserCut callback | 對應 callback | ❌ | — |

> ★ **`workMemory` 與 `nodeFileInd` 的順序雷**：框架的 `Configuration()` 在設定 `workMemory` 時會強制 `MIP.Strategy.File = 0`。要做「記憶體爆 → 溢寫節點檔」，MUST 在設 `workMemory` **之後**再設 `nodeFileInd = 2/3`，否則被覆蓋成 0。
>
> ★★ **抽象旋鈕 vs camelCase 欄位**：`config.Seed = 7` 等同 `config.randomSeed = 7`。抽象旋鈕（`ITunableConfig`：`Emphasis` / `Seed` / `FeasibilityTol` / `OptimalityTol` / `RootAlgorithm` / `Presolve` / `HeuristicEffort` / `MemoryLimitMb` / `TimeLimit` / `MipGap` / `Threads`）寫的 tuning code 可跨 solver；camelCase 欄位是 CPLEX 專屬。**同一設定 NEVER 兩邊都寫。**
>
> ★★★ **`ProjectConfig` 不是 tuning 的對象**。它管專案身分與輸出（`ProjectName` / `EnableSolverLog` / `ExportLP` / `DataId`…），`CplexConfig` 才管 solver 怎麼解。實驗快照只擷取 solver 那層，所以輸出開關不會混進 tuning 記錄。

### A.1 建立變數的正確 API（旋鈕表的常見誤用）

調參時若需要改動 `Program.cs`，**變數建立一律 `engine.BuildVars<T>(sets...)`**，型別由類別名前綴決定（`VariableB_` Binary / `VariableX_` Continuous / `VariableI_` Integer）。

- ❌ `BuildBVs` / `BuildCVs` / `BuildIVs` 已禁用
- ❌ `BuildCVs<T>(lb, ub, sets)` 設界限已禁用——**界限一律寫成獨立 `Constraint_*`**
- 但實務上：Phase 3 **不該動到變數層**。需要改變數宣告 = 你在改模型，依 §1 退回 Phase 1 / 2

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
| Heuristic callback | `Cplex.HeuristicCallback` | 自訂啟發式 | 暴露 callback 註冊點 |
| Lazy / user cut callback | `LazyConstraintCallback` / `UserCutCallback` | 延遲生成限制式 | 暴露 callback 註冊點 |

框架內部目前只有私有的 `MIPInfoCallback`（收斂軌跡擷取，經 `ITrajectorySource.EnableTrajectory()` 開啟），沒有公開的 heuristic / lazy / start hook。

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
| CPLEX 內建 tune | 內部啟發式 | CPLEX 原生 | 零成本，**MUST 當 baseline 先跑**；學界普遍打得過它（框架目前未封裝，見附錄 B） |
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
