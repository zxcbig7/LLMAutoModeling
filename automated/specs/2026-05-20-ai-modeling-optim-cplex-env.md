---
title: AI Modeling 自動建模環境 — OptimFoundation (CPLEX) 版
status: draft
created: 2026-05-20
updated: 2026-05-20
modules: [claude-code-assistant, aspnet-webapi, rag, csharp-codegen]
---

# AI Modeling 自動建模環境 — OptimFoundation (CPLEX) 版

## Summary

兩個互補的框架，共用相同的多階段推理邏輯（Prompt 模板 + OptimFoundation 慣例）：

| | Framework 1 | Framework 2 |
|---|---|---|
| **模式** | Claude Code 互動助手 | ASP.NET Core Web API |
| **使用者** | 建模工程師（直接與 Claude Code 對話） | 前端操作者（REST API + UI） |
| **優先順序** | **現在** | 後續 |
| **端到端連貫** | 每個 stage 輸出立刻存檔，context 重置也能 resume | 非同步 Background Job |

共同流程：**問題描述 → 問題分類（LP/IP/MILP）→ 數學模型（AML Markdown）→ 驗證 → C# 專案 → Build & Run**

## Motivation / Why

OptimFoundation 是公司自建的 C# 最佳化框架，手寫 CPLEX 模型需熟悉框架 API（`BuildCVs<>`、`AddLHS/AddRHS`、`CreateMinimize()` 等），且需將自然語言問題轉為嚴謹數學模型，門檻高且耗時。AI 輔助建模系統可將「**問題描述 → 生成數學模型 → 可執行 C# 專案**」的時間從數小時縮短到數分鐘；數學模型（AML Markdown）同時作為可審閱的中間產物，使用者可在生成 code 前確認模型正確性。

## Scope

### In Scope

**Framework 1（Claude Code 互動模式）**
- 建模工程師直接描述問題給 Claude Code，逐 stage 推理並確認
- 每個 stage 輸出立刻寫入 `projects/<ProjectName>/stages/` 資料夾
- `status.json` 追蹤各 stage 完成狀態，支援 context 重置後 resume
- 建模完成後，建模師手動執行 `dotnet build` 並將 error 貼回給 Claude Code 修復
- Model tuning：修改約束 / 參數 / 目標函數後重新生成受影響的 code

**Framework 2（Web API，後續）**
- REST API 接受中文或英文的最佳化問題描述（自然語言）
- 雙模式：Chat 模式一問一答、Batch 模式全自動
- 非同步執行（Background Job），client polling 進度
- 自動 `dotnet build` + 自動錯誤修復（最多 5 次）

**共用**
- 問題類型分類（LP / IP / MILP）— 影響 CplexConfig 設定
- 多階段 LLM 推理：Classify → SimpleModel → StandardModel → KeyInfo(JSON) → AMLModel(Markdown) → **AML 驗證** → C# Code（9 檔）→ **Dataload 驗證**
- AMLModel 格式：**遵循 AMPL 命名慣例與數學結構，以 Markdown 語法撰寫**，供 AI 生成 code 使用
- RAG 增強：從 `docs/` 向量資料庫檢索相似範例，輔助代碼生成
- 生成完整 OptimFoundation CPLEX C# 專案（含 .sln、.csproj）
- `WriteRagData()`：成功求解後，將生成的 AML Markdown 寫回 `docs/` RAG 文件庫（累積知識）

### Out of Scope

- Oracle DB 資料讀取（只支援 CSV）
- Gurobi solver（只支援 CPLEX）
- 前端 UI（Framework 2 後續再做）
- 批次實驗（多問題同時跑）

## User Stories / Use Cases

1. As a 建模工程師, I want to POST 問題描述到 API, so that 無需熟悉框架 API 即可生成可執行的 CPLEX 模型。
2. As a 建模工程師, I want to 透過 Chat Session 一問一答補充約束與資料, so that AI 能精確理解問題範圍。
3. As a 建模工程師, I want to 查詢 Job 狀態與進度, so that 知道目前推理跑到哪個階段。
4. As a 建模工程師, I want to 生成的 C# 專案直接能 build 並跑出結果, so that 不需要手動修改任何 code。

## Acceptance Criteria

