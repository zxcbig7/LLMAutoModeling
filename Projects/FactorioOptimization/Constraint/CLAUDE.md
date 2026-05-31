# Constraint 資料夾規則

## 基本規範

- 繼承 `ConstraintBase`
- Namespace：`ProjectName.Constraint`
- 宣告 `public new int ConstraintCount = 0;`
- 限制式名稱格式：`$"{ConstraintName}@{index1}@{index2}"`
- 每條限制式建立後 `ConstraintCount++`
- Build() 結尾：`Logging.Info($"[{ConstraintName}] {ConstraintCount}")`

## 係數來源（天條）

所有數值係數必須從 `dataload` 查詢，禁止 hardcode magic number。

```csharp
// ✓ 正確
double rate = dataload.InputRate("Assembler_Rocket", "SolidFuel");
engine.AddLHS(rate, new VariableI_Machine { MachineType = "Assembler_Rocket" });

// ✗ 禁止
engine.AddLHS(0.667, new VariableI_Machine { MachineType = "Assembler_Rocket" });
```

## AddLHS / AddRHS 模式

```csharp
engine.AddLHS(coeff, var);          // 左側累加
engine.AddRHS(constValue);          // 常數右側
engine.AddRHS(coeff, var);          // 移項：等效 LHS - coeff*var = 0
engine.CreateEqual($"...");         // LHS = RHS
engine.CreateLessEqual($"...");     // LHS ≤ RHS
engine.CreateGreatEqual($"...");    // LHS ≥ RHS
ConstraintCount++;
```

AddLHS / AddRHS 為累加；呼叫 `CreateXxx` 後自動清空，開始下一條。
