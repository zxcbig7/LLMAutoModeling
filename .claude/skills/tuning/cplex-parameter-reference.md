# CPLEX 參數導航

本文件不維護 `CplexConfig` 完整欄位鏡像。欄位名稱、型別、預設值與 XML comment 的 canonical source 是：

- [`../../../../OptimFoundation/OptimFoundation/specs/developer-guide.md`](../../../../OptimFoundation/OptimFoundation/specs/developer-guide.md)
- sibling source：`OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs`

調校順序與實驗判定以 [`solver-tuning-guide.md`](solver-tuning-guide.md) 為準。每次使用參數前，直接在 canonical source 確認 PascalCase property；NEVER 從舊報告、歷史 code 或本文件猜欄位名。

## 分類導航

| 類別 | 用途 | 在 tuning workflow 的位置 |
| --- | --- | --- |
| 終止條件 | 時間、gap、node、solution limit | baseline 契約；不得當作一般效能 variant 混掃 |
| 執行環境 | threads、parallel mode、seed、記憶體 | 先定版並凍結；seed 是重複量測共同因子 |
| 搜尋策略 | emphasis、search、branch、node selection | 依瓶頸剖面一次只測一個假設 |
| Heuristic | RINS、heuristic frequency/effort、pump | primal search 情境 |
| Cut | 各 cut family 與 passes | bound search 情境 |
| Presolve | aggregation、presolve passes/reduce | build/root 情境 |
| Algorithm | root/node algorithm、barrier/net | LP/root relaxation 情境 |
| Numerical | feasibility/optimality/integrality tolerance、scaling | 只有數值證據時調整 |

`ProjectConfig` 是輸出政策，不是 solver 參數；它不屬於本表，也不可放進 tuning variant。

## 使用規則

1. 從 `solver-tuning-guide.md` 的瓶頸分流選候選類別。
2. 到 `CplexConfig.cs` 或 developer guide 查實際 property、型別與語意。
3. 從 `productionBaseline.Clone()` 產生 variant，只改一個假設所需欄位。
4. 用 `OptExperiment` 跑 `tuning-r<N>`，由 `-summary.csv` 與 `-trial.csv` 裁決。
5. 不確定欄位是否存在時視為不存在，先查 canonical source；禁止使用 camelCase 舊名。