- [ ] `POST /api/projects/generate`（Batch）：接受問題描述，回傳 `jobId`，非同步執行完整生成流程
- [ ] `POST /api/sessions`（Chat）：建立 Session，回傳 `sessionId`
- [ ] `POST /api/sessions/{id}/messages`：傳送訊息，AI 回覆問題（補充細節或確認）
- [ ] `POST /api/sessions/{id}/generate`：對話完成後觸發生成，回傳 `jobId`
- [ ] `GET /api/jobs/{id}/status`：回傳目前階段（e.g. `generating_aml_model`）與進度百分比
- [ ] `GET /api/jobs/{id}/result`：生成完成後回傳專案路徑與各中間產物內容
- [ ] 問題類型自動分類（LP / IP / MILP），結果影響 CplexConfig 參數
- [ ] 多階段推理完整執行：Classify → SimpleModel → StandardModel → KeyInfo(JSON) → AMLModel(Markdown)
- [ ] AML 模型驗證（`GetAmlVerifyPrompt`）：驗證線性性、命名慣例、Sets/Params/Vars 完整性
- [ ] 自動生成 9 個 C# 檔案（Parameters.cs、Variables.cs、Constraints.cs、ObjectiveFunction.cs、Dataload.cs、VariableCreate.cs、BuildConstraints.cs、Project.cs、Program.cs）
- [ ] Dataload 驗證（`GetDataloadVerifyPrompt`）：驗證 constructor 與 Param 類別一致性，所有 Sets/Params 皆有資料
- [ ] 生成的 code 使用 OptimFoundation.Cplex 標準 API
- [ ] `dotnet build` 成功（含自動修復最多 5 次）
- [ ] `dotnet run` 輸出 CSV 求解結果至 `Results/`
- [ ] 輸出專案位於 `projects\<ProjectName>_<YYYYMMDD_HHMMSS>\`
- [ ] 每個推理階段的中間產物（txt/json/md）保存至 `Model\` 子目錄
- [ ] 求解成功後，`WriteRagData()` 將 AML Markdown 寫入 `docs/<ProjectName>.md`

## Framework 1：Claude Code 互動模式設計

### 工作流程

```
1. 使用者描述問題 → Claude Code 執行 Stage 0（分類）
2. 逐 stage 推理，每個 stage 輸出立刻寫入 stages/ 資料夾
3. 關鍵 stage（AMLModel、Dataload）完成後暫停讓使用者確認
4. 全部 stage 完成後，使用者執行 dotnet build
5. Build error → 貼回給 Claude Code 修復（最多 5 次）
6. 成功後 WriteRagData，AML 寫回 docs/
7. Tuning：修改描述 → 重新執行受影響的 stage（從任意 stage resume）
```

### Project Workspace 結構（端到端連貫的關鍵）

每個 stage 的輸出立刻存檔，Claude Code context 重置後讀檔即可 resume：

```
projects/<ProjectName>/
├── stages/
│   ├── 00_classify.json          # {"ProblemType": "MILP", "Reasoning": "..."}
│   ├── 01_simple.md
│   ├── 02_standard.md
│   ├── 03_keyinfo.json
│   ├── 04_aml.md
│   ├── 04b_aml_verified.md
│   ├── 05_parameters.cs
│   ├── 06_variables.cs
│   ├── 07_dataload.cs
│   ├── 07b_dataload_verified.cs
│   ├── 08_constraints.cs
│   ├── 09_objective.cs
│   ├── 10_varcreate.cs
│   ├── 11_buildconstraints.cs
│   ├── 12_project.cs
│   └── 13_program.cs
├── status.json                   # {"completed": ["00","01",...], "current": "04"}
└── csharp/                       # 生成的 OptimFoundation C# 專案
    ├── Model/
    │   ├── Parameters.cs
    │   ├── Variables.cs
    │   ├── Constraints.cs
    │   └── ObjectiveFunction.cs
    ├── Project/
    │   ├── Dataload.cs
    │   ├── VariableCreate.cs
    │   ├── BuildConstraints.cs
    │   └── Project.cs
    ├── Program.cs
    └── <ProjectName>.csproj
```

### Resume 規則

若 Claude Code context 重置，使用者說「繼續」時：
1. Claude Code 讀取 `status.json` 確認已完成的 stage
2. 讀取最後完成的 stage 輸出檔作為 context
3. 從 `current` stage 繼續執行，不重跑已完成的 stage

---

## Module Interactions

```
HTTP Client
    ↓
[ASP.NET Core Web API]
    ├─ SessionsController  → Chat 模式多輪對話
    └─ ProjectsController  → Batch 模式 & Job 狀態查詢
    ↓
