# Parameter 資料夾規則

## 定義方式

### 預設：source generator（AI 首選）

光桿 `[OptParam]` + 逐維 `[OptDim<Set_X>("Name")]`（唯一 paved path，2026-07-15 定版）：

```csharp
using OptimFoundation.Modeling;
using ProjectName.Set;

[OptParam]                                    // 含值 → 自動補 QTY
[OptDim<Set_Date>("Date")]
[OptDim<Set_Group>("Group")]
public partial class Parameter_ShiftDemand { }

[OptParam(HasValue = false)]                  // 純 key → 不補 QTY
[OptDim<Set_Employee>("Employee")]
[OptDim<Set_Group>("Group")]
public partial class Parameter_PreAssign { }
```

- `[OptDim<TSet>("Name")]` 的泛型參數必須是已宣告 `[OptSet<T>]` 的 `Set_<Name>` 積木；attribute 順序 = property 順序 = 建構 key 的順序
- property 名 = `[OptDim]` 的字串參數（PascalCase，如 `Date`、`Group`），NOT `ALL_CAPS_WITH_UNDERSCORES`
- Parameter 類別漏掛 `[OptParam]`，一旦被 `Dataload : DataContext` 的 `List<Parameter_*>` 欄位引用（Stage 7 一定會）就是 **compile error `OPTF006`**——一定要掛，別靠這個代碼事後補救
- `[FullGrid]`（Core）是 **opt-in**：只有該 parameter 語意上必須全格覆蓋（例：每台機每天每班都要有產能值）才加；MILP 資料多半稀疏，預設 NEVER 加 `[FullGrid]`
- generator 自動補 `QTY`（除非 `HasValue = false`）+ 無參數建構子 + `params object[]` 建構子，但**專案端一律用 object-initializer**（`new Parameter_X { Date = d, Group = g, QTY = 5 }`），不要用位置參數建構——具名屬性可讀、不怕順序錯
- 可運作範例：`Projects/HospitalRostering_Generator`（註：建於本波資料防護規格之前，仍用舊字串式 `[OptParam("Date:DateTime", "Group")]`——那是遷移期逃生口，新專案 NEVER 照抄，一律用上面的 `[OptDim]` 寫法）

### 後路：手寫（generator 不適用時）

- 所有 Parameter 類別繼承 `ParameterBase`
- 數量欄位統一命名為 **`QTY`**（禁止 `Quantity`、`Amount` 等）
- PascalCase 屬性名（`Date`、`Group`，不是 `DATE`/`GROUP`），string 屬性加 `= string.Empty;` 避免 CS8618
- NEVER 寫建構子——框架用 reflection 讀屬性順序組 key，物件一律用 initializer 建立
- 完整手寫示範：`Projects/HospitalRostering_Manual`

## 共同規則

- Namespace：`ProjectName.Parameter`

## 與 Sets 的關係

Sets 必須在 `Dataload` 中由 Parameters 衍生，不可獨立定義字面清單；但 Set 仍是 `Set_<Name>` 積木（`[OptSet<T>]`），不是裸 `List<string>`：

```csharp
// ✓ 正確：Parameter 資料先建好，Set 積木用 LoadFrom 導出
ITEM.LoadFrom(parameter_Demand.Select(p => p.Item).Distinct());

// ✗ 禁止（與 parameter 重複定義字面值）
public List<string> Items = new() { "A", "B" };
```

## 參數化天條

即使題目只有少數幾個數值，也必須定義為 Parameter，不得在 Constraint 或 Objective 中直接寫數字。若某數值是由其他 Parameter 推導出的比值（如 Big-M），用 `Numeric.SafeRatio(...)`，NEVER 手寫裸除法。
