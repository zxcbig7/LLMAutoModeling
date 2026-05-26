# SandwichProduction — 數學模型

## 問題描述

早餐店生產多種三明治，共用有限食材庫存。
在食材用量限制下，決定各種三明治的生產數量以最大化總利潤。

---

## Sets

| 符號 | 說明 |
| --- | --- |
| $I$ | 三明治種類集合 |
| $J$ | 食材種類集合 |

---

## Parameters

| 符號 | 說明 |
| --- | --- |
| $p_i$ | 三明治 $i$ 的單位利潤，$i \in I$ |
| $r_{ji}$ | 製作一個三明治 $i$ 需要食材 $j$ 的用量，$j \in J,\ i \in I$ |
| $A_j$ | 食材 $j$ 的總庫存量，$j \in J$ |

---

## Decision Variables

| 變數 | 說明 |
| --- | --- |
| $x_i \geq 0$ | 生產三明治 $i$ 的數量（連續），$\forall i \in I$ |

---

## Objective Function

$$\max \quad \sum_{i \in I} p_i \cdot x_i$$

---

## Constraints

### [C1] 食材庫存上限

$$\sum_{i \in I} r_{ji} \cdot x_i \leq A_j, \quad \forall j \in J$$

### [C2] 非負

$$x_i \geq 0, \quad \forall i \in I$$
