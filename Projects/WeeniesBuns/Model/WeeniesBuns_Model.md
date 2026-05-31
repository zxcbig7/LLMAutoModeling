# WeeniesBuns 數學模型

**問題類型：** LP（線性規劃）

---

## 集合

$$\mathcal{I} = \{\text{Frankfurter},\ \text{Bun}\}$$

---

## 參數

| 符號 | 說明 | 數值 |
| --- | --- | --- |
| $f_i$ | 每單位麵粉用量（磅） | Frankfurter $= 0$，Bun $= 0.1$ |
| $p_i$ | 每單位豬肉用量（磅） | Frankfurter $= 0.25$，Bun $= 0$ |
| $l_i$ | 每單位人工時數（分鐘） | Frankfurter $= 3$，Bun $= 2$ |
| $c_i$ | 每單位利潤（美元） | Frankfurter $= 0.88$，Bun $= 0.33$ |
| $F$ | 每週麵粉產能上限（磅） | $200$ |
| $P$ | 每週豬肉供應量（磅） | $800$ |
| $L$ | 每週人工時數上限（分鐘） | $12{,}000$ |

---

## 決策變數

$$x_i \geq 0, \quad \forall\, i \in \mathcal{I}$$

---

## 目標函數

$$\max \quad Z = \sum_{i \in \mathcal{I}} c_i \, x_i$$

---

## 限制式

$$\text{[C1] 麵粉：} \quad \sum_{i \in \mathcal{I}} f_i \, x_i \leq F$$

$$\text{[C2] 豬肉：} \quad \sum_{i \in \mathcal{I}} p_i \, x_i \leq P$$

$$\text{[C3] 人工：} \quad \sum_{i \in \mathcal{I}} l_i \, x_i \leq L$$

---

## 最佳解

$$x_{\text{Frankfurter}}^* = 3200, \quad x_{\text{Bun}}^* = 1200$$

$$Z^* = 0.88 \times 3200 + 0.33 \times 1200 = \$3{,}212.00$$

**緊束限制式：** C2（豬肉）、C3（人工）
