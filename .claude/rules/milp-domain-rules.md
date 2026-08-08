# MILP Model — 數學模型開發規範（domain root）

<system_context>
MILP / LP / IP 數學模型開發 domain，服務 OptimizationFramework 專案（OptimFoundation CPLEX C# 框架）。
開發 = 三階段 phase gate：**Model Design（建模）→ Foundation Coding（轉譯實作）→ Foundation Tuning（調校）**。
本檔是天條層（硬規則）；各 phase 細節在子資料夾 CLAUDE.md。
外部專案根：`C:/Users/zxcbi/Desktop/Projects/OptimizationFramework`（下稱 `$OPT`）。
</system_context>

<critical_notes>

## 三階段 Phase Gate（天條）

- MUST 依序走三階段：Modeling → Coding →（使用者提出才做）Tuning
- NEVER 在使用者明確確認數學模型前產生任何 `.cs` 檔 —— ALWAYS 先產 `Model/<Project>_Model.md` 等「模型確認」或「開始實作」—— Why: 模型錯了 code 全部重寫，先鎖模型再轉譯是最省的路
- NEVER 對不清楚的術語或題目描述自行猜測 —— ALWAYS 追問後才繼續，確認後補進 `Model/<Project>_Model.md` 的 Terminology Mapping Table；NEVER 另建 `Glossary.md`
- MUST Coding 階段是 Model.md 的**純機械轉譯**，不允許任何自行詮釋；發現 Model.md 有歧義 → 立即停止，回 Model Design 補模型

## 命名（天條）

- NEVER 在 Model.md 用無意義單一字母符號（`i`、`j`、`k`、`x`、`y`、`z`、`t`）—— ALWAYS 用語意名稱（`GlassType`、`Assign_{Employee,Date}`）
- MUST 程式類別名直接對應 Model.md 符號：`Parameter_` / `VariableB_`（binary）/ `VariableX_`（continuous）/ `VariableI_`（integer）/ `Constraint_` 前綴 + 符號語意核心轉 PascalCase
- ★ 變數前綴是 load-bearing（決定型別，非僅慣例）：`VariableB_`→Binary / `VariableX_`→Continuous / `VariableI_`→Integer。generator 依前綴判型並產碼，前綴非法直接 compile error（`OPTF001`，訊息教正確取名）；`BuildVars<T>` 亦由前綴推型 —— NEVER 用 `[OptVar]` 參數指定型別（該參數已移除，attribute 只帶 sets）—— Why: 型別過去在 attribute / 前綴 / Build call 三處各表述一次可互相矛盾，收斂成前綴單一真相後才驗得了
- Set 成員字串 PascalCase 單數：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌
- MUST `Dataload` 欄位名用「小寫元件 + PascalCase 語意」：`set_Item` / `set_Date` / `parameter_Demand` —— NEVER `ITEM`、`ROW` 全大寫，NEVER `SetA` PascalCase —— Why: `data.set_Item` 一眼看得出是哪種積木，且與類別名 `Set_Item` 區分得開
- MUST 全專案單一 namespace `<Project>`，block 寫法 `namespace X { }` —— NEVER 子 namespace（`MyProject.Set`）、NEVER file-scoped `namespace X;`
- MUST 每個型別 `sealed` 且有 `<summary>` 標明對應的 Model.md 符號

## 結構（天條）

- MUST 八個資料夾，NEVER 增減：`Model/`（**只放** `<Project>_Model.md`，術語表內嵌其中，不放 `Glossary.md` 或 `.cs`）、`Set/`、`Parameter/`、`Variable/`、`Objective/`、`Constraint/`、`Solution/`、`Data/`（`Dataload.cs` + input CSV + 選用的 `raw/`）
- MUST 一個型別一個 `.cs` 檔，檔名 = 類別名 —— NEVER 集中檔（`Sets.cs`）
- MUST 模型組裝只在 `Program.cs`（唯一知道 `Dataload` 的地方），平坦分三段：材料 → 模型 → 環境；三態 CLI 為 `import` / `exp` / 預設求解 —— NEVER 用轉呼叫 helper 或 local function 包裝組裝順序
- MUST `OptEngine` 只從 `Build(OptEngine engine)` 進來 —— NEVER 進建構子 —— Why: 建構子簽名要純粹是資料依賴清單
- MUST 只用 generator（`[OptSet<T>]` / `[OptParam]` / `[OptVar]` + `[OptDim]`）—— NEVER 手寫 `: VariableBase` / `: ParameterBase`，也 NEVER 用 generator 產的位置式 ctor（改 `[OptDim]` 順序會靜默接錯）
- MUST `Dataload` 以「input CSV 已就位」為前提（就位 = 執行檔目錄下的 `Data/`），`Dataload(IDataSource)` 只允許 `Load(source, name)` 與 `LoadParam<T>(name)` 兩種句子；CSV 還不存在時（攤平不規則來源、或依規格生成 instance）用選用的 `import` 模式先產 CSV，import ctor MUST 與它同在 `Data/Dataload.cs`，且 import 與求解分兩次命令
- Objective / Constraint 建構子 NEVER 收整包 `Dataload`；`Solution/<Project>Solution.cs` 的解讀層不在此限（`ValidateRules` 本來就要對照全部原始資料）

