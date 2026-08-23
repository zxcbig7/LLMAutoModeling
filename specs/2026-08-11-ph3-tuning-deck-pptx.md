---
title: 把 AI-CPLEX-Tuning-20min.md 轉成 3 頁無主題 .pptx，內容可直接複製到既有簡報
status: approved
created: 2026-08-11
updated: 2026-08-11
modules: [docgen, pptx]
---

# PH3 Tuning 簡報產出（3 頁、無主題 .pptx）

## Summary

把既有的口語化說明文件 `ppt/auto tuning/AI-CPLEX-Tuning-20min.md` 轉成一份 3 張投影片的 `.pptx`。這份 `.pptx` 不做任何視覺設計（不配色、不換字型、不套主題、不加背景），只做版面排列（標題 / 條列層級 / 表格位置 / 左右欄配置），目的是讓使用者開檔後把物件複製貼上到公司既有的簡報範本裡。

## Motivation / Why

來源檔是「寫給人讀」的說明稿：每頁混雜著「本頁要回答的問題」「投影片要放的列點」「建議主視覺」「講稿敘述」「轉場」等寫作用的 meta 段落，還有 ASCII art 流程圖與 markdown 表格。這些東西直接貼進 PowerPoint 會壞掉（ASCII art 一換字型就爛版），也分不出哪些是投影片正文、哪些是講者備註。

需要一次機械性的轉換，把「文件」變成「投影片物件」，而視覺設計交由使用者既有的簡報範本負責——所以這裡刻意不做設計。

## Scope

### In Scope

- 產出 `.pptx`，**剛好 3 張投影片**，對應來源檔的 Slide 1 / Slide 2 / Slide 3
- 每張投影片：標題、正文條列（含子層）、原生 PowerPoint 表格
- 來源檔的 ASCII art 流程圖改寫成編號條列
- 來源檔的「講稿敘述」「轉場」「收尾句」原文放進該頁的 speaker notes
- 來源檔的「建議主視覺」改寫成版面配置建議，放在 speaker notes 最上方，標明是配置提示不是講稿
- 保留兩個記憶點：**926 億種組合**、**45% 單次加速不可直接採用**
- 產生器腳本落檔（可重跑、可改一行重生，不是一次性丟棄的產物）

### Out of Scope

- 任何視覺設計：配色、字型家族、背景、logo、母片、主題、圖示、動畫、轉場特效
- 封面頁、議程頁、章節頁、收尾頁（使用者已決定「忠實 3 頁，不擴充」）
- 擴充成 20 分鐘份量的內容（檔名的 `20min` 不採信，以檔案實際內容的 3 頁為準）
- 新增任何來源檔沒有的論述、數據或範例
- 來源檔的 meta 段落原樣搬運：「本頁要回答的問題」「簡報主線」「簡報製作規則」不進投影片
- 修改來源檔 `AI-CPLEX-Tuning-20min.md` 本身

## User Stories / Use Cases

1. As a 簡報者, I want to 開啟產出的 `.pptx` 後直接框選整頁物件複製到公司範本, so that 不用手動把 markdown 一條一條敲進 PowerPoint。
2. As a 簡報者, I want to 表格是 PowerPoint 原生表格而不是一坨管線符號文字, so that 貼過去之後套範本表格樣式就能用。
3. As a 講者, I want to 講稿與版面提示留在 speaker notes 而不是投影片上, so that 投影片正文乾淨、上台時仍看得到要講什麼。
4. As a 內容負責人, I want to 投影片上的每一句話都能在來源檔找到出處, so that 不會有 AI 自己編出來的內容混進對外簡報。

## Acceptance Criteria

