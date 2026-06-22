# AML Verify — Max Weighted Independent Set

驗證 [04_aml.md](04_aml.md) 對照問題描述，結論：**通過，無需修正**。

## 檢查項

| 檢查 | 結果 |
|---|---|
| 目標方向（最大化總權重） | ✅ `max Σ w_i x_i` |
| 變數型別（二元決策） | ✅ `x_i ∈ {0,1}` |
| 獨立集定義（相鄰不可同選） | ✅ `x_i + x_j ≤ 1 ∀(i,j)∈E` 完整涵蓋 |
| LHS/RHS 方向 | ✅ LHS=`x_i+x_j`、RHS=`1`、`≤`，無移項/改號 |
| 數值保真 | ✅ 權重直接來自實例（隨機種子固定），無四捨五入 |
| 遺漏約束 | ✅ MWIS 僅需邊衝突 + 二元，無其他隱含約束 |

## 正確性 gate 的期望性質（供 tuning 驗收）

- 任何 **solver 層** tuning（mipEmphasis / cuts / probe / 決定論）**不得改變最佳目標值**；所有 trial 的 `ObjectiveValue` 必須一致，否則代表模型/數值被動到。
- 解必為合法獨立集：被選節點集合內任兩點不得相鄰（由 (C1) 保證）。
