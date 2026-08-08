# MILP DevPipeline — 整體任務與階段契約

本檔是 **pipeline 層的唯一權威**：這條開發線在做什麼、切成幾段、每段吃什麼吐什麼、什麼條件才准往下走、階段之間靠什麼交接。

分層界線（避免內容再度四散）：

| 層 | 管什麼 | 權威檔 |
| --- | --- | --- |
| 天條 | 全流程不可違反的硬規則 | [`../AGENTS.md`](../AGENTS.md) |
| **pipeline** | **階段切分、I/O 契約、交接物、狀態機** | **本檔** |
| 執行細則 | 某一階段怎麼做 | `../Ph1_Modeling/` `../Ph2_Coding/` `../Ph3_Tuning/` |
| 調度 | 派誰、給什麼、驗什麼 | 各 phase 的 `agent-workflow-prompts.md` |

本檔 NEVER 重述天條與執行細則，只定義契約與交接。

## 整體任務

**輸入**：一段自然語言的最佳化題目（LP / IP / MILP），可能夾帶表格、原始報表或生成規格。

**輸出**：`Projects/<Project>/` 底下一個可 build、可求解、解已驗證的 OptimFoundation CPLEX C# 專案，附一份與 code 逐條對得上的數學模型文件。

**中間的不變式**：模型先鎖定，再機械轉譯，最後才碰效能。每一步都必須能反向對回上一步——Model.md 對得回題目原文，`.cs` 對得回 Model.md，tuning 後的解對得回 tuning 前。任何一環對不回去，這條 pipeline 就失去驗證能力，錯誤會一路活到「求解成功、答案是錯的」。

## 唯一路線：三階段 phase gate

本框架**只允許**依 Phase 1 → Phase 2 → Phase 3 分階段完成，階段之間有明確 gate。**沒有第二條路線。**

- NEVER 走全自動 / 一路直通 / 免 gate 的量產路線 —— Why: 這條 pipeline 的全部驗證能力來自「每一步都能反向對回上一步」；跳過 gate 等於同時放棄模型確認、解驗證協定、promotion 驗證三道關卡，而錯誤不會報錯，它會一路活到「求解成功、答案是錯的」
- NEVER 為了趕時間把兩個階段併成一次交付。要一次做多題 → 逐題依序走三階段
- 已停用：`workflows/automated/` 的 16-stage 量產路線。該資料夾僅供歷史查閱，NEVER 引用它的規則、stage prompt 或 `status.json` schema

執行入口是三個 skill：[`modeling`](../../skills/modeling/SKILL.md) → [`coding`](../../skills/coding/SKILL.md) → [`tuning`](../../skills/tuning/SKILL.md)，流程總綱見 [`../../workflows/interactive/README.md`](../../workflows/interactive/README.md)。

## 三階段契約總表

| | Phase 1 · Modeling | Phase 2 · Coding | Phase 3 · Tuning |
| --- | --- | --- | --- |
| **Input** | 題目原文（文字或檔案路徑） | 已確認的 `Model/<Project>_Model.md` | 已 `solveVerified` 的專案 + 效能症狀 |
| **本質** | 降維：自然語言 → 數學 | 翻譯：數學 → C#，零詮釋 | 實驗：只動 solver 旋鈕 |
| **Output** | `Model/<Project>_Model.md` | 八資料夾專案，build 綠、解已驗證 | 更新後的 `productionBaseline` + `TuningHistory.md` |
| **出口 gate** | 使用者明說「模型確認」/「開始實作」 | 解驗證協定四步全過 | champion promotion 後 production 重跑通過 |
| **啟動條件** | 使用者給題目 | 上一階段 gate 通過 | **使用者主動提出**（NEVER 自己建議） |
| **退場** | 術語不明 → 追問 | Model.md 有歧義 → 退回 Phase 1 | 前提破裂 → 退回 Phase 1 / 2 |
| **skill** | [`modeling`](../../skills/modeling/SKILL.md) | [`coding`](../../skills/coding/SKILL.md) | [`tuning`](../../skills/tuning/SKILL.md) |

