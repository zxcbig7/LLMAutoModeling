# HospitalRostering_Manual 專案概述

## 問題類型

MIP（混合整數規劃）— 醫院護理人員月排班，加權懲罰最小化。

## 架構：手寫 class + Problem.Execute（後路）

- 變數 / 參數：全手寫 `: VariableBase` / `: ParameterBase`（不掛 generator）
- composition root：手寫 `HospitalRosteringProblem : IDisposable` 的 `Execute()`，自行 new/Build/Solve/Dispose `OptEngine`
- 與 `Projects/HospitalRostering_Generator`（預設架構）用**同一份數學模型**、跑**同一組 tuning**，專供兩架構對照（見 `tutorial/` §5.8）
- ⚠ 這是**示範用的後路**；新題目請優先用 generator 版的寫法

## 開發兩階段原則

**第一階段：** 數學模型見 `Model/HospitalRostering_Model.md`（與 Generator 版字字相同）。

**第二階段：** 程式碼純轉譯模型，所有係數透過 `Dataload` 從 `Parameter.QTY` / `Penalty_*` 取得，不得 hardcode。

## 執行

- `dotnet run` — 求解（`HospitalRosteringProblem.Execute()`）
- `dotnet run -- experiment` — 參數掃描（`ExperimentRunner`，與 solve 共用 `VariableCreate`/`BuildModel`）
