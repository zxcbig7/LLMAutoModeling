---
title: 新手接手開發 — 可見性、安全網、單一路徑
status: draft
created: 2026-07-21
updated: 2026-07-21
modules: [docs, tutorial, tooling, projects]
superseded_by: 2026-08-01-optimfoundation-dual-config.md, 2026-08-01-optimfoundation-runner-symmetry.md, 2026-08-01-optim-docs-and-projects-migration.md
---

# 新手接手開發：讓小白敢動、動了知道對不對

## Summary

讓一個沒碰過這個 repo 的人能接手開發，做法**不是**把 code 寫長寫白，而是三件事：把 generator 隱形的產物變可見、把已經寫在文件裡的驗收標準變可執行、把教材裡過期的規範表換成一條走得完的路。

核心判斷：**框架精神（模型是唯一真相、code 是機械轉譯、關掉自由發揮）與新手友善同向，不衝突。** 唯一的真實代價是「為了少打字而讓東西變隱形」。解法是給隱形的東西一個出口，不是把 code 變冗長。

## Motivation / Why

現況三個具體障礙（皆有實據）：

1. **看不見**：`Variable/VariableB_ShiftAssign.cs` 是空 class，屬性由 `[OptDim]` 編譯期生成。新手在 Constraint 看到 `new VariableB_ShiftAssign { Employee = e }` 會 Ctrl+Click 跳不過去。正解是**文件把 `[OptDim<Set_X>("Name")] → public string Name` 的對照直接寫出來**（教材已於 2026-07-21 補上 inline 範例）——NEVER 叫新手去讀機器產的 `.g.cs`，那比空 class 更難懂。`Generated/` 是 build 產物（generator debug 用），已 gitignore，不是教學材料。
2. **不敢動**：`Projects/` 零測試。`run.ps1` 只驗 build + 跑得動，不驗答案。解落在 `bin/**/Solution/*.csv`（gitignore），clean 即失。改壞目標值不會有人知道。
3. **被教錯路**：`tutorial/` 是唯一人類向材料，卻是全 repo 唯一未納入稽核的部分。其正典表教手寫繼承（無 `OPTF001`~`OPTF006` 編譯期防呆），而治理文件教 paved path——AI 走有護欄的路，人走沒護欄的路。

第 3 點的兩處已於 2026-07-21 修正（見 In Scope 項 1），本規格處理其餘。

## Scope

### In Scope

| # | 項目 | 相依 |
|---|---|---|
| 1 | ~~`tutorial/` 正典表與前綴語意修正（md + html 各一處）~~ **已完成 2026-07-21** | — |
| 2 | 教材把 `[OptDim] → 生成 property` 對照 inline 寫清楚（**已完成 2026-07-21**）；`Generated/` 從版控移除並 gitignore（**已完成**）——它是 generator debug 產物，非教學材料 | — |
| 3 | `Projects/expected.json` + `run.ps1 -Verify`：目標值回歸網 | — |
| 4 | 「新增限制式 Checklist」補 Step 0（先寫數學式）與結尾驗證步驟，API 對齊 paved path | 2, 3 |
| 5 | Constraint 逐項對照註記寫成規範，進 `claudemdTemplate/Constraint/CLAUDE.md` | — |
| 6 | 教材去規範化：正典表改為連結，改以一個 worked example 貫穿 | 2, 4 |
| 7 | `tutorial/` 納入日後稽核與 `/harness-eval` 範圍 | — |

### Out of Scope

- 改寫既有 8 個專案的 code 使其「更好懂」——語意風險 > 收益，且會破壞 Generator/Manual 兩版的逐行對照關係
- `Template_CPLEX/` 實碼的資料防護遷移（獨立議題，已在 2026-07-21 治理稽核中列為待決）
- 商用 CPLEX 授權造成的「無授權者仍無安全網」——結構性限制，僅在文件誠實標註

## User Stories

- 新手打開一個空 class，想知道屬性哪來的 → 讀教材的 `[OptDim] → property` inline 對照範例（不必翻任何生成檔）
- 新手改了一條限制式，想知道有沒有改壞 → `.\run.ps1 -Verify` → `8/8 green` 或指出哪個專案目標值變了
- 新手要做第一個貢獻 → 照 Checklist 從**寫數學式**開始，到**跑起來驗目標值**結束

## Acceptance Criteria

