# Cap6000 — TuningHistory

> Phase 3 決策紀錄（solver-tuning-guide §6），檔案模式：模型是 `Instances/tune/cap6000.mps.gz`（MIPLIB cap6000），沒有 Phase 1/2。
> 契約區塊整期固定 → S1 / R0 / S2.5 → 每輪一節，只追加，NEVER 改寫歷史節。
> 每輪的目標、依據、假設、預測都在跑之前寫；TUNING-FACTS 用 `Import-Csv` 從 `Experiments/Cap6000-trial.csv` 逐欄抄出，NEVER 手填數字。
> 所有指令用 `-c Release`：bin 暫存在 `bin/Release/net8.0/`。

## 契約區塊（整期固定，變更即重跑 §2.3 與 §3.0）

### 模型凍結契約（檔案模式，取代 Phase 2 的「model chain 唯讀」）

| 項目 | 值 | 備註 |
| --- | --- | --- |
| instances.lock | 2026-10-03 | `tune/cap6000.mps.gz` sha256 `b70aa3265a2a258e4738fdcec71a03658d614ce779d6aea564479de376ae388f`，188164 bytes；每次執行自動比對 |
| 模型檔來源 | 使用者提供 `D:\Downloads\cap6000.mps.gz`（MIPLIB 3 cap6000），2026-10-03 複製進 `Instances/tune/` | 純 BP：6000 binary、2176 限制式（取自 `-meta.csv` 的 `model.cap6000.*`） |
| 正確性證據 | MIPLIB 公告最佳值 −2451377；本專案 `MipGap = 0` 探針解出 Optimal −2451377（見 R0 產出 C），production 解 −2451346 在 `MipGap` 容差內 | 沒有 Model.md / ValidateRules；改用公告最佳值 + facts 的結果不變式 |
| tune instances | `tune/cap6000.mps.gz` | |
| holdout instances | 無 → 改用 holdout seeds 66, 77, 88 | NEVER 用來選 config |

### 停止契約

| 項目 | 值 | 備註 |
| --- | --- | --- |
| `MipGap` | 1e-4 | 使用者 2026-10-03 定案（= CPLEX 預設） |
| `TimeLimit` | 300 秒 | 使用者 2026-10-03 定案 |

### 環境契約（§2.3 sizing 定版）

| 項目 | 值 | 備註 |
| --- | --- | --- |
| `Threads` | 8 | 沒有 Phase 2 明設值 → 實掃 S1（見下方 S1 節）；候選 12 / 11 / 10 都沒有 3 個 seed 全贏 → 維持 8 |
| `ParallelMode` | 1 | 決定論 |
| `MemoryLimitMb` / `NodeFileStrategy` | 未設 | CPLEX 預設 |
| 機器 | VICLAI，i5-12500H（12 核 / 16 執行緒），23.7 GB RAM | CPLEX 22.1.1，DLL 取自 `AI-Modeling/dlls`（VERSION.txt 2026-10-02 22:56） |

### 量測契約

| 項目 | 值 |
| --- | --- |
| 進場情境（§0.0.1） | **A `Optimal`**（主指標 runtime） |
| `ModelType`（§0.0.2） | `BP`（主表 `Experiment = solve` 列；`-meta.csv` binaryVarCount 6000、integer / semiContinuous / semiInteger / sos 皆 0，一致） |
| 比大小規則（§4.2、§4.4） | 逐 seed 跟 baseline 比；勝出 = `Losses = 0` 且 `Wins ≥ 3`；hold-out 通過 = `Losses = 0` |
| seeds | tuning 11, 22, 33, 44, 55；holdout 66, 77, 88 |
| warm-up / 順序輪替 | runner 自動（`Tuning/TuningRound.cs`） |
| export（experiment 期間） | 全關（`ProjectConfig.Quiet()`） |
| 解正確性驗證 | 無 ValidateRules → 每輪看 facts 的結果不變式表（bound 越線 / Optimal 分散） |
| dynamic search | 每輪看 facts 的 log 掃描：`MIP search method: dynamic search.` 次數 = trial 數 + 1（warm-up），`traditional branch-and-cut` 0 次 |
| 總預算（§7.2） | 單次求解取 R0 最長 172 ms；(sizing 12 + R0 5 + 6 輪 × 2 config × 5 + hold-out 2 × 3) × 0.172 s ≈ 14 s 求解時間；上限訂 300 s 累計求解時間（條件 F） |

### Phase 2 結果基線（§0.1.1 不變式；S0 `dotnet run` production 結果）

| 項目 | 值 |
| --- | --- |
| phase2Status | Optimal |
| phase2Objective | −2451346 |
| phase2Bound | −2451470.55383104 |
| phase2Gap | 5.081038378096274E-05 |
| verifiedOn | production（`Experiments` 外的 bin 紀錄：`Cap6000-cap6000-trial.csv`，Experiment = solve，RunId 20261003-010402，SolveTimeMs 187.99999984912574，NodeCount 0） |
| 目標式方向 | Minimize（`[模型統計對帳]` 目標式 Minimize/Minimize） |
| 不變式容差 | `MipGap` × \|phase2Objective\| = 1e-4 × 2451346 = 245.13 |

