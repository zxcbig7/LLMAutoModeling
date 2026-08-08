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
   │   → Program 三階段模型定義 → Project/Experiment runner → build & fix
   ▼  Tuning
   │   正確性 gate → 模型優化（優先）→ solver 旋鈕
   ▼
最佳解 + 解值輸出（CSV）
```

三階段是「正確性逐層收斂」：Modeling 確保數學意義對、Coding 確保能編譯能跑、Tuning 確保跑得快且收斂。**前一層沒過，不進下一層。**

## 階段對照總表

| Phase    | Stage           | 產物                                                             | 重點                     |
| -------- | --------------- | ---------------------------------------------------------------- | ------------------------ |
| Modeling | 0 Classify      | ProblemType（LP/IP/MILP）                                        | 決定 CplexConfig 預設    |
| Modeling | 1 SimpleModel   | 結構化自然語言                                                   | 抽出決策、目標、限制     |
| Modeling | 2 StandardModel | 標準化描述 + 約束分類                                            | 統一術語 / 量綱          |
| Modeling | 3 KeyInfo       | JSON（Sets/Params/Vars/Obj/Constraints）                         | 機器可讀骨架             |
| Modeling | 4 AMLModel      | AMPL-style Markdown（數學式用 LaTeX）                            | 可審閱中間產物           |
| Modeling | 4b AMLVerify    | 修正後 AML                                                       | 驗線性性、命名、完整性   |
| Coding   | 5–6             | Parameters.cs / Variables.cs                                     | 類別宣告                 |
| Coding   | 7、7b           | Dataload.cs（+ 驗證）                                            | 數值保真                 |
| Coding   | 8–9             | Constraints.cs / ObjectiveFunction.cs                            | Pool 機制建模            |
| Coding   | 10–11           | Program.cs 的 `AddVariables` / `AddObjective` / `AddConstraints` | 建變數、按階段組裝       |
| Coding   | 12–13           | `ProjectConfig` / `CplexConfig` + runners                        | 設定、求解、實驗、輸出   |
| Coding   | 14 FixCode      | 修正後 .cs                                                       | build 失敗循環修復（≤5） |
| Tuning   | —               | 調校後設定 / 模型                                                | 先模型後參數             |

> Modeling 的 **Stage 4（AML）** 與 Coding 的 **Stage 7b（Dataload 驗證）** 是兩個「暫停確認」關卡——數學結構與數值是兩個最易出錯處，要先讓人確認再往下。

---

## Part 1：Modeling（Stage 0–4b）

目標：自然語言 → 嚴謹、無歧義、可被機器解析的數學模型。

### Stage 0：問題分類

分成 LP / IP / MILP，直接影響 solver 預設：

| ProblemType | 變數型態 | timeLimit | mipEmphasis   |
| ----------- | -------- | --------- | ------------- |
| LP          | 全連續   | 300       | 0（平衡）     |
| IP          | 全整數   | 1800      | 1（重可行解） |
| MILP        | 混合     | 3600      | 2（重最佳解） |

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

| 類型                   | 語言線索                                |
| ---------------------- | --------------------------------------- |
| UB / LB                | "at most" / "at least" / "no more than" |
| Balance                | "must equal" / "total in = total out"   |
| Proportional           | "in proportion to" / "at least X times" |
| Conjunction            | "only if all"                           |
| Disjunction            | "at least one of"                       |
| Exclusive XOR          | "exactly one of"                        |
| Implication            | "if A then B"                           |
| Conditional Activation | "only if" / Big-M 條件啟動              |

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
│   └── VariableB_/I_/X_Xxx.cs 決策變數類別
├── Objective/
│   └── ObjectiveFunction.cs   目標式
├── Constraint/
│   └── Constraint_Xxx.cs      一條限制式一個檔
├── Model/                     數學模型文件（.md）
└── Program.cs                 唯一組裝點（模型定義 + project / experiment runners）
```

