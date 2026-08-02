---
title: AI 框架技術文件——四 Agent 分工 + /loop 15 分鐘調度生成
status: done
created: 2026-07-07
updated: 2026-07-21
modules: [docs, automation, agents]
superseded_by: 2026-08-01-optimfoundation-dual-config.md, 2026-08-01-optimfoundation-runner-symmetry.md, 2026-08-01-optim-docs-and-projects-migration.md
---

# Tech-Doc Story Loop — 四 Agent 分工生成兩主軸技術文件

## Summary

由 AI 自動盤點 AI-Modeling 與 OptimFoundation 兩主軸的現有框架文件與 code，寫成**題目旅程式**的故事型技術文件。採**四 Agent 分工**：三個內容 Agent 平行產出主題稿（開發流程 / Foundation 連結 / AI 能力），一個整合 Agent 把主題稿融成統一敘事的章節。主會話當 orchestrator，以 `/loop` 每 15 分鐘一輪調度：派工 → 驗收 → 整合 → 修訂，coverage 全綠且終審通過後自動停止。

## Motivation / Why

- 框架知識散落多處（`interactive/`、`automated/`、`ROADMAP.md`、`tutorial/why-optimfoundation.md`、兩個 HospitalRostering 專案），準備報告需要一份收斂後的敘事文件，手動整理耗時
- 既有文件偏「規範/參考」體，缺一份**讓讀者跟著走一遍**的故事：能想像「基本數學模型的開發流程長什麼樣、每一步會撞到哪些痛點、框架怎麼把痛點接走」
- 內容橫跨三個知識域（流程 / 框架工程 / AI 能力），單線逐章寫容易顧此失彼——分工讓各域由專責 Agent 深挖，再由整合 Agent 統一故事聲音
- 產出放 `ppt/`，同時是投影片的素材庫（使用者後續整合）

## Scope

### In Scope

- **來源盤點（唯讀）**：AI-Modeling（`interactive/` 全部、`automated/`、`ROADMAP.md`、`README.md`、`CPLEX_API_REFERENCE.md`、`tutorial/why-optimfoundation.md`、`Projects/HospitalRostering_Generator|_Manual`）＋ OptimFoundation repo（工程內幕佐證）
- **四 Agent 分工**（詳見 Module Interactions）：
  - **Agent W（Workflow）**：寫基本數學模型的**開發流程**——從讀題到求解的完整旅程與每步痛點
  - **Agent F（Foundation-Link)**：整理 **OptimFoundation 與開發流程的連結**，分三塊：資料框架（Sets/Parameters/變數身份證）、開發（Pool API/六資料夾/Generator vs Manual）、實驗 tuning（Experiment API/Trial.Capture/SolveMetrics）
  - **Agent C（AI-Capabilities）**：整理 **AI 可以做到哪些事**——interactive 協作下 AI 的角色、automated 16-stage 全自動能力、防幻覺護欄、能力現況 ✅/🟡/❌
  - **Agent I（Integrator）**：把三份主題稿**整合**成題目旅程式章節——統一敘事聲音、去重、術語一致、補 References，維護 `index.md` 與最終章節檔
- **故事形式**：題目旅程式——主角是一位工程師與一道排班題；每章是旅程一站；有場景、有衝突（痛點）、有解決（框架設計）；**故事性由 Agent I 統一把關**
- **序章特別要求**（Agent W 主稿）：完整走一遍「沒有框架的基本數學模型開發流程」——讀題 → 定義 Sets/Parameters/Variables → 列式 → 線性化 → 手寫 CPLEX code → debug 不可行 → 調參——每一步具象標出痛點（樣板地獄、移項抄錯號、infeasible 難查、調參靠翻 log），讓讀者「想像得到」
- **loop 調度協定**：15 分鐘一輪；orchestrator 每輪依 `coverage.md` 派工/驗收/推進；全綠＋終審通過 → 自動停

### Out of Scope

- 不生投影片（文件是素材，deck 由使用者後續整合）
- 不修改 AI-Modeling / OptimFoundation repo 任何內容（純唯讀來源；本 spec 檔除外）
- 不寫新程式、不跑 build、不驗證求解結果
- 不做英文版

## User Stories / Use Cases

1. As repo owner，我要 AI 分工自動把散落的框架知識整理成一份故事型技術文件，so that 報告與投影片有單一素材來源。
2. As 讀者工程師，我要跟著一道題從自然語言走到求解，so that 我能想像整個開發流程與每步的痛點，理解框架為何長成這樣。
3. As repo owner，我要 loop 每 15 分鐘自動調度各 Agent 並在覆蓋全綠時自動停，so that 我不用人在迴路盯著。

## Acceptance Criteria

