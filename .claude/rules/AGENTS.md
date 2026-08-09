# AGENTS.md — AI Modeling 總流程（任何 AI agent 適用）

> 這份檔案是給**任何** AI coding agent 讀的（Claude Code / Cursor / Copilot / Codex / Gemini CLI…）。
> 它自足、只用**相對路徑**、不依賴任何全域設定或機器專屬安裝——`git clone` 到任何機器就完整可跑。
>
> **分層**：本檔管**天條 + 階段契約 + 交接物**（總流程層唯一權威）。各階段怎麼做，在三個 phase 檔裡，一階段一檔，本檔 NEVER 重述它們的內容。
>

## 你的任務

在本 repo 把自然語言最佳化題目，做成可求解的 OptimFoundation CPLEX C# 專案。

**輸入**：一段自然語言的最佳化題目（LP / IP / MILP），可能夾帶表格、原始報表或生成規格。

**輸出**：`Projects/<Project>/` 底下一個可 build、可求解、解已驗證的專案，附一份與 code 逐條對得上的數學模型文件。

**不變式**：模型先鎖定，再機械轉譯，最後才碰效能。每一步都必須能**反向對回上一步**——Model.md 對得回題目原文，`.cs` 對得回 Model.md，tuning 後的解對得回 tuning 前。任何一環對不回去，這條 pipeline 就失去驗證能力，而錯誤不會報錯，它會一路活到「求解成功、答案是錯的」。

## 唯一路線：三階段 phase gate

| 階段 | 唯一規範檔（讀這一份就夠） | 產物 | 出口 gate |
| --- | --- | --- | --- |
| **Phase 1 · Modeling** | [`Ph1_Modeling/model-design-guide.md`](Ph1_Modeling/model-design-guide.md) | `Model/<Project>_Model.md` | 使用者明說「模型確認」/「開始實作」 |
| **Phase 2 · Coding** | [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md) | 八資料夾專案，build 綠、解已驗證 | 解驗證協定四步全過 |
| **Phase 3 · Tuning** | [`Ph3_Tuning/solver-tuning-guide.md`](Ph3_Tuning/solver-tuning-guide.md) | promotion 後的 baseline + `TuningHistory.md` | champion promotion 後 production 重跑通過 |

執行入口是三個 skill：[`modeling`](../skills/modeling/SKILL.md) → [`coding`](../skills/coding/SKILL.md) → [`tuning`](../skills/tuning/SKILL.md)。

**沒有第二條路線，也 NEVER 另建免 gate 的量產路線。** 這條 pipeline 的全部驗證能力來自「每一步都能反向對回上一步」；跳過 gate 等於同時放棄模型確認、解驗證協定、promotion 驗證三道關卡。

## 天條（全流程通用，唯一權威在本檔——其他文件只引用不重複）

### 流程

- NEVER 跳階或併階：MUST 依 Phase 1 → 2 → 3 分階段完成，每階段過 gate 才准進下一階段
- NEVER 為了趕時間把兩個階段併成一次交付。要一次做多題 → 逐題依序走三階段
- NEVER 模型未經**使用者**確認就產任何 `.cs`（`modelConfirmed` 不是 AI 自評的結果）
- **階段只能往前走或整段退回，NEVER 在下游階段補上游的決定**：Phase 2 遇到模型歧義的正解是停下退回，不是選一種解釋繼續；Phase 3 遇到 infeasible 的正解是退回，不是加 penalty 讓它有解

### 數學一致性

- NEVER 移項 / 改號 / 翻轉比較方向 / 合併化簡 —— ALWAYS 左側項 → `AddLHS`、右側項 → `AddRHS`，`>=` → `CreateGreatEqual`、`<=` → `CreateLessEqual`、`=` → `CreateEqual`
- NEVER 四捨五入 / 推算 / 填佔位符——所有數值與題目描述完全一致
- NEVER 在 Constraint / Objective 的**常數位**出現裸數字（含結構常數與迴圈邊界）——一律 `Parameter` 的 `QTY` 經 `Dataload` 取得。字面數字只允許出現在**係數位**（`Σ x` 的 identity）與線性化 pattern 自帶的常數（`= 1` / `− 1` / `= 0`）
- NEVER 用 soft constraint 繞過 infeasible——要不要 soft 是 Model.md 說了算

