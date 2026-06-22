# dataset/

## NLP4P.csv

自然語言最佳化題庫（OptiMUS / NL4OPT 風格），約 2 萬筆。每列一題，欄位：

| 欄位 | 內容 |
|------|------|
| `ID` | 題目編號（如 `NLP4P_001`） |
| `description` | 自然語言題目描述 |
| `solution` | 參考解（JSON：`variables` + `objective`） |
| `problem_info` | 結構化模型（JSON：parametrized_description / parameters / variables / constraints / objective，含 gurobipy 片段） |
| `parameters` | 該題參數實值（JSON） |
| `optimus_code` | 自動生成的求解程式碼 |

### 用途

當作 **Phase 1（Modeling）的練習 / 測試素材**：把某題的 `description` 丟給 AI，要求先產出 `Model/*.md` 數學模型，再依框架（預設 generator + OptModel）翻成 OptimFoundation 專案，最後用 `solution` 欄位驗證目標值。

> 注意：`optimus_code` / `problem_info` 內的程式碼是 **gurobipy**，僅供對照模型結構，**不可直接套用**到本框架（本框架是 CPLEX + OptimFoundation）。
