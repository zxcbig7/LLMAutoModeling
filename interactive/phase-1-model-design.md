# Phase 1 · Model Design — 建模規範

> 天條見 [`README.md`](README.md)；constraint linearization 手法見 [`linearization-patterns.md`](linearization-patterns.md)。

## 系統脈絡

三階段的第一階段：把自然語言題目轉成完整、無歧義、**程式好轉譯**的數學模型文件 `Projects/<Project>/Model/<Project>_Model.md`。
方法論：**4 次漸進降維**（去故事化 → 語義判別 → 結構抽取 → 建模自驗），每階段只降一層抽象，少一步錯步步錯。
本階段**只輸出 Model.md（+ Glossary.md）**。出口 gate：使用者明確說「模型確認」或「開始實作」才進 Phase 2。

## 硬規則

- MUST 依 1a→1b→1c→1d 四階段順序推進，NEVER 跳過 1a/1b 直接上符號 —— Why: 資料沒清乾淨、子句沒歸類就符號化，等於在雜訊上建模
- MUST Model.md 五段固定順序：**SET → PARAM → VAR → CONSTRAINT → OBJ**，數學式用 LaTeX（`$...$` / `$$...$$`）
- MUST 每個元素都帶「程式轉譯 metadata」（見下）—— Why: Phase 2 是純機械轉譯，metadata 缺一項 Coding 就得猜
- NEVER 在 CONSTRAINT 預先移項 / 化簡 / 翻方向 —— ALWAYS 寫成 `LHS (op) RHS` 原形，左邊項留左邊、右邊項留右邊 —— Why: Coding 端 `AddLHS`/`AddRHS` 要逐項對照
- MUST 每條 constraint 標一個 pattern tag（見 linearization-patterns 8 類）—— Why: 標了就照該 pattern 的 template 填空，不 freehand，這是「不亂寫」的機制
- MUST 所有歧義在本階段清完；預設慣例表內項目直接套用不追問，表外不確定就問
- NEVER 在本階段討論 soft constraint / penalty 放鬆 —— ALWAYS 預設 Hard constraint，放鬆屬 Phase 3

## 4 階段建模法（每階段交付一個中間產物，累積成 Model.md）

### 1a · 去故事化 + 單位正規化（純自然語言，NEVER 引入符號）
- 去背景故事，只留資料與邏輯；表格轉成宣告句
- 每個數字都帶單位；單位不一致（時/分、噸/公斤）就換算成單一單位並標明換算
- 產物：一段乾淨、self-contained 的問題敘述

### 1b · 語義判別 + Terminology Table（防漏句、防亂設變數）
- **語義判別鐵律**：1a 敘述的每個子句，強制歸類成 `parameter` / `variable` / `derived` / `constraint` / `objective` 之一；與模型無關的子句明確標 `irrelevant`，NEVER 靜默略過
- **單位推理守則**：NEVER 自動推導 derived 值（如「300元/班 ÷ 10小時/班 = 時薪」），除非題目明說；真需要 derived → 另立一列標 `derived` + 註明推導來源
- 產物：Terminology Mapping Table（見下模板）

### 1c · 結構抽取 → Model.md 五段（程式好轉譯角度寫）
- 依下方元素 metadata 逐段填 SET / PARAM / VAR / CONSTRAINT / OBJ
- 每條 constraint 標 pattern tag，邏輯 / 非線性約束查 linearization-patterns 套 template
- 每個 `sum` 標明 index 範圍（`∀` 哪個 set、over 哪個 set）

### 1d · 建模自驗 gate（交付前對照自驗清單，全過才等使用者確認）
跑完列「已套用的預設假設」讓使用者一併確認。

## 元素 metadata（每段「程式好轉譯」的必填欄，決定 Phase 2 類別）

### Terminology Mapping Table（1b 產物）
| Term | 中文語意 | Role | Unit | Derived? | Raw phrase |
| --- | --- | --- | --- | --- | --- |
| MachineCapacity | 機器產能 | parameter | hours | No | "each machine ... up to 40 hours" |
| Produce | 生產量 | variable | units | No | "how many should be produced" |

Role ∈ {parameter, variable, derived, constraint, objective, irrelevant}。

