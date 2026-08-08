# AI Modeling — `.claude` 文件索引

這個資料夾是 AI agent 的本機知識庫。閱讀順序固定為：**入口規則 → 選定工作流 → 當前 phase 規則 → API／參考資料**。

`Template/` 與 `Projects/<Project>/` 不建立或保留 `CLAUDE.md`；它們不應有獨立 AI 規則。所有問題一律回到本資料夾查閱。

## 快速入口

| 需求 | 請讀 | 說明 |
| --- | --- | --- |
| 全域規則與路線選擇 | [`rules/AGENTS.md`](rules/AGENTS.md) | 唯一天條來源；先讀這份。 |
| 整體任務與階段契約 | [`rules/MILP DevPipeline/README.md`](rules/MILP%20DevPipeline/README.md) | 三階段 I/O、交接物、`status.json` schema；pipeline 層唯一權威。 |
| 三階段開發流程（唯一路線） | [`workflows/interactive/README.md`](workflows/interactive/README.md) | Modeling → Coding → Tuning 的 phase gate。 |
| 專案開發唯一指引 | [`rules/Ph2_Coding/optimfoundation-api-guide.md`](rules/Ph2_Coding/optimfoundation-api-guide.md) | 專案結構、命名、注入方式與允許 API 的唯一權威。 |
| 驗收案例 | [`evals/`](evals/) | 可觀察的行為判準。 |

## 資料夾責任

| 資料夾 | 內容 | 維護原則 |
| --- | --- | --- |
| `rules/` | 穩定、可跨工作流套用的規則 | 同一條硬規則只在權威文件定義，其餘文件以連結引用。 |
| `workflows/` | 三階段流程的步驟與 prompt | 只有 `interactive/` 有效；`automated/` 已停用，僅供歷史查閱，NEVER 引用。 |
| `skills/` | Claude skill 入口與交付前 checklist | `SKILL.md` 必須保留 YAML front matter。 |
| `commands/` | 可由使用者觸發的審查指令 | 每個 phase 都提供一致的 `review` 交付格式。 |
| `agents/` | 可委派的角色定義 | 僅放角色、輸入／輸出與邊界；不重複流程規則。 |
| `reference/` | API、架構與基準資料 | 不在此放可執行流程規則。 |
| `evals/` | 評估案例 | 保留 YAML front matter 與可觀察 PASS/FAIL 條件。 |
| `hooks/` | 工具事件 hook | 腳本與文件規則分離。 |

## 文件標準

- 編碼一律 UTF-8，換行一律 LF，檔案末尾保留一個換行。
- 每份 Markdown 僅有一個 `#` 標題；標題採「範圍 — 主題」或「Phase N · 主題」。
- 一般規則文件使用 Markdown 標題；只有既有 agent prompt 可保留結構化區塊。新增文件不再引入第二種格式。
- `SKILL.md` 與 `evals/*.md` 使用 YAML front matter；其他文件除非需要機器讀取 metadata，否則不加。
- 使用相對連結；不可連到機器專屬路徑或不存在的模板資料夾。
- 文件改動後應檢查內部連結，並避免把相同規則複製到多個位置。

## 命名標準

- 資料夾與一般文件：小寫 kebab-case；既有 phase 資料夾因相容性保留 `Ph1_Modeling`、`Ph2_Coding`、`Ph3_Tuning`。
- `SKILL.md`、`AGENTS.md`、`README.md`、`BASELINE.md` 為工具慣例檔名，保留大寫。
- 已停用的 `workflows/automated/` 維持原樣不再維護（含 `NN_Name.md` stage prompt）；新增文件 NEVER 沿用它的命名或結構。

## 維護檢查

1. 新增規則前先確認是否已有權威來源；有則補連結，不複製內容。
2. 新增或搬移文件後，更新本索引及受影響的相對連結。
3. 把可驗證的行為新增到 `evals/`，不要只寫抽象原則。
