# Model Design — Phase 1 端到端建模規範

> **這份文件是什麼**：把一段自然語言的最佳化題目（LP / IP / MILP）做成一份完整、無歧義、**程式好轉譯**的數學模型文件 `Model/<Project>_Model.md`，從「拿到題目」到「停在使用者確認 gate」的完整標準流程。
> **給誰看**：第一次替這條 pipeline 建模的人，以及要用它產 Model.md 的 AI。
> **怎麼用**：從 §0 一路往下做，每節結尾的過關條件過了才進下一節。constraint 形狀有疑慮 → 查附錄 A，**NEVER freehand 寫式子**。
> **前置**：只需要題目原文。本階段 NEVER 產生任何 `.cs`。
> **本檔自足**：讀這一份就能從題目走到可交付的 Model.md，不需要開任何其他文件。天條全文在 [`../AGENTS.md`](../AGENTS.md)，本檔只在需要時引用不重複。

### Canonical 邊界（AI MUST 先判斷）

- **本檔是 Phase 1 的唯一權威**。方法、順序、Model.md 段落結構、命名、pattern tag、歧義處理、交付 gate 一律以本檔為準。
- **權威順序**：[`../AGENTS.md`](../AGENTS.md) 的天條 → 本檔 → 任何既有 Model.md。既有 Model.md 與本檔衝突 → 視為待遷移，NEVER 反向修改本檔去迎合它。
- **NEVER 從既有專案的 Model.md 反推規則**。`Projects/*/Model/*.md` 可能早於本版規範。
- 只有一條路線：**1a → 1b → 1c → 1d 四階段降維**，每階段只降一層抽象。NEVER 跳過 1a/1b 直接上符號。

---

## §0 心智模型

### 0.0 輸入契約 — 你拿到的題目

**三階段 phase gate（天條）**：

| 階段 | 產物 | 本檔涵蓋 |
| --- | --- | --- |
| **1 · Model Design（建模）** | `Model/<Project>_Model.md` | **§0 – §9（全部）** |
| 2 · Foundation Coding（轉譯實作） | 八資料夾專案 + 可求解的 `.cs` | 只定義「合格的輸出長什麼樣」（§0.1） |
| 3 · Foundation Tuning（調校） | promotion 後的 baseline | 不涵蓋 |

**輸入**：一段自然語言的最佳化題目，可能夾帶表格、原始報表或生成規格。

**落檔規則**：題目原文**原封**寫進 `_wip/<Project>/00-raw.md`，一次性。之後對話中 NEVER 再重述原文——要引用就引路徑。

- `<Project>` 由題目語意取 PascalCase 名（醫院排班 → `HospitalRostering`），第一次回報時把取的名字講出來讓使用者當場否決。
- `_wip/` 放在 **repo 根**而不是專案內 —— Why: 專案內結構受天條「八資料夾 NEVER 增減」管轄，塞 `_wip/` 進去等於自己製造一個 Phase 2 驗收 FAIL。

### 0.1 輸出契約 — Model.md 八段

**唯一交付物**：`Projects/<Project>/Model/<Project>_Model.md`，固定八段順序：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

- 術語表**內嵌在 Model.md**，NEVER 另開 `Glossary.md`。
- `Model/` 只放這一份 `.md`，NEVER 放 `.cs`、NEVER 放第二份文件。
- 數學式一律 LaTeX：行內 `$...$`、獨立 `$$...$$`。

**各段必填欄 = Phase 2 的直接輸入**（缺一項，轉譯就得靠猜）：

| 段 | 必填欄 | 下游用途 | 缺了會怎樣 |
| --- | --- | --- | --- |
| Terminology | Term / 中文語意 / Role / Unit / Derived? / Raw phrase / 來源 S-id | 決定類別命名 | 名稱沒語意，類別名跟著沒語意 |
| SET | 名 / 語意 / 成員範例 | 決定建哪幾顆 `Set_*` | 不知道要建哪幾顆積木 |
| PARAM | 名 / 語意 / **Dim** / 值 | 決定 `[OptDim]` property | property 名靠猜 |
| VAR | 名 / 語意 / Dim / **型別** / LB / UB | 決定 `VariableB_` / `X_` / `I_` 前綴 | 前綴選錯，型別就錯 |
| CONSTRAINT | **`LHS op RHS` 原形** + **pattern tag** + Dim + 一句中文 | 逐條對照 `AddLHS` / `AddRHS` | 對不回去，逐條驗收失效 |
| OBJ | 方向（min/max）+ 所有項在 LHS | 決定 `CreateMinimize` / `CreateMaximize` | Phase 2 直接停止並退回 |
| 已套用假設 | Phase 1 自行套用的預設清單 | 驗收時分辨哪些是題目、哪些是 AI 補的 | 分不出來，使用者無從確認 |

Role ∈ `{parameter, variable, derived, constraint, objective, irrelevant}`。

### 0.2 方法：四次漸進降維

每階段只降一層抽象，少一步錯步步錯：

| 階段 | 做什麼 | 產物 | 只做這件事 |
| --- | --- | --- | --- |
| **1a** | 去故事化 + 單位正規化 | `_wip/<Project>/1a-normalized.md` | 純自然語言，NEVER 引入任何數學符號 |
| **1b** | 語義判別 + Terminology Table | `_wip/<Project>/1b-terminology.md` | 每個子句強制歸類 role，無關的明標 `irrelevant` |
| **1c** | 結構抽取 → Model.md 各段 | `_wip/<Project>/1c-draft.md` + `1c-constraints/` | SET/PARAM/VAR/CONSTRAINT/OBJ，每條 constraint 標 pattern tag |
| **1d** | 建模自驗 gate | `_wip/<Project>/1d-audit.md` + `1d-redteam.md` | 逐條對照 §7 清單，全過才交付 |

Why 要拆四步：資料沒清乾淨、子句沒歸類就符號化，等於在雜訊上建模。而且錯誤在建模階段是最便宜的——模型錯了，Phase 2 的 code 全部重寫。

### 0.3 三條不可協商的規矩

**一律 Hard constraint。** 本階段 NEVER 討論 soft constraint / penalty 放鬆。
Why: 提早放鬆會掩蓋建模錯誤——你以為模型對了只是太緊，其實是寫錯。放鬆屬 Phase 3 的決定，且必須由使用者提出後回到本階段改 Model.md。

