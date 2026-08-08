# Model Design — Phase 1 Agent Workflow Prompts

<system_context>
Phase 1（建模）的 **multi-agent 執行層**：把 `CLAUDE.md` 的建模規則轉成「派誰、給什麼、驗什麼」的可複製 prompt。
規則單一來源仍是同層 `CLAUDE.md` + `linearization-patterns.md`；本檔 NEVER 重述規則，只規定調度。
設計前提：**不考慮 token 成本，但嚴格控制單一 context 的載入量**——寧可多派 6 個 agent，不讓任何一個 agent 讀滿視窗。
產物：`$OPT/AI-Modeling/Projects/<Project>/Model/<Project>_Model.md`（唯一交付物）。
</system_context>

<critical_notes>

## Context 鐵則（三 phase 共用，違反必爆視窗）

- NEVER 把題目原文 / Model.md 全文 / 中間產物貼進 orchestrator 對話 —— ALWAYS 落檔到 `_wip/<Project>/`，agent 之間**只傳路徑** —— Why: orchestrator 的 context 一旦被原始材料灌滿，後面的 gate 判斷就開始漏規則，而漏的正是你派它去把關的那些
- NEVER 讓任何 agent `Read` 整份 API guide 或整份長文件 —— ALWAYS 先 `grep -n "^## §"` 取行號，再 `Read offset/limit` 只讀該節
- MUST 單一 agent 的輸入預算 ≤ 800 行文字（規範 + 材料合計）；超過 → 再拆一層 fan-out，NEVER 靠「請精簡閱讀」自律
- MUST 回報上限：執行類 agent ≤ 15 行、稽核類 ≤ 30 行（逐條 PASS/FAIL）；證據引用 ≤ 5 行原文
- MUST orchestrator 的 context 只允許裝四種東西：**工單表、狀態表、PASS/FAIL 表、檔案路徑**。其餘一律在 subagent 內部消化
- MUST 平行 fan-out 一次 ≤ 6 個 agent —— Why: 回報同時湧入一樣會撐爆 orchestrator，分批收斂才守得住

## Phase 1 專屬

- MUST 1a 產物**逐句編號** `S001` 起 —— Why: 「每個子句都要有 role」這條規則原本無法機械驗證，編號後 auditor 才驗得出哪一句被靜默略過
- NEVER 讓同一個 agent 同時做「抽取」與「自驗」 —— ALWAYS 1d 派 fresh-context auditor —— Why: 產出者對自己的產出必然樂觀，自驗等於沒驗
- MUST 交付前跑 M6 反向紅隊（拿 Model.md 反推題目，與原題目對照）—— Why: auditor 驗「規則有沒有違反」，紅隊驗「語意有沒有走樣」，兩者抓的是不同類的錯

</critical_notes>

<paved_path>

## Agent 拓樸

```text
M0 orchestrator（主對話，不下場做事）
 │
 ├─ M1 normalizer ──────► _wip/1a-normalized.md      （去故事化 + 單位 + 逐句編號）
 ├─ M2 classifier ──────► _wip/1b-terminology.md     （語義判別 + Terminology Table）
 ├─ M3 structurer ──────► _wip/1c-draft.md           （SET/PARAM/VAR/OBJ 四段）
 ├─ M4 constraint-writer × N ─► _wip/1c-constraints/<Name>.md  （fan-out，每組一個）
 │        └─ 合併 ─────► Model/<Project>_Model.md
 ├─ M5 model-auditor ───► _wip/1d-audit.md           （fresh context，逐條 PASS/FAIL）
 └─ M6 adversary ───────► _wip/1d-redteam.md         （反向翻譯，抓語意走樣）
        │
        └─► 全 PASS → orchestrator 交付 + 停下等使用者 gate
```

