# Cap6000Spec — TuningHistory

> Phase 3 決策紀錄（solver-tuning-guide §6）。模型是 `Data/cap6000.mps.gz`（MIPLIB cap6000），以 `read-model` 讀入，沒有 Phase 1/2。
> 契約區塊整期固定 → S1 / R0 / S2.5 → 每輪一節，只追加，NEVER 改寫歷史節。
> 每輪的目標、依據、假設、預測都在跑之前寫；TUNING-FACTS 用 `Import-Csv` 從 `Experiments/Cap6000Spec-trial.csv` 逐欄抄出，NEVER 手填數字。
> 指令一律 `dotnet run -- read-model cap6000.mps.gz [exp]`（Debug，bin 暫存在 `bin/Debug/net8.0/`）；exp 跑哪個區塊由 `Program.cs` exp 分支的 `currentExperiment` 決定。

## 契約區塊（整期固定，變更即重跑 §2.3 與 §3.0）

### 模型凍結

| 項目 | 值 | 備註 |
| --- | --- | --- |
| 模型檔 | `Data/cap6000.mps.gz`，sha256 `b70aa3265a2a258e4738fdcec71a03658d614ce779d6aea564479de376ae388f`，188164 bytes | 使用者提供的 `D:\Downloads\cap6000.mps.gz`（MIPLIB 3 cap6000）；csproj 複製到輸出的 `Model/`。沒有自動指紋檢查，收尾時重算 sha256 比對 |
| 模型名 | `cap6000.mps` | `OptModel.ReadModel(modelFile)` 取檔名去掉最後一層副檔名 |
| 模型結構 | BP，6000 binary、2176 限制式 | 取自 `-meta.csv` 的 `model.cap6000.mps.*` |
| 正確性證據 | MIPLIB 公告最佳值 −2451377；本專案 `MipGap = 0` 探針解出 Optimal −2451377（R0 產出 C）；production 解 −2451346 在 `MipGap` 容差內 | 沒有 Model.md / `ValidateRules`（read-model 沒有資料可驗）→ 用 §0.1.1 不變式當唯一自動驗證 |

### 停止契約

| 項目 | 值 | 備註 |
| --- | --- | --- |
| `MipGap` | 1e-4 | 使用者 2026-10-03 定案（= CPLEX 預設） |
| `TimeLimit` | 300 秒 | 使用者 2026-10-03 定案 |

### 環境契約（§2.3 sizing 定版）

| 項目 | 值 | 備註 |
| --- | --- | --- |
| `Threads` | 8 | 沒有 Phase 2 明設值 → 實掃 S1；候選 12 / 11 / 10 都沒有 3 個 seed 全贏 → 維持 8 |
| `ParallelMode` | 1 | 決定論 |
| `MemoryLimitMb` / `NodeFileStrategy` | 未設 | CPLEX 預設 |
| 機器 | VICLAI，i5-12500H（12 核 / 16 執行緒），23.7 GB RAM | CPLEX 22.1.1；DLL 取自 `AI-Modeling/dlls`（VERSION.txt 2026-10-02 22:56） |

### 量測契約

| 項目 | 值 |
| --- | --- |
| 進場情境（§0.0.1） | **A `Optimal`**（主指標 runtime） |
| `ModelType`（§0.0.2） | `BP`（主表 `Experiment = solve` 列）；`-meta.csv` binaryVarCount 6000、integer / semiContinuous / semiInteger / sos 皆 0，一致 |
| 比大小規則（§4.2、§4.4） | 逐 seed 跟 baseline 比；勝出 = `Losses = 0` 且 `Wins ≥ 3`；hold-out 通過 = `Losses = 0` |
| seeds | tuning 11, 22, 33, 44, 55；holdout 66, 77, 88 |
| warm-up / 順序輪替 | R1 起每輪第一個 config 是 `r<N>-warmup-exclude`（不計入）；variant 順序依 seed 輪替（§3.2） |
| export（experiment 期間） | 全關（實驗預設 `ProjectConfig.Quiet()`） |
| 解正確性驗證 | 無 `ValidateRules` → 每輪查 §0.1.1 情境 A 不變式：objective 在 phase2Objective ± 245.13 內、BestBound 不高於已知最佳 incumbent |
| dynamic search | 每輪 log `MIP search method: dynamic search.` 次數 = 本輪 trial 數（含 warm-up）、`traditional branch-and-cut` 0 次 |
| archive | 每次 exp 後把 `bin/Debug/net8.0/Experiment/` 的四個累積檔整檔複製到 `Experiments/`；複製前確認 bin 檔以 archive 現有內容開頭 |
| 總預算（§7.2） | 單次求解取 R0 最長 188 ms；(sizing 13 + R0 5 + 6 輪 × (2 config × 5 + warm-up 1) + hold-out 2 × 3) × 0.188 s ≈ 17 s 求解時間；上限 300 s 累計求解時間（條件 F） |

