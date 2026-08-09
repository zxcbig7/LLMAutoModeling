# HospitalRostering_Generator — Tuning History

> 這份檔案是 tuning 的**永久決策紀錄**，進版控。
> `bin/Experiments/*.json` 會被 `dotnet clean` 清掉，不能當唯一憑證。
> 每輪一節，格式為「假設 → 預測 → 實測 → 裁決」四段；**預測必須在跑實驗之前寫**。

---

## 契約區塊（整個 tuning 週期固定，變更即重跑 S1–S2）

### 停止契約（決定「什麼叫解完了」）

| 項目 | 現值 | 備註 |
| --- | --- | --- |
| `MipGap` | **0.03** | ✅ **定案 2026-08-09**（使用者決定）：維持 0.03，接受 2.7% 品質損失換取約 7s。詳見發現 4 |
| `TimeLimit` | **180** | ✅ **定案 2026-08-09**（使用者決定）：3 分鐘。實際求解 5–17s、從未觸及，故 R-1 歷史數據仍可比；未來若出現 timeout，PAR10 罰則基準為 1800s |
| `AbsoluteMipGap` / `NodeLimit` / `IntegerSolutionLimit` | null | 用 CPLEX 預設 |
| 容差 `IntegralityTolerance` / `OptimalityTol` / `FeasibilityTol` | 預設 | 未動 |

### 環境契約（決定量測基準）

| 項目 | 現值 | 狀態 |
| --- | --- | --- |
| `Threads` | 10 | ⚠️ **未定版**，待跑 S1 sizing |
| `ParallelMode` | null（依 CPLEX 預設） | ⚠️ 應明設 `1`，否則可重現性隨 DLL 版本浮動 |
| `MemoryLimitMb` | 2048（框架預設） | 注意：框架設它時會強制 `MIP.Strategy.File = 0` |
| `NodeFileStrategy` | null | 被上一行的行為覆蓋成 0 |

### 量測契約

| 項目 | 現值 | 狀態 |
| --- | --- | --- |
| experiment 期間 export | **全關** | ✅ 符合量測契約 —— exp 分支未呼叫 `.UseConfig(() => projectConfig)`，框架預設 solver log / LP / MPS / Sol 全 OFF |
| production 期間 export | LP / MPS / Sol 皆開 | 正常（promotion 驗證時要開回來確認時間仍可接受） |
| 解正確性驗證 | **無** | ⚠️ 專案無 `Solution/` 與 `ValidateRules`，`OnSolved` 只做 `WriteToCSV` |
| dynamic search | 未檢查 | 每輪應確認 solver log 無停用 warning |

---

## R-1 · 回溯分析（2026-08-02）— **非正式輪次**

> ⚠️ 這一輪**不是**照 SOP 跑的：只有 1 個 seed 樣本點、契約旋鈕與策略旋鈕混在同一輪比較、環境未定版。
> 以下結論是事後從既有 experiment 資料讀出來的，**足以排除方向，不足以支持任何 promotion**。

**來源**：`bin/Debug/net8.0/Experiments/hospital-generator-tuning.{csv,json}` + `-trajectory.csv`（241 點）

### 實測

| Trial | runtime | obj | gap | 首個可行解 | bound 軌跡 |
| --- | --- | --- | --- | --- | --- |
| baseline | 10.16s | 3.7 | 2.70% | 0.66s | 3.300 → 3.600 |
| seed=20260622 | 5.62s | 3.7 | 2.70% | 0.97s | 3.300 → 3.600 |
| threads=4 | 10.26s | 3.7 | 2.70% | 0.83s | 3.300 → 3.600 |
| nodesel=bestbound | 10.81s | 3.7 | 2.70% | 0.80s | 3.300 → 3.600 |
| varsel=strong | 12.42s | 3.7 | 2.70% | 2.14s | 3.300 → 3.600 |
| emphasis=optimal | 15.27s | 3.7 | 2.70% | 0.81s | 3.300 → 3.600 |
| gap=0.01 | 17.40s | **3.6** | 0% | 0.85s | 3.300 → 3.600 |

### 四個發現

#### 1 · 雜訊地板 θ ≥ 45%（樣本不足，僅為下限）