| Agent | subagent_type | model | 輸入預算 | 產物 |
| --- | --- | --- | --- | --- |
| M1 normalizer | `general-purpose` | `sonnet` | 題目原文 | `1a-normalized.md` |
| M2 classifier | `general-purpose` | `opus` | 1a 產物 | `1b-terminology.md` |
| M3 structurer | `general-purpose` | `opus` | 1a + 1b | `1c-draft.md` |
| M4 constraint-writer | `general-purpose` | `opus` | 1a + 1b + 指派子句 + linearization-patterns | `<Name>.md` |
| M5 model-auditor | `verifier` | （定義內建） | Model.md + 自驗清單 | `1d-audit.md` |
| M6 adversary | `second-opinion` | （定義內建） | Model.md + 原題目 | `1d-redteam.md` |

M2/M3/M4 給 `opus`：語義判別與 linearization 選型屬 `judgment.md §1` 的「約束互相牽制 + 一次性高後果」——建模錯了下游 code 全部重寫。

## 工作區

中間產物一律放 **repo 層** `_wip/<Project>/`，NEVER 放進 `Projects/<Project>/` —— Why: 專案內結構受天條「八個資料夾 NEVER 增減、`Model/` 只放 `<Project>_Model.md`」管轄（`../CLAUDE.md`），流程暫存檔擠進去就破壞那條天條，而它正是 Phase 2 驗收會查的項目。

```text
$OPT/AI-Modeling/
├── _wip/<Project>/               ← 三 phase 共用交接區（repo 層，不受八資料夾天條管）
│   ├── 00-raw.md                 ← 題目原文（orchestrator 唯一一次落檔）
│   ├── 1a-normalized.md
│   ├── 1b-terminology.md
│   ├── 1c-draft.md
│   ├── 1c-constraints/<Name>.md
│   ├── 1d-audit.md
│   └── 1d-redteam.md
└── Projects/<Project>/
    ├── Model/<Project>_Model.md  ← 唯一交付物（M4 合併後產生）；Model/ 內不放別的
    └── status.json
```

`_wip/<Project>/` 是**交接介質**：每個 agent 讀前一棒的檔、寫自己那棒的檔。跨 session resume 時只要看它有什麼就知道走到哪，不依賴對話記憶。上方拓樸圖的 `_wip/xxx` 都是它的簡寫。

</paved_path>

<patterns>

## M0 · orchestrator（主對話自己執行，不派工）

1. 把使用者題目原文**原封**寫進 `_wip/<Project>/00-raw.md`（一次性，之後 NEVER 在對話中重述題目內容）
2. 讀同層 `CLAUDE.md`（約 130 行，在 800 行預算內，可全讀）
3. 依下列順序派工，每棒收到回報才派下一棒；M5 / M6 可平行
4. 任一 agent 回報 FAIL → 依 `$FW/orchestration/dispatch.md §升降級路徑` 處理，NEVER 自己下場改產物
5. 全 PASS → 交付 Model.md 摘要 + 「已套用假設」清單 + 追問清單，**停下等 gate**

## M1 · normalizer（1a 去故事化 + 單位正規化）

```text
目標：把最佳化題目原文轉成乾淨、self-contained、逐句編號的問題敘述。
動機：這是建模第一次降維。資料沒清乾淨就符號化，等於在雜訊上建模；編號是為了讓下游 auditor 能機械驗證「沒有子句被靜默略過」。

輸入：_wip/<Project>/00-raw.md（題目原文，先讀它）
規範：先讀 $FW/MILP Model/Model Design/CLAUDE.md 的 <paved_path> 1a 段（只讀這段）

做法：
1. 去掉背景故事、人物、動機敘述，只留資料與邏輯
2. 表格逐列轉成宣告句（一列一句，NEVER 整表原樣搬）
3. 每個數字都標單位；單位不一致（時/分、噸/公斤）換算成單一單位，並在該句標明換算式
4. 逐句編號 S001、S002……一句一個事實，複合句拆開
5. NEVER 引入任何數學符號、變數名、集合名——這階段還是純自然語言
6. NEVER 推導題目沒明說的數值（如「300元/班 ÷ 10小時 = 時薪30」）

輸出：寫入 _wip/<Project>/1a-normalized.md，格式：
| ID | 敘述 | 單位 | 換算 |
| S001 | 每台機器每週最多運轉 40 小時 | hour | — |

驗收條件（auditor 會逐條打分）：
1. 原文每個事實都對應到至少一個 S-id，無遺漏
2. 每個數字都有單位欄位，換算過的標明換算式
3. 全文無數學符號、無變數命名
4. 無題目未明說的推導值

回報格式：句數、換算了哪幾項（≤3 條）、你判定為背景故事而捨棄的內容（列出來，讓我覆核是否誤刪）。總長 ≤15 行，NEVER 貼產物全文。
```