- [ ] 產出檔 `ppt/auto tuning/AI-CPLEX-Tuning-3slides.pptx` 能被 PowerPoint 正常開啟，投影片張數剛好為 3
- [ ] 三張標題逐字對應來源檔：`PH3 幫我們用實驗找出可採用的 solver 設定` / `每一輪實驗，都先診斷再改參數` / `報告讓開發者知道問題在哪，也知道下一步該做什麼`
- [ ] Slide 1 含一張 5 列 × 3 欄原生表格（表頭：類別 / 可以調什麼 / 作用），資料列為搜尋策略、運算資源、品質與停止、量測設定
- [ ] Slide 2 含一張 5 列 × 3 欄原生表格（表頭：觀察到的情況 / 代表的問題 / 下一輪要測的方向），資料列 4 筆
- [ ] Slide 2 含 7 個編號步驟（原 ASCII 流程圖），文字與來源檔一致
- [ ] Slide 3 含報告 5 段結構的編號條列，以及決策 gate 的條列版（品質不退化 → 多次結果穩定 → 獨立測試成立）
- [ ] 產出檔中**不存在任何 ASCII art**（無 `↓`、`→` 拼成的方框圖、無 code block 圍欄符號）
- [ ] 每張投影片的 notes 皆包含三塊且依序：`[版面配置]`、`[講稿]`、`[轉場]`（Slide 3 為 `[收尾]`）
- [ ] `926 億種` 出現在 Slide 1 正文；`45%` 與「不可直接採用」的語意出現在 Slide 3 正文
- [ ] 投影片正文的每一句話都能在來源檔對應到出處（人工逐頁比對，無自創內容）
- [ ] 產出檔未套用任何佈景主題、未設定任何字型顏色 / 字型家族 / 填色 / 背景（僅設定字級以避免溢出）
- [ ] 所有文字框內文字不溢出投影片邊界（16:9 預設尺寸下目視確認）
- [ ] 產生器腳本可重複執行，重跑結果一致（覆寫產出檔不報錯）

## Module Interactions

這是文件產生任務，不涉及 frontend / backend / DB。實際涉及的元件：

- **輸入**：[AI-CPLEX-Tuning-20min.md](../../ppt/auto%20tuning/AI-CPLEX-Tuning-20min.md)（唯讀，NEVER 修改）
- **產生器**：`ppt/auto tuning/build_deck.py`，使用 python-pptx 1.0.2（環境已確認可用，Python 3.10.11）
- **輸出**：`ppt/auto tuning/AI-CPLEX-Tuning-3slides.pptx`
- **規格**：本檔，位於 AI-Modeling repo 的 `specs/`（AI-Modeling 是 git repo；`ppt/` 上層不是，故規格與產物分開放）
- **第三方**：無網路依賴、無外部 API

## API Design

沒有 HTTP endpoint。此處定義產生器的內部介面。

### 產生器函式簽名

```python
def build_slide_1(prs: Presentation) -> None: ...
def build_slide_2(prs: Presentation) -> None: ...
def build_slide_3(prs: Presentation) -> None: ...

def add_bullets(slide, left, top, width, height, items: list[Bullet]) -> None: ...
def add_table(slide, left, top, width, height, header: list[str], rows: list[list[str]]) -> None: ...
def set_notes(slide, layout_hint: str, script: str, transition: str) -> None: ...
```

### 版面座標約定（16:9，13.333 × 7.5 吋）

| 區塊 | left | top | width | height |
|---|---|---|---|---|
| 標題 | 0.5" | 0.3" | 12.33" | 1.0" |
| 左欄正文 | 0.5" | 1.5" | 6.2" | 5.5" |
| 右欄（表格 / 次要條列） | 6.9" | 1.5" | 5.9" | 5.5" |

單欄頁（若某頁不需要右欄）正文寬度改用 12.33"。

## Data Model

投影片內容以 Python dataclass 表示，內容與排版分離：

```python
@dataclass
class Bullet:
    text: str
    level: int = 0          # 0 = 第一層，1 = 子層

@dataclass
class Table:
    header: list[str]
    rows: list[list[str]]

@dataclass
class Notes:
    layout_hint: str        # 來源檔「建議主視覺」改寫
    script: str             # 來源檔「講稿敘述」原文
    transition: str         # 來源檔「轉場」/「收尾句」原文

@dataclass
class Slide:
    title: str
    left_body: list[Bullet]
    right_body: list[Bullet] | None
    table: Table | None
    notes: Notes
```

### 三頁內容對照表（來源 → 產出）

| 頁 | 左欄 | 右欄 | Notes |
|---|---|---|---|
| 1 | 前提、Tuning 範圍、AI 參考的資訊（3 子項）、PH3 產出（3 子項）、926 億種記憶點 | 「可以調什麼」表格 + 輸入→處理→輸出的 3 步編號條列 | 主視覺改寫 + 講稿 + 轉場 |
| 2 | 七步驟編號條列、AI 寫什麼、AI 做什麼 | 四類瓶頸表格 | 主視覺（含顏色語意）改寫 + 講稿 + 轉場 |
| 3 | 報告五段結構編號條列 | 判斷順序、45% 案例、TuningHistory、決策 gate 條列 | 主視覺改寫 + 講稿 + 收尾句 |

