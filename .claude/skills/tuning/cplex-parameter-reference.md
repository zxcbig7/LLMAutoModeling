# CPLEX 旋鈕查表 — `CplexConfig` 全 182 顆

> **這份文件是什麼**：`CplexConfig` 每一顆旋鈕的官方參數路徑、C# 型別、值域與預設，依
> [`solver-tuning-guide.md`](solver-tuning-guide.md) §2.0 的五分類分組。
>
> **怎麼用**：**查表用，不必通讀。** 要寫欄位名或設值時 grep 這一份，
> NEVER 憑記憶寫欄位名。規範本體（什麼時候能調、怎麼判勝負）在 `solver-tuning-guide.md`，
> 本檔不含任何流程規則。
>
> **來源**：本機官方文件
> `%CPLEX_STUDIO_DIR2211%\doc\html\en-US\CPLEX\Parameters\topics\<ParamName>.html`，
> 值域為 IBM 原文。每顆的完整說明（含 Interactive Optimizer 指令名）在 `CplexConfig` 的
> XML 註解裡，IntelliSense 直接看得到。
>
> **能不能調**：分組標題已標 ✅／❌。只有「搜尋策略」那五組共 109 顆可進 variant 池，
> 其餘 73 顆一律凍結——理由見 guide §2.0。

---

**CPLEX 22.1.1 的 .NET API 全部 182 顆參數都已接線**（2026-08）。過去這裡的 ❌「框架尚未提供」欄位已不存在，
附錄 B 只剩三個真正還沒開的接口。欄位為 `null` 即採用 CPLEX 預設，tuning 時**只設要動的那一個**。

**權威來源是 `CplexConfig` 自己的 XML 註解** —— 每顆 property 都標了官方參數路徑、Interactive Optimizer 指令名、
官方值域與預設、以及它屬於哪一類。本附錄只是常用旋鈕的速查，查不到的直接看 `CplexConfig.cs` 或本機官方文件
`%CPLEX_STUDIO_DIR2211%\doc\html\en-US\CPLEX\Parameters	opics\<ParamName>.html`。

★ **查表前先查 §2.0 的分類。** 本表只列「這顆旋鈕是什麼」，不代表它可以進 variant 池：

| 類別 | 顆數 | 成員 | 可否當 variant |
| --- | --- | --- | --- |
| **停止條件** | 27 | `AbsoluteMipGap` `BarrierConvergeTol` `BarrierIterationLimit` `BarrierQcpConvergeTol` `DeterministicTimeLimit` `FeasOptRelaxTolerance` `FeasibilityTol` `IntegerSolutionLimit` `IntegralityTolerance` `LinearizationTolerance` `LowerCutoff` `LowerObjectiveStop` `MipGap` `NetworkFeasibilityTol` `NetworkIterationLimit` `NetworkOptimalityTol` `NodeLimit` `ObjectiveDifference` `OptimalityTol` `RelativeObjectiveDifference` `SiftingIterationLimit` `SimplexIterationLimit` `SimplexLowerObjectiveLimit` `SimplexUpperObjectiveLimit` `TimeLimit` `UpperCutoff` `UpperObjectiveStop` | ❌ 整期固定 |
| **執行資源** | 9 | `AuxiliaryRootThreads` `CpuMask` `MemoryEmphasis` `MemoryLimitMb` `NodeFileStrategy` `ParallelMode` `Threads` `TreeMemoryLimitMb` `WorkDir` | ❌ §2.3 sizing 定版後凍結 |
| **重複量測** | 2 | `ClockType` `Seed` | ❌ `Seed` 是重複量測的自變數 |
| **非 tuning** | 35 | 輸出、顯示、讀檔上限、診斷、solution pool、CPLEX 內建 tune | ❌ 不改求解路線 |
| **搜尋策略** | **109** | 其餘全部，全清單見 §2.2.1 | ✅ **唯一的 variant 池** |

**全 182 顆，依 §2.0 的分類分組。** ⚠️ = 有實測補充（值域與官方文件不符、或有使用限制），
🕸️ = IBM 自 V20.1.0 標為過時但仍可設。每顆的完整說明（含 Interactive Optimizer 指令名）在
`CplexConfig` 的 XML 註解裡，IntelliSense 直接看得到。


#### 停止條件（27 顆） — ❌ 凍結，NEVER 掃描

> 定義「什麼叫解出來了」。同一個 tuning 週期內固定共用，NEVER 進 variant 池；改了它 = 終點線移動，前後數據不可比。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`AbsoluteMipGap`**<br>絕對 MIP gap | `double?` | `Param.MIP.Tolerances.AbsMIPGap` | Any nonnegative number; default : 1e-06. |
| **`BarrierConvergeTol`**<br>barrier 收斂容差（LP / QP） | `double?` | `Param.Barrier.ConvergeTol` | Any positive number greater than or equal to 1e-12; default : 1e-8. |
| **`BarrierIterationLimit`**<br>barrier 迭代次數上限 | `long?` | `Param.Barrier.Limits.Iteration` | `0` No barrier iterations ｜ `9223372036800000000` default ｜ `Any positive integer` Number of barrier iterations before termination |
| **`BarrierQcpConvergeTol`**<br>barrier 收斂容差（QCP） | `double?` | `Param.Barrier.QCPConvergeTol` | Any positive number greater than or equal to 1e-12; default : 1e-7. For LPs and for QPs (that is, when all the constraints are linear) see convergence tolerance for LP and QP problems . |
| **`DeterministicTimeLimit`**<br>決定論時間上限（ticks），跨機器可重現 | `double?` | `Param.DetTimeLimit` | — |
| **`FeasOptRelaxTolerance`**<br>FeasOpt 放鬆量的容差 | `double?` | `Param.Feasopt.Tolerance` | Any nonnegative value; default : 1e-6. |
| **`FeasibilityTol`**<br>simplex 可行性容差（constraint 違反量） | `double?` | `Param.Simplex.Tolerances.Feasibility` | — |
| **`IntegerSolutionLimit`** ⚠️<br>找到 N 個整數解即停 | `long?` | `Param.MIP.Limits.Solutions` | Any positive integer strictly greater than zero; zero is not allowed; default : 9223372036800000000. |
| **`IntegralityTolerance`**<br>整數容差：離整數多近算整數 | `double?` | `Param.MIP.Tolerances.Integrality` | — |
| **`LinearizationTolerance`**<br>QP/MIQP 線性化時使用的 epsilon | `double?` | `Param.MIP.Tolerances.Linearization` | — |
| **`LowerCutoff`**<br>下界剪枝：最大化問題的對應項 | `double?` | `Param.MIP.Tolerances.LowerCutoff` | — |
| **`LowerObjectiveStop`**<br>目標值低於此值即停止（最小化問題的早停門檻） | `double?` | `Param.MIP.Limits.LowerObjStop` | — |
| **`MipGap`**<br>相對 MIP gap，達到即視為收斂停止 | `double?` | `Param.MIP.Tolerances.MIPGap` | Any number from 0.0 to 1.0; default : 1e-04. |
| **`NetworkFeasibilityTol`**<br>network simplex 可行性容差 | `double?` | `Param.Network.Tolerances.Feasibility` | Any number from 1e-11 to 1e-1; default : 1e-6. |
| **`NetworkIterationLimit`**<br>network simplex 迭代次數上限 | `long?` | `Param.Network.Iterations` | Any nonnegative integer; default : 9223372036800000000. |
| **`NetworkOptimalityTol`**<br>network simplex 最佳性容差 | `double?` | `Param.Network.Tolerances.Optimality` | Any number from 1e-11 to 1e-1; default : 1e-6. |
| **`NodeLimit`**<br>B&B 節點數上限 | `long?` | `Param.MIP.Limits.Nodes` | Any nonnegative integer; default : 9223372036800000000. |
| **`ObjectiveDifference`**<br>絕對目標差門檻：新 incumbent 至少要好這麼多才接受 | `double?` | `Param.MIP.Tolerances.ObjDifference` | Any number; default : 0.0. |
| **`OptimalityTol`**<br>simplex 最佳性容差（reduced cost） | `double?` | `Param.Simplex.Tolerances.Optimality` | Any number from 1e-9 to 1e-1; default : 1e-06. |
| **`RelativeObjectiveDifference`**<br>相對目標差門檻 | `double?` | `Param.MIP.Tolerances.RelObjDifference` | Any number from 0.0 to 1.0; default : 0.0. |
| **`SiftingIterationLimit`**<br>sifting 迭代次數上限 | `long?` | `Param.Sifting.Iterations` | Any nonnegative integer; default : 9223372036800000000. |
| **`SimplexIterationLimit`**<br>simplex 迭代次數上限 | `long?` | `Param.Simplex.Limits.Iterations` | Any nonnegative integer; default : 9223372036800000000. |
| **`SimplexLowerObjectiveLimit`**<br>純 LP：目標值低於此值即停止 | `double?` | `Param.Simplex.Limits.LowerObj` | Any number; default : -1e+75. |
| **`SimplexUpperObjectiveLimit`**<br>純 LP：目標值高於此值即停止 | `double?` | `Param.Simplex.Limits.UpperObj` | Any number; default : 1e+75. |
| **`TimeLimit`**<br>牆鐘求解時間上限（秒） | `double?` | `Param.TimeLimit` | Any nonnegative value in seconds; default : 1e+75. |
| **`UpperCutoff`**<br>上界剪枝：已知最小化問題的解不會大於此值時填入，直接砍掉更差的分支 | `double?` | `Param.MIP.Tolerances.UpperCutoff` | — |
| **`UpperObjectiveStop`**<br>目標值超過此值即停止（最大化問題的早停門檻） | `double?` | `Param.MIP.Limits.UpperObjStop` | — |