- [ ] `ppt/tech-doc/coverage.md` 存在：兩層狀態——**主題稿**（W/F/C 各一份，⬜ 未派 / 🔄 進行中 / 🟡 已交稿 / ✅ 驗收過）與**章節**（00–06，⬜/🟡/✅）＋迭代 log（每輪一行）
- [ ] 三份主題稿齊備：`_drafts/workflow.md`（W）、`_drafts/foundation-link.md`（F，含資料框架/開發/實驗 tuning 三塊）、`_drafts/ai-capabilities.md`（C）
- [ ] 序章讓讀者能想像「基本數學模型開發流程」：流程每一步都有具體痛點場景（至少含：移項抄錯號導致 infeasible、變數樣板重複、調參翻 log 不可累積）
- [ ] 最終章節以故事線推進（場景開場、衝突、解決），**單一敘事聲音**——讀不出「三個人寫的」拼接感；技術細節內嵌在敘事中
- [ ] 兩主軸內容全覆蓋：OptimFoundation 五設計、Phase 1 四階段（1a–1d）、Phase 2（六資料夾、Pool API、解驗證協定）、Phase 3（正確性 gate、ExperimentRunner）、automated 16-stage＋防幻覺護欄、Generator/Manual 雙架構、scope 矩陣與現況
- [ ] 每章末有 References（相對路徑指回來源檔）；技術敘述全部可回溯，NEVER 為故事發明不存在的 API/機制
- [ ] coverage 全 ✅ 且終審輪通過後，loop 不再排下一輪並回報完成

## Module Interactions

無 frontend/backend/DB——這是 multi-agent docs pipeline：

| 角色 | 執行體 | 輸入 | 輸出 |
| --- | --- | --- | --- |
| Orchestrator | 主會話（/loop 每 15 分鐘喚醒） | `coverage.md` | 派工、驗收、更新狀態 |
| Agent W | subagent（背景） | `interactive/`、`tutorial/ppt/`、HospitalRostering 專案 | `_drafts/workflow.md` |
| Agent F | subagent（背景） | `why-optimfoundation.md`、`tech-report/`、`CPLEX_API_REFERENCE.md`、OptimFoundation repo | `_drafts/foundation-link.md` |
| Agent C | subagent（背景） | `automated/`、`ROADMAP.md`、`AGENTS.md`、`interactive/README.md` | `_drafts/ai-capabilities.md` |
| Agent I | subagent（每次整合一批） | 三份主題稿 + 章節模板 | `00–06` 章節檔、`index.md` |

- W/F/C 互不依賴 → **平行派出**（背景執行）；I 依賴三者交稿後才開工
- Orchestrator 不自己寫內容，只派工/驗收/推進狀態——單一狀態源是 `coverage.md`

## API Design（→ 分工與調度協定）

**交稿介面（主題稿格式，W/F/C 遵守）**：markdown，開頭 frontmatter 標 `agent`、`sources`（讀過的檔）、`status: submitted`；內文依「可直接被引用的節」組織，每節末標來源相對路徑——讓 Agent I 可溯源取材。

**每輪 orchestrator 流程**：

1. 讀 `coverage.md` 找當前階段：**盤點 → 派工（W/F/C 平行）→ 驗收主題稿 → 派 I 整合（逐批章節）→ 修訂 → 終審**
2. 背景 Agent 完成會自動通知；15 分鐘 wakeup 是 fallback（Agent 卡住/超時時介入：檢查產出、必要時重派）
3. 驗收標準：主題稿 frontmatter 齊、來源可溯、無發明 API；不合格 → 用 SendMessage 給該 Agent 退件重修（保留 context）
4. 更新 `coverage.md`（狀態 + log 一行）；未全綠 → 排下一輪；全綠且終審 pass → 停止並回報

**輪次規劃（預估）**：

| 輪 | 動作 |
| --- | --- |
| 1 | 盤點 → 定稿 coverage → **平行派出 W/F/C（背景）** |
| 2–3 | 驗收三份主題稿（先到先驗；退件重修在此消化） |
| 4–8 | 派 I 整合：每輪 1–2 章（00–06） |
| 9 | 修訂輪：I 跨章一致性、故事線連貫、術語統一 |
| 10 | 終審輪：orchestrator 對 Acceptance Criteria 逐條自審 → 停 |

## Data Model（→ 檔案結構）

```text
ppt/tech-doc/
├── index.md            ← 目錄 + 故事總覽（旅程地圖）【Agent I】
├── coverage.md         ← 狀態檔：主題稿×狀態 + 章節×狀態 + 迭代 log【Orchestrator】
├── _drafts/            ← 主題稿工作區（整合後保留供追溯）
│   ├── workflow.md         【Agent W】開發流程 + 痛點全景
│   ├── foundation-link.md  【Agent F】資料框架 / 開發 / 實驗 tuning 三塊連結
│   └── ai-capabilities.md  【Agent C】AI 能做到哪些事 + 現況
├── 00-prologue.md      ← 序章：一道排班題的誕生 + 沒有框架的開發之旅（痛點全景）
├── 01-foundation.md    ← 基座登場：OptimFoundation 五設計逐一接走序章的痛點
├── 02-modeling.md      ← 旅程站一：Phase 1 建模（1a–1d 四階段降維 → AML）
├── 03-coding.md        ← 旅程站二：Phase 2 轉譯（六資料夾、Pool API 不移項、Generator vs Manual）
├── 04-tuning.md        ← 旅程站三：Phase 3 調校（正確性 gate → ExperimentRunner）
├── 05-automated.md     ← 支線：量產線（16-stage pipeline、防幻覺護欄）
└── 06-landscape.md     ← 終章：scope 組合矩陣、誠實現況、roadmap
```

