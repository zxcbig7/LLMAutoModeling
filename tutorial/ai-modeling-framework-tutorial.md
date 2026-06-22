# AI 自動建模框架教學：Modeling → Coding → Tuning

把「自然語言最佳化問題」變成「可執行的 OptimFoundation CPLEX C# 專案」的整體框架。
分三階段：**Modeling**（建數學模型）、**Coding**（生成 C# 專案）、**Tuning**（調校求解）。
本筆記說明各階段的職責、產物、慣例與銜接方式，不綁定特定問題。

## 整體流程

```
自然語言描述
   │
   ▼  Modeling（Stage 0–4b）
   │   問題分類 → 結構化 → 標準化 → KeyInfo(JSON) → AML 數學模型 → 驗證
   ▼  Coding（Stage 5–14）
   │   Parameters → Variables → Dataload → Constraints → Objective
   │   → VariableCreate → BuildModel → Project → Program → build & fix
   ▼  Tuning
   │   正確性 gate → 模型優化（優先）→ solver 旋鈕
   ▼
最佳解 + 解值輸出（CSV）
```

三階段是「正確性逐層收斂」：Modeling 確保數學意義對、Coding 確保能編譯能跑、Tuning 確保跑得快且收斂。**前一層沒過，不進下一層。**

## 階段對照總表

| Phase | Stage | 產物 | 重點 |
|---|---|---|---|
| Modeling | 0 Classify | ProblemType（LP/IP/MILP） | 決定 CplexConfig 預設 |
| Modeling | 1 SimpleModel | 結構化自然語言 | 抽出決策、目標、限制 |
| Modeling | 2 StandardModel | 標準化描述 + 約束分類 | 統一術語 / 量綱 |
| Modeling | 3 KeyInfo | JSON（Sets/Params/Vars/Obj/Constraints） | 機器可讀骨架 |
| Modeling | 4 AMLModel | AMPL-style Markdown（數學式用 LaTeX） | 可審閱中間產物 |
| Modeling | 4b AMLVerify | 修正後 AML | 驗線性性、命名、完整性 |
| Coding | 5–6 | Parameters.cs / Variables.cs | 類別宣告 |
| Coding | 7、7b | Dataload.cs（+ 驗證） | 數值保真 |
| Coding | 8–9 | Constraints.cs / ObjectiveFunction.cs | Pool 機制建模 |
| Coding | 10–11 | VariableCreate.cs / BuildModel.cs | 建變數、組裝 |
| Coding | 12–13 | Project / Program | 設定、求解、輸出 |
| Coding | 14 FixCode | 修正後 .cs | build 失敗循環修復（≤5） |
| Tuning | — | 調校後設定 / 模型 | 先模型後參數 |

> Modeling 的 **Stage 4（AML）** 與 Coding 的 **Stage 7b（Dataload 驗證）** 是兩個「暫停確認」關卡——數學結構與數值是兩個最易出錯處，要先讓人確認再往下。

---

## Part 1：Modeling（Stage 0–4b）

目標：自然語言 → 嚴謹、無歧義、可被機器解析的數學模型。

### Stage 0：問題分類

分成 LP / IP / MILP，直接影響 solver 預設：

| ProblemType | 變數型態 | timeLimit | mipEmphasis |
|---|---|---|---|
| LP | 全連續 | 300 | 0（平衡） |
| IP | 全整數 | 1800 | 1（重可行解） |
| MILP | 混合 | 3600 | 2（重最佳解） |

### Stage 1–3：逐步結構化

- **SimpleModel**：用結構化自然語言列出「決策 / 目標 / 限制 / 已知數據」。
- **StandardModel**：統一術語與量綱，並將每條約束分類（見下表）。
- **KeyInfo**：抽成 JSON 骨架（Sets、Parameters、Variables、Objective、Constraints），供後續機器解析。

### Stage 4：AML 數學模型

用 AMPL 命名慣例 + Markdown 撰寫，**數學式一律用 LaTeX**。包含 Sets / Parameters / Decision Variables / Objective / Constraints 五區塊。命名規則：

- 所有 identifier 用 PascalCase（`ProductIndex`、`TotalCost`），不隨意複數化。
- Constraint 名稱即未來的 `Constraint_<Name>` 類別名。

