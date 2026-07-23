# Set 資料夾規則（通用）

## Dataload 規範

- 檔名 `Dataload.cs`，Namespace：`ProjectName.Set`
- MUST 宣告 `public partial class Dataload : DataContext`（`partial` + 繼承缺一不可）—— 少了任一個，generator 的註冊碼不會產生，這顆 Dataload 永遠不會被驗證
- MUST 透過 `OptData.Load(() => new Dataload())` 建構；NEVER 把裸 `new Dataload()` 當建構終點——那樣仍可編譯，但完全跳過框架的參照完整性 / 重複 key / 數值 sanity 驗證
- NEVER 手寫驗證邏輯——`DataContext` 建構完成時自動聚合檢查，一次列出所有問題丟 `DataValidationException`
- 全域常數（罰分權重等）直接宣告為 public field
- 由數據推導的比值（BigM 之類）用 `Numeric.SafeRatio(分子, 分母, context: "...")`，NEVER 手寫裸除法

## Set 積木

- 一個 Set 一顆積木：`[OptSet<T>] public partial class Set_<Name> { }`，元素型別 MUST 顯式寫出
- NEVER 用裸 `List<string>` 宣告 Set —— 積木化才能被 `DataContext` 註冊進驗證
- 成員載入：字面值用 `.LoadInline("A", "B")`；由 Parameters 衍生用 `.LoadFrom(...)`，不重複定義資料

```csharp
[OptSet<string>] public partial class Set_Item { }

// Dataload 內：
public Set_Item ITEM = new();
// 建構子內，Parameter 資料先建好再導出 Set：
ITEM.LoadFrom(parameter_Demand.Select(p => p.Item).Distinct());
```

```csharp
// ✗ 禁止：裸 List，不會被框架註冊驗證
public List<string> Items = new() { "A", "B" };
```

## WriteToCSV 規範

- 必須先呼叫 `FolderDir.Solution.CreateFolder()`
- 解值輸出格式：整數變數 `:F0`、連續變數 `:F4`
- 呼叫 `CsvCtrl.WriteSolution<T>(engine, "ProjectName", "USER")`

## 取解方式

```csharp
engine.GetSetVarValues<VariableX_Xxx>() // Dictionary<string, double>
engine.GetVariableValue("VariableX_Xxx@label") // 單一變數值，分隔符是 @
engine.GetObjectiveValue() // 目標函數值
```

> ⚠ 本範本 `Set/Dataload.cs` 的實碼**尚未套用資料防護層**（仍是 `public class Dataload`、裸 `new Dataload()`、Set 用硬編字面陣列），落後本文件所述的 paved path。照抄本範本時 MUST 依上述規範改寫，NEVER 原樣複製資料層。