**NEVER 預先移項 / 化簡 / 翻方向。** 寫成 `LHS (op) RHS` 原形，左邊項留左邊、右邊項留右邊。
Why: Phase 2 的 `AddLHS` / `AddRHS` 要逐項對照，Model.md 先移項就對不回去，整條 pipeline 的驗證能力當場失效。

**NEVER 自行詮釋不清楚的術語。** 追問後才繼續，確認後補進 Terminology Mapping Table。
Why: 猜錯的術語不會報錯——它會一路活到「求解成功、答案是錯的」。

---

## §1 1a · 去故事化 + 單位正規化

目標：把題目原文轉成乾淨、self-contained、**逐句編號**的問題敘述。

### 1.1 做法

1. 去掉背景故事、人物、動機敘述，只留**資料與邏輯**
2. 表格逐列轉成宣告句（一列一句），NEVER 整表原樣搬
3. 每個數字都標單位；單位不一致（時/分、噸/公斤）換算成單一單位，並在該句標明換算式
4. **逐句編號 `S001`、`S002`……**，一句一個事實，複合句拆開
5. NEVER 引入任何數學符號、變數名、集合名——這階段還是純自然語言
6. NEVER 推導題目沒明說的數值

### 1.2 為什麼要編號

「每個子句都要有 role」這條規則原本無法機械驗證。編號之後，1d 的 auditor 才驗得出**哪一句被靜默略過**——而靜默漏句正是建模階段最貴的錯誤類型：模型完全合規、求解正常、就是少了題目的一整條限制。

### 1.3 產物格式

```markdown
| ID | 敘述 | 單位 | 換算 |
| --- | --- | --- | --- |
| S001 | 每台機器每週最多運轉 40 小時 | hour | — |
| S002 | 每批鋼胚重 2000 公斤 | kg | 2 噸 × 1000 = 2000 kg |
```

### 1.4 NEVER 自動推導

❌ Bad：題目寫「每班 300 元、每班 10 小時」，1a 就寫成「時薪 30 元」。
✅ Good：`S010 | 每班工資 300 元 | TWD/shift | —` 與 `S011 | 每班 10 小時 | hour/shift | —` 兩句分開留著。

Why: 推導值是**建模決定**，不是資料整理。真的需要 derived 值 → 到 1b 另立一列標 `derived` 並註明推導來源與依據的 S-id，讓使用者在「已套用假設」段看得到。

**✅ 過關條件**

- [ ] 原文每個事實都對應到至少一個 S-id，無遺漏
- [ ] 每個數字都有單位欄位，換算過的標明換算式
- [ ] 全文無數學符號、無變數命名
- [ ] 無題目未明說的推導值
- [ ] 判定為背景故事而捨棄的內容已列出來，讓使用者覆核是否誤刪

---

## §2 1b · 語義判別 + Terminology Mapping Table

目標：把 1a 的每個編號子句歸類，並產出 Terminology Mapping Table。

### 2.1 語義判別鐵律

1a 敘述的**每個子句**，強制歸類成下列之一：

| Role | 意義 | 例 |
| --- | --- | --- |
| `parameter` | 題目給定的資料 | 「每台機器最多 40 小時」 |
| `variable` | 要決定的量 | 「該生產多少」 |
| `derived` | 由其他資料推導 | 「時薪 = 班薪 ÷ 班時」（**MUST 註明來源**） |
| `constraint` | 限制關係 | 「總工時不得超過產能」 |
| `objective` | 追求什麼 | 「利潤最大化」 |
| `irrelevant` | 與模型無關 | 「工廠位於台中」（**MUST 寫原因**） |

- **NEVER 靜默略過任何子句**——與模型無關的也要明確標 `irrelevant` 並寫原因
- **NEVER 自動推導 derived 值**（見 §1.4）；真需要 derived → 另立一列標 `derived` + 註明推導來源與依據的 S-id
- 術語表外、你不確定語意的名詞 → 列進「待追問」（§6），NEVER 自行詮釋

### 2.2 兩張表

**表一 · Terminology Mapping Table**（這張表會原封搬進 Model.md）

| Term | 中文語意 | Role | Unit | Derived? | Raw phrase | 來源 S-id |
| --- | --- | --- | --- | --- | --- | --- |
| MachineCapacity | 機器產能 | parameter | hour | No | "each machine ... up to 40 hours" | S001 |
| Produce | 生產量 | variable | unit | No | "how many should be produced" | S004 |

**表二 · 子句歸類覆蓋表**（這張表只在 `_wip/`，是給 auditor 驗漏句用的）

| S-id | Role | 對應 Term | 備註 |
| --- | --- | --- | --- |
| S001 | parameter | MachineCapacity | — |
| S003 | irrelevant | — | 工廠地點，與模型無關 |

### 2.3 命名（天條）

- **NEVER 用無意義單一字母符號**：`i`、`j`、`k`、`x`、`y`、`z`、`t`、`c1` 一律禁止
- **ALWAYS 語意名稱、PascalCase**：`MachineCapacity`、`Assign_{Employee,Date}`、`Produce_{GlassType}`
- Set 成員字串 PascalCase 單數：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌

✅ Good：`Assign_{Employee,Date}`、`MachineCapacity`、`Produce_{GlassType}`
❌ Bad：`x_{i,j}`（單字母 + 無語意 index）、`c1`（看不出約束語意）

Why: Phase 2 的類別名由符號機械對應（`Assign` → `VariableB_Assign`、`MachineCapacity` → `Parameter_MachineCapacity`）。符號沒語意 → 程式碼跟著沒語意，而且是整個專案十幾支檔一起沒語意。

**✅ 過關條件**

- [ ] 1a 的每個 S-id 在表二都出現且**恰好一次**
- [ ] 每個 `irrelevant` 都寫了原因
- [ ] 無自動推導的 derived；有 derived 的都註明來源 S-id
- [ ] 無單字母命名
- [ ] 待追問術語清單已列出

---

## §3 1c · SET / PARAM / VAR

這三段是下游 Phase 2 決定 C# 類別的依據，每個 metadata 欄位缺一項，Coding 階段就得靠猜。

### 3.1 SET