1. `Generated/` 全 repo 已從 git 移除並 gitignore（`**/Generated/`）；csproj 保留 `<Compile Remove="Generated/**/*.cs" />`（避免本機 build 撞名）
2. 教材（`tutorial/` + `claudemdTemplate/{Variable,Parameter}/CLAUDE.md`）用 inline 範例講清 `[OptDim<Set_X>("Name")] → public 型別 Name` 的對照，NEVER 指向 `.g.cs`
3. `run.ps1 -Verify` 對 4 個 LP 專案（GlassFactory / WeeniesBuns / ClinicVitamin / SandwichProduction）秒級回報 pass/fail
4. `expected.json` 每筆有 `source` 欄位指回 `Model.md:行號`
5. 斷言目標值，NEVER 斷言變數組合（依 `WoodworkingShop_Model.md:96` 已寫明的判準——存在多重最佳解時變數組合會 flaky）
6. 修正後的 Checklist 第一步是「在 `Model/<Name>_Model.md` 寫下數學式並編號 `[Cn]`」，最後一步是「跑起來確認目標值變化符合預期」
7. `tutorial/` 全文 grep 無規範性斷言與治理文件衝突（用 2026-07-21 的 `ground-truth-api.md` 判準複驗）

## Data Model

`Projects/expected.json`：

```json
{
  "MaxWeightIndependentSet": {
    "objective": 1332,
    "tol": 0,
    "tier": "slow",
    "source": "Model/MaxWeightIndependentSet_Model.md:11"
  },
  "WoodworkingShop": {
    "objective": 72000,
    "tol": 1e-6,
    "tier": "no-code",
    "source": "Model/WoodworkingShop_Model.md:96"
  }
}
```

`tier`：`fast`（LP，pre-commit 預設）｜`slow`（MILP，需 `timeLimit`，nightly）｜`no-code`（有模型無實作，跳過）

## 修正後的「新增限制式 Checklist」（取代 `Template_CPLEX/CLAUDE.md` 現行版本）

```text
0. Model/<Name>_Model.md 寫下數學式，編號 [Cn]，標注語意與涉及索引   ← 現行版缺這步
1. 需要新變數才建：光桿 [OptVar] + 逐維 [OptDim<Set_X>("Name")]
2. VariableCreate.Build() 加 BuildVars<T>()（需自訂 bounds 才用 Build*Vs）
3. Constraint/Constraint_Xxx.cs 繼承 ConstraintBase，逐項 AddLHS/AddRHS
   每行加對照註記指向 [Cn] 的哪一項
4. Constraint/BuildModel.Build() 掛上 new Constraint_Xxx(dataload, engine).Build()
5. 有罰分 → Objective 加 AddLHS(penalty, var)，penalty 進 Parameter（NEVER 裸數字）
6. run.ps1 -Verify 跑起來，確認目標值變化符合預期                    ← 現行版缺這步
```

現行版本從第 1 步開始、停在第 5 步，等於教新手**跳過數學模型直接寫 code、寫完不驗**——與框架「模型是唯一真相、code 是機械轉譯」的精神直接相反。

## Edge Cases & Error Handling

- **`expected.json` 與 `Model.md` 分歧**：接受這份重複（最佳值極少變，變的時候正是要人停下來看的時刻）。`source` 欄位是分歧時的裁決依據——`Model.md` 為真相，`expected.json` 為執行版
- **MILP 跑太久**：`tier: slow` 不進 pre-commit；未實測耗時，執行時 MUST 實測後再定 `timeLimit`
- **無 CPLEX 授權**：`-Verify` 應明確報「授權缺失，無法驗證」而非靜默通過
- **多重最佳解**：只斷言目標值（見 Acceptance Criteria 5）
- **`Generated/` 誤入編譯**：csproj 漏 `<Compile Remove>` → 第二次 build 撞名炸（`AGENTS.md:40` 已載明）

## 通用鐵則

- NEVER 為了「看起來清楚」把 generator 改回手寫——會失去 `OPTF001`~`OPTF006` 編譯期防呆，這才是真的傷框架精神
- NEVER 把逐項 `AddLHS`/`AddRHS` 合併成一行 LINQ——逐項對照消失即違反 NEVER 移項的天條
- 教材可以有**範例**，不該有**規範表**——範例過期只是舊，規範表過期是說謊