### 進場 findings（回報使用者，不自行處理）

1. **沒有 Phase 1/2**：外部 MIPLIB 模型檔，以 read-model 宿主專案承接。runner 原樣複製自 `OptimFoundation/Templates/ModelTuner`；exp 形狀（`tuning-r0` / `r0-` label / marker / baseline × 5 seeds）由該 runner 提供，未重寫。
2. **初始 `Threads = 8` 沒有 sizing 依據**（取自 AI-Modeling Template 預設）→ 依 §2.3 實掃，結果維持 8。
3. **量測解析度**：R0 五個 seed 在 CPLEX log 的確定性時間都是 103.07–103.16 ticks（root 就解完、路徑幾乎相同），但主表 `SolveTimeMs` 落在 140–172 ms，且數值集中在 15.6 ms 的倍數附近（125 / 140 / 156 / 172）。也就是說 seed 之間的時間差幾乎全是牆鐘計時雜訊；框架逐 seed 比 `SolveTimeMs` 沒有容差，差一個 tick（例：sizing 的 `s1-threads12-s11` 171 vs baseline 172 判 win）就算勝負。這是框架層的量測特性，本階段不改，判讀時記住。

---

## S1 — 2026-10-03（Threads sizing，不計入輪次）

- 指令：`dotnet run -c Release -- sizing`；experiment：`sizing`；RunId：`20261003-010432`
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment = sizing + RunId）
- 設計：`ParallelMode = 1` 固定，只掃 `Threads`；候選 = 實體核心 12 / 11 / 10，對照現行 8（label `s1-threads8-baseline`），seed 11 / 22 / 33，順序輪替 + warm-up 1 次

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000 | s1-threads8-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | baseline |
| cap6000 | s1-threads12 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 171.00000008940697 | win | Threads=12 |
| cap6000 | s1-threads11 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | tie | Threads=11 |
| cap6000 | s1-threads10 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 187.99999984912574 | lose | Threads=10 |
| cap6000 | s1-threads12 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | win | Threads=12 |
| cap6000 | s1-threads11 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | win | Threads=11 |
| cap6000 | s1-threads10 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 139.99999989755452 | win | Threads=10 |
| cap6000 | s1-threads8-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.99999989010394 | baseline | none |
| cap6000 | s1-threads11 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 155.9999999590218 | lose | Threads=11 |
| cap6000 | s1-threads10 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | tie | Threads=10 |
| cap6000 | s1-threads8-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | baseline | none |
| cap6000 | s1-threads12 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 140.99999982863665 | lose | Threads=12 |

- 勝負（`Experiments/Cap6000-summary.csv`，Experiment = sizing，RunId = 20261003-010432）：`s1-threads12` Wins 2 / Losses 1 / Ties 0；`s1-threads11` Wins 1 / Losses 1 / Ties 1；`s1-threads10` Wins 1 / Losses 1 / Ties 1；NotCompared 皆 0
- 判定（§2.3）：沒有候選 3 個 seed 全贏 → **維持 `Threads = 8`，凍結**。三個候選的輸贏都在一兩個計時 tick 之內，且 12 個 trial 全部 NodeCount 0（root 就解完），threads 數對這題本來就幾乎沒有平行空間。

---

## R0 — 2026-10-03（校準，不計入輪次）

- 指令：`dotnet run -c Release -- exp 0`；experiment：`tuning-r0`；RunId：`20261003-010450`
- exp 設定快照：`Program.cs` 的 `// R0 — Cap6000-tuning-r0` 區塊（baseline × 5 seeds，完整值 = 契約區塊的停止 + 環境契約，`Seed` 由 runner 換成 11–55）
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）

<!-- TUNING-FACTS:R0:BEGIN -->
- experiment：`tuning-r0`；RunId：`20261003-010450`
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000 | r0-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | baseline |
| cap6000 | r0-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | none |
| cap6000 | r0-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 155.9999999590218 | baseline | none |
| cap6000 | r0-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | none |
| cap6000 | r0-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | baseline | none |
<!-- TUNING-FACTS:R0:END -->

### 產出 A · baseline 對照組與情境確認

5 個 seed 全部 `Optimal`，`SolveTimeMs` 140–172 ms，遠低於 `TimeLimit` 300 s → **情境 A 確認**，之後逐 seed 比大小會比到 runtime。seed 33 停在 −2451274（gap 8.0e-5），其餘停在 −2451346（gap 5.1e-5），都在 `MipGap` 1e-4 內。

### 產出 B · 瓶頸剖面（§2.1）

