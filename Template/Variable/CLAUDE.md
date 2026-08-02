# Variable 資料夾規則（通用）

## 變數型別命名

| 前綴 | 類型 | 預設 Build（依前綴推型） | 自訂 bounds |
|---|---|---|---|
| `VariableB_` | Binary（0/1） | `BuildVars<T>(sets...)` / `BuildBVs<T>(sets...)` | 不支援自訂 bounds；固定 `[0,1]` |
| `VariableX_` | Continuous（連續） | `BuildVars<T>(sets...)` | `BuildCVs<T>(lb, ub, sets...)` |
| `VariableI_` | Integer（整數） | `BuildVars<T>(sets...)` | `BuildIVs<T>(lb, ub, sets...)` |

前綴不合法（非 B_/X_/I_）→ compile error `OPTF001`。
`BuildBVs<T>` 只有 sets 參數；不存在 `(lb, ub, sets...)` overload。

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

## Program.cs 三階段註冊規範

- `Program.cs` 先載入材料，再直接建立一個 `OptModel`，最後交給 `OptProject` 或 `OptExperiment`。
- 每個 variable family 各占一個 `.AddVariables(...)`；即使種類多也不使用純轉呼叫 local helper 隱藏清單。
- 不建立只負責轉呼叫的 facade class 或 local helper；solve / experiment 共用同一個 `OptModel`。
- 框架固定依 variables → objective → constraints 執行，不靠註冊順序維持正確性。

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.EMPLOYEE, data.SHIFT))
    .AddVariables(engine => engine.BuildCVs<VariableX_Slack>(0, maxSlack, data.EMPLOYEE))
    .AddObjective(engine => new ObjectiveFunction(engine, data.EMPLOYEE, data.parameter_Cost).Build())
    .AddConstraints(engine => new Constraint_Coverage(engine, data.EMPLOYEE, data.SHIFT, data.parameter_Demand).Build());
```

> 本範本 `Variable/` 底下的實碼已全部改用光桿 `[OptVar]` + `[OptDim<Set_X>]` 寫法（2026-07-20 遷移完成）。字串式 attribute 仍是框架逃生口，但 NEVER 在新 code 照抄。
