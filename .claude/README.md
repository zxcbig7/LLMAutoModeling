# AI Modeling — `.claude` 文件索引

這個資料夾是 AI agent 的本機知識庫。閱讀順序固定為：**總流程 → 當前 phase 的唯一規範檔**。

`Template/` 與 `Projects/<Project>/` 不建立或保留 `CLAUDE.md`；它們不應有獨立 AI 規則。所有問題一律回到本資料夾查閱。

## 快速入口（四份檔就是全部）

| 需求 | 請讀 |
| --- | --- |
| **總流程**：天條、三階段契約、正式交付物、`status.json` schema | [`skills/AGENTS.md`](skills/AGENTS.md) |
| **Phase 1 建模**：四階段降維、Model.md 契約、線性化 pattern、multi-agent | [`skills/modeling/model-design-guide.md`](skills/modeling/model-design-guide.md) |
| **Phase 2 轉譯**：專案結構、資料/變數/模型層、`Program.cs`、解驗證、API 簽名 | [`skills/coding/optimfoundation-api-guide.md`](skills/coding/optimfoundation-api-guide.md) |
| **Phase 3 調校**：進場 gate、旋鈕全表、promotion 閉環、multi-agent | [`skills/tuning/solver-tuning-guide.md`](skills/tuning/solver-tuning-guide.md) |

**一階段一份權威 guide。** Phase 2 另有兩份非權威輔助檔：交付前機械驗收用 [`skills/coding/checklist.md`](skills/coding/checklist.md)、派工用 [`skills/coding/agent-workflow-prompts.md`](skills/coding/agent-workflow-prompts.md)。

## 資料夾責任

| 資料夾 | 內容 | 維護原則 |
| --- | --- | --- |
| `skills/` | **唯一的規則與執行來源**：總流程 1 檔 + 三個 phase skills；三個 phase 的 orchestrator 入口 + 交付前 checklist | 同一條硬規則只在權威文件定義，其餘文件以連結引用；NEVER 新增第二份談同一階段的規則檔；`SKILL.md` 必須保留 YAML front matter，只做調度與 gate，NEVER 複製規則 |
| `commands/` | 可由使用者觸發的審查指令 | 每個 phase 提供一致的 `review` 交付格式 |
| `reference/` | CodeMap 等架構資料 | 不在此放可執行流程規則 |
| `hooks/` | 工具事件 hook | 腳本與文件規則分離 |

## 文件標準

- 編碼一律 UTF-8，換行一律 LF，檔案末尾保留一個換行。
- 每份 Markdown 僅有一個 `#` 標題；規範檔用 `## §N` 編號章節，方便派工時只讀某一節。
- `SKILL.md` 使用 YAML front matter；其他文件除非需要機器讀取 metadata，否則不加。
- 使用相對連結；不可連到機器專屬路徑（`C:/Users/...`）或不存在的資料夾。
- 文件改動後檢查內部連結，並避免把相同規則複製到多個位置。

## 命名標準

- 資料夾與一般文件：小寫 kebab-case；phase 資料夾保留 `Ph1_Modeling`、`Ph2_Coding`、`Ph3_Tuning`。
- `SKILL.md`、`AGENTS.md`、`README.md` 為工具慣例檔名，保留大寫。
- 各 phase 的唯一規範檔命名為 `<主題>-guide.md`：`model-design-guide` / `optimfoundation-api-guide` / `solver-tuning-guide`。

## 維護檢查

1. 新增規則前先確認是否已有權威來源；有則補連結、或直接寫進該 phase 的規範檔，**不新開檔案**。
2. 搬移或刪除文件後，全 repo grep 一次舊檔名，把斷連結修掉。
3. `skills/` 的 `SKILL.md` 與按需讀取的同層文件都會影響 context，篇幅即成本——寫進去之前先問「這條規則有沒有已經在別處講過」。