| Set | 語意 | 成員範例 | → 程式 |
| --- | --- | --- | --- |
| GlassType | 玻璃種類 | Regular, Tempered | `Set_GlassType`（`[OptSet]` + `[OptDim<string>("GlassType")]`） |
| Date | 規劃日期 | 2026-01-01, 2026-01-02 | `Set_Date`（`[OptSet]` + `[OptDim<DateTime>("Date")]`） |

**多維 Set**：只有 key、語意是「這些 tuple 存在」時（可行弧、合法路徑、已預先指派的組合），寫成多維 Set 而不是 Parameter。

| Set | 語意 | 成員範例 | → 程式 |
| --- | --- | --- | --- |
| Arc | 可行的有向弧 | (Taipei, Taichung) | `Set_Arc`（`[OptSet]` + `[OptDim<string>("From")]` + `[OptDim<string>("To")]`） |

判準：**有沒有值？** 有值 → 單維 Set + Parameter；只有「存在與否」→ 多維 Set。

### 3.2 PARAM（MUST 標 Dim）

| Param | 語意 | Dim | 值 | 單位 |
| --- | --- | --- | --- | --- |
| DemandQty | 需求量 | GlassType, Date | Regular@01-01=60 | unit |
| MachineCapacity | 機器產能 | （scalar） | 40 | hour |

- **Dim 欄決定 Phase 2 的 `[OptDim]` property**，順序就是 key 順序，寫錯順序 = 寫錯 key
- 零維（scalar）明確標「（scalar）」——Phase 2 會用 `.Single().QTY` 取值
- **結構常數也是 PARAM**：3×3 宮的 3、時間窗長度 7、班別數，一律做成 SET 或 PARAM
  Why: 寫死的模型換一組資料就得改 code，違反「換資料不改 code」

### 3.3 VAR（MUST 標型別 + LB/UB）

| Var | 語意 | Dim | 型別 | LB | UB |
| --- | --- | --- | --- | --- | --- |
| Produce | 生產量 | GlassType | Continuous | 0 | INFTY |
| Assign | 是否指派 | Employee, Date | Binary | 0 | 1 |
| Batch | 生產批數 | Item, Machine | Integer | 0 | INFTY |
| Makespan | 總完工時間 | （0 維） | Continuous | 0 | INFTY |

- **型別欄是 load-bearing**：`Binary` → `VariableB_`、`Continuous` → `VariableX_`、`Integer` → `VariableI_`。前綴決定 Phase 2 建出來的變數型別，標錯就是換一題
- LB/UB 若非預設值，Phase 2 會把它寫成**一條獨立 constraint**，所以本段標了之後 §4 也 MUST 有對應條目——只寫在 VAR 表裡它不會出現在模型裡
- 題目沒提 LB/UB → 套 §6 預設慣例（`LB=0, UB=INFTY`；比例變數 `0..1`），並記進「已套用假設」

### 3.4 宣告先於使用

每個符號都要標它來自哪些 S-id。CONSTRAINT / OBJ 出現的每個符號，MUST 已在 SET / PARAM / VAR 宣告過。§7 的 auditor 會逐一比對。

**✅ 過關條件**

- [ ] 每個 VAR 標了型別 + LB + UB；每個 PARAM 標了 Dim
- [ ] 1b 中 role 為 parameter / variable 的 Term 全數出現在對應段
- [ ] 無單字母符號；每個符號標了來源 S-id
- [ ] 結構常數已做成 SET 或 PARAM，不是散在文字裡的數字

---

## §4 1c · CONSTRAINT

這是全 Phase 1 最容易出錯、也最影響下游的一段。

### 4.1 每條的固定格式

```markdown
### Capacity `[UB]` ∀ machine ∈ MACHINE
$$\sum_{g \in GlassType} UsageRate_g \cdot Produce_g \le MachineCapacity$$
每台機器的總使用時數不得超過其產能。
```

四個要素缺一不可：

| 要素 | 說明 |
| --- | --- |
| **名稱** | PascalCase 語意名（`Capacity`、`Coverage`），會變成 `Constraint_Capacity.cs` |
| **pattern tag** | 附錄 A 的 8 類之一，用反引號標在名稱後 |
| **Dim** | `∀ <index> ∈ <SET>`，決定 Phase 2 的 `foreach` 迴圈 |
| **LaTeX + 一句中文** | 原形式子 + 讓人讀得懂它在講什麼 |

### 4.2 原形鐵律（天條）

**左邊項留左邊、右邊項留右邊，NEVER 預先移項 / 化簡 / 翻轉比較方向。**

✅ Good：$x \le a \cdot y$
❌ Bad：$x - a y \le 0$

✅ Good：$\sum_i Flow_i = \sum_j Flow_j$
❌ Bad：$\sum_i Flow_i - \sum_j Flow_j = 0$

Why: Phase 2 逐項把左邊丟 `AddLHS`、右邊丟 `AddRHS`，然後拿 code 反推回數學式與 Model.md 對照。你在這裡動過手腳，那道反向驗證就永遠對不回去——而移項改號正是最隱形的一類錯，正向看每一行都合理。

### 4.3 每條都要 match 一個 pattern

**先查附錄 A 對形狀，再套它的 template 填空——NEVER freehand。**

這是「不亂寫」的機制：標了 tag 就代表你確認過這條式子屬於哪一類已知形狀。形狀對不上，多半是漏了某條 linking constraint（最常見：fixed-charge 只加了成本卻漏 `Produce ≤ M · Open`），這時該回頭補，不是硬寫一條。

邏輯 / 非線性條件（abs、max、min、either-or、變數相乘）→ 一律查附錄 A 的 recipe，NEVER 自己想。

### 4.4 每個 sum 標明 index 範圍

$$\sum_{g \in GlassType} UsageRate_g \cdot Produce_g \le MachineCapacity_m \quad \forall m \in Machine$$

- `∀` 哪個 set（外層迴圈）
- `over` 哪個 set（內層 `Σ`）

兩者都要寫出來。Phase 2 的巢狀迴圈直接照抄這兩個範圍。

### 4.5 禁止裸數字

**CONSTRAINT / OBJ 內的每個「資料類」數值都 MUST 是具名 PARAM。**

