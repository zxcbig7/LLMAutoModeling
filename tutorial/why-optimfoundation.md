# 為什麼是 OptimFoundation —— 框架價值與工程內幕

> 給工程師看的一份文件：不是「怎麼用」，而是「它解決了什麼、底層怎麼做到的、為什麼這樣設計」。
> 對應的可分享網頁版見 `why-optimfoundation.html`（同一份內容）。

---

## TL;DR

OptimFoundation 是一個 **solver-agnostic 的 MIP（混合整數規劃）建模框架**。它的核心主張只有一句：

> **讓「定義問題」和「呼叫求解器」徹底解耦——上層只寫變數 / 參數 / 限制式 / 目標式，換 solver 只換引擎，模型程式碼一行不動。**

工程上它靠五個設計撐起這個主張：

| # | 技術 | 一句話 | 殺掉的痛點 |
|---|---|---|---|
| 1 | 泛型 `EngineBase<TModel,TVar,TExpr,TConstr>` | 用四個型別參數把 solver 型別整個抽掉 | 建模 code 與 CPLEX/Gurobi API 黏死 |
| 2 | 反射 `ModelElementBase` + deterministic key | 物件即變數身份證（`ClassName@v1@v2`） | 變數命名 / 查找 / 對解值散落各處 |
| 3 | `VariableBuilder`（編譯式 ctor + 笛卡爾積） | 建幾十萬個變數名稱不靠反射、不建實例 | 大模型建變數又慢又吃記憶體 |
| 4 | `AutoSetsGenerator`（Roslyn Source Generator） | 一行 attribute 宣告，編譯期長出整個 class | 每個變數 / 參數一堆重複樣板 |
| 5 | Pool API + Experiment telemetry | 不移項建約束、可記錄可調參、LLM-ready | 限制式手抄易錯、調參靠人工翻 log |

---

## 1. 它到底消滅了什麼痛點

直接用 CPLEX / Gurobi 的 .NET API 寫最佳化問題，工程上會撞到四道牆：

1. **建模邏輯與 solver 黏死。** `INumVar`、`GRBVar`、`ILinearNumExpr`… 散落整個專案。換 solver = 重寫。
2. **樣板地獄。** 每個變數、每個參數都是一段「宣告欄位 + 寫建構子 + 組 key」的重複 code。一個排班問題十幾個變數類別，全是噪音。
3. **限制式容易抄錯。** 數學式右邊有變數要移項、改號、合併同類項——人腦做這個，錯一個符號整個模型不可行，還很難 debug。
4. **調參不可累積。** 比較不同 `mipEmphasis` 的效果，只能人工翻 solver log、手抄目標值與時間，無法系統化，更不能餵給工具學「設定 → 結果」。

OptimFoundation 把這四件事**全部下沉到框架**。下面逐一拆它怎麼做到。

---

## 2. 工程核心一：用泛型把 solver 型別整個抽掉

所有共用邏輯（變數管理、Pool、批次建變數、實驗記錄）都在 `EngineBase`：

```csharp
public abstract class EngineBase<TModel, TVar, TExpr, TConstr> : ISolverEngine
```

- `TModel` = solver 的模型物件（CPLEX `Cplex` / Gurobi `GRBModel`）
- `TVar` = 變數型別（`INumVar` / `GRBVar`）
- `TExpr` = 線性表達式（`ILinearNumExpr` / `GRBLinExpr`）
- `TConstr` = 限制式（`IRange` / `GRBConstr`）

接新 solver 只要實作一份 `OptEngine : EngineBase<...>`，override **11 個 Solver Contract** 抽象方法（建模型、加變數、線性式、加限制式、設目標、求解、取值、Dispose…）。框架其餘部分完全不知道底下是誰。

```
EngineBase<TModel,TVar,TExpr,TConstr>     ← solver 無關的共用邏輯
   ├── OptimFoundation.Cplex.OptEngine     : EngineBase<Cplex, INumVar, ILinearNumExpr, IRange>
   ├── OptimFoundation.Gurobi.OptEngine    : EngineBase<GRBModel, GRBVar, GRBLinExpr, GRBConstr>
   └── OptimFoundation.Solver.OptEngine    : EngineBase<...>（自研 solver）
```

**工程價值**：solver 差異被收斂到一個 class 的 11 個方法。`AddVariables` 這種可選 override 還能讓各 solver 用原生 batch API（CPLEX `NumVarArray` / Gurobi `AddVars`）一次 interop 建 N 個變數，把效能熱點留在該優化的地方，而不污染上層。

> 來源：`OptimFoundation/src/OptimFoundation.Core/EngineBase.cs`、各 `OptEngine.cs`

