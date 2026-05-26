# GlassFactory — 數學模型

## 問題描述

玻璃工廠生產多種玻璃，共用加熱機與冷卻機。
在機器產能限制下，決定各類玻璃的生產數量以最大化總利潤。

---

## Sets

| 符號 | 說明 |
| --- | --- |
| $I$ | 玻璃種類集合 |

---

## Parameters

| 符號 | 說明 |
| --- | --- |
| $h_i$ | 玻璃 $i$ 的加熱時間（分鐘/片），$i \in I$ |
| $c_i$ | 玻璃 $i$ 的冷卻時間（分鐘/片），$i \in I$ |
| $p_i$ | 玻璃 $i$ 的單位利潤（元/片），$i \in I$ |
| $H$ | 加熱機總產能上限 |
| $C$ | 冷卻機總產能上限 |

---

## Decision Variables

| 變數 | 說明 |
| --- | --- |
| $x_i \geq 0$ | 生產玻璃 $i$ 的片數（連續），$\forall i \in I$ |

---

## Objective Function

$$\max \quad \sum_{i \in I} p_i \cdot x_i$$

---

## Constraints

### [C1] 加熱機產能

$$\sum_{i \in I} h_i \cdot x_i \leq H$$

### [C2] 冷卻機產能

$$\sum_{i \in I} c_i \cdot x_i \leq C$$

### [C3] 非負

$$x_i \geq 0, \quad \forall i \in I$$
