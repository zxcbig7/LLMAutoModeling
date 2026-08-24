# CandyBlending — 數學模型

## 問題描述

某糖果廠以三種原料（A、B、C）配製出三種牌號的糖果（甲、乙、丙）。每個牌號對某些原料的**含量百分比**有下限或上限要求；每種原料每月可用量有上限。每種牌號有各自的單位加工費與單位售價，每種原料有各自的單位成本。

要決定的是：這個月每種原料各投入多少 kg 到每個牌號，使得「售價收入 − 加工費 − 原料成本」的月獲利最大；由此得出三種牌號各應生產多少 kg。

**敘述逐句表**（1a 去故事化 + 單位正規化的產物，供下方 role 覆蓋表逐句對照）

| ID | 敘述 | 單位 | 換算 |
| --- | --- | --- | --- |
| S001 | 工廠以三種原料加工出三種牌號的糖果 | — | — |
| S002 | 原料共三種，分別稱為 A、B、C | — | — |
| S003 | 糖果牌號共三種，分別稱為甲、乙、丙 | — | — |
| S004 | 甲牌號糖果中原料 A 的含量不低於 60% | ratio | 60% = 0.60 |
| S005 | 甲牌號糖果中原料 C 的含量不高於 20% | ratio | 20% = 0.20 |
| S006 | 乙牌號糖果中原料 A 的含量不低於 30% | ratio | 30% = 0.30 |
| S007 | 乙牌號糖果中原料 C 的含量不高於 50% | ratio | 50% = 0.50 |
| S008 | 丙牌號糖果中原料 C 的含量不高於 60% | ratio | 60% = 0.60 |
| S009 | 表中原料 B 對甲、乙、丙三個牌號的含量欄皆為空白 | — | — |
| S010 | 表中原料 A 對丙牌號的含量欄為空白 | — | — |
| S011 | 原料 A 的成本為 2.00 元/kg | 元/kg | — |
| S012 | 原料 B 的成本為 1.50 元/kg | 元/kg | — |
| S013 | 原料 C 的成本為 1.00 元/kg | 元/kg | — |
| S014 | 原料 A 每月限制用量為 2000 kg | kg/month | — |
| S015 | 原料 B 每月限制用量為 2500 kg | kg/month | — |
| S016 | 原料 C 每月限制用量為 1200 kg | kg/month | — |
| S017 | 甲牌號的加工費為 0.50 元/kg | 元/kg | — |
| S018 | 乙牌號的加工費為 0.40 元/kg | 元/kg | — |
| S019 | 丙牌號的加工費為 0.30 元/kg | 元/kg | — |
| S020 | 甲牌號的售價為 3.40 元/kg | 元/kg | — |
| S021 | 乙牌號的售價為 2.85 元/kg | 元/kg | — |
| S022 | 丙牌號的售價為 2.25 元/kg | 元/kg | — |
| S023 | 要決定每月三種牌號糖果各生產多少 kg | kg/month | — |
| S024 | 目標是使該廠獲利最大 | 元/month | — |
| S025 | 題目要求建立的是線性規劃模型 | — | — |

## Terminology Mapping Table

