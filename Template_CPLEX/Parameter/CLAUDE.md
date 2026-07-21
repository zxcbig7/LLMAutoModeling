# Parameter 資料夾規則（通用）

## 基本規範

- **數量欄位統一命名為 `QTY`**，不可用 `Quantity`、`Amount` 等
- property 名用 PascalCase（`Item`、`Date`），不是 `ALL_CAPS_WITH_UNDERSCORES`
- Namespace：`ProjectName.Parameter`

## 類別命名

`Parameter_描述.cs`，描述對應 Sets 名稱或資料實體。前綴非 `Parameter_` → compile error `OPTF002`。

## 定義規範（預設：source generator）

光桿 `[OptParam]` + 逐維 `[OptDim<Set_X>("Name")]`，generator 自動補 `QTY` 與建構子：

```csharp
using OptimFoundation.Modeling;
using ProjectName.Set;

[OptParam]
[OptDim<Set_Item>("Item")]
public partial class Parameter_Demand { }

[OptParam(HasValue = false)]
[OptDim<Set_Employee>("Employee")]
public partial class Parameter_PreAssign { }
```

- `HasValue = false` = 純 key 參數，不補 `QTY`
- `[OptDim<TSet>]` 的泛型參數 MUST 是已掛 `[OptSet<T>]` 的 Set 積木；attribute 順序 = key 組成順序
- 物件一律用 object-initializer 建立：`new Parameter_Demand { Item = i, QTY = 5 }`
- 漏掛 `[OptParam]` 又被 `Dataload : DataContext` 的欄位引用 → compile error `OPTF006`
- `[FullGrid]` 是 opt-in：只有語意上必須全格覆蓋才加，MILP 資料多半稀疏，預設 NEVER 加

## 定義規範（後路：手寫）

繼承 `ParameterBase`，string 屬性加 `= string.Empty;` 避免 CS8618。NEVER 寫建構子——框架用 reflection 讀屬性順序組 key。

```csharp
public class Parameter_Demand : ParameterBase
{
    public string Item { get; set; } = string.Empty;
    public double QTY { get; set; }
}
```

> 本範本 `Parameter/` 底下的實碼已全部改用光桿 `[OptParam]` + `[OptDim<Set_X>]` 寫法（2026-07-20 遷移完成）。字串式 attribute 仍是框架逃生口，但 NEVER 在新 code 照抄。
