# Projects 資料夾

此資料夾包含所有 OptimFoundation CPLEX 題目專案。**interactive 與 automated 兩條路線統一產出下列扁平結構**（不再有 `stages/` + `csharp/` 兩層中繼）。

## 每個專案的標準架構（統一扁平，D3）

```text
ProjectName/
├── CLAUDE.md ← 專案概述、問題類型
├── ProjectName.csproj ← 複製 Template_CPLEX；DLL/Analyzer 相對 repo 根 dlls/
├── Program.cs ← 唯一進入點：材料、三階段模型定義、solve / experiment runner
├── status.json ← 進度追蹤（automated 斷點續跑用）
├── Model/ ← 數學模型文件（Markdown，Model/<Name>_Model.md）
├── Set/ ← Set 積木（[OptSet<T>]）+ Dataload 組合器
├── Parameter/ ← Parameter 積木（[OptParam] + [OptDim<TSet>]，含 QTY）
├── Variable/ ← Variable 積木（[OptVar] + [OptDim<TSet>]，前綴 B/X/I）
├── Objective/ ← ObjectiveFunction
└── Constraint/ ← ConstraintBase 子類別
```

> paved path 是光桿 `[OptParam]`/`[OptVar]` + 逐維具名宣告 `[OptDim<TSet>("Name")]`（見 `CPLEX_API_REFERENCE.md` §7.5 / `claudemdTemplate/`）。上圖標示的兩種泛型式，以及字串式 `[OptVar("Date:DateTime", "Employee")]`（僅 `HospitalRostering_Generator` 沿用），皆為仍受支援的逃生口，非首選——既有 code 不需遷移，新 code NEVER 照抄。

## resume（automated 斷點續跑，D6）

`status.json`：`{ "completed": ["00","01",...], "current": "05", "projectType": "MILP" }`
每個 stage 產物直接寫入上面的最終扁平資料夾（直寫終點）；續跑時讀 `status.json` + 已完成檔當 context，從 `current` 續跑不重跑。

## 通用規則（適用所有專案）

- **天條唯一權威在 [`../AGENTS.md`](../AGENTS.md)**（數值保真、API 白名單、框架唯讀、相對路徑、DLL 引用）
- **預設 composition root**：`Program.cs` 是唯一組裝點，依「材料 → 模型 → 環境」排列。`OptModel` 以 `.AddVariables(...)` / `.AddObjective(...)` / `.AddConstraints(...)` 定義模型；簡單組裝偏好直接逐項註冊，有意義的群組可使用 `Program.cs` local helper 或 block lambda。`OptProject` 執行一次正式求解，`OptExperiment` 執行 model × solver config。手寫 `XxxProblem : IDisposable` 的 `Execute()` 是框架認可的後路，僅 `HospitalRostering_Manual` 保留作對照範例，新專案一律走 paved path
- **設定分層**：`ProjectConfig` 管專案名、保留期、solver log 與 LP/MPS/Sol 輸出；`CplexConfig` 只放 solver 旋鈕。`ProjectConfig.EnableSolverLog` 預設為 `true`
- **資料層（硬性）**：`Dataload` MUST 是 `public partial class Dataload : DataContext`，建構走 `OptData.Load(() => new Dataload())`，NEVER 裸 `new Dataload()`——後者跳過框架的參照完整性 / 重複 key / 數值 sanity 驗證。載入後視為唯讀；`Freeze()` 只攔截框架受控 mutation API，直接改 public field / mutable `List` 不保證立即攔截，專案 code 仍一律不得修改
- **顯式依賴**：Objective / Constraint 建構子只收實際使用的 engine、Set、Parameter、scalar 與界限值，NEVER 接整包 `Dataload`
- **三階段**：框架固定依 variables → objective → constraints 執行。local helper 或 block lambda 可整理同階段的有意義群組，但不得混合階段或新增只負責轉呼叫的 facade class
- **雙模式**：`dotnet run` 由 `OptProject` 求解；`dotnet run -- experiment` 由 `OptExperiment` 掃描。兩者共用同一個 `OptModel` 與同一份已載入資料
- **實驗預設**：solver log OFF、LP/MPS/Sol 輸出 OFF、housekeeping OFF；variant 由 `CplexConfig.Clone()` 產生具體物件
- **禁止 Hardcode**：所有數值透過 `Parameter.QTY` 取得
- **兩階段開發**：先完成數學模型（`Model/`），再實作程式碼
- **資料夾規則以 `claudemdTemplate/` 為單一來源**：各子資料夾規則見 `claudemdTemplate/{Set,Variable,Parameter,Objective,Constraint,Experiment,Root,Model}/CLAUDE.md`
- canonical code 範本：`Template_CPLEX/`；tuning 策略與旋鈕對照：`../tuning/CLAUDE.md`；端到端流程：`../tutorial/`
- 建模基本物件（Set 積木 / SetBase / 三檔位讀取）→ OptimFoundation `specs/2026-07-13-optset-basic-objects.md`

## 未完成專案

- `WoodworkingShop/`：只有 `Model/WoodworkingShop_Model.md`（Phase 1 數學模型已完成），**尚無 `CLAUDE.md`、無任何 `.cs`**——Phase 2 程式實作未開始。稽核時不算遺漏，接手時從 stub 開始建。