## 數學一致性（天條）

- NEVER 移項、改號、翻轉比較方向、合併化簡 —— ALWAYS Model 左側項 → `AddLHS(...)`、右側項 → `AddRHS(...)`，`>=` → `CreateGreatEqual`、`<=` → `CreateLessEqual`、`=` → `CreateEqual` —— Why: 轉譯必須可逐條對照 Model.md 驗證，動過手腳就驗不了
- MUST 數值保真：所有數值與題目描述完全一致，NEVER 四捨五入、推算、填佔位符
- ★ 禁止 Hardcode：字面數值只能出現在 `AddLHS` / `AddRHS` 的**係數位**（第一參數且第二參數是變數，即 `Σ x` 的 identity 係數）—— NEVER 出現在**常數位**（單參數形式）—— 模型所有數值（係數、容量、比例、RHS）一律定義在 `Parameter` 的 `QTY` 欄位經 `Dataload` 取得
- ★ 結構常數也是資料：維度／結構常數（3×3 宮的 3、時間窗長度 7）MUST 做成 `Set_*` 或 `Parameter_*` —— NEVER 寫成迴圈邊界的字面數字（`for (i = 0; i < 3; i++)`）—— ALWAYS `foreach (var x in set)` 或 `set.Count` —— Why: 寫死 3 的模型換成 4×4 就得改 code，違反「換資料不改 code」
- ★ 只允許 `engine.BuildVars<T>(sets...)`：型別由前綴單一決定 —— NEVER `BuildBVs` / `BuildCVs` / `BuildIVs`。`BuildVars` 沒有 bounds overload（`VariableB_` 為 `[0,1]`、`VariableX_` / `VariableI_` 為 `[0, 1E100]`），**變數界限一律寫成一條 constraint** —— Why: 界限本來就是 Model.md 的一條式子，藏進 `BuildCVs(0, 100, ...)` 參數就無法逐條對回去驗證
- ★ 只允許單參數 `CreateEqual(name)` / `CreateLessEqual(name)` / `CreateGreatEqual(name)` —— NEVER 用 `CreateEqual(rhs, name)` 那組 overload —— ALWAYS 用 `AddRHS(常數)` 表達右側 —— Why: 那組 overload 是**覆蓋** `_rhsConst` 不是累加，混用會把先前 `AddRHS` 的值靜默丟掉，模型變成另一題
- MUST 目標式是 Model.md `OBJ` 段的逐項轉譯；Model.md 沒有 OBJ 段 → 停止 Phase 2 退回 Model Design —— NEVER 自創零係數目標式或省略 `.AddObjective(...)`

## Tuning Gate（天條）

