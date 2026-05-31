# Data 資料夾規則

## Dataload 結構

- Namespace：`ProjectName.Data`
- 全域常數直接宣告為 `public field`
- Sets 用 `=>` 由 Parameters 衍生，不重複定義資料

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 整數變數解值格式 `:F0`，連續變數 `:F4`
- 呼叫 `CsvCtrl.SaveSolutionToCSV<T>(engine, dataId, userId)`

## 取解 API

```csharp
engine.GetSetVarValues<VariableX_Xxx>()            // Dictionary<varName, double>
engine.GetVariableValue("VariableX_Xxx@label")     // 單一變數值
engine.GetObjectiveValue()                          // 目標函數值
```

## Helper Methods（依需求提供）

模型若有複雜的係數查詢邏輯，透過 helper method 封裝，讓 Constraint 程式碼保持乾淨：

```csharp
public double GetCoeff(string key) =>
    parameter_Xxx.First(p => p.Key == key).QTY;
```
