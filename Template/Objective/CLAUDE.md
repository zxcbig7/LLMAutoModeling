# Objective 資料夾規則（通用）

## 規範

- Namespace：`ProjectName.Objective`
- 無需繼承特定 base class，直接實作 `Build()`
- 結尾呼叫 `CreateMaximize()` 或 `CreateMinimize()`（二選一）
- 目標式開始／完成與項數由 `EngineBase` 自動記錄；只有業務目標分解需要另寫 log

## 係數來源

與 Constraint 相同：**係數只能來自建構子顯式注入的 Set、Parameter 清單、scalar 與界限值，禁止 hardcode。**

Objective 建構子只收自己真正使用的依賴，NEVER 接整包 `Dataload`。`Dataload` 只在 `Program.cs` 出現；新增或刪除依賴時，同步修改 constructor 與 `.AddObjective(...)` call site。

## 範例

```csharp
public ObjectiveFunction(
    OptEngine engine,
    Set_Item items,
    List<Parameter_Profit> profit)
{
    this.engine = engine;
    this.items = items;
    this.profit = profit;
}

public void Build()
{
    foreach (var item in items)
    {
        double coefficient = profit.First(x => x.Item == item).QTY;
        engine.AddLHS(coefficient, new VariableX_Production { Item = item });
    }
    engine.CreateMaximize();
}
```

`Program.cs` 在單一組裝點註冊模型三階段；簡單模型可直接逐項寫：

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableX_Production>(data.Items))
    .AddObjective(engine => new ObjectiveFunction(engine, data.Items, data.parameter_Profit).Build())
    .AddConstraints(engine => new Constraint_Capacity(engine, data.Items, data.parameter_Capacity).Build());
```

框架固定依 variables → objective → constraints 執行；`OptModel` 只定義模型，求解交給 `OptProject`，實驗交給 `OptExperiment`。

Objective 直接占一個 `.AddObjective(...)` fluent call；NEVER 用純轉呼叫 `BuildObjective` local helper 隱藏依賴。
