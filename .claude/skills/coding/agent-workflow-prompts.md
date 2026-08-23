# Foundation Coding — Phase 2 Agent Workflow Prompts

<system_context>
Phase 2（轉譯實作）的 **multi-agent 執行層**：把 `optimfoundation-api-guide.md` 的轉譯規則轉成「派誰、給什麼、驗什麼」的可複製 prompt。
規則單一來源是同層 `optimfoundation-api-guide.md`（**唯一標準**）；`checklist.md` 是 AI 的專案產出一致性契約與機械驗收規格，不是使用者人工驗收表。本檔 NEVER 重述規則，只規定調度。
**本 phase 是全流程 context 壓力最大的一段**：API guide、Model.md 與十幾支 `.cs` 不應同時塞進單一 agent context，所以一律走工單 fan-out。
產物：repo 根 `Projects/<Project>/` 的完整可 build 專案。
</system_context>

<critical_notes>

## Context 鐵則（三 phase 共用）

- NEVER 把 Model.md 全文 / `.cs` 原文 / build log 全文貼進 orchestrator 對話 —— ALWAYS 落檔，agent 之間**只傳路徑與工單列** —— Why: orchestrator 一旦裝進 code 原文，就會忍不住自己下場改，而它手上的規則早被稀釋了
- NEVER 任何 agent `Read` 整份 API guide —— ALWAYS 三步定位：`rg -n "^## §" <guide>` 取行號 → 算出該節區間 → `Read offset/limit`（見下方分片表）
- MUST 單一 agent 輸入預算 ≤ 800 行（規範 + 材料合計）；超過 → 再拆一層
- MUST 回報上限：執行類 ≤ 15 行、稽核類 ≤ 30 行；證據引用 ≤ 5 行原文
- MUST orchestrator 只持有：**工單表、狀態表、PASS/FAIL 表、檔案路徑**
- MUST 平行 fan-out 一次 ≤ 6 個 agent，分批收斂

## Phase 2 專屬

- MUST 在開始寫 code 前完成**轉譯工單**；它只作 orchestrator 當次的短暫全域視圖，NEVER 落檔或建立中間文件
- MUST 每個 constraint agent **只拿它那一條的 Model.md 節錄**（行號區間），NEVER 給整份 Model.md —— Why: 給全文它就會「順手參考」別條的寫法，錯誤會跨條擴散
- NEVER 讓寫 code 的 agent 自己宣告完成 —— ALWAYS 派 V1 checklist-verifier + V2 back-translator
- MUST V2 反向翻譯（`.cs` → 數學式 → 對照 Model.md）—— Why: 移項 / 改號 / 翻方向是本 phase 最致命且最隱形的錯，正向 review 看 code 覺得合理，反推成數學式才顯形

</critical_notes>

<paved_path>

## Agent 拓樸

```text
C0 orchestrator（主對話，不寫 code）
 │
 ├─ C1 manifest-builder ──► 回報 manifest（Model.md → 工單表，含行號區間）
 ├─ C2 scaffold ──────────► csproj + 八資料夾 + Program.cs 骨架
 ├─ C3 data-layer ────────► Set/ + Parameter/ + Data/Dataload.cs + 選定來源（CSV / DB / memory）
 ├─ C4 variable ──────────► Variable/Variable[B|C|I]_*.cs
 ├─ C5 constraint × N ────► Constraint/Constraint_*.cs   （fan-out，一條或一群一個）
 ├─ C6 objective ─────────► Objective/ObjectiveFunction.cs
 ├─ C7 program ───────────► Program.cs（不可變四段：import → 模型 → 實驗 → 正式跑）
 ├─ C8 solution ──────────► Solution/<Project>Solution.cs
 ├─ C9 builder ───────────► build + fix loop ≤5
 ├─ V1 checklist-verifier ► 直接回報 checklist 結論
 ├─ V2 back-translator ───► 直接回報 back-translation 結論
 └─ V3 solve-verifier ────► 直接回報 solve 驗證結論（dotnet run 後的四步協定）
```

依賴順序：C1 → C2 → C3 → C4 →（C5 ∥ C6）→ C7 → C8 → C9 →（V1 ∥ V2）→ `dotnet run` → V3

