# Parameter 資料夾規則

## 定義方式

### 預設：source generator（AI 首選）

```csharp
using OptimModeling;

[OptParam("Date:DateTime", "Group")]                       // 含值 → 自動補 QTY
public partial class Parameter_ShiftDemand { }

[OptParam("Employee", "Group", HasValue = false)]          // 純 key → 不補 QTY
public partial class Parameter_PreAssign { }
```

generator 自動補 `QTY`（除非 `HasValue = false`）+ 無參數建構子 + `params object[]` 建構子。可運作範例：`Projects/HospitalRostering_Generator`。

### 後路：手寫（generator 不適用時）

- 所有 Parameter 類別繼承 `ParameterBase`
- 數量欄位統一命名為 **`QTY`**（禁止 `Quantity`、`Amount` 等）
- string 屬性加 `= string.Empty;` 避免 CS8618
- 完整手寫示範：`Projects/HospitalRostering_Manual`

## 共同規則

- Namespace：`ProjectName.Parameter`

## 與 Sets 的關係

Sets 必須在 `Dataload` 中由 Parameters 衍生，不可獨立定義：

```csharp
// ✓ 正確
public List<string> Items => parameter_Demand.Select(p => p.Item).ToList();

// ✗ 禁止（與 parameter 重複定義）
public List<string> Items = new() { "A", "B" };
```

## 參數化天條

即使題目只有少數幾個數值，也必須定義為 Parameter，不得在 Constraint 或 Objective 中直接寫數字。
