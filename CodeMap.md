---
title: "CodeMap — Phase 3 Tuning 架構研究 — 2026-09-19"
toc:
  depth_from: 1
  depth_to: 3
  ordered: false
---

[TOC]

## 審查範圍 / Scope

本圖供現有 Phase 3 Tuning 文件架構與防護流程研究使用。Base 為工作目錄現況，非 branch diff；File Index 的 +行 / -行不適用。此次不執行 solver，不修改既有 skill、command 或專案實作。文件標題視為 Symbol；行號指向來源文件。

## File Index

| 檔案 | +行 | -行 | 語言 | 關鍵 Symbol |
| --- | --- | --- | --- | --- |
| `AGENTS.md` | 不適用 | 不適用 | Markdown | root router、三階段入口 |
| `README.md` | 不適用 | 不適用 | Markdown | 框架定位、整體架構 |
| `.claude/README.md` | 不適用 | 不適用 | Markdown | 文件索引、文件標準 |
| `.claude/skills/AGENTS.md` | 不適用 | 不適用 | Markdown | 通用天條、Phase 2/3 出口契約、正式交付物 |
| `.claude/skills/tuning/SKILL.md` | 不適用 | 不適用 | Markdown | S0–S5、S2.5、Fatal |
| `.claude/skills/tuning/solver-tuning-guide.md` | 不適用 | 不適用 | Markdown | §0–§9、T0–T7、附錄 |
| `.claude/skills/tuning/cplex-parameter-reference.md` | 不適用 | 不適用 | Markdown | 旋鈕分類、策略參數查表 |
| `.claude/skills/tuning/checklist.md` | 不適用 | 不適用 | Markdown | 進場、實驗、判定、promotion、交付自檢 |
| `.claude/commands/Ph3_Tuning/review.md` | 不適用 | 不適用 | Markdown | Phase 3 review 入口 |

## Dependency Graph

本次使用相依表表達文件導引，無程式 import graph。

| 來源 | 相依目標 | 關係 |
| --- | --- | --- |
| `AGENTS.md:3` | `.claude/skills/AGENTS.md` | 全流程規範的唯一權威 |
| `AGENTS.md:18` | tuning `SKILL.md`、`solver-tuning-guide.md` | Phase 3 調度與規範 |
| `.claude/skills/tuning/SKILL.md:14` | `solver-tuning-guide.md` | S0–S5 引用規範，不複製規則 |
| `.claude/skills/tuning/solver-tuning-guide.md:1420` | `cplex-parameter-reference.md` | 旋鈕查表移至獨立參考檔 |
| `.claude/skills/tuning/checklist.md:1` | tuning 規範與調度 | 交付前驗收 |
| `.claude/commands/Ph3_Tuning/review.md:3` | skills `AGENTS.md`、`solver-tuning-guide.md` | 審查依據 |

## Symbol Index

### `.claude/skills/AGENTS.md`

| Symbol | 種類 | 行號 | 說明 |
| --- | --- | --- | --- |
| 天條 | 規範 | 31 | 全流程共同限制 |
| Phase 2 出口契約 | 契約 | 118 | Phase 3 上游交接 |
| Phase 3 出口契約 | 契約 | 159 | Tuning 交付條件 |
| 正式交付物 | 規範 | 178 | 可寫文件與狀態 |
| 執行層 | 調度 | 237 | 單線 / multi-agent 導引 |

### `.claude/skills/tuning/SKILL.md`

| Symbol | 種類 | 行號 | 說明 |
| --- | --- | --- | --- |
| S0 | gate | 54 | 進場與契約凍結 |
| S1 | sizing | 68 | 環境定版 |
| S2 | calibration | 74 | R0 校準 |
| S2.5 | baseline | 86 | CPLEX 內建 tune 基準 |
| S3 | experiment | 90 | 策略輪循環 |
| S4 | validation | 107 | Hold-out |
| S5 | promotion | 111 | Promotion 閉環 |
| Fatal | 規範 | 135 | 階段禁止事項 |

### `.claude/skills/tuning/solver-tuning-guide.md`

| Symbol | 種類 | 行號 | 說明 |
| --- | --- | --- | --- |
| §0.0 / §0.0.1 | gate | 86 / 98 | 正確性與進場情境 |
| §0.1 / §0.1.1 | 契約 | 121 / 156 | 可寫範圍與結果不變式 |
| §2.1 / §2.2 | diagnosis | 330 / 364 | 收斂剖面與候選旋鈕 |
| §2.3 / §2.4 | sizing | 437 / 471 | 環境定版與規模預警 |
| §3.0 / §3.0.1 | evidence gate | 497 / 574 | R0 與每輪證據 → 目標 → 設定 |
| §3.3.2 | 去重 | 653 | 跨輪有效 config 去重 |
| §3.6 | CPLEX tune | 696 | 內建 tuning tool |
| §4.6 / §5.3 | validation | 783 / 848 | Hold-out 與 production 重跑 |
| §6.2 / §6.2.2 | audit | 925 / 983 | 實驗前預測與機械事實區塊 |
| §7.1 / §7.2 | budget | 1036 / 1054 | 停止條件與總預算 |
| §8.1 | context | 1089 | Context 鐵則 |
| T0–T7 / §8.5 | orchestration | 1073 / 1362 | 分工與每輪完成条件 |
| §9 | anti-pattern | 1377 | 常見錯誤與反模式 |

### 其他文件

| 檔案 | Symbol | 行號 | 說明 |
| --- | --- | --- | --- |
| `AGENTS.md` | 三階段 router | 12 | phase gate 與規範來源 |
| `README.md` | 整體架構 | 25 | 框架概觀 |
| `.claude/README.md` | 快速入口 / 文件標準 | 7 / 26 | 文件導引與維護 |
| `.claude/skills/tuning/cplex-parameter-reference.md` | 搜尋策略表 | 101 | 合法候選參數查表 |
| `.claude/skills/tuning/checklist.md` | 判定與證據 / 停損與交付 | 110 / 155 | 結論與交付驗收 |
| `.claude/commands/Ph3_Tuning/review.md` | review 輸出 | 5 | Blocker / Finding / Next experiment / Verified |

## Review Scope Notes

- 先查文件大小，再以標題、行號定位並分段閱讀。索引 pattern 為 `^#{1,4} `、`S[0-5]`、`T[0-7]`、`8\.1`、`2\.5`、`零成本`。
- 搜尋限定已列出的 Markdown 文件；排除 `.git`、`bin`、`obj`、`Models`、原始資料、solver log 與 JSON/CSV/LP/MPS/SAV 內容。
- 本圖只記錄文件架構；現有實作是否機械執行每項規則、歷次實驗是否遵守，須有另行限定範圍的實作或 archive 證據才能判定。
- 不由模型原始檔大小、個別參數查表或本文推測瓶頸與預期加速；本圖不提供實驗結論。