| Agent | subagent_type | model | 讀哪節 guide |
| --- | --- | --- | --- |
| C1 manifest-builder | `general-purpose` | `sonnet` | 不讀（只讀 Model.md） |
| C2 scaffold | `general-purpose` | `sonnet` | §1 |
| C3 data-layer | `general-purpose` | `opus` | §2 |
| C4 variable | `general-purpose` | `sonnet` | §3 |
| C5 constraint | `general-purpose` | `opus` | §4 + 附錄 A |
| C6 objective | `general-purpose` | `sonnet` | §4 |
| C7 program | `general-purpose` | `opus` | §5 |
| C8 solution | `general-purpose` | `sonnet` | §6 |
| C9 builder | `general-purpose` | `sonnet` | §9（只讀 `rg` 命中處） |
| V1 checklist-verifier | `verifier` | （內建） | §10 |
| V2 back-translator | `second-opinion` | （內建） | 不讀 guide |
| V3 solve-verifier | `verifier` | （內建） | §7 |

## API guide 分片表

權威 guide 是同層 `optimfoundation-api-guide.md`，Phase 2 的唯一標準（同層已無第二份規範，NEVER 去別處找）。**NEVER 硬編行號**——行號會隨改版失效，一律現場搜尋：

```powershell
rg -n "^## §|^## 附錄" ".claude/skills/coding/optimfoundation-api-guide.md"
```

| 節 | 內容 | 誰讀 |
| --- | --- | --- |
| §0 | 心智模型 | 所有寫 code 的 agent（開場必讀，最短） |
| §1 | 建立專案 / csproj / DLL | C2 |
| §2 | 資料層 Set / Parameter / Dataload | C3 |
| §3 | 變數層 | C4 |
| §4 | 模型層 Objective 與 Constraint | C5、C6 |
| §5 | Program.cs 組裝、公開 runner/config API | C7 |
| §6 | Solution 取解與輸出 | C8 |
| §7 | 驗收 / 解驗證協定 | V3 |
| §9 | API 速查卡 | 所有 agent，但**只搜尋自己要用的 API 名，讀命中處 ±20 行** |
| §10 | 常見錯誤與反模式 | V1 |
| 附錄 A | 線性化 pattern 對照 | C5 |

§9 用法示範（NEVER 整節讀）：

```powershell
rg -n "BuildVars|AddLHS|CreateLessEqual" ".claude/skills/coding/optimfoundation-api-guide.md"
```

## 中間產物

manifest、稽核與反向翻譯只作為當次 agent 回報與 orchestrator 的短暫狀態，NEVER 落檔。正式產物只有專案本身：

```text
Projects/<Project>/               ← 只有八資料夾 + csproj + Program.cs，NEVER 多一個
    ├── Model/<Project>_Model.md  ← Phase 1 交付物（唯讀，NEVER 在本 phase 修改）
    └── Set/ Parameter/ Variable/ Objective/ Constraint/ Solution/ Data/
```

</paved_path>

<patterns>

## C1 · manifest-builder（轉譯工單）

```text
目標：讀 Model.md，產出一張「轉譯工單」，把模型拆成可獨立指派的 unit，每個 unit 標明它在 Model.md 的行號區間。
動機：後續每個 coding agent 只會拿到它那一格的 Model.md 節錄（避免 context 爆掉），所以行號區間必須精確——區間錯了，agent 就會照著別條式子寫。

輸入：Projects/<Project>/Model/<Project>_Model.md
規範：以 `rg` 定位並讀 API guide 的 §1（八資料夾結構）與 §0（單向鏈，即轉譯順序）

做法：
1. 逐段掃 Model.md，每個 SET / PARAM / VAR / CONSTRAINT / OBJ 元素開一列
2. 每列標：unit 類型、Model.md 符號、目標 C# 類別名（依命名規則推導）、目標檔案路徑、Model.md 行號區間、依賴的其他 unit
3. Constraint 依語意分組（同一群 ∀ 迴圈或同主題的合併成一個 unit），每組 ≤ 3 條
4. 標出 scalar parameter（只有一列值的），它們要在 Program.cs 用 .Single().QTY 取出
5. 發現 Model.md 缺 OBJ 段、缺 pattern tag、有未宣告且無法判定為式子結構的資料值、有預先移項 → 列進「阻塞項」，NEVER 自己補

輸出：直接回報 manifest：
| # | 類型 | Model 符號 | C# 類別 | 檔案路徑 | Model.md 行號 | 依賴 |
| 1 | SET | GLASSTYPE | Set_GlassType | Set/Set_GlassType.cs | 42-45 | — |

另附「阻塞項」段落。

驗收條件：
1. Model.md 的每個 SET/PARAM/VAR/CONSTRAINT/OBJ 元素都有對應列，無遺漏
2. 每列的行號區間真實存在且涵蓋該元素完整定義
3. C# 類別名符合前綴規則（Set_ / Parameter_ / VariableB_|C_|I_ / Constraint_）
4. 阻塞項段落存在（無阻塞則明寫「無」）

回報格式：各類型 unit 數量、constraint 分成幾組、阻塞項清單（有就逐條列，這會擋住整個 phase）。總長 ≤15 行。
```

