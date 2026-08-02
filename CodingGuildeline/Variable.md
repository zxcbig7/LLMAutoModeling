# Variable 資料夾規則

## 型別與命名

| 前綴 | 型別 | 預設 Build（依前綴推型） | 自訂 bounds |
|------|------|-----------|-----------|
| `VariableB_` | Binary（0/1） | `BuildVars<T>(sets...)` / `BuildBVs<T>(sets...)` | 不支援自訂 bounds；固定 `[0,1]` |
| `VariableX_` | Continuous | `BuildVars<T>(sets...)` | `BuildCVs<T>(sets...)` / `BuildCVs<T>(lb, ub, sets...)` |
| `VariableI_` | Integer | `BuildVars<T>(sets...)` | `BuildIVs<T>(sets...)` / `BuildIVs<T>(lb, ub, sets...)` |

## 定義規範

### 預設：source generator（AI 首選）

用 `AutoSetsGenerator` 光桿 `[OptVar]` + 逐維 `[OptDim<Set_X>("Name")]` 宣告，編譯期補完整 class，樣板最省、最不易錯（2026-07-15 定版的唯一 paved path）。csproj 需以 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />` 掛入（範本 `Template_CPLEX` 已掛）。

```csharp
using OptimFoundation.Modeling;
using ProjectName.Set;

[OptVar]                                 // 光桿，不帶型別——型別由類別名前綴決定
[OptDim<Set_Date>("Date")]
[OptDim<Set_Employee>("Employee")]
[OptDim<Set_Group>("Group")]
public partial class VariableB_ShiftAssign { }
```

- `[OptVar]` 只光桿宣告，**不帶型別**——型別由類別名前綴決定（`VariableB_`→Binary / `VariableX_`→Continuous / `VariableI_`→Integer）
- 前綴不合法（非 B_/X_/I_）→ compile error `OPTF001`，訊息會教正確取名
- Variable 類別漏掛 `[OptVar]` → generator 不生成屬性/建構子，該 partial class 留空，下游用到屬性處變成一般 C# 錯誤（如 `CS1061`）——務必掛，不要漏（`OPTF006` 是 `Parameter_*`/`Set_*` 專屬：只在它們被 `Dataload : DataContext` 的欄位引用卻漏掛 attribute 時才觸發，Variable 類別不會被 Dataload 引用，不適用此代碼）
- `[OptDim<TSet>("Name")]` 泛型參數 = 已宣告 `[OptSet<T>]` 的 `Set_<Name>` 積木；attribute 順序 = property 順序 = `BuildVars`/`Build*Vs` 傳入 sets 的順序，property 名 = `[OptDim]` 字串參數（PascalCase）
- Namespace：`ProjectName.Variable`
- 可運作範例：`Projects/HospitalRostering_Generator`（註：建於本波規格之前，仍用舊字串式 `[OptVar("Date:DateTime", "Employee", "Group")]`——那是遷移期逃生口，新專案 NEVER 照抄，一律用上面的 `[OptDim]` 寫法）

### 後路：手寫（generator 不適用時）

- 繼承 `VariableBase`，Namespace：`ProjectName.Variable`
- PascalCase 屬性名（不是 `ALL_CAPS`），string 屬性加 `= string.Empty;`，屬性順序對應 `Build*Vs` 傳入 sets 的順序
- NEVER 寫建構子——框架用 reflection 讀屬性順序組 key
- 完整手寫示範：`Projects/HospitalRostering_Manual`

`BuildBVs<T>` 只有 sets 參數，不存在 `(lb, ub, sets...)` overload。需要其他整數界限時使用 `VariableI_` + `BuildIVs<T>(lb, ub, sets...)`。

## Program.cs 註冊規範

```csharp
var model = new OptModel("Canonical") // 模型定義；執行屬 runner
    .AddVariables(engine => engine.BuildVars<VariableX_Xxx>(data.SetA, data.SetB))
    .AddVariables(engine => engine.BuildCVs<VariableX_Bounded>(0, 1, data.SetA));
```

- 每個 variable family 各寫一個 `.AddVariables(...)`，直接列在 `Program.cs`。
- NEVER 新增只負責轉呼叫的 class 或 local function。
- 先完成所有 `.AddVariables(...)`，再註冊 `.AddObjective(...)` 與 `.AddConstraints(...)`。