[ModelingService] 主協調器（Background Job）
    ├─ ClassifyProblem()   → LP / IP / MILP
    ├─ GenModel() → 4 階段推理 + 驗證
    │   ├─ [LlmService] GenerateSimpleModelAsync()
    │   ├─ [LlmService] GenerateStandardModelAsync()
    │   ├─ [LlmService] ExtractKeyInfoAsync()           → KeyInfo.json
    │   ├─ [LlmService] GenerateAmlModelAsync()         → AMLModel.md
    │   └─ [LlmService] VerifyAmlModelAsync()           → AMLModel_verified.md
    ├─ GenCode() → 9 個 C# 檔案 + Dataload 驗證
    │   ├─ 各 Prompt 呼叫 [LlmService] + [RagService] 檢索
    │   └─ [LlmService] VerifyDataloadAsync()           → Dataload_verified.cs
    ├─ WriteProject() → [CSharpCtrlService] 建目錄、寫檔
    ├─ BuildAndRun() → [CSharpCtrlService] dotnet build/run/fix loop
    └─ WriteRagData() → [RagService] 寫 AML.md 回 docs/（求解成功後）
    ↓
[JobStore] 記憶體或檔案存 Job 狀態與進度
```

**服務說明：**

| 服務 | 職責 |
|------|------|
| `ModelingService` | 主協調器，管理多階段推理流程，回報 Job 進度 |
| `LlmService` | 可抽換的 LLM interface，`<TODO: 待確認>` 接公司 AI API |
| `RagService` | **Microsoft Semantic Kernel** 向量資料庫，載入 `docs/` 範例，執行相似度檢索 |
| `PromptLibrary` | 14+ 個 static Prompt 方法（各推理階段專用） |
| `CSharpCtrlService` | 建目錄結構、寫入 .cs/.csproj/.sln、執行 dotnet build/run |
| `JobStore` | 儲存 Job 狀態（Pending / Running / Done / Failed）與進度 |
| `SessionStore` | 儲存 Chat Session 對話歷史 |

## API Design

### Endpoints

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/api/projects/generate` | Batch 模式：送入問題描述，回傳 jobId | `<TODO>` |
| POST | `/api/sessions` | 建立 Chat Session | `<TODO>` |
| POST | `/api/sessions/{id}/messages` | 傳送訊息（Chat 模式） | `<TODO>` |
| POST | `/api/sessions/{id}/generate` | 對話完成，觸發生成 | `<TODO>` |
| GET | `/api/jobs/{id}/status` | 查詢 Job 狀態與進度 | `<TODO>` |
| GET | `/api/jobs/{id}/result` | 取得生成結果 | `<TODO>` |

### Request / Response Schema

```csharp
// POST /api/projects/generate
record GenerateRequest(
    string ProjectName,
    string ProblemDescription
);
record GenerateResponse(string JobId);

// POST /api/sessions
record CreateSessionRequest(string ProjectName);
record CreateSessionResponse(string SessionId);

// POST /api/sessions/{id}/messages
record SendMessageRequest(string Content);
record SendMessageResponse(string Reply, bool IsReadyToGenerate);

// GET /api/jobs/{id}/status
record JobStatusResponse(
    string JobId,
    string Status,       // Pending | Running | Done | Failed
    string CurrentStage, // generating_simple_model | generating_code | building | ...
    int ProgressPercent,
    string? ErrorMessage
);

// GET /api/jobs/{id}/result
record JobResultResponse(
    string JobId,
    string ProjectPath,
    string SimpleModel,
    string StandardModel,
    string KeyInfoJson,
    string AmlModelMarkdown,
    bool BuildSuccess,
    int BuildRetryCount
);
```

### 多階段推理 Prompt 對應表

