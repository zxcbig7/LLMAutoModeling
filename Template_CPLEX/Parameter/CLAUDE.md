# Parameter 資料夾規則（通用）

## 基本規範

- 所有 Parameter 類別繼承 `ParameterBase`
- **數量欄位統一命名為 `QTY`**，不可用 `Quantity`、`Amount` 等
- string 屬性加 `= string.Empty;` 避免 CS8618
- Namespace：`ProjectName.Parameter`

## 類別命名

`Parameter_描述.cs`，描述對應 Sets 名稱或資料實體。

## 範例

```csharp
public class Parameter_Demand : ParameterBase
{
    public string Item { get; set; } = string.Empty;
    public double QTY  { get; set; }
}
```
