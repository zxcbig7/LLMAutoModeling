# HospitalRostering_Generator 專案概述

## 問題類型

MIP（混合整數規劃）— 醫院護理人員月排班，加權懲罰最小化。

## 架構：Generator + 注入 Action（框架預設，AI 首選）

- 變數 / 參數：`[OptVar]` / `[OptParam]` 由 `AutoSetsGenerator` 編譯期生成（class 只留宣告殼）
- composition root：Fluent `OptModel`，建構步驟以 `Action<OptEngine>` 注入（`Program.cs`）
- 與 `Projects/HospitalRostering_Manual`（手寫後路架構）用**同一份數學模型**、跑**同一組 tuning**，專供兩架構對照（見 `tutorial/` §5.8）

## 開發兩階段原則

**第一階段：** 數學模型見 `Model/HospitalRostering_Model.md`（與 Manual 版字字相同）。

**第二階段：** 程式碼純轉譯模型，所有係數透過 `Dataload` 從 `Parameter.QTY` / `Penalty_*` 取得，不得 hardcode。

## 執行

- `dotnet run` — 求解
- `dotnet run -- experiment` — 參數掃描（`ExperimentRunner`，與 solve 共用 `VariableCreate`/`BuildModel`）