---

## 3. 工程核心二：物件即變數身份證（反射 + deterministic key）

所有模型元素（Variable / Parameter / Constraint）都繼承 `ModelElementBase`。它只做兩件事，但這兩件事撐起整個框架的「無樣板」體驗：

```csharp
public abstract class ModelElementBase
{
    // PropertyInfo[] 快取：反射 GetProperties() 每次 ~100ns，快取後 ~10ns
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propsCache = new();

    // 把 sets 依「property 宣告順序」填值（反射 + 型別轉換）
    public void InitClassBySets(params object[] sets) { /* 數量檢查 + Convert.ChangeType */ }

    // 產生唯一 key：ClassName@val1@val2@...（DateTime 固定 yyyy-MM-dd）
    public override string ToString() { /* 反射讀 props 拼 key */ }
}
```

於是一個變數類別**只剩 properties**，零建構子：

```csharp
public class VariableB_ShiftAssign : VariableBase
{
    public DateTime Date     { get; set; }
    public string   Employee { get; set; }
    public string   Group    { get; set; }
}
```

而它的 `ToString()` 就是它在整個系統裡的唯一識別字串：

```
VariableB_ShiftAssign@2026-01-01@E1@D
│                     │          │  │
class 名稱            Date        E1 Group
```

**為什麼這個 key 是關鍵設計**：它讓「建立變數」和「在限制式裡引用變數」用的是**同一個物件、同一個 key**。你在約束裡寫 `new VariableB_ShiftAssign { Date = d, Employee = e, Group = g }`，框架 `ToString()` 出來的 key 必然對得上建立時的 key——不需要手動維護字串、不需要 ID 表。

代價與護欄也很清楚（框架文件直接列為天條）：
- **property 宣告順序 = key 的 `@` 順序**，必須與 `Build*Vs` 傳入的 set 順序一致；不一致 → `KeyNotFoundException`。
- 類別**只能有 properties**，多一個成員 `InitClassBySets` 的數量檢查就炸 `ArgumentException`。

這是「用約定換掉樣板」的典型取捨：少寫大量 code，代價是兩條必須遵守的紀律——而這兩條紀律正好是 source generator（見 §5）能在編譯期幫你守住的。

> 來源：`OptimFoundation/src/OptimFoundation.Core/DesignBases.cs`

---

## 4. 工程核心三：建幾十萬個變數還要快——VariableBuilder

排班、路徑這類問題，變數數量是多個 set 的**笛卡爾積**，動輒數十萬。`VariableBuilder` 在這裡做了三個明確的效能決策：

**(1) 建構子用 `Expression.Lambda` 編譯成 delegate 後快取**，而非每次反射 `Activator.CreateInstance`：

```csharp
private static readonly ConcurrentDictionary<Type, Func<string[], object>> _ctorCache = new();
// 優先無參數建構子（零建構子設計），向下相容 object[] / string[] 建構子
```

**(2) 笛卡爾積直接 `yield return string[]`**，不做「字串拼接再 Split」的來回：

```csharp
private static IEnumerable<string[]> GenVarParts(List<string>[] lists) { /* SelectMany 累積 */ }
```

**(3) 真正的熱路徑 `GetVarNames<T>` 根本不建實例**——純拼字串產出所有變數名稱，官方註解標「比舊版快 10x 以上」（舊版每個名稱都做一次 `InitClassBySets` + `ToString` 的反射）：

```csharp
public static IEnumerable<string> GetVarNames<TVariable>(object[] sets)
{
    string typeName = typeof(TVariable).Name;
    var stringLists = ConvertSetsToStringLists(sets);
    foreach (var parts in GenVarParts(stringLists))
        yield return typeName + "@" + string.Join("@", parts);
}
```

附帶一個好用的細節：`ConvertSetsToStringLists` 支援 `DateTime / int / long / double / decimal / string / enum` 當 set，浮點數一律走 `InvariantCulture`，確保和 `ModelElementBase.ToString()` 的 key 格式**逐字一致**——否則 build 與 lookup 的 key 會對不上。enum 因為是 value type 無法靠 `IEnumerable<Enum>` 共變比對，這裡特地用非泛型 `IEnumerable` + 元素型別偵測繞過。

**工程價值**：上層只寫 `BuildBVs<VariableB_ShiftAssign>(dates, employees, groups)`，背後是編譯式建構子 + 零分配的字串產生 + solver 原生 batch API。效能熱點被框架吃掉，使用者無感。

> 來源：`OptimFoundation/src/OptimFoundation.Core/VariableBuilder.cs`

---

## 5. 工程核心四：一行宣告長出整個 class——Roslyn Source Generator