唯一明顯變快的 `seed=20260622` **沒有改變任何求解策略**——它是 baseline 的第二次抽樣。45% 的落差因此是變異幅度，不是改善。

推論：其餘 variant 的 +1%～+50% **全部落在雜訊內** → 本輪**無人勝出**。

⚠️ 若當時直接 promote `seed=20260622`，production 下次仍會跑約 10s——promote 的是一個雜訊樣本。

#### 2 · 瓶頸剖面 = Dual-bound

- `t_feas` 0.66s、`r_primal` ≈ 6.5% → **incumbent 極早到位**
- 其後約 9s 全數用於推升 bound
- 結論：primal 側無問題，瓶頸 100% 在證明側

#### 3 · 搜尋策略類旋鈕整組否證（早停 B）

七個 trial 的 bound 軌跡起訖**完全相同**（3.300 → 3.600）。`Emphasis` / `VariableSelect` / `NodeSelect` 對 dual 側**免疫**。

補充：當時試的 `Emphasis = 2`（OPTIMALITY）本就不是推 bound 用的——推 bound 是 **`3`（BESTBOUND）**。此值仍待驗證，不在本次否證範圍內。

#### 4 · 契約檢視 — **已決議：維持 0.03**

`gap=0.01` 揭露**真最佳解為 3.6**，而 `MipGap = 0.03` 讓 production **常態交付 obj 3.7**。

- 品質損失：2.7%
- 取得真最佳的代價：約 +7s（10.2s → 17.4s）

**決議（2026-08-09，使用者）**：維持 `MipGap = 0.03`，接受 2.7% 品質損失換取那 7 秒。

⚠️ **這個取捨的已知後果**：在 0.03 契約下，production **永遠不會輸出 obj 3.6 的最佳班表**，穩定停在 3.7。這是刻意的取捨，不是 bug——日後若有人質疑「為什麼班表不是最佳的」，答案在這裡。

**對後續輪次的影響**：契約定在 0.03 之後，加速的唯一途徑是**讓 bound 更快從 3.300 爬到 3.600**（bound 到 3.6、incumbent 3.7 時 gap 才降到 2.70% < 3% 而停止）。這與發現 2 的 Dual-bound 剖面一致，R1 方向因此更明確。

### 裁決

**retain** — 保留現行 baseline（`MipGap=0.03` / `TimeLimit=100` / `Threads=10`），未 promotion。

理由：無 candidate 的改善超過 θ；唯一「變快」者為雜訊樣本。

### 已否證清單（累積，後續輪次不重試）

| 方向 | 證據 | 否證於 |
| --- | --- | --- |
| `Emphasis = 2`（OPTIMALITY） | bound 軌跡與 baseline 全等；且此值非推 bound 用途 | R-1 |
| `VariableSelect = 3`（strong branching） | bound 軌跡全等；且首個可行解延後至 2.14s | R-1 |
| `NodeSelect = 1`（best-bound） | bound 軌跡全等 | R-1 |
| `Threads` 10 → 4 | 差異 1%，遠低於 θ；且 `r_primal` 僅 6.5%，無平行化空間 | R-1 |
| **搜尋策略類整類**（對 dual 側） | 七條 bound 軌跡完全重疊 | R-1（早停 B） |

---

## 待辦 · 進入正式流程前

| # | 項目 | 阻擋什麼 |
| --- | --- | --- |
| 1 | 補 `status.json` | `/tuning` 進場 gate |
| 2 | 決定是否補 `Solution/` + `ValidateRules` | 無自動化手段確認「解沒被調壞」 |
| ~~3~~ | ~~拍板 `MipGap` 契約~~ ✅ **已完成 2026-08-09**：`MipGap = 0.03`、`TimeLimit = 180` | — |
| 4 | S1 sizing：`Threads` 三候選 × 3 seeds，`ParallelMode = 1` | θ 的量測基準 |
| 5 | S2 R0：baseline × 5 seeds，補滿 θ 的確切值 | 所有後續裁決的門檻 |
| 6 | S2.5：`.lp` 丟 CPLEX Interactive Optimizer 跑 `tune`（零成本，不改程式） | 候選來源 |
| 7 | R1：掃 cuts 類 + `Emphasis = 3` | — |

> 流程與判準見 `_wip/TuningSOP-design.md`（設計稿，尚未併入規範）。