## M2 · classifier（1b 語義判別 + Terminology Table）

```text
目標：把 1a 的每個編號子句歸類成 parameter / variable / derived / constraint / objective / irrelevant，並產出 Terminology Mapping Table。
動機：這步是防漏句與防亂設變數的關卡。子句沒歸類就進結構抽取，會出現「題目講了但模型沒有」的靜默漏洞——那種錯到求解出結果都看不出來。

輸入：_wip/<Project>/1a-normalized.md
規範：先讀 $FW/MILP Model/Model Design/CLAUDE.md 的 <paved_path> 1b 段與 <patterns> 的 Terminology Mapping Table 模板

做法：
1. 逐一處理每個 S-id，強制給一個 role；與模型無關的明確標 irrelevant 並寫原因，NEVER 靜默略過
2. NEVER 自動推導 derived 值；真需要 derived → 另立一列標 derived + 註明推導來源與依據的 S-id
3. 術語表外、你不確定語意的名詞 → 列進「待追問」，NEVER 自行詮釋
4. 命名用語意名稱（MachineCapacity、Assign），NEVER 單字母

輸出：寫入 _wip/<Project>/1b-terminology.md，含兩張表：
（表一）Terminology Mapping Table：Term | 中文語意 | Role | Unit | Derived? | Raw phrase | 來源 S-id
（表二）子句歸類覆蓋表：S-id | Role | 對應 Term | 備註（irrelevant 必填原因）

驗收條件：
1. 1a 的每個 S-id 在表二都出現且恰好一次
2. 每個 irrelevant 都寫了原因
3. 無自動推導的 derived；有 derived 的都註明來源
4. 無單字母命名

回報格式：各 role 的數量統計表、irrelevant 的 S-id 清單、待追問術語清單。總長 ≤15 行。
```

## M3 · structurer（1c 四段抽取：SET / PARAM / VAR / OBJ）

```text
目標：依 1b 的分類產出 Model.md 的 SET、PARAM、VAR、OBJ 四段（CONSTRAINT 段由後續 agent 分工，你不要寫）。
動機：這四段是下游 Phase 2 決定 C# 類別的依據，每個 metadata 欄位缺一項，Coding 階段就得靠猜。

輸入：_wip/1a-normalized.md、_wip/1b-terminology.md
規範：先讀 $FW/MILP Model/Model Design/CLAUDE.md 的 <patterns> 元素 metadata 段

做法：
1. SET 表：Set | 語意 | 成員範例 | → 程式
2. PARAM 表：Param | 語意 | Dim | 值 | → 程式（Dim 決定 Parameter 類別的 property）
3. VAR 表：Var | 語意 | Dim | 型別 | LB | UB（型別決定 VariableB_/I_/X_ 前綴，此欄 load-bearing）
4. OBJ：方向 + 所有項在 LHS，LaTeX $$...$$
5. 每個符號都標它來自哪些 S-id
6. 題目沒提 LB/UB → 套預設慣例表（LB=0, UB=INFTY；比例變數 0..1），並記進「已套用假設」

輸出：寫入 _wip/<Project>/1c-draft.md，段落順序：問題描述 → Terminology Mapping Table（從 1b 搬入）→ SET → PARAM → VAR →（CONSTRAINT 留空佔位）→ OBJ → 已套用假設

驗收條件：
1. 每個 VAR 標了型別 + LB + UB；每個 PARAM 標了 Dim
2. OBJ 段存在且標明 max/min，所有項在 LHS
3. 1b 中 role 為 parameter / variable / objective 的 Term 全數出現在對應段
4. 無單字母符號；每個符號標了來源 S-id
5. 「已套用假設」段列出所有套用預設慣例之處

回報格式：SET/PARAM/VAR 各幾個、OBJ 一行、已套用假設條數。總長 ≤15 行。
```

