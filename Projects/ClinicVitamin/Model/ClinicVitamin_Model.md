# ClinicVitamin — 數學模型

## 問題描述

診所生產維生素注射液（Shots）與維生素藥丸（Pills），
共用維生素 C 與維生素 D 兩種原料庫存。
在原料、產量限制下，決定各產品的批次數量以最大化可供應人數。

---

## Sets

| 符號 | 說明 |
| --- | --- |
| $P$ | 產品種類集合 |
| $V$ | 維生素種類集合 |

---

## Parameters

| 符號 | 說明 |
| --- | --- |
| $r_{vp}$ | 生產一批產品 $p$ 所需維生素 $v$ 的用量，$v \in V,\ p \in P$ |
| $s_{p}$ | 一批產品 $p$ 可供應的人數，$p \in P$ |
| $A_{v}$ | 維生素 $v$ 的庫存總量，$v \in V$ |
| $M$ | 注射液（Shots）的最大批次數量上限 |

---

## Decision Variables

| 變數 | 說明 |
| --- | --- |
| $x_p \geq 0$ | 生產產品 $p$ 的批次數（連續），$\forall p \in P$ |

---

## Objective Function

$$\max \quad \sum_{p \in P} s_p \cdot x_p$$

---

## Constraints

### [C1] 維生素原料上限

$$\sum_{p \in P} r_{vp} \cdot x_p \leq A_v, \quad \forall v \in V$$

### [C2] 藥丸批次必須多於注射液批次

$$x_{\text{Pills}} \geq x_{\text{Shots}}$$

### [C3] 注射液批次上限

$$x_{\text{Shots}} \leq M$$

### [C4] 非負

$$x_p \geq 0, \quad \forall p \in P$$
