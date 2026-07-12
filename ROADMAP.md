---
title: AI Modeling — 系統規格 / 開發方向 / 目標（單一總覽）
status: living
updated: 2026-07-05
scope: 整份 AI-Modeling repo；散落各 spec 的方向與目標在此收斂
---

# AI Modeling — ROADMAP（規格 · 方向 · 目標）

> 本檔是**方向與目標的單一入口**：把原本散在多份 spec 的「願景 / 決策轉折 / 現況」串成一條線。
> 操作規範看 [`AGENTS.md`](AGENTS.md) → [`interactive/`](interactive/) / [`automated/`](automated/)；
> 仍在進行中的細部 spec 見文末「Spec 索引」。

[TOC]

## 1. North Star（從未改變）

> 自然語言最佳化題目 → **幾分鐘內**產出可 build/run 的 OptimFoundation CPLEX C# 專案；
> 中間的 **AML 數學模型當可審閱產物**，使用者在生 code 前先確認正確性。

痛點：手寫 CPLEX 要熟 `BuildCVs<>` / `AddLHS` / `CreateMinimize()` 等框架 API，又要把 NL 轉成嚴謹數學模型，門檻高、耗時數小時。目標是把這段從數小時壓到數分鐘，且不犧牲正確性（AML 中間產物是把關點）。

## 2. 架構全景（收斂後的最終形態）

兩個正交的維度：

**維度一 — 開發路線（怎麼跑流程）**

| 路線 | 位置 | 何時用 |
| --- | --- | --- |
| **interactive**（有 gate，預設） | [`interactive/`](interactive/) | 一般開發：Modeling → Coding → Tuning 三階段 phase gate，模型經確認才寫 code |
| **automated**（全自動 16-stage） | [`automated/`](automated/) | 量產 / 不需人在迴路：`00 → 14` 一路生到底 |

**維度二 — code 建構方式（怎麼組裝專案）** — 由 2026-06-22 拍板「雙 pattern 並存」

| 建構法 | 機制 | 定位 |
| --- | --- | --- |
| **Generator**（預設偏好） | `[OptVar]`/`[OptParam]` → `AutoSetsGenerator` 編譯期生成 class body；Fluent `OptModel` 注入 `Action<OptEngine>` | AI 預設走這條（樣板少、易對） |
| **Manual**（後路） | 手寫每個 Variable/Parameter class + 手寫 `XxxProblem.Execute()` 自接 `OptEngine` | 教學用、generator 不適用時的退路 |

並排教學範例：[`Projects/HospitalRostering_Generator`](Projects/HospitalRostering_Generator/)（B）/ [`Projects/HospitalRostering_Manual`](Projects/HospitalRostering_Manual/)（A），**同一份數學模型**、跑同一組 tuning。

## 3. 開發方向的演進（決策轉折 — 轉過兩次彎）

| 日期 | 來源 | 方向決策 |
| --- | --- | --- |
| 2026-05-20 | env spec（保留） | 定調**兩個 Framework**：①Claude Code 互動（先做）②Web API+RAG（後續）。16-stage pipeline、Semantic Kernel RAG 全在此 |
| 2026-05-29 | ~~cplex 開發說明書~~（已併入本檔） | 舊資料夾慣例：`Data/` / `VariablesClass/` / `Constraints/` + 手寫 `Problem.Execute()`。**已全面被取代** |
| 2026-06-21 | ~~spec-refresh~~（已併入本檔） | **第一次轉彎**：收斂成「**唯一** Fluent `OptModel` pattern」+ 每專案標配可跑 tuning（`ExperimentRunner`），淘汰手寫 Problem |
| 2026-06-21 | tuning-protocol（保留） | 加 **Stage 15 調校協定**：碰模型必先讀，**正確性 gate → 效能 tuning**，實驗回饋接 `Trial.Capture` / `SolveMetrics` |
| 2026-06-22 | dual-architecture（保留） | **第二次轉彎（部分推翻 06-21）**：不再「唯一 OptModel」，改**雙 pattern 並存**；Manual 保留為教學/後路，AI 預設偏好 Generator |

**淨結果**：路線 = interactive / automated 並存；建構法 = Generator（預設）/ Manual（後路）並存。舊的「`Data/`+`VariablesClass/`+手寫 Problem」慣例正式作廢，資料夾標準統一為 `Model/ Parameter/ Set/ Variable/ Objective/ Constraint/`，namespace `ProjectName.*`。

## 4. 系統規格摘要（automated 16-stage）

生成流程的關鍵是「**在生成中間夾驗證關卡**」：

```
00 分類 → 01 簡化 → 02 標準化 → 03 KeyInfo(JSON) → 04 AML數學模型
  → 04b AML驗證✓ → 05~13 逐檔生 C#(9檔) → 07b Dataload驗證✓
  → 14 自動修復(build失敗迴圈≤5次) → WriteRagData 回寫知識庫
```

