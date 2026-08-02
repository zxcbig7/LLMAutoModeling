# Model 資料夾規則

## 文件規範

- 檔名：`ProjectName_Model.md`
- 必要章節順序：**問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設**
- Terminology Mapping Table 就是本專案的持久化術語表；未知術語先追問，確認後回填本文件
- `Model/` 只保留這一份模型文件，NEVER 另建 `Glossary.md`
- 數學模型階段不考慮任何程式細節，純數學定義

## Constraints 格式

- 編號 `[C1]`、`[C2]`、… 與程式碼一一對應
- 每條標注語意說明與涉及的集合索引

## 參數化原則

Model 中出現的所有數值（係數、上下限、比例等）都必須對應到 `Parameter` 類別的 `QTY` 欄位，不允許在程式碼中直接使用裸數字。