| Term | 中文語意 | Role | Unit | Derived? | Raw phrase | 來源 S-id |
| --- | --- | --- | --- | --- | --- | --- |
| RawMaterial | 原料種類 | parameter | — | No | 「原料 A、B、C」 | S002 |
| CandyBrand | 糖果牌號 | parameter | — | No | 「三種不同牌號的糖果甲、乙、丙」 | S003 |
| MinContentRatio | 某原料在某牌號中的含量下限比例 | parameter | ratio | No | 「$\geq 60\%$」「$\geq 30\%$」 | S004, S006 |
| MaxContentRatio | 某原料在某牌號中的含量上限比例 | parameter | ratio | No | 「$\leq 20\%$」「$\leq 50\%$」「$\leq 60\%$」 | S005, S007, S008 |
| MaterialCost | 原料單位成本 | parameter | 元/kg | No | 「原料成本(元/kg)」 | S011, S012, S013 |
| MonthlySupplyLimit | 原料每月限制用量 | parameter | kg/month | No | 「每月限制用量(kg)」 | S014, S015, S016 |
| ProcessingCost | 牌號單位加工費 | parameter | 元/kg | No | 「加工費(元/kg)」 | S017, S018, S019 |
| SellingPrice | 牌號單位售價 | parameter | 元/kg | No | 「售價(元/kg)」 | S020, S021, S022 |
| Blend | 投入某原料到某牌號的月用量 | variable | kg/month | No | 「用原料 A、B、C 加工成三種牌號」 | S001 |
| Produce | 某牌號的月產量 | variable | kg/month | No | 「每月生產這三種牌號糖果各多少 kg」 | S023 |
| MaterialAvailability | 原料月用量不得超過限制用量 | constraint | — | No | 「每月限制用量」 | S014, S015, S016 |
| BrandMassBalance | 牌號產量等於投入該牌號的原料總量 | constraint | — | No | 「用原料 … 加工成 …」（含量百分比的分母定義） | S001, S004 |
| MinContent | 含量下限限制 | constraint | — | No | 「$\geq 60\%$」「$\geq 30\%$」 | S004, S006 |
| MaxContent | 含量上限限制 | constraint | — | No | 「$\leq 20\%$」「$\leq 50\%$」「$\leq 60\%$」 | S005, S007, S008 |
| TotalProfit | 月獲利（售價收入 − 加工費 − 原料成本） | objective | 元/month | No | 「才能使其獲利最大」 | S024 |

**子句歸類覆蓋表**（1a 每個 S-id 恰好出現一次）

| S-id | Role | 對應 Term | 備註 |
| --- | --- | --- | --- |
| S001 | variable | Blend | 「用原料加工成牌號」＝投入決策存在 |
| S002 | parameter | RawMaterial | 集合成員 |
| S003 | parameter | CandyBrand | 集合成員 |
| S004 | constraint | MinContent | — |
| S005 | constraint | MaxContent | — |
| S006 | constraint | MinContent | — |
| S007 | constraint | MaxContent | — |
| S008 | constraint | MaxContent | — |
| S009 | irrelevant | — | 空白格代表該組合無含量限制，不產生任何限制式（見「已套用假設」3） |
| S010 | irrelevant | — | 同上 |
| S011 | parameter | MaterialCost | — |
| S012 | parameter | MaterialCost | — |
| S013 | parameter | MaterialCost | — |
| S014 | constraint | MaterialAvailability | — |
| S015 | constraint | MaterialAvailability | — |
| S016 | constraint | MaterialAvailability | — |
| S017 | parameter | ProcessingCost | — |
| S018 | parameter | ProcessingCost | — |
| S019 | parameter | ProcessingCost | — |
| S020 | parameter | SellingPrice | — |
| S021 | parameter | SellingPrice | — |
| S022 | parameter | SellingPrice | — |
| S023 | variable | Produce | 題目問的就是這個量 |
| S024 | objective | TotalProfit | — |
| S025 | irrelevant | — | 對建模形式的要求（線性規劃），不是模型內的限制；已反映為變數型別 Continuous |

## SET

| Set | 語意 | 成員範例 |
| --- | --- | --- |
| RawMaterial | 原料種類 | MaterialA, MaterialB, MaterialC |
| CandyBrand | 糖果牌號 | BrandJia, BrandYi, BrandBing |

牌號成員對應題目的甲 = `BrandJia`、乙 = `BrandYi`、丙 = `BrandBing`；原料成員對應 A = `MaterialA`、B = `MaterialB`、C = `MaterialC`。

## PARAM

| Param | 語意 | Dim | 值 | 單位 |
| --- | --- | --- | --- | --- |
| MaterialCost | 原料單位成本 | RawMaterial | MaterialA=2.00, MaterialB=1.50, MaterialC=1.00 | 元/kg |
| MonthlySupplyLimit | 原料每月限制用量 | RawMaterial | MaterialA=2000, MaterialB=2500, MaterialC=1200 | kg/month |
| ProcessingCost | 牌號單位加工費 | CandyBrand | BrandJia=0.50, BrandYi=0.40, BrandBing=0.30 | 元/kg |
| SellingPrice | 牌號單位售價 | CandyBrand | BrandJia=3.40, BrandYi=2.85, BrandBing=2.25 | 元/kg |
| MinContentRatio | 含量下限比例 | RawMaterial, CandyBrand | (MaterialA, BrandJia)=0.60；(MaterialA, BrandYi)=0.30 | ratio |
| MaxContentRatio | 含量上限比例 | RawMaterial, CandyBrand | (MaterialC, BrandJia)=0.20；(MaterialC, BrandYi)=0.50；(MaterialC, BrandBing)=0.60 | ratio |