#### 執行資源（9 顆） — ❌ 凍結，NEVER 掃描

> 定義量測基準（機器資源、計時、記憶體）。R0 校準之前先單獨定版，之後整期凍結；改了它 = 量測的尺會伸縮。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`AuxiliaryRootThreads`**<br>root 節點輔助工作分到幾個執行緒 | `int?` | `Param.MIP.Limits.AuxRootThreads` | `-1` Off: do not use additional threads for auxiliary tasks ｜ `0` Automatic: let CPLEX choose the number of threads to use **(預設)** ｜ `N > n > 0` Use n threads for auxiliary root tasks |
| **`CpuMask`** ⚠️<br>把執行緒綁到指定 core，用來壓低多執行緒時序造成的量測雜訊 | `string` | `Param.CPUmask` | — |
| **`MemoryEmphasis`**<br>記憶體節約模式：犧牲速度換記憶體 | `bool?` | `Param.Emphasis.Memory` | `0` Off; do not conserve memory **(預設)** ｜ `1` On; conserve memory where possible |
| **`MemoryLimitMb`**<br>工作記憶體 (MB)，管的是 live tree 大小不是行程總記憶體 | `double?` | `Param.WorkMem` | — |
| **`NodeFileStrategy`**<br>樹超過記憶體上限時節點怎麼存 | `int?` | `Param.MIP.Strategy.File` | `0` No node file ｜ `1` Node file in memory and compressed **(預設)** ｜ `2` Node file on disk ｜ `3` Node file on disk and compressed |
| **`ParallelMode`**<br>平行模式：決定搜尋路徑可不可重現 | `int?` | `Param.Parallel` | `-1` Enable opportunistic parallel search mode ｜ `0` Automatic: let CPLEX decide whether to invoke deterministic or opportunistic search **(預設)** ｜ `1` Enable deterministic parallel search mode |
| **`Threads`**<br>求解可用的工作執行緒上限 | `int?` | `Param.Threads` | `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Sequential; single threaded ｜ `N` Uses up to N threads; N is limited by available processors and Processor Value Units (PVU) |
| **`TreeMemoryLimitMb`**<br>B&B 樹記憶體上限 (MB) | `double?` | `Param.MIP.Limits.TreeMemory` | Any nonnegative number; default : 1e+75. |
| **`WorkDir`**<br>節點檔等暫存檔的目錄；NodeFileStrategy 設 2/3 時才有意義 | `string` | `Param.WorkDir` | Any existing directory; default : ‘.’ |

#### 重複量測（2 顆） — ❌ 凍結，NEVER 掃描

> 同一個設定再量一次用的工具，不是候選設定。Seed 換一個只是同一設定再量一次。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`ClockType`**<br>計時基準：1 CPU time / 2 wall-clock | `int?` | `Param.ClockType` | `0` Automatic: let CPLEX choose ｜ `1` CPU time ｜ `2` Wall clock time (total physical time elapsed) **(預設)** |
| **`Seed`** ⚠️<br>隨機種子。重複量測用的自變數，NEVER 當成候選設定排名 | `int?` | `Param.RandomSeed` | Any nonnegative integer; that is, an integer in the interval [0, BIGINT]. The default value of this parameter changes with each release. |

#### 搜尋策略 · emphasis 與搜尋分支（18 顆） — ✅ 可進 variant 池

> 改求解路徑、終點不變。可進 variant 池，但一輪只改一顆。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`AdvancedStart`**<br>是否使用 advanced basis / 起始向量 | `int?` | `Param.Advance` | `0` Do not use advanced start information ｜ `1` Use an advanced basis supplied by the user **(預設)** ｜ `2` Crush an advanced basis or starting vector supplied by the user |
| **`BacktrackTolerance`**<br>回溯容差：越小越傾向繼續往深處走 | `double?` | `Param.MIP.Strategy.Backtrack` | Any number from 0.0 to 1.0; default : 0.9999 |
| **`BestBoundInterval`**<br>best-estimate 搜尋中每隔幾個節點強制選一次 best-bound | `long?` | `Param.MIP.Strategy.BBInterval` | `0` Never select best bound node; always select best estimate ｜ `1` Always select best bound node ｜ `7` Select best bound node occasionally **(預設)** ｜ `Any positive integer` Select best bound node less frequently than best estimate node |
| **`BranchDirection`**<br>分支方向：先試哪一邊 | `int?` | `Param.MIP.Strategy.Branch` | `-1` Down branch selected first ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Up branch selected first |
| **`DiveType`**<br>潛降策略 | `int?` | `Param.MIP.Strategy.Dive` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Traditional dive ｜ `2` Probing dive ｜ `3` Guided dive |
| **`Emphasis`**<br>MIP emphasis：整體偏向可行解、最佳性還是推 bound | `int?` | `Param.Emphasis.MIP` | `0` Balance optimality and feasibility **(預設)** ｜ `1` Emphasize feasibility over optimality ｜ `2` Emphasize optimality over feasibility ｜ `3` Emphasize moving best bound ｜ `4` Emphasize finding hidden feasible solutions ｜ `5` Emphasize finding high quality feasible solutions earlier |
| **`KappaStatistics`**<br>計算 MIP kappa（條件數）統計，判斷數值不穩的量化證據 | `int?` | `Param.MIP.Strategy.KappaStats` | `–1` No MIP kappa statistics ｜ `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Compute MIP kappa for a sample of subproblems ｜ `2` Compute MIP kappa for all subproblems |
| **`MipSearch`**<br>搜尋模式：dynamic search 或傳統 B&C | `int?` | `Param.MIP.Strategy.Search` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Apply traditional branch and cut strategy; disable dynamic search ｜ `2` Apply dynamic search |
| **`NodeSelect`**<br>節點選擇策略 | `int?` | `Param.MIP.Strategy.NodeSelect` | `0` Depth-first search ｜ `1` Best-bound search **(預設)** ｜ `2` Best-estimate search ｜ `3` Alternative best-estimate search |
| **`NumericalEmphasis`**<br>數值穩定優先（犧牲速度換精度） | `bool?` | `Param.Emphasis.Numerical` | `0` Do not emphasize numerical precision **(預設)** ｜ `1` Exercise extreme caution in computation |
| **`OptimalityTarget`** ⚠️<br>非凸 QP 要求全域最佳還是局部最佳 | `int?` | `Param.OptimalityTarget` | `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Searches for a globally optimal solution to a convex model ｜ `2` Searches for a solution that satisfies first-order optimality conditions, but is not necessarily globally optimal ｜ `3` Searches for a globally optimal solution to a nonconvex model; changes problem type to MIQP if necessary |
| **`PriorityOrderType`**<br>沒有優先序時讓 CPLEX 自動產生一份，依什麼規則產 | `int?` | `Param.MIP.OrderType` | `0` Do not generate a priority order ｜ `1` Use decreasing cost ｜ `2` Use increasing bound range ｜ `3` Use increasing cost per coefficient count |
| **`Probe`**<br>探測強度 | `int?` | `Param.MIP.Strategy.Probe` | `-1` No probing ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Moderate probing level ｜ `2` Aggressive probing level ｜ `3` Very aggressive probing level |
| **`SolutionType`**<br>LP/QP 要回基底解還是非基底解 | `int?` | `Param.SolutionType` | `0` Automatic: let CPLEX decide **(預設)** ｜ `1` CPLEX computes a basic solution ｜ `2` CPLEX computes a primal-dual pair of solution-vectors |
| **`StrongBranchingCandidateLimit`** ⚠️<br>strong branching 候選清單長度 | `int?` | `Param.MIP.Limits.StrongCand` | Any positive number; default : 10. |
| **`StrongBranchingIterationLimit`**<br>strong branching 每個候選跑幾次 simplex 迭代 | `long?` | `Param.MIP.Limits.StrongIt` | `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Limit of the simplex iterations performed on each candidate variable |
| **`UsePriorityOrder`**<br>是否套用分支優先序 | `bool?` | `Param.MIP.Strategy.Order` | `0` Off: do not use priority order ｜ `1` On: use priority order, if it exists **(預設)** |
| **`VariableSelect`**<br>分支變數選擇策略 | `int?` | `Param.MIP.Strategy.VariableSelect` | `-1` Branch on variable with minimum infeasibility ｜ `0` Automatic: let CPLEX choose variable to branch on **(預設)** ｜ `1` Branch on variable with maximum infeasibility ｜ `2` Branch based on pseudo costs ｜ `3` Strong branching ｜ `4` Branch based on pseudo reduced costs |