> 資料夾命名以 master `claudemdTemplate/` 為準。舊版 `Data/`→`Set/`、`VariablesClass/`→`Variable/`、`Constraints/`→`Constraint/`。一律用框架的 Fluent `OptModel` + `[OptVar]`/`[OptParam]` source generator；手寫 `XxxProblem.cs` + 手寫 class **已廢止**，`Projects/HospitalRostering_Manual` 僅供理解舊 code。

新專案只從 `AI-Modeling/Template/` 建立。`OptimFoundation/OptimFoundation/Templates/` 是 framework integration／相容性案例，不是 scaffold；其中的 `ProjectReference`、舊資料夾名、手寫 class 或不同入口不能反向定義 canonical 寫法。手寫 `VariableBase` 路線已廢止，generator 是唯一 paved path。

### 類別命名與建構慣例

| 元素     | 前綴                | 唯一寫法（paved path）                                                    | 舊 code 可能看到（已廢止） | 建立 API                  |
| -------- | ------------------- | ------------------------------------------------------------------------- | -------------------------- | ------------------------- |
| 集合     | `Set_`              | `[OptSet<T>] partial class`，元素型別顯式寫出                             | —                          | `Load(source, "Set_X")`   |
| 參數     | `Parameter_`        | 光桿 `[OptParam]` + 逐維 `[OptDim<Set_X>("Name")]`                        | 手寫繼承 `ParameterBase`   | —                         |
| 連續變數 | `VariableX_`        | 光桿 `[OptVar]` + 逐維 `[OptDim<Set_X>("Name")]`                          | 手寫繼承 `VariableBase`    | `BuildVars<T>()`          |
| 整數變數 | `VariableI_`        | 同上                                                                      | 同上                       | `BuildVars<T>()`          |
| 二元變數 | `VariableB_`        | 同上                                                                      | 同上                       | `BuildVars<T>()`          |
| 限制式   | `Constraint_`       | 繼承 `ConstraintBase`；ctor 只收所需 Set / Parameter / 界限與 `OptEngine` | —                          | Pool API                  |
| 目標式   | `ObjectiveFunction` | ctor 只收所需 Set / Parameter / 權重與 `OptEngine`                        | —                          | `CreateMinimize/Maximize` |

**預設走 generator**：class body 留空，編譯期由 `AutoSetsGenerator` 補完 —— 樣板最省，且有 `OPTF001`~`OPTF006` 一整組編譯期防呆（前綴錯、attribute 漏掛、Set 型別非法都會直接 build 失敗）。手寫路徑沒有這層保護，只在 generator 不適用時才用。

**attribute 展開成什麼**（你寫左邊，generator 補出右邊，所以 class body 才能留空）：

```csharp
// 你寫的（宣告殼）
[OptVar]
[OptDim<Set_Employee>("Employee")]
[OptDim<Set_Date>("Date")]
public partial class VariableB_Assign { }

// generator 依 [OptDim] 補出的（等價於手寫這些 property）
public partial class VariableB_Assign : VariableBase
{
    public string Employee { get; set; } = string.Empty;   // ← [OptDim<Set_Employee>("Employee")]
    public DateTime Date { get; set; }                       // ← [OptDim<Set_Date>("Date")]
}
```

所以 Constraint 裡 `new VariableB_Assign { Employee = e, Date = d }` 的 `Employee`/`Date`，就是上面 `[OptDim]` 的第二個字串參數——property 名 = `[OptDim]` 宣告名。

關鍵慣例：

