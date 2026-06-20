# HospitalRosteringProblem_new — Registry 架構（完整模型）

把 `GenericProblem` 從「繼承 / 寫死流程」改成 **Registry（Composition over Inheritance）**：
模型內容在 `Execute()` **之前**用註冊的方式組裝進去，`Execute()` 對任何具體 model class 零依賴。

本專案接的是**完整 Hospital Rostering 模型**（9 組變數 + 目標式 + 10 條限制式），
與原 `RosteringProblem` 產生的模型逐一對應，結果一致。

## 核心概念

| 角色 | 檔案 | 職責 |
|---|---|---|
| 框架層 | `OptModel.cs` | library-grade 求解管線，namespace `OptimFoundation.Cplex`（等同它將來在 foundation DLL 內的樣子）。只提供 `UseConfig` / `AddVariables` / `AddModel` / `OnSolved` 註冊 API + 固定的 `Execute()` 流程。**永不需修改。** |
| 組裝點 | `Program.cs` | Composition Root：註冊變數/約束/目標式，順序對應原 `VariableCreate` / `BuildModel`。 |
| 模型物件 | `Data/`、`VariablesClass/`、`Constraints/` | 與原專案**逐字相同**，一行都沒改。 |

## 關鍵技術：delegate（`Action<OptEngine>`）

```csharp
.AddModel(e => new Constraint_FullfillDemand(data, e).Build())
```

- **延遲執行**：註冊時只是把這段程式碼「存起來」，等到 `Execute()` 拿到真正的 `OptEngine` 才跑。
- **closure**：lambda 記住外部的 `data`，所以 `Execute()` 不需要知道 `Dataload` 長怎樣。
- **吸收異質簽名**：即使每個 Constraint 建構子參數不同，全被包進 `Action<OptEngine>`，
  不需要統一介面、不需要改 model class。

## 與原 RosteringProblem 的驗證結果

跑 `dotnet run` 比對，模型結構**完全一致**：

| 項目 | 原專案 | _new |
|---|---|---|
| Constraint_FullfillDemand | 124 | 124 |
| Constraint_OneGroup | 496 | 496 |
| Constraint_PreAssign | 4 | 4 |
| Constraint_SixDayWork | 2912 | 2912 |
| Constraint_NightToDay | 3840 | 3840 |
| Constraint_OffOneDay | 464 | 464 |
| Constraint_CrossGroup | 1488 | 1488 |
| Constraint_BelowAVG | 16 | 16 |
| Constraint_WeekendLT4 | 16 | 16 |
| Constraint_DoubleOffLT2 | 496 | 496 |
| Reduced MIP | 1585 rows, 3429 cols, 8366 nonzeros | 同左 |
| Status | Optimal | Optimal |

> **目標值會跳動**（_new: 1.6/1.9/1.8；原版: 2.3/1.5/2.1）是 `epGap=0.03`（3% gap 容忍）
> 搭配 `workThreads=10` 的求解器非決定性，**兩支程式都一樣會跳**，與重構無關。
> 若要 bit-identical 的目標值，設 `workThreads = 1` 且 `epGap = 0`。

## 執行

```powershell
dotnet run
```