| seed | FirstSolutionMs | SolveTimeMs | FirstSolutionMs ÷ SolveTimeMs | BoundChange | LastBoundChangeMs | NodeCount |
| --- | --- | --- | --- | --- | --- | --- |
| 11 | 94.0000000409782 | 172.0000000204891 | 0.55 | 66.77119300328195 | 125 | 0 |
| 22 | 77.9999999795109 | 156.00000019185245 | 0.50 | 66.77119300374761 | 125 | 0 |
| 33 | 109.00000017136335 | 155.9999999590218 | 0.70 | 66.77119300374761 | 125 | 0 |
| 44 | 93.00000010989606 | 172.0000000204891 | 0.54 | 66.77119300328195 | 125 | 0 |
| 55 | 77.9999999795109 | 140.00000013038516 | 0.56 | 66.77119300374761 | 94.0000000409782 | 0 |

- 首解佔比 0.50–0.70（大），`BoundChange` 66.8 相對 |objective| 2.45M 可忽略 → **剖面 = Primal-search**。
- CPLEX log（seed 11）印證：root LP bound 已是 −2451537.3，一回 cut（MIR 2、Gomory 1）後到 −2451470.55 就不再動；之後 gap 是靠 root 啟發式把 incumbent 從 −85207 → −2445344 → −2447959 → −2451346 才收進 1e-4。整題 **root 就解完（NodeCount 0）**，總時間 103 ticks，其中 presolve + probing + root LP 約 41 ticks，剩下是 root 的 cut / 啟發式迴圈。
- 候選（§2.2 Primal-search 列）：`Emphasis = 1` → `RinsHeuristicFrequency` → `HeuristicEffort` → `NodeSelect = 0` → `DiveType` → `Emphasis = 4`。其中 `NodeSelect` / `DiveType` 只作用在 B&B 節點，**這題 NodeCount = 0，條件不成立 → 不排輪次**（記為不適用，不是已否證）。

### 產出 C · 契約健檢探針（§3.0）

- 指令：`dotnet run -c Release -- probe`；experiment：`probe-mipgap0`；RunId：`20261003-010544`；不是 variant，不進排名；`TimeLimit` 未放大
- 結果：`probe-mipgap0-s11` Optimal，objective −2451377、bestBound −2451377、gap 0、SolveTimeMs 311.9999999180436、NodeCount 2039
- **真最佳值 = −2451377**（與 MIPLIB 公告一致）
- 現行契約的品質損失 =（obj_contract − obj_true）/ obj_true，以同 seed 11 的 R0 結果：(−2451346 − (−2451377)) / (−2451377) = −1.26e-5，也就是交付解比真最佳差 31；seed 33 的交付解 −2451274 差 103
- 取得真最佳的額外時間：seed 11 由 172.0000000204891 ms（R0）→ 311.9999999180436 ms（探針），NodeCount 0 → 2039
- **finding（回報使用者，不變更契約）**：`MipGap = 1e-4` 讓 CPLEX 停在 root，交付的是次佳解；要拿到真最佳只多約 0.14 秒。要不要把契約改成 `MipGap = 0` 是使用者的品質決策。

### R0 出口 gate

| 檢查 | 結果 |
| --- | --- |
| 情境已確認 | PASS：A（5/5 Optimal） |
| 剖面已分類 | PASS：Primal-search（root 就解完） |
| 結果不變式（情境 A） | PASS：facts 不變式表 Optimal objective 分散 72（容許 245.1346），bound 越線 0；5 個 objective 都在 phase2Objective ± 245.13 內 |
| dynamic search | PASS：log `Cap6000-tuning-r0_exp_2026-10-03_01-04-50.txt` dynamic search = 6（5 trial + warm-up 1），traditional branch-and-cut = 0 |
| 契約與環境已定版 | PASS：本檔契約區塊 |

---

## S2.5 — 2026-10-03（CPLEX 內建 tune）

- 指令：`dotnet run -c Release -- cplex-tune 600`（總預算 600 s、每次試跑 300 s、`Tune.Repeat = 3`）；狀態 Complete，實際約 3 秒
- archive：`Experiments/Cap6000-cplex-tune-20261003-010555.prm`
- 建議（fixed set 以外的差異）：`CPXPARAM_MIP_Cuts_Gomory -1` → 對應 `CplexConfig.GomoryCuts = -1`
- 處置：拆成獨立 variant 進 S3（R1）驗證，NEVER 直接 promote。它不在 Primal-search 那一列，候選來源是 §3.6；對應的機制是 §2.2 Node-cost 列的「減 cuts」——這題時間大多花在 root 的 cut / 啟發式迴圈。

---

## R1 — 2026-10-03

**本輪目標（跑之前）**：情境 A。在同一停止契約下，`r1-GomoryCuts=-1` 在 5 個 tuning seed 上都不比 baseline 慢、至少快 3 個（R0 baseline 各 seed 140–172 ms），且 objective 維持在 phase2Objective −2451346 ± 245.13 內、bound 不越線。
**決策依據（跑之前）**：
  - 歷史：已讀 S1、R0、S2.5；已否證方向：無；不適用：`NodeSelect` / `DiveType`（NodeCount = 0）
  - experiment 摘要：`tuning-r0`（RunId 20261003-010450）5/5 Optimal、NodeCount 0、IterationCount 132；現行 baseline = 契約區塊的 `MipGap 1e-4 / TimeLimit 300 / Threads 8 / ParallelMode 1`
  - 收斂軌跡：R0 剖面 Primal-search；log 顯示 root 一回 cut（MIR 2、Gomory 1）後 bound 就停在 −2451470.55，時間花在 root 的 cut / 啟發式迴圈
  - 候選來源：S2.5 CPLEX tune 建議（§3.6，`Tune.Repeat = 3` 下偏好 Gomory = −1），不是 Primal-search 列