**阻塞項處理**：manifest 回報任何阻塞項 → orchestrator **立即停止 Phase 2**，回報使用者並退回 Phase 1，NEVER 讓 coding agent 自行補假設。

## C2 · scaffold（專案骨架）

```text
目標：在 Projects/<Project>/ 建立八資料夾結構、csproj 與**完整四段不可變模板**的 Program.cs 骨架。
動機：所有最終 Phase 2 成果的 Program.cs 都必須同形；C2 先固定組裝外型，C7 只填入 manifest 對應的實際型別。此時尚未有 model components，不能以假型別或假模型要求空專案 build；完整 build 由 C9 在 C3–C8 完成後驗證。

規範：以 `rg` 定位並讀 API guide 的 §0（心智模型）、§1（建立專案 / csproj / DLL）與 §5（Program.cs 四段模板）

做法：照 §1 建結構與 csproj、照 §5 建 Program.cs 骨架，規則不在本檔重述。本 agent 專屬邊界：
1. Program.cs 骨架無條件建立不可變四段 `import → 模型 → 實驗 → 正式跑`，保留四個逐字段落標記與固定控制流；import 與 exp 是固定能力，NEVER 因 Model.md、manifest 或現有 CSV 而省略
2. 此時尚未有 model components，NEVER 為了讓空專案 build 過而加入假型別或假模型

驗收條件：
1. 八個資料夾存在，無額外資料夾
2. 四段標記與固定控制流齊全；C2 不得為了「空專案 build」加入假的 model components
3. csproj 無 ProjectReference、無絕對路徑，有 Analyzer 與 Data copy 兩項
4. Program.cs 的 namespace 是 block 寫法且無子 namespace

回報格式：建立的檔案清單（路徑）、build 結果一行、csproj 的 HintPath 相對深度。總長 ≤15 行。
```

## C3 · data-layer（Set / Parameter / Dataload / IDataSource）

```text
目標：依工單建立 Set_*.cs、Parameter_*.cs、Data/Dataload.cs，以及 manifest 指定的資料來源；未指定時才採預設 `CsvDataSource` 與 Data/*.csv。
動機：資料層是「換資料不改 code」的基礎。instance 決定的 index domain、資料值與可變範圍必須來自 Set 或 Parameter；`1`、`-1` 等 Model.md 明寫的代數結構係數不屬於資料 magic number。

輸入：orchestrator 提供的 manifest SET 與 PARAM 列 + 各列標示的 Model.md 行號區間（用 Read offset/limit 只讀這些區間）
規範：以 `rg` 定位並讀 API guide §2 全節（宣告、CSV、Load<T>、輸出與實際驗證範圍）

做法：照 §2 全節逐項做，規則不在本檔重述。本 agent 專屬邊界：
1. 資料來源由 manifest 指定；未指定才用預設 CsvDataSource + Data/*.csv，NEVER 為通過檢查而建立假的 CSV
2. CSV / SQL 取得的值與 Model.md 逐筆一致，NEVER 四捨五入或填佔位值
3. 需要「算」出來的資料一律不進 Dataload(IDataSource) —— 該由 import 階段先產 CSV；Model.md 缺值就停止回報

驗收條件：
1. manifest 的每個 SET / PARAM 列都有對應 .cs 檔，檔名 = 類別名
2. Dataload(IDataSource) 內除 source.Load<T> 與必要欄位指定外無其他資料處理
3. CsvDataSource：Data/ 下每個 Load 名稱都有對應 .csv；DbDataSource：每個 Load 都是完整 SQL，SELECT 欄位或 alias 對應生成 property
4. CSV 或 DB 查詢所得資料與 Model.md 逐筆一致
5. dotnet build 通過

回報格式：建立的檔案清單、資料來源與列數、Load 名稱 ↔ CSV 檔名或 SQL 對照表、build 結果。總長 ≤15 行。
```

