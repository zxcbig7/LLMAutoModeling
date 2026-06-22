# CPLEX 數學模型 Tuning 策略

本文件提供 CPLEX Solver 加速求解的系統性方法，適用於 LP / IP / MILP。
所有「Solver 層」設定都對應 OptimFoundation 的 `CplexConfig` 欄位（單一來源），
由 `OptEngine.Configuration()` 接線到 CPLEX。欄位 `null` = 用 CPLEX 預設，tuning 時只設要動的。

> **黃金順序**：先做「模型優化」（結構、變數型態、邊界、對稱性），再做「參數優化」（solver 旋鈕）。
> 模型改一刀的效益通常遠大於調十個參數。
> **不變式**：tuning 不得移項 / 改號 / 翻轉比較方向 / 四捨五入數值 —— 只改求解策略，不改數學意義。

---

## 1. 模型設計與架構優化（最高優先）

### 1.1 變數型態選擇

原則：**盡可能使用自由度最高（求解最容易）的變數類型**。求解難度 CV ≪ IV < BV。

| 型態    | 鬆弛後                       | 求解難度 | Foundation 建立 API          | 何時用                               |
| ------- | ---------------------------- | -------- | ---------------------------- | ------------------------------------ |
| CV 連續 | 本身即 LP，無分支            | 最低     | `BuildCVs<VariableX_*>(...)` | 量、流量、比例等可分割量             |
| IV 整數 | 需分支定界                   | 中       | `BuildIVs<VariableY_*>(...)` | 數量（台數、批次）等不可分割但範圍大 |
| BV 二元 | 需分支，且常引發對稱與弱鬆弛 | 最高     | `BuildBVs<VariableY_*>(...)` | 是/否決策、選址、開關                |

何時可用連續變數替代離散變數：
- 二元僅用於表示「是否超過某閾值」，且閾值可由連續量 + 約束表達時。
- 可用 Big-M 線性化、且後續對連續解取整不影響可行性時。
- 問題本質為網路流 / 指派且具備 totally unimodular 結構 —— LP 鬆弛即得整數解，直接用 CV。

### 1.2 模型精簡化

目標：用更少的變數與限制式表達相同問題，縮小搜尋空間。
- **聚合限制式**：將多條同型限制式合併（e.g. 逐項上界 → 一條總量上界）。
- **移除冗餘限制式**：刪掉被其他限制式涵蓋（dominated）的式子。
- **緊湊上下界**：給變數設更嚴格的 `lb` / `ub`（`BuildCVs<>(lb, ub, ...)`），直接收斂 LP 鬆弛、減少分支。
- **Big-M 取最小可行值**：M 過大會使 LP 鬆弛鬆散、節點爆增；取問題上界即可。

> 模型精簡屬「modeling 層」，在 Stage 4（AML）重塑，不在 solver 旋鈕處理。

### 1.3 初始解（Warm Start）

效果：提供高品質初始可行解 → 快速建立 incumbent（上/下限）→ 大幅減少 B&B node 數、提早剪枝。

產生初始解的方法：Relax-and-Fix、縮小問題規模求子問題、啟發式（貪婪 / 局部搜尋）、線性鬆弛後修復。

> ⚠️ **Foundation 現況**：尚未提供注入 MIP start 的公開 API（見 §5 需補清單）。
> 在現有介面下，等效手段是讓 CPLEX 自己快速找好 incumbent：
> `mipEmphasis=1`（重可行解）、`rinsHeur`（RINS 啟發式頻率）、`HeuristicEffort`（提高啟發式投入）。
> Relax-and-Fix 等需在 modeling 層自行實作。

### 1.4 對稱性消除

問題存在大量對稱排列時，Solver 會探索大量等價分支。技巧：
- **打斷對稱性**：對等價變數加排序限制式（e.g. `x_1 ≥ x_2 ≥ … ≥ x_n`）。
- **連結 / bridge 限制式**：為等價變數建立順序或 lexicographic 關聯。
- **CPLEX symmetry presolve**：`Param.Preprocessing.Symmetry`（-1 自動…5 積極）—— **Foundation 尚未提供旋鈕**（見 §5）。

---

## 2. CPLEX Solver 參數 Tuning（次優先）

下列每項都對應一個 `CplexConfig` 欄位；括號為 CPLEX 參數與預設行為。

### 2.1 執行緒控制
- `workThreads`（`Param.Threads`，Foundation 預設 32）。實測選最快設定，候選：核心數、核心數-1、核心數-2。
- `parallelMode`（`Param.Parallel`，-1 機會式 / 0 自動 / 1 決定論）。要可重現實驗 → 1。

