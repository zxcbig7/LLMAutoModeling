---
name: coding
description: Phase 2 轉譯 orchestrator——把已確認的 Model.md 純機械轉譯成 OptimFoundation CPLEX C# 專案，build 後跑解驗證協定。當使用者說「模型確認」「開始實作」「寫 code」「轉成程式」「build 這個專案」「跑跑看」時使用。模型未確認 NEVER 進本階段。
---

# coding — Phase 2 轉譯調度

三階段 phase gate：`modeling` → **`coding`（本 skill）** → `tuning`。

你是第二棒：把已經定案的 `Model/<Project>_Model.md` **逐條機械翻譯**成可 build、可求解的專案。你不是在「寫程式解問題」，是在抄一份已經寫好的數學模型——發現模型有歧義就停下回 `modeling`，NEVER 自行補假設。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.claude/skills/coding/SKILL.md`）。
> 規則單一來源：`../AGENTS.md`（天條 + 三階段契約）+ `optimfoundation-api-guide.md`（**Phase 2 唯一標準**：端到端規範 + §9 API 簽名權威 + 黑名單）。
> 本 skill 只做調度與 gate 把關，**NEVER 在此複製規則**——每次執行都實際讀那兩份檔，不憑記憶。

## 文件遵循 gate（不可跳過）

在讀取 Model.md、規劃、建立、修改或驗證任何專案檔案前，MUST 逐一讀取並遵守完整文件集：`../AGENTS.md`、`optimfoundation-api-guide.md`、`PH2_SOP.md`、`model-to-code-checklist.md`、`checklist.md`、`agent-workflow-prompts.md`。任何檔案缺失、無法讀取、內容相互矛盾，或無法證明交付物符合其中所有適用要求時，MUST 停下並回報檔名與衝突；NEVER 猜測、挑選較方便的規則，或先產出再補讀。

權威順序僅用於判定衝突，不會免除閱讀：`../AGENTS.md` → `optimfoundation-api-guide.md` → `PH2_SOP.md` → `model-to-code-checklist.md` → `checklist.md` → `agent-workflow-prompts.md`。交付前 MUST 完成兩份 checklist 的所有適用項目；不適用項目必須說明原因。multi-agent 工作時，`agent-workflow-prompts.md` 的 context、工單與驗收限制同樣強制適用。

## Review-only 模式

當使用者要求審查、驗收或 review 既有 Phase 2 專案時，**不建立或修改模型與程式**。讀取 Model.md、兩份 checklist 與 API guide §9 後，只報告可由文件或程式碼證明的問題：

- phase gate 與每個程式元素是否能對應 Model.md 宣告；
- Constraint 是否保留 LHS／運算子／RHS，沒有移項、改號或偷放常數；
- 命名、維度、變數型別、bounds、Dataload 與資料來源是否一致；
- API、DLL、Generated 排除、build／run／解驗證證據是否完整。

輸出固定為 `Blocker`、`Major`、`Minor`、`Verified` 四節。每項附檔案路徑、行號（可得時）、違反的 Model.md／規則依據與最小修正建議；沒有證據時明列「未驗證」，NEVER 自行補寫需求或改變數學模型。

## 輸入（`$ARGUMENTS`）

`/coding <Model.md 檔案>`——參數指出要轉譯**哪一份**模型，`<Project>` 由它反推。

| 參數形式 | 動作 |
| --- | --- |
| `Projects/<Project>/Model/<Project>_Model.md` | `<Project>` 取自路徑，直接進 Step 0 |
| 其他位置的 `.md` | 先讀它確認是不是 Model.md；不在 canonical 位置就問使用者要建成哪個 `<Project>`，NEVER 自行搬檔或改名 |
| 專案名（無副檔名） | 對到 `Projects/<Project>/Model/<Project>_Model.md` |
| 空 | 掃 `Projects/*/status.json` 找 `modelConfirmed: true` 且 `solveVerified: false` 的專案；恰好一個就用它並回報，0 個或 2 個以上停下問使用者 |

參數只決定**轉譯哪一份**，不代表模型已確認——Step 0 的 gate 照走。

## Step 0 · 入口 gate（不通過就不准動手）

1. 確認 `Projects/<Project>/Model/<Project>_Model.md` 存在
2. 確認使用者已明說「模型確認」/「開始實作」，或 `status.json` 的 `modelConfirmed: true`

**任一項不成立 → 停下，回 `modeling`**，不產任何 `.cs`。

3. 讀 `status.json` 判斷從哪接手：

| 現況 | 從哪開始 |
| --- | --- |
| 專案資料夾不存在 | Step 2 建專案 |
| `.cs` 不齊 | Step 3 續轉譯（已完成的檔 NEVER 重寫） |
| `buildOk: false` | Step 4 build fix loop |
| `buildOk: true`、`solveVerified: false` | Step 5 解驗證 |
| 兩者皆 true | 本 skill 已完成；要調校改用 `tuning` |

## Step 1 · 讀本階段細則

讀 `optimfoundation-api-guide.md`——Phase 2 唯一標準。先完成文件遵循 gate，再依需要定位章節：

```powershell
Select-String -Path ".claude/skills/coding/optimfoundation-api-guide.md" -Pattern "^## §|^## 附錄"
```

| 要查什麼 | 節 |
| --- | --- |
| 進場檢查（Model.md 合格嗎） | §0.0 |
| 建專案 / csproj / DLL | §1 |
| 資料層 Set / Parameter / Dataload / CSV | §2 |
| 變數層 | §3 |
| Objective 與 Constraint | §4 |
| `Program.cs` 組裝 + 三態 CLI | §5 |
| Solution 取解與輸出 | §6 |
| 解驗證協定四步 | §7 |
| **API 簽名與黑名單** | §9（**NEVER 憑記憶發明 API**） |
| 常見錯誤與反模式 | §10 |
| 線性化 pattern 對照 | 附錄 A |

題目大（constraint > 8 條或 Model.md > 300 行）→ 依 `agent-workflow-prompts.md` 的 C0–C9 / V1–V3 拓樸派工。

## Step 2 · 建專案

- 唯一 scaffold 是 `Template/`，生成到 `Projects/<Project>/`
- DLL 一律 `<Reference>` + HintPath `..\..\dlls\`；generator 用 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />`
- csproj MUST `<Compile Remove="Generated/**/*.cs" />`
- NEVER 從 sibling `OptimFoundation/OptimFoundation/Templates/` 複製結構（那是相容性範例，不是 scaffold）