## C4 · variable（變數層）

```text
目標：依工單的 VAR 列建立 Variable/Variable[B|C|I]_*.cs。
動機：變數前綴是 load-bearing——generator 依前綴判定型別並產碼，前綴取錯直接 compile error 或靜默變成另一種型別的模型。

輸入：manifest.md 的 VAR 列 + 對應 Model.md 行號區間
規範：以 `rg` 定位並讀 API guide §3

做法：照 §3 逐項做，規則不在本檔重述。本 agent 專屬邊界：
1. 前綴由 Model.md 型別欄決定（Binary → VariableB_、Continuous → VariableC_、Integer → VariableI_）；前綴是 load-bearing，取錯是型別錯不是命名錯
2. Model.md 的 LB/UB 若非預設值 → **不要寫進變數宣告**，記進回報，交給 C5 寫成一條 constraint

驗收條件：
1. manifest 每個 VAR 列都有對應檔，前綴與 Model.md 型別欄一致
2. [OptDim] 的 set 順序與 Model.md 的 Dim 欄順序一致
3. 無手寫 : VariableBase
4. dotnet build 通過

回報格式：檔案清單（類別名 | 前綴 | dims）、需要轉成 constraint 的 LB/UB 清單、build 結果。總長 ≤15 行。
```

## C5 · constraint（fan-out，每組一個 agent）

```text
目標：把工單指派給你的 constraint 群轉譯成 Constraint_*.cs。
動機：這是純機械轉譯。Model.md 左邊的項進 AddLHS、右邊的項進 AddRHS、比較符號直接對應 CreateXxx——動過手腳（移項、改號、翻方向）就再也無法逐條對回去驗證，而那種錯會一路活到求解出「看起來合理」的錯答案。

你負責的 unit：{{manifest 列號}}
Model.md 節錄：只讀 Projects/<Project>/Model/<Project>_Model.md 的第 {{起}}-{{迄}} 行（用 Read offset/limit）。NEVER 讀整份 Model.md。

規範：
1. 以 `rg` 定位並讀 API guide §4（Objective、pool、owner overload、保留 overload）與附錄 A
2. 用到的每個 API 簽名不確定 → 搜尋 §9 該 API 名，讀命中處 ±20 行

做法：照 §4 與附錄 A 逐項做，規則不在本檔重述。本 agent 專屬邊界：
1. 一條式子一個檔：左邊項 → AddLHS、右邊項 → AddRHS、比較符號 → CreateXxx，逐項對得回 Model.md
2. 建構子只收這條式子實際用到的 Set / Parameter / scalar；OptEngine 只從 Build(engine) 進來
3. 發現 Model.md 該條有歧義（缺符號宣告、pattern tag 對不上式子、預先移項）→ 立即停止並回報，NEVER 自行詮釋

驗收條件：
1. 指派的每條式子都有對應 code，且 LHS 項 ↔ AddLHS、RHS 項 ↔ AddRHS 逐項對得上
2. 比較方向與 Model.md 完全一致，未移項未改號
3. 無未由 Set / Parameter 提供的資料 magic number；結構 literal 可逐項對回 Model.md
4. 建構子不含 Dataload、不含 OptEngine
5. 沒有 string-name `CreateXxx` 或手工組名
6. dotnet build 通過

回報格式：
- 檔案路徑清單
- 逐條對照表：| Model.md 式子（≤1 行） | CreateXxx | LHS 項數 | RHS 來源 |
- 歧義而停止的項目（無則寫「無」）
總長 ≤15 行，NEVER 貼 code 全文。
```

## C6 · objective / C7 · program / C8 · solution

同 C5 格式，差異只在讀的節與驗收條件：

