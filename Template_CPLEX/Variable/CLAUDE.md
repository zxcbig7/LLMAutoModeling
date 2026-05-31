# Variable 資料夾規則（通用）

## 變數型別命名

| 前綴 | 類型 | Build 方法 |
|------|------|-----------|
| `VariableB_` | Binary（0/1） | `BuildBVs<T>(sets...)` |
| `VariableX_` | Continuous（連續） | `BuildCVs<T>(sets...)` |
| `VariableI_` | Integer（整數） | `BuildIVs<T>(sets...)` |

## 定義規範

- 繼承 `VariableBase`
- Namespace：`ProjectName.Variable`
- string 屬性加 `= string.Empty;`
- 屬性順序對應 `Build*Vs` 傳入 sets 的順序

## VariableCreate 規範

- 在 `Build()` 裡呼叫所有 `Build*Vs`
- 結尾 `Logging.Info($"Variables created: {_engine.varCount}")`

## 範例

```csharp
public class VariableB_Assign : VariableBase
{
    public string Employee { get; set; } = string.Empty;
    public string Shift    { get; set; } = string.Empty;
}
// VariableCreate: engine.BuildBVs<VariableB_Assign>(dataload.Employees, dataload.Shifts);
```