`MinContentRatio` 與 `MaxContentRatio` 是**稀疏**參數：只有題目表格有填數字的 (原料, 牌號) 組合才有列。未列出的組合代表該方向沒有含量限制，不產生限制式。

## VAR

| Var | 語意 | Dim | 型別 | LB | UB |
| --- | --- | --- | --- | --- | --- |
| Blend | 本月投入某原料到某牌號的重量 | RawMaterial, CandyBrand | Continuous | 0 | INFTY |
| Produce | 某牌號本月的產量 | CandyBrand | Continuous | 0 | INFTY |

## CONSTRAINT

### [C1] MaterialAvailability `[UB]` ∀ material ∈ RawMaterial

$$\sum_{brand \in CandyBrand} Blend_{material,\,brand} \le MonthlySupplyLimit_{material}$$

每種原料本月投入到所有牌號的總量，不得超過該原料的每月限制用量。

### [C2] BrandMassBalance `[Balance]` ∀ brand ∈ CandyBrand

$$Produce_{brand} = \sum_{material \in RawMaterial} Blend_{material,\,brand}$$

每個牌號的產量等於投入該牌號的各原料重量之和（加工無質量損耗）。這條同時定義了含量百分比的分母。

### [C3] MinContent `[Proportional]` ∀ (material, brand) ∈ dom(MinContentRatio)

$$Blend_{material,\,brand} \ge MinContentRatio_{material,\,brand} \cdot Produce_{brand}$$

有含量下限的組合：該原料在該牌號中的投入量，不得低於該牌號產量的指定比例。

### [C4] MaxContent `[Proportional]` ∀ (material, brand) ∈ dom(MaxContentRatio)

$$Blend_{material,\,brand} \le MaxContentRatio_{material,\,brand} \cdot Produce_{brand}$$

有含量上限的組合：該原料在該牌號中的投入量，不得高於該牌號產量的指定比例。

## OBJ

$$\max \sum_{brand \in CandyBrand} SellingPrice_{brand} \cdot Produce_{brand} - \sum_{brand \in CandyBrand} ProcessingCost_{brand} \cdot Produce_{brand} - \sum_{material \in RawMaterial} \sum_{brand \in CandyBrand} MaterialCost_{material} \cdot Blend_{material,\,brand}$$

最大化月獲利＝售價收入 − 加工費 − 原料成本。

## 已套用假設

1. **加工無質量損耗**：每個牌號的產量等於投入該牌號的三種原料重量之和（[C2]）。題目未明說損耗，但「含量百分比」必須有一個分母才有定義，此為該分母的唯一合理來源。
2. **含量百分比的分母是該牌號糖果的總重量**（不是原料的可用總量，也不是全廠總產量）。
3. **表格空白 = 無限制**（S009、S010）：原料 B 對三個牌號、原料 A 對丙牌號沒有含量欄位值，因此不列入 `MinContentRatio` / `MaxContentRatio`，也不產生對應限制式。
4. **變數型別為 Continuous**：題目明示要建立線性規劃模型（S025），糖果以 kg 計重可分割。LB=0、UB=INFTY 為預設慣例（`model-design-guide.md` §6.1）。
5. **原料成本按實際投入量計**：未投入的原料不計成本；每月限制用量是上限，不要求用完（S014–S016）。
6. **三種牌號都不強制生產**：題目未給最低產量或市場需求下限，因此某牌號產量可以是 0。
7. **百分比已換算成 0..1 的比例**：60% → 0.60，其餘同理。
8. **引入 `Produce` 變數**：題目直接問「三種牌號各生產多少 kg」，因此把該量顯式建成變數並由 [C2] 定義。這與只用 `Blend` 的寫法完全等價，只是讓答案與含量式子都直接可讀。
