# coding · 交付前自檢

> 規則本體在 `.claude/rules/Ph2_Coding/optimfoundation-api-guide.md`（API 在 §9）；交付後的人工驗收用 `.claude/rules/Ph2_Coding/model-to-code-checklist.md`。
> 本檔只是交付前的勾選面。**逐條對照回 Model.md**，不是掃一眼 code 就打勾。

## 入口 gate

- [ ] `Model/<Project>_Model.md` 存在且使用者已確認
- [ ] 轉譯過程中沒有「自己決定」的模型解釋（有歧義應該已回 `modeling`）

## 結構

- [ ] 專案在 `Projects/<Project>/`，從 `Template/` 長出來
- [ ] 八資料夾：`Model/` `Set/` `Parameter/` `Variable/` `Objective/` `Constraint/` `Solution/` `Data/`，沒有增減
- [ ] `Model/` 只放 `<Project>_Model.md`；`Dataload.cs` 在 `Data/`，與 CSV 同層
- [ ] 一個型別一個 `.cs`，檔名 = 類別名（沒有 `Sets.cs` 這種集中檔）
- [ ] DLL 走 `<Reference>` + HintPath `..\..\dlls\`；generator 走 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />`
- [ ] csproj 有 `<Compile Remove="Generated/**/*.cs" />`
- [ ] 沒有任何絕對路徑

## 逐條對照（每條 constraint 都做一次）

- [ ] Model.md 的每條 `[Cn]` 都找得到對應的 `Constraint_*.cs`，且 `///` 註記寫了條號
- [ ] 左式的項全在 `AddLHS`、右式的項全在 `AddRHS`——**沒有任何移項**
- [ ] 比較符號對得上：`>=` → `CreateGreatEqual`、`<=` → `CreateLessEqual`、`=` → `CreateEqual`
- [ ] 係數與 Model.md 完全一致（沒有四捨五入、沒有合併化簡）
- [ ] 變數前綴對得上 Model.md 標的型別（`VariableB_` 二元 / `VariableX_` 連續 / `VariableI_` 整數）
- [ ] 變數的 LB / UB 是**一條 constraint**，不是藏在 build 參數裡
- [ ] Objective 是 OBJ 段的逐項轉譯，方向正確

## Hardcode 稽查

- [ ] Constraint / Objective 的**常數位**沒有裸數字（單參數的 `AddRHS(40)` 這種）
- [ ] 結構常數（宮邊長、時間窗長度）做成 `Set_*` 或 `Parameter_*`，沒有寫成迴圈邊界字面數字
- [ ] 迴圈一律 `foreach (var x in set)` 或 `set.Count`
- [ ] 所有數值都經 `Parameter` 的 `QTY` 由 `Dataload` 取得

## 組裝與依賴

- [ ] `Program.cs` 是唯一組裝點，平坦三段：材料 → `OptModel` → runner
- [ ] 每個 variable family / Objective / 每條 Constraint 各占一個 fluent call
- [ ] 沒有純轉呼叫的 helper class 或 local function
- [ ] Objective / Constraint 建構子只收實際用到的 Set / Parameter / scalar（**沒有整包 `Dataload`**）
- [ ] `OptEngine` 從 `Build(engine)` 或 canonical 第一參數進來，不從別處偷渡
- [ ] scalar 用 `.Single().QTY` 取出成區域變數再傳入（不是 `First()` / `FirstOrDefault()?.QTY ?? 0`）
- [ ] 參數查詢先存局部變數，`AddLHS` / `AddRHS` 內沒有內嵌 LINQ
- [ ] Objective 註冊在所有 Constraint 之前

## API

- [ ] 用到的每個 API 都在 `.claude/rules/Ph2_Coding/optimfoundation-api-guide.md` §9 查得到，且沒有被標 ❌
- [ ] 沒有用 `GetVarSol` / `GetSetVarSol` / `CsvCtrl.SaveToCSV`（不存在）
- [ ] 沒有用 `BuildBVs` / `BuildCVs` / `BuildIVs`、`CreateXxx(rhs, name)` overload

## Build 與解驗證

- [ ] `dotnet build` 通過，fix loop 沒超過 5 次
- [ ] `Status` 已三分診斷（Optimal 才往下；Infeasible → IIS；Unbounded → 查漏界）
- [ ] 解已代回**每條** constraint，LHS op RHS 成立
- [ ] 目標值與關鍵變數的單位、量級對得上題目
- [ ] LP bound sanity 檢查過（max：整數解 ≤ LP bound）
- [ ] 與 Model.md 的小例 / 已知解對照過

## 交付

- [ ] 回報含 build 結果、目標值、解摘要、輸出檔位置
- [ ] `status.json` 已更新（`buildOk` / `solveVerified`）
