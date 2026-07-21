# HospitalRostering_Manual 專案概述

## 問題類型

MIP（混合整數規劃）— 醫院護理人員月排班，加權懲罰最小化。

## 架構：手寫 composition root（後路）

- composition root：手寫 `HospitalRosteringProblem : IDisposable` 的 `Execute()`，自行 new/Build/Solve/Dispose `OptEngine` —— **這才是本專案「後路」定位的實質內容**
- Parameter：**已改為 attribute 驅動**（`[OptParam]` + `[OptDim<Set_X>]`），與 Generator 版結構相同（`OPTF006` 強制，見下方定位變更）
- Variable：**仍維持手寫繼承 `VariableBase`**（框架認可的後路寫法），未使用 attribute —— 這是本專案與 Generator 版最主要的結構差異
- `Dataload` 為 `partial class ... : DataContext`，建構走 `OptData.Load(() => new Dataload())`，載入後框架自動驗資料
- 與 `Projects/HospitalRostering_Generator`（預設架構）用**同一份數學模型**、跑**同一組 tuning**，專供兩架構對照（見 `tutorial/` §5.8）
- ⚠ 這是**示範用的後路**；新題目請優先用 Generator 版的寫法

> **2026-07-19 定位變更**：框架資料防護上線後，`OPTF006` 要求所有 `Parameter_*` 走 attribute 路徑（漏掛即 compile error），且 generator 會無條件重 emit 該型別的屬性——手寫屬性與 generator 產出並存會 `CS0102` 重複定義。因此本專案的 Parameter 層**已無法維持純手寫**，與 Generator 版結構收斂。
> 兩者現存差異僅剩 composition root 與 Constraint 組法。**若對照價值已不足，可考慮合併為單一範例。**

## 開發兩階段原則

**第一階段：** 數學模型見 `Model/HospitalRostering_Model.md`（與 Generator 版字字相同）。

**第二階段：** 程式碼純轉譯模型，所有係數透過 `Dataload` 從 `Parameter.QTY` / `Penalty_*` 取得，不得 hardcode。

## 執行

- `dotnet run` — 求解（`HospitalRosteringProblem.Execute()`）
- `dotnet run -- experiment` — 參數掃描（`ExperimentRunner`，與 solve 共用 `VariableCreate`/`BuildModel`）