| 數字 | 判定 | 處理 |
| --- | --- | --- |
| 容量 40、比例 0.8、penalty 1.5、Big-M | 資料 | 立成具名 PARAM |
| 3×3 宮的 3、時間窗的 7、班別數 | **結構常數，也是資料** | 立成 SET 或 PARAM（§3.2） |
| `Σ z = 1` 的 1、`z₁+z₂−1` 的 −1、`= 0` | pattern 自帶的形狀 | 直接寫數字，不必具名 |

分辨法：**換一批資料它會不會變？** 會變 → 資料，要具名；不會變（改了就是換一個 pattern、換一題） → 直接寫。

### 4.6 Big-M

- ALWAYS `M` = 「該式最緊的合法上界」，由題目數據推導（總產能、最大需求…）
- MUST 立成具名 PARAM，並在該條下方寫明**推導依據**
- NEVER magic number（`99999`）

完整鐵律見附錄 A.3。

### 4.7 只用已宣告的符號

出現的每個符號都要能在 SET / PARAM / VAR 找到宣告。需要新符號（例如線性化引入的輔助變數 `t`、Big-M 參數）→ **回頭補進 §3 的表**，NEVER 只在 constraint 裡用一次。

**✅ 過關條件**

- [ ] 1b 中 role 為 constraint 的每個 S-id 都對應到至少一條 constraint
- [ ] 每條都有 pattern tag + Dim + LaTeX + 中文說明
- [ ] 每條都是 `LHS op RHS` 原形，無預先移項
- [ ] 每個 sum 標明了 index 範圍
- [ ] 出現的每個符號都能在 SET/PARAM/VAR 找到宣告
- [ ] CONSTRAINT 內無「資料類」裸數字
- [ ] 用到 Big-M 的都寫明了上界推導依據
- [ ] 全是 hard constraint，無 soft / penalty

---

## §5 1c · OBJ

```markdown
## OBJ

$$\max \sum_{g \in GlassType} Profit_g \cdot Produce_g$$

最大化總利潤。
```

| 規則 | 說明 |
| --- | --- |
| 方向 | MUST 明標 `\max` 或 `\min`。題目沒提 → **追問**（§6），這是少數不套預設的項目 |
| 所有項在 LHS | 目標式沒有右邊，全部項寫在求和裡 |
| 無裸數字 | 同 §4.5，權重與係數一律具名 PARAM |
| 純可行性問題 | 題目真的沒有目標式 → **停下追問使用者**要不要加。NEVER 自創零係數目標式 —— Why: Phase 2 遇到沒有 OBJ 段會直接停止並退回本階段，你現在省的一句話，下游要退回一整輪 |

線性化引入輔助變數時（min-of-max 的 `t`），目標式改成 `min t`，同時 §3.3 要有 `t` 的宣告、§4 要有 `t ≥ e_i ∀i` 那幾條。三處缺一不可。

**✅ 過關條件**

- [ ] OBJ 段存在且標明 max / min
- [ ] 所有項在 LHS
- [ ] 無裸數字
- [ ] 出現的符號都已宣告

---

## §6 歧義處理 — 預設慣例 vs 追問

### 6.1 預設慣例（直接套用，不追問，但 MUST 列進「已套用假設」）

| 項目 | 預設行為 |
| --- | --- |
| Index domain 邊界 | Dataset 已清洗，直接篩選 |
| 參數未定義的組合 | 填不影響模型的預設值（通常 0 或略過該條） |
| Soft vs Hard | **一律 Hard**；放鬆屬 Phase 3 |
| Big-M 值 | 取「該式最緊的合法上界」（附錄 A.3） |
| 變數 LB/UB | 題目沒提 → `LB=0, UB=INFTY`；比例 / 百分比變數 → `0..1` |
| 集合成員順序 | 依題目出現順序，保序 |

### 6.2 仍需明確追問

| 歧義類型 | 要問的問題 |
| --- | --- |
| **目標函數方向** | 描述通常會含；真沒提 → 必問 |
| **Linearization 選擇** | 同一邏輯有多種等價 formulation 且效能差異大 → 問要哪種 |
| **Time boundary** | 時間序列有無 wrap-around（末期接回首期）？ |
| **不熟悉的術語** | Terminology Mapping Table 查無 → 必問，確認後回填該表 |
| **子句歸類不明** | 1b 判不出 role（像資料又像約束）→ 問清楚再歸類 |
| **互斥的兩種讀法** | 「每人每天最多一班」是「每人每天」還是「每人整期」→ 必問 |

### 6.3 追問協定

1. 先查 §6.1 預設慣例表——表內項目直接套用，**不追問**，但 MUST 列進「已套用假設」
2. 表外的不確定 → 查 Terminology Mapping Table
3. 兩處都查不到 → **停下來問使用者**，NEVER 猜；確認後回填 Terminology Table 再繼續

**一次問完所有已知歧義**，不要一題一題來回。追問時附上：你的兩種理解、各自會導致什麼模型、你傾向哪一種與理由。

### 6.4 「已套用假設」段

Model.md 最後一段，逐條列出本階段自行套用的每一個預設：

```markdown
## 已套用假設

1. 題目未提 `Produce` 的上界 → 套預設 `UB = INFTY`（§6.1）
2. 題目未提時間序列是否 wrap-around → 已追問，使用者確認**不** wrap
3. `BigM_Produce` 取值 = `TotalCapacity`（S007 的總產能），非題目明給
```

Why: 驗收時使用者要能一眼分出「哪些是我說的、哪些是 AI 補的」。沒有這段，確認 gate 等於在確認一份自己看不懂來源的文件。

---

## §7 1d · 建模自驗 gate 與交付

### 7.1 自驗清單（交付前逐點對照，全過才 gate）

