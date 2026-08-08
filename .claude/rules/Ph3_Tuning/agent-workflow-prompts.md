# Foundation Tuning — Phase 3 Agent Workflow Prompts

<system_context>
Phase 3（調校）的 **multi-agent 執行層**：把 `CLAUDE.md` 的 tuning 閉環轉成「派誰、給什麼、驗什麼」的可複製 prompt。
規則單一來源仍是同層 `CLAUDE.md` + `solver-tuning-research.md`；本檔 NEVER 重述旋鈕表與評分規則，只規定調度。
**使用者提出才啟動**，NEVER 主動建議 tuning。
完成的定義不是「跑出實驗報表」，而是 **champion 已 promotion 到 production baseline 且 production 驗證通過**（或有證據支持保留原 baseline）。
</system_context>

<critical_notes>

## Context 鐵則（三 phase 共用）

- NEVER 把 solver log / experiment JSON / Trial 明細全文讀進任何 context —— ALWAYS `grep` 抽欄位（Status、objective、MipGap、time、nodes）—— Why: 單次 MIP 的 solver log 可以上萬行，讀一次就把整個視窗吃光，而你要的只有五個數字
- NEVER 把每輪分析結果留在對話裡累積 —— ALWAYS 落檔 `_wip/t<N>-*.md`，orchestrator 只持有一張跨輪摘要表 —— Why: tuning 是多輪迭代，第 4 輪時前 3 輪的原始數據還留在 context，判斷力已經被稀釋
- MUST 單一 agent 輸入預算 ≤ 800 行；回報上限：執行類 ≤ 15 行、分析類 ≤ 30 行
- MUST orchestrator 只持有：**輪次摘要表、baseline 現值、promotion 狀態**
- MUST 平行 fan-out 一次 ≤ 6 個 agent

## Phase 3 專屬

- MUST 先過正確性 gate 才開始 —— Why: 調快一個錯模型沒有任何價值，而且會讓錯誤看起來更可信
- NEVER 讓產生 variants 的 agent 同時做 champion 判定 —— ALWAYS 分成 T2（設計）與 T4（分析）—— Why: 設計者對自己的 variant 有偏好，會不自覺放寬 eligibility gate
- MUST promotion 決策派 `second-opinion` 裁決 —— Why: promotion 會改寫 `Program.cs` 的 production baseline，屬 `judgment.md §1` 的「一次性高後果」；改壞了要到下次 production 求解才發現
- NEVER 只跑 experiment 就宣稱 tuning 完成 —— ALWAYS 走完 promotion + production 驗證 + `TuningHistory.md` 落檔

</critical_notes>

<paved_path>

## Agent 拓樸

```text
T0 orchestrator（主對話，不下場調參）
 │
 ├─ T1 scope-guard ─────────► _wip/t<N>-triage.md      （判「這是不是 tuning」+ 本輪量化目標）
 │      └─ 要動資料 / 結構 → 停止 Phase 3，退回對應 phase
 │      └─ infeasible / unbounded → 前提破裂，infeasible 先派 T1b 取證再退回
 ├─ T2 variant-designer ────► _wip/t<N>-plan.md        （本輪 variants，一次一旋鈕）
 ├─ T3 experiment-runner ───► _wip/t<N>-trials.md      （跑 OptExperiment，只回關鍵欄位）
 ├─ T4 analyst ─────────────► _wip/t<N>-analysis.md    （eligibility → lexicographic → variability）
 ├─ T5 promotion-judge ─────► _wip/t<N>-verdict.md     （second-opinion 裁決 promote / retain）
 ├─ T6 promoter ────────────► Program.cs + TuningHistory.md
 └─ T7 promotion-verifier ──► _wip/t<N>-prodverify.md  （build + 無參數 production + ValidateRules）
```

| Agent | subagent_type | model | 職責邊界 |
| --- | --- | --- | --- |
| T1 scope-guard | `general-purpose` | `sonnet` | 只判範圍，不動手 |
| T1b iis-analyst | `general-purpose` | `opus` | 讀 `.ilp` 找最小衝突集（取證用，不修） |
| T2 variant-designer | `general-purpose` | `opus` | 只產 config plan，不執行 |
| T3 experiment-runner | `general-purpose` | `sonnet` | 只執行與抽數，不判優劣 |
| T4 analyst | `general-purpose` | `opus` | 只判優劣，不寫回 code |
| T5 promotion-judge | `second-opinion` | （內建 opus） | 只裁決，不動手 |
| T6 promoter | `general-purpose` | `opus` | 只寫回，不重新判定 |
| T7 promotion-verifier | `verifier` | （內建） | 只驗收，不修東西 |

