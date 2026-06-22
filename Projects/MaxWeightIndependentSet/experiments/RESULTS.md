# Tuning 實驗結果 — Max Weighted Independent Set

## 實例

| 項目 | 值 |
|---|---|
| 模型 | Max Weighted Independent Set (IP, NP-hard) |
| 節點 / 邊 | 250 / 9275（G(n,p)，p=0.30，seed=42） |
| 變數 / 約束 | 250 binary / 9275 |
| 最佳目標值 | **1332**（選 16 個節點） |

## Solver-層 tuning sweep

固定模型 + 數值，只掃 `CplexConfig` 旋鈕；epGap=1e-9（逼證明最佳）、timeLimit=300s、threads=8。
資料來源：[mwis-tuning.csv](mwis-tuning.csv)（由 OptimFoundation `Trial.Capture` / `Experiment.Save` 落地）。

| variant | Status | Obj | Gap | WallTime |
|---|---|---|---|---|
| **baseline-emphasis1** | Optimal | 1332 | 0% | **9.9 s** ⬅ 最快 |
| clique-aggressive | Optimal | 1332 | 0% | 13.0 s |
| clique+probe | Optimal | 1332 | 0% | 13.0 s |
| emphasis3-bestbound | Optimal | 1332 | 0% | 13.6 s |
| emphasis2-optimality | Optimal | 1332 | 0% | 15.0 s |
| deterministic-seed | Optimal | 1332 | 0% | 27.0 s |
| emphasis0-balanced | Optimal | 1332 | 0% | 32.6 s |
| probe-aggressive | Optimal | 1332 | 0% | 34.7 s ⬅ 最慢 |

## 結論

1. **正確性不變量成立**：8 組 solver 設定的 `ObjectiveValue` 全為 **1332**、Gap 全 0% → tuning 沒有動到數學結構或數值（符合協定的正確性 gate）。
2. **效能差異 3.5×**：最快 9.9s vs 最慢 34.7s。
3. 本實例下 **IP 預設 `mipEmphasis=1`（重可行性）已是最佳**；強制 `emphasis=0`（平衡）或 `probe=3`（積極探測）反而拖慢 2–3.5×（探測/平衡的額外開銷無法被這個規模的搜尋省回來）。
4. clique cuts 積極化（`cliqueCuts=2`）穩定落在 ~13s——對「每條邊一條 x_i+x_j≤1」的 MWIS，clique cuts 有效但此規模下還贏不過 emphasis=1 的快速收斂。
5. 依協定：正確性通過、baseline 已是最佳解法、加旋鈕無進一步改善 → **維持 baseline `mipEmphasis=1`**，不需回 structure reformulation。

## 重現

```powershell
$env:PATH = "C:\IBM\ILOG\CPLEX_Studio2211\cplex\bin\x64_win64;$env:PATH"
cd "projects\MaxWeightIndependentSet\csharp"
dotnet run -c Debug -- experiment        # 跑 sweep，append 進 Experiments\mwis-tuning.csv+json
dotnet run -c Debug                      # 單次求解（正確性 gate）
# 換實例：$env:MWIS_N=300; $env:MWIS_P=0.25; $env:MWIS_SEED=7
```

> 備註：現行 `OptEngine.LastMetrics` 未填 `NodeCount` / `IterationCount`（CSV 該兩欄為空）；補抓需動 OptimFoundation Core+Cplex DLL（本次範圍外）。效能比較以 `WallTimeMs` 為準。