線性化是硬規則——禁止 `x*y`、`x/y`、`abs/min/max/if`；比例與邏輯條件要先線性化（Big-M、比例上下界）。

### 約束分類系統

分類影響後續 code 的建模手法：

| 類型 | 語言線索 |
|---|---|
| UB / LB | "at most" / "at least" / "no more than" |
| Balance | "must equal" / "total in = total out" |
| Proportional | "in proportion to" / "at least X times" |
| Conjunction | "only if all" |
| Disjunction | "at least one of" |
| Exclusive XOR | "exactly one of" |
| Implication | "if A then B" |
| Conditional Activation | "only if" / Big-M 條件啟動 |

### Stage 4b：AML 驗證

對照原始描述檢查線性性、命名一致、Sets/Params/Vars 是否齊全，輸出修正後 AML。**這是進入 Coding 前的最後數學關卡。**

---

## Part 2：Coding（Stage 5–14）

目標：AML → 可編譯、可執行的 OptimFoundation CPLEX C# 專案。

### 生成的專案結構

```
ProjectName/
├── Parameter/
│   └── Parameter_Xxx.cs       一個 Parameter 一個檔（數值欄位 QTY）
├── Set/
│   └── Dataload.cs            Sets 由 Parameters 衍生、資料載入、WriteToCSV
├── Variable/
│   ├── VariableB_/I_/X_Xxx.cs 決策變數類別
│   └── VariableCreate.cs      BuildBVs / BuildIVs / BuildCVs
├── Objective/
│   └── ObjectiveFunction.cs   目標式
├── Constraint/
│   ├── Constraint_Xxx.cs      一條限制式一個檔
│   └── BuildModel.cs          依序呼叫所有 Build()
├── Model/                     數學模型文件（.md）
├── ExperimentRunner.cs        參數掃描（tuning，dotnet run -- experiment）
└── Program.cs                 唯一進入點（Fluent OptModel；solve / experiment 雙模式）
```

> 資料夾命名以 master `claudemdTemplate/` 為準。舊版 `Data/`→`Set/`、`VariablesClass/`→`Variable/`、`Constraints/`→`Constraint/`。**預設**用框架的 Fluent `OptModel` + `[OptVar]`/`[OptParam]` source generator；手寫 `XxxProblem.cs` + 手寫 class 為**後路**（兩架構並排見 `tutorial/index.html` §5.8、`Projects/HospitalRostering_Manual`）。

### 類別命名與建構慣例

| 元素 | 前綴 | 繼承 | 建構方式 | 建立 API |
|---|---|---|---|---|
| 參數 | `Parameter_` | `ParameterBase` | 只宣告 properties，無建構子；object initializer | — |
| 連續變數 | `VariableX_` | `VariableBase` | 同上（無資料欄位） | `BuildCVs<T>()` |
| 整數變數 | `VariableI_` | `VariableBase` | 同上 | `BuildIVs<T>()` |
| 二元變數 | `VariableB_` | `VariableBase` | 同上 | `BuildBVs<T>()` |
| 限制式 | `Constraint_` | `ConstraintBase` | `(Dataload, OptEngine)`，`public void Build()` | Pool API |
| 目標式 | `ObjectiveFunction` | — | `(Dataload, OptEngine)` | `CreateMinimize/Maximize` |

關鍵慣例：

- Parameter / Variable **只宣告 properties，不寫建構子**；index 屬性用 PascalCase（對應 AML set 名），Parameter 的數值欄位固定 `public double QTY`，放最後。
- 建立實例一律用 **object initializer**：`new VariableB_X { Date = d, Group = g }`。
- 變數型別由 `Build*Vs<T>` 決定，前綴只是命名約定（`B`→`BuildBVs`、`I`→`BuildIVs`、`X`→`BuildCVs`）。
- `Constraint.Build()` 是 `public void Build()`，**不是 `override`**（`ConstraintBase.Build` 非 virtual）。

```csharp
public class Parameter_ShiftDemand : ParameterBase
{
    public DateTime Date { get; set; }
    public string Group { get; set; }
    public double QTY { get; set; }
}

public class VariableB_ShiftAssign : VariableBase
{
    public DateTime Date { get; set; }
    public string Employee { get; set; }
    public string Group { get; set; }
}
```