- [ ] **1a**：每個數字都帶單位，單位不一致已換算標明
- [ ] **1b**：1a 的每個 S-id 都有 role，無句靜默略過（**逐一比對，不要抽樣**）
- [ ] **1b**：無自動推導的 derived；有 derived 的都註明來源
- [ ] Terminology Mapping Table 已寫進 Model.md，且**沒有**獨立 `Glossary.md`
- [ ] **宣告先於使用**：CONSTRAINT / OBJ 出現的每個符號都已在 SET/PARAM/VAR 宣告
- [ ] **無裸數字**：CONSTRAINT / OBJ 內每個資料類數值都是具名 PARAM
- [ ] 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
- [ ] 每條 CONSTRAINT 是 `LHS op RHS` 原形 + 有 pattern tag + 有 Dim
- [ ] 每個 `sum` 標明了 index 範圍
- [ ] 無單一字母符號；符號皆語意命名
- [ ] OBJ 段存在且標明方向
- [ ] 有「已套用的預設假設」清單
- [ ] `Model/` 目錄下無任何 `.cs` 檔
- [ ] 全是 hard constraint，無 soft / penalty

### 7.2 反向紅隊（與自驗抓的是不同類的錯）

自驗檢查的是「規則有沒有被違反」；反向紅隊檢查的是「**模型有沒有在解另一道題**」。一個完全合規的模型也可能漏掉題目的一整個限制。

做法（照順序，先做完 1 再看 2）：

1. **只讀 Model.md**，用自然語言寫出你理解的問題敘述（決策什麼、受什麼限制、追求什麼）。此時 NEVER 開啟原題目。
2. **再讀 `_wip/<Project>/00-raw.md` 與 `1a-normalized.md`**，逐項對照你的反推版本與原題目。

找這五類問題：

| 類型 | 內容 |
| --- | --- |
| A | 原題目有、模型沒有的限制（**漏句**） |
| B | 模型有、原題目沒有的限制（**多加的假設**） |
| C | 語意偏移（式子技術上成立，但描述的不是原題目那件事） |
| D | 邊界情境未涵蓋（時間 wrap-around、空集合、單一元素） |
| E | 目標函數與題目訴求不一致（方向、缺項、權重來源不明） |

**高嚴重度發現必須為 0 才能交付。**

### 7.3 交付格式

自驗全過 + 紅隊無高嚴重度發現 → 向使用者交付：

1. **Model.md 路徑** + 規模摘要（幾個 SET / PARAM / VAR / CONSTRAINT）
2. **「已套用的預設假設」清單**（逐條，讓使用者一併確認）
3. **追問清單**（§6 的待確認項）
4. 明確一句：**「請確認模型；說『模型確認』或『開始實作』我才進 Phase 2。」**

### 7.4 出口 gate

**使用者明確確認後才算通過。**

`status.json` 的 `modelConfirmed` 在使用者說話之後才可設為 `true`——**這個欄位不是 AI 自評的結果**。

```json
{ "phase": "modeling", "modelConfirmed": false, "wipStage": "1d", "auditPass": true, "redteamHigh": 0, "updated": "YYYY-MM-DD" }
```

| 欄位 | 意義 |
| --- | --- |
| `wipStage` | `1a` / `1b` / `1c` / `1d`，供跨 session resume |
| `auditPass` | §7.1 自驗全 PASS |
| `redteamHigh` | §7.2 高嚴重度發現數，MUST 為 0 |
| `modelConfirmed` | 使用者確認過模型 |

使用者一句話明說「模型我確認過了，直接寫 code」= 通過 gate，改用 `coding` skill 接手。**本階段 NEVER 寫任何 `.cs`。**

---

## §8 multi-agent 執行層

**何時用**：constraint 超過 8 條、或題目敘述超過 300 行、或使用者要求最高保真度。題目小就單線做完，不必派工。

**Why 要拆**：一個 agent 從頭做到尾會把 context 讀滿，而讀滿之後漏掉的正是它該把關的規則。拆開之後每個 agent 只拿判斷所需的最小資訊。

### 8.1 Context 鐵則

- NEVER 把題目原文 / Model.md 全文 / 中間產物貼進 orchestrator 對話 —— ALWAYS 落檔到 `_wip/<Project>/`，agent 之間**只傳路徑**
  Why: orchestrator 的 context 一旦被原始材料灌滿，後面的 gate 判斷就開始漏規則，而漏的正是你派它去把關的那些
- MUST 單一 agent 的輸入預算 ≤ 800 行（規範 + 材料合計）；超過 → 再拆一層 fan-out，NEVER 靠「請精簡閱讀」自律
- MUST 回報上限：執行類 agent ≤ 15 行、稽核類 ≤ 30 行；證據引用 ≤ 5 行原文
- MUST orchestrator 的 context 只裝四種東西：**工單表、狀態表、PASS/FAIL 表、檔案路徑**
- MUST 平行 fan-out 一次 ≤ 6 個 agent

### 8.2 拓樸

```text
M0 orchestrator（主對話，不下場做事）
 │
 ├─ M1 normalizer ──────► _wip/<Project>/1a-normalized.md      （§1）
 ├─ M2 classifier ──────► _wip/<Project>/1b-terminology.md     （§2）
 ├─ M3 structurer ──────► _wip/<Project>/1c-draft.md           （§3 + §5）
 ├─ M4 constraint-writer × N ─► _wip/<Project>/1c-constraints/<Name>.md  （§4，fan-out）
 │        └─ 合併 ─────► Projects/<Project>/Model/<Project>_Model.md
 ├─ M5 model-auditor ───► _wip/<Project>/1d-audit.md           （§7.1，fresh context）
 └─ M6 adversary ───────► _wip/<Project>/1d-redteam.md         （§7.2，反向翻譯）
        │
        └─► 全 PASS → orchestrator 交付 + 停在 §7.4 gate
```

M5 / M6 可平行；其餘依序，每棒收到回報才派下一棒。

| Agent | subagent_type | model | 輸入 | 讀本檔哪節 |
| --- | --- | --- | --- | --- |
| M1 normalizer | `general-purpose` | `sonnet` | `00-raw.md` | §1 |
| M2 classifier | `general-purpose` | `opus` | `1a` | §2 |
| M3 structurer | `general-purpose` | `opus` | `1a` + `1b` | §3 + §5 |
| M4 constraint-writer | `general-purpose` | `opus` | `1a` + `1b` + `1c-draft` 的 SET/PARAM/VAR 段 + 指派的 S-id | §4 + 附錄 A |
| M5 model-auditor | `verifier` | （定義內建） | Model.md + `1a` + `1b` | §7.1 |
| M6 adversary | `second-opinion` | （定義內建） | Model.md + `00-raw` + `1a` | §7.2 |

