# Constraint 資料夾規則

## 基本規範

- 繼承 `ConstraintBase`，Namespace：`ProjectName.Constraint`
- 限制式名稱格式：`$"{ConstraintName}@{idx1}@{idx2}"`
- `EngineBase` 會在每次 `CreateXxx` 自動統計；NEVER 自行維護第二份 count

## Program.cs 串接

模型組裝直接列在 `Program.cs` 的 `OptModel` pipeline。目標式一個 `.AddObjective(...)`，每條限制式一個 `.AddConstraints(...)`：

```csharp
var model = new OptModel("Canonical") // 模型定義；執行屬 runner
    .AddObjective(engine => new ObjectiveFunction(engine, data.PRODUCT, data.parameter_Profit).Build())
    .AddConstraints(engine => new Constraint_Capacity(engine, data.PRODUCT, data.parameter_Capacity).Build()) // [C1] ≤
    .AddConstraints(engine => new Constraint_Demand(engine, data.PRODUCT, data.parameter_Demand).Build()) // [C2] ≥
    .AddConstraints(engine => new Constraint_BatchDef(engine, data.PRODUCT, data.BATCH).Build()); // [C3] =
```

```csharp
// ✗ 別這樣：用 block lambda 隱藏多個組成，還維護第二份計數
.AddConstraints(engine => {
var c1 = new Constraint_Capacity(engine, data.PRODUCT, data.parameter_Capacity); c1.Build();
var c2 = new Constraint_Demand(engine, data.PRODUCT, data.parameter_Demand); c2.Build();
Logging.Info($"總數：{c1.ConstraintCount + c2.ConstraintCount}"); // ← 不需要
})
```

- 每條限制式各占一個 fluent call，不留 `var cN =` 參照、不自行加總
- 目標式 MUST 使用 `.AddObjective(...)`，限制式 MUST 使用 `.AddConstraints(...)`；框架會固定依此階段順序套用
- 不建立只負責轉呼叫的 helper class 或 local function

## 顯式資料依賴

Objective / Constraint 建構子只收自己真正使用的 `OptEngine`、Set 積木、Parameter 清單、scalar 與界限值，NEVER 接整包 `Dataload`，也 NEVER 保存未使用的資料。

```csharp
public Constraint_Capacity(
    OptEngine engine,
    Set_Product products,
    List<Parameter_Capacity> capacity)
{
    this.engine = engine;
    this.products = products;
    this.capacity = capacity;
}
```

`Dataload` 只在 `Program.cs` 出現。新增或刪除依賴時，同步修改 constructor 與對應的 `.AddObjective(...)` / `.AddConstraints(...)` call site。

## Set 積木迭代

注入的 Set 積木是 `IReadOnlyList<T>` / `IEnumerable<T>`，不是 `List<T>` —— 一律用 `foreach`，NEVER `.ForEach(...)`：

```csharp
// ✓ 正確
foreach (var p in products) { ... }

// ✗ 禁止：Set 積木沒有 .ForEach
products.ForEach(p => { ... });
```

## 係數來源（天條）

所有數值係數透過建構子注入的 Parameter 清單或 scalar 取得，不得 hardcode 任何裸數字。

```csharp
// ✓ 正確
double coeff = coefficients.First(x => x.Key == key).QTY;
_engine.AddLHS(coeff, new VariableX_Xxx { ... });

// ✗ 禁止
_engine.AddLHS(10.0, new VariableX_Xxx { ... });
```

## AddLHS / AddRHS 模式

```csharp
_engine.AddLHS(coeff, var); // 左側累加
_engine.AddRHS(constValue); // 常數 RHS
_engine.AddRHS(coeff, var); // RHS 變數項（框架內部表達）
_engine.CreateEqual($"..."); // LHS = RHS
_engine.CreateLessEqual($"..."); // LHS ≤ RHS
_engine.CreateGreatEqual($"..."); // LHS ≥ RHS
```

`AddRHS` 只放 Model.md 原式本來就在右側的項；NEVER 為了配合 API 自行移項、改號或翻轉比較方向。

## 逐項對照註記（框架精神的執行點）

天條是「code 是數學模型的機械轉譯」。要讓這句話**可驗證**，code 旁邊要能對回模型的公式——這樣蓋住 `Model.md`，光看 code 就能還原它。

這種註記不是在解釋 code 在做什麼（那種要刪），而是指出**對應公式的哪一項**——真相在另一個檔案（`Model.md`）裡，code 本身推導不出來，所以是必要的。