#### 搜尋策略 · 啟發式與 solution polishing（13 顆） — ✅ 可進 variant 池

> 找可行解、改善 incumbent。找不到解或 gap 收不下來時的主力。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`CardinalityLocalSearch`**<br>基數限制式的區域搜尋啟發式 | `int?` | `Param.MIP.Strategy.CardLs` | `-1` None: do not apply the CLSH **(預設)** ｜ `0` Automatic: let CPLEX choose ｜ `1` Apply the CLSH only at the root node ｜ `2` Apply the CLSH at the nodes of the branch and bound tree |
| **`FeasibilityPumpHeuristic`**<br>可行解幫浦：專門對付「連第一個可行解都找不到」 | `int?` | `Param.MIP.Strategy.FPHeur` | `-1` Do not apply the feasibility pump heuristic ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Apply the feasibility pump heuristic with an emphasis on finding a feasible solution ｜ `2` Apply the feasibility pump heuristic with an emphasis on finding a feasible solution with a good objective value |
| **`HeuristicEffort`**<br>啟發式投入程度（0 關閉 / 1 預設 / >1 更積極） | `double?` | `Param.MIP.Strategy.HeuristicEffort` | `0` Disable heuristics ｜ `<1` Decrease effort ｜ `1` default ｜ `>1` Increase effort |
| **`HeuristicFrequency`**<br>週期性啟發式的頻率：每 N 個節點跑一次 | `long?` | `Param.MIP.Strategy.HeuristicFreq` | `-1` None ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Apply the periodic heuristic at this frequency |
| **`LocalBranchingHeuristic`**<br>local branching：對每個新 incumbent 再試著改善 | `bool?` | `Param.MIP.Strategy.LBHeur` | `0` Local branching heuristic is off **(預設)** ｜ `1` Apply local branching heuristic to new incumbent |
| **`PolishAfterAbsoluteMipGap`**<br>絕對 gap 收到多小之後開始 polishing | `double?` | `Param.MIP.PolishAfter.AbsMIPGap` | Any nonnegative value; default : 0.0. |
| **`PolishAfterDetTime`**<br>跑滿幾個 ticks 後開始 polishing（決定論版） | `double?` | `Param.MIP.PolishAfter.DetTime` | Any nonnegative value in deterministic ticks; default :1.0E+75 ticks. |
| **`PolishAfterMipGap`**<br>相對 gap 收到多小之後開始 polishing | `double?` | `Param.MIP.PolishAfter.MIPGap` | Any number from 0.0 to 1.0, inclusive; default : 0.0. |
| **`PolishAfterNodes`**<br>處理幾個節點後開始 polishing | `long?` | `Param.MIP.PolishAfter.Nodes` | Any nonnegative integer; default : 9223372036800000000 |
| **`PolishAfterSolutions`** ⚠️<br>找到幾個整數解後開始 polishing | `long?` | `Param.MIP.PolishAfter.Solutions` | Any positive integer strictly greater than zero; zero is not allowed; default : 9223372036800000000 |
| **`PolishAfterTime`**<br>跑滿幾秒後開始 solution polishing | `double?` | `Param.MIP.PolishAfter.Time` | Any nonnegative value in seconds; default :1.0E+75 seconds. |
| **`RepairTries`**<br>MIP start 不可行時嘗試修復幾次 | `long?` | `Param.MIP.Limits.RepairTries` | `-1` None: do not try to repair ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Number of attempts |
| **`RinsHeuristicFrequency`**<br>RINS 啟發式頻率 | `long?` | `Param.MIP.Strategy.RINSHeur` | `-1` None: do not apply RINS heuristic ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Frequency to apply RINS heuristic |

