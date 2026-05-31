# Model 資料夾規則

## 數學模型文件規範

- 檔名格式：`ProjectName_Model.md`
- 必要章節順序：問題描述 → Parameters → Decision Variables → Objective → Constraints

## Constraints 命名

- 編號格式：`[C1]`、`[C2]`、…，與程式碼一一對應
- 每條限制式標注語意說明

## 本專案模型

見 `FactorioOptimization_Model.md`
- 問題類型：MIP
- 機台變數為整數，資源流量為連續
- 核心：5:9:11 比例約束（C12、C13）
