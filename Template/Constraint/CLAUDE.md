# Constraint 資料夾規則（通用）

## 基本規範

- 繼承 `ConstraintBase`
- Namespace：`ProjectName.Constraint`
- 宣告 `public new int ConstraintCount = 0;`
- 限制式名稱格式：`$"{ConstraintName}@{index1}@{index2}"`
- 每建一條限制式後 `ConstraintCount++`
- Build() 結尾：`Logging.Info($"[{ConstraintName}] {ConstraintCount}")`

## 係數來源（天條）

**所有數值係數必須透過 `dataload` 從 `Parameter.QTY` 取得，禁止 hardcode。**
**即使題目只提到少數幾個數值，也必須先定義為 Parameter，再透過 Dataload 存取。**

```csharp
// ✓ 正確
engine.AddLHS(dataload.FlourPerUnit(spec), new VariableX_Production { Item = spec.Item });

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

ConstraintCount++;
```

## BuildModel 規範

依邏輯群組依序呼叫各 Constraint：

```csharp
new ObjectiveFunction(dataload, engine).Build();
new Constraint_A(dataload, engine).Build();
new Constraint_B(dataload, engine).Build();
```