- Parameter / Variable **只宣告 properties，不寫建構子**；index 屬性用 PascalCase（對應 AML set 名），Parameter 的數值欄位固定 `public double QTY`，放最後。
- 建立實例一律用 **object initializer**：`new VariableB_X { Date = d, Group = g }`。
- **變數型別由類別名前綴決定**，前綴不是命名約定而是語意的一部分：`BuildVars<T>` 依前綴推導型別（`VariableB_`→Binary、`VariableX_`→Continuous、`VariableI_`→Integer），前綴不在這三者內直接編譯失敗（`OPTF001`）。Binary 只能用 `BuildBVs<T>(sets...)`，固定 `[0,1]`，沒有 bounds overload；只有 Continuous / Integer 能用 `BuildCVs<T>(lb, ub, sets...)` / `BuildIVs<T>(lb, ub, sets...)`。
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
    foreach (var i in items)
    {
        var coef = coefficients
            .FirstOrDefault(x => x.Set1 == i)?.QTY ?? 0.0;
        engine.AddLHS(coef, new VariableX_Amount { Set1 = i });

        engine.AddRHS(rhsValue);
        engine.CreateLessEqual($"{ConstraintName}@{i}");
    }
}
```

`EngineBase` 在每次 `CreateXxx` 自動統計限制式群組與數量；`ConstraintCount` 已 obsolete，Constraint class 不再自行計數或輸出建立摘要。

### Dataload：數值保真

所有數值必須與原始問題描述**完全一致**——不四捨五入、不推算、不填佔位符。Sets 與 Parameters 全部要有資料，無空集合。Scalar 參數也用 `List<Parameter_X>`，只放一筆。

```csharp
ProductIndex.AddRange(new[] { "Condos", "DetachedHouse" });
parameter_Budget.Add(new Parameter_Budget { QTY = 760000.0 });
parameter_Profit.Add(new Parameter_Profit { ProductIndex = "Condos", QTY = 0.5 });
```

### Program：三階段模型定義與兩種 runner

`Program.cs` 直接登記三個模型階段；不另開只做轉呼叫的包裝層：

```csharp
// Program.cs — 唯一進入點
var data = OptData.Load(() => new Dataload());
var projectConfig = new ProjectConfig
{
    ProjectName = "ProjectName",
    EnableSolverLog = true,
    ExportSol = true,
    DataId = "ProjectName",
    UserId = "SYSTEM",
};
var baseline = new CplexConfig { epGap = 0.01, timeLimit = 300, workThreads = 8 };

var model = new OptModel("baseline-model")
    .AddVariables(e => e.BuildBVs<VariableB_ShiftAssign>(data.Date, data.Employee, data.Group))
    .AddVariables(e => e.BuildCVs<VariableX_BelowAVG>(data.Employee))
    .AddObjective(e => new ObjectiveFunction(e, data.Date, data.Employee, data.Penalties).Build())
    .AddConstraints(e => new Constraint_FullfillDemand(e, data.Date, data.Employee, data.Group, data.parameter_ShiftDemand).Build())
    .AddConstraints(e => new Constraint_OneGroup(e, data.Date, data.Employee, data.Group).Build());

if (args.Contains("experiment"))
{
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;
    new OptExperiment("project-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis", emphasis)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => baseline)
    .OnSolved(e => data.WriteToCSV(e));
