# Parameter 資料夾規則

## 繼承與命名

- 所有 Parameter 類別繼承 `ParameterBase`
- 數量欄位統一命名為 **`QTY`**（禁止 `Quantity`、`Amount` 等）
- string 屬性加 `= string.Empty;` 避免 CS8618
- Namespace：`ProjectName.Parameter`

## 與 Sets 的關係

Sets 必須在 `Dataload` 中由 Parameters 衍生，不可獨立定義：

```csharp
// ✓ 正確
public List<string> Items => parameter_Demand.Select(p => p.Item).ToList();
```

## 參數化天條

即使題目只有少數幾個數值，也必須定義為 Parameter，不得在 Constraint 或 Objective 中直接寫數字。