M2 / M3 / M4 給 `opus`：語義判別與 linearization 選型是「約束互相牽制 + 一次性高後果」——建模錯了下游 code 全部重寫。

### 8.3 工作區

```text
<repo 根>/
├── _wip/<Project>/               ← 三 phase 共用交接區（repo 層，不受八資料夾天條管）
│   ├── 00-raw.md                 ← 題目原文（orchestrator 唯一一次落檔）
│   ├── 1a-normalized.md
│   ├── 1b-terminology.md
│   ├── 1c-draft.md
│   ├── 1c-constraints/<Name>.md
│   ├── 1d-audit.md
│   └── 1d-redteam.md
└── Projects/<Project>/
    ├── Model/<Project>_Model.md  ← 唯一交付物；Model/ 內不放別的
    └── status.json
```

`_wip/<Project>/` 是**交接介質**：每個 agent 讀前一棒的檔、寫自己那棒的檔。跨 session resume 時看它有什麼就知道走到哪，不依賴對話記憶。

### 8.4 派工 prompt（可直接複製，`{{}}` 處替換）

所有 prompt 的路徑一律**相對 repo 根**，NEVER 用絕對路徑。

#### M0 · orchestrator（主對話自己執行，不派工）

1. 把使用者題目原文**原封**寫進 `_wip/<Project>/00-raw.md`（一次性，之後 NEVER 在對話中重述題目內容）
2. 讀本檔 §0（心智模型，最短）
3. 依 §8.2 順序派工，每棒收到回報才派下一棒
4. 任一 agent 回報 FAIL → 退回對應 agent 並附原過關條件，NEVER 自己下場改產物
5. 全 PASS → 依 §7.3 交付，**停下等 gate**

#### M1 · normalizer

```text
目標：把最佳化題目原文轉成乾淨、self-contained、逐句編號的問題敘述。
動機：這是建模第一次降維。資料沒清乾淨就符號化，等於在雜訊上建模；編號是為了讓下游 auditor 能機械驗證「沒有子句被靜默略過」。

輸入：_wip/{{Project}}/00-raw.md（題目原文，先讀它）
規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §1（只讀這節）

做法與過關條件見該節。

輸出：寫入 _wip/{{Project}}/1a-normalized.md，格式為 §1.3 的表。

回報格式：句數、換算了哪幾項（≤3 條）、你判定為背景故事而捨棄的內容（列出來讓我覆核是否誤刪）。總長 ≤15 行，NEVER 貼產物全文。
```

#### M2 · classifier

```text
目標：把 1a 的每個編號子句歸類成 parameter / variable / derived / constraint / objective / irrelevant，並產出 Terminology Mapping Table。
動機：這步是防漏句與防亂設變數的關卡。子句沒歸類就進結構抽取，會出現「題目講了但模型沒有」的靜默漏洞——那種錯到求解出結果都看不出來。

輸入：_wip/{{Project}}/1a-normalized.md
規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §2（只讀這節）

輸出：寫入 _wip/{{Project}}/1b-terminology.md，含 §2.2 的表一與表二。

回報格式：各 role 的數量統計、irrelevant 的 S-id 清單、待追問術語清單。總長 ≤15 行。
```

#### M3 · structurer

```text
目標：依 1b 的分類產出 Model.md 的 SET、PARAM、VAR、OBJ 四段。CONSTRAINT 段由後續 agent 分工，你留空佔位、不要寫。
動機：這四段是下游 Phase 2 決定 C# 類別的依據，每個 metadata 欄位缺一項，Coding 階段就得靠猜。

輸入：_wip/{{Project}}/1a-normalized.md、_wip/{{Project}}/1b-terminology.md
規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §3、§5、§6.1（三節）

輸出：寫入 _wip/{{Project}}/1c-draft.md，段落順序：
問題描述 → Terminology Mapping Table（從 1b 搬入）→ SET → PARAM → VAR →（CONSTRAINT 留空佔位）→ OBJ → 已套用假設

回報格式：SET/PARAM/VAR 各幾個、OBJ 一行、已套用假設條數、追問項。總長 ≤15 行。
```

#### M4 · constraint-writer（fan-out）

orchestrator 先把 1b 中 role=constraint 的 S-id **依語意分組**（「產能上限」「人力覆蓋」「時間連續」各一組），每組派一個 agent，一次最多 6 個。組數 ≤ 3 時可合併成一個 agent。

```text
目標：把指派給你的子句寫成 Model.md 的 CONSTRAINT 條目。
動機：constraint 是 Phase 2 逐條機械轉譯的對象。寫成原形（LHS op RHS）才能對回 AddLHS/AddRHS 驗證；預先移項的式子在 code 端永遠對不回去。

你負責的子句：{{S-id 清單}}
群組名稱：{{例：Capacity}}

輸入：_wip/{{Project}}/1a-normalized.md、_wip/{{Project}}/1b-terminology.md、
      _wip/{{Project}}/1c-draft.md 的 SET/PARAM/VAR 段（符號只能用這裡已宣告的）
規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §4 與附錄 A（兩段，全讀）

額外規則：
- 只用 1c-draft 已宣告的符號；需要新符號（輔助變數、Big-M）→ 停下回報，NEVER 自己補宣告
- 一律 Hard constraint，NEVER 討論 soft / penalty

輸出：寫入 _wip/{{Project}}/1c-constraints/{{群組名稱}}.md

回報格式：條數、各條的 pattern tag 一覽（名稱 | tag）、新增的符號需求（如 Big-M PARAM）、
需要新符號而卡住的項目。總長 ≤15 行，NEVER 貼 LaTeX 全文。
```

**合併**：orchestrator 用 `Read` 逐檔取回後**直接寫入** Model.md 的 CONSTRAINT 段（機械拼接，不改寫內容），或派一個 `general-purpose` 做拼接並明確要求「NEVER 修改任何一條式子的內容，只做順序編排」。

#### M5 · model-auditor（fresh context）

