# Model Design — 文件與符號風格

本檔補充 Phase 1 的呈現風格；流程、硬規則與驗收仍分別以 [`../../AGENTS.md`](../../AGENTS.md)、[`../../../workflows/interactive/phase-1-model-design.md`](../../../workflows/interactive/phase-1-model-design.md) 為準。

## Model.md 結構

`Projects/<Project>/Model/<Project>_Model.md` 固定依序使用：問題描述、Terminology Mapping Table、SET、PARAM、VAR、CONSTRAINT、OBJ、已套用假設。

- 每個符號使用語意名稱與 PascalCase，例如 `Assign_{Employee,Date}`、`MachineCapacity`；不可使用 `x`、`i`、`c1` 等單一字母或無語意代號。
- 每個集合、參數與變數在第一次使用前宣告；維度、單位、型別、上下界與資料來源必須可由 Coding 機械轉譯。
- Constraint 保留題目原本的 LHS／運算子／RHS；不移項、不化簡、不翻轉方向。每條標示 pattern tag 與適用維度。
- 所有題目數值都以具名 parameter 表示；CONSTRAINT 與 OBJ 中不放裸數字。

## Markdown 與數學式

- 使用 `##` 作主要章節、`###` 作元素或約束名稱；不可跳級。
- 行內數學使用 `$...$`，獨立方程使用 `$$...$$`；求和必須標明 index domain。
- Terminology Mapping Table 每一列都保留原始敘述、角色與單位；確認術語後直接更新該表，不另建 Glossary。