### Phase 2 結果基線（§0.1.1；S0 production 結果）

| 項目 | 值 |
| --- | --- |
| phase2Status | Optimal |
| phase2Objective | −2451346 |
| phase2Bound | −2451470.55383104 |
| phase2Gap | 5.081038378096274E-05 |
| verifiedOn | production（`Experiments/Cap6000Spec-trial.csv`，Experiment = solve，RunId 20261003-092951，SolveTimeMs 172.0000000204891，NodeCount 0） |
| 目標式方向 | Minimize |
| 不變式容差 | `MipGap` × \|phase2Objective\| = 1e-4 × 2451346 = 245.13 |

### 進場 findings（回報使用者，不自行處理）

1. **沒有 Phase 1/2**：外部 MIPLIB 模型檔，本專案只有 `read-model` 一種模型來源。`Program.cs` 照 Template 的四段、兩軸寫法；import-data 段沒有事可做，沒有八資料夾、`Dataload`、`ValidateRules`。
2. **規範之間的不一致（依權威順序處理，記錄於此）**：
   - Template / api-guide §5.1 把 read-model 的實驗命名成 `tuning-r0-{model.Name}`，但 tuning guide §3.3 與 checklist A 要求 `tuning-r<N>`。依 guide §0 的權威順序（tuning guide > api-guide > 專案 code）採 `tuning-r<N>`；本專案沒有 canonical 模型，不會撞名。
   - tuning guide §3.6 的 Interactive Optimizer 腳本寫 `tune`，CPLEX 22.1.1 回 `Command 'tune' does not exist`；實際指令是 `tools tune`。
   - 規範要求保留每輪區塊、當次只執行目前那輪（§3.3.1），但沒有規定怎麼選；本專案在 exp 分支用 `currentExperiment` 字串選區塊。
3. **初始 `Threads = 8` 沒有 sizing 依據**（取自 Template 預設）→ 依 §2.3 實掃，結果維持 8。
4. **量測解析度**：R0 五個 seed 在 CPLEX log 的確定性時間都是 103.07–103.16 ticks，主表 `SolveTimeMs` 卻在 140.99999982863665–188.0000000819564 ms 之間，而且都落在約 15.6 ms 的倍數附近。框架比 `SolveTimeMs` 沒有容差：S1 的 `s1-threads10-s33` 109.00000017136335 對 baseline 108.99999993853271 被判 lose——同一個計時 tick，只差浮點誤差。
5. **前一版**：`Projects/Cap6000/` 是同一題、同契約的前一次調校，用 ModelTuner runner（`switch (mode)` + `Tuning/`），程式形狀不合 AI-Modeling 規範。本期不把它的實驗當成本專案的 archive，只在決策依據中當參考。

---

## S1 — 2026-10-03（Threads sizing，不計入輪次）

- 指令：`currentExperiment = "sizing"`，`dotnet run -- read-model cap6000.mps.gz exp`；experiment：`sizing`；RunId：`20261003-093001`
- 設定快照：`Program.cs` 的 `// S1 — Cap6000Spec-sizing` 區塊（已 materialize）
- archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）
- 設計：`ParallelMode = 1` 固定，只掃 `Threads`；候選 = 實體核心 12 / 11 / 10，對照現行 8（`s1-threads8-baseline`），seed 11 / 22 / 33，第一個 trial 是 warm-up，順序輪替

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000.mps | s1-warmup-exclude | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | n/a | none |
| cap6000.mps | s1-threads8-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.99999982863665 | baseline | baseline |
| cap6000.mps | s1-threads12 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.99999982863665 | tie | Threads=12 |
| cap6000.mps | s1-threads11 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 139.99999989755452 | win | Threads=11 |
| cap6000.mps | s1-threads10 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | lose | Threads=10 |
| cap6000.mps | s1-threads12 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 141.0000000614673 | lose | Threads=12 |
| cap6000.mps | s1-threads11 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | tie | Threads=11 |
| cap6000.mps | s1-threads10 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.99999982863665 | lose | Threads=10 |
| cap6000.mps | s1-threads8-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000.mps | s1-threads11 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | lose | Threads=11 |
| cap6000.mps | s1-threads10 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 109.00000017136335 | lose | Threads=10 |
| cap6000.mps | s1-threads8-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 108.99999993853271 | baseline | none |
| cap6000.mps | s1-threads12 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | lose | Threads=12 |

