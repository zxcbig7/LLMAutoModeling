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

| 階段                   | 唯一規範檔（讀這一份就夠）                                                   | 產物                                         | 出口 gate                                 |
| ---------------------- | ---------------------------------------------------------------------------- | -------------------------------------------- | ----------------------------------------- |
| **Phase 1 · Modeling** | [`modeling/model-design-guide.md`](modeling/model-design-guide.md)           | `Model/<Project>_Model.md`                   | 使用者明說「模型確認」/「開始實作」       |
| **Phase 2 · Coding**   | [`coding/optimfoundation-api-guide.md`](coding/optimfoundation-api-guide.md) | 八資料夾專案，build 綠、解已驗證             | 解驗證協定四步全過                        |
| **Phase 3 · Tuning**   | [`tuning/solver-tuning-guide.md`](tuning/solver-tuning-guide.md)             | promotion 後的 baseline + `TuningHistory.md` | champion promotion 後 production 重跑通過 |

執行入口是三個 skill：[`modeling`](modeling/SKILL.md) → [`coding`](coding/SKILL.md) → [`tuning`](tuning/SKILL.md)。

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
- 類別名直接對應 Model.md 符號：`Set_` / `Parameter_` / `VariableB_`（binary）/ `VariableC_`（continuous）/ `VariableI_`（integer）/ `Constraint_`
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
- NEVER 建立流程暫存、草稿、交接或工作文件；可稽核內容直接寫入該 phase 的正式交付物。

### API 與框架

- NEVER 呼叫 [`coding/optimfoundation-api-guide.md`](coding/optimfoundation-api-guide.md) §9 沒列的 API，也 NEVER 用它標 ❌ 的 API（憑記憶發明 API）
- NEVER 手寫 `: VariableBase` / `: ParameterBase`，NEVER 用 generator 產的位置式 ctor —— 只用裸 `[OptSet]` / `[OptParam]` / `[OptVar]` + 每維一個 `[OptDim<資料型別>("Name")]`（`T` 是 `string` / `DateTime` / `int`…）
- NEVER 改 OptimFoundation 框架本體（唯讀）——擴充在專案端寫 helper

### 路徑與 DLL

