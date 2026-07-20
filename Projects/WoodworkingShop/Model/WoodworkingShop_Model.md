# WoodworkingShop — 數學模型（Phase 1 產物，待使用者確認）

## 1a · 去故事化 + 單位正規化

- 產品兩種：Desk（書桌）、Bookcase（書櫃）。
- Desk：單位利潤 900 元/張；消耗木工 3 小時/張；消耗上漆 1 小時/張。
- Bookcase：單位利潤 1200 元/座；消耗木工 4 小時/座；消耗上漆 2 小時/座。
- 每週木工可用時數上限 240 小時。
- 每週上漆可用時數上限 100 小時。
- 決策：每週各生產多少數量。
- 目標：最大化每週總利潤。

單位一致性：所有時間皆為「小時」，無需換算；金額單位「元」；規劃期為「每週」單一期，無跨期結轉。

## 1b · 語義判別 + Terminology Mapping Table

| Term | 中文語意 | Role | Unit | Derived? | Raw phrase |
|---|---|---|---|---|---|
| ProductType | 產品種類 | parameter | - | No | "生產書桌和書櫃" |
| ResourceType | 工序資源種類 | parameter | - | No | "木工"、"上漆" |
| UnitProfit | 單位利潤 | parameter | 元/單位 | No | "書桌每張利潤 900 元"、"書櫃每座利潤 1200 元" |
| ResourceUsageRate | 單位資源耗用率 | parameter | 小時/單位 | No | "需要 3 小時木工和 1 小時上漆"、"4 小時木工和 2 小時上漆" |
| ResourceCapacity | 每週資源可用時數 | parameter | 小時/週 | No | "每週木工上限 240 小時、上漆上限 100 小時" |
| Produce | 每週生產數量 | variable | 單位/週 | No | "求每週該生產幾張書桌、幾座書櫃" |
| ResourceCapacityLimit | 資源時數不得超上限 | constraint | - | No | "每週木工上限 240 小時、上漆上限 100 小時" |
| TotalProfit | 每週總利潤 | objective | 元/週 | No | "利潤最大" |
| 「一家木工坊」 | 場景背景 | irrelevant | - | - | "一家木工坊生產" |

無自動推導的 derived 值（未把利潤換算成時薪、未推導單位成本）。

## 2 · SET

| Set | 語意 | 成員 | → 程式 |
|---|---|---|---|
| ProductType | 產品種類 | Desk, Bookcase | `List<string>`，由 `Parameter_UnitProfit` 衍生 |
| ResourceType | 工序資源種類 | Carpentry, Painting | `List<string>`，由 `Parameter_ResourceCapacity` 衍生 |

## 3 · PARAM

| Param | 語意 | Dim | 值 | → 程式 |
|---|---|---|---|---|
| UnitProfit | 單位利潤（元/單位） | ProductType | Desk=900, Bookcase=1200 | `Parameter_UnitProfit`（QTY 欄） |
| ResourceUsageRate | 單位耗用時數（小時/單位） | ProductType, ResourceType | (Desk,Carpentry)=3, (Desk,Painting)=1, (Bookcase,Carpentry)=4, (Bookcase,Painting)=2 | `Parameter_ResourceUsageRate`（QTY 欄） |
| ResourceCapacity | 每週可用時數（小時） | ResourceType | Carpentry=240, Painting=100 | `Parameter_ResourceCapacity`（QTY 欄） |

## 4 · VAR

| Var | 語意 | Dim | 型別 | LB | UB |
|---|---|---|---|---|---|
| Produce | 每週生產數量 | ProductType | Integer | 0 | INFTY |

→ 程式：`VariableI_Produce`（property `PRODUCT_TYPE`）。

## 5 · CONSTRAINT

### ResourceCapacityLimit `[UB]` ∀ ResourceType

$$\sum_{p \in ProductType} ResourceUsageRate_{p,r} \cdot Produce_p \le ResourceCapacity_r \quad \forall r \in ResourceType$$

每項工序每週投入的總時數，不得超過該工序的每週可用時數上限。

- 展開（木工）：$3 \cdot Produce_{Desk} + 4 \cdot Produce_{Bookcase} \le 240$
- 展開（上漆）：$1 \cdot Produce_{Desk} + 2 \cdot Produce_{Bookcase} \le 100$
- → 程式：`Constraint_ResourceCapacityLimit`，約束名 `ResourceCapacityLimit@{ResourceType}`

## 6 · OBJ

$$\max \sum_{p \in ProductType} UnitProfit_p \cdot Produce_p$$

每週總利潤最大化。→ 程式：`ObjectiveFunction`（Maximize）。

## 7 · 1d 建模自驗

- [x] 每個數字都帶單位，單位一致（小時 / 元 / 單位）
- [x] 每個子句都有 role，背景句已標 irrelevant，無靜默略過
- [x] 宣告先於使用：CONSTRAINT / OBJ 的符號皆已在 SET/PARAM/VAR 宣告
- [x] CONSTRAINT / OBJ 內無裸數字（900/1200/3/4/1/2/240/100 全部具名為 PARAM）
- [x] VAR 標了型別 + LB/UB；PARAM 標了 Dim
- [x] CONSTRAINT 為 `LHS ≤ RHS` 原形，未預先移項；標了 pattern tag `[UB]` 與 Dim
- [x] 無單一字母符號
- [x] 已列出預設假設（見下）

### 已套用的預設假設（請一併確認）

1. **Produce 設為 Integer**：書桌 / 書櫃為不可分割成品，故取整數變數。若你要的是 LP 教科書解（允許分數產量）→ 說一聲，改 `VariableX_Produce`（Continuous）。
2. 無市場需求上限、無最低產量要求、無產品間耦合關係。
3. 木工與上漆為互相獨立的兩種資源，不可互相支援、無加班 / 外包選項。
4. 利潤為單位邊際利潤，無固定成本 / 設置成本（不需 fixed-charge 結構）。
5. 全部為 Hard constraint（放鬆屬 Phase 3）。
6. 單週單期規劃，無存貨結轉、無 wrap-around。

## 8 · 手算預期解（Phase 2 解驗證用）

兩條約束交點：$3D + 4B = 240$ 與 $D + 2B = 100$ → $Produce_{Desk} = 40$、$Produce_{Bookcase} = 30$，總利潤 $= 900 \cdot 40 + 1200 \cdot 30 = 72000$ 元。

★ **注意存在多重最佳解**：目標係數比 $900 : 1200 = 3 : 4$ 與木工約束係數比相同（目標函數平行於木工約束），故 $(Desk, Bookcase) = (80, 0)$ 亦得 $900 \cdot 80 = 72000$ 元，且該線段上所有整數點皆為最佳解。驗證時 MUST 以**目標值 72000** 為判準，NEVER 以特定變數組合為判準（solver 回哪個頂點取決於內部路徑）。

代回檢查 $(40, 30)$：木工 $3 \cdot 40 + 4 \cdot 30 = 240 \le 240$（緊）；上漆 $1 \cdot 40 + 2 \cdot 30 = 100 \le 100$（緊）。
代回檢查 $(80, 0)$：木工 $3 \cdot 80 = 240 \le 240$（緊）；上漆 $1 \cdot 80 = 80 \le 100$（鬆）。
LP relaxation bound = 72000，整數解 = 72000 ≤ bound，sanity 通過。