- 勝負（`Experiments/Cap6000Spec-summary.csv`，Experiment = sizing，RunId = 20261003-093001）：`s1-threads12` Wins 0 / Losses 2 / Ties 1；`s1-threads11` Wins 1 / Losses 1 / Ties 1；`s1-threads10` Wins 0 / Losses 3 / Ties 0；NotCompared 皆 0
- 判定（§2.3）：沒有候選 3 個 seed 全贏 → **維持 `Threads = 8`，凍結**。13 個 trial 全部 NodeCount 0（root 就解完），threads 數對這題沒有平行空間；輸贏都在一個計時 tick 之內。

---

## R0 — 2026-10-03（校準，不計入輪次）

- 指令：`currentExperiment = "tuning-r0"`，`dotnet run -- read-model cap6000.mps.gz exp`；experiment：`tuning-r0`；RunId：`20261003-093037`
- exp 設定快照：`Program.cs` 的 `// R0 — Cap6000Spec-tuning-r0` 區塊（§8.4 形狀，已 materialize）
- archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）

<!-- TUNING-FACTS:R0:BEGIN -->
- experiment：`tuning-r0`；RunId：`20261003-093037`
- archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000.mps | r0-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | baseline |
| cap6000.mps | r0-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | none |
| cap6000.mps | r0-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 140.99999982863665 | baseline | none |
| cap6000.mps | r0-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 188.0000000819564 | baseline | none |
| cap6000.mps | r0-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | none |
<!-- TUNING-FACTS:R0:END -->

### 產出 A · baseline 對照組與情境確認

5 個 seed 全部 `Optimal`，`SolveTimeMs` 140.99999982863665–188.0000000819564 ms，遠低於 `TimeLimit` 300 s → **情境 A 確認**，之後逐 seed 比大小會比到 runtime。seed 33 停在 −2451274（gap 8.0e-5），其餘停在 −2451346（gap 5.1e-5），都在 `MipGap` 1e-4 內。

### 產出 B · 瓶頸剖面（§2.1）

主表同一批（`Experiments/Cap6000Spec-trial.csv`，tuning-r0，RunId 20261003-093037）：

| seed | FirstSolutionMs | SolveTimeMs | FirstSolutionMs ÷ SolveTimeMs | BoundChange | LastBoundChangeMs | NodeCount |
| --- | --- | --- | --- | --- | --- | --- |
| 11 | 94.0000000409782 | 172.0000000204891 | 0.55 | 66.77119300328195 | 125 | 0 |
| 22 | 94.0000000409782 | 172.0000000204891 | 0.55 | 66.77119300374761 | 125 | 0 |
| 33 | 77.9999999795109 | 140.99999982863665 | 0.55 | 66.77119300374761 | 125 | 0 |
| 44 | 110.00000010244548 | 188.0000000819564 | 0.59 | 66.77119300328195 | 156.00000019185245 | 0 |
| 55 | 94.0000000409782 | 172.0000000204891 | 0.55 | 66.77119300374761 | 125 | 0 |

- 首解佔比 0.55–0.59（大），`BoundChange` 66.8 相對 |objective| 2.45M 可忽略 → **剖面 = Primal-search**。
- CPLEX log（`bin/Debug/net8.0/Log/Cap6000Spec-tuning-r0_exp_2026-10-03_09-30-37.txt`）：每個 seed 都是 root 解完（NodeCount 0），確定性時間 103.11 / 103.10 / 103.16 / 103.07 / 103.11 ticks。root LP bound −2451537.3，一回 cut（MIR 2、Gomory 1）後到 −2451470.55 就不再動；gap 是靠 root 啟發式把 incumbent 推到 −2451346 才收進 1e-4。
- 候選（§2.2 Primal-search 列）：`Emphasis = 1` → `RinsHeuristicFrequency` → `HeuristicEffort` → `NodeSelect = 0` → `DiveType` → `Emphasis = 4`。`NodeSelect` / `DiveType` 只作用在 B&B 節點，這題 NodeCount = 0，**條件不成立，不排輪次**（記為不適用，不是已否證）。

### 產出 C · 契約健檢探針（§3.0）

- 指令：`currentExperiment = "probe-mipgap0"`；experiment：`probe-mipgap0`；RunId：`20261003-093059`；不是 variant，不進排名；`TimeLimit` 未放大
- 結果（主表）：`probe-mipgap0-s11` Optimal，objective −2451377、bestBound −2451377、gap 0、SolveTimeMs 313.0000000819564、NodeCount 2039
- **真最佳值 = −2451377**（與 MIPLIB 公告一致）
- 現行契約的品質損失 =（obj_contract − obj_true）/ obj_true，用同 seed 11 的 R0 結果：(−2451346 − (−2451377)) / (−2451377) = −1.26e-5，交付解比真最佳差 31；seed 33 的交付解 −2451274 差 103
- 取得真最佳的額外時間：seed 11 由 172.0000000204891 ms（R0）→ 313.0000000819564 ms（探針），NodeCount 0 → 2039
- **finding（回報使用者，不變更契約）**：`MipGap = 1e-4` 讓 CPLEX 停在 root 交付次佳解；多約 0.14 秒就能拿到真最佳。要不要改契約是使用者的品質決策。