### 命名

- NEVER 用無意義單一字母符號（`i`、`j`、`k`、`x`、`y`、`z`、`t`）—— ALWAYS 語意名稱
- 類別名直接對應 Model.md 符號：`Set_` / `Parameter_` / `VariableB_`（binary）/ `VariableX_`（continuous）/ `VariableI_`（integer）/ `Constraint_`
- **變數前綴是 load-bearing**（決定型別，非慣例）：generator 依前綴判型產碼，前綴非法直接 compile error `OPTF001`；`BuildVars<T>` 亦由前綴推型
- `Dataload` 欄位名「小寫元件 + PascalCase 語意」：`set_Item` / `parameter_Demand` —— NEVER `ITEM` 全大寫、NEVER `SetA`
- Set 成員字串 PascalCase 單數：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌
- 全專案單一 namespace `<Project>`，block 寫法 `namespace X { }` —— NEVER 子 namespace、NEVER file-scoped
- 每個型別 `sealed` 且有 `<summary>` 標明對應的 Model.md 符號

### 結構

- **八資料夾，NEVER 增減**：`Model/` `Set/` `Parameter/` `Variable/` `Objective/` `Constraint/` `Solution/` `Data/`
- `Model/` **只放** `<Project>_Model.md`——術語表內嵌其中，NEVER 另建 `Glossary.md`、NEVER 放 `.cs`
- 一個型別一個 `.cs`，檔名 = 類別名 —— NEVER 集中檔（`Sets.cs`）
- 模型組裝只在 `Program.cs`（唯一知道 `Dataload` 的地方），平坦三段：材料 → 模型 → 環境 —— NEVER 用 helper / local function 包裝組裝順序
- `OptEngine` 只從 `Build(OptEngine engine)` 進來 —— NEVER 進建構子
- 流程暫存檔一律放 repo 根 `_wip/<Project>/`，NEVER 進專案資料夾

### API 與框架

- NEVER 呼叫 [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md) §9 沒列的 API，也 NEVER 用它標 ❌ 的 API（憑記憶發明 API）
- NEVER 手寫 `: VariableBase` / `: ParameterBase`，NEVER 用 generator 產的位置式 ctor —— 只用裸 `[OptSet]` / `[OptParam]` / `[OptVar]` + 每維一個 `[OptDim<資料型別>("Name")]`（`T` 是 `string` / `DateTime` / `int`…，**NEVER 是 Set 積木**）
- NEVER 改 OptimFoundation 框架本體（唯讀）——擴充在專案端寫 helper

### 路徑與 DLL

