# Objective 資料夾規則（通用）

## 規範

- Namespace：`ProjectName.Objective`
- 無需繼承特定 base class，直接實作 `Build()`
- 結尾呼叫 `CreateMaximize()` 或 `CreateMinimize()`（二選一）
- 呼叫後 `Logging.Info("目標函數：...")`

## 係數來源

與 Constraint 相同：**係數從 dataload 查詢，禁止 hardcode。**

## 範例

```csharp
public void Build()
{
    dataload.Items.ForEach(i =>
        engine.AddLHS(dataload.Profit(i), new VariableX_Production { Item = i }));
    engine.CreateMaximize();
    Logging.Info("目標函數：max Σ Profit[i]·x[i]");
}
```