| # | 階段 | Prompt 方法 | 輸入 | 輸出 | RAG |
|---|------|------------|------|------|-----|
| 0 | ProblemClassify | `GetClassifyPrompt()` | 原始描述 | LP / IP / MILP | No |
| 1 | SimpleModel | `GetSimpleModelPrompt()` | 原始描述 | 簡化自然語言 | No |
| 2 | StandardModel | `GetStandardModelPrompt()` | SimpleModel | 標準化（統一術語、量綱） | No |
| 3 | KeyInfo | `GetExtractionPrompt()` | StandardModel | JSON（Sets/Params/Vars/Obj/Constraints） | No |
| 4 | AMLModel | `GetAmlPrompt()` | StandardModel + KeyInfo | **AMPL 結構 Markdown** 數學模型 | **Yes** |
| 4b | AML 驗證 | `GetAmlVerifyPrompt()` | 原始描述 + AMLModel | 驗證報告 + 修正後 AMLModel.md | No |
| 5 | Parameters.cs | `GetParamCodePrompt()` | AMLModel | C# code | No |
| 6 | Variables.cs | `GetVarCodePrompt()` | AMLModel | C# code | No |
| 7 | Dataload.cs | `GetDataloadCodePrompt()` | StandardModel + AMLModel + ParamCode | C# code | **Yes** |
| 7b | Dataload 驗證 | `GetDataloadVerifyPrompt()` | 原始描述 + AMLModel + ParamCode + DataloadCode | 驗證報告 + 修正後 Dataload.cs | No |
| 8 | Constraints.cs | `GetConstraintCodePrompt()` | AMLModel + ParamCode + VarCode + DataloadCode | C# code | **Yes** |
| 9 | ObjectiveFunction.cs | `GetObjCodePrompt()` | AMLModel + ParamCode + VarCode + DataloadCode + ConstraintCode | C# code | **Yes** |
| 10 | VariableCreate.cs | `GetVarCreateCodePrompt()` | AMLModel + VarCode + DataloadCode | C# code | **Yes** |
| 11 | BuildConstraints.cs | `GetBuildConstraintsCodePrompt()` | AMLModel + ConstraintCode | C# code | No |
| 12 | Project.cs | `GetProjCodePrompt()` | AMLModel + VarCode | C# code | No |
| 13 | Program.cs | `GetProgramCodePrompt()` | ProjectCode | C# code | No |
| 14 | Fix | `GetFixPrompt()` | error msg + 失敗 code + 相關 classes | 修正後 C# code | No |

### 生成的 C# 專案結構

```text
projects/<ProjectName>_<YYYYMMDD_HHMMSS>/
├── Model/                          # 中間推理產物
│   ├── 00_SimpleModel.txt
│   ├── 01_StandardModel.txt
│   ├── 02_KeyInfo.json
│   └── 03_AMLModel.md
├── <ProjectName>/                  # 生成的 CPLEX C# 專案
│   ├── Model/
│   │   ├── Parameters.cs
│   │   ├── Variables.cs
│   │   ├── Constraints.cs
│   │   └── ObjectiveFunction.cs
│   ├── Project/
│   │   ├── Dataload.cs
│   │   ├── VariableCreate.cs
│   │   ├── BuildConstraints.cs
│   │   └── Project.cs
│   ├── Program.cs
│   └── <ProjectName>.csproj
├── <ProjectName>.sln
└── Logs/
    └── <ProjectName>_<timestamp>.log
```

## Data Model

### KeyInfo JSON（LLM 提取的結構化模型）

```json
{
  "Sets": [
    { "Name": "string", "Type": "STR|INT|DATE", "Description": "string" }
  ],
  "Parameters": [
    { "Name": "string", "Dim": ["SetName"], "Description": "string" }
  ],
  "Variables": [
    {
      "Name": "string",
      "Dim": ["SetName"],
      "Type": "NUM|INT|BIN",
      "Description": "string",
      "LB": 0,
      "UB": "INFTY|number"
    }
  ],
  "Objective": {
    "Description": "string",
    "Sense": "Maximize|Minimize"
  },
  "Constraints": [
    { "Description": "string", "Dim": ["SetName"], "Type": "UB|LB|EQ" }
  ]
}
```

### RAG docs/ 文件結構

```text
docs/
├── Basic_Knapsack.md           # 背包問題範例
├── Basic_Transportation.md     # 運輸問題範例
├── Basic_TSP.md                # TSP 範例
├── CodeTemplate_Variables.md   # OptimFoundation CPLEX 變數代碼樣板
├── CodeTemplate_Constraints.md # 約束代碼樣板
├── CodeTemplate_Dataload.md    # 資料載入樣板
└── ...                         # 其他範例（遷移自 OMG_LLM docs/）
```

## Edge Cases & Error Handling

- **Build 失敗**：擷取 compiler error → 送 Fix Prompt → 重寫對應 .cs → 重試（最多 5 次）；5 次後 Job 狀態改 `Failed`，保留 error log
- **LLM API 失敗**：retry 3 次，每次間隔 10 秒；全失敗後 Job 狀態改 `Failed`，保留已生成的中間產物
- **KeyInfo JSON 格式錯誤**：偵測 JSON parse 失敗後要求 LLM 重新生成，最多 3 次
- **Chat 問題描述不清**：AI 主動回覆「缺少哪些資訊」，`IsReadyToGenerate: false`，最多追問 3 輪後強制允許觸發生成
- **Batch 空描述**：400 Bad Request
- **Job 不存在**：404 Not Found

## Non-Functional Requirements

