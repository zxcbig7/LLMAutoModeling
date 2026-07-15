# AI Modeling — automated 路線（全自動 16-stage）

<system_context>
不需人在迴路的量產路線：Claude Code 依序跑 Stage 00 → 14 一路把自然語言最佳化題目生成可求解的 OptimFoundation CPLEX C# 專案。
一般開發走有 gate 的 interactive 路線（見 `../interactive/`）；本路線為全自動量產。
**天條唯一權威在 [`../AGENTS.md`](../AGENTS.md)**；API 簽名見 [`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md)。
（歷史註記：早期曾規劃 ASP.NET Web API + Semantic Kernel RAG 版本，已廢；本路線純由 Claude Code 驅動 `Prompts/` 模板。）
</system_context>

---

## 多階段推理流程（16 個階段）

```text
Stage 00: Classify → ProblemType (LP/IP/MILP)
Stage 01: SimpleModel → 結構化自然語言
Stage 02: StandardModel → 標準化描述 + 約束分類
Stage 03: KeyInfo → JSON（Sets/Params/Vars/Obj/Constraints）
Stage 04: Model → Markdown 數學模型（AMPL-style 記法）
Stage 04b: ModelVerify → 驗證 & 修正 Model（必要）
Stage 05: ParamCode → Parameter/
Stage 06: VarCode → Variable/
Stage 07: DataloadCode → Set/Dataload.cs
Stage 07b: DataloadVerify → 驗證 & 修正 Dataload（必要）
Stage 08: ConstraintCode → Constraint/
Stage 09: ObjCode → Objective/
Stage 10: VarCreateCode → VariableCreate
Stage 11: BuildConstraints → BuildConstraints
Stage 12: ProjectCode → Program 求解主體
Stage 13: ProgramCode → Program.cs 入口
Stage 14: FixCode → build 失敗時自動修復（循環最多 5 次）
```

Prompt 模板在 [`Prompts/`](Prompts/)，一階段一個 `.md`。

### Stage 00 — 問題分類決定 CplexConfig 預設

| ProblemType | mipEmphasis | timeLimit |
|---|---|---|
| LP | 0 | 300 |
| IP | 1 | 1800 |
| MILP | 2 | 3600 |

---

## 輸出結構（統一扁平，D3）

每個 stage 產物**直接寫入最終扁平資料夾**（D6 直寫終點），不留 `stages/` + `csharp/` 兩層中繼：

```text
Projects/<ProjectName>/
├── Model/<ProjectName>_Model.md # 數學模型（Stage 00-04 推導併入此檔）
├── Set/ # Set 積木 + Dataload
├── Parameter/ # Parameter 積木
├── Variable/ # Variable 積木
├── Constraint/ # Constraint 類別 + BuildConstraints
├── Objective/ # ObjectiveFunction
├── Program.cs # 入口
├── status.json # 進度追蹤
└── <ProjectName>.csproj # 複製 Template_CPLEX，DLL/Analyzer 相對 dlls/
```

### resume（context 重置後續跑，D6）

`status.json`：`{ "completed": ["00","01",...], "current": "05", "projectType": "MILP" }`

繼續時：讀 `status.json` 確認已完成 stage → 讀 `current` 所需前置檔當 context → 從 `current` 續跑，**不重跑**已完成的 stage。

---

## 生成 Code 命名規則（注入每個 Prompt）

paved path = Set 積木 + 泛型宣告（generator 自動生成 class body，完整見 `../../OptimFoundation/OptimFoundation/specs/2026-07-13-optset-basic-objects.md`）：

```csharp
[OptSet<DateTime>] public partial class Set_Date { } // 元素型別；[OptSet] 預設 string
[OptParam<Set_Date, Set_Group>] public partial class Parameter_ShiftDemand { } // → Date/Group/QTY + ctor
[OptVar<Set_Date, Set_Employee>] public partial class VariableB_ShiftAssign { } // 型別由前綴 B/X/I 決定
```

- property 名 = 積木類名去 `Set_` 前綴；泛型參數順序 = key 組成順序；`QTY` 永遠最後
- 前綴天條：`VariableB_`=Binary / `VariableX_`=Continuous / `VariableI_`=Integer（違反 → OPTF001）
- 字串式 `[OptVar("Date:DateTime")]` 為逃生口，永久保留
- 數值保真：所有數值與原始問題描述完全一致，NEVER 四捨五入 / 推算 / 佔位符

### Constraint 分類（Stage 02/04 標記）

| 類型 | 語言線索 |
|---|---|
| UB / LB | "at most" / "at least" / "no more than" |
| Balance | "must equal" / "total in = total out" |
| Proportional | "at least X times" / "in proportion to" |
| Conjunction | "only if all" / "must all be active" |
| Disjunction | "at least one of" |
| Exclusive XOR | "exactly one must be selected" |
| Implication | "if... then..." |
| Conditional Activation | "only if"（Big-M 條件啟動） |

---

## LHS/RHS 嚴格規則（天條，全文見 ../AGENTS.md）

- Model 左側的項 → `AddLHS(coef, variable)`；右側的項 → `AddRHS(value)`
- **NEVER** 移項 / 改號 / 合併化簡 / 翻轉比較方向
- 方向：`>=` → `CreateGreatEqual()`、`<=` → `CreateLessEqual()`、`=` → `CreateEqual()`
- 參數先 LINQ 查詢存變數再傳入（禁止把 LINQ 直接嵌入 `AddLHS(...)`）

## Prompt 實作指引

- **CoT 觸發**：每個 Prompt 含 "Think step by step" + 明確 Step 1→N + 輸出格式要求（JSON / code block / Markdown）
- **Context 傳入**：

  | Stage | 傳入 context |
  |---|---|
  | 09_ObjCode | Model + ParamCode + VarCode + DataloadCode + ConstraintCode |
  | 08_ConstraintCode | Model + ParamCode + VarCode + DataloadCode |
  | 07_DataloadCode | StandardModel + Model + ParamCode |
  | 07b_DataloadVerify | ProblemDescription + Model + ParamCode + DataloadCode |
  | 04b_ModelVerify | ProblemDescription + Model |

## 自動修復循環（Stage 14）

```text
dotnet build 失敗 → 擷取 compiler error → GetFixPrompt(error, failedCode) → LLM 修正 → 寫回
最多 5 次；全失敗 → 保留 error log
```

---

## 指標（不重複，一律引用）

- 天條（數值保真 / API 白名單 / 框架唯讀 / 相對路徑 / DLL 引用）→ [`../AGENTS.md`](../AGENTS.md)
- CPLEX API 簽名 → [`../CPLEX_API_REFERENCE.md`](../CPLEX_API_REFERENCE.md)
- solver 旋鈕全表 + tuning 策略 → [`../tuning/CLAUDE.md`](../tuning/CLAUDE.md)
- 建模基本物件（Set 積木 / SetBase / 三檔位讀取）→ OptimFoundation `specs/2026-07-13-optset-basic-objects.md`