**跨階段鐵則**：階段只能往前走或整段退回，NEVER 在下游階段補上游的決定。Phase 2 遇到模型歧義的正解是停下退回，不是選一種解釋繼續；Phase 3 遇到 infeasible 的正解是退回，不是加 penalty 讓它有解。

## Phase 1 · Modeling — 輸入輸出契約

**Input**：題目原文。落檔到 `_wip/<Project>/00-raw.md` 後，對話中 NEVER 再重述原文。

**Output**：`Projects/<Project>/Model/<Project>_Model.md`，唯一交付物，固定八段順序：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

**輸出契約**（下游 Phase 2 逐項在吃，缺一項轉譯就得猜）：

| 段 | 必填欄 | 下游用途 |
| --- | --- | --- |
| Terminology | Term / 語意 / Role / Unit / Derived? / Raw phrase | 決定類別命名 |
| SET | 名 / 語意 / 成員範例 | 決定建哪幾顆 `Set_*` |
| PARAM | 名 / 語意 / **Dim** / 值 | 決定 `[OptDim]` property |
| VAR | 名 / 語意 / Dim / **型別** / LB / UB | 決定 `VariableB_` / `X_` / `I_` 前綴 |
| CONSTRAINT | **`LHS op RHS` 原形** + **pattern tag** + Dim + 一句中文 | 逐條對照 `AddLHS` / `AddRHS` |
| OBJ | 方向 + 所有項在 LHS | 決定 `CreateMinimize` / `CreateMaximize` |
| 已套用假設 | Phase 1 自行套用的預設清單 | 驗收時分辨哪些是題目、哪些是 AI 補的 |

**出口 gate**：使用者明確確認。`modelConfirmed` 在使用者說話之後才可設為 `true`——這個欄位不是 AI 自評的結果。

**細則**：[`../Ph1_Modeling/model-design-rules.md`](../Ph1_Modeling/model-design-rules.md)、手法庫 [`../Ph1_Modeling/linearization-patterns.md`](../Ph1_Modeling/linearization-patterns.md)、調度 [`../Ph1_Modeling/agent-workflow-prompts.md`](../Ph1_Modeling/agent-workflow-prompts.md)。

## Phase 2 · Coding — 輸入輸出契約

**Input**：`Model/<Project>_Model.md` + `modelConfirmed: true`。開工前先跑進場檢查（[`../Ph2_Coding/optimfoundation-api-guide.md`](../Ph2_Coding/optimfoundation-api-guide.md) §0.0），任一項不過就退回 Phase 1，NEVER 在 code 這層補。

**Output**：`Projects/<Project>/`，八資料夾固定不增減：

```text
Projects/<Project>/
├── <Project>.csproj
├── Program.cs          唯一組裝點：三態 CLI + 材料 → 模型 → 環境
├── Model/              只放 <Project>_Model.md（Phase 1 交付物，本階段唯讀）
├── Set/                Set_*.cs
├── Parameter/          Parameter_*.cs
├── Variable/           Variable[B|X|I]_*.cs
├── Objective/          ObjectiveFunction.cs
├── Constraint/         Constraint_*.cs（一條一檔）
├── Solution/           <Project>Solution.cs
├── Data/               Dataload.cs + *.csv + raw/（選用）
└── status.json
```

**轉譯順序**（依賴決定，不可跳）：Set → Parameter → Dataload + CSV → Variable →（Constraint ∥ Objective）→ Program.cs → Solution。

**出口 gate**：解驗證協定四步全過——① Status 三分診斷 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity。看到 `Optimal` 就宣稱完成不合格。