- **C6 objective**：讀 §4；驗收＝「是 Model.md OBJ 段的逐項轉譯、方向正確、無未宣告的資料 magic number；Model.md 無 OBJ 段則停止退回 Phase 1」
- **C7 program**：讀 §5 + manifest **全表**（它需要全域視圖才組裝得起來）；驗收＝「不可變四段 `import → 模型 → 實驗 → 正式跑` 均存在且順序正確、AddObjective 先於 AddConstraints、manifest 每個 unit 都被註冊；import / exp 永遠保留」
- **C8 solution**：讀 §6；驗收＝「ValidateRules 逐條把解代回 Model.md 每條 constraint、透過 `ISolutionSink` 或 `CsvCtrl.WriteSolution` 輸出；CSV 寫入端會自行建立 `Solution/`」

C7 是唯一拿到 manifest 全表的 coding agent——它的工作本質就是組裝，給它局部視圖反而做不出來。

## C9 · builder（build + fix loop）

```text
目標：跑 dotnet build，修到綠或用完 5 次上限。
動機：fix loop 有硬上限是因為第 5 次還沒過，通常代表某支檔案的轉譯方向錯了，繼續修只會把錯誤攤平到更多檔案——那時該退回重譯，不是再修一次。

做法：
1. cd Projects/<Project> && dotnet build
2. 失敗 → 讀錯誤訊息，定位到具體檔案與行
3. 修正前先確認：這是筆誤，還是轉譯方向錯？方向錯 → 停止，回報該 unit 需重譯
4. API 簽名錯（CS1061 / CS7036）→ 以 `rg` 搜尋 API guide §9 的 API 名確認正確簽名，NEVER 憑記憶改
5. 每輪回報「第 N 次 / 5：錯誤數 X → Y」
6. 第 5 次仍失敗 → 停止，回報剩餘錯誤與你的診斷

NEVER 為了讓 build 過而修改模型語意（改比較方向、拿掉 constraint、把數值改成常數）。
NEVER 修改 Model.md。

回報格式：最終 build 狀態、用了幾次 fix、每次修了什麼（一行一次）、需重譯的 unit（無則寫「無」）。總長 ≤15 行。
```

## V1 · checklist-verifier

```text
目標：依 checklist.md 的 AI 專案產出一致性契約逐條稽核專案，回報 PASS/FAIL；先保證所有專案的 canonical 架構一致，再驗證轉譯正確性。
動機：你是 fresh context 的稽核者。寫 code 的 agent 對自己的產出必然樂觀；此 contract 的每一條都要能機械阻止專案結構、命名或 API 寫法漂移。

輸入：Projects/<Project>/
規範：同層 checklist.md（全讀並逐條執行）

先做：checklist §0「Canonical 專案架構」與 §15「AI 最終靜態掃描」的 PowerShell 指令**照跑一遍**；任何未列出的手寫資料夾、缺少固定資料夾或錯名路徑均為 FAIL。命中靜態掃描時逐筆判定是否為舊 API 或反模式。
再做：§1 到 §14 各段逐條核對。

回報格式：
| 段 | 條目 | 判定 | 證據（檔案:行號） |
只列 FAIL 與 WARN，PASS 的用「A 段 8/8 PASS」一行帶過。
FAIL 項附「該怎麼修」一句。總長 ≤30 行。NEVER 自己修改任何檔案。
```

## V2 · back-translator（反向翻譯紅隊）

```text
目標：只讀 Constraint/ 與 Objective/ 的 .cs，把每支反推成數學式，再與 Model.md 對照，找出轉譯走樣。
動機：移項、改號、翻轉比較方向是本階段最致命也最隱形的錯——正向看 code 覺得合理，反推成數學式才顯形。你是唯一能抓到這類錯的關卡。

步驟（照順序，先做完 1 再看 2）：
1. 先只讀 Projects/<Project>/Constraint/*.cs 與 Objective/*.cs，逐支寫出它實際建立的數學式（把 AddLHS 的項寫左邊、AddRHS 的項寫右邊、CreateXxx 寫成比較符號）。此時 NEVER 開啟 Model.md。
2. 再讀 Model/<Project>_Model.md，逐條對照。

找這五類問題：
A. 移項（code 的 LHS/RHS 分佈與 Model.md 不同）
B. 比較方向翻轉（<= 寫成 >=，或 CreateXxx 用錯）
C. 係數或符號改變（含隱含的 -1）
D. 漏條或多條（Model.md 有的 code 沒有，或反之）
E. 迴圈範圍錯（∀ 的 set 與 code 的 foreach 對象不一致、缺內層迴圈）

回報格式：
- 逐支反推結果：| 檔案 | 反推的數學式（≤1 行） |
- 發現：| 類型 | 檔案:行號 | Model.md 該條 | 嚴重度(高/中/低) |
- 結論一行：轉譯忠實 / 需修正
總長 ≤30 行。NEVER 修改任何檔案。
```

