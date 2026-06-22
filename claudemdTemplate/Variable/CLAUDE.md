# Variable 資料夾規則

## 型別與命名

| 前綴 | 型別 | Build 方法 |
|------|------|-----------|
| `VariableB_` | Binary（0/1） | `BuildBVs<T>(sets...)` |
| `VariableX_` | Continuous | `BuildCVs<T>(sets...)` |
| `VariableI_` | Integer | `BuildIVs<T>(sets...)` |

## 定義規範

### 預設：source generator（AI 首選）

用 `AutoSetsGenerator` 一行 attribute 宣告，編譯期補完整 class，樣板最省、最不易錯。csproj 需以 analyzer 掛入 `OptimModeling.Generators`（範本 `Template_CPLEX` 已掛）。

```csharp
using OptimModeling;

[OptVar(VarType.Binary, "Date:DateTime", "Employee", "Group")]   // 屬性順序＝Build*Vs 傳入順序
public partial class VariableB_ShiftAssign { }
```

- set 字串：`"Name"`＝string；`"Name:DateTime"` / `:int` / `:double` 指定型別
- Namespace：`ProjectName.Variable`；前綴 `VariableB_/X_/I_` 對應 Build 方法
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