職責切得這麼細的原因：tuning 最常見的失敗不是算錯，是**同一個 agent 既設計又評分**，於是「我設計的 variant 贏了」。切開後每個 agent 只拿得到判斷所需的最小資訊。

## 工作區

```text
$OPT/AI-Modeling/
├── _wip/<Project>/t<N>-*.md   ← 每輪中間產物（N = 輪次）；repo 層，NEVER 放進專案
└── Projects/<Project>/
    ├── TuningHistory.md       ← 永久 provenance（每輪一節）——這份 MUST 在專案根
    ├── Program.cs             ← 唯一 productionBaseline 所在
    └── bin/Experiments/*.json ← 框架產出；NEVER 全文讀，用 grep 抽欄位
```

`_wip/` 與 `TuningHistory.md` 的差別是刻意的：前者是本輪拋棄式草稿，放 repo 層才不會撞到「八資料夾 NEVER 增減」天條；後者是規範明文要求的專案根永久記錄（`../CLAUDE.md`），天條管的是資料夾，專案根放檔案不受限。

`bin/Experiments/*.json` 會被 clean build 清掉，所以**決策證據必須落到 `TuningHistory.md`**——那才是可追溯的來源。

</paved_path>

<patterns>

## T0 · orchestrator（主對話）

1. 確認正確性 gate：`status.json` 的 `solveVerified == true`；否則停止並要求先完成 Phase 2
2. 讀同層 `CLAUDE.md`（約 120 行，可全讀）
3. 讀 `Program.cs` 的 `productionBaseline` 現值（只讀該 initializer 區塊，不讀全檔）
4. 依序派 T1 → T2 → T3 → T4 → T5；判定 promote 才派 T6 → T7
5. 每輪結束更新輪次摘要表（orchestrator 唯一持有的跨輪視圖）：

| 輪 | 目標 | variants | champion | 決策 | production 驗證 |
| --- | --- | --- | --- | --- | --- |
| r1 | gap 8% → 3% | 3 | r1-emphasis=optimal | promote | PASS |

**跨輪停止**：連續 3 輪無實質改善 → 停止 tuning 並回報，把「可能要改模型結構」當建議交還使用者，NEVER 自行升級去動模型。

## T1 · scope-guard（範圍把關）

```text
目標：判定「這件事是不是 tuning」，並輸出本輪要打的量化目標。
動機：Phase 3 只動 CplexConfig，前提是模型與資料凍結且已 feasible。前提不成立卻硬調，會燒掉好幾輪實驗才發現方向從一開始就錯。

輸入：
- 使用者的訴求原文：{{貼在這裡}}
- _wip/<Project>/v3-solve.md（Phase 2 的解驗證結果）
- Program.cs 的 productionBaseline 現值（只讀該區塊）
規範：讀 $FW/MILP Model/Foundation Tuning/CLAUDE.md 的 <paved_path> 範圍界線段

判定規則：
- ✅ 是 tuning：模型正確且有解，症狀是 timeout / gap 下不去 / 太慢 → 只調 CplexConfig
- ❌ 要換參數值或換一批資料 → 不是 tuning，換 CSV 重跑即可
- ❌ 要加刪約束、改 Big-M、reformulation → 不是 tuning，退回 Phase 1 Model Design
- ❌ Infeasible / Unbounded → 前提破裂，退回 Phase 1 / 2；Infeasible 先取 IIS 證據
- 判不出來 → 明說判不出來並列出你需要的資訊，NEVER 猜一個

另外輸出本輪的量化目標（例：「gap 從 8% 降到 3% 以內，時間不超過 600s」）。目標寫不出數字 → 回報卡住，NEVER 用「更快」這種無法驗收的目標。

輸出：寫入 _wip/<Project>/t{{N}}-triage.md

回報格式：判定（是 tuning / 退回哪個 phase）、依據（≤3 行）、本輪量化目標、建議先動的旋鈕方向（不要給具體值，那是 T2 的事）。總長 ≤15 行。
```