### 2.2 節點選擇策略
- `nodeSelect`（`Param.MIP.Strategy.NodeSelect`）：0 DFS / 1 best-bound（預設）/ 2 best-estimate / 3 交替 best-estimate。
- 求**最佳解 / 收 gap** → best-bound 或 best-estimate；**急著找可行解** → DFS（0）。

### 2.3 分支策略
- `varSel`（`Param.MIP.Strategy.VariableSelect`）：-1 min-infeas / 0 自動 / 1 max-infeas / 2 pseudo cost / 3 strong branching / 4 pseudo reduced cost。難題收斂慢 → 試 strong branching（3，較貴但分支更準）。
- `branchDir`（`Param.MIP.Strategy.Branch`）：-1 向下 / 0 自動 / 1 向上。
- `diveType`（`Param.MIP.Strategy.Dive`）：0 自動 / 1 傳統 / 2 探測 / 3 引導。
- `mipSearch`（`Param.MIP.Strategy.Search`）：0 自動 / 1 傳統 B&C / 2 動態 B&C。

### 2.4 切割平面（Cuts）
每族取值 -1 關 / 0 自動 / 1..3 漸積極；總量由 `cutsFactor`、`cutPasses` 控制。
- 已接線：`gomoryCuts`（Gomory）、`coverCuts`（Covers）、`cliqueCuts`（團切割 Cliques）、`mirCuts`（MIR）、`flowCoverCuts`（Flow covers）。
- **改善 gap** 時優先加切割（gomory / mir / cover）；切割過多反而拖慢根節點 → 用 `cutsFactor` 收斂。
- **零一半切割（ZeroHalf）、Disjunctive、Implied bound 等 Foundation 尚未提供**（見 §5）。

### 2.5 優先級規則（Branching Priority）
為關鍵整數變數設較高分支優先級，指導 Solver 優先分支。
> ⚠️ **Foundation 尚未提供** priority / order 注入 API（見 §5）。目前只能間接以 `varSel` 影響分支選擇。

### 2.6 Tolerance 與停止條件
- `epGap`（`Param.MIP.Tolerances.MIPGap`，預設 1e-4）相對 gap；`epAGap`（`AbsMIPGap`）絕對 gap。
- `epInt`（`MIP.Tolerances.Integrality`）整數容差；`epOpt` / `epRHS`（Simplex 最佳性 / 可行性容差，預設 1e-6）。
- `timeLimit`（`Param.TimeLimit`，牆鐘秒）/ `detTimeLimit`（`DetTimeLimit`，決定論 ticks，實驗可重現首選）。
- `nodeLimit`（`MIP.Limits.Nodes`）/ `intSolLimit`（`MIP.Limits.Solutions`，找到 N 個整數解即停）。
- `numericalEmphasis`（`Emphasis.Numerical`）數值不穩時開。

### 2.7 預處理（Presolve）
- `PreIndicator` / `Presolve`（`Param.Preprocessing.Presolve`，bool 開關）。一般保持開啟；只有 debug 模型才關。
- 進階 presolve（Aggregator、NumPass、Symmetry、Reduce…）**Foundation 尚未提供**（見 §5）。

### 2.8 記憶體與節點檔
- `workMemory`（`IntParam.WorkMem`，MB，預設 2048）。
- `treeMemoryLimit`（`MIP.Limits.TreeMemory`，MB）+ `nodeFileInd`（`MIP.Strategy.File`，0 不存 / 1 記憶體壓縮（預設）/ 2 磁碟 / 3 磁碟壓縮）。
- ⚠️ **Gotcha**：`Configuration()` 在設定 `workMemory` 時會強制 `MIP.Strategy.File=0`。若要「記憶體爆 → 溢寫節點檔」，需在設 `workMemory` **之後**再設 `nodeFileInd=2/3`，否則被覆蓋為 0。

### 2.9 純 LP（Simplex / Barrier）
- `algorithm`（`RootAlgorithm`）：0 自動 / 1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent。大型 LP 試 barrier（4）。
- `NodeAlgorithm`（`IntParam.NodeAlg`）子問題 LP 演算法。
- `simplexIterLimit`（`Simplex.Limits.Iterations`）/ `barrierAlgorithm`（`Barrier.Algorithm`）。

---

## 3. 進階技術（Foundation 缺口，需擴充 DLL 才能用）

> 以下三項皆需在 `OptEngine` 暴露 callback / start 注入點才能使用；目前內部僅有私有
> `MIPInfoCallback`（軌跡擷取），無公開 heuristic / lazy / start hook。

