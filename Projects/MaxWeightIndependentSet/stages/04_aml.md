# AML Model — Max Weighted Independent Set (MWIS)

## Sets

- $V$ — 節點集合 (vertices)
- $E \subseteq \{(i,j) : i,j \in V,\ i \neq j\}$ — 邊集合 (edges)，無向

## Parameters

- $w_i \ge 0$ — 節點 $i \in V$ 的權重

## Decision Variables

$$
x_i \in \{0,1\}, \quad \forall i \in V
$$

$x_i = 1$ 表示節點 $i$ 被選入獨立集，否則 $x_i = 0$。

## Objective

最大化被選節點的總權重：

$$
\max \; \sum_{i \in V} w_i \, x_i
$$

## Constraints

**(C1) Edge conflict（邊衝突上界）** — 相鄰兩節點不可同時入選：

$$
x_i + x_j \le 1, \quad \forall (i,j) \in E
$$

**(C2) Integrality（二元）**：

$$
x_i \in \{0,1\}, \quad \forall i \in V
$$

## Notes

- 此為純整數規劃 (IP)，NP-hard；等價於補圖 $\bar{G}$ 上的 Maximum Weight Clique。
- (C1) 為「每條邊一條 $\le 1$ 的上界約束」，屬 UB / Conjunction 類；CPLEX 的 **clique cuts** 能把多條 (C1) 聚合成更緊的 clique 不等式 $\sum_{i \in Q} x_i \le 1$（$Q$ 為團），是本問題 tuning 的關鍵旋鈕。
- LHS = $x_i + x_j$，RHS = $1$，方向 $\le$。**不得移項、改號或翻轉方向。**
