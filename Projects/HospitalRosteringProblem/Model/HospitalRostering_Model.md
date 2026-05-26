# HospitalRosteringProblem — 數學模型

## 問題描述

醫院排班問題：在滿足每日各班別人力需求的前提下，
最小化排班違規的加權懲罰分數（六日連工、跨組別上班、不良班別順序等）。

---

## Sets

| 符號 | 說明 |
| --- | --- |
| $E$ | 員工集合 |
| $D$ | 排班日期集合 |
| $G$ | 班別集合（含休假 O） |
| $G^+$ | 工作班別集合，$G^+ = G \setminus \{O\}$ |
| $R$ | 不良班別轉換對集合，元素為 $(g', g)$（前一天 $g'$、今天 $g$） |
| $PA$ | 預排班集合，元素為 $(e, d, g)$ |
| $CG_e$ | 員工 $e$ 的跨組別上班班別集合 |

---

## Parameters

| 符號 | 說明 |
| --- | --- |
| $\text{Demand}_{dg}$ | 日期 $d$、班別 $g$ 的人力需求，$d \in D,\ g \in G^+$ |
| $\text{Cost}_{eg}$ | 員工 $e$ 上班別 $g$ 的跨組別成本，$(e,g) \in CG$ |
| $\text{AVGOFF}$ | 員工平均休假天數下限（由資料計算） |
| $W$ | 週末天數集合，$W \subseteq D$ |
| $w_1$ | 罰分：單日休（做休做模式）|
| $w_2$ | 罰分：連休低於 2 天 |
| $w_3$ | 罰分：跨組別上班 |
| $w_4$ | 罰分：不良班別轉換（夜→早等） |
| $w_5$ | 罰分：低於平均休假 |
| $w_6$ | 罰分：連續工作 6 天 |
| $w_7$ | 罰分：週末出勤低於 4 天（彈性休） |

---

## Decision Variables

| 變數 | 說明 |
| --- | --- |
| $y_{edg} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 被排入班別 $g$，$\forall e \in E,\ d \in D,\ g \in G$ |
| $s^{\text{off1}}_{ed} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 發生「做休做」模式的指示變數 |
| $s^{\text{six}}_{ed} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 達成連續 6 天工作的指示變數 |
| $s^{\text{mis}}_{ed} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 跨組別上班的指示變數 |
| $s^{\text{ntd}}_{ed} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 發生不良班別轉換的指示變數 |
| $s^{\text{dfl}}_{ed} \in \{0,1\}$ | 員工 $e$ 在日期 $d$ 發生連休標記的指示變數 |
| $s^{\text{dlt}}_{e} \in \{0,1\}$ | 員工 $e$ 本月連休天數低於 2 天的指示變數 |
| $z^{\text{avg}}_{e} \geq 0$ | 員工 $e$ 休假天數低於平均的差距（連續） |
| $z^{\text{wkd}}_{e} \geq 0$ | 員工 $e$ 週末出勤低於 4 天的差距（連續） |

---

## Objective Function

$$\min \quad \sum_{e \in E} \sum_{d \in D} \left(
  w_1 \cdot s^{\text{off1}}_{ed}
  + w_6 \cdot s^{\text{six}}_{ed}
  + w_3 \cdot s^{\text{mis}}_{ed}
  + w_4 \cdot s^{\text{ntd}}_{ed}
\right)
+ \sum_{e \in E} \left(
  w_2 \cdot s^{\text{dlt}}_{e}
  + w_5 \cdot z^{\text{avg}}_{e}
  + w_7 \cdot z^{\text{wkd}}_{e}
\right)$$

---

## Constraints

### [C1] 每人每天只排一個班別

$$\sum_{g \in G} y_{edg} = 1, \quad \forall e \in E,\ d \in D$$

### [C2] 每日各班別人力需求

$$\sum_{e \in E} y_{edg} = \text{Demand}_{dg}, \quad \forall d \in D,\ g \in G^+$$

### [C3] 預排班固定

$$y_{edg} = 1, \quad \forall (e, d, g) \in PA$$

### [C4] 連續工作 6 天指示（滑動視窗 6 天）

$$\forall e \in E,\ d \in D,\ \tau \in [d-5, d]:$$

$$s^{\text{six}}_{ed} \leq 1 - y_{e\tau O}$$

$$s^{\text{six}}_{ed} \geq 1 - y_{e\tau O} - \sum_{\tau' \in [d-5,d]} y_{e\tau' O} + (6-1) \cdot \mathbf{1}$$

### [C5] 跨組別上班指示

$$y_{edg} \leq s^{\text{mis}}_{ed}, \quad \forall e \in E,\ d \in D,\ g \in CG_e$$

### [C6] 不良班別轉換指示

$$s^{\text{ntd}}_{ed} \geq y_{e,d-1,g'} + y_{edg} - 1, \quad \forall e \in E,\ d \in D,\ (g',g) \in R$$

### [C7] 做休做指示（連休 ≥ 2 天）

$$s^{\text{off1}}_{ed} \geq (1 - y_{e,d-2,O}) + y_{e,d-1,O} + (1 - y_{edO}) - 2$$

### [C8] 連休旗標（滑動視窗 3 天）

$$(s^{\text{dfl}}_{ed} \geq \text{連休條件，詳見 Constraint\_DoubleOffLT2})$$

### [C9] 每月至少有一次連休 2 天

$$\sum_{d \in D} s^{\text{dfl}}_{ed} + 2 \cdot s^{\text{dlt}}_{e} \geq 2, \quad \forall e \in E$$

### [C10] 休假天數不低於平均

$$\sum_{d \in D} y_{edO} + z^{\text{avg}}_{e} \geq \text{AVGOFF}, \quad \forall e \in E$$

### [C11] 週末出勤彈性（週末休假不足 4 天）

$$z^{\text{wkd}}_{e} \geq 4 - \sum_{d \in W} y_{edO}, \quad \forall e \in E$$

### [C12] 非負 / 二元

$$y_{edg} \in \{0,1\},\quad s^{(\cdot)}_{(\cdot)} \in \{0,1\},\quad z^{(\cdot)}_{e} \geq 0$$