主題稿 → 章節的映射：`workflow.md` 主餵 00/02/03/04；`foundation-link.md` 主餵 01/03/04；`ai-capabilities.md` 主餵 05/06；交疊處由 Agent I 裁決去重。

章節結構模板（每章）：開場場景（旅程中的處境）→ 衝突（痛點具象化）→ 解決（框架設計怎麼接）→ 技術深潛（可回溯來源）→ 站點小結 + References。

## Edge Cases & Error Handling

- **背景 Agent 卡住/超時**：15 分鐘 wakeup 當看門狗——檢查 `_drafts/` 產出與 coverage 標記，無進展 → SendMessage 催收或重派新 Agent
- **主題稿品質不合格**（發明 API、無來源標註）：退件重修（SendMessage 保留該 Agent context），最多兩次；仍不過 → orchestrator 降級自寫該稿
- **三稿內容衝突**（同一機制描述不一致）：Agent I 以來源檔原文為準回查；來源本身衝突（舊 spec vs 現行）→ 以 `ROADMAP.md` 收斂結果為準
- **故事性 vs 技術正確性衝突**：正確性優先、敘事讓步；不可為戲劇效果發明 API 或誇大現況（現況表照 ROADMAP §5 的 ✅/🟡/❌ 誠實寫）
- **context 膨脹**：orchestrator 每輪只讀 coverage.md ＋當輪需要的檔；內容細讀下放給各 Agent
- **使用者中途改方向**：coverage.md 是單一狀態源，人工改它即可改道，下一輪自動跟隨

## Non-Functional Requirements

- **單輪工作量**：orchestrator 每輪 15 分鐘內可完成（派工/驗收/推進；重內容產出都在背景 Agent）
- **可讀性**：故事性優先於完整性——寧可少列三個 API，不可斷掉敘事；code 引用只節錄關鍵行
- **語言**：繁體中文，technical terms 保留英文
- **可追溯**：主題稿每節、章節每章都標來源相對路徑；引用數據（如反射快取 ~100ns→~10ns）必須來自來源檔原文
- **Observability**：coverage.md 迭代 log 每輪一行（時間、階段、派了誰/收了什麼），人隨時可看進度

## Open Questions

- [ ] 章節數（現規劃 7 章）與主題稿映射，第一輪盤點後若需增減，是否直接調整不再請示？（預設：可，記錄在 coverage.md log）

## Implementation Plan

### Stub 階段（approve 後先做）

- [ ] 建 `ppt/tech-doc/` 骨架：`index.md`（旅程地圖 TODO）、`coverage.md`（兩層狀態初版：主題稿全 ⬜、章節全 ⬜、log 空）、`_drafts/` 三個主題稿 stub（frontmatter + 節綱要 + TODO）、7 個章節 stub（標題＋故事線一句話＋主餵來源＋TODO）
- [ ] 檢查骨架內部連結與相對路徑可達（等同 build check）

### 逐層實作（= loop 輪次）

- [ ] 啟動 `/loop 15m`（調度 prompt 固定：依 coverage.md 執行當前階段）
- [ ] Round 1 盤點 + 平行派出 W/F/C
- [ ] Round 2–3 驗收主題稿（⬜→🟡→✅）
- [ ] Round 4–8 Agent I 逐批整合章節（⬜→🟡）
- [ ] Round 9 修訂（🟡→✅）
- [ ] Round 10 終審對照 Acceptance Criteria → 全過 → loop 停止、回報

## References

- 敘事素材源：`tutorial/why-optimfoundation.md`、`tutorial/tech-report/`、`tutorial/ppt/`
- 框架規範源：`interactive/README.md`、`phase-1-model-design.md`、`phase-2-coding.md`、`phase-3-tuning.md`、`linearization-patterns.md`、`automated/`
- 全景源：`ROADMAP.md`（§3 決策轉折、§5 現況）
- 對照專案：`Projects/HospitalRostering_Generator`、`Projects/HospitalRostering_Manual`
- 受影響的既有規格：無（純新增文件；`2026-06-22-dual-architecture-tutorial.md` 已於 2026-07-15 刪除，本規格不再與其相關）

---

## 完工紀錄（2026-07-21 補記）

Implementation Plan 已 100% 執行完畢——三份主題稿與 7 個章節全部完成、終審檢核已勾完。完工證據見 sibling repo 的 `../ppt/tech-doc/coverage.md`（跨 repo 路徑，不在 AI-Modeling 內）。
