# Set 資料夾規則（通用）

## Dataload 規範

- Namespace：`ProjectName.Set`
- 全域常數直接宣告為 public field（不包在 property 裡）
- **Sets 用 `=>` 由 Parameters 衍生**，不重複定義資料

```csharp
// ✓ 正確
public List<string> Items => parameter_Demand.Select(p => p.Item).ToList();

// ✗ 禁止
public List<string> Items = new() { "A", "B" };  // 與 parameter 重複
```

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 解值輸出格式：整數變數 `:F0`、連續變數 `:F4`
- 呼叫 `CsvCtrl.SaveSolutionToCSV<T>(engine, "ProjectName", "USER")`

## 取解方式

```csharp
engine.GetSetVarValues<VariableX_Xxx>()          // Dictionary<string, double>
engine.GetVariableValue("VariableX_Xxx@label")   // 單一變數值
engine.GetObjectiveValue()                        // 目標函數值
```
