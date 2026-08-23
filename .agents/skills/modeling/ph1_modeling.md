# OptimFoundation API — 端到端開發規範

> **這份文件是什麼**：用 OptimFoundation（封裝 IBM ILOG CPLEX 的 C# 套件）寫一個最佳化專案，從「拿到模型後」到「寫模型程式」到「組模型寫實驗程式」的完整標準流程。
> **給誰看**：第一次用這套套件的人，以及要用它產 code 的 AI。
> **怎麼用**：從 §0 一路往下做，每節結尾的 checklist 過了才進下一節。API 簽名有疑慮 → 查 §9（框架完整簽名表就在本檔內），**NEVER 憑記憶發明方法名**。
> **前置**：數學模型必須先定完並經使用者確認，成果就是 `Model/<Project>_Model.md`。它長什麼樣、缺哪一項就得退回重補，見 §0.0。
> **本檔自足**：讀這一份就能從模型走到可交付的專案，不需要開任何其他文件。凡本檔會用到的外部規則（phase gate、Model.md 契約、線性化 pattern、框架 API 簽名、solver 旋鈕）都已內嵌在對應章節與附錄。
> **本檔是 Phase 2 的唯一文件**：專案結構、命名、注入方式、允許的 API 與框架簽名全在這裡，§9 是簽名權威。

路徑一律**相對本 repo 根**（`.claude/` 的上一層），NEVER 絕對路徑。`$SIB` 指 sibling 的框架 repo `../OptimFoundation/`，只在說明「哪些東西不是 scaffold」時出現，本檔不要求你去讀那底下的任何文件。

### Canonical 邊界（AI MUST 先判斷）

- **本 guide 是專案結構與寫法的唯一權威**。結構、命名、namespace、注入方式、允許的 API 一律以本檔為準。
- **權威順序**：本 guide 的規則 → 本檔 §9 的框架簽名表 → 任何既有專案 code。既有專案與本檔衝突 → 視為待遷移，NEVER 反向修改本檔去迎合它。
- **NEVER 從既有專案推導結構**。`Template/`、`Projects/*`、sibling 的 `$SIB/OptimFoundation/Templates/*` 都**早於本版規範**，含已被禁止的寫法（手寫 `VariableBase`、`Sets.cs` 集中檔、子 namespace、`CreateXxx(rhs, name)`、`ProjectReference`）。它們可讀來理解 API 行為，NEVER 複製其結構。
- **只有一條 paved path**：generator（`[OptSet]` / `[OptParam]` / `[OptVar]` + `[OptDim<T>("Name")]`）。`T` 直接是支援的 C# 基礎型別（`string` / `DateTime` / `int` / `long` / `double` / `decimal`），不引用 `Set_*` 積木。Set 與 Param 共用同一個 Dim→property→CSV 欄位流程；Param 僅在最後固定多 `QTY`。**手寫 `: VariableBase` / `: ParameterBase` 已廢止，沒有後路。**

---

## §0 心智模型

### 0.0 輸入契約 — 你拿到的模型

**三階段 phase gate（天條）**：

| 階段                              | 產物                                      | 本檔涵蓋                             |
| --------------------------------- | ----------------------------------------- | ------------------------------------ |
| 1 · Model Design（建模）          | `Model/<Project>_Model.md`                | 只定義「合格的輸入長什麼樣」（本節） |
| 2 · Foundation Coding（轉譯實作） | 八資料夾專案 + 可求解的 `.cs`             | §1 – §7                              |
| 3 · Foundation Tuning（調校）     | experiment 證據 + promotion 後的 baseline | §8（使用者提出才做）                 |

- NEVER 在使用者明確確認數學模型前產生任何 `.cs` —— ALWAYS 先有 Model.md 並等到「模型確認」或「開始實作」。使用者一句話明說「模型我確認過了，直接寫 code」= 通過 gate。
- **Phase 2 是 Model.md 的純機械轉譯**，不允許任何自行詮釋。發現歧義 → 立即停止，退回 Phase 1 補模型，NEVER 在 code 這一層替使用者做建模決定。

**Model.md 固定八段順序**（數學式一律 LaTeX，`$...$` / `$$...$$`）：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

術語表**內嵌在 Model.md**，NEVER 另開 `Glossary.md`。

**各段必填欄 = Phase 2 的直接輸入**（缺一項，轉譯就得靠猜）：