## Edge Cases & Error Handling

- **ASCII art 轉換失真**：來源檔的方框圖靠字元排列表達流程，換字型必爛版。一律改寫成編號條列 + 文字箭頭（`→`），並在 notes 的 `[版面配置]` 註明原本想表達的圖形關係，讓使用者自己畫。
- **markdown code block 被誤當正文**：Slide 3 的報告模板是 ```` ```markdown ```` 區塊，內容是投影片要講的五段結構。轉成編號條列，圍欄符號與 `## R<N>` 標記不進投影片。
- **內容溢出**：Slide 1 左欄條列最長。若目視發現溢出，先降字級（最低 14pt）；仍溢出則把子層條列合併成一行，NEVER 為了塞下而刪掉來源檔的資訊點。
- **中文字型 fallback**：不指定字型家族，PowerPoint 在 Windows 會以系統預設中文字型 render。這是刻意的——指定字型就等於做設計，且會與使用者的目標範本衝突。
- **表格列高自動撐開**：python-pptx 設定的 row height 是最小值，文字多時 PowerPoint 會自己撐高並可能超出頁面。表格欄寬需給足（第 2 欄最寬），驗收時目視確認。
- **產出檔已存在**：直接覆寫。產出檔為衍生物，不進版控考量。
- **重跑不一致**：內容全部硬編在腳本裡，不做 markdown parsing，避免來源檔格式微調造成輸出漂移。

## Non-Functional Requirements

- **設計中立性**（本案最重要的約束）：不呼叫任何設定 `.color`、`.fill`、`.font.name`、主題、母片的 API。唯一允許的格式設定是 `font.size`（避免溢出）與 `bold`（僅用於表頭與條列的粗體標籤，因來源檔本身就用 `**粗體**` 標示）。
- **可貼上性**：表格用 `shapes.add_table` 產生原生表格；條列用 textbox + paragraph level，不用圖片、不用 SmartArt、不用群組物件。
- **內容保真**：投影片文字為來源檔的逐字或最小改寫（僅刪除 meta 標記、把 ASCII 改寫成條列）。NEVER 新增來源檔沒有的論述或數據。
- **可重現**：`python "ppt/auto tuning/build_deck.py"` 單一指令重生，無互動輸入。
- **相依**：僅 python-pptx（已裝）。不引入新套件。

## Open Questions

approve 時（2026-08-11）使用者回覆「都可」，兩題皆採用規格原提案，無待決事項。

- [x] 表格形式 → 採用原生 PowerPoint 表格（管線符號文字貼進 PowerPoint 不會變成表格，不符「貼進別的 PPT」的目的）
- [x] 產出檔名 → `AI-CPLEX-Tuning-3slides.pptx`（避免與來源檔名的 `20min` 混淆）

## Implementation Plan

### Stub 階段（先做）

- [ ] 建 `ppt/auto tuning/build_deck.py`：dataclass 定義、三個 `build_slide_N` 函式簽名、`add_bullets` / `add_table` / `set_notes` helper 簽名，body 為 `# TODO` + `pass`
- [ ] 三張投影片先只填正確標題，正文放 `TODO: <對應規格段落>`，表格放 placeholder 表頭
- [ ] 跑一次 `python "ppt/auto tuning/build_deck.py"`，確認能產出可開啟的 3 頁 `.pptx`（結構驗證點：張數、標題、notes 欄位連得起來）

### 逐頁實作

- [ ] `add_bullets` / `add_table` / `set_notes` helper 實作（含字級與座標）
- [ ] Slide 1 內容填入 + 目視檢查溢出
- [ ] Slide 2 內容填入 + 目視檢查溢出
- [ ] Slide 3 內容填入 + 目視檢查溢出
- [ ] 逐頁對照來源檔驗收內容保真（無自創、無遺漏資訊點）
- [ ] 對照 Acceptance Criteria 逐條打勾

## References

- 來源文件：[AI-CPLEX-Tuning-20min.md](../../ppt/auto%20tuning/AI-CPLEX-Tuning-20min.md)
- PH3 規範：[solver-tuning-guide.md](../.claude/rules/Ph3_Tuning/solver-tuning-guide.md)
- 既有簡報產物參考：`ppt/deliverables/`、`ppt/ClaudeModelFramework.pptx`
- 受影響的既有規格：無（`specs/` 原為空）