**假設**：Gomory fractional cut 的分離要解 tableau 列，對這題（root LP 只要 4.93 ticks）是 root 迴圈裡相對貴的一步，且只貢獻 1 條 cut。關掉它，root LP bound −2451537.3 加上 MIR cut 仍足以讓 incumbent −2451346 的 gap 落在 1e-4 內（只靠 root LP bound 時 gap 約 7.8e-5），所以仍在 root 收斂、但少一段分離成本。
**預測**：`r1-GomoryCuts=-1` 5 個 seed 全部 Optimal、NodeCount 仍為 0、objective 在容差內；`Losses = 0` 且 `Wins ≥ 3`（勝出）。風險：關 cut 會改變 root LP 序列，啟發式找到的 incumbent 路徑可能跟著變，若某 seed 改成要分支，就會輸。←跑之前寫死
**每個設定的理由（跑之前）**：

| config label | 唯一改動 | 證據 → 瓶頸 → 候選 → 預期效果 |
| --- | --- | --- |
| r1-baseline | 無（對照組） | — |
| r1-GomoryCuts=-1 | `GomoryCuts` 0 → −1 | S2.5 tune 建議 + log 只用到 1 條 Gomory cut → root 迴圈成本 → §3.6 / §2.2「減 cuts」→ 少一段分離、root 更快收斂 → runtime 贏 |

**實測**：
  - 指令：`dotnet run -c Release -- exp 1`；experiment：`tuning-r1`；RunId：`20261003-010916`
  - exp 設定快照：`Program.cs` 的 `// R1 — Cap6000-tuning-r1` 區塊（已 materialize）；archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）
  - seeds：11, 22, 33, 44, 55；勝負抄自 `Experiments/Cap6000-summary.csv`（Experiment = tuning-r1，RunId = 20261003-010916）

<!-- TUNING-FACTS:R1:BEGIN -->
- experiment：`tuning-r1`；RunId：`20261003-010916`
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000 | r1-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | baseline |
| cap6000 | r1-GomoryCuts=-1 | 11 | Optimal | -2451346 | -2451528.8163508927 | 7.457794652107529E-05 | 187.99999984912574 | lose | GomoryCuts=-1 |
| cap6000 | r1-GomoryCuts=-1 | 22 | Optimal | -2451346 | -2451528.816350893 | 7.457794652126526E-05 | 156.00000019185245 | win | GomoryCuts=-1 |
| cap6000 | r1-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | none |
| cap6000 | r1-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 155.9999999590218 | baseline | none |
| cap6000 | r1-GomoryCuts=-1 | 33 | Optimal | -2451274 | -2451517.790216098 | 9.945449431523112E-05 | 156.00000019185245 | lose | GomoryCuts=-1 |
| cap6000 | r1-GomoryCuts=-1 | 44 | Optimal | -2451346 | -2451528.816350893 | 7.457794652126526E-05 | 125 | tie | GomoryCuts=-1 |
| cap6000 | r1-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000 | r1-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000 | r1-GomoryCuts=-1 | 55 | Optimal | -2451346 | -2451528.816350893 | 7.457794652126526E-05 | 140.99999982863665 | lose | GomoryCuts=-1 |
<!-- TUNING-FACTS:R1:END -->

**分析報告**：

- 剖面 / 候選對應：本輪候選來自 S2.5（§3.6），不是 Primal-search 列；機制屬 §2.2 Node-cost 的「減 cuts」。
- eligibility：10 個 trial 全部 Optimal、NodeCount 0；facts 不變式表（跨 15 筆 trial）Optimal objective 分散 72（容許 245.1346）、bound 越線 0 → PASS；dynamic search = 11（10 trial + warm-up），traditional 0 → PASS。沒有淘汰。
- 勝負（`-summary.csv`）：`r1-GomoryCuts=-1` Wins 1 / Losses 3 / Ties 1 / NotCompared 0 → 不符勝出條件。
- 有效設定：baseline → variant 唯一差異 `GomoryCuts: null → -1`（主表 `ConfigChanges` = `GomoryCuts=-1`）。

