## §8 寫實驗 — 參數掃描

### 8.0 進場條件與範圍界線

**MUST 先過 §7 正確性 gate 才做 tuning。** 對錯的模型調參數只會更快得到錯答案。資料驗證（`OptData.Load` 自動跑）→ §7 四步解驗證 → 才輪到本節。

**Phase 3 只動一樣東西：`CplexConfig`。** 進場時模型與資料已凍結、已 feasible；`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 全程唯讀。落在下表哪一格決定要不要留在本節：

| 使用者要的                                       | 是不是 tuning | 動作                                                                          |
| ------------------------------------------------ | ------------- | ----------------------------------------------------------------------------- |
| 模型正確且有解，但 timeout / gap 收不下來 / 太慢 | ✅ 是         | 調 `CplexConfig`（§8.2 閉環）；連續 3 輪無實質改善就停止回報                  |
| 換參數值、換一批資料                             | ❌ 否         | 換 `Data/*.csv` 重跑即可，`.cs` 一行都不動，不需要本節                        |
| 加刪約束、改 Big-M、reformulation                | ❌ 否         | **回 Model.md 改，經確認後重走 §1–§7 轉譯**                                   |
| `Infeasible` / `Unbounded`                       | ❌ 否         | 前提破裂：`Infeasible` 讀 IIS 取證後退回；`Unbounded` 補漏掉的界限 constraint |

- Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，該輪實驗證據整批作廢。
- NEVER 用 soft constraint / penalty 讓 infeasible 模型「有解」—— 旋鈕不會把 infeasible 變 feasible，只會讓你更快確認它 infeasible。
- NEVER 以 tuning 名義移項 / 改號 / 翻方向 / 四捨五入；只改求解策略，不改數學意義。
- NEVER 沒有 `OptExperiment` 記錄就宣稱改善。

**常用 solver 決策**（欄位對照見附錄 B）：

| 症狀         | 動作                                                                                                                  |
| ------------ | --------------------------------------------------------------------------------------------------------------------- |
| timeout      | 提 `TimeLimit`、試 `Emphasis = 1`（重可行解）、實測篩 `Threads`                                                |
| gap 大       | 加切割（`GomoryCuts` / `MirCuts` / `CoverCuts`）→ `NodeSelect = 1`（best-bound）→ 收 `MipGap` / `Emphasis = 2`      |
| 可行解難找   | `NodeSelect = 0`（DFS）→ `IntegerSolutionLimit` / `NodeLimit` → 放寬 `MipGap` → `Emphasis = 1`（僅限模型本身有解、只是難找） |
| 記憶體爆     | `TreeMemoryLimitMb` + `NodeFileStrategy = 2/3`（**順序有雷**，見附錄 B 註記）                                                |
| 數值不穩     | `NumericalEmphasis = true`                                                                                            |
| 要可重現     | `ParallelMode = 1` + 固定 `Seed` + 用 `DeterministicTimeLimit` 取代牆鐘 `TimeLimit`                                       |
| `Infeasible` | **不是 tuning 題目**：讀 IIS（`bin/.../IISs/*.ilp`）取最小衝突集當證據，退回 Phase 1 / Phase 2 修模型或資料           |

### 8.1 實驗 runner

`OptExperiment` 將已定義的模型與具體 solver configs 展開成笛卡兒積，擷取 Trial，最後輸出 `Experiments/<name>.csv + .json`。同一份 `OptModel` 可先交給 `OptProject` 正式求解，再交給實驗 runner。

**同名實驗是 append，不是覆寫。** `Run()` 內的 `Save()` 會先讀既有 JSON，再把歷史 trials 合併回傳值的 `result.Trials`。因此 experiment 命名 MUST 用 `<Project>-tuning-r<N>`，每輪 N 加一 —— NEVER 重複用同一個名字 —— Why: 重跑同名 experiment 後直接 `foreach (result.Trials)` 會再次看到歷史資料，把舊 trial 誤報成本輪結果。

```csharp
var baseline = solverConfig.Clone();
var emphasis = baseline.Clone();
emphasis.Emphasis = 2;
var tighterGap = baseline.Clone();
tighterGap.MipGap = 0.01;
var threads4 = baseline.Clone();
threads4.Threads = 4;

var result = new OptExperiment("MyProject-tuning-r1", "一次只改一個 solver 旋鈕")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=optimal", emphasis)
    .AddConfig("r1-gap=0.01", tighterGap)
    .AddConfig("r1-threads=4", threads4)
    .Run();
```

| 規則                | 說明                                                                                |
| ------------------- | ----------------------------------------------------------------------------------- |
| 共用一份 data       | 所有 cell 引用同一份 `OptData.Load` 結果，載入後視為唯讀                            |
| experiment 預設安靜 | solver log、LP/MPS/Sol export 與 housekeeping 預設都 OFF                            |
| 具體 config         | 用 `Clone()` 建 variant，NEVER 用 tune delegate 突變共用 baseline                   |
| 笛卡兒積            | `.AddModel` × `.AddConfig` 自動展開；單一 cell 用 `.AddTrial(model, label, config)` |
| label               | 自動成為 `ModelName \| config-label`；輪次前綴（`r1-`）寫進 config label            |
| 命名                | experiment name 一律 `<Project>-tuning-r<N>`，每輪遞增                              |
| baseline 可重現     | 固定 `Threads` / `ParallelMode` / `Seed`，讓每次跑的停點一致              |
| `OnSolved` 邊界     | 只屬 `OptProject`；實驗不應大量寫 solution                                          |
| 一次只動一個旋鈕    | 同時改兩個就分不出是哪個造成差異                                                    |

### 8.2 AI 閉環：Trial → champion → production baseline

`OptExperiment` 本身只負責執行與保存證據，不會改傳入的 config，也不會替 production 選 winner。完成 tuning 的責任在 AI workflow：

```text
productionBaseline
  └─ Clone variants
       └─ OptExperiment.Run()
            └─ Trial.Config + Trial.Metrics
                 └─ AI promotion gate
                      └─ 更新 productionBaseline
                           └─ build + production + ValidateRules
```

每輪 MUST：

1. 使用新實驗名 `<Project>-tuning-r<N>`，只分析本輪 Trial；同名 append 的歷史結果不得混進本輪排名。
2. 先淘汰錯誤、Infeasible、Unbounded、無可行解或未達 objective / gap 品質門檻的 candidate。
3. 在相同 instance、seed 與資源限制下比較；正式 promotion 用 3–5 seeds 與 hold-out instances。第一個 solve 作 warm-up 並排除，variants 跨 seed 輪替或隨機化執行順序，避免固定讓 baseline 承擔 cold-start。runtime 用 shifted geometric mean，timeout 用 PAR10，改善幅度須大於 baseline variability。
4. 依序比較解的正確性與品質，再比較 runtime；node / iteration 只作診斷或 tie-break。NEVER 讓一個比較快但解較差的 trial 勝出。
5. 從 champion 的 `Trial.Config.Settings` 取得完整設定快照，由 AI 明確更新 `Program.cs` 的 `productionBaseline` initializer。同步更新 initializer 上方 provenance，至少寫來源 experiment + Trial label。NEVER 讓執行中的程式修改 source，NEVER 讓 prod 每次動態挑歷史上最快的一列。
6. 在專案根 `TuningHistory.md` 新增本輪紀錄：experiment、baseline / champion Trial、seeds / instances、彙總方法、Status / objective / gap、before/after config diff、決策與理由。該檔 MUST 納入 source control；`bin/Experiments/*.json` 會被清除，不能作唯一 provenance。
7. promotion 後 MUST `dotnet build` 並跑無參數 production；`OnSolved` / `ValidateRules`、Status、objective、gap 與輸出全數通過才完成升級，並把結果補回同一筆 history。失敗則保留原 baseline並記錄 rejected。
8. 成功 promotion 的 champion 成為下一輪 baseline；沒有可靠勝者也記錄 retain。連續 3 輪無實質改善就停止 tuning 並回報，是否回頭改模型結構由使用者決定。

若沒有 candidate 可靠勝過 baseline，正確結果是保留既有 production baseline，而不是硬挑本輪牆鐘時間最小者。完成報告與 `TuningHistory.md` MUST 寫明 baseline、champion（若有）、before / after、seeds / instances、promotion 或不 promotion 的理由。

**評分採 lexicographic gate**，NEVER 把不同品質的解只用 runtime 混排：

1. 正確且可接受的 Status / 解品質（錯誤、Infeasible、Unbounded、無可行解一律淘汰）。
2. 達成專案要求的 objective 與 MipGap。
3. 前兩者相同或都達標時，才比較穩健彙總後的 runtime；node / iteration 只作診斷與 tie-break。

若需要覆寫 experiment 的 project-level defaults，可使用 `.UseConfig(() => projectConfig)`；factory 每個 cell 都會執行。一般 solver 掃描保留預設即可。

Soft constraint 不屬 solver tuning。Phase 3 的模型是凍結的——使用者要放鬆限制式就是回 Phase 1 改 Model.md（§4.5），NEVER 在調參途中把 canonical hard model 換成 soft 版來「讓它有解」。

### 8.3 `TuningHistory.md`（專案根，MUST 納入 source control）

`bin/Experiments/*.json` 會隨 clean 消失，不能當唯一 provenance。每輪一節，至少包含：

```text
日期 / round / experiment name
baseline Trial ｜ champion 或 candidate Trial
instances / seeds / 彙總方法（shifted geometric mean、PAR10）
Status / objective / gap
before → after config diff
決策：promotion ｜ retain ｜ rejected + 理由
production 驗證：dotnet build 結果、ValidateRules 結果
```

這是決策索引；完整逐 Trial 數值仍由 experiment JSON 保存。即使本輪沒有勝者也要記 retain 與證據，避免下一輪重做同一組實驗。

---

## 附錄 B · `CplexConfig` 全旋鈕對照（§8 tuning 用）

✅ = 框架已提供且已接線；❌ = 框架尚未提供（要用得改框架本體並重編 Core + Cplex DLL，屬框架維護，不在本流程內）。欄位為 `null` 即採用 CPLEX 預設，tuning 時只設要動的那一個。

| 用途                                | CPLEX 參數                                        | `CplexConfig` 欄位                  | 取值 / 預設                                                                                        |
| ----------------------------------- | ------------------------------------------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------- |
| 執行緒數                            | `Param.Threads`                                   | ✅ `Threads`                    | 整數，預設 32；實測比較核心數 / -1 / -2                                                            |
| 平行模式                            | `Param.Parallel`                                  | ✅ `ParallelMode`                   | -1 機會式 / 0 自動 / 1 決定論                                                                      |
| 限制式讀取上限                      | `Param.Read.Constraints`                          | ✅ `RowRead`                        | 整數，預設 30000                                                                                   |
| 工作記憶體                          | `IntParam.WorkMem`                                | ✅ `MemoryLimitMb`                     | MB，預設 2048                                                                                      |
| 樹記憶體上限                        | `Param.MIP.Limits.TreeMemory`                     | ✅ `TreeMemoryLimitMb`                | MB                                                                                                 |
| 節點檔策略                          | `Param.MIP.Strategy.File`                         | ✅ `NodeFileStrategy`                    | 0 不存 / 1 記憶體壓縮（預設） / 2 磁碟 / 3 磁碟壓縮                                                |
| 節點選擇                            | `Param.MIP.Strategy.NodeSelect`                   | ✅ `NodeSelect`                     | 0 DFS / 1 best-bound（預設） / 2 best-estimate / 3 交替                                            |
| 分支變數選擇                        | `Param.MIP.Strategy.VariableSelect`               | ✅ `VariableSelect`                         | -1 min-infeas / 0 自動 / 1 max-infeas / 2 pseudo cost / 3 strong branching / 4 pseudo reduced cost |
| 分支方向                            | `Param.MIP.Strategy.Branch`                       | ✅ `BranchDirection`                      | -1 向下 / 0 自動 / 1 向上                                                                          |
| 潛降策略                            | `Param.MIP.Strategy.Dive`                         | ✅ `DiveType`                       | 0 自動 / 1 傳統 / 2 探測 / 3 引導                                                                  |
| 搜尋模式                            | `Param.MIP.Strategy.Search`                       | ✅ `MipSearch`                      | 0 自動 / 1 傳統 B&C / 2 動態 B&C                                                                   |
| 探測強度                            | `Param.MIP.Strategy.Probe`                        | ✅ `Probe`                          | -1..3                                                                                              |
| RINS 頻率                           | `Param.MIP.Strategy.RINSHeur`                     | ✅ `RinsHeuristicFrequency`                       | -1 關 / 0 自動 / N                                                                                 |
| 啟發式投入                          | `Param.MIP.Strategy.HeuristicEffort`              | ✅ `HeuristicEffort`                | 倍率                                                                                               |
| MIP emphasis                        | `Param.Emphasis.MIP`                              | ✅ `Emphasis`（= `Emphasis`）    | 0 平衡 / 1 重可行解 / 2 重最佳性 / 3 best bound / 4 hidden                                         |
| 數值穩定                            | `Param.Emphasis.Numerical`                        | ✅ `NumericalEmphasis`              | bool                                                                                               |
| Cut 數量倍數                        | `Param.MIP.Limits.CutsFactor`                     | ✅ `CutsFactor`                     | 倍率                                                                                               |
| Cut 回合數                          | `Param.MIP.Limits.CutPasses`                      | ✅ `CutPasses`                      | -1 / 0 / N                                                                                         |
| Gomory 切割                         | `Param.MIP.Cuts.Gomory`                           | ✅ `GomoryCuts`                     | -1 關 / 0 自動 / 1..3 漸積極                                                                       |
| 覆蓋切割                            | `Param.MIP.Cuts.Covers`                           | ✅ `CoverCuts`                      | -1 / 0 / 1..3                                                                                      |
| 團切割                              | `Param.MIP.Cuts.Cliques`                          | ✅ `CliqueCuts`                     | -1 / 0 / 1..3                                                                                      |
| MIR 切割                            | `Param.MIP.Cuts.MIRCut`                           | ✅ `MirCuts`                        | -1 / 0 / 1..3                                                                                      |
| Flow cover 切割                     | `Param.MIP.Cuts.FlowCovers`                       | ✅ `FlowCoverCuts`                  | -1 / 0 / 1..3                                                                                      |
| MIP gap（相對）                     | `Param.MIP.Tolerances.MIPGap`                     | ✅ `MipGap`（= `MipGap`）            | 預設 1e-4                                                                                          |
| MIP gap（絕對）                     | `Param.MIP.Tolerances.AbsMIPGap`                  | ✅ `AbsoluteMipGap`                         | 數值                                                                                               |
| 整數容差                            | `Param.MIP.Tolerances.Integrality`                | ✅ `IntegralityTolerance`                          | 數值                                                                                               |
| 最佳性容差                          | `Param.Simplex.Tolerances.Optimality`             | ✅ `OptimalityTol`（= `OptimalityTol`）     | 預設 1e-6                                                                                          |
| 可行性容差                          | `Param.Simplex.Tolerances.Feasibility`            | ✅ `FeasibilityTol`（= `FeasibilityTol`）    | 預設 1e-6                                                                                          |
| 時間限制（牆鐘）                    | `Param.TimeLimit`                                 | ✅ `TimeLimit`（= `TimeLimit`）     | 秒                                                                                                 |
| 決定論時間                          | `Param.DetTimeLimit`                              | ✅ `DeterministicTimeLimit`                   | ticks，可重現實驗首選                                                                              |
| 計時方式                            | `Param.ClockType`                                 | ✅ `ClockType`                      | 1 CPU / 2 wall                                                                                     |
| 節點上限                            | `Param.MIP.Limits.Nodes`                          | ✅ `NodeLimit`                      | 整數                                                                                               |
| 整數解上限                          | `Param.MIP.Limits.Solutions`                      | ✅ `IntegerSolutionLimit`                    | 找到 N 個整數解即停                                                                                |
| Solution polishing                  | `Param.MIP.PolishAfter.Time`                      | ✅ `PolishAfterTime`                | 秒                                                                                                 |
| 隨機種子                            | `Param.RandomSeed`                                | ✅ `Seed`（= `Seed`）         | 整數                                                                                               |
| 預處理                              | `Param.Preprocessing.Presolve`                    | ✅ `Presolve`                       | bool；一般保持開啟，只有 debug 才關                                                                |
| Root 演算法                         | `IntParam.RootAlgorithm`                          | ✅ `RootAlgorithm`（= `RootAlgorithm`） | 0 自動 / 1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent                      |
| 節點 LP 演算法                      | `IntParam.NodeAlg`                                | ✅ `NodeAlgorithm`                  | 0..6                                                                                               |
| Simplex 迭代上限                    | `Param.Simplex.Limits.Iterations`                 | ✅ `SimplexIterationLimit`               | 整數                                                                                               |
| Barrier 演算法                      | `Param.Barrier.Algorithm`                         | ✅ `BarrierAlgorithm`               | 0..3                                                                                               |
| ZeroHalf / Disjunctive 切割         | `Param.MIP.Cuts.ZeroHalfCut` / `.Disjunctive`     | ❌                                  | —                                                                                                  |
| 對稱性消除                          | `Param.Preprocessing.Symmetry`                    | ✅ `Symmetry`                       | −1 auto / 0 off / 1..5 逐步提高強度                                                                |
| 進階 presolve                       | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | ❌                                  | —                                                                                                  |
| 記憶體 emphasis                     | `Param.Emphasis.MemUsage`                         | ❌                                  | —                                                                                                  |
| 分支優先級                          | `Cplex.SetPriority` / order file                  | ❌                                  | 只能間接用 `VariableSelect` 影響                                                                           |
| MIP start（初始解注入）             | `Cplex.AddMIPStart` / `SetVectors`                | ❌                                  | 等效手段：`Emphasis = 1` + `RinsHeuristicFrequency` + `HeuristicEffort`                                       |
| 自動調參                            | `Cplex.TuneParam`                                 | ❌                                  | —                                                                                                  |
| Heuristic / Lazy / UserCut callback | 對應 callback                                     | ❌                                  | —                                                                                                  |

> ★ **`MemoryLimitMb` 與 `NodeFileStrategy` 的順序雷**：框架的 `Configuration()` 在設定 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`。要做「記憶體爆 → 溢寫節點檔」，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy = 2/3`，否則被覆蓋成 0。
>
> ★ **單一 PascalCase 設定面**：`config.Seed = 7`、`config.MipGap = 0.01` 等 public property 是唯一寫法；不再區分抽象旋鈕與 solver 專屬欄位，也不可混用兩套命名。
