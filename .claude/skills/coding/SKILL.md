---
name: coding
description: Phase 2 轉譯 orchestrator——把已確認的 Model.md 純機械轉譯成 OptimFoundation CPLEX C# 專案，build 後跑解驗證協定。當使用者說「模型確認」「開始實作」「寫 code」「轉成程式」「build 這個專案」「跑跑看」時使用。模型未確認 NEVER 進本階段。
---

# coding — Phase 2 轉譯調度

三階段 phase gate：`modeling` → **`coding`（本 skill）** → `tuning`。

你是第二棒：把已經定案的 `Model/<Project>_Model.md` **逐條機械翻譯**成可 build、可求解的專案。你不是在「寫程式解問題」，是在抄一份已經寫好的數學模型——發現模型有歧義就停下回 `modeling`，NEVER 自行補假設。

> 路徑基準：以下所有路徑相對 **repo 根**（本檔位於 `.claude/skills/coding/SKILL.md`）。

## 規則在哪（本 skill 只有調度，規則一條都不複製）

| 文件 | 管什麼 | 何時讀 |
| --- | --- | --- |
| [`../AGENTS.md`](../AGENTS.md) | 天條、三階段契約、`status.json` schema | 動手前 |
| [`optimfoundation-api-guide.md`](optimfoundation-api-guide.md) | **Phase 2 唯一標準**：端到端轉譯規範，§9 = API 簽名權威 + 黑名單 | 每個 Step 依下表定位該節 |
| [`checklist.md`](checklist.md) | 交付前的機械驗收契約 | 宣告完成前逐條跑 |
| [`agent-workflow-prompts.md`](agent-workflow-prompts.md) | C0–C9 / V1–V3 派工拓樸 | 走 multi-agent 時 |

衝突順序：`../AGENTS.md` → api-guide → checklist。**兩份規則互相矛盾 → 停下回報檔名與衝突**，NEVER 挑方便的那條、NEVER 先產出再補讀。憑記憶寫 API 一律視為違規。

審查既有 Phase 2 專案 → 用 `/review`（[`../../commands/Ph2_Coding/review.md`](../../commands/Ph2_Coding/review.md)），不在本 skill 重述審查規則。

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
2. 確認使用者已表達「開始實作」，或 `status.json` 的 `modelConfirmed: true`

**使用者主動叫 `/coding` 並指向某一份 Model.md，本身就算「開始實作」**——本 skill 的 description 就是用這幾句話觸發的。但 MUST 在動手前用一句話覆述該 Model.md「已套用假設」段裡**會改變模型形狀**的項目（質量守恆、空白格語意、變數型別這類），給使用者當場否決的機會；覆述完直接開工，不要停下來等回覆。通過後把 `modelConfirmed` 設 true，並在交付時寫明依據是使用者的這次呼叫。

**空參數、或由你自己掃 `status.json` 挑出來的專案不算確認**：`modelConfirmed: false` 時停下問使用者。

**任一項不成立 → 停下，回 `modeling`**，不產任何 `.cs`。

3. 讀 `status.json` 判斷從哪接手：

| 現況 | 從哪開始 |
| --- | --- |
| 專案資料夾不存在 | Step 2 建專案 |
| `.cs` 不齊 | Step 3 續轉譯（已完成的檔 NEVER 重寫） |
| `buildOk: false` | Step 4 build fix loop |
| `buildOk: true`、`solveVerified: false` | Step 5 解驗證 |
| 兩者皆 true | 本 skill 已完成；要調校改用 `tuning` |

## Step 1 · 定位本階段細則

api-guide 是 Phase 2 唯一標準，**NEVER 整份讀**——現場搜章節行號再讀那一段：

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
| exp 分支交棒契約 | §8.4 |
| **API 簽名與黑名單** | §9（**NEVER 憑記憶發明 API**） |
| 常見錯誤與反模式 | §10 |
| 線性化 pattern 對照 | 附錄 A |

題目大（constraint > 8 條或 Model.md > 300 行）→ 依 `agent-workflow-prompts.md` 的 C0–C9 / V1–V3 拓樸派工。

## Step 2 · 建專案

唯一 scaffold 是 repo 根的 `Template/`，生成到 `Projects/<Project>/`。csproj 的 DLL `<Reference>`、`<Analyzer>` 與 `Generated/` 排除寫法照 **api-guide §1.4 照抄**；八資料夾結構見 §1.1。

**NEVER 從 sibling `OptimFoundation/OptimFoundation/Templates/` 複製結構**（那是相容性範例，不是 scaffold）。

## Step 3 · 轉譯順序（依序，一項一檔）