1. **整體實驗收斂與結果**：本輪只測一個方向（關 Gomory cut），1 個 variant × 5 seed，全數通過 eligibility。逐 seed 比大小 1 贏 3 輸 1 平，預測的「勝出」被否證。objective 與 baseline 逐 seed 完全相同（seed 33 兩邊都停在 −2451274，其餘 −2451346），差異只在 bound 與時間。
2. **收斂軌跡解讀**：關掉 Gomory 後 root 只剩 MIR 2 條 cut，bound 停在 −2451528.8（seed 33 為 −2451517.8），比 baseline 的 −2451470.55 弱（facts `bestBound`）；但 incumbent −2451346 的 gap 7.46e-5 仍在 1e-4 內（facts `gap`），所以照樣 root 收斂。主表同一批（`Experiments/Cap6000-trial.csv`，tuning-r1，RunId 20261003-010916）的診斷欄：`BoundChange` 由 66.77 縮到 4.85、`NodeCount` 兩邊都是 0、`IterationCount` 132 → 129、`FirstSolutionMs` 沒有一致提前（seed 11 94 → 110，seed 44 62 → 78，其餘相同）。CPLEX log（`bin/Release/net8.0/Log/Cap6000-tuning-r1_exp_2026-10-03_01-09-16.txt` 的 `Total (root+branch&cut)` 行）的確定性時間：baseline 103.07–103.16 ticks，variant 104.52–104.54 ticks——variant 每個 seed 都多做了約 1.4 ticks 的工作，不是更少。
3. **分析者見解與下一步**：事實是關 Gomory 沒有省下 root 工作量（ticks 反而略增），主表上的 1 贏 3 輸 1 平都落在 15.6 ms 的計時 tick 之內，是雜訊而非效果。推論：Gomory 分離在這題的成本小於「少一條 cut 導致後續 root 迴圈多跑」的成本；CPLEX tune 偏好 −1 很可能是它自己的量測雜訊（它也只量牆鐘）。結論 retain，`GomoryCuts = -1` 進已否證。下一輪回到 R0 剖面 Primal-search 列的第一顆 `Emphasis = 1`。

**裁決**：retain（Wins 1、Losses 3，不符「0 輸且 ≥ 3 贏」）
**已否證**（累積，後續輪次不重試）：

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `GomoryCuts = -1` | `-summary.csv` tuning-r1 Wins 1 / Losses 3；ticks 103.1 → 104.5 | R1 |

## R2 — 2026-10-03

**本輪目標（跑之前）**：情境 A。`r2-Emphasis=1` 在 5 個 tuning seed 上都不比 baseline 慢、至少快 3 個（R1 baseline 各 seed 125–172 ms），objective 維持在 −2451346 ± 245.13 內、bound 不越線。
**決策依據（跑之前）**：
  - 歷史：已讀 S1、R0、S2.5、R1；已否證：`GomoryCuts = -1`（R1）；不適用：`NodeSelect` / `DiveType`（NodeCount = 0）
  - experiment 摘要：`tuning-r1`（RunId 20261003-010916）baseline 5/5 Optimal、NodeCount 0；R1 確認 root cut 迴圈不是可省的成本（關 cut 後 ticks 反增）
  - 收斂軌跡：情境 A 沿用 R0 剖面 Primal-search（§2.1：情境 A 不必每輪重判）；gap 由 incumbent 側收進 1e-4，bound 一回 cut 後就不動
**假設**：gap 的收尾取決於 root 啟發式何時找到 −2451346。`Emphasis = 1`（FEASIBILITY）讓 CPLEX 在 root 更積極跑啟發式、少花心力在 bound 上；root LP bound −2451537.3 本身就讓 −2451346 的 gap 約 7.8e-5 < 1e-4，所以 bound 變弱不會逼出分支，若啟發式更早找到收尾解就會更快。
**預測**：不勝出（`Losses ≥ 1`）。理由：baseline 在 root 第一回 cut 後就找到收尾的 incumbent，可行性側能省的空間很小；FEASIBILITY 模式多跑的啟發式反而可能增加 root 工作量（R1 已看到 root 工作量對設定變動很敏感）。若出乎預期勝出，代表 baseline 的 root 啟發式排程有可省的空檔。←跑之前寫死
**每個設定的理由（跑之前）**：

| config label | 唯一改動 | 證據 → 瓶頸 → 候選 → 預期效果 |
| --- | --- | --- |
| r2-baseline | 無（對照組） | — |
| r2-Emphasis=1 | `Emphasis` 0 → 1 | R0 首解佔比 0.50–0.70、bound 一回 cut 即停 → Primal-search → §2.2 該列第一顆 `Emphasis = 1` → 更早找到收尾 incumbent → runtime 贏 |

**實測**：
  - 指令：`dotnet run -c Release -- exp 2`；experiment：`tuning-r2`；RunId：`20261003-011054`
  - exp 設定快照：`Program.cs` 的 `// R2 — Cap6000-tuning-r2` 區塊（已 materialize）；archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）
  - seeds：11, 22, 33, 44, 55；勝負抄自 `Experiments/Cap6000-summary.csv`（Experiment = tuning-r2，RunId = 20261003-011054）

<!-- TUNING-FACTS:R2:BEGIN -->
- experiment：`tuning-r2`；RunId：`20261003-011054`
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000 | r2-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | baseline |
| cap6000 | r2-Emphasis=1 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | win | Emphasis=1 |
| cap6000 | r2-Emphasis=1 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 141.0000000614673 | win | Emphasis=1 |
| cap6000 | r2-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.00000019185245 | baseline | none |
| cap6000 | r2-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | baseline | none |
| cap6000 | r2-Emphasis=1 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 125 | tie | Emphasis=1 |
| cap6000 | r2-Emphasis=1 | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | tie | Emphasis=1 |
| cap6000 | r2-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | baseline | none |
| cap6000 | r2-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 139.99999989755452 | baseline | none |
| cap6000 | r2-Emphasis=1 | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.00000013038516 | lose | Emphasis=1 |
<!-- TUNING-FACTS:R2:END -->

