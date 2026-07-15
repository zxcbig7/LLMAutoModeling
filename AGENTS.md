# AGENTS.md — AI Modeling 入口（任何 AI agent 適用）

> 這份檔案是給**任何** AI coding agent 讀的（Claude Code / Cursor / Copilot / Codex / Gemini CLI…）。
> 它自足、只用**相對路徑**、不依賴任何全域設定或機器專屬安裝——`git clone` 到任何機器就完整可跑。

## 你的任務

在本 repo 把自然語言最佳化題目，做成可求解的 OptimFoundation CPLEX C# 專案。有兩條路線：

| 路線 | 位置 | 何時用 |
| --- | --- | --- |
| **interactive**（有 gate，預設） | [`interactive/`](interactive/) | 一般開發：三階段 phase gate，模型經使用者確認才寫 code、不亂寫 |
| **automated**（全自動 16-stage） | [`automated/`](automated/) | 大量跑 benchmark / 不需人在迴路：`00_ProblemClassify → 14_FixCode` 一路生到底 |

預設走 **interactive**（下方流程）；要全自動量產才走 automated。

## 怎麼開始（interactive 路線）

1. 讀 [`interactive/README.md`](interactive/README.md) —— 三階段流程總綱 + 硬規則（天條）
2. 依當前 phase 讀對應細則：
   - Phase 1 建模 → [`interactive/phase-1-model-design.md`](interactive/phase-1-model-design.md)（手法庫 [`linearization-patterns.md`](interactive/linearization-patterns.md)）
   - Phase 2 轉譯 → [`interactive/phase-2-coding.md`](interactive/phase-2-coding.md)
   - Phase 3 調校 → [`interactive/phase-3-tuning.md`](interactive/phase-3-tuning.md)
3. API 簽名的權威來源在本 repo：[`CPLEX_API_REFERENCE.md`](CPLEX_API_REFERENCE.md)，NEVER 憑記憶發明 API

## 天條（全流程通用，唯一權威在本檔——其他文件只引用不重複）

- NEVER 模型未經使用者確認就產任何 `.cs`（interactive 路線）
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER 在 Constraint / Objective 出現裸數字（一律 Parameter 的 QTY）
- NEVER 呼叫 `CPLEX_API_REFERENCE.md` 沒列的 API（憑記憶發明 API）
- NEVER 改 OptimFoundation 框架本體（唯讀）——擴充在專案端寫 helper
- NEVER 用絕對路徑（`C:/Users/...`）—— ALWAYS 相對本 repo；本檔所在資料夾即專案根

### DLL 引用規則（消費端 csproj）

- 一般組件（`OptimFoundation.Core`/`Cplex`、`ILOG.*`、`NLog`）：`<Reference>` + `HintPath` 相對指 repo 根 `dlls/`（`Projects/<X>/` 用 `..\..\dlls\`、`Template_CPLEX/` 用 `..\dlls\`）
- Source generator：唯一寫法 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />`（依巢狀深度調 `..`）—— NEVER `ProjectReference` 跨 repo、NEVER 絕對路徑、NEVER 指向 bin 輸出
- OptimFoundation rebuild／public API 變更後 MUST 跑 `scripts/setup-dlls.ps1 -Build` 回填 `dlls/`（含 `VERSION.txt` provenance）—— Why: stale DLL 會遮住 API drift，看似 build 綠實則已編不過
- `Generated/` 僅供檢視：csproj MUST `<Compile Remove="Generated/**/*.cs" />`，否則第二次 build 撞名炸

## repo 內資源（都在本 repo，相對可達）

| 資源 | 路徑 | 用途 |
| --- | --- | --- |
| API 對照 | [`CPLEX_API_REFERENCE.md`](CPLEX_API_REFERENCE.md) | Pool / 取解 / soft constraint / Experiment API |
| 專案輸出 | [`Projects/`](Projects/) | 新專案建這裡：`Projects/<Project>/` |
| DLL 唯一來源 | [`dlls/`](dlls/) | csproj HintPath 一律指這裡 |
| 資料夾模板 | [`claudemdTemplate/`](claudemdTemplate/) | 各資料夾結構模板 |
| 可運作範例 | `Projects/HospitalRostering_Generator`（generator）、`Projects/HospitalRostering_Manual`（手寫） | 起手參考 |
</content>