- NEVER 用絕對路徑（`C:/Users/...`）—— ALWAYS 相對本 repo；`.claude/` 所在資料夾即專案根
- 一般組件（`OptimFoundation.Core`/`Cplex`、`ILOG.*`、`NLog`）：`<Reference>` + `HintPath` 相對指 repo 根 `dlls/`（`Projects/<X>/` 用 `..\..\dlls\`）
- Source generator 唯一寫法：`<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />` —— NEVER `ProjectReference` 跨 repo、NEVER 絕對路徑、NEVER 指向 bin 輸出
- OptimFoundation rebuild／public API 變更後 MUST 依 `dlls/README.md` 回填 `dlls/` 並手寫更新 `VERSION.txt` provenance —— Why: stale DLL 會遮住 API drift，看似 build 綠實則已編不過
- `Generated/` 僅供檢視：csproj MUST `<Compile Remove="Generated/**/*.cs" />`，否則第二次 build 撞名炸

## Canonical 寫法（不得從相容範例反推）

- 新專案唯一 scaffold 是本 repo 的 [`../../Template/`](../../Template/)；生成到 `Projects/<Project>/` 後使用 repo 根 `dlls/` 的 `<Reference>` / `<Analyzer>`
- **generator 是唯一 paved path；手寫 base class 已廢止，沒有後路。** `Projects/HospitalRostering_Manual` 降為歷史參考，可讀來理解 API 行為，NEVER 當新專案的起手範本
- sibling `OptimFoundation/OptimFoundation/Templates/` 是框架整合與相容性案例，可能用 `ProjectReference` 或歷史寫法，**不是** AI 新建專案的 scaffold
- 規則與範本 code 衝突時，以本檔與三個 phase 檔為準；把 Template 標成待修，NEVER 為迎合落後範本而放寬規則

## 三階段契約總表

| | Phase 1 · Modeling | Phase 2 · Coding | Phase 3 · Tuning |
| --- | --- | --- | --- |
| **Input** | 題目原文（文字或檔案路徑） | 已確認的 `Model/<Project>_Model.md` | 已 `solveVerified` 的專案 + 效能症狀 |
| **本質** | 降維：自然語言 → 數學 | 翻譯：數學 → C#，零詮釋 | 實驗：只動 solver 旋鈕 |
| **Output** | `Model/<Project>_Model.md` | 八資料夾專案，build 綠、解已驗證 | 更新後的 `productionBaseline` + `TuningHistory.md` |
| **啟動條件** | 使用者給題目 | 上一階段 gate 通過 | **使用者主動提出**（NEVER 自己建議） |
| **可寫範圍** | 只有 `Model/*.md` | 整個 `Projects/<Project>/`（`Model/` 除外，唯讀） | 只有 `Program.cs` 的 `productionBaseline` + `TuningHistory.md` |
| **退場** | 術語不明 → 追問 | Model.md 有歧義 → 退回 Phase 1 | 前提破裂 → 退回 Phase 1 / 2 |

### Phase 1 出口契約

Model.md 固定八段順序，**下游逐項在吃，缺一項轉譯就得猜**：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

| 段 | 必填欄 | 下游用途 |
| --- | --- | --- |
| Terminology | Term / 語意 / Role / Unit / Derived? / Raw phrase | 決定類別命名 |
| SET | 名 / 語意 / 成員範例 | 決定建哪幾顆 `Set_*` |
| PARAM | 名 / 語意 / **Dim** / 值 | 決定 `[OptDim]` property |
| VAR | 名 / 語意 / Dim / **型別** / LB / UB | 決定 `VariableB_` / `X_` / `I_` 前綴 |
| CONSTRAINT | **`LHS op RHS` 原形** + **pattern tag** + Dim + 一句中文 | 逐條對照 `AddLHS` / `AddRHS` |
| OBJ | 方向 + 所有項在 LHS | 決定 `CreateMinimize` / `CreateMaximize` |
| 已套用假設 | Phase 1 自行套用的預設清單 | 驗收時分辨哪些是題目、哪些是 AI 補的 |

### Phase 2 出口契約

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

轉譯順序（依賴決定，不可跳）：Set → Parameter → Dataload + CSV → Variable →（Constraint ∥ Objective）→ Program.cs → Solution。

出口 gate = **解驗證協定四步全過**：① Status 三分診斷 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity。看到 `Optimal` 就宣稱完成不合格。

### Phase 3 出口契約

凍結範圍：`Model.md`、`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective` 全部唯讀。本階段只動 `Program.cs` 裡那顆 `CplexConfig productionBaseline`。

`git diff` 只准出現 `Program.cs` 與 `TuningHistory.md`。多出任何其他改動就是越界。

出口 gate = champion 寫回 baseline → 重新 build → 跑無參數 production → `ValidateRules` 通過。只產出 experiment 報表而 production 仍跑舊 config，**不算完成**。沒有可靠勝者時，「retain + 證據」也是合法交付。

## 交接物

階段之間**只靠檔案交接，不靠對話記憶**——換 session、換 agent、context 重置都能從檔案續跑。

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

### `Projects/<Project>/status.json`

單一檔，各階段**只更新自己負責的欄位**，NEVER 整檔覆寫。

| 欄位 | 型別 | 誰寫 | 誰讀 | 意義 |
| --- | --- | --- | --- | --- |
| `phase` | string | 全部 | 全部 | `modeling` / `coding` / `tuning` |
| `updated` | date | 全部 | 全部 | `YYYY-MM-DD` |
| `modelConfirmed` | bool | P1 | P2 gate | 使用者確認過模型，非 AI 自評 |
| `wipStage` | string | P1 | P1 resume | `1a` / `1b` / `1c` / `1d` |
| `auditPass` | bool | P1 | P1 gate | 自驗清單全 PASS |
| `redteamHigh` | int | P1 | P1 gate | 反向紅隊高嚴重度發現數，須為 0 |
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

已刪除的 automated 路線用的是另一套 schema（`completed[]` / `current` / `projectType`），**一律作廢**。既有專案的 `status.json` 若還留著這些欄位，讀取端忽略它們並改用上表。

## 執行層：單線或 multi-agent

規則相同，只差調度：

| 跑法 | 何時 |
| --- | --- |
| 單線 | 預設。一個 agent 依該 phase 規範檔從頭做到尾 |
| multi-agent | constraint > 8 條、或 Model.md > 300 行、或使用者要求最高保真度 |

走 multi-agent 就讀該 phase 規範檔的「multi-agent 執行層」節依其拓樸 fan-out，orchestrator 只收工單狀態與 PASS/FAIL。理由是 context 預算：Phase 2 的 API guide 上千行、Model.md 數百行、產出十幾支 `.cs`，單線 agent 會把視窗讀滿，而讀滿之後漏掉的正是它該把關的規則。

| Phase | multi-agent 節 |
| --- | --- |
| 1 | [`Ph1_Modeling/model-design-guide.md`](Ph1_Modeling/model-design-guide.md) §8（M0–M6） |
| 2 | [`Ph2_Coding/agent-workflow-prompts.md`](Ph2_Coding/agent-workflow-prompts.md)（C0–C9 / V1–V3） |
| 3 | [`Ph3_Tuning/solver-tuning-guide.md`](Ph3_Tuning/solver-tuning-guide.md) §8（T0–T7） |

## repo 內資源

| 資源 | 路徑 | 用途 |
| --- | --- | --- |
| Phase 1 唯一規範 | [`Ph1_Modeling/model-design-guide.md`](Ph1_Modeling/model-design-guide.md) | 四階段降維、Model.md 契約、線性化 pattern、multi-agent |
| Phase 2 唯一標準 | [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md) | 端到端轉譯規範；§9 = 框架簽名 + 黑名單，§8 = Experiment API |
| Phase 2 人工驗收表 | [`Ph2_Coding/model-to-code-checklist.md`](Ph2_Coding/model-to-code-checklist.md) | 交付後逐條核對 |
| Phase 2 multi-agent | [`Ph2_Coding/agent-workflow-prompts.md`](Ph2_Coding/agent-workflow-prompts.md) | C0–C9 / V1–V3 拓樸與派工 prompt |
| Phase 3 唯一規範 | [`Ph3_Tuning/solver-tuning-guide.md`](Ph3_Tuning/solver-tuning-guide.md) | 進場 gate、旋鈕全表、promotion 閉環、multi-agent |
| 專案輸出 | [`../../Projects/`](../../Projects/) | 新專案建這裡：`Projects/<Project>/` |
| DLL 唯一來源 | [`../../dlls/`](../../dlls/) | csproj HintPath 一律指這裡 |
| 專案模板 | [`../../Template/`](../../Template/) | 新專案的資料夾結構與程式範本 |
| 可運作範例 | `Projects/HospitalRostering_Generator`（generator，起手參考）；`Projects/HospitalRostering_Manual`（**已廢止寫法、僅歷史參考**） | 只照抄 generator 版 |

## 已知不一致（待修，勿照抄）

| 位置 | 問題 | 正解 |
| --- | --- | --- |
| `tutorial(for developer)/` 7 檔 | 仍教已禁用的 `BuildBVs` / `BuildCVs` / `BuildIVs`（含 `BuildCVs<>(lb, ub, …)` 自訂界限） | 一律 `BuildVars<T>(sets...)`；界限寫成獨立 `Constraint_*` |
| `Template/`、`Projects/HospitalRostering_Manual` | 早於現行規範，含手寫 base class、舊資料夾結構 | 以本檔與三個 phase 檔為準，範本標成待修 |
