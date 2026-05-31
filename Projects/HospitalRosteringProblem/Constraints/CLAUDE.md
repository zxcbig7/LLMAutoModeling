# Constraint 資料夾規則

## 基本規範

- 繼承 `ConstraintBase`，Namespace：`ProjectName.Constraints`
- 宣告 `public new int ConstraintCount = 0;`
- 限制式名稱格式：`$"{ConstraintName}@{idx1}@{idx2}"`
- 每條限制式建立後 `ConstraintCount++`

## 係數來源（天條）

所有數值係數透過 `dataload` 從 `Parameter.QTY` 取得，不得 hardcode 任何裸數字。
