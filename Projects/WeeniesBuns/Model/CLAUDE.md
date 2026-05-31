# Model 資料夾規則

## 文件規範

- 檔名：`ProjectName_Model.md`
- 必要章節順序：**問題描述 → Sets → Parameters → Decision Variables → Objective → Constraints**
- 數學模型階段不考慮任何程式細節，純數學定義

## Constraints 格式

- 編號 `[C1]`、`[C2]`、… 與程式碼一一對應
- 每條標注語意說明

## 參數化原則

Model 中出現的所有數值都必須對應到 `Parameter.QTY`，不允許在程式碼中直接使用裸數字。
