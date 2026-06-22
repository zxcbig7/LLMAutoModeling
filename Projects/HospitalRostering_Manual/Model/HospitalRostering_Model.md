# 醫院護理人員月排班 — 數學模型（MILP，加權懲罰）

> Canonical 模型。`HospitalRostering_Manual` 與 `HospitalRostering_Generator` 兩專案**字字共用此檔**。
> 精神：少數硬約束保證可行，其餘規則靠**指示變數 + 目標懲罰**軟性逼近。先定稿數學，再翻譯成 code。

## Sets（集合）

$$ E = \{\text{E1},\dots,\text{E16}\} \qquad D = \{\text{1/1},\dots,\text{1/31}\} \qquad G = \{O,D,E,N,C\} \qquad G^{+}=G\setminus\{O\} $$

組合型集合：

- $R=\{(g',g)\}$：不良班別轉換對（昨天 $g'$ → 今天 $g$）
- $PA=\{(e,d,g)\}$：預排班（固定指派）
- $CG_e$：員工 $e$ 的跨組別班別集合（非主責、非 Backup、非休）
- $W\subseteq D$：週末日

## Parameters（已知參數）

| 符號 | 意義 | C# 來源 |
|------|------|---------|
| $\text{Demand}_{d,g}$ | 日期 $d$、工作班別 $g\in G^+$ 的人力需求 | `Parameter_ShiftDemand` |
| $\text{AVGOFF}$ | 每位員工休假天數下限 | Dataload 推算 |
| $(e,d,g)\in PA$ | 預排班 | `Parameter_PreAssign` |
| $(g',g)\in R$ 成本 | 不良轉換罰分 | `Parameter_NightToDay` |
| $CG_e$ 成本 | 跨組別罰分 | `Parameter_CrossGroup` / `Parameter_BackupGroup` |
| $w_1,\dots,w_7$ | 七種違規罰分權重 | Dataload `Penalty_*` |

## Decision Variables（決策變數）

| 變數 | 符號 | 型別 | = 1 / > 0 代表 | C# |
|------|------|------|----------------|-----|
| 排班 | $y_{e,d,g}$ | Binary | 員工 $e$ 在 $d$ 排班別 $g$（核心決策） | `VariableB_ShiftAssign` |
| 做一休一做 | $s^{off1}_{ed}$ | Binary | 出現「上班-休-上班」 | `VariableB_Off1Day` |
| 連六天 | $s^{six}_{ed}$ | Binary | 達成連續工作 6 天 | `VariableB_SixDayWork` |
| 跨組別 | $s^{mis}_{ed}$ | Binary | 跨組別支援 | `VariableB_GroupMismatch` |
| 不良轉換 | $s^{ntd}_{ed}$ | Binary | 發生不良班別轉換 | `VariableB_NightToDay` |
| 連休旗標 | $s^{dfl}_{ed}$ | Binary | 形成一段「連休 2 天」 | `VariableB_DoubleOffFlag` |
| 整月無連休 | $s^{dlt}_{e}$ | Binary | 整月一次連休 2 天都沒有 | `VariableB_DoubleOffLT2` |
| 低於平均 | $z^{avg}_{e}$ | Continuous ≥0 | 休假比 AVGOFF 少的天數 | `VariableX_BelowAVG` |
| 週末不足 | $z^{wkd}_{e}$ | Continuous ≥0 | 週末休假比 4 天少的天數 | `VariableX_WeekendLT4` |

## Objective（目標式）

最小化七種違規的加權總和：

$$ \min\ \sum_{e\in E}\sum_{d\in D}\Big( w_1 s^{off1}_{ed} + w_6 s^{six}_{ed} + w_3 s^{mis}_{ed} + w_4 s^{ntd}_{ed} \Big) \;+\; \sum_{e\in E}\Big( w_2 s^{dlt}_{e} + w_5 z^{avg}_{e} + w_7 z^{wkd}_{e} \Big) $$

## Constraints（限制式）

**硬約束 C1–C3**

- **C1 每人每天恰一班**（`Constraint_OneGroup`，=）
  $$ \sum_{g\in G} y_{e,d,g} = 1 \qquad \forall e\in E,\ d\in D $$
- **C2 每日各班別需求**（`Constraint_FullfillDemand`，=）
  $$ \sum_{e\in E} y_{e,d,g} = \text{Demand}_{d,g} \qquad \forall d\in D,\ g\in G^{+} $$
- **C3 預排班固定**（`Constraint_PreAssign`，=）
  $$ y_{e,d,g} = 1 \qquad \forall (e,d,g)\in PA $$

**指示連動 C4–C9**

- **C4 連續工作 6 天指示**（`Constraint_SixDayWork`，滑動視窗，≤ 與 ≥）。令 $W_6(d)=\{d-5,\dots,d\}$：
  $$ s^{six}_{ed} \le 1 - y_{e,\tau,O}\ \ \forall \tau\in W_6(d) \qquad s^{six}_{ed} \ge 1 - \sum_{\tau\in W_6(d)} y_{e,\tau,O} $$
- **C5 跨組別支援指示**（`Constraint_CrossGroup`，≤）
  $$ y_{e,d,g} \le s^{mis}_{ed} \qquad \forall e\in E,\ d\in D,\ g\in CG_e $$
- **C6 不良班別轉換指示**（`Constraint_NightToDay`，≥）
  $$ s^{ntd}_{ed} \ge y_{e,d-1,g'} + y_{e,d,g} - 1 \qquad \forall e,\ d,\ (g',g)\in R $$
- **C7 做一休一做指示**（`Constraint_OffOneDay`，≥）
  $$ s^{off1}_{ed} \ge (1 - y_{e,d-2,O}) + y_{e,d-1,O} + (1 - y_{e,d,O}) - 2 $$
- **C8 連休 2 天旗標**（`Constraint_DoubleOffLT2` 之一，≥）
  $$ s^{dfl}_{ed} \ge y_{e,d,O} + y_{e,d-1,O} + (1 - y_{e,d-2,O}) - 2 $$
- **C9 每月至少一次連休 2 天**（`Constraint_DoubleOffLT2` 之二，≥）
  $$ \sum_{d\in D} s^{dfl}_{ed} + 2\, s^{dlt}_{e} \ge 2 \qquad \forall e\in E $$

**差距量 C10–C11**

- **C10 休假不低於平均**（`Constraint_BelowAVG`，≥）
  $$ \sum_{d\in D} y_{e,d,O} + z^{avg}_{e} \ge \text{AVGOFF} \qquad \forall e\in E $$
- **C11 週末休假彈性**（`Constraint_WeekendLT4`，≥）
  $$ z^{wkd}_{e} \ge 4 - \sum_{d\in W} y_{e,d,O} \qquad \forall e\in E $$

## 限制式 → 框架方法對照

| 限制式 | 分類 | 方向 | 框架方法 |
|--------|------|------|----------|
| C1 每天一班 | Balance | = | `CreateEqual` |
| C2 需求 | Balance | = | `CreateEqual` |
| C3 預排班 | Fixing | = | `CreateEqual` |
| C4 連六天 | Implication（上下界） | ≤ , ≥ | `CreateLessEqual` + `CreateGreatEqual` |
| C5 跨組別 | Implication | ≤ | `CreateLessEqual`（變數移 RHS） |
| C6 不良轉換 | Implication | ≥ | `CreateGreatEqual`（變數移 RHS） |
| C7 做休做 | Implication | ≥ | `CreateGreatEqual` |
| C8 連休旗標 | Implication | ≥ | `CreateGreatEqual` |
| C9 至少連休 | Disjunction | ≥ | `CreateGreatEqual` |
| C10 低於平均 | Soft / 差距 | ≥ | `CreateGreatEqual` |
| C11 週末彈性 | Soft / 差距 | ≥ | `CreateGreatEqual` |

> **天條**：所有數值（需求、罰分權重、跨組別成本、AVGOFF）一律放 `Parameter` / `Dataload`，Constraint / Objective 只能透過 dataload 查詢係數，**禁止寫死裸數字**。
