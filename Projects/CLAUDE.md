# Projects 資料夾

此資料夾包含所有 OptimFoundation CPLEX 題目專案。

## 每個專案的標準架構

```text
ProjectName/
├── CLAUDE.md           ← 專案概述、問題類型、執行流程
├── ProjectName.csproj
├── Program.cs
├── ProjectNameProblem.cs
├── Model/              ← 數學模型文件（Markdown）
├── Parameter/          ← ParameterBase 子類別，QTY 欄位
├── Data/               ← Dataload（Sets 由 Parameters 衍生）
├── Variable/           ← VariableB_ / VariableX_ / VariableI_
├── Objective/          ← ObjectiveFunction
└── Constraint/         ← ConstraintBase 子類別 + BuildModel
```

## 通用規則（適用所有專案）

- **禁止 Hardcode**：所有數值透過 `Parameter.QTY` 取得
- **兩階段開發**：先完成數學模型（Model/），再實作程式碼
- 詳細框架規則見根目錄 `CLAUDE.md` 與 `Template_CPLEX/CLAUDE.md`
- 各子資料夾規則見各自的 `CLAUDE.md`
- 新建專案範本：`claudemdTemplate/`