- **Performance**：全流程（LLM 呼叫 + dotnet compile）< 5 分鐘（simple LP）、< 15 分鐘（MILP）
- **Async**：生成流程以 Background Service 執行，HTTP 立即回傳 `jobId`，client polling `/jobs/{id}/status`
- **Observability**：每個推理階段記錄開始/結束時間與 LLM token 用量；使用 ASP.NET Core `ILogger`
- **LLM 抽換性**：`ILlmService` interface，公司 AI / 其他模型可替換實作
- **Portability**：Windows 環境；dotnet ≥ 8.0

## Open Questions

- [ ] 公司 AI API 的 endpoint、認證方式、model name（影響 `ILlmService` 實作）
- [x] RAG 框架：**Microsoft Semantic Kernel**（`VolatileMemoryStore`）
- [x] Embedding Model：**`text-embedding-3-small`（Azure OpenAI / OpenAI）**，1536-dim，Semantic Kernel 原生支援，中英混合 + 技術術語表現佳；若公司 AI 提供 embedding endpoint，實作自訂 `ITextEmbeddingGenerationService` 替換
- [ ] OptimFoundation DLL reference 路徑：生成的 .csproj 要指向哪個 DLL？（`OptimFoundation\bin\Debug\` 或 NuGet？）
- [ ] Auth：API 是否需要認證？（JWT / API Key / 無）

## Implementation Plan

### Phase 1：Framework 1 — Claude Code 互動助手（現在）

**目標**：Claude Code 能夠引導使用者完成「問題描述 → AML 數學模型 → C# 專案 → Build」全流程，每個 stage 輸出存檔，支援 context 重置後 resume。

- [ ] 建立 `projects/` 目錄結構與 `status.json` 規格
- [ ] 確認 16 個 `Prompts/*.md` 模板完整（含 00、04b、07b）
- [ ] 補齊缺少的 Prompt 模板（05-08、11-13）
- [ ] 建立 `docs/` 範例文件（至少 2-3 個基礎 LP/MILP 模型，作為 RAG seed）
- [ ] 端到端驗證：用一個簡單 LP 問題走完全部 16 stages，確認每個 stage 輸出正確

### Phase 2：Framework 2 — Web API（後續）

**目標**：將 Phase 1 的 Prompt 邏輯包裝成 REST API，配合前端操作。

**Stub 階段**

- [ ] 建立 Solution 結構：`src/AIModeling.Api/`（ASP.NET Core Web API project）
- [ ] `Controllers/ProjectsController.cs` — stub endpoints，全回傳 `501 Not Implemented`
- [ ] `Controllers/SessionsController.cs` — stub endpoints，全回傳 `501 Not Implemented`
- [ ] `Services/IModelingService.cs` + `ModelingService.cs` — interface + stub
- [ ] `Services/ILlmService.cs` + `LlmService.cs` — interface stub
- [ ] `Services/IRagService.cs` + `RagService.cs` — interface stub（`QueryAsync()`、`QueryRagAsync()`、`WriteRagDataAsync()`）
- [ ] `Services/CSharpCtrlService.cs` — stub
- [ ] `Prompts/PromptLibrary.cs` — 16 個 static 方法，讀取 `Prompts/*.md` 並填入變數
- [ ] `Models/` — 所有 Request / Response DTO record 定義（含 `ProblemType` enum）
- [ ] `Program.cs` — 註冊所有服務，含 Semantic Kernel + OpenAI embedding
- [ ] `dotnet build` 成功確認結構無誤

**逐層實作**

- [ ] `ILlmService` 實作（接公司 AI API，HttpClient + JSON）
- [ ] `IRagService` 實作（Semantic Kernel，`text-embedding-3-small`，top-5）
- [ ] `ModelingService` 實作（分類 + 16 stages + 驗證 + WriteRagData）
- [ ] `CSharpCtrlService` 實作（建目錄、寫 .csproj/.sln、dotnet build/run、fix loop）
- [ ] Background Job 機制（`IHostedService` 或 `IBackgroundTaskQueue`）
- [ ] `JobStore` / `SessionStore` 實作
- [ ] Controllers 串接 Service
- [ ] 端到端測試（一個簡單 LP 問題全流程跑通）

## References

- 參考架構：研究來源 paper「OMG_LLM Reasoning Module」（外部，非本 repo 相依）
- 目標框架：sibling 資料夾 `../../OptimFoundation/`（本 repo 外，相對路徑）
- CPLEX 框架模板：`../../OptimFoundation/Templates/Template_CPLEX`
- 參考 Prompt 庫：paper 附的 `Prompts.py`（Gurobi 版，改寫時換掉所有 Gurobi API）