### R0 出口 gate

| 檢查 | 結果 |
| --- | --- |
| 情境已確認 | PASS：A（5/5 Optimal） |
| 剖面已分類 | PASS：Primal-search（root 就解完） |
| 結果不變式（情境 A） | PASS：5 個 objective（−2451346 ×4、−2451274）都在 −2451346 ± 245.13 內；BestBound −2451470.55383104 低於最佳 incumbent −2451346，未越線 |
| dynamic search | PASS：log `Cap6000Spec-tuning-r0_exp_2026-10-03_09-30-37.txt` dynamic search = 5（= trial 數），traditional branch-and-cut = 0 |
| 契約與環境已定版 | PASS：本檔契約區塊 |

---

## S2.5 — 2026-10-03（CPLEX 內建 tune）

- 工具：CPLEX 22.1.1 Interactive Optimizer（`C:\IBM\ILOG\CPLEX_Studio2211\cplex\bin\x64_win64\cplex.exe`），模型用 S0 production 匯出的 `bin/Debug/net8.0/Model/Cap6000Spec_LP_2026-10-03_09-29-51.lp`
- 指令：`read <lp>`、`set threads 8`、`set parallel 1`、`set randomseed 11`、`set timelimit 300`、`set mip tolerances mipgap 1e-4`、`set tune repeat 3`、`tools tune`、`display settings changed`（guide 寫的 `tune` 在 22.1.1 不存在，見進場 finding 2）
- 結果：Tuning Complete，3.23 秒；以確定性時間量測，`Default test: Deterministic time = 332.43 ticks`、**`Best test: 'defaults'`**，`Fixed and tuned parameter settings` 沒有列出任何參數
- 試過的設定（原檔 + 2 份 permutation 加總）：`no_cuts`、`no_Gomory_cuts`、`node_lp_solve`、`no_heuristic`；其中 `no_heuristic` 在原檔上撞確定性時限且沒找到整數解，`no_Gomory_cuts` 三份分別 103.61 / 129.32 / 106.97 ticks（defaults 102.13 / 114.40 / 115.90）
- 處置：**沒有建議 → 不產生 variant**。

---

## R1 — 2026-10-03

**本輪目標（跑之前）**：情境 A。`r1-Emphasis=1` 在 5 個 tuning seed 上都不比 baseline 慢、至少快 3 個（R0 baseline 各 seed 140.99999982863665–188.0000000819564 ms），objective 維持在 −2451346 ± 245.13 內、BestBound 不越線。
**決策依據（跑之前）**：
  - 歷史：已讀 S1、R0、S2.5；本專案尚無已否證方向；不適用：`NodeSelect` / `DiveType`（NodeCount = 0）；S2.5 沒有建議
  - experiment 摘要：`tuning-r0`（RunId 20261003-093037）5/5 Optimal、NodeCount 0、IterationCount 132；現行 baseline = `MipGap 1e-4 / TimeLimit 300 / Threads 8 / ParallelMode 1`
  - 收斂軌跡：R0 剖面 Primal-search；bound 一回 cut 後就停在 −2451470.55，gap 由 incumbent 側收尾
  - 參考（非本專案 archive）：`Projects/Cap6000/` 前一版 R2 測過同一設定，CPLEX log ticks 逐 seed 與 baseline 相同
**假設**：gap 的收尾取決於 root 啟發式何時找到 −2451346。`Emphasis = 1`（FEASIBILITY）讓 CPLEX 更積極找可行解、少花心力在 bound 上；root LP bound −2451537.3 本身就讓 −2451346 的 gap 約 7.8e-5 < 1e-4，bound 變弱也不會逼出分支，若啟發式更早找到收尾解就會更快。
**預測**：不勝出（`Losses ≥ 1`）。emphasis 主要調整 B&B 節點上的排程，這題沒有節點；預期 CPLEX log 的確定性時間逐 seed 與 baseline 相同（前一版的觀察會重現），主表勝負只剩計時雜訊。若 ticks 不同，代表 emphasis 確實改變了 root 行為，要回頭看是變多還是變少。←跑之前寫死
**每個設定的理由（跑之前）**：