**分析報告**：

- 剖面 / 候選對應：Primal-search → §2.2 該列第一顆 `Emphasis = 1`。
- eligibility：10 個 trial 全部 Optimal；facts 不變式表（跨 25 筆 trial）Optimal objective 分散 72（容許 245.1346）、bound 越線 0 → PASS；log `Cap6000-tuning-r2_exp_2026-10-03_01-10-54.txt` dynamic search = 11（10 trial + warm-up）、traditional 0 → PASS。沒有淘汰。
- 勝負（`-summary.csv`）：`r2-Emphasis=1` Wins 2 / Losses 1 / Ties 2 / NotCompared 0 → 不符勝出條件。
- 有效設定：baseline → variant 唯一差異 `Emphasis: null → 1`（主表 `ConfigChanges` = `Emphasis=1`）。

1. **整體實驗收斂與結果**：1 個 variant × 5 seed，全數通過 eligibility，2 贏 1 輸 2 平，不勝出——預測「不勝出」被支持，但原因比預測的更直接：variant 根本沒有改變 CPLEX 的求解。facts 的 objective、bestBound、gap 逐 seed 與 baseline 完全相同。
2. **收斂軌跡解讀**：主表同一批（`Experiments/Cap6000-trial.csv`，tuning-r2，RunId 20261003-011054）的 `BoundChange` 逐 seed 與 baseline 相同（66.77119300328195 / 66.77119300374761），`NodeCount` 0、`IterationCount` 132 兩邊一樣；CPLEX log 的確定性時間逐 seed 完全相同（seed 11 103.11 / 103.11、22 103.10 / 103.10、33 103.16 / 103.16、44 103.07 / 103.07、55 103.11 / 103.11 ticks），兩邊都套用 MIR 2、Gomory 1 條 cut。也就是 `Emphasis = 1` 在這題 root 階段完全沒有作用——emphasis 主要改變 B&B 的節點與啟發式排程，這題沒有節點。
3. **分析者見解與下一步**：這一輪是一個天然的對照實驗：兩組做的是**逐 tick 相同**的計算，框架仍判出 2 贏 1 輸 2 平。這直接量到了這台機器上 `SolveTimeMs` 的雜訊——同一份計算在 125–156 ms 之間跳、勝負由一兩個 15.6 ms tick 決定。推論：這題任何「贏」只要確定性時間沒變，就是雜訊；之後判讀勝出時要同時看 log ticks 有沒有真的下降（只當判讀，不取代框架判定）。依 §7.1 B（本輪唯一 variant 的 bound 軌跡起訖與 baseline 完全一致）→ `Emphasis` 所屬的「emphasis 與搜尋分支」群組標記已否證，同群組剩下的候選 `Emphasis = 4`、`NodeSelect = 0`、`DiveType` 跳過（後兩者本來就因 NodeCount = 0 不適用）。Primal-search 列還剩「啟發式」群組的 `RinsHeuristicFrequency` → `HeuristicEffort`，下一輪測 RINS。

**裁決**：retain（Wins 2、Losses 1，不符「0 輸且 ≥ 3 贏」）
**已否證**（累積，後續輪次不重試）：

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `GomoryCuts = -1` | `-summary.csv` tuning-r1 Wins 1 / Losses 3；ticks 103.1 → 104.5 | R1 |
| `Emphasis = 1` | `-summary.csv` tuning-r2 Wins 2 / Losses 1；ticks 逐 seed 與 baseline 相同 | R2 |
| 「emphasis 與搜尋分支」群組（`Emphasis = 4`、`NodeSelect = 0`、`DiveType`） | §7.1 B：R2 bound 軌跡起訖與 baseline 一致；root 就解完、沒有節點 | R2 |

## R3 — 2026-10-03

**本輪目標（跑之前）**：情境 A。`r3-RinsHeuristicFrequency=10` 在 5 個 tuning seed 上都不比 baseline 慢、至少快 3 個（R2 baseline 各 seed 125–156 ms），objective 維持在 −2451346 ± 245.13 內、bound 不越線。
**決策依據（跑之前）**：
  - 歷史：已讀 S1、R0、S2.5、R1、R2；已否證：`GomoryCuts = -1`、`Emphasis = 1`、「emphasis 與搜尋分支」群組；連續無人勝出 2 輪（R1、R2）
  - experiment 摘要：`tuning-r2`（RunId 20261003-011054）baseline 5/5 Optimal、NodeCount 0；R2 證明 root 路徑完全不受 emphasis 影響
  - 收斂軌跡：情境 A 沿用 R0 剖面 Primal-search；收尾 incumbent −2451346 由 root 啟發式找到