### Pool 機制：AddLHS / AddRHS / Create*

限制式與目標式都用「Pool」累積項，再一次建立：

```
AddLHS(coef, varObj)   ← 左側變數項
AddLHS(constant)       ← 左側常數項（若有）
AddRHS(value)          ← 右側常數
AddRHS(coef, varObj)   ← 右側變數項（若有）
CreateLessEqual(name)  ← <=   建立並清空 Pool
CreateGreatEqual(name) ← >=
CreateEqual(name)      ← =
```

**不變式（最重要的硬規則）**：

- AML 左側的項 → `AddLHS`；右側的項 → `AddRHS`。
- 嚴禁移項、改號、合併化簡、翻轉比較方向。
- 參數值先用 LINQ 查進區域變數，再傳入 `AddLHS/AddRHS`（不可把 LINQ 內嵌進參數）。

```csharp
public void Build()
{
    dataload.Set1.ForEach(i =>
    {
        var coef = dataload.parameter_Coef
            .FirstOrDefault(x => x.Set1 == i)?.QTY ?? 0.0;
        engine.AddLHS(coef, new VariableX_Amount { Set1 = i });

        engine.AddRHS(rhsValue);
        engine.CreateLessEqual($"{ConstraintName}@{i}");
        ConstraintCount++;
    });
    Logging.Info($"[{ConstraintName}] {ConstraintCount}");
}
```

### Dataload：數值保真

所有數值必須與原始問題描述**完全一致**——不四捨五入、不推算、不填佔位符。Sets 與 Parameters 全部要有資料，無空集合。Scalar 參數也用 `List<Parameter_X>`，只放一筆。

```csharp
ProductIndex.AddRange(new[] { "Condos", "DetachedHouse" });
parameter_Budget.Add(new Parameter_Budget { QTY = 760000.0 });
parameter_Profit.Add(new Parameter_Profit { ProductIndex = "Condos", QTY = 0.5 });
```

### VariableCreate / BuildModel / Program（Fluent OptModel）

`VariableCreate` 與 `BuildModel` 是兩個建模 step class，被 solve 與 experiment 兩模式**共用**：

```csharp
// VariableCreate：依型別建立所有變數
optEngine.BuildBVs<VariableB_ShiftAssign>(dataload.Date, dataload.Employee, dataload.Group);
optEngine.BuildCVs<VariableX_BelowAVG>(dataload.Employee);
```

composition root **預設**用框架內建的 Fluent `OptModel`（手寫 `XxxProblem.Execute()` 為後路，見 `Projects/HospitalRostering_Manual`）：

```csharp
// Program.cs — 唯一進入點
if (args.Contains("experiment")) { ExperimentRunner.Run(); return; }

var dataload = new Dataload();
using (var m = new OptModel("ProjectName")
    .UseConfig(() => new CplexConfig { epGap = 0.01, timeLimit = 300, workThreads = 8, enableLog = true, exportSol = true })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
}
```

- `OptModel` 保證 `AddVariables` 先於 `AddModel`，內建 build/solve 計時與整體時間 log，並自行 Dispose engine。
- `BuildModel.Build()` 內固定順序：先 `ObjectiveFunction`，再各 `Constraint_Xxx`。
- Tuning 走 `dotnet run -- experiment` → `ExperimentRunner`（見 Part 3 與 `claudemdTemplate/Experiment/CLAUDE.md`）。

### Stage 14：build & fix 循環

```
dotnet build → 失敗 → 擷取 compiler error → Fix Prompt 修正對應 .cs → 重試（最多 5 次）
              → 5 次全失敗 → 標記 Failed，保留 error log
```

---

## Part 3：Tuning

**黃金順序：先做模型優化，再調 solver 參數。** 模型改一刀的效益通常大於調十個旋鈕。
**正確性優先：** 未通過「正確性 gate」（解 / 目標值對得上問題描述）禁止進效能 tuning。

### 三類觸發與入口

| 觸發 | 線索 | 動作入口 |
|---|---|---|
| structure（數學結構） | 加/刪約束、改 Big-M、加 valid inequality | 回 Modeling（Stage 4）重跑 |
| data（數值） | 改參數值 | 改 Dataload |
| solver（求解參數） | timeout / gap 過大 / 太慢但模型正確 | 調 `CplexConfig` 旋鈕 |