§3 的「零建構子」已經夠精簡，但連 properties 都還要手寫。`AutoSetsGenerator`（一個 **Incremental Source Generator**）把它推到極限——**宣告即模型**：

```csharp
using OptimFoundation.Modeling;

[OptVar(VarType.Binary, "Date:DateTime", "Employee", "Group")]
public partial class VariableB_ShiftAssign { }   // ← 你只寫這一行

[OptParam("Date:DateTime", "Group")]
public partial class Parameter_ShiftDemand { }    // ← 自動補 set 屬性 + QTY + 建構子
```

編譯期生成 `VariableB_ShiftAssign.AutoSets.g.cs`，內容就是 §3 那個手寫版的等價物。幾個值得一看的工程決策：

- **零組件參考**。`OptVarAttribute` / `OptParamAttribute` / `VarType` enum 不放在某個要被參考的 DLL，而是用 `RegisterPostInitializationOutput` **注入到使用端自己的編譯**。使用者只要 `using OptimFoundation.Modeling;`，不需要加任何 NuGet / 專案參考。
- **增量、精準觸發**。用 `ForAttributeWithMetadataName` 而非掃整棵語法樹，只有帶 `[OptVar]`/`[OptParam]` 的節點會進 pipeline，編譯效能友善。
- **生成 code 用 `global::` 全名**指向 `OptimFoundation.Core.VariableBase`，避免使用端 namespace 衝突。
- **參數強制 `QTY` 在最後**：一個參數代表「某組 key 下的一個數值」，QTY 就是那個數值，排在所有 set 之後——和 §3 反射填值的順序約定對齊。

**這一層真正的價值不只是少打字**：source generator 是把 §3 那兩條「必須遵守的紀律」收編的最佳位置。後續可以讓它在編譯期直接報錯——類別沒標 `partial`、混進非 property 成員、`QTY` 不在最後、`BuildBVs` 的 set 數與宣告不符——把原本要等執行期才炸的 `KeyNotFoundException` / `ArgumentException` 提前到紅色波浪線。**這是框架目前最有發展空間的一塊。**

> 來源：`OptimFoundation/src/OptimFoundation.Generators/AutoSetsGenerator.cs`
> 兩種寫法（手寫 FORM 01 vs 宣告式 FORM 02）完全等價、可混用——產出的是 `partial class`，需要時在另一個同名 partial 補方法即可。

---

## 6. 工程核心五：不准移項——Pool API 的不變式

數學限制式翻成 code 最容易錯的地方是「整理式子」。OptimFoundation 直接**禁止**使用者做這件事，改用 LHS / RHS 雙 Pool：

```csharp
// 每位員工每天只能排一個班別： Σ_g X[d,e,g] = 1
dataload.Group.ForEach(g =>
    engine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = g }));
engine.AddRHS(1);
engine.CreateEqual($"OneGroup@{d:yyyy_MM_dd}@{e}");   // 送出後 Pool 自動清空
```

送出時框架自己算：

```
(LHS terms − RHS terms)  {sense}  (RHS const − LHS const)
```

所以**兩邊都能放變數和常數，框架負責移項合併**。使用者唯一要守的不變式是：

> **AML 數學式左邊的項 → `AddLHS`；右邊的項 → `AddRHS`。嚴禁自己移項、改號、化簡、翻轉比較方向。**

這把「人腦做代數」這個最大的出錯來源從流程裡刪掉了。配合 §3 的物件即 key——約束裡引用變數就是 `new VariableB_ShiftAssign { ... }`，key 自動對上——整個限制式建構變成「照抄數學式」。

軟性限制式（`CreateLeSoft` / `GeSoft` / `EqSoft`）也走同一個 Pool：違反不報不可行，而是把偏差量乘 penalty 加進目標式，框架依目標方向（Min/Max）自動決定懲罰符號。

> 來源：`OptimFoundation/specs/developer-guide.md` §6、§11

---

## 7. 工程核心六：可記錄、可調參、面向 LLM 的實驗環境

最後一道牆是「調參不可累積」。框架在 Core 層補了一套 **solver-agnostic 的單次求解記錄器**：

```csharp
// 不接管 engine 生命週期，只讀設定 + 跑一次 + 讀指標
Trial t = Trial.Capture(engine, "emphasis=2", () => engine.Solve());
experiment.AddTrial(t);
experiment.Save();   // → experiments/<name>.csv（給人比對）+ .json（給 LLM 學）
```

它補上了兩個原本散落或缺失的抽象面：