| 順序 | 產出 | 錨點 | 規範 |
| --- | --- | --- | --- |
| 1 | `Set/Set_*.cs` | Model.md 的 SET 段 | §2.1 |
| 2 | `Parameter/Parameter_*.cs` | PARAM 段，同下標的係數併一個類 | §2.2 |
| 3 | `Data/Dataload.cs` + `Data/*.csv` | 顯式載入，數值保真 | §2.0、§2.4 |
| 4 | `Variable/Variable{B,C,I}_*.cs` | VAR 段，型別由前綴決定 | §3 |
| 5 | `Objective/ObjectiveFunction.cs` | OBJ 段逐項 | §4.3 |
| 6 | `Constraint/Constraint_*.cs` | 每條 `[Cn]` 一檔，`///` 註記寫回條號 | §4.2、附錄 A |
| 7 | `Solution/<Project>Solution.cs` | `ValidateData`（建模前資料驗收）+ `ValidateRules`（解後逐條代回） | §6 |
| 8 | `Program.cs` | 材料 → 資料驗收 → OptModel → runner，順序照 `[C1][C2]…` | §5 |

轉譯鐵律（移項禁令、`AddLHS`/`AddRHS`/`CreateXxx` 對應、係數來源）全在 **api-guide §4 與 `../AGENTS.md` 的數學一致性天條**，動手前讀那兩處。

Objective 與 Constraint **誰先寫成檔案不拘**（5 / 6 可對調，multi-agent 時 C5 ∥ C6 平行派工）；真正受規範的是 `Program.cs` 的**註冊順序**——`.AddObjective` MUST 在 `.AddConstraints` 之前（§4.3）。

**中途發現 Model.md 歧義 → 立即停止，回 `modeling` 補模型**，NEVER 自己選一種解釋繼續。

## Step 4 · build fix loop（上限 5 次）

`dotnet build` → 有錯就修 → 重 build。**第 5 次仍失敗就停下回報**，附最後一次的錯誤摘要與你已試過的修法，不要無限迴圈。

（`.claude/hooks/dotnet-build-summary.ps1` 會自動摘要 error/warning 行。）

## Step 5 · 解驗證協定（四步全過才算完成）

判定表與四步定義在 **api-guide §7**（`SolveStatus` 分流、每一步怎麼驗、`TimeLimit` 無解時怎麼換小 instance）。

本 skill 只管一件事：**四步沒全過就不准宣告完成**。看到 `Optimal` 就宣稱正確不合格；看到非 `Optimal` 就宣稱失敗同樣不合格——`Feasible` 照樣往下驗，只有 `Infeasible` / `Unbounded` 是真的擋（退回 `modeling`）。

## Step 5.5 · exp 分支交棒定形

**Phase 2 的交付 MUST 讓 Phase 3 一行 code 都不用改就跑得出 R0。** 形狀契約（experiment 名、`r0-` label、marker、baseline × 5 seeds、`productionBaseline` 的三顆環境旋鈕）逐條定義在 **api-guide §8.4**，驗收項在 `checklist.md` §13。

MUST 實跑一次 `-- exp` 確認管線可執行；bin 產物**不 archive**（archive 是 Phase 3 每輪的責任）。

**不做**：sizing 比較、算 θ、判瓶頸剖面、指定 holdout seeds —— 那些是解讀，屬 Phase 3。

## Step 6 · 交付 + 更新 status.json

交付內容：build 結果、目標值、解摘要、輸出檔位置（`Solution/`、`Models/`）、與 Model.md 小例的對照結果、exp 分支的 R0-ready 確認結果。
交付前 MUST 逐條跑完 `checklist.md`；不適用項目要說明原因。

`Projects/<Project>/status.json` **只更新下列欄位**（完整 schema 在 `../AGENTS.md`，NEVER 整檔覆寫掉其他欄位）：

```json
{ "phase": "coding", "buildOk": true, "solveVerified": true, "solveStatus": "Optimal", "verifiedOn": "production", "updated": "YYYY-MM-DD" }
```

## Fatal（本階段特有；通用天條全在 `../AGENTS.md`，一樣適用）

- NEVER 模型未確認就產 `.cs`
- NEVER 自行詮釋 Model.md（歧義一律回 `modeling`）
- NEVER fix loop 超過 5 次
- NEVER 用 `Optimal` 以外的 Status 當作「失敗」而退回 `modeling`
- NEVER 交出「Phase 3 得先重寫才能跑」的 exp 分支（§8.4 四條缺一即 FAIL）
- NEVER 在 exp 分支混掃多顆旋鈕充當 r0