**假設**：CPLEX 官方說明 `RINSHeur` 設正整數 N 時在 node 0、N、2N…呼叫 RINS——**node 0 就是 root**，所以即使這題沒有分支，設正值也會在 root 跑一次 RINS。RINS 以 root LP 解與現有 incumbent 一致的變數固定後解子 MIP，可能比預設的 root 啟發式更早找到 −2451346，讓 gap 提早收進 1e-4。值取 10：在 root 的效果跟任何正值相同，萬一出現分支也不至於每個節點都跑。
**預測**：不勝出（`Losses ≥ 1`）。理由：baseline 在第一回 cut 後就已找到收尾解，root 再加一次 RINS 子 MIP 只會增加工作量（預期 log ticks 上升）；若 CPLEX 預設（0 = 自動）本來就在 root 跑 RINS，則路徑不變、跟 R2 一樣只剩雜訊。←跑之前寫死
**每個設定的理由（跑之前）**：

| config label | 唯一改動 | 證據 → 瓶頸 → 候選 → 預期效果 |
| --- | --- | --- |
| r3-baseline | 無（對照組） | — |
| r3-RinsHeuristicFrequency=10 | `RinsHeuristicFrequency` 0 → 10 | R0 首解佔比 0.50–0.70、gap 由 incumbent 側收尾 → Primal-search → §2.2 該列下一顆（emphasis 群組已否證）→ root 多一次 RINS 更早找到 −2451346 → runtime 贏 |

**實測**：
  - 指令：`dotnet run -c Release -- exp 3`；experiment：`tuning-r3`；RunId：`20261003-011218`
  - exp 設定快照：`Program.cs` 的 `// R3 — Cap6000-tuning-r3` 區塊（已 materialize）；archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）
  - seeds：11, 22, 33, 44, 55；勝負抄自 `Experiments/Cap6000-summary.csv`（Experiment = tuning-r3，RunId = 20261003-011218）

<!-- TUNING-FACTS:R3:BEGIN -->
- experiment：`tuning-r3`；RunId：`20261003-011218`
- archive：`Experiments/Cap6000-trial.csv`、`Experiments/Cap6000-meta.csv`、`Experiments/Cap6000-summary.csv`、`Experiments/Cap6000-trajectory.csv`（篩 Experiment + RunId）

| instance | label | seed | status | objectiveValue | bestBound | gap | solveTimeMs | vsBaseline | configDiffFromBaseline |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| cap6000 | r3-baseline | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | baseline | baseline |
| cap6000 | r3-RinsHeuristicFrequency=10 | 11 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 156.99999989010394 | win | RinsHeuristicFrequency=10 |
| cap6000 | r3-RinsHeuristicFrequency=10 | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | lose | RinsHeuristicFrequency=10 |
| cap6000 | r3-baseline | 22 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | baseline | none |
| cap6000 | r3-baseline | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 156.00000019185245 | baseline | none |
| cap6000 | r3-RinsHeuristicFrequency=10 | 33 | Optimal | -2451274 | -2451470.55383104 | 8.01843576197226E-05 | 140.00000013038516 | win | RinsHeuristicFrequency=10 |
| cap6000 | r3-RinsHeuristicFrequency=10 | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 172.0000000204891 | lose | RinsHeuristicFrequency=10 |
| cap6000 | r3-baseline | 44 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 140.99999982863665 | baseline | none |
| cap6000 | r3-baseline | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 125 | baseline | none |
| cap6000 | r3-RinsHeuristicFrequency=10 | 55 | Optimal | -2451346 | -2451470.55383104 | 5.081038378096274E-05 | 155.9999999590218 | lose | RinsHeuristicFrequency=10 |
<!-- TUNING-FACTS:R3:END -->

**分析報告**：

- 剖面 / 候選對應：Primal-search → §2.2 該列的 `RinsHeuristicFrequency`（emphasis 群組已於 R2 否證）。
- eligibility：10 個 trial 全部 Optimal；facts 不變式表（跨 35 筆 trial）Optimal objective 分散 72（容許 245.1346）、bound 越線 0 → PASS；log `Cap6000-tuning-r3_exp_2026-10-03_01-12-18.txt` dynamic search = 11（10 trial + warm-up）、traditional 0 → PASS。沒有淘汰。
- 勝負（`-summary.csv`）：`r3-RinsHeuristicFrequency=10` Wins 2 / Losses 3 / Ties 0 / NotCompared 0 → 不符勝出條件。
- 有效設定：baseline → variant 唯一差異 `RinsHeuristicFrequency: null → 10`（主表 `ConfigChanges` = `RinsHeuristicFrequency=10`）。