**細則**：[`../Ph2_Coding/optimfoundation-api-guide.md`](../Ph2_Coding/optimfoundation-api-guide.md)（唯一標準，§9 是 API 簽名權威）、人工驗收 [`../Ph2_Coding/model-to-code-checklist.md`](../Ph2_Coding/model-to-code-checklist.md)、調度 [`../Ph2_Coding/agent-workflow-prompts.md`](../Ph2_Coding/agent-workflow-prompts.md)。

## Phase 3 · Tuning — 輸入輸出契約

**Input**：`solveVerified: true` 的專案 + 使用者描述的症狀。進場時**實跑一次確認**，不能只信 `status.json`。

**凍結範圍**：`Model.md`、`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective` 全部唯讀。本階段只動 `Program.cs` 裡那顆 `CplexConfig productionBaseline`。

**Output**：

- `Program.cs` 的 `productionBaseline`（promotion 後的值 + 上方 provenance 註解）
- 專案根 `TuningHistory.md`（每輪一節，永久 provenance）

`git diff` 只准出現這兩個檔。多出任何 `.csv` / `Constraint_*.cs` / `Model.md` 的改動，就是本階段越界。

**出口 gate**：champion 寫回 baseline → 重新 build → 跑無參數 production → `ValidateRules` 通過。只產出 experiment 報表而 production 仍跑舊 config，**不算完成**。沒有可靠勝者時，「retain + 證據」也是合法交付。

**細則**：[`../Ph3_Tuning/foundation-tuning-rules.md`](../Ph3_Tuning/foundation-tuning-rules.md)、旋鈕全表 [`../Ph3_Tuning/cplex-tuning-strategy.md`](../Ph3_Tuning/cplex-tuning-strategy.md)、調度 [`../Ph3_Tuning/agent-workflow-prompts.md`](../Ph3_Tuning/agent-workflow-prompts.md)。

## 交接物

階段之間只靠檔案交接，不靠對話記憶——換 session、換 agent、context 重置都能從檔案續跑。

### `_wip/<Project>/`（repo 根，三階段共用草稿區）

放在 repo 層而不是專案內，因為專案內受「八資料夾 NEVER 增減」天條管轄，塞 `_wip/` 進去等於自己製造一個驗收 FAIL。

```text
_wip/<Project>/
├── 00-raw.md              題目原文（orchestrator 唯一一次落檔）
├── 1a-normalized.md … 1d-redteam.md    Phase 1 各棒產物
├── manifest.md            Phase 2 轉譯工單（含 Model.md 行號區間）
├── v1-checklist.md / v2-backtranslation.md / v3-solve.md
└── t<N>-triage.md … t<N>-prodverify.md  Phase 3 每輪產物
```

用途是**交接介質**：每個 agent 讀前一棒的檔、寫自己那棒的檔。跨 session resume 時看它有什麼就知道走到哪。內容一律落檔傳路徑，NEVER 貼回主對話。

### `Projects/<Project>/status.json`（機器可讀的階段狀態）

單一檔，各階段**只更新自己負責的欄位**，NEVER 整檔覆寫。

| 欄位 | 型別 | 誰寫 | 誰讀 | 意義 |
| --- | --- | --- | --- | --- |
| `phase` | string | 全部 | 全部 | `modeling` / `coding` / `tuning` |
| `updated` | date | 全部 | 全部 | `YYYY-MM-DD` |
| `modelConfirmed` | bool | P1 | P2 gate | 使用者確認過模型，非 AI 自評 |
| `wipStage` | string | P1 | P1 resume | `1a` / `1b` / `1c` / `1d` |
| `auditPass` | bool | P1 | P1 gate | M5 auditor 全 PASS |
| `redteamHigh` | int | P1 | P1 gate | M6 高嚴重度發現數，須為 0 |
| `manifestUnits` | int | P2 | P2 進度 | 轉譯工單 unit 數 |
| `buildOk` | bool | P2 | P2 resume | `dotnet build` 通過 |
| `v1Pass` | bool | P2 | P2 gate | checklist 稽核無 FAIL |
| `v2Faithful` | bool | P2 | P2 gate | 反向翻譯結論為轉譯忠實 |
| `solveVerified` | bool | P2 | **P3 gate** | 解驗證協定四步全過 |
| `tuningRound` | int | P3 | P3 停損 | 目前輪次 |
| `productionBaseline` | string | P3 | P3 下輪 | 現行 baseline 的 Trial label 或 `initial` |
| `baselineSourceExperiment` | string | P3 | provenance | 來源 experiment 名 |
| `baselineSourceTrial` | string | P3 | provenance | 來源 Trial label |
| `promotionVerified` | bool | P3 | P3 gate | promotion 後 production 驗證通過 |

