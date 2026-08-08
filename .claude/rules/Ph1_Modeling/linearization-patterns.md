# Linearization Patterns — constraint 手法庫

> Phase 1（Model Design）建 CONSTRAINT 時，每條先 match 下面一個 pattern，再套它的 LaTeX 原形填空——**不 freehand**。
> 符號一律語意命名；二元變數用語意名（`Open`、`Assign`）；`M` 為 Big-M（見末節，NEVER magic number）。
> 寫進 Model.md 時保持 `LHS (op) RHS` 原形，NEVER 預先移項（Coding 端 `AddLHS`/`AddRHS` 逐項對照）。

## Part A · 8 類 constraint canonical form（對應 Model Design 分類表）

### 1. UB / LB（Range）
語言線索：at most / no more than / at least。直接線性。
$$\sum_{i} Coef_i \cdot x_i \le CapacityUB \qquad \sum_{i} Coef_i \cdot x_i \ge RequireLB$$

### 2. Balance（等量平衡）
語言線索：input = output / must equal。直接等式。
$$\sum_{i \in In} Flow_i = \sum_{j \in Out} Flow_j$$

### 3. Proportional（比例範圍，NEVER 用除法）
語言線索：at least X times / no more than Y%。**交叉相乘**，NEVER `A/B <= r`。
✅ Good：$$A \le Ratio \cdot B \qquad A \ge Ratio \cdot B$$
❌ Bad：$A / B \le Ratio$（變數相除，非線性）

### 4. Implication（若 A 則 B，兩者 binary）
語言線索：if A then B / implies。「A 開 ⇒ B 必開」= `z_A <= z_B`。
$$z_A \le z_B$$

### 5. Conjunction（全部同時成立）
語言線索：only if all / must all be active。
$$\sum_{s \in S} z_s = |S| \quad\text{(等價逐條 } z_s = 1)$$

### 6. Disjunction（至少 k 個成立）
語言線索：at least one of / a minimum of。
$$\sum_{s \in S} z_s \ge k$$

### 7. Exclusive XOR（恰好選一）
語言線索：exactly one / mutually exclusive。
$$\sum_{s \in S} z_s = 1$$

### 8. Conditional Activation（Big-M 條件啟動）
語言線索：only if / can be used when。連續量 `x` 只在 binary `z` 開時可正。
$$x \le M \cdot z$$
Threshold-triggered（輸入超過門檻才啟動）：$Input \ge Threshold - M \cdot (1 - z)$。

## Part B · 非線性 → 線性 recipe（LLM 最常錯，務必查表）

### abs：$|expr| \le b$
拆兩條（NEVER 用 `abs()`）：
$$expr \le b \qquad -expr \le b$$
目標裡 `min |expr|` → 加輔助變數 `t`：$t \ge expr$、$t \ge -expr$，目標 `min t`。

### max / min in objective（加輔助變數）
`min` 目標裡壓一個 max：加 `t`，$t \ge e_i \ \forall i$，目標 `min t`。
`max` 目標裡抬一個 min：加 `t`，$t \le e_i \ \forall i$，目標 `max t`。
✅ 方向要對：min-of-max 用 `t >=`；max-of-min 用 `t <=`。

### Fixed-charge（設置成本 / 開了才能用）
語言線索：租金 / 開廠 / setup cost。`Open` binary，`Produce` 連續：
$$Produce \le M \cdot Open \qquad \text{objective} \mathrel{+}= FixedCost \cdot Open$$
M = `Produce` 的最緊上界（如總產能），寫成 PARAM。
❌ Bad：只加 `FixedCost * Open` 卻漏 `Produce <= M*Open` linking（開關與量沒綁，可白吃產能）。

### Either-Or（兩約束至少一條成立，disjunctive）
加 binary `y`，兩式各鬆一邊：
$$g_1(x) \le b_1 + M \cdot (1 - y) \qquad g_2(x) \le b_2 + M \cdot y$$

### Product 二元 × 二元：$w = z_1 \cdot z_2$
$$w \le z_1 \qquad w \le z_2 \qquad w \ge z_1 + z_2 - 1 \qquad w \in \{0,1\}$$

### Product 二元 × 連續：$w = z \cdot c$（`z` binary、$c \in [0, U]$）
$$w \le U \cdot z \qquad w \le c \qquad w \ge c - U \cdot (1 - z) \qquad w \ge 0$$

## Big-M 取值鐵律（最容易靜默出錯）

- ALWAYS `M` = 「被約束式的最緊合法上界」，由題目數據推導（總產能、最大需求…），定義成 PARAM
- NEVER magic number（`99999`）——
  - M **太小** → 砍掉合法解，solver 靜默給錯的最佳解（build/solve 都不報錯，最難抓）
  - M **太大** → LP relaxation 鬆、B&B 慢
- ✅ Good：`param BigM_Produce := TotalCapacity`（有依據）
- ❌ Bad：`x <= 1000000 * z`（憑感覺的大數）
</content>
