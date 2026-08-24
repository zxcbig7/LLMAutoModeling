# Template — 數學模型

> 這是 scaffold 附的示範模型，用途是讓 `Template/` 本身就是一個**符合現行規範、可 build、可求解、解已驗證**的完整專案。
> 複製到 `Projects/<Project>/` 後，本檔 MUST 換成 Phase 1 產出的真實 `Model/<Project>_Model.md`，八段格式見 `.claude/skills/modeling/model-design-guide.md` 附錄 B。

## 問題描述

某工廠要在規劃期間內生產數種品項。每個品項有期間總需求；每一天有共用的總產能上限；並不是每個品項每一天都能生產（只有列在 AllowedSlot 的組合可以）。某品項某日一旦生產，當天就算「開工」，而每個品項有最少開工天數的要求。需求不一定要全部滿足，未滿足的部分計為缺口，但缺口有上限。

要決定的是：每個允許的 (品項, 日期) 組合各生產多少、當天是否開工，使得總缺口懲罰最小。

**敘述逐句表**

| ID | 敘述 | 單位 | 換算 |
| --- | --- | --- | --- |
| S001 | 規劃對象是數個品項與數個日期 | — | — |
| S002 | 只有列在 AllowedSlot 的 (品項, 日期) 組合可以生產 | — | — |
| S003 | 每個品項有期間總需求量 | unit | — |
| S004 | 每一天所有品項的產量合計不得超過當日總產能 | unit/day | — |
| S005 | 某品項某日生產量大於 0 時，該 (品項, 日期) 必須標記為開工 | — | — |
| S006 | 每個品項有最少開工天數 | day | — |
| S007 | 未滿足的需求記為缺口，且每個品項的缺口有上限 | unit | — |
| S008 | 目標是最小化總缺口懲罰 | TWD | — |
| S009 | 生產量為整數，開工與否為 0/1，缺口為連續量 | — | — |

## Terminology Mapping Table

| Term | 中文語意 | Role | Unit | Derived? | Raw phrase | 來源 S-id |
| --- | --- | --- | --- | --- | --- | --- |
| Item | 品項 | parameter | — | No | 「數個品項」 | S001 |
| Date | 規劃日期 | parameter | — | No | 「數個日期」 | S001 |
| AllowedSlot | 允許生產的 (品項, 日期) 組合 | parameter | — | No | 「只有列在 AllowedSlot 的組合可以生產」 | S002 |
| Demand | 品項期間總需求 | parameter | unit | No | 「期間總需求量」 | S003 |
| DailyCapacity | 每日總產能 | parameter | unit/day | No | 「當日總產能」 | S004 |
| MinActiveDays | 最少開工天數 | parameter | day | No | 「最少開工天數」 | S006 |
| MaxShortage | 缺口上限 | parameter | unit | No | 「缺口有上限」 | S007 |
| BigMProduce | Produce 的最緊合法上界 | parameter | unit | No | 由 S004 的單日產能推導（見「已套用假設」3） | S005 |
| ShortagePenalty | 缺一單位的懲罰 | parameter | TWD/unit | No | 「缺口懲罰」 | S008 |
| Produce | 某品項某日的生產量 | variable | unit | No | 「各生產多少」 | S001, S009 |
| Use | 某品項某日是否開工 | variable | — | No | 「是否開工」 | S005, S009 |
| Shortage | 某品項的需求缺口 | variable | unit | No | 「未滿足的部分」 | S007, S009 |
| DemandCoverage | 產量加缺口等於需求 | constraint | — | No | 「未滿足的部分計為缺口」 | S003, S007 |
| DailyCapacity（限制） | 每日產能上限 | constraint | — | No | 「不得超過當日總產能」 | S004 |
| ProduceOnlyWhenUsed | 沒開工不能生產 | constraint | — | No | 「一旦生產就算開工」 | S005 |
| MinActiveDays（限制） | 最少開工天數 | constraint | — | No | 「最少開工天數」 | S006 |
| ShortageCap | 缺口上限 | constraint | — | No | 「缺口有上限」 | S007 |
| TotalShortagePenalty | 總缺口懲罰 | objective | TWD | No | 「最小化總缺口懲罰」 | S008 |

**子句歸類覆蓋表**

