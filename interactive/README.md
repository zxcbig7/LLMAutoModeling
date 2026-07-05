# AI Modeling — 三階段流程總綱（天條層）

> 規範單一來源。任何 AI agent 從 [`../AGENTS.md`](../AGENTS.md) 進來，先讀本檔。
> 本檔是硬規則（天條）；各階段細則在同資料夾的 `phase-*.md`。所有路徑相對本 repo。

## 系統脈絡

MILP / LP / IP 數學模型開發，服務 OptimFoundation CPLEX C# 框架。
開發 = 三階段 phase gate：**Model Design（建模）→ Foundation Coding（轉譯實作）→ Foundation Tuning（調校）**。
先鎖模型、再機械轉譯、需要才調校——模型錯了 code 全部重寫，先鎖模型是最省的路。

## 三階段 Phase Gate（天條）

- MUST 依序走三階段：Modeling → Coding →（使用者提出才做）Tuning
- NEVER 在使用者明確確認數學模型前產生任何 `.cs` 檔 —— ALWAYS 先產 `Projects/<Project>/Model/<Project>_Model.md` 等使用者說「模型確認」/「開始實作」—— Why: 模型錯了 code 全部重寫
- NEVER 對不清楚的術語或題目描述自行猜測 —— ALWAYS 追問後才繼續，確認後補進 `Model/Glossary.md`
- MUST Coding 階段是 Model.md 的**純機械轉譯**，不允許自行詮釋；發現 Model.md 有歧義 → 立即停止回 Model Design

## 命名（天條）

- NEVER 用無意義單一字母符號（`i`、`j`、`x`、`y`、`t`）—— ALWAYS 語意名稱（`GlassType`、`Assign_{Employee,Date}`）
- MUST 程式類別名直接對應 Model.md 符號：`Parameter_` / `VariableB_`（binary）/ `VariableX_`（continuous）/ `VariableI_`（integer）/ `Constraint_` 前綴 + 符號語意核心轉 PascalCase
- Set 成員字串 PascalCase 單數：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌

## 數學一致性（天條）

- NEVER 移項、改號、翻轉比較方向、合併化簡 —— ALWAYS AML 左側項 → `AddLHS(...)`、右側項 → `AddRHS(...)`，`>=` → `CreateGreatEqual`、`<=` → `CreateLessEqual`、`=` → `CreateEqual` —— Why: 轉譯必須逐條對照 Model.md 驗證
- MUST 數值保真：所有數值與題目描述完全一致，NEVER 四捨五入、推算、填佔位符
- ★ 禁止 Hardcode：模型所有數值一律定義在 `Parameter` 類別的 `QTY` 欄位、經 `Dataload` 取得；`Constraint` / `Objective` 內不得出現任何裸數字

## 標準流程（新題目）

1. **Model Design** → [`phase-1-model-design.md`](phase-1-model-design.md)：4 階段降維（去故事化+單位 → 語義判別+Terminology → SET/PARAM/VAR/CONSTRAINT/OBJ 抽取 → 建模自驗）產 `Projects/<Project>/Model/<Project>_Model.md`（LaTeX，每條 constraint 標 pattern tag，手法見 [`linearization-patterns.md`](linearization-patterns.md)）→ 歧義追問 → 等使用者確認
2. **Foundation Coding** → [`phase-2-coding.md`](phase-2-coding.md)：在 [`../Projects/`](../Projects/)`<Project>/` 依五資料夾逐條轉譯 → `dotnet build` → fix loop（≤5）→ `dotnet run` → 解驗證協定（四步）
3. **Foundation Tuning** → [`phase-3-tuning.md`](phase-3-tuning.md)（使用者提出才做）：正確性 gate → 依觸發類型走 solver / IIS / structure 路線

## repo 內權威參考（相對可達，讀來參照不複製）

- API 對照：[`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md)（含 Pool / 取解 / soft constraint / Experiment）
- 資料夾模板：[`../claudemdTemplate/`](../claudemdTemplate/)
- DLL 唯一來源：[`../dlls/`](../dlls/)（csproj HintPath 一律指這裡）
- 可運作範例：`../Projects/HospitalRostering_Generator`（generator + OptModel）、`../Projects/HospitalRostering_Manual`（手寫後路）

> 框架 13 章 API 手冊 `developer-guide.md` 在**同層 sibling 資料夾** `OptimFoundation/`（本 repo 外）；CPLEX_API_REFERENCE.md 為 repo 內主要來源，developer-guide 為進階補充，非硬相依。

## Fatal

- NEVER 模型未確認就產 `.cs`（使用者明說「模型我確認過了直接寫」視同通過 gate）
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER Constraint / Objective 出現裸數字
- NEVER 呼叫不存在的 API（見 phase-2 禁止清單）
- NEVER 改 OptimFoundation 框架本體（唯讀）——要擴充在專案端寫 helper
- NEVER 用絕對路徑
</content>
