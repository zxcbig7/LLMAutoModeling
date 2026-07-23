# Variable 資料夾規則（通用）

## 變數型別命名

| 前綴 | 類型 | 預設 Build（依前綴推型） | 自訂 bounds |
|---|---|---|---|
| `VariableB_` | Binary（0/1） | `BuildVars<T>(sets...)` | `BuildBVs<T>(sets...)` |
| `VariableX_` | Continuous（連續） | `BuildVars<T>(sets...)` | `BuildCVs<T>(lb, ub, sets...)` |
| `VariableI_` | Integer（整數） | `BuildVars<T>(sets...)` | `BuildIVs<T>(lb, ub, sets...)` |

前綴不合法（非 B_/X_/I_）→ compile error `OPTF001`。

## 定義規範（預設：source generator）

光桿 `[OptVar]` + 逐維 `[OptDim<Set_X>("Name")]`，編譯期由 generator 補完整 class：

```csharp
using OptimFoundation.Modeling;
using ProjectName.Set;

[OptVar]
[OptDim<Set_Employee>("Employee")]
[OptDim<Set_Shift>("Shift")]
public partial class VariableB_Assign { }
```

- `[OptVar]` 光桿宣告不帶型別——型別由類別名前綴決定
- `[OptDim<TSet>]` 的泛型參數 MUST 是已掛 `[OptSet<T>]` 的 Set 積木；attribute 順序 = property 順序 = `Build*` 傳入 sets 的順序
- property 名 = `[OptDim]` 字串參數（PascalCase，不是 `ALL_CAPS`）
- Namespace：`ProjectName.Variable`
- 0 維（scalar）變數：只掛光桿 `[OptVar]`，不加任何 `[OptDim]`

## 定義規範（後路：手寫）

generator 不適用時才手寫，繼承 `VariableBase`、string 屬性加 `= string.Empty;` 避免 CS8618，屬性順序對應傳入 sets 的順序。NEVER 寫建構子——框架用 reflection 讀屬性順序組 key。

```csharp
public class VariableB_Assign : VariableBase
{
    public string Employee { get; set; } = string.Empty;
    public string Shift { get; set; } = string.Empty;
}
```

## VariableCreate 規範

- 在 `Build()` 裡呼叫所有 `BuildVars` / `Build*Vs`
- 結尾 `Logging.Info($"Variables created: {engine.varCount}")`

```csharp
engine.BuildVars<VariableB_Assign>(dataload.EMPLOYEE, dataload.SHIFT);
```

> 本範本 `Variable/` 底下的實碼已全部改用光桿 `[OptVar]` + `[OptDim<Set_X>]` 寫法（2026-07-20 遷移完成）。字串式 attribute 仍是框架逃生口，但 NEVER 在新 code 照抄。
