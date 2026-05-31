# Variable 資料夾規則

## 型別與命名

| 前綴 | 型別 | Build 方法 |
|------|------|-----------|
| `VariableB_` | Binary（0/1） | `BuildBVs<T>(sets...)` |
| `VariableX_` | Continuous | `BuildCVs<T>(sets...)` |
| `VariableI_` | Integer | `BuildIVs<T>(sets...)` |

## 定義規範

- 繼承 `VariableBase`
- string 屬性加 `= string.Empty;`
- 屬性順序對應 `Build*Vs` 傳入 sets 的順序