- MUST 先過正確性 gate 才做效能 tuning：`dotnet build` → `Solve()` → `Status == Optimal` → 解 / 目標值對照題目描述驗證
- Tuning 只動 `CplexConfig`：進場時模型與資料已凍結、已 feasible，`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 全程唯讀。要換資料 → 換 CSV 重跑，不走本 phase；要改結構 / infeasible → 退回 Model Design。NEVER 用 soft constraint 繞過 infeasible
- Tuning MUST 閉環到 production：`Program.cs` 只保留一顆 `productionBaseline`；AI 用 `OptExperiment` 產生證據、讀 Trial 選 champion、通過 promotion gate 後寫回該 baseline，再跑 production 驗證。只產 experiment 報表但 prod 仍用舊 config，不算完成。
- Baseline provenance MUST 永久可追溯：每次 promotion 同步更新 `Program.cs` 註解與專案根 `TuningHistory.md`，記錄來源 experiment / Trial、config diff、證據與 production 驗證；NEVER 只依賴可被清除的 `bin/Experiments`。

</critical_notes>

<file_map>
MILP Model/CLAUDE.md - 本檔（天條）
Ph2_Coding/optimfoundation-api-guide.md - ★ Phase 2 的**唯一標準**：端到端 API 開發規範（建專案 → 資料/變數/模型層 → Program.cs 組裝 → 寫實驗 → 驗收 → API 速查/黑名單）。同層已無第二份 Phase 2 規範，衝突時一律以本檔為準
Model Design/CLAUDE.md - Phase 1：4 階段建模法、五段 Model.md（SET/PARAM/VAR/CONSTRAINT/OBJ）、程式轉譯 metadata、建模自驗 gate
Model Design/linearization-patterns.md - constraint 8 類 canonical form + 非線性→線性 recipe（abs/max/min/fixed-charge/either-or）+ Big-M 鐵律
Model Design/agent-workflow-prompts.md - Phase 1 multi-agent 執行層：M1–M6 拓樸（normalizer / classifier / structurer / constraint-writer fan-out / auditor / 反向紅隊）+ 可複製派工 prompt + context 預算鐵則
Ph2_Coding/model-to-code-checklist.md - Phase 2 交付後人工核對用 CHK LIST（Model.md ↔ Code 逐條比對，guide 規則轉可勾選項）
Ph2_Coding/agent-workflow-prompts.md - Phase 2 multi-agent 執行層：轉譯工單 manifest + C1–C9 / V1–V3 拓樸（constraint 逐條 fan-out、反向翻譯紅隊）+ API guide 分片讀取表
Foundation Tuning/CLAUDE.md - Phase 3：正確性 gate、三方向 tuning、Experiment API、stop conditions
Foundation Tuning/agent-workflow-prompts.md - Phase 3 multi-agent 執行層：T1–T7 拓樸（triage / variant 設計與 champion 判定分離 / second-opinion 裁決 promotion / production 驗證）
Foundation Tuning/solver-tuning-research.md - Solver tuning 文獻地圖（reference，非規範）：Algorithm Configuration 四條研究線、configurator 工具、實驗方法論鐵則（performance variability / over-tuning）
evals/ - behavioral eval cases（改本 domain 規範後跑 `/harness-eval milp-model`）
../workflow skill/SKILL.md - milp-dev orchestrator skill（部署後觸發「幫我建模」等）

外部權威參考（$OPT，讀來參照不複製）：
$OPT/OptimFoundation/OptimFoundation/specs/developer-guide.md - 框架 13 章 API 手冊
（消費端 API 對照已併入 Ph2_Coding/optimfoundation-api-guide.md §9，原 CPLEX_API_REFERENCE.md 已刪除）
$OPT/AI-Modeling/claudemdTemplate/ - 專案內各資料夾 CLAUDE.md 模板（單一來源）
$OPT/AI-Modeling/dlls/ - DLL 唯一來源（HintPath 一律 `..\..\dlls\`）
$OPT/AI-Modeling/Template/、Projects/*、$OPT/OptimFoundation/OptimFoundation/Templates/* - 既有內容可能早於現行規範，含已禁止寫法（手寫 VariableBase、Sets.cs 集中檔、轉呼叫 local function、Dataload 內生成資料）；可讀來理解 API 行為，NEVER 複製其舊結構。框架 repo 內 `Templates/*` 的 framework `ProjectReference` + solver `$(CplexDir)` HintPath 是限定例外，AI-Modeling 專案仍一律使用 `dlls/`
</file_map>

<paved_path>
## 標準流程（新題目）

1. **Model Design**：讀 `Model Design/CLAUDE.md` → 4 階段降維（去故事化+單位正規化 → 語義判別+Terminology 表 → SET/PARAM/VAR/CONSTRAINT/OBJ 抽取 → 建模自驗）產 `Model/<Project>_Model.md`（LaTeX，每條 constraint 標 pattern tag，手法見 `Model Design/linearization-patterns.md`）→ 歧義追問 → 等使用者確認
2. **Foundation Coding**：讀 `Ph2_Coding/optimfoundation-api-guide.md`（唯一標準）→ 在 repo 根 `Projects/<Project>/` 依八資料夾結構逐條轉譯 → 備妥 `Data/*.csv`（不規則來源先 `dotnet run -- import raw/<file>`）→ `dotnet build` → fix loop（≤5 次）→ `dotnet run` → 解驗證協定（四步）
3. **Foundation Tuning**（使用者提出才做）：讀 `Foundation Tuning/CLAUDE.md` → 正確性 gate（build 綠 + 已 feasible）→ 模型與資料凍結，只掃 `CplexConfig` 旋鈕 → champion promotion 回 production baseline

整段流程由 `milp-dev` skill orchestrate（含 resume）；手動走也依同順序。

## 執行層：multi-agent 或單線

上述每個 phase 有兩種跑法，規則相同、調度不同：

- **單線**（預設，題目小）：一個 agent 依該 phase CLAUDE.md 從頭做到尾
- **multi-agent**（題目大、constraint 多、或要最高保真度）：讀該 phase 的 `agent-workflow-prompts.md`，依其拓樸 fan-out 派工，orchestrator 只收工單狀態與 PASS/FAIL —— Why: Phase 2 的 API guide 上千行、Model.md 數百行、產出十幾支 `.cs`，單線 agent 會把 context 讀滿，讀滿之後漏掉的正是它該把關的規則

判準：constraint 超過 8 條、或 Model.md 超過 300 行、或使用者要求最高保真度 → 走 multi-agent。
</paved_path>

<patterns>
## 命名對應（Model.md 符號 → 程式類別）

| Model.md 符號 | 程式類別 | `Dataload` 欄位 |
| --- | --- | --- |
| `GLASSTYPE` set | `Set_GlassType` | `set_GlassType` |
| `DemandQty` parameter | `Parameter_Demand`（值放 `QTY` 欄位） | `parameter_Demand` |
| `Produce_{GlassType}`（連續量） | `VariableX_Production` | — |
| `Assign_{Employee,Date}`（0/1） | `VariableB_Assign` | — |
| `Capacity_{GlassType}` constraint | `Constraint_Capacity` | — |

## Pool API 最小速查（詳見 Foundation Coding）

限制式類別的 `Build(OptEngine engine)` 內；`glassTypes` / `usageRates` / `machineCapacity` 由建構子逐項注入：

```csharp
// Model: sum_g Produce_g * UsageRate_g <= MachineCapacity
foreach (var glassType in glassTypes)
{
    var rate = usageRates.FirstOrDefault(p => p.GlassType == glassType)?.QTY ?? 0.0;
    engine.AddLHS(rate, new VariableX_Production { GlassType = glassType });
}
engine.AddRHS(machineCapacity);
engine.CreateLessEqual(ConstraintName);
```

✅ Good：係數先 LINQ 存局部變數再傳入；scalar 在 `Program.cs` 材料段用 `.Single().QTY` 取出後注入。
❌ Bad：LINQ 直接內嵌 `AddLHS(...)`、`engine.AddRHS(40.0)` 裸數字、建構子收整包 `Dataload` 或 `OptEngine`。
</patterns>

<common_tasks>
- 新題目 / 「幫我建模」 → milp-dev skill（或手動走 paved_path）
- 使用者要改約束 / 嫌太慢 / infeasible → 先讀 `Foundation Tuning/CLAUDE.md` 再動手
- API 簽名不確定 → 查 `Ph2_Coding/optimfoundation-api-guide.md` §9，NEVER 憑記憶寫
- 改本 domain 規範 → 跑 `/harness-eval milp-model` 回歸
</common_tasks>

<hatch>
- 純數學討論 / 教學（不落地成專案）→ 不必 phase gate，但命名與 LaTeX 慣例照用
- 使用者一句話明說「模型我確認過了，直接寫 code」→ 視同通過 gate，直接進 Coding
</hatch>

<fatal_implications>
- NEVER 跳階或併階——MUST 分階段走 Phase 1 → 2 → 3，NEVER 走全自動免 gate 的量產路線（`.claude/workflows/automated/` 已停用）
- NEVER 模型未確認就產 `.cs`
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER Constraint / Objective 的常數位出現裸數字（含結構常數與迴圈邊界）
- NEVER 手寫 `: VariableBase` / `: ParameterBase`，NEVER 自創資料夾，NEVER 用 helper／local function 包裝 `Program.cs` 的組裝順序
- NEVER 呼叫不存在或已禁用的 API（`GetVarSol`、`GetSetVarSol`、`CsvCtrl.SaveToCSV` 不存在；`BuildBVs`/`BuildCVs`/`BuildIVs`、`CreateXxx(rhs, name)`、`LoadCsv`、`LoadInline` 已禁用，見 guide §9 黑名單）
- NEVER 改 OptimFoundation 框架本體（唯讀）—— 要擴充在專案端寫 helper；真要改框架 → Core + Cplex DLL 一起 rebuild 並更新 `$OPT/AI-Modeling/dlls/`
</fatal_implications>
