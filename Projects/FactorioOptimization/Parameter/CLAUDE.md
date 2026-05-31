# Parameter 資料夾規則

## 繼承

所有 Parameter 類別繼承 `ParameterBase`。

## 欄位命名

- 數量欄位統一命名為 `QTY`
- string 屬性加 `= string.Empty;` 避免 CS8618
- Namespace：`ProjectName.Parameter`

## Sets 與 Parameters 的關係

Sets 必須由 Parameters 衍生（在 Dataload 裡），不可獨立定義：

```csharp
// ✓ 正確
public List<string> Items => parameter_Demand.Select(p => p.Item).ToList();
```

## 類別命名格式

`Parameter_描述.cs`，描述對應資料實體名稱。