#### 搜尋策略 · 切割平面（22 顆） — ✅ 可進 variant 池

> 推 dual bound 用。每族 -1 關閉 / 0 自動 / 1..N 漸積極。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`AggregationLimitForCut`**<br>生 cut 時最多聚合幾條限制式 | `int?` | `Param.MIP.Limits.AggForCut` | Any nonnegative integer; default : 3 |
| **`BqpCuts`**<br>Boolean Quadric Polytope cuts（非凸 QP / MIQP） | `int?` | `Param.MIP.Cuts.BQP` | `-1` Do not generate BQP cuts ｜ `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Generate BQP cuts moderately ｜ `2` Generate BQP cuts aggressively ｜ `3` Generate BQP cuts very aggressively |
| **`CliqueCuts`**<br>clique cuts | `int?` | `Param.MIP.Cuts.Cliques` | `-1` Do not generate clique cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate clique cuts moderately ｜ `2` Generate clique cuts aggressively ｜ `3` Generate clique cuts very aggressively |
| **`CoverCuts`**<br>cover cuts | `int?` | `Param.MIP.Cuts.Covers` | `-1` Do not generate cover cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate cover cuts moderately ｜ `2` Generate cover cuts aggressively ｜ `3` Generate cover cuts very aggressively |
| **`CutPasses`**<br>cut 生成回合數 | `long?` | `Param.MIP.Limits.CutPasses` | `-1` None ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Number of passes to perform |
| **`CutsFactor`**<br>cut 總數上限倍數（相對原始列數） | `double?` | `Param.MIP.Limits.CutsFactor` | — |
| **`DisjunctiveCuts`**<br>disjunctive cuts | `int?` | `Param.MIP.Cuts.Disjunctive` | `-1` Do not generate disjunctive cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate disjunctive cuts moderately ｜ `2` Generate disjunctive cuts aggressively ｜ `3` Generate disjunctive cuts very aggressively |
| **`EachCutLimit`**<br>每一族 cut 各自的數量上限 | `int?` | `Param.MIP.Limits.EachCutLimit` | `0` No cuts ｜ `Any positive number` Limit each type of cut ｜ `2100000000` default |
| **`FlowCoverCuts`**<br>flow cover cuts | `int?` | `Param.MIP.Cuts.FlowCovers` | `-1` Do not generate flow cover cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate flow cover cuts moderately ｜ `2` Generate flow cover cuts aggressively |
| **`FlowPathCuts`**<br>flow path cuts | `int?` | `Param.MIP.Cuts.PathCut` | `-1` Do not generate flow path cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate flow path cuts moderately ｜ `2` Generate flow path cuts aggressively |
| **`GomoryCandidateLimit`** ⚠️<br>Gomory cut 的候選數上限 | `int?` | `Param.MIP.Limits.GomoryCand` | Any positive integer; default : 200. |
| **`GomoryCuts`**<br>Gomory fractional cuts | `int?` | `Param.MIP.Cuts.Gomory` | `-1` Do not generate Gomory fractional cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate Gomory fractional cuts moderately ｜ `2` Generate Gomory fractional cuts aggressively |
| **`GomoryPassLimit`**<br>Gomory cut 的回合數上限 | `long?` | `Param.MIP.Limits.GomoryPass` | `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Number of passes to generate Gomory fractional cuts |
| **`GubCoverCuts`**<br>GUB cover cuts：對「一組 0-1 變數只能挑一個」的結構有效 | `int?` | `Param.MIP.Cuts.GUBCovers` | `-1` Do not generate GUB cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate GUB cuts moderately ｜ `2` Generate GUB cuts aggressively |
| **`ImpliedBoundCuts`**<br>全域有效的 implied bound cuts：big-M / indicator 結構的標配 | `int?` | `Param.MIP.Cuts.Implied` | `-1` Do not generate implied bound cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate implied bound cuts moderately ｜ `2` Generate implied bound cuts aggressively |
| **`LiftAndProjectCuts`**<br>lift-and-project cuts | `int?` | `Param.MIP.Cuts.LiftProj` | `-1` Do not generate lift-and-project cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate lift-and-project cuts moderately ｜ `2` Generate lift-and-project cuts aggressively ｜ `3` Generate lift-and-project cuts very aggressively |
| **`LocalImpliedBoundCuts`**<br>只在子樹內有效的 implied bound cuts | `int?` | `Param.MIP.Cuts.LocalImplied` | `-1` Do not generate locally valid implied bound cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate locally valid implied bound cuts moderately ｜ `2` Generate locally valid implied bound cuts aggressively ｜ `3` Generate locally valid implied bound cuts very aggressively |
| **`McfCuts`**<br>multi-commodity flow cuts：網路流結構 | `int?` | `Param.MIP.Cuts.MCFCut` | `-1` Turn off MCF cuts ｜ `0` Automatic: let CPLEX decide whether to generate MCF cuts **(預設)** ｜ `1` Generate a moderate number of MCF cuts ｜ `2` Generate MCF cuts aggressively |
| **`MirCuts`**<br>mixed-integer rounding cuts | `int?` | `Param.MIP.Cuts.MIRCut` | `-1` Do not generate MIR cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate MIR cuts moderately ｜ `2` Generate MIR cuts aggressively |
| **`NodeCuts`**<br>root 之外的節點要不要繼續生 cut | `int?` | `Param.MIP.Cuts.Nodecuts` | `-1` Do not generate node cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate node cuts moderately ｜ `2` Generate node cuts aggressively ｜ `3` Generate node cuts very aggressively |
| **`RltCuts`**<br>Reformulation Linearization Technique cuts（非凸 QP / MIQP） | `int?` | `Param.MIP.Cuts.RLT` | `-1` Do not generate RLT cuts ｜ `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Generate RLT cuts moderately ｜ `2` Generate RLT cuts aggressively ｜ `3` Generate RLT cuts very aggressively |
| **`ZeroHalfCuts`**<br>zero-half cuts：對純 0-1 問題特別有效 | `int?` | `Param.MIP.Cuts.ZeroHalfCut` | `-1` Do not generate zero-half cuts ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Generate zero-half cuts moderately ｜ `2` Generate zero-half cuts aggressively |

#### 搜尋策略 · 前處理（16 顆） — ✅ 可進 variant 池

> presolve、對稱破除、縮放。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`AggregatorFill`**<br>聚合器允許產生的 fill-in 量 | `long?` | `Param.Preprocessing.Fill` | Any nonnegative integer; default : 10 |
| **`AggregatorLimit`**<br>前處理聚合器套用次數上限 | `int?` | `Param.Preprocessing.Aggregator` | `-1` Automatic (1 for LP, infinite for MIP) default ｜ `0` Do not use any aggregator ｜ `Any positive integer` Number of times to apply aggregator |
| **`BoundStrengthening`**<br>界限收緊 | `int?` | `Param.Preprocessing.BoundStrength` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Do not apply bound strengthening ｜ `1` Apply bound strengthening |
| **`CoefficientReduction`**<br>係數縮減，會影響 LP 鬆弛的緊度 | `int?` | `Param.Preprocessing.CoeffReduce` | `-1` Automatic: let CPLEX decide **(預設)** ｜ `0` Do not use coefficient reduction ｜ `1` Reduce only to integral coefficients ｜ `2` Reduce all potential coefficients ｜ `3` Reduce aggressively with tilting |
| **`DependencyCheck`**<br>偵測並移除相依（重複）的限制式 | `int?` | `Param.Preprocessing.Dependency` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Off: do not use dependency checker ｜ `1` Turn on only at the beginning of preprocessing ｜ `2` Turn on only at the end of preprocessing ｜ `3` Turn on at the beginning and at the end of preprocessing |
| **`LpFolding`**<br>純 LP 的 folding 縮減 | `int?` | `Param.Preprocessing.Folding` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Turn off folding ｜ `1` Exert a moderate level of folding ｜ `2` Exert an aggressive level of folding ｜ `3` Exert a very aggressive level of folding ｜ `4` Exert a highly aggressive level of folding ｜ `5` Exert an extremely aggressive level of folding |
| **`NodePresolve`**<br>節點上要不要做 presolve；單節點太貴時的正面手段 | `int?` | `Param.MIP.Strategy.PresolveNode` | `-1` No node presolve ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Force presolve at nodes ｜ `2` Perform probing on integer-infeasible variables ｜ `3` Perform aggressive node probing |
| **`PreIndicator`**<br>是否啟用前處理。infeasible 找不出原因時可關掉它再跑 IIS | `bool?` | `Param.Preprocessing.Presolve` | `0` Do not apply presolve ｜ `1` Apply presolve **(預設)** |
| **`PresolveDual`**<br>是否對 LP 走對偶形式做 presolve | `int?` | `Param.Preprocessing.Dual` | `-1` Turn off this feature ｜ `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Turn on this feature |
| **`PresolvePasses`**<br>presolve 回合數上限 | `int?` | `Param.Preprocessing.NumPass` | `-1` Automatic: let CPLEX choose; presolve continues as long as helpful **(預設)** ｜ `0` Do not use presolve; other reductions may still occur ｜ `Any positive integer` Apply presolve specified number of times |
| **`PresolveReduce`**<br>做 primal / dual / 兩者 / 都不做 的縮減 | `int?` | `Param.Preprocessing.Reduce` | `0` No primal or dual reductions ｜ `1` Only primal reductions ｜ `2` Only dual reductions ｜ `3` Both primal and dual reductions **(預設)** |
| **`PresolveReformulations`**<br>允許哪些 presolve reformulation | `int?` | `Param.Preprocessing.Reformulations` | `0` no reformulations ｜ `1` allow reformulations that interfere with crushing forms ｜ `2` allow reformulations that interfere with uncrushing forms ｜ `3` allow all reformulations **(預設)** |
| **`RelaxedLpPresolve`**<br>root 鬆弛是否額外做一次 LP presolve | `int?` | `Param.Preprocessing.Relax` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Off: do not use presolve on initial relaxation ｜ `1` On: use presolve on initial relaxation |
| **`RepeatPresolve`**<br>root 處理完後要不要重跑一次 presolve | `int?` | `Param.Preprocessing.RepeatPresolve` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Turn off represolve ｜ `1` Represolve without cuts ｜ `2` Represolve with cuts ｜ `3` Represolve with cuts and allow new root cuts |
| **`Scaling`**<br>矩陣縮放方式：係數量級差距大時的第二手段（第一手段是 NumericalEmphasis） | `int?` | `Param.Read.Scale` | `-1` No scaling ｜ `0` Equilibration scaling **(預設)** ｜ `1` More aggressive scaling |
| **`Symmetry`**<br>對稱破除強度：排班、指派這類同質資源的題目值得試 | `int?` | `Param.Preprocessing.Symmetry` | `-1` Automatic: let CPLEX choose **(預設)** ｜ `0` Turn off symmetry breaking ｜ `1` Exert a moderate level of symmetry breaking ｜ `2` Exert an aggressive level of symmetry breaking ｜ `3` Exert a very aggressive level of symmetry breaking ｜ `4` Exert a highly aggressive level of symmetry breaking ｜ `5` Exert an extremely aggressive level of symmetry breaking |

