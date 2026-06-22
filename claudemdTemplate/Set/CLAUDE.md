# Set 資料夾規則

## Dataload 結構

- 檔名 `ProjectNameDataload.cs`，Namespace：`ProjectName.Set`
- 全域常數（罰分權重等）直接宣告為 `public field`
- Sets 用 `=>` 由 Parameters 衍生，不重複定義資料

```csharp
// ✓ Sets 由 Parameter 衍生
public List<string> Items => parameter_Demand.Select(p => p.Item).Distinct().ToList();

// ✗ 禁止：與 parameter 重複定義
public List<string> Items = new() { "A", "B" };
```

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 呼叫 `CsvCtrl.SaveSolutionToCSV<T>(engine, dataId, userId)`
- 整數變數解值格式 `:F0`，連續變數 `:F4`

## 取解 API

```csharp
engine.GetSetVarValues<VariableX_Xxx>()        // Dictionary<varName, double>
engine.GetVariableValue("VariableX_Xxx@label") // 單一變數值
engine.GetObjectiveValue()                      // 目標函數值
```

## Helper Methods（依需求提供）

模型若有複雜的係數查詢邏輯，透過 helper method 封裝，讓 Constraint 程式碼保持乾淨：

```csharp
public double GetCoeff(string key) => parameter_Xxx.First(p => p.Key == key).QTY;
```

> 命名歷史：本資料夾舊稱 `Data/`，現統一為 `Set/`（與 Template_CPLEX 及框架慣例一致）。
