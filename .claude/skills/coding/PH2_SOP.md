# Phase 2 Coding SOP

> API 與程式寫法的唯一權威是同層的 [`optimfoundation-api-guide.md`](optimfoundation-api-guide.md)。本檔只描述執行順序，不複製 API 簽名，避免兩份規格漂移。

## 1. 進場

確認 `Model/<Project>_Model.md` 已由使用者核准，且 SET、PARAM、VAR、CONSTRAINT、OBJ 都有明確的名稱、維度、型別與數學式。缺資料形狀或限制式方向時，回到 Phase 1 補規格。

## 2. 建立資料層

1. 每個 Set 建一個 `[OptSet]` partial class，至少一個使用 framework 支援基礎型別的 `OptDim`。
2. 每個 Parameter 建一個 `[OptParam]` partial class，可為零維，值欄固定為生成的 `QTY`。
3. 資料來源由專案決定：使用 `CsvDataSource` 時，每個 Set/Parameter 建一份帶表頭 CSV；使用 `DbDataSource` 時，以完整 SQL（必要時用 `AS`）提供相同欄位契約，不建立假的 CSV。
4. `Dataload(IDataSource source)` 對 Set 與 Parameter 一律呼叫 `source.Load<T>(sourceName)`；CSV 的 `sourceName` 是 `AppDomain.BaseDirectory/Data/` 下的檔名，不必等於 row class 名稱；DB 的 `sourceName` 是完整 SQL。
5. 經 `OptData.Load(() => new Dataload())` 建立資料環境。

## 3. 建立變數

1. 依 Model.md 選 `VariableB_`、`VariableC_` 或 `VariableI_`。
2. 維度使用 framework 支援的基礎型別 `OptDim`，順序與模型 index 一致。
3. 一般情況用 `engine.BuildVars<T>(sets...)`。
4. 傳入多維 Set list 時，該 row 的各欄會展開成多個變數 key token。

## 4. 建立目標與限制式

1. 先建立 Objective，再建立 Constraint。
2. Model.md 左右側原樣對應 `AddLHS` / `AddRHS`，不自行移項。
3. 新限制式用 `CreateEqual(this, dims...)`、`CreateLessEqual(this, dims...)` 或 `CreateGreatEqual(this, dims...)`。
4. owner 與原始維度值交給框架組名；不要手拼日期或 `@` key。

## 5. 組裝與輸出

1. 在 `Program.cs` 用 `OptModel` 串接 Variables、Objective、Constraints。
2. 用 `OptProject` 掛載 project/solver config 與 `OnSolved`。
3. 解答透過 `ISolutionSink` 或 `GetSolution` 讀取。
4. import 產生的 Set/Parameter 資料以 `CsvCtrl.WriteRows` 寫回 Template CSV。

## 6. 驗收

依序執行：

1. Model.md ↔ class ↔ 資料來源 ↔ Dataload 數量、schema 與名稱核對。
2. Variable 維度順序與 `BuildVars` 引數核對。
3. 每條 Constraint 反向翻譯回數學式核對。
4. 靜態搜尋錯誤 API；使用 CSV 時，確認每份輸入都有表頭。
5. `dotnet build`、unit tests、小型求解與輸出檢查。

詳細驗收項目見 [`model-to-code-checklist.md`](model-to-code-checklist.md)。