| S-id | Role | 對應 Term | 備註 |
| --- | --- | --- | --- |
| S001 | parameter | Item, Date | 集合成員 |
| S002 | parameter | AllowedSlot | 多維 Set，同時是 Produce / Use 的 domain |
| S003 | parameter | Demand | — |
| S004 | constraint | DailyCapacity | — |
| S005 | constraint | ProduceOnlyWhenUsed | — |
| S006 | constraint | MinActiveDays | — |
| S007 | constraint | ShortageCap | — |
| S008 | objective | TotalShortagePenalty | — |
| S009 | variable | Produce, Use, Shortage | 決定三支變數的型別前綴 |

## SET

| Set | 語意 | 成員範例 |
| --- | --- | --- |
| Item | 品項 | ItemA, ItemB, ItemC |
| Date | 規劃日期 | 2026-01-01, 2026-01-02, 2026-01-03 |
| AllowedSlot | 允許生產的 (品項, 日期) 組合 | (ItemA, 2026-01-01), (ItemC, 2026-01-02) |

`AllowedSlot` 是**多維 Set**：成員本身就是 tuple，語意是「這些組合存在」。它是 `Produce` / `Use` 的 domain——沒列出來的組合連變數都不建立。

## PARAM

| Param | 語意 | Dim | 值 | 單位 |
| --- | --- | --- | --- | --- |
| Demand | 品項期間總需求 | Item | ItemA=10, ItemB=8, ItemC=6 | unit |
| DailyCapacity | 每日總產能 | Date | 每日=12 | unit/day |
| MinActiveDays | 最少開工天數 | Item | ItemA=2, ItemB=1, ItemC=1 | day |
| MaxShortage | 缺口上限 | Item | ItemA=10, ItemB=8, ItemC=6 | unit |
| ShortagePenalty | 缺一單位的懲罰 | （scalar） | 5 | TWD/unit |
| BigMProduce | Produce 的最緊合法上界 | （scalar） | 12 | unit |

以上皆為**全格語意**（Demand / MinActiveDays / MaxShortage 對每個 Item、DailyCapacity 對每個 Date 都必須有值），缺格代表資料漏了，由 `TemplateSolution.ValidateData` 在建模前檢查。

## VAR

| Var | 語意 | Dim | 型別 | LB | UB |
| --- | --- | --- | --- | --- | --- |
| Produce | 某品項某日的生產量 | AllowedSlot(Item, Date) | Integer | 0 | INFTY |
| Use | 某品項某日是否開工 | AllowedSlot(Item, Date) | Binary | 0 | 1 |
| Shortage | 某品項的需求缺口 | Item | Continuous | 0 | INFTY |

## CONSTRAINT

### [C1] DemandCoverage `[Balance]` ∀ item ∈ Item

$$\sum_{(item,\,date) \in AllowedSlot} Produce_{item,\,date} + Shortage_{item} = Demand_{item}$$

每個品項的總產量加上缺口，等於它的需求。

### [C2] DailyCapacity `[UB]` ∀ date ∈ Date

$$\sum_{(item,\,date) \in AllowedSlot} Produce_{item,\,date} \le DailyCapacity_{date}$$

每天所有品項的產量合計不得超過當日總產能。

### [C3] ProduceOnlyWhenUsed `[BigM]` ∀ (item, date) ∈ AllowedSlot

$$Produce_{item,\,date} \le BigMProduce \cdot Use_{item,\,date}$$

沒開工就不能生產。`BigMProduce` 取該式最緊的合法上界＝單日總產能 12（S004），非題目明給的數字。

### [C4] MinActiveDays `[LB]` ∀ item ∈ Item

$$\sum_{(item,\,date) \in AllowedSlot} Use_{item,\,date} \ge MinActiveDays_{item}$$

每個品項至少要開工的天數。

### [C5] ShortageCap `[Range]` ∀ item ∈ Item

$$0 \le Shortage_{item} \le MaxShortage_{item}$$

缺口不得超過可容忍上限。

## OBJ

$$\min \sum_{item \in Item} ShortagePenalty \cdot Shortage_{item}$$

最小化總缺口懲罰。

## 已套用假設

1. 缺口以連續變數表示（S009 明示），生產量為整數、開工為 0/1。
2. `AllowedSlot` 沒列到的組合視為**不可生產**，因此不建立對應變數——這是模型宣告的 domain，不是資料缺漏。
3. `BigMProduce = 12` 由單日總產能推導（S004），不是題目明給；換一批資料時 MUST 一併重算（api-guide 附錄 A.3）。
4. 各 Parameter 未提上下界者一律套預設 `LB=0, UB=INFTY`。