- NEVER 用絕對路徑（`C:/Users/...`）—— ALWAYS 相對本 repo；`.claude/` 所在資料夾即專案根
- 一般組件（`OptimFoundation.Core`/`Cplex`、`ILOG.*`、`NLog`）：`<Reference>` + `HintPath` 相對指 repo 根 `dlls/`（`Projects/<X>/` 用 `..\..\dlls\`）
- Source generator 唯一寫法：`<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />` —— NEVER `ProjectReference` 跨 repo、NEVER 絕對路徑、NEVER 指向 bin 輸出
- OptimFoundation rebuild／public API 變更後 MUST 依 `dlls/README.md` 回填 `dlls/` 並手寫更新 `VERSION.txt` provenance —— Why: stale DLL 會遮住 API drift，看似 build 綠實則已編不過
- `Generated/` 僅供檢視：csproj MUST `<Compile Remove="Generated/**/*.cs" />`，否則第二次 build 撞名炸

## Canonical 寫法（不得從相容範例反推）

- 新專案唯一 scaffold 是本 repo 的 [`../../Template/`](../../Template/)；生成到 `Projects/<Project>/` 後使用 repo 根 `dlls/` 的 `<Reference>` / `<Analyzer>`
- **generator 是唯一 paved path；手寫 base class 已廢止，沒有後路。**
- **可照抄結構的既有專案只有 [`../../Projects/CandyBlending/`](../../Projects/CandyBlending/)**（八資料夾、四段 `Program.cs`、`Data/Dataload.cs`、`Solution/` 的兩個驗證入口都符合現行規範，build 綠且解已驗證）。
- `Projects/HospitalRostering_Generator` 是**早於本版規範**的可運作範例：子 namespace、建構子收整包 `Dataload` 與 `OptEngine`、`Build()` 無參數、`FirstOrDefault`、`try/catch` 吞例外，且有 `Constraint/BuildModel.cs`、`Variable/VariableCreate.cs` 兩個八資料夾外的組裝檔。**可讀來理解 API 行為，NEVER 照抄結構。**
- sibling `OptimFoundation/OptimFoundation/Templates/` 是框架整合與相容性案例，可能用 `ProjectReference` 或歷史寫法，**不是** AI 新建專案的 scaffold
- 規則與範本 code 衝突時，以本檔與三個 phase 檔為準；把 Template 標成待修，NEVER 為迎合落後範本而放寬規則

## 三階段契約總表

|              | Phase 1 · Modeling         | Phase 2 · Coding                                        | Phase 3 · Tuning                                                                                                                      |
| ------------ | -------------------------- | ------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| **Input**    | 題目原文（文字或檔案路徑） | 已確認的 `Model/<Project>_Model.md`                     | 已 `solveVerified`、**exp 分支 R0-ready** 的專案 + 效能症狀                                                                           |
| **本質**     | 降維：自然語言 → 數學      | 翻譯：數學 → C#，零詮釋                                 | 實驗：只動 solver 旋鈕                                                                                                                |
| **Output**   | `Model/<Project>_Model.md` | 八資料夾專案，build 綠、解已驗證、**exp 分支 R0-ready** | 更新後的 `productionBaseline` + `TuningHistory.md`                                                                                    |
| **啟動條件** | 使用者給題目               | 上一階段 gate 通過                                      | **使用者主動提出**（NEVER 自己建議）                                                                                                  |
| **可寫範圍** | 只有 `Model/*.md`          | 整個 `Projects/<Project>/`（`Model/` 除外，唯讀）       | 白名單五處：`Program.cs` 的 `productionBaseline`、`Program.cs` exp 分支、`TuningHistory.md`、`Experiments/`、`status.json` 的 P3 欄位 |
| **退場**     | 術語不明 → 追問            | Model.md 有歧義 → 退回 Phase 1                          | 前提破裂 → 退回 Phase 1 / 2                                                                                                           |

### Phase 1 出口契約

Model.md 固定八段順序，**下游逐項在吃，缺一項轉譯就得猜**：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

| 段          | 必填欄                                                   | 下游用途                                             |
| ----------- | -------------------------------------------------------- | ---------------------------------------------------- |
| Terminology | Term / 語意 / Role / Unit / Derived? / Raw phrase        | 決定類別命名                                         |
| SET         | 名 / 語意 / 成員範例                                     | 決定建哪幾顆 `Set_*`                                 |
| PARAM       | 名 / 語意 / **Dim** / 值                                 | 決定 `[OptDim]` property                             |
| VAR         | 名 / 語意 / Dim / **型別** / LB / UB                     | 決定 `VariableB_` / `VariableC_` / `VariableI_` 前綴 |
| CONSTRAINT  | **`LHS op RHS` 原形** + **pattern tag** + Dim + 一句中文 | 逐條對照 `AddLHS` / `AddRHS`                         |
| OBJ         | 方向 + 所有項在 LHS                                      | 決定 `CreateMinimize` / `CreateMaximize`             |
| 已套用假設  | Phase 1 自行套用的預設清單                               | 驗收時分辨哪些是題目、哪些是 AI 補的                 |

### Phase 2 出口契約

```text
Projects/<Project>/
├── <Project>.csproj
├── Program.cs          唯一組裝點：三態 CLI + 材料 → 模型 → 環境
├── Model/              只放 <Project>_Model.md（Phase 1 交付物，本階段唯讀）
├── Set/                Set_*.cs
├── Parameter/          Parameter_*.cs
├── Variable/           Variable[B|C|I]_*.cs
├── Objective/          ObjectiveFunction.cs
├── Constraint/         Constraint_*.cs（一條一檔）
├── Solution/           <Project>Solution.cs
├── Data/               Dataload.cs + *.csv + raw/（選用）
└── status.json
```

轉譯順序（依賴決定，不可跳）：Set → Parameter → Dataload + CSV → Variable →（Objective ∥ Constraint，寫檔先後不拘）→ Program.cs → Solution。

出口 gate = **解驗證協定四步全過**：① `SolveStatus` 七態分流 ② 解代回每一條 constraint ③ 單位與量級對得上題目 ④ LP bound sanity。看到 `Optimal` 就宣稱完成不合格。

**① 依框架 `SolveStatus` 的七個列舉值判定，NEVER 只認 `Optimal`**（與 [`coding/optimfoundation-api-guide.md`](coding/optimfoundation-api-guide.md) §7 同一張表）：

| `SolveStatus` | 語意                                                                                  | Phase 2 gate                                | 下一步                                              |
| ------------- | ------------------------------------------------------------------------------------- | ------------------------------------------- | --------------------------------------------------- |
| `Optimal`     | 證明最佳                                                                              | ②③④ 照跑                                    | 過 gate                                             |
| `Feasible`    | **有 incumbent、未證明最佳**（撞 `TimeLimit` / `NodeLimit` / `IntegerSolutionLimit`） | ②③④ 對 incumbent 照跑（④ 改比 `BestBound`） | **過 gate**，記錄 `MipGap`；效能不足是 Phase 3 的事 |
| `TimeLimit`   | **中止且無任何可用解**（名字誤導，非時間專屬）                                        | 無解可驗，②③ 做不了                         | 換小 instance 求到 `Optimal` 完成 ②③④ 才過 gate     |
| `Infeasible`  | 無可行解                                                                              | ✗                                           | 讀 IIS 取證，退回 Phase 1 / 2                       |
| `Unbounded`   | 目標式無界                                                                            | ✗                                           | 補漏掉的界限 constraint，退回 Phase 2               |
| `Error`       | 求解過程出錯                                                                          | ✗                                           | 執行面問題，先修到跑得起來再談驗證                  |
| `NotSolved`   | 沒跑到求解                                                                            | ✗                                           | lifecycle 沒走完，先修                              |

★ 純 LP（模型無整數變數）沒有 MIP bound，`BestBound` / `MipGap` 會是 `-1E+75` / `1E+75` 佔位值。那不是異常，是「這題沒有 gap 可言」；NEVER 把它當品質指標寫進交付報告（api-guide §7）。

Why 把 `Feasible` 放進 gate：撞時限但有 incumbent **正是 Phase 3 最典型的進場情境**。要求 `Optimal` 才准出 Phase 2 會讓這類專案卡死在 Phase 2——而 Phase 2 手上沒有任何合法工具能修「太慢」，那顆旋鈕在 Phase 3。轉譯忠實與否用 incumbent 就驗得出來，跟有沒有證明最佳無關。

**出口 gate 之外另有一條交棒契約：exp 分支 MUST 已是 R0-ready 形狀**（`coding/optimfoundation-api-guide.md` §8.4）——experiment 名 `<Project>-tuning-r0`、config label 帶 `r0-` 前綴、`// R0 —` marker 就位、內容是 baseline × 5 seeds、`productionBaseline` 已明設 `ParallelMode = 1` / `Seed` / 實測定版的 `Threads`。Phase 3 進場時**一行 code 都不用改**就跑得出 R0。

Why: exp 分支雖在 Phase 3 的白名單內，但「每次接棒都先重寫一次」等於把 Phase 2 沒做完的事推給下游，重寫期間的 build 失敗還會污染調校紀錄。Phase 2 只交形狀、不跑 R0——sizing、θ、瓶頸剖面都是**解讀**，屬 Phase 3。

### Phase 3 出口契約

凍結範圍：`Model.md`、`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective` 全部唯讀。本階段只動 `Program.cs` 裡那顆 `CplexConfig productionBaseline`。

`git diff` 只准出現下列五類（權威定義在 `tuning/solver-tuning-guide.md` §0.1.2，本處與其一致）：

```text
Projects/<Project>/Program.cs
Projects/<Project>/TuningHistory.md
Projects/<Project>/Experiments/<Project>-tuning-r<N>.csv
Projects/<Project>/Experiments/<Project>-tuning-r<N>-meta.csv
Projects/<Project>/Experiments/<Project>-tuning-r<N>.json
Projects/<Project>/Experiments/<Project>-tuning-r<N>-trajectory.csv   ← 有收集到軌跡才會有
```

（`status.json` 若已納管則可額外出現。）多出任何其他改動就是越界。`Experiments/` 是本階段唯一允許的專案結構擴充；每輪 `.csv` / `-meta.csv` / `.json` **三者缺一不可**，`-trajectory.csv` 則**只有在真的收集到軌跡時才會產生**（純 LP 或求解太快就沒有，看主表 `TrajectoryPoints` 是不是 0）。MUST 通過 [`tuning/checklist.md`](tuning/checklist.md) 的「每輪 archive 逐項驗收」A–E。

出口 gate = champion 寫回 baseline → 重新 build → 跑無參數 production → `ValidateRules` 通過。只產出 experiment 報表而 production 仍跑舊 config，**不算完成**。沒有可靠勝者時，「retain + 證據」也是合法交付。

## 正式交付物

階段之間只以正式交付物續跑，不建立草稿區或中間交接文件。

### 不建立中間工作文件

Phase 1 直接完成 `Model/<Project>_Model.md`；Phase 2 只交付完整專案與 `status.json`；Phase 3 直接在 `TuningHistory.md` 追加計畫、事實、分析與裁決，並在 `Experiments/` 保存原始證據。

```text
Projects/<Project>/
├── Model/<Project>_Model.md             Phase 1 唯一正式模型
├── status.json                           phase gate 狀態
├── TuningHistory.md                      Phase 3 計畫、分析與裁決
└── Experiments/<Project>-tuning-r<N>.*   Phase 3 原始證據
```

跨 session resume 時只讀上述正式檔案；短暫的 agent 工單、稽核結果與分析草稿不落檔。

### `Projects/<Project>/status.json`

單一檔，各階段**只更新自己負責的欄位**，NEVER 整檔覆寫。

| 欄位                       | 型別   | 誰寫 | 誰讀            | 意義                                                                |
| -------------------------- | ------ | ---- | --------------- | ------------------------------------------------------------------- |
| `phase`                    | string | 全部 | 全部            | `modeling` / `coding` / `tuning`                                    |
| `updated`                  | date   | 全部 | 全部            | `YYYY-MM-DD`                                                        |
| `modelConfirmed`           | bool   | P1   | P2 gate         | 使用者確認過模型，非 AI 自評                                        |
| `auditPass`                | bool   | P1   | P1 gate         | 自驗清單全 PASS                                                     |
| `redteamHigh`              | int    | P1   | P1 gate         | 反向紅隊高嚴重度發現數，須為 0                                      |
| `manifestUnits`            | int    | P2   | P2 進度         | 轉譯工單 unit 數                                                    |
| `buildOk`                  | bool   | P2   | P2 resume       | `dotnet build` 通過                                                 |
| `v1Pass`                   | bool   | P2   | P2 gate         | checklist 稽核無 FAIL                                               |
| `v2Faithful`               | bool   | P2   | P2 gate         | 反向翻譯結論為轉譯忠實                                              |
| `solveVerified`            | bool   | P2   | **P3 gate**     | 解驗證協定四步全過                                                  |
| `solveStatus`              | string | P2   | **P3 進場分流** | 框架 `SolveStatus` 值：`Optimal` / `Feasible` / `TimeLimit`         |
| `verifiedOn`               | string | P2   | **P3 進場分流** | 解驗證是在哪份資料上完成的：`production` 或 `small-instance:<說明>` |
| `tuningRound`              | int    | P3   | P3 停損         | 目前輪次                                                            |
| `productionBaseline`       | string | P3   | P3 下輪         | 現行 baseline 的 Trial label 或 `initial`                           |
| `baselineSourceExperiment` | string | P3   | provenance      | 來源 experiment 名                                                  |
| `baselineSourceTrial`      | string | P3   | provenance      | 來源 Trial label                                                    |
| `promotionVerified`        | bool   | P3   | P3 gate         | promotion 後 production 驗證通過                                    |

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

| 跑法        | 何時                                                            |
| ----------- | --------------------------------------------------------------- |
| 單線        | 預設。一個 agent 依該 phase 規範檔從頭做到尾                    |
| multi-agent | constraint > 8 條、或 Model.md > 300 行、或使用者要求最高保真度 |

走 multi-agent 就讀該 phase 規範檔的「multi-agent 執行層」節依其拓樸 fan-out，orchestrator 只收工單狀態與 PASS/FAIL。理由是 context 預算：Phase 2 的 API guide 上千行、Model.md 數百行、產出十幾支 `.cs`，單線 agent 會把視窗讀滿，而讀滿之後漏掉的正是它該把關的規則。

| Phase | multi-agent 節                                                                          |
| ----- | --------------------------------------------------------------------------------------- |
| 1     | [`modeling/model-design-guide.md`](modeling/model-design-guide.md) §8（M0–M6）          |
| 2     | [`coding/agent-workflow-prompts.md`](coding/agent-workflow-prompts.md)（C0–C9 / V1–V3） |
| 3     | [`tuning/solver-tuning-guide.md`](tuning/solver-tuning-guide.md) §8（T0–T7）            |

## repo 內資源

| 資源                   | 路徑                                                                                         | 用途                                                                                                   |
| ---------------------- | -------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------ |
| Phase 1 唯一規範       | [`modeling/model-design-guide.md`](modeling/model-design-guide.md)                           | 四階段降維、Model.md 契約、線性化 pattern、multi-agent                                                 |
| Phase 2 唯一標準       | [`coding/optimfoundation-api-guide.md`](coding/optimfoundation-api-guide.md)                 | 端到端轉譯規範；§9 = 框架簽名 + 黑名單，§8 = Experiment API                                            |
| Phase 2 產出一致性契約 | [`coding/checklist.md`](coding/checklist.md)                                                 | AI 交付前機械驗收，逐條核對                                                                            |
| Phase 2 multi-agent    | [`coding/agent-workflow-prompts.md`](coding/agent-workflow-prompts.md)                       | C0–C9 / V1–V3 拓樸與派工 prompt                                                                        |
| Phase 3 唯一規範       | [`tuning/solver-tuning-guide.md`](tuning/solver-tuning-guide.md)                             | 進場 gate、旋鈕全表、promotion 閉環、multi-agent                                                       |
| 專案輸出               | [`../../Projects/`](../../Projects/)                                                         | 新專案建這裡：`Projects/<Project>/`                                                                    |
| DLL 唯一來源           | [`../../dlls/`](../../dlls/)                                                                 | csproj HintPath 一律指這裡                                                                             |
| 專案模板               | [`../../Template/`](../../Template/)                                                         | 新專案的資料夾結構與程式範本                                                                           |
| **結構參考專案**       | [`../../Projects/CandyBlending/`](../../Projects/CandyBlending/)                             | 符合現行規範的最小完整專案：八資料夾、四段 `Program.cs`、`ValidateData` + `ValidateRules` 兩個驗證入口 |
| 舊版可運作範例         | [`../../Projects/HospitalRostering_Generator/`](../../Projects/HospitalRostering_Generator/) | 早於本版規範，只讀 API 行為，NEVER 照抄結構                                                            |

## 文件同步原則

`tutorial(for developer)/`、[`../../Template/`](../../Template/) 與 [`../../Projects/CandyBlending/`](../../Projects/CandyBlending/) 必須與 Phase 2 guide 使用同一套 row-based Set/Parameter、`Load<T>`、B/C/I 前綴與 owner-based constraint naming。歷史設計文件不得作為產碼依據。

`Projects/HospitalRostering_Generator` **不在同步範圍內**：它早於本版規範，保留為可運作的 API 行為參考。要拿它當結構依據前，先改成符合本檔與 Phase 2 guide 的形狀。
