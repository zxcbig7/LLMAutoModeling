# Constraint 資料夾規則（通用）

## 基本規範

- 繼承 `ConstraintBase`
- Namespace：`ProjectName.Constraint`
- 限制式名稱格式：`$"{ConstraintName}@{index1}@{index2}"`
- `EngineBase` 在每次 `CreateXxx` 自動統計群組與數量；`ConstraintCount` 已 obsolete，NEVER 宣告、遞增或手寫建立摘要

## 係數來源（天條）

**所有數值係數必須由建構子顯式注入的 Parameter 清單、scalar 或界限值取得，禁止 hardcode。**
**即使題目只提到少數幾個數值，也必須先定義為 Parameter；`Program.cs` 從 `data` 取出後，只把這條限制式真正需要的依賴傳入。**

```csharp
// ✓ 正確
engine.AddLHS(flourPerUnit.First(x => x.Item == item).QTY, new VariableX_Production { Item = item });

// ✗ 禁止
engine.AddLHS(0.1, new VariableX_Production { Item = "Bun" });
```

## AddLHS / AddRHS 模式

```csharp
// LHS = RHS 等式
engine.AddLHS(coeff, varLeft);
engine.AddRHS(coeff, varRight);   // 移項：等效 LHS - coeff*varRight = 0
engine.AddRHS(constantValue);     // 常數 RHS
engine.CreateEqual($"{ConstraintName}@{idx}");

// 不等式
engine.CreateLessEqual(...)    // LHS ≤ RHS
engine.CreateGreatEqual(...)   // LHS ≥ RHS
```

## Program.cs 組裝規範

模型三階段都在 `Program.cs` 的單一組裝點。簡單模型偏好目標式與限制式逐項註冊：

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.EMPLOYEE, data.SHIFT))
    .AddObjective(engine => new ObjectiveFunction(engine, data.EMPLOYEE, data.parameter_Cost).Build())
    .AddConstraints(engine => new Constraint_A(engine, data.EMPLOYEE, data.parameter_LimitA).Build())
    .AddConstraints(engine => new Constraint_B(engine, data.SHIFT, data.parameter_LimitB).Build());
```

- 式子只寫在各 `Constraint_Xxx.cs`，composition root 只顯示依賴與組裝關係。
- 目標式 MUST 經 `.AddObjective(...)`，限制式 MUST 經 `.AddConstraints(...)`；框架固定依 variables → objective → constraints 套用，註冊順序不會改變階段順序。
- 每條 Constraint 各占一個 `.AddConstraints(...)`；NEVER 用 block lambda 或 `BuildConstraints` local helper 隱藏清單。
- Objective / Constraint 建構子 NEVER 接整包 `Dataload`，也不保存未使用的資料。

```csharp
public Constraint_A(
    OptEngine engine,
    Set_Employee employees,
    List<Parameter_LimitA> limits)
{
    this.engine = engine;
    this.employees = employees;
    this.limits = limits;
}
```