### 3.1 自動參數優化（CPLEX Tuning Tool）
`Cplex.TuneParam()` 讓 CPLEX 自動搜尋參數組合。Foundation 未封裝 → 需補（見 §5）。

### 3.2 啟發式回調（Heuristic Callbacks）
在 B&B 過程插入自訂啟發式以更快得到 incumbent。需暴露 `HeuristicCallback`。

### 3.3 Lazy Constraints / User Cuts
延遲生成大量潛在限制式（e.g. 子迴路消除）。需暴露 `LazyConstraintCallback` / `UserCutCallback`。

---

## 4. Tuning 流程建議

1. **基準測量**：無 tuning，僅設 `timeLimit`（或 `detTimeLimit` 求可重現），記錄 obj / gap / 時間 / nodes。
2. **模型優化優先於參數優化**（§1 → §2）。
3. **單一變數測試**：一次只改一個旋鈕，與基準比。
4. **疊加有效組合**：保留有改進者，逐步疊加。
5. **記錄實驗**：每次記 obj、gap、wall/det 時間、node 數、變更項。
6. **迭代改進**：有改進就更新基準。

```
基準 → 單一測試 → 篩選有效 → 疊加組合 → 若有改進則更新基準
```

> 可重現性：固定 `randomSeed` + `parallelMode=1`（決定論）+ 用 `detTimeLimit`，否則多執行緒計時不可比。

---

## 4.5 用 ExperimentRunner 實際跑掃描（可執行架構）

§4 的流程已可直接執行——每個專案標配 `ExperimentRunner.cs`：

```
dotnet run -- experiment
```

即掃描多組 `CplexConfig`，用框架的 `Trial.Capture` 記錄「完整設定快照 + 收斂數據」（CPLEX 自動含收斂軌跡），`Experiment.Save()` 落地 `Experiments/<name>.csv + .json`。

- **掃描單位**：`(label, Action<CplexConfig> tune)` 陣列——baseline + 每次只動一個旋鈕（呼應 §4 步驟 3「單一變數測試」）。
- **抽象旋鈕**（跨引擎，`ITunableConfig`）：`config.Emphasis / Seed / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / MemoryLimitMb`。
- **CPLEX 專屬欄位**：直接設 camelCase 欄位 `varSel / nodeSelect / workThreads / gomoryCuts / mirCuts / …`（§6.2 對照表）。
- **每 Trial 用全新 Dataload + engine**，避免狀態跨 Trial 污染；掃描時 `enableLog=false`、關 export 以加速；`timeLimit` 確保每 Trial 收斂。
- **可重現**：固定 `randomSeed` + `parallelMode=1`（決定論）+ 用 `detTimeLimit`（§4 末）。
- **讀回累積**：`Experiment.Load(name)`（同名實驗 append、以 RunAt+Label 去重）。

樣板與完整規範見 `claudemdTemplate/Experiment/CLAUDE.md`。

> 黃金順序不變：先在 Stage 4（AML）做模型優化（§1），再用此 runner 掃 solver 旋鈕（§2）。模型一刀的效益通常遠大於調十個參數。

---

## 5. Foundation 尚未提供、需補的 tuning 接口

> 動 `CplexConfig` / `OptEngine` 屬改 OptimFoundation DLL → **Core + Cplex DLL 必須一起 rebuild 並更新到使用端**。

| 缺口                       | CPLEX 對應                                        | 影響的策略      | 建議補法                              |
| -------------------------- | ------------------------------------------------- | --------------- | ------------------------------------- |
| MIP start / 初始解注入     | `Cplex.AddMIPStart` / `SetVectors`                | §1.3 初始解     | `OptEngine.SetMIPStart(dict)`         |
| 分支優先級                 | `Cplex.SetPriority` / order file                  | §2.5 優先級規則 | `OptEngine.SetBranchPriority(var, p)` |
| Symmetry presolve          | `Param.Preprocessing.Symmetry`                    | §1.4 對稱性     | 加 `int? symmetry` 欄位               |
| ZeroHalf 切割              | `Param.MIP.Cuts.ZeroHalfCut`                      | §2.4 切割       | 加 `int? zeroHalfCuts`                |
| Disjunctive / Implied 切割 | `Param.MIP.Cuts.Disjunctive` / `Implied`          | §2.4 切割       | 加對應欄位                            |
| 進階 presolve              | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | §2.7 預處理     | 加對應欄位                            |
| 記憶體 emphasis            | `Param.Emphasis.MemUsage`                         | §2.8 記憶體     | 加 `bool? memoryEmphasis`             |
| 自動調參                   | `Cplex.TuneParam`                                 | §3.1            | `OptEngine.AutoTune()`                |
| Heuristic callback         | `Cplex.HeuristicCallback`                         | §3.2            | 暴露 callback 註冊點                  |
| Lazy / user cut callback   | `LazyConstraintCallback` / `UserCutCallback`      | §3.3            | 暴露 callback 註冊點                  |