#### 搜尋策略 · root 與節點的 LP 演算法（25 顆） — ✅ 可進 variant 池

> Simplex / Barrier / Sifting / Network 的選擇與內部設定。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`BarrierAlgorithm`**<br>barrier 演算法選擇 | `int?` | `Param.Barrier.Algorithm` | `0` Default setting ｜ `1` Infeasibility-estimate start ｜ `2` Infeasibility-constant start ｜ `3` Standard barrier |
| **`BarrierColumnNonzeros`**<br>被視為稠密行的非零數門檻 | `int?` | `Param.Barrier.ColNonzeros` | `0` Dynamically calculated **(預設)** ｜ `Any positive integer` Number of nonzero entries that make a column dense |
| **`BarrierCorrectionLimit`**<br>barrier 中心化修正次數上限 | `long?` | `Param.Barrier.Limits.Corrections` | `-1` Automatic; let CPLEX choose **(預設)** ｜ `0` None ｜ `Any positive integer` Maximum number of centering corrections per iteration |
| **`BarrierCrossover`**<br>barrier 之後要不要 crossover 回基底解 | `int?` | `Param.Barrier.Crossover` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal crossover ｜ `2` Dual crossover |
| **`BarrierGrowthLimit`**<br>barrier 不穩定判定的成長上限 | `double?` | `Param.Barrier.Limits.Growth` | 1.0 or greater; default : 1e12. |
| **`BarrierObjectiveRange`**<br>barrier 目標值的可接受範圍 | `double?` | `Param.Barrier.Limits.ObjRange` | Any nonnegative number; default : 1e20 |
| **`BarrierOrdering`**<br>barrier 的排序演算法 | `int?` | `Param.Barrier.Ordering` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Approximate minimum degree (AMD) ｜ `2` Approximate minimum fill (AMF) ｜ `3` Nested dissection (ND) |
| **`BarrierStartAlgorithm`** ⚠️<br>barrier 起始點演算法 | `int?` | `Param.Barrier.StartAlg` | `1` Dual is 0 (zero) **(預設)** ｜ `2` Estimate dual ｜ `3` Average of primal estimate, dual 0 (zero) ｜ `4` Average of primal estimate, estimate dual |
| **`DualSimplexPricing`**<br>dual simplex 定價法 | `int?` | `Param.Simplex.DGradient` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Standard dual pricing ｜ `2` Steepest-edge pricing ｜ `3` Steepest-edge pricing in slack space ｜ `4` Steepest-edge pricing, unit initial norms ｜ `5` devex pricing |
| **`MarkowitzTolerance`**<br>Markowitz 樞紐容差：數值不穩時調大 | `double?` | `Param.Simplex.Tolerances.Markowitz` | Any number from 0.0001 to 0.99999; default : 0.01. |
| **`NetworkExtractionLevel`** ⚠️<br>從模型中萃取網路結構的積極程度 | `int?` | `Param.Network.NetFind` | `1` Extract pure network only ｜ `2` Try reflection scaling **(預設)** ｜ `3` Try general scaling |
| **`NetworkPricing`**<br>network simplex 定價法 | `int?` | `Param.Network.Pricing` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Partial pricing ｜ `2` Multiple partial pricing ｜ `3` Multiple partial pricing with sorting |
| **`NodeAlgorithm`**<br>子問題（非根節點）用哪個連續最佳化器 | `int?` | `Param.NodeAlgorithm` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal simplex ｜ `2` Dual simplex ｜ `3` Network simplex ｜ `4` Barrier ｜ `5` Sifting |
| **`PrimalSimplexPricing`**<br>primal simplex 定價法 | `int?` | `Param.Simplex.PGradient` | `-1` Reduced-cost pricing ｜ `0` Hybrid reduced-cost & devex pricing **(預設)** ｜ `1` Devex pricing ｜ `2` Steepest-edge pricing ｜ `3` Steepest-edge pricing with slack initial norms ｜ `4` Full pricing |
| **`RootAlgorithm`**<br>root 鬆弛用哪個連續最佳化器 | `int?` | `Param.RootAlgorithm` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal Simplex ｜ `2` Dual Simplex ｜ `3` Network Simplex ｜ `4` Barrier ｜ `5` Sifting ｜ `6` Concurrent (Dual, Barrier, and Primal in opportunistic mode; Dual and Barrier in deterministic mode) |
| **`SiftingAlgorithm`**<br>sifting 子問題用哪個演算法 | `int?` | `Param.Sifting.Algorithm` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal Simplex ｜ `2` Dual Simplex ｜ `3` Network Simplex ｜ `4` Barrier |
| **`SiftingFromSimplex`**<br>是否允許 simplex 內部切換到 sifting | `bool?` | `Param.Sifting.Simplex` | `1` default CPLEX executes sifting during simplex optimization under appropriate conditions ｜ `0` CPLEX turns off sifting during simplex optimization |
| **`SimplexCrash`**<br>起始基底的 crash 排序 | `int?` | `Param.Simplex.Crash` | `-1` Alternate ways of using objective coefficients ｜ `0` Ignore objective coefficients during crash ｜ `1` Alternate ways of using objective coefficients **(預設)** ｜ `-1` Aggressive starting basis ｜ `0` Aggressive starting basis ｜ `1` Default starting basis **(預設)** ｜ `-1` Slack basis ｜ `0` Ignore Q terms and use LP solver for crash ｜ `1` Ignore objective and use LP solver for crash **(預設)** ｜ `-1` Slack basis ｜ `0` Use Q terms for crash ｜ `1` Use Q terms for crash **(預設)** |
| **`SimplexDynamicRows`**<br>dual simplex 的動態列管理 | `int?` | `Param.Simplex.DynamicRows` | `-1` automatic: Let CPLEX decide. default ｜ `0` Tell CPLEX to keep all rows ｜ `1` Let CPLEX manage rows |
| **`SimplexPerturbationConstant`**<br>擾動常數 | `double?` | `Param.Simplex.Perturbation.Constant` | Any positive number greater than or equal to 1e-8; default : 1e-6. |
| **`SimplexPerturbationIndicator`**<br>一開始就強制擾動（退化嚴重時） | `bool?` | `Param.Simplex.Perturbation.Indicator` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Turn on perturbation from beginning |
| **`SimplexPerturbationLimit`**<br>停滯幾次之後自動擾動 | `int?` | `Param.Simplex.Limits.Perturbation` | `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Number of degenerate iterations before perturbation |
| **`SimplexPricingCandidateList`**<br>定價候選清單大小 | `int?` | `Param.Simplex.Pricing` | `0` Automatic: let CPLEX choose **(預設)** ｜ `Any positive integer` Number of pricing candidates |
| **`SimplexRefactorFrequency`**<br>重新分解基底的頻率 | `int?` | `Param.Simplex.Refactor` | `0` Automatic: let CPLEX choose **(預設)** ｜ `Integer from 1 to 10 000` Number of iterations between refactoring of the basis matrix |
| **`SimplexSingularityLimit`**<br>奇異基底修復次數上限 | `int?` | `Param.Simplex.Limits.Singularity` | Any nonnegative integer; default : 10. |

#### 搜尋策略 · 特定模型類別（15 顆） — ✅ 可進 variant 池

> Benders、QP / MIQCP、SOS、subMIP、FeasOpt。模型沒有對應結構時設了也沒作用。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`BendersFeasibilityCutTol`**<br>Benders 可行性割的容差 | `double?` | `Param.Benders.Tolerances.FeasibilityCut` | — |
| **`BendersOptimalityCutTol`**<br>Benders 最佳性割的容差 | `double?` | `Param.Benders.Tolerances.OptimalityCut` | — |
| **`BendersStrategy`** ⚠️<br>Benders 分解策略；模型要有 Benders annotation 才有作用 | `int?` | `Param.Benders.Strategy` | `-1` Execute conventional branch and bound; ignore any Benders annotations. That is, do not use Benders algorithm even if a Benders partition of the current model is present ｜ `0` default Let CPLEX decide. case 1: If the user supplies no annotations with the model, CPLEX executes conventional branch and bound. case 2: If annotations specifying a Benders partition of the current model are available, CPLEX attempts to decompose the model. CPLEX uses the master as given by the annotations, and attempts to partition the subproblems further, if possible, before applying Benders algorithm to solve the model. If the user supplied annotations, but the annotations supplied do not lead to a complete decomposition into master and disjoint subproblems (that is, if the annotations are wrong in that sense), CPLEX produces the error CPXERR_BAD_DECOMPOSITION  ｜ `1` CPLEX applies Benders algorithm to a decomposition based on annotations supplied by the user. If no annotations to decompose the model are available, this setting produces the error CPXERR_NO_DECOMPOSITION . If the user supplies annotations, but the supplied annotations do not lead to a complete partition of the original model into disjoint master and subproblems, then this setting produces the error CPXERR_BAD_DECOMPOSITION  ｜ `2` CPLEX accepts the master as given and attempts to decompose the remaining elements into disjoint subproblems to assign to workers. It then solves the Benders decomposition of the model. If no annotations to decompose the model are available, this setting produces the error CPXERR_NO_DECOMPOSITION . If the user supplies annotations, but the supplied annotations do not lead to a complete partition of the original model into disjoint master and subproblems, then this setting produces the error CPXERR_BAD_DECOMPOSITION  ｜ `3` CPLEX ignores any annotation file supplied with the model; CPLEX applies presolve; CPLEX then automatically generates a Benders partition, putting integer variables in master and continuous linear variables into disjoint subproblems. CPLEX then solves the Benders decomposition of the model. If the problem is a strictly linear program (LP), that is, there are no integer-constrained variables to put into master, then CPLEX reports the error CPXERR_PARAM_INCOMPATIBLE . If the problem is a mixed integer linear program (MILP) where all variables are integer-constrained, (that is, there are no continuous linear variables to decompose into disjoint subproblems) then CPLEX reports the error CPXERR_NO_DECOMPOSITION . If the problem is a mixed integer linear program (MILP) where all variables are continuous, (that is, there are no integer-constrained variables to decompose into master) then CPLEX reports the error CPXERR_NO_DECOMPOSITION  |
| **`BendersWorkerAlgorithm`**<br>Benders 子問題用哪個演算法 | `int?` | `Param.Benders.WorkerAlgorithm` | `0` default Let CPLEX decide ｜ `1` CPLEX applies the primal simplex algorithm to workers ｜ `2` CPLEX applies the dual simplex algorithm to workers ｜ `3` CPLEX applies the network simplex algorithm to workers ｜ `4` CPLEX applies the barrier algorithm to workers ｜ `5` CPLEX applies the sifting algorithm to workers |
| **`CalculateQcpDuals`**<br>是否計算 QCP 的對偶值 | `int?` | `Param.Preprocessing.QCPDuals` | `0` no ｜ `1` if_possible ｜ `2` force |
| **`FeasOptMode`**<br>FeasOpt 放鬆不可行模型時的目標 | `int?` | `Param.Feasopt.Mode` | `0` Minimize the sum of all required relaxations in first phase only **(預設)** ｜ `1` Minimize the sum of all required relaxations in first phase and execute second phase to find optimum among minimal relaxations ｜ `2` Minimize the number of constraints and bounds requiring relaxation in first phase only ｜ `3` Minimize the number of constraints and bounds requiring relaxation in first phase and execute second phase to find optimum among minimal relaxations ｜ `4` Minimize the sum of squares of required relaxations in first phase only ｜ `5` Minimize the sum of squares of required relaxations in first phase and execute second phase to find optimum among minimal relaxations |
| **`MiqcpStrategy`**<br>MIQCP 用哪種鬆弛 | `int?` | `Param.MIP.Strategy.MIQCPStrat` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Solve a QCP node relaxation at each node ｜ `2` Solve an LP node relaxation at each node |
| **`QpMakePsd`**<br>把非凸二元 QP 重新表述成凸的 | `bool?` | `Param.Preprocessing.QPMakePSD` | `0` Turn off attempts to make binary model PSD ｜ `1` On: CPLEX attempts to make binary model PSD **(預設)** |
| **`QpToLinear`**<br>把 QP / MIQP 的二次項線性化 | `int?` | `Param.Preprocessing.QToLin` | `-1` Automatic: let CPLEX decide ( default ) ｜ `0` Off: CPLEX does not linearize quadratic terms in the objective function of QP, MIQP ｜ `1` On: CPLEX linearizes quadratic terms in the objective function of QP, MIQP |
| **`Sos1Reformulation`**<br>SOS1 重新表述 | `int?` | `Param.Preprocessing.SOS1Reform` | `-1` No reformulation: CPLEX does not reformulate special ordered sets of type 1 (SOS1) as linear constraints ｜ `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Logarithmic: CPLEX reformulates special ordered sets of type 1 (SOS1) as linear constraints, with a reformulation which is logarithmic in the size of the special ordered sets |
| **`Sos2Reformulation`**<br>SOS2 重新表述 | `int?` | `Param.Preprocessing.SOS2Reform` | `-1` No reformulation: CPLEX does not reformulate special ordered sets of type 2 (SOS2) as linear constraints ｜ `0` Automatic: let CPLEX decide **(預設)** ｜ `1` Logarithmic: CPLEX reformulates special ordered sets of type 2 (SOS2) as linear constraints, with a reformulation which is logarithmic in the size of the special ordered sets |
| **`SubMipNodeAlgorithm`**<br>subMIP 子問題的演算法 | `int?` | `Param.MIP.SubMIP.SubAlg` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal Simplex ｜ `2` Dual Simplex ｜ `3` Network Simplex ｜ `4` Barrier ｜ `5` Sifting |
| **`SubMipNodeLimit`** ⚠️<br>啟發式內部解 subMIP 時的節點上限 | `long?` | `Param.MIP.SubMIP.NodeLimit` | — |
| **`SubMipRootAlgorithm`**<br>subMIP 初始鬆弛的演算法 | `int?` | `Param.MIP.SubMIP.StartAlg` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Primal Simplex ｜ `2` Dual Simplex ｜ `3` Network Simplex ｜ `4` Barrier ｜ `5` Sifting |
| **`SubMipScaling`**<br>subMIP 的縮放設定 | `int?` | `Param.MIP.SubMIP.Scale` | `-1` No scaling ｜ `0` Equilibration scaling **(預設)** ｜ `1` More aggressive scaling |