## M4 · constraint-writer（1c CONSTRAINT 段，fan-out）

orchestrator 先把 1b 中 role=constraint 的 S-id 分組（一個語意群一組，例如「產能上限」「人力覆蓋」「時間連續」），**每組派一個 agent**，一次最多 6 個。組數 ≤ 3 時可合併成一個 agent。

```text
目標：把指派給你的子句寫成 Model.md 的 CONSTRAINT 條目（LaTeX + pattern tag + Dim）。
動機：constraint 是 Phase 2 逐條機械轉譯的對象。寫成原形（LHS op RHS）才能對回 AddLHS/AddRHS 驗證；預先移項的式子在 code 端永遠對不回去。

你負責的子句：{{S-id 清單}}
群組名稱：{{例：Capacity}}

輸入：_wip/1a-normalized.md、_wip/1b-terminology.md、_wip/1c-draft.md 的 SET/PARAM/VAR 段（符號只能用這裡已宣告的）
規範：
- 先讀 $FW/MILP Model/Model Design/CLAUDE.md 的 <patterns> CONSTRAINT 段
- 再讀 $FW/MILP Model/Model Design/linearization-patterns.md（79 行，全讀）

做法：
1. 每條寫成 `### <Name> `[pattern tag]` ∀ <Dim>` + LaTeX $$...$$ + 一句中文說明
2. NEVER 移項 / 化簡 / 翻轉比較方向：左邊項留左邊、右邊項留右邊
   ✅ Good: $x \le a \cdot y$   ❌ Bad: $x - a y \le 0$
3. 每個 sum 標明 index 範圍（∀ 哪個 set、over 哪個 set）
4. 邏輯 / 非線性條件 → 查 linearization-patterns 套對應 template 填空，NEVER freehand
5. 需要 Big-M → 取「該式最緊的合法上界」，並把 M 立成具名 PARAM，寫明推導依據
6. 只用 1c-draft 已宣告的符號；需要新符號 → 停下回報，NEVER 自己補宣告
7. 一律 Hard constraint，NEVER 討論 soft / penalty（那屬 Phase 3）

輸出：寫入 _wip/<Project>/1c-constraints/{{群組名稱}}.md

驗收條件：
1. 指派的每個 S-id 都對應到至少一條 constraint
2. 每條都有 pattern tag + Dim + LaTeX + 中文說明
3. 每條都是 LHS op RHS 原形，無預先移項
4. 出現的每個符號都能在 1c-draft 的 SET/PARAM/VAR 找到宣告
5. CONSTRAINT 內無裸數字（全部是具名 PARAM）
6. 用到 Big-M 的有寫明上界推導

回報格式：條數、各條的 pattern tag 一覽（名稱 | tag）、新增的 PARAM 需求（如 Big-M）、需要新符號而卡住的項目。總長 ≤15 行，NEVER 貼 LaTeX 全文。
```

合併：orchestrator 用 `Read` 逐檔取回後**直接寫入** Model.md 的 CONSTRAINT 段（機械拼接，不改寫內容），或派一個 `general-purpose` 做拼接並要求「NEVER 修改任何一條式子的內容，只做順序編排」。

## M5 · model-auditor（1d 自驗 gate，fresh context）

```text
目標：對照建模自驗清單，逐條稽核 Model.md，回報 PASS/FAIL。
動機：你是 fresh context 的稽核者，沒有參與建模，所以看得到產出者看不到的問題。你不修東西，只判定。

輸入：Projects/<Project>/Model/<Project>_Model.md、_wip/1a-normalized.md、_wip/1b-terminology.md
規範：$FW/MILP Model/Model Design/CLAUDE.md 的 <self-check> 段（9 條）+ <fatal_implications>

