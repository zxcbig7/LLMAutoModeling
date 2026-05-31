# Constraint 資料夾規則

## 基本規範

- 繼承 `ConstraintBase`，Namespace：`ProjectName.Constraint`
- 宣告 `public new int ConstraintCount = 0;`
- 限制式名稱格式：`$"{ConstraintName}@{idx1}@{idx2}"`
- 每條限制式建立後 `ConstraintCount++`
- Build() 結尾：`Logging.Info($"[{ConstraintName}] {ConstraintCount}")`

## 係數來源（天條）

所有數值係數透過 `dataload` 從 `Parameter.QTY` 取得，不得 hardcode 任何裸數字。

## AddLHS / AddRHS 模式

```csharp
_engine.AddLHS(coeff, var);       // 左側累加
_engine.AddRHS(constValue);       // 常數 RHS
_engine.AddRHS(coeff, var);       // 移項（等效 LHS - coeff*var）
_engine.CreateEqual($"...");      // LHS = RHS
_engine.CreateLessEqual($"...");  // LHS ≤ RHS
_engine.CreateGreatEqual($"..."); // LHS ≥ RHS
ConstraintCount++;
```

`AddLHS`/`AddRHS` 為累加；`CreateXxx` 後自動清空，開始下一條。
