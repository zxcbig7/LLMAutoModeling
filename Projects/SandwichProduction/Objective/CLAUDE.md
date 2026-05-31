# Objective 資料夾規則

## 規範

- Namespace：`ProjectName.Objective`
- 結尾呼叫 `CreateMaximize()` 或 `CreateMinimize()`（二選一）

## 係數來源（天條）

所有係數透過 `dataload` 從 `Parameter.QTY` 取得，不得 hardcode。
