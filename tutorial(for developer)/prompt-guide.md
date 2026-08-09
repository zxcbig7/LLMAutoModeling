# 如何對 AI 下開發 Prompt

一個 prompt 只處理一個 phase。先給目標、輸入檔路徑、完成條件與不可跨越的範圍；不要把建模、實作與調校混在同一個要求裡。

## 1. 新題目的建模 Prompt

在尚未確認數學模型時使用。把題目原文或檔案路徑放在最後一行。

```text
請為 <Project> 執行 Phase 1 Modeling。

先讀 .claude/rules/AGENTS.md 與
.claude/rules/Ph1_Modeling/model-design-guide.md。

只建立或更新 Projects/<Project>/Model/<Project>_Model.md 與 repo 根
_wip/<Project>/ 的建模交接檔。不要建立任何 C# 檔案。

請依規範完成問題描述、Terminology Mapping Table、SET、PARAM、VAR、
CONSTRAINT、OBJ、已套用假設。遇到會改變模型的歧義時，列為問題並停止，
不要自行假設。完成後請等待我的模型確認。

題目：<貼上題目或提供檔案路徑>
```

確認 Model.md 無誤後，明確回覆「模型確認，開始實作」，再進入下一個 prompt。

## 2. 將已確認模型轉成程式的 Prompt

```text
模型已確認，請為 <Project> 執行 Phase 2 Coding。

先讀 .claude/rules/AGENTS.md、
.claude/rules/Ph2_Coding/optimfoundation-api-guide.md，並以
Projects/<Project>/Model/<Project>_Model.md 為唯一模型輸入。

先建立 _wip/<Project>/manifest.md，再依相依順序完成專案。資料層採 row-data：
Set 與 Parameter 都以 primitive OptDim 宣告；Set CSV 為 Dim 欄，Parameter
CSV 為 Dim 欄加 QTY；Dataload 只用 source.Load<T>() 讀成 List<T>；輸出使用
CsvCtrl.WriteRows(rows)。變數使用 BuildVars<T>。

完成條件：dotnet build 成功，輸入資料可載入；另外驗證 Set duplicate、參照與模型要求的完整性，
並完成每條限制式的解驗證、單位與量級檢查、LP bound sanity。若 Model.md 有歧義，停止並指出行號與問題，
不要自行補模型假設。
```

若只要補資料層，可以把範圍收斂成：

```text
請只處理 <Project> 的 Phase 2 資料層：Set/、Parameter/、Data/Dataload.cs 與
Data/*.csv。維持既有 Model.md、Variable/、Objective/、Constraint/ 不變。
完成後檢查 CSV 表頭、型別轉換、Set/Parameter 重複 key、Set-driven lookup 的缺值處理與 scalar Parameter 的單筆 QTY；Parameter 查找使用 `FindParameterOrLog`。
```

## 3. 實作驗收 Prompt

當程式已完成、但你需要獨立檢查時使用：

```text
請只驗收 <Project> 的 Phase 2 實作，不修改模型語意。

檢查 Model.md 到 C# 的對應、Set/Parameter 的 Dim 與 CSV 欄名、Dataload 的
Load<T>() 型別、變數展開順序、每條限制式的 LHS/RHS 與比較方向，以及求解後的
四步解驗證。將發現寫入 _wip/<Project>/，每項附檔案路徑與行號；不要自行改寫
有歧義的 Model.md。
```

## 4. Tuning Prompt

只在解已驗證且使用者確實要改善效能時使用。

```text
<Project> 已通過資料驗證與解驗證，請執行 Phase 3 Tuning。

先讀 .claude/rules/AGENTS.md 與
.claude/rules/Ph3_Tuning/solver-tuning-guide.md。

模型、CSV、Dataload、Variable、Objective、Constraint 均保持不動。只在
Program.cs 的具名 CplexConfig baseline 建立可比較的實驗，記錄每個 trial，選出
champion 後寫回 baseline，並重新驗證 production 執行。結果寫入 TuningHistory.md。
```

## 5. Prompt 的必要資訊

| 必填內容 | 範例 |
| --- | --- |
| 專案名稱與路徑 | `Projects/RoutePlanning/` |
| 目前 phase | `Phase 2 Coding` |
| 唯一輸入 | `Model/RoutePlanning_Model.md`、資料檔路徑 |
| 範圍 | 「只處理資料層」或「完成整個 Phase 2」 |
| 可驗收結果 | build、資料驗證、解驗證或 tuning promotion |

題目或資料定義不完整時，要求 AI 先列出阻塞問題；不要以「請合理假設」取代模型確認。