| 段          | 必填欄                                                                                                                          | 範例                                                                                         | 本檔哪一節在吃它 | 缺了會怎樣                         |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | ---------------- | ---------------------------------- |
| Terminology | Term / 中文語意 / Role / Unit / Derived? / Raw phrase；Role ∈ {parameter, variable, derived, constraint, objective, irrelevant} | `MachineCapacity｜機器產能｜parameter｜hours｜No｜"up to 40 hours"`                          | §1.3 命名總表    | 名稱沒語意，類別名跟著沒語意       |
| SET         | Set 名 / 語意 / 成員範例                                                                                                        | `GlassType｜玻璃種類｜Regular, Tempered`                                                     | §2.1             | 不知道要建哪幾顆積木               |
| PARAM       | Param 名 / 語意 / **Dim** / 值                                                                                                  | `DemandQty｜需求量｜GlassType｜Regular=60`                                                   | §2.2             | `[OptDim]` property 名靠猜         |
| VAR         | Var 名 / 語意 / Dim / **型別** / LB / UB                                                                                        | `Assign｜是否指派｜Employee,Date｜Binary｜0｜1`                                              | §3               | 前綴 `B_/C_/I_` 選錯，型別就錯     |
| CONSTRAINT  | **`LHS op RHS` 原形**（未預先移項）+ **pattern tag** + Dim + 一句中文                                                           | `### Capacity [UB] ∀ machine` + `$$\sum_g UsageRate_g \cdot Produce_g \le MachineCapacity$$` | §4.2             | 對不回去，天條的逐條驗收失效       |
| OBJ         | 方向（min/max）+ 所有項寫在 LHS                                                                                                 | `$$\max \sum_g Profit_g \cdot Produce_g$$`                                                   | §4.3             | 停止 Phase 2（見 §4.3）            |
| 已套用假設  | Phase 1 自行套用的預設清單                                                                                                      | 「未提 LB/UB 者取 `0..INFTY`」                                                               | §7 驗收          | 驗收時分不出哪些是題目、哪些是假設 |

pattern tag 是附錄 A 的 8 類之一；轉譯前先用附錄 A 對一次式子形狀，能在寫 code 前抓出漏 linking constraint 這類建模漏洞。

**進場檢查（開工前逐項對照，任一不過 → 停止 Phase 2，退回 Model Design）**：

- [ ] 八段齊全、順序正確；術語表在 Model.md 內
- [ ] **宣告先於使用**：CONSTRAINT / OBJ 出現的每個符號都已在 SET / PARAM / VAR 宣告
- [ ] **CONSTRAINT / OBJ 內沒有「資料類」裸數字**：容量、比例、penalty、Big-M、3×3 的 3、時間窗的 7 —— 凡是換一批資料會變的，都 MUST 是具名 PARAM。線性化 pattern 自帶的常數（`Σ z = 1` 的 1、`z₁ + z₂ − 1` 的 −1、`= 0`）不在此限，那是式子的形狀不是資料（§4.4）
- [ ] 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
- [ ] 每條 CONSTRAINT 是原形（未預先移項 / 化簡 / 翻方向）+ 有 pattern tag + 標了 Dim
- [ ] 有 OBJ 段且標了方向
- [ ] 無單一字母符號（`i`、`x`、`c1`）；符號皆語意命名（`Assign_{Employee,Date}`、`MachineCapacity`）
- [ ] 每個數字都帶單位，單位不一致已換算標明
- [ ] 已列出「已套用的預設假設」

✅ Good：`x <= a * y`（原形）　❌ Bad：`x - a*y <= 0`（Phase 1 就移項，Coding 端 `AddLHS`/`AddRHS` 對不回去）

### 0.1 一條單向鏈

**資料的前提**：`Data/*.csv` 已經按框架標準形狀就位（兩個實體位置與讀取路徑見 §2.0）。`Dataload` 的工作就只是把它們讀成積木——不生成、不補值、不判斷。

```text
① 資料層   Data/*.csv                     input，已就位
           Set_*（維度）+ Parameter_*（係數與結構常數）
           └ Data/Dataload.cs : DataContext
             └ OptData.Load(...)          唯一建構入口 → 驗證不過當場丟例外
② 變數層   VariableB_/C_/I_*              決策變數宣告（前綴決定型別）
             └ engine.BuildVars<T>        把宣告 × sets 展開成實際變數
③ 模型層   ObjectiveFunction              目標式（MUST 先建）
           Constraint_*                   限制式（AddLHS / AddRHS / CreateXxx）
④ 組裝     Program.cs 的 OptModel chain   唯一組裝點，唯一知道 Dataload 的地方
⑤ 執行環境 OptProject                     一個模型 × 一組設定；可掛 OnSolved
           OptExperiment                  m 個模型 × n 組 solver 設定
⑥ 解讀層   Solution/<Project>Solution.cs  ReadAndValidate + Print
```

CSV 不是現成的（來源是 9×9 矩陣、原始報表，或根本還沒有資料、要依題目規格生一批），用 `import` 模式先產出標準 CSV，見 §2.0。那是**選用的前置步驟**，不改變 ① 的前提。

### 0.2 三條不可協商的分工

**組裝只在 `Program.cs`。** 每種變數一行 `.AddVariables(...)`、目標式一行 `.AddObjective(...)`、每條限制式一行 `.AddConstraints(...)`。
Why: pipeline 本身就是模型組成清單，review 從這一個檔就能讀完模型由哪些部分組成；轉呼叫的 helper 或 local function 會把組裝順序藏起來。

**`Dataload` 只准出現在 `Program.cs`。** Objective / Constraint 的建構子逐項列出它實際用到的 Set、Parameter、scalar。
Why: 建構子簽名就是依賴清單，看簽名就知道這條式子碰哪些資料，測試也只需準備那幾樣。