| config label | 唯一改動 | 證據 → 瓶頸 → 候選 → 預期效果 |
| --- | --- | --- |
| r1-warmup-exclude | 無（暖機，不計入） | §3.4 cold-start |
| r1-baseline | 無（對照組） | — |
| r1-Emphasis=1 | `Emphasis` 0 → 1 | R0 首解佔比 0.55–0.59、bound 一回 cut 即停 → Primal-search → §2.2 該列第一顆 `Emphasis = 1` → 更早找到收尾 incumbent → runtime 贏 |

**實測**：
  - 指令：`currentExperiment = "tuning-r1"`，`dotnet run -- read-model cap6000.mps.gz exp`；experiment：`tuning-r1`；RunId：`20261003-093354`
  - exp 設定快照：`Program.cs` 的 `// R1 — Cap6000Spec-tuning-r1` 區塊；archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）
  - seeds：11, 22, 33, 44, 55；勝負抄自 `Experiments/Cap6000Spec-summary.csv`（Experiment = tuning-r1，RunId = 20261003-093354）

<!-- TUNING-FACTS:R1:BEGIN -->
- experiment：`tuning-r1`；RunId：`20261003-093354`
- archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000.mps | r1-warmup-exclude | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 203.00000021234155 | n/a | none |
| cap6000.mps | r1-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | baseline |
| cap6000.mps | r1-Emphasis=1 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | win | Emphasis=1 |
| cap6000.mps | r1-Emphasis=1 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | win | Emphasis=1 |
| cap6000.mps | r1-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | none |
| cap6000.mps | r1-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | baseline | none |
| cap6000.mps | r1-Emphasis=1 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 109.00000017136335 | win | Emphasis=1 |
| cap6000.mps | r1-Emphasis=1 | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | win | Emphasis=1 |
| cap6000.mps | r1-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.99999982863665 | baseline | none |
| cap6000.mps | r1-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000.mps | r1-Emphasis=1 | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | tie | Emphasis=1 |
<!-- TUNING-FACTS:R1:END -->

**分析報告**：

- 剖面 / 候選對應：Primal-search → §2.2 該列第一顆 `Emphasis = 1`。
- eligibility（§4.1）：11 個 trial 全部 Optimal；objective（−2451346、−2451274）都在 −2451346 ± 245.13 內，BestBound −2451470.55383104 未越過最佳 incumbent −2451346；log `Cap6000Spec-tuning-r1_exp_2026-10-03_09-33-54.txt` dynamic search = 11（= trial 數，含 warm-up），traditional 0。**全數通過，沒有淘汰。**
- 勝負（`-summary.csv`，Experiment = tuning-r1，RunId = 20261003-093354，Config = r1-Emphasis=1）：Wins 4 / Losses 0 / Ties 1 / NotCompared 0 → **符合勝出條件（0 輸、≥ 3 贏）**。
- 有效設定：baseline → variant 唯一差異 `Emphasis: null → 1`（主表 `ConfigChanges` = `Emphasis=1`）。

1. **整體實驗收斂與結果**：1 個 variant × 5 seed，全數通過 eligibility，框架判 4 贏 0 輸 1 平，依 §4.4 是本輪勝出者，預測「不勝出」被框架結果推翻。但 facts 的 objective、bestBound、gap 逐 seed 與 baseline 完全相同，贏的只有 `solveTimeMs`。
2. **收斂軌跡解讀**：主表同一批的 `BoundChange` 逐 seed 與 baseline 相同（66.77119300328195 / 66.77119300374761）、`NodeCount` 0、`IterationCount` 132 兩邊一樣；CPLEX log 的確定性時間逐 seed 完全相同（seed 11 / 22 / 33 / 44 / 55 兩邊都是 103.11 / 103.10 / 103.16 / 103.07 / 103.11 ticks）。也就是 `Emphasis = 1` 沒有改變 CPLEX 的任何計算，預測裡「ticks 相同」的那一半被支持。
3. **分析者見解與下一步**：事實是「框架判勝出」與「兩邊做的是逐 tick 相同的計算」同時成立。推論：這 4 個 win 是牆鐘計時雜訊（每個 win 都在 1–2 個 15.6 ms tick 之內，例：seed 33 125 → 109.00000017136335），不是 `Emphasis = 1` 的效果；這正是 §3.4 說的「兩個設定其實一樣好時靠運氣過關」，機率約 3%，規範設計的第二道關卡是 hold-out。依 §4.5 職責分離與「勝負由框架判、NEVER 自己比」，**不以 ticks 推翻框架判定**，照流程進 §4.6 hold-out；ticks 證據記成 warning。另依 §7.1 B（本輪唯一 variant 的 bound 軌跡起訖與 baseline 一致）→「emphasis 與搜尋分支」群組剩下的 `Emphasis = 4`、`NodeSelect = 0`、`DiveType` 跳過；Primal-search 列還剩「啟發式」群組。

**裁決**：待 hold-out（框架判勝出：Wins 4、Losses 0）

