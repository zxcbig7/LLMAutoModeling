# Standard Model — Max Weighted Independent Set (MWIS)

## 問題描述

給定一張無向圖 $G=(V,E)$，每個節點 $i$ 有非負權重 $w_i$。要選出一個**獨立集 (independent set)**：集合內任兩節點都不相鄰（不存在連接它們的邊）。目標是讓被選節點的**總權重最大**。

此為經典 **NP-hard** 問題（等價於補圖上的 Maximum Weight Clique）。

## 約束分類

| 約束 | 類型 | 語言線索 |
|---|---|---|
| 相鄰兩節點不可同時入選 | UB / Conjunction（每條邊一條 ≤ 1 上界） | "cannot both be selected", "independent" |

## 決策

- 每個節點一個二元決策：選 / 不選。