逐條驗（每條給 PASS / FAIL + 證據）：
1. 每個數字都帶單位，單位不一致已換算標明
2. 1a 每個 S-id 在 1b 都有 role，無靜默略過（用 S-id 逐一比對，不要抽樣）
3. Terminology Mapping Table 在 Model.md 內，且無獨立 Glossary.md
4. 宣告先於使用：CONSTRAINT / OBJ 出現的每個符號都已在 SET/PARAM/VAR 宣告
5. CONSTRAINT / OBJ 內無裸數字
6. 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
7. 每條 CONSTRAINT 是 LHS op RHS 原形 + 有 pattern tag + 有 Dim
8. 無單一字母符號
9. 有「已套用的預設假設」清單
10. Model/ 目錄下無任何 .cs 檔

回報格式：
| # | 判定 | 證據（檔案:行號 或 引用 ≤2 行） |
FAIL 項另附「該怎麼修」一句。總長 ≤30 行。NEVER 自己動手修改任何檔案。
```

## M6 · adversary（反向翻譯紅隊）

```text
目標：只看 Model.md，用自然語言反推「這個模型在描述什麼問題」，再與原題目對照，找出語意走樣。
動機：auditor 驗的是「規則有沒有被違反」，你驗的是「模型有沒有在解另一道題」。後者規則檢查抓不到——一個完全合規的模型也可能漏掉題目的一整個限制。

步驟（照順序，先做完 1 再看 2）：
1. 先只讀 Projects/<Project>/Model/<Project>_Model.md，寫出你理解的問題敘述（決策什麼、受什麼限制、追求什麼）。此時 NEVER 開啟原題目。
2. 再讀 _wip/00-raw.md 與 _wip/1a-normalized.md，逐項對照你的反推版本與原題目。

找這五類問題：
A. 原題目有、模型沒有的限制（漏句）
B. 模型有、原題目沒有的限制（多加的假設）
C. 語意偏移（模型的式子技術上成立，但描述的不是原題目那件事）
D. 邊界情境未涵蓋（時間 wrap-around、空集合、單一元素）
E. 目標函數與題目訴求不一致（方向、缺項、權重來源不明）

回報格式：
- 你的反推敘述（≤8 行）
- 發現：| 類型 | 描述 | 涉及 S-id 或 constraint 名 | 嚴重度(高/中/低) |
- 結論一行：可交付 / 需回 M3-M4 修正
總長 ≤30 行。NEVER 修改任何檔案。
```

</patterns>

<verification>

## Gate（M0 執行）

M5 全 PASS 且 M6 無「高」嚴重度發現 → 向使用者交付：

1. Model.md 路徑 + 規模摘要（幾個 SET / PARAM / VAR / CONSTRAINT）
2. 「已套用的預設假設」清單（逐條，讓使用者一併確認）
3. 追問清單（M2 的待追問術語 + M4 卡住的項目）
4. 明確一句：**「請確認模型；說『模型確認』或『開始實作』我才進 Phase 2。」**

FAIL 處理：M5 FAIL 的條目退回對應 agent（結構問題回 M3、constraint 問題回 M4）並附原驗收條件；M6 高嚴重度發現退回 M2 重跑語義判別。同一項重試 2 輪仍 FAIL → 停下問使用者（`judgment.md §換路訊號`）。

## status.json

```json
{ "phase": "modeling", "modelConfirmed": false, "wipStage": "1d", "auditPass": true, "redteamHigh": 0, "updated": "YYYY-MM-DD" }
```

</verification>

<fatal_implications>

- NEVER orchestrator 自己下場寫 Model.md 的內容（只做拼接與 gate）—— 下場就開始累積原始材料，gate 判斷力隨之下降
- NEVER 未經 M5 + M6 就向使用者交付模型
- NEVER 任何 agent 在本 phase 產生 `.cs`
- NEVER 把中間產物貼回主對話——一律落檔傳路徑
- NEVER 跳過 1a 的逐句編號（跳過就等於放棄漏句偵測能力）

</fatal_implications>
</content>
</invoke>