```text
目標：對照自驗清單逐條稽核 Model.md，回報 PASS/FAIL。
動機：你是 fresh context 的稽核者，沒有參與建模，所以看得到產出者看不到的問題。你不修東西，只判定。

輸入：Projects/{{Project}}/Model/{{Project}}_Model.md、
      _wip/{{Project}}/1a-normalized.md、_wip/{{Project}}/1b-terminology.md
規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §7.1（清單）與 §9（反模式）

逐條驗 §7.1 的每一項（每條給 PASS / FAIL + 證據）。
第 2 項（每個 S-id 都有 role）MUST 逐一比對，不要抽樣。

回報格式：
| # | 判定 | 證據（檔案:行號 或 引用 ≤2 行） |
FAIL 項另附「該怎麼修」一句。總長 ≤30 行。NEVER 自己動手修改任何檔案。
```

#### M6 · adversary（反向紅隊）

```text
目標：只看 Model.md，用自然語言反推「這個模型在描述什麼問題」，再與原題目對照，找出語意走樣。
動機：auditor 驗的是「規則有沒有被違反」，你驗的是「模型有沒有在解另一道題」。後者規則檢查抓不到——一個完全合規的模型也可能漏掉題目的一整個限制。

規範：讀 .claude/rules/Ph1_Modeling/model-design-guide.md 的 §7.2（只讀這節）

步驟（照順序，先做完 1 再看 2）：
1. 先只讀 Projects/{{Project}}/Model/{{Project}}_Model.md，寫出你理解的問題敘述。
   此時 NEVER 開啟原題目。
2. 再讀 _wip/{{Project}}/00-raw.md 與 1a-normalized.md，逐項對照。

回報格式：
- 你的反推敘述（≤8 行）
- 發現：| 類型(A–E) | 描述 | 涉及 S-id 或 constraint 名 | 嚴重度(高/中/低) |
- 結論一行：可交付 / 需回 M3-M4 修正
總長 ≤30 行。NEVER 修改任何檔案。
```

### 8.5 FAIL 處理

- M5 FAIL：結構問題退回 M3、constraint 問題退回 M4，附原過關條件
- M6 高嚴重度發現：退回 M2 重跑語義判別（那類錯通常源自子句歸類）
- **同一項重試 2 輪仍 FAIL → 停下問使用者**，NEVER 第三次硬修

---

## §9 常見錯誤與反模式

| 症狀 | 真正原因 | 修法 |
| --- | --- | --- |
| Phase 2 轉譯到一半回報「Model.md 有歧義」 | 1b 有子句被靜默略過，或術語沒追問就自行詮釋 | 退回 §2 逐 S-id 補歸類 |
| Phase 2 說「constraint 對不回去」 | Model.md 預先移項了 | 退回 §4.2 改回原形 |
| Phase 2 說「有裸數字，缺 PARAM」 | §4.5 沒做，或結構常數沒立成資料 | 退回 §3.2 + §4.5 |
| 求解 Infeasible，模型看起來對 | Big-M 太小，或兩條 hard constraint 互斥 | 退回 §4.6 重推 M；互斥要回 §6 追問使用者哪條才對 |
| 求解 Unbounded | 某方向漏了上界 constraint（VAR 表寫了 UB 但 §4 沒對應條目） | 補 §3.3 說的那條獨立 constraint |
| 解出來「看起來合理但不是那題」 | 語意偏移，§7.2 反向紅隊沒做或做得太寬鬆 | 重跑 M6，且第 1 步 MUST 先不看原題目 |
| 使用者說「這不是我要的」 | 「已套用假設」沒列全，使用者確認的是他看不見來源的文件 | 補 §6.4 |

### 反模式

❌ **1a 就開始用符號** —— 那是 1c 的事；提早符號化會讓你在還沒理解題目時就鎖死結構
❌ **表格原樣搬進 1a** —— 一列一句才驗得出漏句
❌ **子句「明顯無關」就不寫進覆蓋表** —— 明顯與否是你的判斷，覆蓋表是給別人覆核你的判斷用的
❌ **自動推導 derived 值**（班薪 ÷ 班時 = 時薪）—— 那是建模決定，要標來源讓使用者看見
❌ **constraint 用單字母 index**（`x_{i,j}`）—— Phase 2 的類別名會跟著沒語意
❌ **freehand 寫非線性條件的線性化** —— 一律查附錄 A 套 template
❌ **Big-M 寫 99999** —— 太小砍掉合法解且完全不報錯，太大讓 B&B 爆炸
❌ **為了讓模型「有解」而放鬆某條限制** —— 那是 Phase 3 的決定，且必須由使用者提出
❌ **另建 `Glossary.md`** —— 術語表內嵌 Model.md，兩份一定會漂移
❌ **本階段產生任何 `.cs`** —— 模型未經使用者確認前一行都不准寫

---

## 附錄 A · 線性化 pattern 手法庫

每條 constraint 先 match 下面一個 pattern，再套它的原形填空——**不 freehand**。
符號一律語意命名；二元變數用語意名（`Open`、`Assign`）；`M` 為 Big-M（見 A.3，NEVER magic number）。
寫進 Model.md 時保持 `LHS (op) RHS` 原形，NEVER 預先移項。

### A.1 八類 canonical form

| # | Pattern tag | 語言線索 | 原形 |
| --- | --- | --- | --- |
| 1 | **`[UB]` / `[LB]`**（Range） | at most / no more than / at least | $\sum_i Coef_i \cdot x_i \le CapacityUB$；$\sum_i Coef_i \cdot x_i \ge RequireLB$ |
| 2 | **`[Balance]`** | input = output / must equal | $\sum_{i \in In} Flow_i = \sum_{j \in Out} Flow_j$ |
| 3 | **`[Proportional]`** | at least X times / no more than Y% | $A \le Ratio \cdot B$（**交叉相乘**） |
| 4 | **`[Implication]`** | if A then B / implies | $z_A \le z_B$ |
| 5 | **`[Conjunction]`** | only if all / must all be active | $\sum_{s \in S} z_s = \lvert S \rvert$ |
| 6 | **`[Disjunction]`** | at least one of / a minimum of | $\sum_{s \in S} z_s \ge k$ |
| 7 | **`[XOR]`** | exactly one / mutually exclusive | $\sum_{s \in S} z_s = 1$ |
| 8 | **`[BigM]`**（Conditional Activation） | only if / can be used when | $x \le M \cdot z$；門檻觸發版 $Input \ge Threshold - M \cdot (1 - z)$ |

