# modeling · 交付前自檢

> 規則本體在 `.claude/workflows/interactive/phase-1-model-design.md`（1d 建模自驗）與 `.claude/rules/AGENTS.md`（天條）。
> 本檔只是交付前的勾選面——每項都要**實際回去檔案裡看過**才打勾，不憑印象。

## 1a 去故事化 + 單位

- [ ] 每個數字都帶單位
- [ ] 單位不一致（時/分、噸/公斤）已換算成單一單位並標明換算式
- [ ] 敘述 self-contained：不看原始題目也讀得懂

## 1b 語義判別 + Terminology Table

- [ ] 1a 敘述的**每個子句**都有 role（parameter / variable / derived / constraint / objective / irrelevant）
- [ ] 沒有子句被靜默略過（無關的要明確標 `irrelevant`）
- [ ] 沒有自動推導的 derived 值（題目沒明說就不推；真要推另立一列標來源）
- [ ] Terminology Mapping Table 已寫進 Model.md 本體
- [ ] 專案內**沒有** `Glossary.md`

## 1c 結構抽取（Model.md 五段）

- [ ] 文件順序：問題描述 → Terminology → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
- [ ] **宣告先於使用**：CONSTRAINT / OBJ 出現的每個符號都已在 SET / PARAM / VAR 宣告
- [ ] 每個 PARAM 標了 Dim；每個 VAR 標了型別（Continuous / Binary / Integer）+ LB / UB
- [ ] 每條 CONSTRAINT 是 `LHS op RHS` 原形，**未預先移項 / 化簡 / 翻方向**
- [ ] 每條 CONSTRAINT 標了 pattern tag（`linearization-patterns.md` 8 類之一）+ Dim + 條號 `[Cn]`
- [ ] 每個 `sum` 標明 index 範圍（∀ 哪個 set、over 哪個 set）
- [ ] OBJ 段存在且標明方向（max / min），所有項在 LHS
- [ ] CONSTRAINT / OBJ 內**沒有裸數字**，每個數值都是具名 PARAM
- [ ] 沒有單一字母符號（`i`/`j`/`k`/`x`/`y`/`z`/`t`），符號皆語意命名
- [ ] Set 成員字串 PascalCase 單數（`"Truck"` 不是 `"trucks"`）

## 轉譯友善度（Phase 2 才不會卡）

- [ ] 同一個下標的係數併成同一張參數表（→ 對到同一個 `Parameter_` 類）
- [ ] 每條 constraint 的條號 `[Cn]` 可直接當 `Constraint_<語意>.cs` 的檔名錨點
- [ ] 變數維度數 = 之後 `[OptDim]` 的個數，順序已固定

## Gate

- [ ] 已列出「已套用的預設假設」清單
- [ ] 所有表外歧義都已追問並回填 Terminology Table（沒有「我猜他的意思是」）
- [ ] **本階段沒有產生任何 `.cs`**
- [ ] `status.json` 已更新（`phase: modeling`）
- [ ] 已明確請使用者確認模型，並說明確認後才會進 `coding`