### SET
| Set | 語意 | 成員範例 | → 程式 |
| --- | --- | --- | --- |
| GlassType | 玻璃種類 | Regular, Tempered | `List<string>`，由 Parameter 衍生 |

### PARAM（標 Dim → 決定 Parameter 類別 property 名）
| Param | 語意 | Dim | 值 | → 程式 |
| --- | --- | --- | --- | --- |
| DemandQty | 需求量 | GlassType | Regular=60, Tempered=30 | `Parameter_Demand`（QTY 欄）|

### VAR（標型別 + LB/UB → 決定 VariableB_/I_/X_ + bound overload）
| Var | 語意 | Dim | 型別 | LB | UB |
| --- | --- | --- | --- | --- | --- |
| Produce | 生產量 | GlassType | Continuous | 0 | INFTY |
| Assign | 是否指派 | Employee, Date | Binary | 0 | 1 |

### CONSTRAINT（LHS op RHS 原形 + pattern tag + Dim）
```markdown
### Capacity `[UB]` ∀ machine
$$\sum_{g \in GlassType} UsageRate_g \cdot Produce_g \le MachineCapacity$$
每台機器總使用率不得超過產能。
```
- pattern tag = linearization-patterns 的 8 類之一
- ✅ Good：`x <= a * y`（原形）　❌ Bad：`x - a*y <= 0`（預先移項，Coding 對不回去）

### OBJ（方向 + 所有項在 LHS）
$$\max \sum_{g \in GlassType} Profit_g \cdot Produce_g$$

## 命名 good/bad（符號直接對應程式類別名）
✅ Good：`Assign_{Employee,Date}`、`MachineCapacity`、`Produce_{GlassType}`
❌ Bad：`x_{i,j}`（單字母 + 無語意 index）、`c1`（看不出約束語意）
Why: Coding 類別名由符號機械對應（`VariableB_Assign`），符號沒語意 → 程式碼跟著沒語意。

## 預設慣例（直接套用，不追問）
| 項目 | 預設行為 |
| --- | --- |
| Index domain 邊界 | Dataset 已清洗，LINQ 直接篩選 |
| 參數未定義的組合 | 填不影響模型的預設值（通常 0 或略過該條）|
| Soft vs Hard | 一律 Hard；放鬆屬 Phase 3 |
| Big-M 值 | 取「該式最緊的合法上界」，見 linearization-patterns Big-M 節 |
| 變數 LB/UB | 題目沒提 → `LB=0, UB=INFTY`；比例/百分比變數 → `0..1` |
| 目標函數方向 | 描述必含；真沒提才追問 |

## 仍需明確確認（追問）
| 歧義類型 | 要問的問題 |
| --- | --- |
| Linearization 選擇 | 同一邏輯有多種等價 formulation、效能差異大 → 指定哪種 |
| Time boundary | 時間序列有無 wrap-around（末期接回首期）？ |
| 不熟悉的術語 | Glossary.md 查無 → 必問 |
| 子句歸類不明 | 1b 判不出 role（像資料又像約束）→ 問清楚再歸類 |

## 1d 建模自驗（交付前逐點對照，全過才 gate）
- [ ] 1a：每個數字都帶單位，單位不一致已換算標明
- [ ] 1b：每個子句都有 role，無句靜默略過；無自動推導的 derived
- [ ] **宣告先於使用**：CONSTRAINT / OBJ 出現的每個符號，都已在 SET/PARAM/VAR 宣告
- [ ] **每個數字都是具名 PARAM**：CONSTRAINT / OBJ 內無裸數字
- [ ] 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
- [ ] 每條 CONSTRAINT 是 LHS op RHS 原形（未預先移項）+ 標了 pattern tag + Dim
- [ ] 無單一字母符號；符號皆語意命名
- [ ] 列出「已套用的預設假設」

## Fatal
- NEVER 本階段產生任何 `.cs`
- NEVER 用單一字母符號命名模型元素
- NEVER 自行詮釋不清楚的術語（Glossary 查無 → 問）
- NEVER 在 Model.md 預先移項 / 化簡 constraint（破壞 Coding 的逐項對照）
</content>