**分流**：判定非 tuning → orchestrator 停止 Phase 3，告知使用者要退回哪個 phase、為什麼；`infeasible` 先派 T1b 取證再退回。

## T1b · iis-analyst（僅 infeasible 時，取證用）

```text
目標：讀 IIS 輸出，找出最小衝突約束集合，判斷是模型錯還是資料錯。
動機：Infeasible 幾乎都是模型或資料的錯，不是 solver 的錯。在這裡調旋鈕沒有任何意義——你產出的是「該退回哪裡、退回去要修什麼」的證據，不是修法本身。

輸入：Projects/<Project>/bin/Debug/net8.0/IISs/*.ilp
NEVER 整檔讀——先 grep 出約束名稱清單，再針對命中的名稱回 Constraint/ 找對應 .cs 與 Model.md 條目。

做法：
1. 列出 IIS 內的約束名稱與變數界限
2. 對每個名稱，回 Model.md 找該條的語意
3. 判定衝突根因：資料值矛盾（例：需求 > 總產能）／模型結構矛盾（兩條 constraint 互斥）／界限設錯
4. NEVER 建議改成 soft constraint——放鬆模型是改語意，屬 Phase 1 由使用者決定，不是 Phase 3 的解法
5. NEVER 自己動手修任何檔案——你只出證據

回報格式：IIS 約束清單（名稱 | Model.md 條目）、根因判定、該退回哪個 phase、退回後要修什麼（資料 / 結構 / 界限，各附一句後果）。總長 ≤20 行。
```

## T2 · variant-designer（本輪實驗設計）

```text
目標：設計本輪的 config variants 與實驗計畫，寫成可直接貼進 Program.cs 的 code 片段。
動機：一次只改一個旋鈕，才知道是哪個旋鈕起作用。一次改三個然後變快了，你學不到任何可複用的知識，下一輪只能重新亂試。

輸入：_wip/t{{N}}-triage.md、Program.cs 的 productionBaseline 現值
規範：
1. 讀 $FW/MILP Model/Foundation Tuning/CLAUDE.md 的 <patterns> OptExperiment 段與常用 solver 決策表
2. 手動掃描已連續無效或要評估自動調參工具 → 讀同層 solver-tuning-research.md

做法：
1. 本輪 baseline 一律從 productionBaseline .Clone() 而來，NEVER 另立一份平行設定
2. 每個 variant 從 baseline .Clone() 後**只改一個旋鈕**，label 寫明改了什麼：r{{N}}-emphasis=optimal
3. variant 數 3–5 個；experiment name 為 <project>-tuning-r{{N}}（每輪遞增，同名會 append 導致舊 Trial 混入）
4. 正式 tuning 需 3–5 seeds 與 hold-out instances；第一個 solve 當 warm-up 排除；variant 執行順序跨 seed 輪替
5. NEVER 用 tune delegate 突變共用 config——一律 Clone() 產具體物件

輸出：寫入 _wip/<Project>/t{{N}}-plan.md，含：
- variants 表：| label | 改動的旋鈕 | 值 | 預期效果 | 依據 |
- 可直接貼用的 C# 片段
- 本輪的評分優先序（沿用規範的 lexicographic：解品質 → objective/gap → 穩健 runtime）

驗收條件：
1. 每個 variant 相對 baseline 只有一處差異（逐項比對，寫在表上）
2. experiment name 含本輪輪次且與歷史不重名
3. baseline 來自 productionBaseline.Clone()
4. seeds / warm-up / 順序輪替都在計畫中寫明

回報格式：variants 表（≤5 列）、experiment name、seeds 設定。總長 ≤15 行。
```

## T3 · experiment-runner（執行 + 抽數）

```text
目標：執行本輪 OptExperiment，把結果抽成一張精簡的 Trial 表。
動機：你只負責跑與抽數，NEVER 判斷誰贏——判優劣是另一個 agent 的事，你先下結論會影響它。

輸入：_wip/t{{N}}-plan.md
做法：
1. 把 plan 的 C# 片段套進 Program.cs 的 exp 分支（只動 exp 分支，NEVER 動 productionBaseline）
2. dotnet build → dotnet run -- exp
3. 從 bin/Experiments/<name>.json 抽每個 Trial 的：label、Status、objective、MipGap、runtime、nodes、seed
   NEVER 整份 JSON 讀進 context——用 grep / 逐欄抽取
4. solver log 同理：只 grep Status、objective、gap、time 幾行

輸出：寫入 _wip/<Project>/t{{N}}-trials.md：
| Trial label | seed | Status | objective | MipGap | runtime(s) | nodes |

驗收條件：
1. plan 的每個 variant × seed 都有對應列
2. warm-up 那次已標記排除
3. 未修改 productionBaseline（附 git diff 摘要佐證）

回報格式：Trial 表（≤15 列）、執行總時間、異常（crash / 無解 / 逾時）清單。總長 ≤20 行。NEVER 下「哪個比較好」的結論。
```

