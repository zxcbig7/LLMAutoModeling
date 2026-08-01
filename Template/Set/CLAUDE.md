# Set 資料夾規則（通用）

## Dataload 規範

- 檔名 `Dataload.cs`，Namespace：`ProjectName.Set`
- MUST 宣告 `public partial class Dataload : DataContext`（`partial` + 繼承缺一不可）—— 少了任一個，generator 的註冊碼不會產生，這顆 Dataload 永遠不會被驗證
- MUST 透過 `OptData.Load(() => new Dataload())` 建構；NEVER 把裸 `new Dataload()` 當建構終點——那樣仍可編譯，但完全跳過框架的參照完整性 / 重複 key / 數值 sanity 驗證
- NEVER 手寫驗證邏輯——`DataContext` 建構完成時自動聚合檢查，一次列出所有問題丟 `DataValidationException`
- 全域常數（罰分權重等）直接宣告為 public field
- 由數據推導的比值（BigM 之類）用 `Numeric.SafeRatio(分子, 分母, context: "...")`，NEVER 手寫裸除法

## Set 積木

- MUST 一顆 Set 積木一個 `.cs` 檔，檔名 = 類別名（`Set_Item` → `Set/Set_Item.cs`）—— NEVER 把多顆塞進 `Sets.cs` —— Why: 一檔一顆才能靠檔名直接定位，與 `Parameter_*` / `Variable*_*` 慣例一致；集中檔改一顆就動到全部人的 diff
- MUST attribute 獨立一行，NEVER 與 class 宣告寫同一行 —— Why: 加第二個 attribute 時不必重排，宣告與修飾一眼分得開
- 元素型別 MUST 顯式寫出（`[OptSet<string>]`，NEVER 裸 `[OptSet]`）
- NEVER 用裸 `List<string>` 宣告 Set —— 積木化才能被 `DataContext` 註冊進驗證
- 成員載入預設 `.Load(source)`（內部即 `source.LoadSet("Set_Xxx")`，字串依 T 自動轉型）；手打字面值用 `.LoadInline("A", "B")`；NEVER 一律從 Parameters 反推 —— 只有「這顆 set 真的沒有來源資料」才用 `.LoadFrom(...)` 衍生 —— Why: 反推只拿得到參數表出現過的成員，題目允許但資料沒用到的維度會靜默消失，變數與約束跟著少建

```csharp
// Set/Set_Item.cs
using OptimFoundation.Modeling;

namespace ProjectName.Set
{
    [OptSet<string>]
    public partial class Set_Item { }
}
```

```csharp
// Dataload 內：
public Set_Item ITEM = new();
// 建構子內，每行一句顯式讀，Set 與 Parameter 各自從 source 進來：
ITEM.Load(source);
parameter_Demand = source.LoadParam<Parameter_Demand>("Parameter_Demand");
```

```csharp
// ✗ 禁止：裸 List，不會被框架註冊驗證
public List<string> Items = new() { "A", "B" };

// ✗ 禁止：多顆積木擠一個 Sets.cs + attribute 與 class 同一行
[OptSet<string>] public partial class Set_Item { }
[OptSet<DateTime>] public partial class Set_Date { }
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

> 本範本 `Set/Dataload.cs` 已套用資料防護層（`public partial class Dataload : DataContext` + `Set_*` 積木），`Program.cs` 一律走 `OptData.Load(() => new Dataload())`。
> 唯一與 paved path 的差別：範本用 `LoadInline(...)` 硬編示範資料，實際專案 MUST 改成顯式 ctor 讀 `IDataSource`（CSV / Oracle / 記憶體只換 source）。
