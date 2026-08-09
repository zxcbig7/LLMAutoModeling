# /review — Phase 1 Model Design

審查目前專案的 `Model/<Project>_Model.md`，不產生任何 `.cs`。依 [`../../rules/AGENTS.md`](../../rules/AGENTS.md) 與 [`../../rules/Ph1_Modeling/model-design-guide.md`](../../rules/Ph1_Modeling/model-design-guide.md)（自驗清單在 §7.1、反向紅隊在 §7.2、pattern 對照在附錄 A）檢查：題目子句是否完整分類、Terminology Mapping Table 是否完備、SET／PARAM／VAR／CONSTRAINT／OBJ 是否宣告先於使用、數值與單位是否保真、constraint 是否為原形並有 pattern tag。

輸出 `Blocker`、`Question`、`Improvement`、`Verified` 四節。每項附 Model.md 章節或行號與具體修正；若存在術語或語意歧義，列為 `Question` 並停止後續推論。