## T4 · analyst（champion 判定）

```text
目標：依規範的評分規則從本輪 Trial 選出 champion，或判定「無人勝出，保留 baseline」。
動機：runtime 快幾毫秒不是改善——MIP 的 performance variability 本來就有數個百分點。改善必須大於 baseline 自身的變異，否則你 promote 的是雜訊。

輸入：_wip/t{{N}}-trials.md、_wip/t{{N}}-triage.md（本輪量化目標）
規範：讀 $FW/MILP Model/Foundation Tuning/CLAUDE.md 的 <paved_path> AI tuning 閉環與 champion promotion 段（步驟 4–5 與 lexicographic gate）

做法（照順序）：
1. eligibility gate：淘汰 error / Infeasible / Unbounded / 無可行解 / 未達目標品質門檻的 candidate
2. lexicographic 比較：① 解品質與 Status ② objective 與 MipGap 是否達本輪目標 ③ 前兩者相同或都達標，才比穩健彙總後的 runtime
3. runtime 用 shifted geometric mean 彙總，timeout 用 PAR10；NEVER 用單次牆鐘時間排序
4. 判斷改善是否大於 baseline 自身 variability（跨 seed 的離散程度）；沒有明顯超出 → 結論是「無人勝出」
5. nodes / iterations 只作診斷與 tie-break

輸出：寫入 _wip/<Project>/t{{N}}-analysis.md，含：eligibility 淘汰名單 + 理由、彙總後比較表、champion（或「無人勝出」）+ 理由、baseline variability 估計

驗收條件：
1. 每個被淘汰的 candidate 都寫了淘汰理由
2. runtime 用了穩健彙總（寫明方法與計算值），非單次時間
3. 結論明確標示改善幅度與 baseline variability 的關係
4. 「無人勝出」是合法結論，NEVER 為了有結果硬選一個

回報格式：champion（或 retain）、改善幅度、variability 對照、淘汰名單一行。總長 ≤30 行。
```

## T5 · promotion-judge（second-opinion 裁決）

```text
目標：獨立裁決本輪該 promote champion 還是 retain 現有 baseline。
動機：promotion 會改寫 Program.cs 的 production baseline，是長期沿用且事後難察覺的變更——改壞了要到下次 production 求解才發現。所以由不參與實驗設計與分析的你來裁決。

輸入（只給這三份，不給實驗過程）：
- _wip/t{{N}}-triage.md（本輪目標）
- _wip/t{{N}}-trials.md（原始 Trial 數據）
- _wip/t{{N}}-analysis.md（分析結論）

裁決依據：
1. analysis 的 eligibility gate 有沒有放水（拿 trials 原始數據覆核，不要只看結論）
2. 改善幅度是否確實大於 variability，而非落在雜訊範圍
3. 是否只在單一 instance / 單一 seed 上贏——那不足以 promote
4. 有沒有隱藏代價（例：runtime 變快但 gap 變差、記憶體用量暴增）
5. 本輪量化目標是否真的達成

輸出：寫入 _wip/<Project>/t{{N}}-verdict.md

回報格式：
- 裁決：PROMOTE <champion label> / RETAIN baseline
- 理由（≤5 行）
- 你不同意 analysis 的地方（無則寫「無」）
- 若 PROMOTE：promotion 後要特別盯的風險（≤2 條）
總長 ≤20 行。NEVER 修改任何檔案。
```

## T6 · promoter（寫回 baseline + provenance）

