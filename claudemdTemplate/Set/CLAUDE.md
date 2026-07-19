# Set 資料夾規則

## Set 積木

- 一個 Set 一顆積木：`[OptSet<T>] public partial class Set_<Name> { }`（`T` = `string`/`DateTime`/`int`/...，元素型別；`[OptSet]` 無泛型參數預設 `string`）
- NEVER 用裸 `public List<string> ITEM = new();` 宣告 Set —— 積木化才能被 `DataContext` 註冊進驗證（見下）
- 成員載入：字面值用 `.LoadInline("A", "B")`；由 Parameters 資料衍生用 `.LoadFrom(parameter_Xxx.Select(p => p.Item).Distinct())`（先建好 Parameter 資料，再導出 Set，不獨立重複定義）

```csharp
// ✓ Set 積木，成員由 Parameter 衍生（Parameter 資料先建好）
[OptSet<string>] public partial class Set_Item { }
// ...
public Set_Item ITEM = new();
// 建構子內：
ITEM.LoadFrom(parameter_Demand.Select(p => p.Item).Distinct());

// ✗ 禁止：裸 List，不會被框架註冊驗證
public List<string> Items = new() { "A", "B" };
```

## Dataload 結構

- 檔名 `ProjectNameDataload.cs`，Namespace：`ProjectName.Set`
- MUST 宣告 `public partial class Dataload : DataContext`（`partial` + 繼承缺一不可）—— 少了任一個，generator 的註冊碼不會產生，這顆 Dataload 永遠不會被驗證
- MUST 透過 `OptData.Load(() => new Dataload())` 建構（見 Root 的 Program.cs 骨架）；NEVER 在文件或範例中把 `new Dataload()` 當成建構終點——那樣仍可編譯，但完全跳過驗證
- NEVER 手寫驗證邏輯（如 `ValidateSetsCoverParameters()`、手動掃 dangling reference / 重複 key）——`DataContext` 建構完成時自動聚合檢查（參照完整性 / 重複 index key / 數值 sanity），一次列出所有問題丟 `DataValidationException`；專案端只留顯式宣告
- 全域常數（罰分權重等）直接宣告為 `public field`
- 由數據推導的比值（BigM 之類）用 `Numeric.SafeRatio(分子, 分母, context: "...")`，NEVER 手寫 `/` 裸除法

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 呼叫 `CsvCtrl.WriteSolution<T>(engine, dataId, userId)`
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
