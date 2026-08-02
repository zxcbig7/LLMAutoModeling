# HospitalRostering_Generator 專案概述

## 問題類型

MIP（混合整數規劃）— 醫院護理人員月排班，加權懲罰最小化。

## 架構：Generator + 注入 Action（框架預設，AI 首選）

- 變數 / 參數：`[OptVar]` / `[OptParam]` 由 `AutoSetsGenerator` 編譯期生成（class 只留宣告殼）
- ⚠ 本專案 Variable 層沿用**舊字串式** `[OptVar("Date:DateTime", "Employee", "Group")]`（建於 2026-07-15 定版之前）。框架仍支援、不需遷移，但**新題目 NEVER 照抄**——一律改用光桿 `[OptVar]` + `[OptDim<Set_X>("Name")]`
- composition root：`OptModel` 定義 variables / objective / constraints 三階段；`OptProject` 與 `OptExperiment` 共用同一份模型定義（`Program.cs`）
- 與 `Projects/HospitalRostering_Manual`（手寫 composition root 的後路）用**同一份數學模型**、跑**同一組 tuning**，專供兩架構對照（見 `tutorial/` §5.8）
- **2026-07-19**：`Dataload` 已改為 `partial class ... : DataContext`，建構走 `OptData.Load(() => new Dataload())`，載入後框架自動驗參照完整性 / index key 唯一性 / 數值 sanity。註：Manual 版的 Parameter 層在此波後與本專案結構收斂（`OPTF006` 要求所有 Parameter 走 attribute），兩者差異僅剩 composition root 與 Constraint 組法

## 開發兩階段原則

**第一階段：** 數學模型見 `Model/HospitalRostering_Model.md`（與 Manual 版字字相同）。

**第二階段：** 程式碼純轉譯模型，所有係數透過 `Dataload` 從 `Parameter.QTY` / `Penalty_*` 取得，不得 hardcode。

## 執行

- `dotnet run` — 求解
- `dotnet run -- experiment` — 參數掃描（`OptExperiment`，與 solve 共用已載入的 data 與同一個 `OptModel`）