```text
目標：把 champion 的設定寫回 Program.cs 的 productionBaseline，並在 TuningHistory.md 留下永久 provenance。
動機：bin/Experiments/*.json 會被 clean build 清掉。沒寫進 TuningHistory.md 的決策，下一輪就會有人重做同一組實驗。

前置：_wip/t{{N}}-verdict.md 的裁決必須是 PROMOTE；RETAIN 則跳過寫回，只做 TuningHistory 記錄。

做法：
1. 把 champion 的 ConfigSnapshot.SolverSpecific 對應值**明確寫成字面值**填進 productionBaseline initializer
   NEVER 讓 production 在 runtime 讀「目前最快的一列」，NEVER 在執行期改寫 source
2. 更新 initializer 上方註解：來源 experiment name + champion Trial label + 日期
3. 在專案根 TuningHistory.md 新增一節，固定欄位：日期、round、experiment name、baseline Trial、champion/candidate Trial、instances/seeds/彙總方法、Status/objective/gap、before/after config diff、promote|retain|rejected 決策、理由、（production 驗證欄先留待 T7 補）
4. NEVER 同時維護「實驗 baseline」與「production config」兩份設定

驗收條件：
1. Program.cs 只有一顆具名 productionBaseline，且值與 champion 完全一致
2. initializer 上方註解含 experiment + Trial + 日期
3. TuningHistory.md 該節所有欄位齊全，before/after diff 逐項可讀
4. git diff 只動了 Program.cs 與 TuningHistory.md

回報格式：before/after config diff（逐項）、TuningHistory 節標題、git diff 檔案清單。總長 ≤15 行。
```

## T7 · promotion-verifier（production 驗證）

```text
目標：驗證 promotion 後的 production 路徑仍然正確，並把結果補回 TuningHistory.md。
動機：tuning 改的是 solver 行為，不該改變解的正確性。如果 promotion 後 ValidateRules 掛了或目標值變了，代表這顆設定動到了不該動的東西。

做法：
1. dotnet build
2. dotnet run（無參數，走 production 路徑，NOT exp）
3. 確認：Status、objective、MipGap 與預期一致；OnSolved / ValidateRules 全數通過
4. 與 promotion 前的 production 結果對照：objective 應相同或在可接受品質門檻內
5. 把驗證結果補進 TuningHistory.md 同一筆記錄

判定：
- 通過 → PASS，該設定成為下一輪 baseline
- 失敗或回退 → FAIL：撤銷本次 promotion（還原 productionBaseline），在 TuningHistory 標記 rejected 並寫明失敗現象

回報格式：build 結果、Status/objective/gap、ValidateRules 結果、before/after 對照、最終判定（PASS/FAIL）。總長 ≤20 行。
```

</patterns>

<verification>

## 每輪完成條件（T0 執行）

一輪完成 = 下列全部成立：

1. T4 有明確結論（champion 或「無人勝出」），且證據落檔
2. T5 裁決完成
3. PROMOTE → T6 寫回 + T7 production 驗證 PASS；RETAIN → TuningHistory.md 有 retain 記錄與證據
4. `TuningHistory.md` 該輪節位欄位齊全
5. `status.json` 更新

**NEVER 把「跑出 experiment CSV」當完成。** 沒有 promotion 或沒有 retain 證據，這輪等於白跑——下一輪會有人重做同一組實驗。

## status.json

```json
{ "phase": "tuning", "tuningRound": 1, "productionBaseline": "r1-emphasis=optimal",
  "baselineSourceExperiment": "<project>-tuning-r1", "baselineSourceTrial": "Canonical | r1-emphasis=optimal",
  "promotionVerified": true, "updated": "YYYY-MM-DD" }
```

## 停止條件

- 連續 3 輪無實質改善 → 停止 tuning，回報並把「可能要改模型結構」當建議交還使用者
- T1 判定非 tuning → 立即停止，告知退回哪個 phase
- T7 連續 2 輪 FAIL → 停止，代表 promotion 流程本身有問題（`judgment.md §換路訊號`）

</verification>

<fatal_implications>

- NEVER 未過正確性 gate 就開始（`status.json` 的 `solveVerified` 必須為 true）
- NEVER 主動發起 tuning——使用者提出才做
- NEVER 在本 phase 改資料或模型（`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 全程唯讀）
- NEVER 用 soft constraint / penalty 繞過 infeasible
- NEVER 同一 agent 既設計 variant 又判定 champion
- NEVER 跳過 T5 second-opinion 就 promote
- NEVER 讀 solver log / experiment JSON 全文
- NEVER 以 tuning 名義移項、改號、翻方向或四捨五入
- NEVER 只留 `bin/Experiments/*.json` 當證據（clean build 就沒了）

</fatal_implications>
</content>