### R1 hold-out（S4，§4.6）— 2026-10-03

**目的（跑之前）**：只估計、不選 config。champion `r1-Emphasis=1` 與 baseline 用 3 個從未參與調參的 holdout seed（66 / 77 / 88）重跑，軌跡關閉（`CaptureTrajectory(false)`，跟正式求解同一種跑法），第一個 trial 是 warm-up。通過 = `Losses = 0`；有任何一個 seed 輸 → over-tuning，retain。
**預測（跑之前）**：不通過（`Losses ≥ 1`）。R1 已顯示兩邊的計算逐 tick 相同，每個 seed 的勝負等於擲硬幣；3 個 seed 全部不輸只能靠運氣。若通過，依規範仍要 promotion，但 History 必須寫明這是雜訊下的 promotion。←跑之前寫死

**實測**：
  - 指令：`currentExperiment = "tuning-r1-holdout"`；experiment：`tuning-r1-holdout`；RunId：`20261003-093515`；軌跡關閉
  - 設定快照：`Program.cs` 的 `// R1 hold-out — Cap6000Spec-tuning-r1-holdout` 區塊；archive 同上四檔（篩 Experiment + RunId）
  - 勝負抄自 `Experiments/Cap6000Spec-summary.csv`（Experiment = tuning-r1-holdout，RunId = 20261003-093515）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000.mps | r1-warmup-exclude | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | n/a | none |
| cap6000.mps | r1-baseline | 66 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 141.0000000614673 | baseline | baseline |
| cap6000.mps | r1-Emphasis=1 | 66 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | lose | Emphasis=1 |
| cap6000.mps | r1-Emphasis=1 | 77 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | win | Emphasis=1 |
| cap6000.mps | r1-baseline | 77 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | none |
| cap6000.mps | r1-baseline | 88 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | baseline | none |
| cap6000.mps | r1-Emphasis=1 | 88 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 139.99999989755452 | win | Emphasis=1 |

- eligibility：7 個 trial 全部 Optimal，objective −2451346 在容差內、bound 未越線；log `Cap6000Spec-tuning-r1-holdout_exp_2026-10-03_09-35-15.txt` dynamic search = 7（= trial 數），traditional 0。
- 勝負：`r1-Emphasis=1` Wins 2 / Losses 1 / Ties 0 / NotCompared 0 → **hold-out 不通過**（`Losses ≠ 0`）。
- 解讀：預測「不通過」被支持。CPLEX log 的確定性時間在 holdout seed 上同樣逐 seed 相同（66：103.10 / 103.10、77：103.09 / 103.09、88：103.09 / 103.09 ticks），seed 66 的 lose（141.0000000614673 → 155.9999999590218）跟 R1 的 4 個 win 一樣，都只差一個計時 tick。hold-out 這道關卡擋下了 R1 的雜訊勝出。

**裁決**：retain（tuning seeds Wins 4 / Losses 0 符合勝出條件，但 hold-out Losses 1 → §4.6 over-tuning；計算逐 tick 與 baseline 相同，勝出為計時雜訊）
**已否證**（累積，後續輪次不重試）：

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `Emphasis = 1` | hold-out `-summary.csv` tuning-r1-holdout Wins 2 / Losses 1；ticks 在 8 個 seed 上都與 baseline 相同 | R1 |
| 「emphasis 與搜尋分支」群組（`Emphasis = 4`、`NodeSelect = 0`、`DiveType`） | §7.1 B：R1 bound 軌跡起訖與 baseline 一致；root 就解完、沒有節點 | R1 |

## R2 — 2026-10-03

**本輪目標（跑之前）**：情境 A。`r2-RinsHeuristicFrequency=10` 在 5 個 tuning seed 上都不比 baseline 慢、至少快 3 個（R1 baseline 各 seed 125–172.0000000204891 ms），objective 維持在 −2451346 ± 245.13 內、BestBound 不越線。
**決策依據（跑之前）**：
  - 歷史：已讀 S1、R0、S2.5、R1（含 hold-out）；已否證：`Emphasis = 1`、「emphasis 與搜尋分支」群組；baseline 未變（R1 retain）
  - experiment 摘要：`tuning-r1`（RunId 20261003-093354）baseline 5/5 Optimal、NodeCount 0；R1 顯示 root 路徑不受 emphasis 影響
  - 收斂軌跡：情境 A 沿用 R0 剖面 Primal-search；收尾 incumbent −2451346 由 root 啟發式找到
  - 參考（非本專案 archive）：`Projects/Cap6000/` 前一版 R3 測過同一設定，ticks 逐 seed 與 baseline 相同