bool ok = project.Execute();
```

- `OptModel` 是純模型定義；框架固定執行 variables → objective → constraints，與註冊順序無關。
- `OptProject` 是單次求解 runner，接受兩層 config 並提供 `OnSolved`。
- `OptExperiment` 讓同一模型搭配 clone 後的多組 solver 設定；預設 log / 匯出 OFF 且沒有 `OnSolved`。
- `OptData.Load` 驗證後凍結框架受控 mutation API。直接 public field / 可變 `List` 寫入不保證立即攔截，callback 仍 MUST 把資料視為唯讀。
- 每個 variable family、Objective、每條 Constraint 各占一個 fluent call；NEVER 用純轉呼叫 helper 或 block lambda 隱藏 composition。Objective / Constraint constructor 統一以 `OptEngine` 為第一個參數。
- `ProjectConfig.DataId` / `UserId` 是 solution metadata 預設；現行 `CsvCtrl.WriteSolution` 仍由呼叫端明確傳入。
- 同名 experiment 是 append；`Run()` 回傳的 `result.Trials` 已包含 `Save()` 合併回來的歷史。只處理本輪時使用唯一 experiment name。

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

| 觸發                  | 線索                                     | 動作入口                   |
| --------------------- | ---------------------------------------- | -------------------------- |
| structure（數學結構） | 加/刪約束、改 Big-M、加 valid inequality | 回 Modeling（Stage 4）重跑 |
| data（數值）          | 改參數值                                 | 改 Dataload                |
| solver（求解參數）    | timeout / gap 過大 / 太慢但模型正確      | 調 `CplexConfig` 旋鈕      |

### 模型層優化（最高優先）

- **變數型態**：盡量用自由度最高者，求解難度 CV ≪ IV < BV；能線性化或取整就用連續。
- **模型精簡**：聚合同型約束、刪冗餘約束、收緊變數上下界、Big-M 取最小可行值。
- **對稱性消除**：對等價變數加排序約束，打斷等價分支。
- **初始解（warm start）**：高品質 incumbent 可大幅減少 B&B 節點。

### Solver 層調校（CplexConfig 旋鈕）

`CplexConfig` 欄位 `null` = 用 CPLEX 預設，tuning 只設要動的。常用決策：

專案名、保留期、solver log 與 LP/MPS/Sol 匯出屬 `ProjectConfig`，不是 solver 旋鈕，也不應進實驗的 `ConfigSnapshot`。

| 症狀           | 動作                                                                                    |
| -------------- | --------------------------------------------------------------------------------------- |
| timeout 但正確 | 提 `timeLimit` / `mipEmphasis=1` / 開 `parallelMode`；連續放寬仍 timeout → 回 structure |
| gap 過大       | 收 `epGap` / `mipEmphasis=2` / 加 cuts（`gomoryCuts`/`mirCuts`）                        |
| 記憶體爆       | `treeMemoryLimit` + `nodeFileInd=2/3`                                                   |
| 要可重現實驗   | `parallelMode=1` + 固定 `randomSeed` + `detTimeLimit`                                   |
| 數值不穩       | `numericalEmphasis=true`                                                                |

> 完整旋鈕全表與 Foundation 尚未提供的接口，見同層的 tuning 策略文件（`../.claude/rules/Ph3_Tuning/cplex-tuning-strategy.md`）。

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

| 錯誤訊息                                        | 原因                                                  | 解法                                                                     |
| ----------------------------------------------- | ----------------------------------------------------- | ------------------------------------------------------------------------ |
| `KeyNotFoundException: 找不到變數`              | `AddLHS/AddRHS` 的變數 key 與 `Build*Vs` 建立時不符   | 確認 property 宣告順序與 `Build*Vs` 傳入 set 順序一致                    |
| `NullReferenceException`（LINQ 查 Parameter）   | `FirstOrDefault` 回 null                              | 確認資料已初始化，並用 `?.QTY ?? 0.0`                                    |
| `缺少建構子` / `ArgumentException 參數數量不符` | Variable/Parameter 加了非 property 成員或誤呼叫建構子 | 只留 properties，用 object initializer 建立                              |
| 編譯：`沒有可覆寫的方法`                        | `Constraint.Build()` 寫了 `override`                  | 改成 `public void Build()`（非 virtual）                                 |
| CPLEX 1217 No solution（Infeasible）            | 模型不可行                                            | 先查 LHS/RHS 是否移項錯；用 IIS 衝突分析定位；暫拿掉部分約束驗證基礎模型 |
| 求解後 `varCount == 0`                          | `Build*Vs` 傳入空 Set                                 | 確認 Dataload 建構子內 Set 有正確 `Add/AddRange`                         |

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
var sol = engine.GetSetVarValues<VariableX_Name>();   // Dictionary<string,double>，key = 完整變數名（含 @ 索引）
engine.Dispose();
```