- **統一 telemetry**：`ISolverEngine.LastMetrics` 讓三個 engine 在 `Solve()` 後各自回填 `SolveMetrics`（Status / Objective / BestBound / MipGap / WallTime / NodeCount / IterationCount / Var·Constraint count / 收斂軌跡）。取不到的欄位填 `null` 而非丟例外。
- **統一可調旋鈕**：`ITunableConfig` 把各家「都有但沒抽象」的旋鈕（Seed / Emphasis / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / MemoryLimit）對映成跨引擎共通介面，能一次掃多 solver。

幾個務實的工程細節：
- **`ConfigSnapshot` 雙軌**：抽象旋鈕 + reflection 補抓 concrete 專屬欄位，確保快照不漏設定、實驗可重現。
- **收斂軌跡可擴展**：`ITrajectorySource` 是個 opt-in hook，本期只 CPLEX 用 `MIPInfoCallback` 實作，Gurobi/Solver 宣告 `SupportsTrajectory=false`，呼叫端不報錯——典型的「抽象先留好，實作後補」。
- **net48 相容性陷阱（值得記）**：JSON 用 `System.Text.Json`，但**必須綁 8.0.0 而非 8.0.5**——net8 消費端的 shared framework 只提供 assembly 8.0.0.0，綁 8.0.5 會在 net8 app 載入失敗。options 開 `UnsafeRelaxedJsonEscaping`（中文直出）+ `AllowNamedFloatingPointLiterals`（非 Optimal 時的 NaN）。

**戰略價值**：CSV 給人做 tuning 對照，JSON 是結構化的「設定 → 指標 → 軌跡」，明擺著為 **LLM 自動調參**鋪路。這條線決定了框架的未來走向。

> 來源：`OptimFoundation/specs/2026-06-18-experiment-tuning-tracking.md`、`Experiments/*.cs`

---

## 8. 進階：框架沒有把上層綁死

值得一提的是這些抽象沒有以犧牲彈性為代價。需要逐行掌控時，框架留了後門：

- **繼承 `OptEngine`** 可直接拿到原生 `Model`、`Variables` 字典、`ReadVar`、`SetVarLB/UB`，做 dual value 擷取或動態改界限。
- **Benders Decomposition** 有一級支援：`ResetConstraint()`（保留變數清約束）、`CopyModel()`、跨 model 的 Thread 限制式 + `MergeModel()`。一般限制式 attach 在 model 上不能跨 model，Thread 限制式用 `Le/Ge/Eq` 建立、暫不屬於任何 model，所以能搬——這個分界是 CPLEX `MultipleUseException` 逼出來的正確設計。

抽象負責 80% 的日常，後門負責 20% 的硬核需求，兩者不打架。

---

## 9. 一張表看懂價值

| 維度 | 直接用 solver API | 用 OptimFoundation |
|---|---|---|
| 換 solver | 重寫建模 code | 換 `OptEngine` + `Config`，模型不動 |
| 定義一個變數 | 宣告欄位 + 建構子 + 手維護 key | `[OptVar(...)] partial class`（或只寫 properties） |
| 變數命名 / 查找 | 自己組字串、自己維護 | 物件 `ToString()` 即 key，自動對上 |
| 建幾十萬變數 | 自己迴圈 + 反射建實例 | `BuildBVs<T>`，編譯式 ctor + 零分配字串 |
| 寫限制式 | 手動移項、改號、化簡 | LHS/RHS 雙池，框架移項 |
| 違反容忍（軟約束） | 自己加 slack 變數與 penalty 項 | `CreateLeSoft/GeSoft/EqSoft` |
| 調參記錄 | 人工翻 log 手抄 | `Trial.Capture` → CSV + LLM-ready JSON |

---

## 10. 收束：這個框架的工程哲學

把全部濃縮成三句：

1. **抽象在對的層級。** solver 型別收進泛型、樣板收進 source generator、代數收進 Pool、調參收進 Experiment——每個痛點都被推到它最該被解決的那一層，上層只剩純粹的問題描述。
2. **約定換樣板，但用工具守約定。** 「物件即 key」「只宣告 properties」靠約定省下大量 code，而 source generator 正是把這些約定在編譯期釘死的地方。
3. **為下一步留好介面。** `ITunableConfig` / `ITrajectorySource` / LLM-ready JSON 不是現在就全用上，而是把「solver 自動調參、LLM-based tuning」的擴展點先挖好。

> 想深入：框架架構看 `OptimFoundation/specs/framework-dev-spec.md`，逐功能 API 看 `OptimFoundation/specs/developer-guide.md`，實驗環境設計看 `OptimFoundation/specs/2026-06-18-experiment-tuning-tracking.md`。
> 想動手：同層的 `getting-started.html`（新手村）與 `index.html`（醫院排班實戰）。