## V3 · solve-verifier（解驗證協定）

```text
目標：跑 dotnet run，依解驗證協定四步驗證，回報 PASS/FAIL。
動機：看到 Optimal 就宣稱正確是不合格的。求解器只保證「在你給它的模型裡最優」，不保證那是你要的模型。

規範：以 `rg` 定位並讀 API guide §7.1（狀態分流）與 §7.2（解驗證四步）

四步（逐步回報）：
1. Status 七態診斷（依框架 `SolveStatus`，NEVER 只認 Optimal）：
   - Optimal → 往下
   - Feasible（有 incumbent、未證明最佳，可能撞 `TimeLimit` / `NodeLimit` / `IntegerSolutionLimit`）→ **照樣往下**，對 incumbent 驗 2-4，另記 MipGap 與 BestBound。「太慢」不是 Phase 2 的 FAIL，是 Phase 3 的進場條件
   - TimeLimit（中止且無任何可用解，ObjectiveValue / MipGap 皆 NaN）→ 沒有解可驗；用同一份 schema 縮小目前資料來源成小 instance，求到 Optimal 後完成 2-4，回報 verifiedOn: "small-instance:<說明>"
   - Infeasible → 讀 bin/Debug/net8.0/IISs/*.ilp 找最小衝突集
   - Unbounded → 查漏掉的上限 constraint
   - Error → 修正 solver / adapter 執行錯誤，不得當成無解
   - NotSolved → 尚未完成 `Solve()`，不得進入解驗證
2. 可行性代回：把解代回 Model.md 每條 constraint，確認 LHS op RHS 成立
3. 單位與量級：目標值與關鍵變數的單位、數量級對得上題目描述
4. BestBound sanity：核對 `engine.LastMetrics.BestBound`；max 的 incumbent ≤ BestBound，min 的 incumbent ≥ BestBound（容許 solver 數值誤差）

NEVER 把 solver log 全文讀進來——用 `rg` 抓 Status、objective、gap、time 幾行即可。

回報格式：
| 步驟 | 判定 | 數值證據 |
另附：Status 落在七態哪一態、目標值、MipGap / BestBound、關鍵變數摘要（≤5 行）、輸出檔路徑、
verifiedOn（production 或 small-instance:<說明>）。總長 ≤30 行。
```

</patterns>

<verification>

## 完成條件（C0 執行）

Phase 2 完成 = 下列**全部**成立：

1. C9 build 綠
2. V1 checklist 無 FAIL
3. V2 反向翻譯結論為「轉譯忠實」，無高嚴重度發現
4. V3 四步全 PASS
5. V1 已確認 canonical 專案架構與 AI 產出一致性契約全數通過；不把本檔當成人工覆核清單交付

任一 FAIL → 退回對應 agent 並附原驗收條件；同一 unit 重試 2 輪仍 FAIL → 停下問使用者。

## status.json

```json
{ "phase": "coding", "modelConfirmed": true, "manifestUnits": 0, "buildOk": false, "v1Pass": false, "v2Faithful": false, "solveVerified": false, "solveStatus": "Optimal", "verifiedOn": "production", "updated": "YYYY-MM-DD" }
```

</verification>

<fatal_implications>

- NEVER orchestrator 自己寫或改 `.cs`（只做派工、收工單狀態、gate）
- NEVER 在 manifest 有阻塞項時繼續轉譯——退回 Phase 1
- NEVER 給 constraint agent 整份 Model.md（只給它那條的行號區間）
- NEVER 讓任何 agent 整份讀 API guide
- NEVER 跳過 V2 反向翻譯就宣稱轉譯正確
- NEVER 為了 build 過而修改模型語意或 Model.md

</fatal_implications>
