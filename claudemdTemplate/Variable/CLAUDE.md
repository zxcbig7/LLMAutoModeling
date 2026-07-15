# Variable 資料夾規則

## 型別與命名

| 前綴 | 型別 | Build 方法 |
|------|------|-----------|
| `VariableB_` | Binary（0/1） | `BuildBVs<T>(sets...)` |
| `VariableX_` | Continuous | `BuildCVs<T>(sets...)` |
| `VariableI_` | Integer | `BuildIVs<T>(sets...)` |

## 定義規範

### 預設：source generator（AI 首選）

用 `AutoSetsGenerator` 一行 attribute 宣告，編譯期補完整 class，樣板最省、最不易錯。csproj 需以 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />` 掛入（範本 `Template_CPLEX` 已掛）。

```csharp
using OptimFoundation.Modeling;

[OptVar("Date:DateTime", "Employee", "Group")]   // 屬性順序＝Build*Vs 傳入順序；型別由類別名前綴決定
public partial class VariableB_ShiftAssign { }
```

- `[OptVar]` 只帶 sets，**不帶型別**——型別由類別名前綴決定（`VariableB_`→Binary / `VariableX_`→Continuous / `VariableI_`→Integer）
- 前綴不合法（非 B_/X_/I_）→ compile error `OPTF001`，訊息會教正確取名；`OptParam` 非 `Parameter_` 前綴 → `OPTF002`
- set 字串：`"Name"`＝string；`"Name:DateTime"` / `:int` / `:double` 指定型別
- Namespace：`ProjectName.Variable`；前綴同時對應 Build 方法（見上表）
- 可運作範例：`Projects/HospitalRostering_Generator`

### 後路：手寫（generator 不適用時）

- 繼承 `VariableBase`，Namespace：`ProjectName.Variable`
- string 屬性加 `= string.Empty;`，屬性順序對應 `Build*Vs` 傳入 sets 的順序
- 完整手寫示範：`Projects/HospitalRostering_Manual`

## VariableCreate 規範

```csharp
public void Build()
{
    _engine.BuildXVs<VariableX_Xxx>(_dataload.SetA, _dataload.SetB);
    Logging.Info($"Variables created: {_engine.varCount}");
}
```