**假設**：CPLEX 官方說明 `RINSHeur` 設正整數 N 時在 node 0、N、2N…呼叫 RINS，**node 0 就是 root**，所以這題也會在 root 多跑一次 RINS。RINS 以 root LP 解與現有 incumbent 一致的變數固定後解子 MIP，可能比預設的 root 啟發式更早找到 −2451346。值取 10：在 root 的效果與任何正值相同，萬一出現分支也不會每個節點都跑。
**預測**：不勝出（`Losses ≥ 1`）。baseline 在第一回 cut 後就找到收尾解，gap 很可能在 CPLEX 呼叫 node 0 的 RINS 之前就收斂；預期 ticks 逐 seed 與 baseline 相同，主表勝負只剩計時雜訊。←跑之前寫死
**每個設定的理由（跑之前）**：

| config label | 唯一改動 | 證據 → 瓶頸 → 候選 → 預期效果 |
| --- | --- | --- |
| r2-warmup-exclude | 無（暖機，不計入） | §3.4 cold-start |
| r2-baseline | 無（對照組） | — |
| r2-RinsHeuristicFrequency=10 | `RinsHeuristicFrequency` 0 → 10 | R0 首解佔比 0.55–0.59、gap 由 incumbent 側收尾 → Primal-search → §2.2 該列下一顆（emphasis 群組已否證）→ root 多一次 RINS 更早找到 −2451346 → runtime 贏 |

**實測**：
  - 指令：`currentExperiment = "tuning-r2"`，`dotnet run -- read-model cap6000.mps.gz exp`；experiment：`tuning-r2`；RunId：`20261003-093617`
  - exp 設定快照：`Program.cs` 的 `// R2 — Cap6000Spec-tuning-r2` 區塊（已 materialize）；archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）
  - seeds：11, 22, 33, 44, 55；勝負抄自 `Experiments/Cap6000Spec-summary.csv`（Experiment = tuning-r2，RunId = 20261003-093617）