### 模型層優化（最高優先）

- **變數型態**：盡量用自由度最高者，求解難度 CV ≪ IV < BV；能線性化或取整就用連續。
- **模型精簡**：聚合同型約束、刪冗餘約束、收緊變數上下界、Big-M 取最小可行值。
- **對稱性消除**：對等價變數加排序約束，打斷等價分支。
- **初始解（warm start）**：高品質 incumbent 可大幅減少 B&B 節點。

### Solver 層調校（CplexConfig 旋鈕）

`CplexConfig` 欄位 `null` = 用 CPLEX 預設，tuning 只設要動的。常用決策：

| 症狀 | 動作 |
|---|---|
| timeout 但正確 | 提 `timeLimit` / `mipEmphasis=1` / 開 `parallelMode`；連續放寬仍 timeout → 回 structure |
| gap 過大 | 收 `epGap` / `mipEmphasis=2` / 加 cuts（`gomoryCuts`/`mirCuts`） |
| 記憶體爆 | `treeMemoryLimit` + `nodeFileInd=2/3` |
| 要可重現實驗 | `parallelMode=1` + 固定 `randomSeed` + `detTimeLimit` |
| 數值不穩 | `numericalEmphasis=true` |

> 完整旋鈕全表與 Foundation 尚未提供的接口，見同層的 tuning 策略文件（`truning/CLAUDE.md`）。

### Tuning 流程

```
基準（無 tuning，僅 timeLimit）→ 單一變數測試 → 篩選有效 → 疊加組合 → 有改進則更新基準
```

每輪記錄：obj、gap、wall/det 時間、node 數、變更項。

---

## 三階段如何銜接

- **進度追蹤**：每個 stage 產物立刻存檔；`status.json` 記已完成 stage，context 重置後可從 `current` resume，不重跑。
- **分類貫穿**：Stage 0 的 ProblemType 一路影響到 Project 的 `CplexConfig` 預設。
- **知識累積**：求解成功後把最終 AML 回寫 `docs/<ProjectName>.md`，進 RAG 索引，供後續相似問題檢索。

---

## 常見錯誤排查

| 錯誤訊息 | 原因 | 解法 |
|---|---|---|
| `KeyNotFoundException: 找不到變數` | `AddLHS/AddRHS` 的變數 key 與 `Build*Vs` 建立時不符 | 確認 property 宣告順序與 `Build*Vs` 傳入 set 順序一致 |
| `NullReferenceException`（LINQ 查 Parameter） | `FirstOrDefault` 回 null | 確認資料已初始化，並用 `?.QTY ?? 0.0` |
| `缺少建構子` / `ArgumentException 參數數量不符` | Variable/Parameter 加了非 property 成員或誤呼叫建構子 | 只留 properties，用 object initializer 建立 |
| 編譯：`沒有可覆寫的方法` | `Constraint.Build()` 寫了 `override` | 改成 `public void Build()`（非 virtual） |
| CPLEX 1217 No solution（Infeasible） | 模型不可行 | 先查 LHS/RHS 是否移項錯；用 IIS 衝突分析定位；暫拿掉部分約束驗證基礎模型 |
| 求解後 `varCount == 0` | `Build*Vs` 傳入空 Set | 確認 Dataload 建構子內 Set 有正確 `Add/AddRange` |

---

## API 速查

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;

// 建變數
engine.BuildCVs<VariableX_Name>(setA, setB);          // 連續
engine.BuildIVs<VariableI_Name>(setA);                // 整數
engine.BuildBVs<VariableB_Name>(setA, setB);          // 二元
engine.BuildCVs<VariableX_Name>(lb, ub, setA);        // 自訂界限

// 建約束 / 目標（Pool）
engine.AddLHS(coef, new VariableX_Name { Set = s });
engine.AddRHS(value);
engine.CreateLessEqual("Name");                       // 或 CreateGreatEqual / CreateEqual
engine.CreateMinimize();                              // 或 CreateMaximize

// 求解與讀解
engine.Build();
bool ok = engine.Solve();
double obj = engine.GetObjectiveValue();
var sol = engine.GetSetVarSol<VariableX_Name>();      // List<T>，只含值 > 0 的變數
engine.Dispose();
```