---

## 6. 快速參考表

### 6.1 按目標分類（皆為「正確性 gate 通過後」才做）

| 目標           | 優先 | 建議動作（Foundation 欄位）                                                            |
| -------------- | ---- | -------------------------------------------------------------------------------------- |
| 加速求解       | 1st  | 模型精簡 / 緊湊邊界 / 連續變數替代 / 對稱性消除（modeling 層）                         |
| 加速求解       | 2nd  | 實測篩 `workThreads` → 切割（`gomoryCuts`/`mirCuts`）→ `nodeSelect`                    |
| 改善 GAP       | 1st  | 加切割 → `nodeSelect=1`（best-bound）→ 收 `epGap` / `mipEmphasis=2`                    |
| 找可行解       | 1st  | `nodeSelect=0`（DFS）→ `intSolLimit` / `nodeLimit` → 放寬 `epGap` → `mipEmphasis=1`    |
| timeout 但正確 | —    | 提 `timeLimit` / `mipEmphasis=1` / 開 `parallelMode`；仍 timeout → 回 §1 reformulation |
| 記憶體爆       | —    | `treeMemoryLimit` + `nodeFileInd=2/3`（注意 §2.8 gotcha 順序）                         |
| 數值不穩       | —    | `numericalEmphasis=true`                                                               |
| 可重現實驗     | —    | `parallelMode=1` + 固定 `randomSeed` + `detTimeLimit`                                  |

> 執行緒測試：依前一輪結果選最快設定（核心數 / 核心數-1 / 核心數-2）。

### 6.2 Solver 參數查詢（CPLEX ↔ Foundation 對照）

✅ = Foundation 已提供且已接線；❌ = 需補（見 §5）。