**`OptEngine` 由 `Build(OptEngine engine)` 傳入，NEVER 進建構子。**
Why: 建構子只放資料依賴，engine 是執行期物件；混在一起就分不出「這個類別需要什麼資料」。

`OptData.Load` 完成後把資料視為唯讀。`DataContext.Freeze()` 只保護框架受控的 mutation API；直接修改 public field 或 mutable `List` 不保證立即攔截，因此專案 code MUST 不做這些寫入。

---

## 附錄 A · 線性化 pattern 對照（讀 Model.md 的 CONSTRAINT 段用）

Model.md 每條 constraint 都標了一個 pattern tag（§0.0）。轉譯前先用本附錄核對式子形狀：形狀對不上，多半是 Phase 1 漏了某條 linking constraint，該退回 Model Design 而不是在 code 裡補。符號一律語意命名，`M` 為 Big-M。

**A.1 八類 canonical form**

| #   | Pattern                                      | 語言線索                           | 原形                                                                                 |
| --- | -------------------------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------ |
| 1   | **UB / LB（Range）**                         | at most / no more than / at least  | $\sum_i Coef_i \cdot x_i \le CapacityUB$；$\sum_i Coef_i \cdot x_i \ge RequireLB$    |
| 2   | **Balance**                                  | input = output / must equal        | $\sum_{i \in In} Flow_i = \sum_{j \in Out} Flow_j$                                   |
| 3   | **Proportional**                             | at least X times / no more than Y% | $A \le Ratio \cdot B$（**交叉相乘**）—— ❌ NEVER $A / B \le Ratio$，變數相除即非線性 |
| 4   | **Implication**（A 開 ⇒ B 開，兩者 binary）  | if A then B / implies              | $z_A \le z_B$                                                                        |
| 5   | **Conjunction**（全部同時成立）              | only if all / must all be active   | $\sum_{s \in S} z_s = \lvert S \rvert$                                               |
| 6   | **Disjunction**（至少 k 個）                 | at least one of / a minimum of     | $\sum_{s \in S} z_s \ge k$                                                           |
| 7   | **Exclusive XOR**（恰好一個）                | exactly one / mutually exclusive   | $\sum_{s \in S} z_s = 1$                                                             |
| 8   | **Conditional Activation**（Big-M 條件啟動） | only if / can be used when         | $x \le M \cdot z$；門檻觸發版 $Input \ge Threshold - M \cdot (1 - z)$                |

**A.2 非線性 → 線性 recipe**

| 情境                                             | 作法                                                                                                                                                 |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `abs`：$\lvert expr \rvert \le b$                | 拆兩條 $expr \le b$、$-expr \le b$（NEVER 用 `abs()`）。目標裡 $\min \lvert expr \rvert$ → 加輔助變數 $t$：$t \ge expr$、$t \ge -expr$，目標 `min t` |
| 目標裡壓一個 max                                 | 加 $t$，$t \ge e_i\ \forall i$，目標 `min t`                                                                                                         |
| 目標裡抬一個 min                                 | 加 $t$，$t \le e_i\ \forall i$，目標 `max t`（方向必須對：min-of-max 用 `t >=`，max-of-min 用 `t <=`）                                               |
| **Fixed-charge**（設置成本 / 開了才能用）        | $Produce \le M \cdot Open$ + 目標加 $FixedCost \cdot Open$。❌ 只加成本卻漏 linking，等於可白吃產能                                                  |
| **Either-Or**（兩約束至少一條成立）              | 加 binary $y$：$g_1(x) \le b_1 + M(1-y)$、$g_2(x) \le b_2 + M \cdot y$                                                                               |
| **二元 × 二元** $w = z_1 z_2$                    | $w \le z_1$、$w \le z_2$、$w \ge z_1 + z_2 - 1$、$w \in \{0,1\}$                                                                                     |
| **二元 × 連續** $w = z \cdot c$（$c \in [0,U]$） | $w \le U \cdot z$、$w \le c$、$w \ge c - U(1-z)$、$w \ge 0$                                                                                          |

**A.3 Big-M 鐵律（最容易靜默出錯）**

- ALWAYS `M` = 被約束式的**最緊合法上界**，由題目數據推導（總產能、最大需求…），且 MUST 定義成 `Parameter_*` 從 CSV 讀入
- NEVER magic number（`99999`）：M **太小** → 砍掉合法解，solver 靜默回一個錯的「最佳解」，build / solve 都不報錯，最難抓；M **太大** → LP relaxation 鬆、B&B 節點爆增
- 由資料推導時用 `Numeric.SafeRatio(分子, 分母, context: "BigM")`，NEVER 裸除法 —— 除零 / 非有限 / 超過量級門檻會當場丟例外，不讓壞的 M 溜進模型
- ✅ Good：`Parameter_BigMProduce.QTY = TotalCapacity`　❌ Bad：`engine.AddRHS(1000000)`

---