**#3 的雷**：❌ NEVER 寫 $A / B \le Ratio$——變數相除是非線性，solver 收不了。一律交叉相乘。

**#5 的等價寫法**：`Σ z = |S|` 也可以逐條寫成 `z_s = 1 ∀s ∈ S`，兩者等價，選可讀性高的那個。

### A.2 非線性 → 線性 recipe（LLM 最常錯，務必查表）

| 情境 | 作法 |
| --- | --- |
| **abs**：$\lvert expr \rvert \le b$ | 拆兩條 $expr \le b$、$-expr \le b$（NEVER 用 `abs()`） |
| **目標裡 min abs**：$\min \lvert expr \rvert$ | 加輔助變數 $t$：$t \ge expr$、$t \ge -expr$，目標 $\min t$ |
| **目標裡壓一個 max**（min-of-max） | 加 $t$，$t \ge e_i\ \forall i$，目標 $\min t$ |
| **目標裡抬一個 min**（max-of-min） | 加 $t$，$t \le e_i\ \forall i$，目標 $\max t$ |
| **Fixed-charge**（設置成本 / 開了才能用） | $Produce \le M \cdot Open$ **且** 目標加 $FixedCost \cdot Open$ |
| **Either-Or**（兩約束至少一條成立） | 加 binary $y$：$g_1(x) \le b_1 + M(1-y)$、$g_2(x) \le b_2 + M \cdot y$ |
| **二元 × 二元** $w = z_1 z_2$ | $w \le z_1$、$w \le z_2$、$w \ge z_1 + z_2 - 1$、$w \in \{0,1\}$ |
| **二元 × 連續** $w = z \cdot c$（$c \in [0,U]$） | $w \le U \cdot z$、$w \le c$、$w \ge c - U(1-z)$、$w \ge 0$ |

**方向必須對**：min-of-max 用 `t ≥`；max-of-min 用 `t ≤`。寫反了模型無界或無效。

**Fixed-charge 最常見的漏**：❌ 只加了目標式的 $FixedCost \cdot Open$ 卻漏掉 $Produce \le M \cdot Open$ 這條 linking constraint——開關與量沒綁，模型可以白吃產能而不付固定成本。

**輔助變數要三處齊全**：引入 $t$ 或 $w$ 時，§3.3 的 VAR 表要有它、§4 要有它的定義式、§5 的目標式引用它。缺任一處都不成立。

### A.3 Big-M 取值鐵律（最容易靜默出錯）

- ALWAYS `M` = 「被約束式的最緊合法上界」，由題目數據推導（總產能、最大需求、期間長度…）
- MUST 定義成具名 PARAM，並在該條 constraint 下方寫明推導依據
- NEVER magic number（`99999`、`1e6`）：
  - M **太小** → 砍掉合法解，solver 靜默給錯的最佳解（build / solve 都不報錯，**最難抓**）
  - M **太大** → LP relaxation 鬆、B&B 節點爆增，症狀看起來像「模型太難」而不是「M 設錯」

✅ Good：`BigM_Produce := TotalCapacity`（依據 S007 的總產能 = 500 unit）
❌ Bad：$x \le 1000000 \cdot z$（憑感覺的大數）

---

## 附錄 B · Model.md 骨架

```markdown
# <Project> — 數學模型

## 問題描述

（1a 的乾淨敘述，三到五句話講完決策什麼、受什麼限制、追求什麼）

## Terminology Mapping Table

| Term | 中文語意 | Role | Unit | Derived? | Raw phrase | 來源 S-id |
| --- | --- | --- | --- | --- | --- | --- |
| MachineCapacity | 機器產能 | parameter | hour | No | "up to 40 hours" | S001 |

## SET

| Set | 語意 | 成員範例 |
| --- | --- | --- |
| GlassType | 玻璃種類 | Regular, Tempered |

## PARAM

| Param | 語意 | Dim | 值 | 單位 |
| --- | --- | --- | --- | --- |
| MachineCapacity | 機器產能 | （scalar） | 40 | hour |
| UsageRate | 單位用時 | GlassType | Regular=8, Tempered=12 | hour/unit |
| Profit | 單位利潤 | GlassType | Regular=500, Tempered=900 | TWD/unit |

## VAR

| Var | 語意 | Dim | 型別 | LB | UB |
| --- | --- | --- | --- | --- | --- |
| Produce | 生產量 | GlassType | Continuous | 0 | INFTY |

## CONSTRAINT

### Capacity `[UB]` ∀ machine ∈ MACHINE

$$\sum_{g \in GlassType} UsageRate_g \cdot Produce_g \le MachineCapacity$$

每台機器的總使用時數不得超過其產能。

### DemandFloor `[LB]` ∀ g ∈ GlassType

$$Produce_g \ge DemandQty_g$$

每種玻璃的產量不得低於其需求量。

## OBJ

$$\max \sum_{g \in GlassType} Profit_g \cdot Produce_g$$

最大化總利潤。

## 已套用假設

1. 題目未提 `Produce` 上界 → 套預設 `UB = INFTY`
2. 題目未提是否允許部分單位 → 套用連續變數（若需整數請告知）
```

---

## 附錄 C · 文件與符號風格

### C.1 Markdown

- `##` 作主要章節（八段），`###` 作元素或 constraint 名稱；**不可跳級**
- 每份文件只有一個 `#` 標題
- 表格欄位齊全，不省略必填欄（寧可寫「—」也不留空）
- 編碼 UTF-8、換行 LF、檔尾一個換行

### C.2 數學式

- 行內 `$...$`，獨立方程 `$$...$$`
- 求和 MUST 標明 index domain：$\sum_{g \in GlassType}$，NEVER 寫成 $\sum_g$
- `∀` 寫在 constraint 標題行，不藏在式子裡
- 集合用大寫（`GlassType`、`MACHINE` 皆可，但**全檔統一**）；參數與變數用 PascalCase；下標用語意名

### C.3 符號

- PascalCase 語意名，多維寫成 `Assign_{Employee,Date}`
- 同一符號在 Terminology / SET / PARAM / VAR / CONSTRAINT / OBJ 六處**拼字完全一致**
- 每個符號在第一次使用前宣告
- Terminology Mapping Table 每一列保留原始敘述、角色與單位；術語確認後**直接更新該表**，不另建檔