全新專案的初始值：

```json
{
  "phase": "modeling",
  "modelConfirmed": false,
  "buildOk": false,
  "solveVerified": false,
  "tuningRound": 0,
  "updated": "YYYY-MM-DD"
}
```

其餘欄位在對應階段第一次寫入時才出現，讀取端一律當「不存在 = false / 0」處理。

已停用的 automated 路線用的是另一套 schema（`completed[]` / `current` / `projectType`），**一律作廢**。既有專案的 `status.json` 若還留著這些欄位，讀取端忽略它們並改用上表；NEVER 依它們判斷進度。

## 執行層：單線或 multi-agent

規則相同，只差調度：

| 跑法 | 何時 |
| --- | --- |
| 單線 | 預設。一個 agent 依該 phase 規則從頭做到尾 |
| multi-agent | constraint > 8 條、或 Model.md > 300 行、或使用者要求最高保真度 |

走 multi-agent 就讀該 phase 的 `agent-workflow-prompts.md` 依其拓樸 fan-out，orchestrator 只收工單狀態與 PASS/FAIL。理由是 context 預算：Phase 2 的 API guide 上千行、Model.md 數百行、產出十幾支 `.cs`，單線 agent 會把視窗讀滿，而讀滿之後漏掉的正是它該把關的規則。

## 已知不一致（待修，勿照抄）

盤點時發現下列衝突，一律以本檔與 [`../AGENTS.md`](../AGENTS.md) 為準：

| 位置 | 問題 | 正解 |
| --- | --- | --- |
| [`../../workflows/interactive/README.md`](../../workflows/interactive/README.md) | 寫「六資料夾」 | 八資料夾 |
| [`../../skills/coding/SKILL.md`](../../skills/coding/SKILL.md) Step 3 | 寫 `Set/Dataload.cs` | `Data/Dataload.cs` |
| `../Ph1_Modeling/model-design-rules.md` 與 `../../workflows/interactive/phase-1-model-design.md` | 兩套並存的 Phase 1 規則，內容高度重疊 | 擇一為權威，另一份改為連結 |
| ~~`../../workflows/interactive/README.md`~~ | ~~指向已刪除的 `reference/CPLEX_API_REFERENCE.md`~~ | ✅ **2026-08-08 已修**：全 repo 指路改指 [`../Ph2_Coding/optimfoundation-api-guide.md`](../Ph2_Coding/optimfoundation-api-guide.md) §9，該 ref 檔已刪除 |
| `tutorial(for developer)/` 7 檔 | 仍教已禁用的 `BuildBVs`/`BuildCVs`/`BuildIVs`（含 `BuildCVs<>(lb, ub, …)` 自訂界限） | 一律 `BuildVars<T>(sets...)`；界限寫成獨立 `Constraint_*` |
| `../Ph3_Tuning/cplex-tuning-strategy.md` §1.1 / §1.2 | 同上，變數型別表用 `BuildCVs`/`BuildIVs`/`BuildBVs` | 同上 |
| `../../workflows/interactive/phase-2-coding.md` | 六資料夾；`Set/` 放 Dataload；Objective/Constraint ctor 收 `OptEngine`（與 api-guide §0.2「engine NEVER 進建構子」相反）；CLI 用 `args.Contains("experiment")` 而非三態 `exp` | 全面對齊 api-guide §1.1 / §4.2 / §5.1 |