#### 非 tuning 參數（35 顆） — ❌ 凍結，NEVER 掃描

> 輸出、顯示、讀檔上限、診斷、solution pool、CPLEX 內建 tune。這些不改求解策略，NEVER 放進 variant 池掃描。

| `CplexConfig` 欄位 | 型別 | 官方參數 | 值域 / 預設（IBM 原文） |
| --- | --- | --- | --- |
| **`BarrierDisplay`**<br>barrier log 詳細度 | `int?` | `Param.Barrier.Display` | `0` No progress information ｜ `1` Normal setup and iteration information **(預設)** ｜ `2` Diagnostic information |
| **`CloneLog`**<br>平行求解時每個 clone 各寫一份 log（診斷用） | `bool?` | `Param.Output.CloneLog` | `-1` CPLEX does not clone log files. (off) ｜ `0` Automatic: CPLEX clones log files if log file is specified. default ｜ `1` CPLEX clones log files. (on) |
| **`ColumnRead`** 🕸️<br>讀檔時的變數（行）數量上限 | `int?` | `Param.Read.Variables` | Any integer from 0 (zero) to CPX_BIGINT ; default : 60 000. |
| **`ConflictAlgorithm`**<br>conflict refiner 找最小衝突集的演算法；IIS 取證用 | `int?` | `Param.Conflict.Algorithm` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Simple, fast algorithm ｜ `2` Bound propagation ｜ `3` Presolve ｜ `4` Irreducibly inconsistent set (IIS) for continuous models ｜ `5` Limited solve ｜ `6` Full solve |
| **`ConflictDisplay`**<br>conflict refiner 的 log 詳細度 | `int?` | `Param.Conflict.Display` | `0` No display ｜ `1` Summary display **(預設)** ｜ `2` Detailed display |
| **`DataCheck`**<br>輸入資料一致性檢查與建模建議的層級 | `int?` | `Param.Read.DataCheck` | — |
| **`FileEncoding`**<br>讀寫檔案的編碼 | `string` | `Param.Read.FileEncoding` | valid string for the name of an encoding (code page); default : ISO-8859-1 or the empty string (“ “) |
| **`IntSolFilePrefix`**<br>每找到一個整數解就存檔的檔名前綴 | `string` | `Param.Output.IntSolFilePrefix` | valid string for the prefix of a file name; default : ” “ (the empty string; that is, the switch is off) |
| **`MipDisplay`**<br>MIP 節點 log 的詳細度 | `int?` | `Param.MIP.Display` | `0` No display until optimal solution has been found ｜ `1` Display integer feasible solutions ｜ `2` Display integer feasible solutions plus an entry at a frequency set by MIP node log interval  **(預設)** ｜ `3` Display the number of cuts added since previous display; information about the processing of each successful MIP start; elapsed time in seconds and elapsed time in deterministic ticks for integer feasible solutions ｜ `4` Display information available from previous options and information about the LP subproblem at root ｜ `5` Display information available from previous options and information about the LP subproblems at root and at nodes |
| **`MipInterval`**<br>每幾個節點印一行 log | `long?` | `Param.MIP.Interval` | `n < 0` Display new incumbents, and display a log line frequently at the beginning of solving and less frequently as solving progresses ｜ `0 (zero)` automatic: let CPLEX decide the frequency to log nodes ( default ) ｜ `n > 0` Display new incumbents, and display a log line every n nodes |
| **`MpsLongNumerics`**<br>MPS / REW 輸出的數值精度 | `bool?` | `Param.Output.MPSLong` | `0` Off: use limited MPS precision ｜ `1` On: use full-precision **(預設)** |
| **`MultiObjectiveDisplay`**<br>多目標求解的 log 詳細度 | `int?` | `Param.MultiObjective.Display` | `0` No display ｜ `1` Summary display after each subproblem default ｜ `2` Summary display after each subproblem, as well as subproblem logs |
| **`NetworkDisplay`**<br>network simplex log 詳細度 | `int?` | `Param.Network.Display` | `0` No display ｜ `1` Display true objective values ｜ `2` Display penalized objective values **(預設)** |
| **`NonzeroRead`** 🕸️<br>讀檔時的非零元素數量上限 | `long?` | `Param.Read.Nonzeros` | Any integer from 0 to CPX_BIGINT or CPX_BIGLONG, depending on integer type; default : 250 000. |
| **`ParamDisplay`**<br>求解前是否印出被改過的參數清單 | `bool?` | `Param.ParamDisplay` | `0`  ｜ `1` default : Display parameters with changed values before optimization |
| **`PopulateLimit`** ⚠️<br>populate 一次最多產生幾個解 | `int?` | `Param.MIP.Limits.Populate` | Any nonnegative integer; default: 20. |
| **`ProbeDetTimeLimit`**<br>probing 花的 ticks 上限 | `double?` | `Param.MIP.Limits.ProbeDetTime` | Any nonnegative number; default : 1e+75. |
| **`ProbeTimeLimit`**<br>probing 花的時間上限（秒） | `double?` | `Param.MIP.Limits.ProbeTime` | Any nonnegative number; default : 1e+75. |
| **`QpNonzeroRead`** 🕸️<br>讀檔時 Q 矩陣的非零元素上限 | `long?` | `Param.Read.QPNonzeros` | Any integer from 0 (zero) to CPX_BIGINT or CPX_BIGLONG , depending on the type of integer; default : 5 000. |
| **`ReadWarningLimit`**<br>同一類讀檔警告最多印幾次 | `long?` | `Param.Read.WarningLimit` | `n>=0` Limits number of warnings of each type displayed **(預設)** is 10 |
| **`Record`**<br>錄下這次呼叫序列供 IBM 重現問題（診斷用） | `bool?` | `Param.Record` | `0` Recording is off by default ｜ `1` Turn on recording |
| **`RowRead`** 🕸️<br>讀檔時的限制式數量上限 | `int?` | `Param.Read.Constraints` | Any integer from 0 (zero) to CPX_BIGINT ; default : 30 000. |
| **`SiftingDisplay`**<br>sifting log 詳細度 | `int?` | `Param.Sifting.Display` | `0` No display of sifting information ｜ `1` Display major iterations **(預設)** ｜ `2` Display LP subproblem information within each sifting iteration |
| **`SimplexDisplay`**<br>simplex 迭代 log 詳細度 | `int?` | `Param.Simplex.Display` | `0` No iteration messages until solution ｜ `1` Iteration information after each refactoring **(預設)** ｜ `2` Iteration information for each iteration |
| **`SolutionPoolAbsGap`**<br>進 pool 的絕對品質門檻 | `double?` | `Param.MIP.Pool.AbsGap` | Any nonnegative real number; default : 1.0e+75. |
| **`SolutionPoolCapacity`**<br>solution pool 最多保留幾個解 | `int?` | `Param.MIP.Pool.Capacity` | Any nonnegative integer; 0 (zero) turns off all features of the solution pool; default : 2100000000. |
| **`SolutionPoolIntensity`**<br>找額外解的積極程度 | `int?` | `Param.MIP.Pool.Intensity` | `0` Automatic: let CPLEX choose **(預設)** ｜ `1` Mild: generate few solutions quickly ｜ `2` Moderate: generate a larger number of solutions ｜ `3` Aggressive: generate many solutions and expect performance penalty ｜ `4` Very aggressive: enumerate all practical solutions |
| **`SolutionPoolRelGap`**<br>進 pool 的相對品質門檻 | `double?` | `Param.MIP.Pool.RelGap` | Any nonnegative real number; default : 1.0e+75. |
| **`SolutionPoolReplace`**<br>pool 滿了要替換掉哪一個 | `int?` | `Param.MIP.Pool.Replace` | `0` Replace the first solution (oldest) by the most recent solution; first in, first out **(預設)** ｜ `1` Replace the solution which has the worst objective ｜ `2` Replace solutions in order to build a set of diverse solutions |
| **`TuningDetTimeLimit`**<br>內建 tune 的 ticks 上限 | `double?` | `Param.Tune.DetTimeLimit` | — |
| **`TuningDisplay`**<br>內建 tune 的 log 詳細度 | `int?` | `Param.Tune.Display` | `0` Turn off display ｜ `1` Display standard, minimal reporting **(預設)** ｜ `2` Display standard report plus parameter settings being tried ｜ `3` Display exhaustive report and log |
| **`TuningMeasure`** ⚠️<br>CPLEX 內建 tune 的評分方式 | `int?` | `Param.Tune.Measure` | `CPX_TUNE_AVERAGE` mean time **(預設)** ｜ `CPX_TUNE_MINMAX` minmax time |
| **`TuningRepeat`** ⚠️<br>內建 tune 對模型做幾次 permutation 重測 | `int?` | `Param.Tune.Repeat` | Any nonnegative integer; default : 1 (one) |
| **`TuningTimeLimit`**<br>內建 tune 的時間上限（秒） | `double?` | `Param.Tune.TimeLimit` | — |
| **`WriteLevel`**<br>寫 MST / SOL 檔時要包含哪些變數 | `int?` | `Param.Output.WriteLevel` | `0` Automatic: let CPLEX decide ｜ `1` CPLEX writes all variables and their values ｜ `2` CPLEX writes only discrete variables and their values ｜ `3` CPLEX writes only nonzero variables and their values ｜ `4` CPLEX writes only nonzero discrete variables and their values |
