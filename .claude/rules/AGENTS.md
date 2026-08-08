# AGENTS.md — AI Modeling 入口（任何 AI agent 適用）

> 這份檔案是給**任何** AI coding agent 讀的（Claude Code / Cursor / Copilot / Codex / Gemini CLI…）。
> 它自足、只用**相對路徑**、不依賴任何全域設定或機器專屬安裝——`git clone` 到任何機器就完整可跑。

## 你的任務

在本 repo 把自然語言最佳化題目，做成可求解的 OptimFoundation CPLEX C# 專案。

**唯一路線是三階段 phase gate**：Phase 1 Modeling → Phase 2 Coding → Phase 3 Tuning，依序推進、每階段之間有 gate，流程總綱見 [`../workflows/interactive/README.md`](../workflows/interactive/README.md)。

整體任務、三階段 I/O 契約、交接物與 `status.json` schema 在 [`MILP DevPipeline/README.md`](MILP%20DevPipeline/README.md)（pipeline 層唯一權威）；本檔只管天條。

## Canonical 寫法（不得從相容範例反推）

- 新專案唯一 scaffold 是本 repo 的 [`../../Template/`](../../Template/)；生成到 `Projects/<Project>/` 後使用 repo 根 `dlls/` 的 `<Reference>` / `<Analyzer>`。
- canonical 結構是八資料夾 `Model/Set/Parameter/Variable/Objective/Constraint/Solution/Data`（NEVER 增減），入口是 top-level `Program.cs`，class 宣告走 `[OptVar]` / `[OptParam]` + `[OptDim<TSet>]`。
- **generator 是唯一 paved path；手寫 `: VariableBase` / `: ParameterBase` 已廢止，沒有後路。** `Projects/HospitalRostering_Manual` 降為歷史參考，可讀來理解 API 行為，NEVER 當新專案的起手範本。
- sibling `OptimFoundation/OptimFoundation/Templates/` 是框架整合與相容性案例，可能用 `ProjectReference` 或歷史寫法，**不是** AI 新建專案的 scaffold。
- 規則與範本 code 衝突時，以本檔與 [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md) 為準；把 Template 標成待修，NEVER 為迎合落後範本而放寬規則。

## 怎麼開始

1. 讀 [`../workflows/interactive/README.md`](../workflows/interactive/README.md) —— 三階段流程總綱 + 硬規則（天條）
2. 依當前 phase 讀對應細則：
   - Phase 1 建模 → [`../workflows/interactive/phase-1-model-design.md`](../workflows/interactive/phase-1-model-design.md)（手法庫 [`linearization-patterns.md`](../workflows/interactive/linearization-patterns.md)）
   - Phase 2 轉譯 → [`../workflows/interactive/phase-2-coding.md`](../workflows/interactive/phase-2-coding.md)
   - Phase 3 調校 → [`../workflows/interactive/phase-3-tuning.md`](../workflows/interactive/phase-3-tuning.md)
3. Phase 2 的唯一標準（含 API 簽名權威）是 [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md)，簽名表在其 §9，NEVER 憑記憶發明 API

## 天條（全流程通用，唯一權威在本檔——其他文件只引用不重複）

- NEVER 跳階或併階：MUST 依 Phase 1 → 2 → 3 分階段完成，每階段過 gate 才准進下一階段 —— NEVER 走全自動 / 免 gate 的量產路線（`workflows/automated/` 已停用，僅供歷史查閱）
- NEVER 模型未經使用者確認就產任何 `.cs`
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER 在 Constraint / Objective 出現裸數字（一律 Parameter 的 QTY）
- NEVER 呼叫 `Ph2_Coding/optimfoundation-api-guide.md` §9 沒列的 API，也 NEVER 用它標 ❌ 的 API（憑記憶發明 API）
- NEVER 改 OptimFoundation 框架本體（唯讀）——擴充在專案端寫 helper
- NEVER 用絕對路徑（`C:/Users/...`）—— ALWAYS 相對本 repo；本檔所在資料夾即專案根

### DLL 引用規則（消費端 csproj）

- 一般組件（`OptimFoundation.Core`/`Cplex`、`ILOG.*`、`NLog`）：`<Reference>` + `HintPath` 相對指 repo 根 `dlls/`（`Projects/<X>/` 用 `..\..\dlls\`、`Template_CPLEX/` 用 `..\dlls\`）
- Source generator：唯一寫法 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />`（依巢狀深度調 `..`）—— NEVER `ProjectReference` 跨 repo、NEVER 絕對路徑、NEVER 指向 bin 輸出
- OptimFoundation rebuild／public API 變更後 MUST 依 `dlls/README.md`「佈置步驟」第 2、3 步回填 `dlls/` 並手寫更新 `VERSION.txt` provenance —— Why: stale DLL 會遮住 API drift，看似 build 綠實則已編不過
- `Generated/` 僅供檢視：csproj MUST `<Compile Remove="Generated/**/*.cs" />`，否則第二次 build 撞名炸

## repo 內資源（都在本 repo，相對可達）

| 資源 | 路徑 | 用途 |
| --- | --- | --- |
| Phase 2 唯一標準 | [`Ph2_Coding/optimfoundation-api-guide.md`](Ph2_Coding/optimfoundation-api-guide.md) | 端到端轉譯規範；§9 = 框架簽名 + 黑名單，§8 = Experiment API |
| 專案輸出 | [`Projects/`](../../Projects/) | 新專案建這裡：`Projects/<Project>/` |
| DLL 唯一來源 | [`dlls/`](../../dlls/) | csproj HintPath 一律指這裡 |
| 專案模板 | [`../../Template/`](../../Template/) | 新專案的資料夾結構與程式範本 |
| 可運作範例 | `Projects/HospitalRostering_Generator`（generator，起手參考）；`Projects/HospitalRostering_Manual`（手寫 base，**已廢止寫法、僅歷史參考**） | 只照抄 generator 版 |
</content>