<!-- TUNING-FACTS:R2:BEGIN -->
- experiment：`tuning-r2`；RunId：`20261003-093617`
- archive：`Experiments/Cap6000Spec-trial.csv`、`Experiments/Cap6000Spec-meta.csv`、`Experiments/Cap6000Spec-summary.csv`、`Experiments/Cap6000Spec-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000.mps | r2-warmup-exclude | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.99999989010394 | n/a | none |
| cap6000.mps | r2-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 186.9999999180436 | baseline | baseline |
| cap6000.mps | r2-RinsHeuristicFrequency=10 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | win | RinsHeuristicFrequency=10 |
| cap6000.mps | r2-RinsHeuristicFrequency=10 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | lose | RinsHeuristicFrequency=10 |
| cap6000.mps | r2-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | baseline | none |
| cap6000.mps | r2-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | baseline | none |
| cap6000.mps | r2-RinsHeuristicFrequency=10 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 110.00000010244548 | win | RinsHeuristicFrequency=10 |
| cap6000.mps | r2-RinsHeuristicFrequency=10 | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | lose | RinsHeuristicFrequency=10 |
| cap6000.mps | r2-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 109.00000017136335 | baseline | none |
| cap6000.mps | r2-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000.mps | r2-RinsHeuristicFrequency=10 | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | tie | RinsHeuristicFrequency=10 |
<!-- TUNING-FACTS:R2:END -->

**分析報告**：

- 剖面 / 候選對應：Primal-search → §2.2 該列的 `RinsHeuristicFrequency`（emphasis 群組已於 R1 否證）。
- eligibility（§4.1）：11 個 trial 全部 Optimal；objective 在 −2451346 ± 245.13 內、BestBound 未越線；log `Cap6000Spec-tuning-r2_exp_2026-10-03_09-36-17.txt` dynamic search = 11（= trial 數，含 warm-up），traditional 0。沒有淘汰。
- 勝負（`-summary.csv`，Experiment = tuning-r2，RunId = 20261003-093617，Config = r2-RinsHeuristicFrequency=10）：Wins 2 / Losses 2 / Ties 1 / NotCompared 0 → 不符勝出條件。
- 有效設定：baseline → variant 唯一差異 `RinsHeuristicFrequency: null → 10`（主表 `ConfigChanges` = `RinsHeuristicFrequency=10`）。

1. **整體實驗收斂與結果**：1 個 variant × 5 seed，全數通過 eligibility，2 贏 2 輸 1 平，不勝出，預測被支持。objective、bestBound、gap 逐 seed 與 baseline 完全相同。
2. **收斂軌跡解讀**：主表同一批的 `BoundChange` 逐 seed 與 baseline 相同、`NodeCount` 0、`IterationCount` 132；CPLEX log 的確定性時間逐 seed 相同（103.11 / 103.10 / 103.16 / 103.07 / 103.11 ticks，兩邊一樣），log 也沒有 RINS 的輸出。推論：gap 在 root 的 cut / 啟發式迴圈中就收進 1e-4，CPLEX 還沒輪到 node 0 的 RINS 就停了。
3. **分析者見解與下一步**：R1、R2 兩個 variant 都和 baseline 做了逐 tick 相同的計算；勝負（R1 4 贏、hold-out 1 輸、R2 2 贏 2 輸）全部是計時雜訊。依 §7.1 B（本輪唯一 variant 的 bound 軌跡起訖與 baseline 一致）→「啟發式」群組標記已否證，`HeuristicEffort` 跳過。Primal-search 列至此全部否證或不適用（`NodeSelect` / `DiveType`）→ **停止**。

**裁決**：retain（Wins 2、Losses 2，不符「0 輸且 ≥ 3 贏」）
**已否證**（累積，後續輪次不重試）：

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `Emphasis = 1` | hold-out `-summary.csv` tuning-r1-holdout Wins 2 / Losses 1；ticks 在 8 個 seed 上都與 baseline 相同 | R1 |
| 「emphasis 與搜尋分支」群組（`Emphasis = 4`、`NodeSelect = 0`、`DiveType`） | §7.1 B：R1 bound 軌跡起訖與 baseline 一致；root 就解完、沒有節點 | R1 |
| `RinsHeuristicFrequency = 10` | `-summary.csv` tuning-r2 Wins 2 / Losses 2；ticks 逐 seed 與 baseline 相同 | R2 |
| 「啟發式」群組（`HeuristicEffort` 等） | §7.1 B：R2 bound 軌跡起訖與 baseline 一致；gap 在 node 0 的 RINS 被呼叫前就已收斂 | R2 |

---

## 收尾 — 2026-10-03

**停止原因**：§7.1 **B**——Primal-search 剖面對應的「emphasis 與搜尋分支」（R1）、「啟發式」（R2）兩個群組都已整類否證，剩下的 `NodeSelect` / `DiveType` 因 NodeCount = 0 不適用。（C「候選耗盡」同時成立；D 未命中：R1 有 variant 符合 §4.4。）
**hold-out（S4）**：R1 `r1-Emphasis=1` 跑過，Losses 1 → 不通過。
**promotion（S5）**：無。**裁決 = retain**，`productionBaseline` 維持 initial（`MipGap 1e-4 / TimeLimit 300 / Threads 8 / ParallelMode 1 / Seed 11`）；production 行為未變，S0 的 production 結果（Optimal −2451346，RunId 20261003-092951）即為現行驗證結果。
**模型凍結複核**：收尾時重算 `Data/cap6000.mps.gz` sha256 = `b70aa3265a2a258e4738fdcec71a03658d614ce779d6aea564479de376ae388f`，與契約區塊一致。

**整期結論**：

1. cap6000 在 CPLEX 22.1.1 上 **root 就解完**：所有 trial NodeCount 0、確定性時間 103.07–103.16 ticks、主表 SolveTimeMs 109–203 ms（含 warm-up）；進場情境 A，剖面 Primal-search。
2. 2 輪 2 個 variant 都沒有改變 CPLEX 的計算（ticks 逐 seed 與 baseline 相同），CPLEX 內建 tune 以確定性時間量測也判定 defaults 最好。在現行停止契約下，**這題沒有搜尋策略能省的時間**：剩下的是 presolve、probing、root LP 與一回 cut / 啟發式。
3. R1 被框架判勝出（4 贏 0 輸）是計時雜訊造成的假陽性，由 hold-out 擋下。

**回報使用者的 findings（都不是本階段能自行決定的）**：

1. **契約**：`MipGap = 1e-4` 讓 CPLEX 停在次佳解（−2451346，比真最佳 −2451377 差 31；seed 33 停在 −2451274，差 103）。`MipGap = 0` 探針只多約 0.14 秒（313 ms、2039 nodes）就拿到真最佳。要改契約的話，需重跑 S1 / R0，那時題目才會進入分支、才有 tuning 空間。
2. **量測**：框架逐 seed 比 `SolveTimeMs` 沒有容差。這題同一份計算（ticks 相同）會被判出 4 贏 0 輸，連浮點誤差都會判輸贏（S1 `s1-threads10-s33`）。這種短解題要比得準，需要改用 CPLEX 確定性時間比大小（`Cplex.GetDetTime()`），屬於框架層變更。
3. **規範不一致**：read-model 實驗命名（Template `tuning-r0-{model.Name}` vs guide `tuning-r<N>`）、§3.6 的 `tune` 指令在 22.1.1 是 `tools tune`、「當次只跑目前那輪」沒有規定選法，詳見進場 findings 2。
