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

## 組裝規範（寫在 Program.cs，不另開 BuildModel.cs）

`Program.cs` 的 `BuildModel(Dataload data, OptEngine engine)` local function 依邏輯群組依序呼叫，**目標式排第一**：

```csharp
static void BuildModel(Dataload data, OptEngine engine)
{
    new ObjectiveFunction(data, engine).Build();   // MUST 先建
    new Constraint_A(data, engine).Build();
    new Constraint_B(data, engine).Build();
}
```

- NEVER 另開 `BuildModel.cs` 包裝類別 —— ALWAYS 用 local function —— Why: 只有轉呼叫、沒有邏輯的一層；收進 Program.cs 才看得到完整組裝關係，且 solve / experiment 兩模式天然共用
- NEVER 在 `BuildModel()` 裡直接寫 `AddLHS` / `AddRHS` —— 它只負責呼叫順序，式子寫在各 `Constraint_Xxx.cs`
- 目標式 MUST 排在所有 constraint 之前 —— Why: soft constraint 的 penalty 掛進既有目標式，順序反了會掛空