| 參數                | CPLEX                                             | Foundation 欄位               | 可設值 / 預設                |
| ------------------- | ------------------------------------------------- | ----------------------------- | ---------------------------- |
| 執行緒數            | `Param.Threads`                                   | ✅ `workThreads`               | 整數，預設 32                |
| 平行模式            | `Param.Parallel`                                  | ✅ `parallelMode`              | -1 機會 / 0 自動 / 1 決定論  |
| 限制式讀取上限      | `Param.Read.Constraints`                          | ✅ `rowRead`                   | 整數，預設 30000             |
| 工作記憶體 (MB)     | `IntParam.WorkMem`                                | ✅ `workMemory`                | 預設 2048（會強制 File=0）   |
| 樹記憶體上限 (MB)   | `Param.MIP.Limits.TreeMemory`                     | ✅ `treeMemoryLimit`           | MB                           |
| 節點檔策略          | `Param.MIP.Strategy.File`                         | ✅ `nodeFileInd`               | 0 / 1(預設) / 2 / 3          |
| 節點選擇            | `Param.MIP.Strategy.NodeSelect`                   | ✅ `nodeSelect`                | 0 DFS / 1 best-bound / 2 / 3 |
| 分支變數選擇        | `Param.MIP.Strategy.VariableSelect`               | ✅ `varSel`                    | -1 / 0 / 1 / 2 / 3 / 4       |
| 分支方向            | `Param.MIP.Strategy.Branch`                       | ✅ `branchDir`                 | -1 下 / 0 自動 / 1 上        |
| 潛降策略            | `Param.MIP.Strategy.Dive`                         | ✅ `diveType`                  | 0 / 1 / 2 / 3                |
| 搜尋模式            | `Param.MIP.Strategy.Search`                       | ✅ `mipSearch`                 | 0 / 1 / 2                    |
| 探測強度            | `Param.MIP.Strategy.Probe`                        | ✅ `probe`                     | -1..3                        |
| RINS 頻率           | `Param.MIP.Strategy.RINSHeur`                     | ✅ `rinsHeur`                  | -1 關 / 0 自動 / N           |
| 啟發式投入          | `Param.MIP.Strategy.HeuristicEffort`              | ✅ `HeuristicEffort`           | 倍率                         |
| 解析模式 (emphasis) | `Param.Emphasis.MIP`                              | ✅ `mipEmphasis`               | 0..4                         |
| 數值穩定            | `Param.Emphasis.Numerical`                        | ✅ `numericalEmphasis`         | bool                         |
| Cut 數量倍數        | `Param.MIP.Limits.CutsFactor`                     | ✅ `cutsFactor`                | 倍率                         |
| Cut 回合數          | `Param.MIP.Limits.CutPasses`                      | ✅ `cutPasses`                 | -1 / 0 / N                   |
| Gomory 切割         | `Param.MIP.Cuts.Gomory`                           | ✅ `gomoryCuts`                | -1 / 0 / 1..3                |
| 覆蓋切割            | `Param.MIP.Cuts.Covers`                           | ✅ `coverCuts`                 | -1 / 0 / 1..3                |
| 團切割              | `Param.MIP.Cuts.Cliques`                          | ✅ `cliqueCuts`                | -1 / 0 / 1..3                |
| MIR 切割            | `Param.MIP.Cuts.MIRCut`                           | ✅ `mirCuts`                   | -1 / 0 / 1..3                |
| Flow cover 切割     | `Param.MIP.Cuts.FlowCovers`                       | ✅ `flowCoverCuts`             | -1 / 0 / 1..3                |
| 零一半切割          | `Param.MIP.Cuts.ZeroHalfCut`                      | ❌ 需補                        | -1 / 0 / 1..2                |
| Disjunctive 切割    | `Param.MIP.Cuts.Disjunctive`                      | ❌ 需補                        | -1 / 0 / 1..3                |
| MIP Gap (相對)      | `Param.MIP.Tolerances.MIPGap`                     | ✅ `epGap`                     | 預設 1e-4                    |
| MIP Gap (絕對)      | `Param.MIP.Tolerances.AbsMIPGap`                  | ✅ `epAGap`                    | 數值                         |
| 整數容差            | `Param.MIP.Tolerances.Integrality`                | ✅ `epInt`                     | 數值                         |
| 最佳性容差          | `Param.Simplex.Tolerances.Optimality`             | ✅ `epOpt`                     | 預設 1e-6                    |
| 可行性容差          | `Param.Simplex.Tolerances.Feasibility`            | ✅ `epRHS`                     | 預設 1e-6                    |
| 時間限制 (牆鐘)     | `Param.TimeLimit`                                 | ✅ `timeLimit`                 | 秒                           |
| 決定論時間          | `Param.DetTimeLimit`                              | ✅ `detTimeLimit`              | ticks                        |
| 計時方式            | `Param.ClockType`                                 | ✅ `clockType`                 | 1 CPU / 2 wall               |
| 節點上限            | `Param.MIP.Limits.Nodes`                          | ✅ `nodeLimit`                 | 整數                         |
| 整數解上限          | `Param.MIP.Limits.Solutions`                      | ✅ `intSolLimit`               | 整數                         |
| Solution polishing  | `Param.MIP.PolishAfter.Time`                      | ✅ `polishAfterTime`           | 秒                           |
| 隨機種子            | `Param.RandomSeed`                                | ✅ `randomSeed`                | 整數                         |
| 預處理 (presolve)   | `Param.Preprocessing.Presolve`                    | ✅ `PreIndicator` / `Presolve` | bool                         |
| Root 演算法         | `IntParam.RootAlgorithm`                          | ✅ `algorithm`                 | 0..6                         |
| 節點 LP 演算法      | `IntParam.NodeAlg`                                | ✅ `NodeAlgorithm`             | 0..6                         |
| Simplex 迭代上限    | `Param.Simplex.Limits.Iterations`                 | ✅ `simplexIterLimit`          | 整數                         |
| Barrier 演算法      | `Param.Barrier.Algorithm`                         | ✅ `barrierAlgorithm`          | 0..3                         |
| Symmetry presolve   | `Param.Preprocessing.Symmetry`                    | ❌ 需補                        | -1..5                        |
| 進階 presolve       | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | ❌ 需補                        | 整數                         |
| 記憶體 emphasis     | `Param.Emphasis.MemUsage`                         | ❌ 需補                        | bool                         |
| 分支優先級          | `Cplex.SetPriority` / order file                  | ❌ 需補                        | 整數 / 檔                    |
| MIP start           | `Cplex.AddMIPStart` / `SetVectors`                | ❌ 需補                        | 解向量                       |
| 自動調參            | `Cplex.TuneParam`                                 | ❌ 需補                        | API                          |
| Heuristic callback  | `Cplex.HeuristicCallback`                         | ❌ 需補                        | callback                     |
| Lazy / user cut     | `LazyConstraintCallback` / `UserCutCallback`      | ❌ 需補                        | callback                     |
