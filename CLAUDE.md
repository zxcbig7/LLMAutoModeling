# AI Modeling — Claude Code 入口（router）

> 本 repo 的操作規範**單一來源**在 [`.claude/skills/`](.claude/skills/)：總流程一檔 + 三個 phase 各一檔。
> 本檔只做導引（router）；**天條與細則一律以 .claude/skills/AGENTS.md 為準，不在此重複**。

`Template/` 與 `Projects/<Project>/` 不建立或保留 `CLAUDE.md`。任何專案的 AI 指引一律從 [`.claude/README.md`](.claude/README.md) 開始，專案實作以 [`.claude/skills/coding/optimfoundation-api-guide.md`](.claude/skills/coding/optimfoundation-api-guide.md) 為唯一開發指導原則。

## 這是什麼

自然語言最佳化題目 → 可求解的 OptimFoundation CPLEX C# 專案。

**唯一路線是三階段 phase gate**，依序推進、每階段之間有 gate，NEVER 跳階或走免 gate 的全自動路線：

| 階段 | skill | 唯一規範檔 | 產物 |
| --- | --- | --- | --- |
| Phase 1 Modeling | [`modeling`](.claude/skills/modeling/SKILL.md) | [`model-design-guide.md`](.claude/skills/modeling/model-design-guide.md) | `Model/<Project>_Model.md`，停在使用者確認 |
| Phase 2 Coding | [`coding`](.claude/skills/coding/SKILL.md) | [`optimfoundation-api-guide.md`](.claude/skills/coding/optimfoundation-api-guide.md) | 八資料夾專案，build 綠 + 解已驗證 |
| Phase 3 Tuning | [`tuning`](.claude/skills/tuning/SKILL.md) | [`solver-tuning-guide.md`](.claude/skills/tuning/solver-tuning-guide.md) | promotion 後的 baseline + `TuningHistory.md`（使用者提出才做） |

**先讀 [`.claude/skills/AGENTS.md`](.claude/skills/AGENTS.md)**（天條 + 三階段 I/O 契約 + 交接物 + `status.json` schema），再依當前 phase 讀上表對應的那一份。一階段一檔，讀那一份就夠；Phase 2 的 API 簽名權威在其 §9。文件全圖見 [`.claude/README.md`](.claude/README.md)。

## 換機器設置（clone 後唯一要做的事）

DLL 不進版控（商用 CPLEX + 建置產物）。clone 後照 [`dlls/README.md`](dlls/README.md) 把 6 個 DLL 就位：CPLEX 兩顆從本機安裝複製，OptimFoundation 四顆先建 sibling `../OptimFoundation/` 再複製建置輸出。

## 天條

全部天條（含數值保真、API 白名單、框架唯讀、相對路徑、DLL 引用規則）唯一權威在 [`.claude/skills/AGENTS.md`](.claude/skills/AGENTS.md#天條全流程通用唯一權威在本檔其他文件只引用不重複)。動手前先讀。
