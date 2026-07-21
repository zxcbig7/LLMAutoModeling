# SandwichProduction 專案概述

## 問題類型

LP（線性規劃）— 決策變數只有連續型 `VariableX_Sandwich`，無 Binary / Integer 變數。

## 開發兩階段原則

**第一階段：** 數學模型見 `Model/SandwichProduction_Model.md`

**第二階段：** 程式碼純轉譯模型，所有係數透過 `Dataload` 從 `Parameter.QTY` 取得，不得 hardcode。
