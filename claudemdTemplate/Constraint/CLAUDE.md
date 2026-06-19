# Constraint 資料夾規則

## 基本規範

- 繼承 `ConstraintBase`，Namespace：`ProjectName.Constraint`
- 宣告 `public new int ConstraintCount = 0;`
- 限制式名稱格式：`$"{ConstraintName}@{idx1}@{idx2}"`
- 每條限制式建立後 `ConstraintCount++`
- Build() 結尾：`Logging.Info($"[{ConstraintName}] {ConstraintCount}")`

## 係數來源（天條）

所有數值係數透過 `dataload` 從 `Parameter.QTY` 取得，不得 hardcode 任何裸數字。

```csharp
// ✓ 正確
double coeff = _dataload.GetCoeff(key);
_engine.AddLHS(coeff, new VariableX_Xxx { ... });

// ✗ 禁止
_engine.AddLHS(10.0, new VariableX_Xxx { ... });
```

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

## 軟限制式（Phase 3 — 使用者明確指示才用）

框架內建軟限制式：自動加彈性變數 + 把 penalty 加進目標式（**不需手動改 Objective**）。

```csharp
_engine.AddLHS(coeff, var);                 // 同 hard 版先累加 LHS
_engine.CreateGeSoft(rhs, penalty);         // soft ≥（加 Deficit 變數）
_engine.CreateLeSoft(rhs, penalty);         // soft ≤（加 Surplus 變數）
_engine.CreateEqSoft(rhs, penalty, name);   // soft =（加 Delta_Neg/Pos）
```

- `penalty` 一律從 `Parameter.QTY` 取得，不得 hardcode。
- 違反量 = 彈性變數解值（`Deficit_*` / `Surplus_*` / `Delta_*`）。
- 改 soft 後**必須同步更新 `Model.md`**（Hard → Soft），penalty 值寫進 Parameter。