## Step 3 · 轉譯順序（依序，一項一檔）

| 順序 | 產出 | 錨點 |
| --- | --- | --- |
| 1 | `Set/Set_*.cs` | Model.md 的 SET 段 |
| 2 | `Parameter/Parameter_*.cs` | PARAM 段，同下標的係數併一個類 |
| 3 | `Data/Dataload.cs` + `Data/*.csv` | 顯式載入，數值保真 |
| 4 | `Variable/Variable{B,C,I}_*.cs` | VAR 段，型別由前綴決定 |
| 5 | `Constraint/Constraint_*.cs` | 每條 `[Cn]` 一檔，`///` 註記寫回條號 |
| 6 | `Objective/ObjectiveFunction.cs` | OBJ 段逐項 |
| 7 | `Program.cs` | 材料 → OptModel → runner，順序照 `[C1][C2]…` |

轉譯鐵律（細則見 api-guide §4）：左式項 → `AddLHS`、右式項 → `AddRHS`、比較符號 → `Create{Less/Great}Equal`；係數先存局部變數再傳入；Objective MUST 先於所有 Constraint。

**中途發現 Model.md 歧義 → 立即停止，回 `modeling` 補模型**，NEVER 自己選一種解釋繼續。

## Step 4 · build fix loop（上限 5 次）

`dotnet build` → 有錯就修 → 重 build。**第 5 次仍失敗就停下回報**，附最後一次的錯誤摘要與你已試過的修法，不要無限迴圈。

（`.claude/hooks/dotnet-build-summary.ps1` 會自動摘要 error/warning 行。）

## Step 5 · 解驗證協定（四步全過才算完成）

1. **Status 五態診斷**（依框架 `SolveStatus`，NEVER 只認 `Optimal`）：

   | 狀態 | 意義 | 動作 |
   | --- | --- | --- |
   | `Optimal` | 證明最佳 | 往下做 2–4 |
   | `Feasible` | 有解未證明最佳（撞 `TimeLimit` / `NodeLimit` / `IntegerSolutionLimit`） | **照樣往下做 2–4**，對 incumbent 驗；記下 `MipGap` 與 `BestBound`。**「太慢」不是本階段能修的事**，那是 Phase 3 |
   | `TimeLimit` | 中止且**無任何可用解**（名字誤導，非時間專屬；`ObjectiveValue` / `MipGap` 皆 `NaN`） | 沒有解可驗 → 縮小 `Data/*.csv` 成小 instance 求到 `Optimal`，用它完成 2–4，並記 `verifiedOn: "small-instance:<說明>"` |
   | `Infeasible` | 無可行解 | 走 IIS（`bin/Debug/net8.0/IISs/*.ilp`），回 `modeling` |
   | `Unbounded` | 目標式無界 | 查漏掉的界限 constraint |

2. **可行性代回**：把解代回每條 constraint，確認 LHS op RHS 成立
3. **單位一致**：目標值與關鍵變數的單位、量級對得上題目
4. **LP bound sanity**：max 的整數解 ≤ LP bound；min 反之（`Feasible` 時比 `BestBound`）

看到 `Optimal` 就宣稱正確**不合格**；看到非 `Optimal` 就宣稱失敗**同樣不合格**——只有 `Infeasible` / `Unbounded` 是真的擋。

## Step 6 · 交付 + 更新 status.json

交付內容：build 結果、目標值、解摘要、輸出檔位置（`Solution/`、`Models/`）、與 Model.md 小例的對照結果。
附上同資料夾 `checklist.md` 提醒使用者逐項人工核對。

`Projects/<Project>/status.json` **只更新下列欄位**（完整 schema 見 `modeling` skill，NEVER 整檔覆寫掉其他欄位）：

```json
{ "phase": "coding", "buildOk": true, "solveVerified": true, "solveStatus": "Optimal", "verifiedOn": "production", "updated": "YYYY-MM-DD" }
```

## Fatal

- NEVER 模型未確認就產 `.cs`
- NEVER 自行詮釋 Model.md（歧義一律回 `modeling`）
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER 裸數字進 Constraint / Objective（一律 `Parameter` 的 `QTY`）
- NEVER 用 helper / local function 隱藏 `Program.cs` 的組裝順序
- NEVER 呼叫 `optimfoundation-api-guide.md` §9 沒列的 API，也 NEVER 用它標 ❌ 的 API
- NEVER 改 OptimFoundation 框架本體或換 DLL 來源
- NEVER fix loop 超過 5 次
- NEVER 用絕對路徑