1. **整體實驗收斂與結果**：1 個 variant × 5 seed，全數通過 eligibility，2 贏 3 輸，不勝出——預測「不勝出」被支持，而且又是 R2 的情形：objective、bestBound、gap 逐 seed 與 baseline 完全相同，variant 沒有改變求解。
2. **收斂軌跡解讀**：主表同一批（`Experiments/Cap6000-trial.csv`，tuning-r3，RunId 20261003-011218）的 `BoundChange` 逐 seed 與 baseline 相同、`NodeCount` 0、`IterationCount` 132 兩邊一樣；CPLEX log 的確定性時間逐 seed 完全相同（seed 11 103.11、22 103.10、33 103.16、44 103.07、55 103.11 ticks，兩邊一樣），首個 incumbent −85207 都在 28.39 ticks 出現，cut 都是 MIR 2、Gomory 1。推論：gap 在 root 的 cut / 啟發式迴圈中就收進 1e-4，CPLEX 在 node 0 呼叫 RINS 之前就已停止，所以 RINS 頻率對這題不會被用到。預測裡「ticks 會上升」的那一半被否證——不是 RINS 太貴，是根本沒輪到它。
3. **分析者見解與下一步**：R2、R3 兩輪的 variant 都和 baseline 做了逐 tick 相同的計算，框架仍判出 R2 2 贏 1 輸、R3 2 贏 3 輸，再次確認這題的時間勝負只剩計時雜訊。依 §7.1 B（本輪唯一 variant 的 bound 軌跡起訖與 baseline 一致）→「啟發式」群組標記已否證，`HeuristicEffort` 跳過；Primal-search 列至此全部否證或不適用。同時 R1、R2、R3 連續 3 輪無人勝出 → §7.1 D 命中。**停止。**

**裁決**：retain（Wins 2、Losses 3，不符「0 輸且 ≥ 3 贏」）
**已否證**（累積，後續輪次不重試）：

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `GomoryCuts = -1` | `-summary.csv` tuning-r1 Wins 1 / Losses 3；ticks 103.1 → 104.5 | R1 |
| `Emphasis = 1` | `-summary.csv` tuning-r2 Wins 2 / Losses 1；ticks 逐 seed 與 baseline 相同 | R2 |
| 「emphasis 與搜尋分支」群組（`Emphasis = 4`、`NodeSelect = 0`、`DiveType`） | §7.1 B：R2 bound 軌跡起訖與 baseline 一致；root 就解完、沒有節點 | R2 |
| `RinsHeuristicFrequency = 10` | `-summary.csv` tuning-r3 Wins 2 / Losses 3；ticks 逐 seed 與 baseline 相同 | R3 |
| 「啟發式」群組（`HeuristicEffort` 等） | §7.1 B：R3 bound 軌跡起訖與 baseline 一致；gap 在 node 0 的啟發式被呼叫前就已收斂 | R3 |

---

## 收尾 — 2026-10-03

**停止原因**：§7.1 **B**（Primal-search 剖面對應的「emphasis 與搜尋分支」、「啟發式」兩個群組都已整類否證，剩下的 `NodeSelect` / `DiveType` 因 NodeCount = 0 不適用）與 **D**（R1、R2、R3 連續 3 輪無人勝出）同時命中。依 B 先檢查的順序記為 B。
**hold-out（S4）**：沒有 champion，不跑。
**promotion（S5）**：無。**裁決 = retain**，`productionBaseline` 維持 initial（`MipGap 1e-4 / TimeLimit 300 / Threads 8 / ParallelMode 1 / Seed 11`）；production 行為未變，S0 的 production 結果（Optimal −2451346）即為現行驗證結果。

**整期結論**：

1. 這題在 CPLEX 22.1.1 上 **root 就解完**（每個 trial NodeCount 0、約 103–105 ticks、主表 SolveTimeMs 125–188 ms），進場情境 A，剖面 Primal-search。
2. 3 輪 3 個 variant 沒有一個改善：關 Gomory 讓 root 工作量多 1.4 ticks；`Emphasis = 1` 與 `RinsHeuristicFrequency = 10` 讓 CPLEX 走出逐 tick 相同的路徑。框架判出的贏輸全部落在 15.6 ms 計時 tick 之內。
3. 在現行停止契約下，**這題沒有可調的 solver 旋鈕空間**：剩下的時間大多是 presolve + probing + root LP（約 41 ticks）與一回 cut / 啟發式迴圈，這些都不是搜尋策略能省的。

**回報使用者的 findings（都不是本階段能自行決定的）**：

1. **契約**：`MipGap = 1e-4` 讓 CPLEX 在 root 停在次佳解（−2451346，比真最佳 −2451377 差 31；seed 33 停在 −2451274，差 103）。`MipGap = 0` 探針只多約 0.14 秒（312 ms、2039 nodes）就拿到真最佳。若要真最佳，改契約後需重跑 S1 / R0；那時題目會變成真的需要分支，才有 tuning 的空間。
2. **量測**：這題的牆鐘時間在雜訊等級，框架逐 seed 比 `SolveTimeMs` 沒有容差，同一份計算也會判出 2 贏 3 輸。要在這種短解題上比得準，需要以 CPLEX 確定性時間（ticks，`Cplex.GetDetTime()`）比大小——這是框架層的改動，留給使用者決定。
3. **CPLEX 內建 tune 的建議（`GomoryCuts = -1`）沒有通過驗證**：同樣是牆鐘雜訊下的選擇。