- Stage 0 分類（LP/IP/MILP）決定 `CplexConfig` 預設：LP→emphasis0/300s、IP→emphasis1/1800s、MILP→emphasis2/3600s
- 防幻覺護欄注入每個 prompt：LHS/RHS 嚴禁移項/改號、命名規則、CoT 觸發、參數先 LINQ 存變數再傳 `AddLHS`
- Prompt 模板：[`automated/Prompts/`](automated/Prompts/)（`00`–`14`，含 `04b`/`07b` 驗證步）

**兩個 Framework 的分工**（願景）：

| | Framework 1 | Framework 2 |
| --- | --- | --- |
| 模式 | Claude Code 互動助手（逐 stage 存 `stages/` + `status.json`，可 resume） | ASP.NET Core Web API（Background Job + polling） |
| 優先序 | **現在** | 後續 |
| 實作狀態 | ✅ 已可用（靠 Prompts 手動驅動） | ❌ 只有 spec，零 `src/`（完整藍圖見 env spec） |

## 5. 目標達成現況（願景 vs 實際）

| 目標 | 狀態 |
| --- | --- |
| NL → AML → C# 全流程走通 | ✅ 已實跑一次（`MaxWeightIndependentSet` 唯一產物） |
| Fluent OptModel + 可跑 tuning（`ExperimentRunner`） | ✅ 已落地 |
| source generator 免手寫樣板 | ✅ 真有 code、有 build、實際被用 |
| 雙架構教學（同模型 Manual vs Generator） | 🟡 兩專案 code 已完整、diff 確認等價；**只差本機 build + CPLEX 驗證** |
| Stage 15 調校協定 | 🟡 spec 有，`Prompts/15_ModelTuning.md` 產物待補 |
| Framework 2：Web API + RAG（Semantic Kernel） | ❌ **只有 spec，未實作** |

## 6. 開放問題 & 未完成工作

**未定案（阻塞 F2 實作）**

- 公司 AI API 的 endpoint / 認證 / model name（卡住 `ILlmService` 實作）
- 生成 `.csproj` 的 OptimFoundation DLL reference 路徑策略
- API 是否需認證（JWT / API Key / 無）

**待辦清單**

- [ ] 補 `automated/Prompts/15_ModelTuning.md`（tuning-protocol spec 的產物）
- [ ] `MaxWeightIndependentSet` 標準化為六資料夾 + 雙模式（stages 數學文件歸入 `Model/`）
- [ ] Framework 2 Web API：依 env spec 的 Stub → 逐層實作
- [ ] tutorial 未提交工作收尾（tech-report / ppt splice / why-optimfoundation）

**`HospitalRosteringProblem_new/` 刪除條件（原 delete-list §F）**

`_new` 是舊命名（`SandBox`/`MyApp` namespace、`Data/`+`VariablesClass/`+`Constraints/` 資料夾）但**目前是唯一保有完整實作參考的版本**。刪除前提：`_Generator`/`_Manual` 的限制式/目標式/Dataload 移植完成 + 本機 build + 求解驗證通過後，才可 `Remove-Item Projects/HospitalRosteringProblem_new -Recurse`。（其餘 delete-list A/B/C/E 已於 2026-06-22 執行完畢。）

## 7. Spec 索引

**仍在進行中 / 實作藍圖（保留）**

| Spec | 狀態 | 為何保留 |
| --- | --- | --- |
| [`automated/specs/2026-05-20-...env.md`](automated/specs/2026-05-20-ai-modeling-optim-cplex-env.md) | draft | F2 Web API+RAG 唯一完整藍圖（endpoints/DTO/module 互動），未實作 |
| [`automated/specs/2026-06-21-model-tuning-protocol.md`](automated/specs/2026-06-21-model-tuning-protocol.md) | draft | Stage 15 協定細節，Prompt 產物未生 |
| [`specs/2026-06-22-dual-architecture-tutorial.md`](specs/2026-06-22-dual-architecture-tutorial.md) | implementing | 雙架構教學，尚有 build 驗證待辦 |
| [`specs/2026-06-22-dual-architecture-CodeMap.md`](specs/2026-06-22-dual-architecture-CodeMap.md) | reference | 雙架構骨架地圖 + 變數/限制式對照 |

**已併入本檔（原 spec 已刪除）**

- `automated/specs/2026-05-29-cplex-project-dev-spec.md` — 舊資料夾慣例，已被現行標準取代 → §3
- `specs/2026-06-21-claudeai-spec-refresh.md` — 唯一 OptModel 收斂，已完成且被 06-22 部分推翻 → §2、§3
- `specs/2026-06-21-manual-delete-list.md` — A/B/C/E 已執行；活的 §F 併入 → §6
