# Objective 資料夾規則

## 規範

- Namespace：`ProjectName.Objective`
- 結尾呼叫 `CreateMaximize()` 或 `CreateMinimize()`（二選一）
- 呼叫後 `Logging.Info("目標函數：...")`

## 係數來源（天條）

所有係數透過 `dataload` 從 `Parameter.QTY` 取得，不得 hardcode。

```csharp
public void Build()
{
    foreach (var i in _dataload.ITEM)   // Set_Item 積木，foreach 迭代（非 .ForEach，積木不是 List<T>）
        _engine.AddLHS(_dataload.GetProfit(i), new VariableX_Xxx { Item = i });
    _engine.CreateMaximize();
    Logging.Info("目標函數：max ...");
}
```
