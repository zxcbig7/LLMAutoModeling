# Data 資料夾規則

> 注意：此資料夾應重新命名為 `Data\`，Namespace 改為 `ProjectName.Data`。

## Dataload 結構

- 全域常數直接宣告為 `public field`
- Sets 用 `=>` 由 Parameters 衍生，不重複定義資料

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 整數變數解值格式 `:F0`，連續變數 `:F4`
- 呼叫 `CsvCtrl.SaveSolutionToCSV<T>(engine, dataId, userId)`

## 取解 API

```csharp
engine.GetSetVarValues<VariableX_Xxx>()
engine.GetVariableValue("VariableX_Xxx@label")
engine.GetObjectiveValue()
```