**註記到哪一層，看限制式複雜度**（過度註記會製造它想解決的噪音）：

- **乾淨的單一 summation**（`Σ_i coef[i]·x[i] ≤ 常數`，一個迴圈跑完）→ **class 層級一句公式就夠**，迴圈內不必逐行註記。範例：`Projects/WeeniesBuns/Constraint/Constraint_Flour.cs` 的 `/// [C1] Σ_i FlourPerUnit[i]·x[i] ≤ FlourCapacity`。
- **多項、含 index 位移或退化分支**（如 `y[e,d] + y[e,d-1] + (1 - y[e,d-2]) - 2`，蓋住公式會推不回來）→ **逐行 `AddLHS`/`AddRHS` 標明對到哪一項**（見下例）。

以 `[C8] s_dfl[e,d] ≥ y[e,d,O] + y[e,d-1,O] + (1 - y[e,d-2,O]) - 2` 為例：

```csharp
// ✓ 逐項對照，每行標明對到公式哪一項
_engine.AddLHS(1, new VariableB_DoubleOffFlag { Date = d, Employee = e }); // s_dfl[e,d]
_engine.AddRHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = OffGroup }); // + y[e,d,O]
_engine.AddRHS(1, new VariableB_ShiftAssign { Date = d.AddDays(-1), Employee = e, Group = OffGroup }); // + y[e,d-1,O]
_engine.AddRHS(1); // + 1
_engine.AddRHS(-1, new VariableB_ShiftAssign { Date = d.AddDays(-2), Employee = e, Group = OffGroup }); // - y[e,d-2,O]
_engine.AddRHS(-2); // - 2
_engine.CreateGreatEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
```

```csharp
// ✗ 冗餘註記（解釋 code 在幹嘛，讀 code 就知道 → 刪掉）
_engine.AddLHS(1, ...); // 加入 LHS
_engine.AddRHS(1, ...); // 再加一項
```

驗收：隨機抽一條限制式，不開 `Model.md`，光靠 code 與註記能還原公式 → 合格。

## 迭代與可讀性

- Set 積木用 `foreach`，NEVER `.ForEach(lambda)` —— lambda 內不能 `break`/`continue`、debugger 難單步，且 Set 積木本就無 `.ForEach`（compile error `CS1061`）
- 巢狀迴圈用 `foreach` 展開，不要為了短把兩層塞進巢狀 lambda
- `try { ... } catch (Exception) { throw; }` 是純噪音（原樣重拋等於沒寫），NEVER 加

`AddLHS`/`AddRHS` 為累加；`CreateXxx` 後自動清空，開始下一條。

## 區間限制式（Range）

`lb ≤ Σ係數·var ≤ ub` 用單一窗口建立，不要拆成兩條 `≤` / `≥`：

```csharp
_engine.AddLHS(coeff, var);              // 先累加 LHS（只用 LHS，RHS 不適用）
_engine.CreateRange(lb, ub, $"...");     // lb ≤ LHS ≤ ub，建完自動清空
```

- `lb` / `ub` 一律從 `Parameter.QTY` 取得，不得 hardcode。
- 只吃 LHS 累加項（`AddRHS` 不參與 Range）；常數項 `AddLHS(const)` 會併入界限平移。

## 軟限制式（Phase 3 — 使用者明確指示才用）

框架內建軟限制式：自動加彈性變數 + 把 penalty 加進目標式（**不需手動改 Objective**）。

```csharp
_engine.AddLHS(coeff, var);                 // 同 hard 版先累加 LHS
_engine.CreateGeSoft(rhs, penalty, name);   // soft ≥（加 Deficit 變數）
_engine.CreateLeSoft(rhs, penalty, name);   // soft ≤（加 Surplus 變數）
_engine.CreateEqSoft(rhs, penalty, name);   // soft =（加 Delta_Neg/Pos）
```

- `penalty` 一律從 `Parameter.QTY` 取得，不得 hardcode。
- Le/Ge 的兩參數呼叫為向下相容 API；新 code MUST 傳入穩定名稱。成功建立後 `EngineBase` 自動記錄 name、sense、rhs、penalty，Constraint class 不再手寫設定 log。
- 違反量 = 彈性變數解值（`Deficit_*` / `Surplus_*` / `Delta_*`）。
- 改 soft 後**必須同步更新 `Model.md`**（Hard → Soft），penalty 值寫進 Parameter。
