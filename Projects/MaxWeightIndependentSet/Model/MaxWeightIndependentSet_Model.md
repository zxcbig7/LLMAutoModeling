# MaxWeightIndependentSet 數學模型

**問題類型：** IP（整數規劃，二元決策變數）

---

## 問題描述

給定一個無向圖 $G=(\mathcal{V},\mathcal{E})$，每個節點 $i \in \mathcal{V}$ 有非負權重 $w_i$。從 $\mathcal{V}$ 中選出一個節點子集合，使子集合內任兩節點都不相鄰（即為一個「獨立集」），並最大化子集合內節點的權重總和。此為圖論中的 NP-hard 組合最佳化問題。

參考實例：$G(n,p)$ 隨機圖，節點數 $n=250$、邊機率 $p=0.30$，固定亂數種子 42（完全可重現），共生成 9275 條邊；已知最佳目標值 1332（選出 16 個節點）。實例規模可由環境變數覆寫（`MWIS_N` / `MWIS_P` / `MWIS_SEED`）。

---

## 集合（Sets）

$$\mathcal{V} = \{1, 2, \dots, n\}$$

節點集合，$n$ 為圖中節點數（參考實例 $n=250$）。

$$\mathcal{E} = \{(i,j) \mid i,j \in \mathcal{V},\ i<j,\ i \text{ 與 } j \text{ 相鄰}\} \subseteq \mathcal{V} \times \mathcal{V}$$

邊集合，無向圖每條邊只列一次；由 $G(n,p)$ 模型對每個節點對各自獨立以機率 $p$ 生成。

---

## 參數（Parameters）

| 符號 | 說明 | 數值來源 |
|---|---|---|
| $w_i$ | 節點 $i$ 的權重 | 隨機整數 $\in [1,100]$，依固定種子生成，不四捨五入 |

---

## 決策變數（Decision Variables）

$$x_i \in \{0,1\}, \quad \forall\, i \in \mathcal{V}$$

$x_i=1$ 表示節點 $i$ 入選獨立集，$x_i=0$ 表示不選。

---

## 目標函數（Objective）

$$\max \quad Z = \sum_{i \in \mathcal{V}} w_i x_i$$

最大化被選入獨立集的節點權重總和。

---

## 限制式（Constraints）

$$\text{[C1] 邊衝突（Edge Conflict）：} \quad x_i + x_j \leq 1, \quad \forall\, (i,j) \in \mathcal{E}$$

語意：對每一條邊 $(i,j) \in \mathcal{E}$，兩端點不可同時入選——確保被選節點集合內任兩點不相鄰，為合法獨立集。LHS $=x_i+x_j$（係數皆為 $+1$），RHS $=1$，方向 $\leq$，不移項、不改號、不翻轉方向。

---

## 附錄：模型驗證紀錄

數學模型驗證（對照原問題描述），結論：**通過，無需修正**。

### 檢查項

| 檢查 | 結果 |
|---|---|
| 目標方向（最大化總權重） | ✅ `max Σ w_i x_i` |
| 變數型別（二元決策） | ✅ `x_i ∈ {0,1}` |
| 獨立集定義（相鄰不可同選） | ✅ `x_i + x_j ≤ 1 ∀(i,j)∈E` 完整涵蓋 |
| LHS/RHS 方向 | ✅ LHS=`x_i+x_j`、RHS=`1`、`≤`，無移項/改號 |
| 數值保真 | ✅ 權重直接來自實例（隨機種子固定），無四捨五入 |
| 遺漏約束 | ✅ MWIS 僅需邊衝突 + 二元，無其他隱含約束 |

### 正確性 gate 的期望性質（供 tuning 驗收）

- 任何 **solver 層** tuning（mipEmphasis / cuts / probe / 決定論）**不得改變最佳目標值**；所有 trial 的 `ObjectiveValue` 必須一致，否則代表模型/數值被動到。
- 解必為合法獨立集：被選節點集合內任兩點不得相鄰（由 (C1) 保證）。
