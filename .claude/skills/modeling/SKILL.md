---
name: modeling
description: Phase 1 建模 orchestrator——把自然語言最佳化題目降維成 Model.md 數學模型，停在使用者確認 gate。當使用者說「幫我建模」「新題目 / 新最佳化問題」「把這個問題寫成數學模型」「LP / IP / MILP 模型」「這題怎麼建模」時使用。NEVER 在本階段產任何 .cs。
argument-hint: <問題敘述，或題目檔路徑>
---

# modeling — Phase 1 建模調度

本 repo 的開發是三階段 phase gate：**`modeling`（本 skill）→ `coding` → `tuning`**，依序推進、不得跳階。

你是第一棒：把自然語言題目變成完整、無歧義、程式好轉譯的 `Model/<Project>_Model.md`，然後**停下來等使用者確認**。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.claude/skills/modeling/SKILL.md`）。
> 規則單一來源：`.claude/rules/AGENTS.md`（天條）+ `.claude/workflows/interactive/README.md`（三階段總綱）+ `.claude/workflows/interactive/phase-1-model-design.md`（本階段細則）。
> 本 skill 只做調度與 gate 把關，**NEVER 在此複製規則**——每次執行都實際讀那三份檔，不憑記憶。

## 輸入（`$ARGUMENTS`）

`/modeling <問題敘述>`——參數就是題目本身，也就是 Step 2 「1a 去故事化」的輸入。

| 參數形式 | 動作 |
| --- | --- |
| 自然語言題目敘述 | 直接當題目原文，進 Step 0 |
| 檔案路徑（`.md` / `.txt` / `.csv`） | 先 `Read` 該檔，內容當題目原文；讀不到就停下回報，NEVER 用檔名猜內容 |
| 空 | 停下問使用者要題目，NEVER 從既有 `Projects/` 挑一題來做 |

參數同時是 `<Project>` 命名的依據：由題目語意取 PascalCase 名（醫院排班 → `HospitalRostering`），Step 0 回報時把取的名字講出來讓使用者當場否決。

## Step 0 · 定位

1. 讀 `.claude/rules/AGENTS.md` 的天條段、`.claude/workflows/interactive/README.md`
2. 決定 `<Project>` 名（PascalCase，無空白）；專案位置固定 `Projects/<Project>/`
3. 讀 `Projects/<Project>/status.json`（不存在 = 全新題目）判斷是否已有進度：

| 現況 | 動作 |
| --- | --- |
| 無 `Model/<Project>_Model.md` | 從 Step 1 全新開始 |
| 有 Model.md、`modelConfirmed: false` | 只補未完成的段落，NEVER 重寫已確認段落 |
| `modelConfirmed: true` | 本 skill 已完成，改用 `coding`；使用者要改模型才回本階段 |

4. 一句話回報：「Phase 1（原因），接下來做 X」

## Step 1 · 讀本階段細則

讀 `.claude/workflows/interactive/phase-1-model-design.md`（含 4 階段建模法、元素 metadata 模板、預設慣例表、追問表、1d 自驗清單）。
constraint 手法查 `.claude/workflows/interactive/linearization-patterns.md`。
想確認「這樣寫模型 Phase 2 好不好轉譯」查 `.claude/workflows/interactive/model-to-code.md` 的一句話對應表。

## Step 2 · 4 階段降維（依序，NEVER 跳步）

| 階段 | 產物 | 只做這件事 |
| --- | --- | --- |
| 1a 去故事化 + 單位正規化 | 乾淨的問題敘述 | 純自然語言，NEVER 引入符號 |
| 1b 語義判別 + Terminology Table | Terminology Mapping Table | 每個子句強制歸類 role，無關的標 `irrelevant` |
| 1c 結構抽取 | Model.md 五段 | SET / PARAM / VAR / CONSTRAINT / OBJ，每條 constraint 標 pattern tag |
| 1d 建模自驗 | 已套用假設清單 | 對照 `checklist.md` 逐條驗，全過才進 Step 4 |

Model.md 固定順序：問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設。
術語表**內嵌 Model.md**，NEVER 另建 `Glossary.md`。

## Step 3 · 歧義處理協定

1. 先查 `phase-1-model-design.md` 的**預設慣例表**——表內項目直接套用，不追問，但 MUST 列進「已套用假設」
2. 表外的不確定 → 查 Model.md 的 Terminology Mapping Table
3. 兩處都查不到 → **停下來問使用者**，NEVER 猜；確認後回填同一張表再繼續

追問時一次問完所有已知歧義，不要一題一題來回。

## Step 4 · 交付並停在 gate

交付內容：Model.md 路徑 + 模型摘要（幾個 set / param / var / constraint）+ **已套用假設清單** + 待確認歧義（若有）。

**出口 gate**：使用者明說「模型確認」/「開始實作」/「可以寫 code 了」才算通過。
通過後把 `modelConfirmed` 設 true 並提示改用 `coding`，**本 skill 不寫任何 `.cs`**。

## Step 5 · 更新 status.json（`Projects/<Project>/status.json`）

```json
{
  "phase": "modeling",
  "modelConfirmed": false,
  "buildOk": false,
  "solveVerified": false,
  "tuningRound": 0,
  "updated": "YYYY-MM-DD"
}
```

## 交付前

逐條對照同資料夾的 `checklist.md`，全過才交付。

## Fatal

- NEVER 本階段產生任何 `.cs`（使用者明說「模型我確認過了直接寫」才視同通過 gate，且該由 `coding` 接手）
- NEVER 用單一字母符號（`i`/`j`/`x`/`y`/`t`）命名模型元素
- NEVER 自行詮釋不清楚的術語或自動推導 derived 值
- NEVER 在 Model.md 預先移項 / 化簡 / 翻轉比較方向
- NEVER 在本階段討論 soft constraint / penalty（屬 Phase 3）
- NEVER 用絕對路徑
