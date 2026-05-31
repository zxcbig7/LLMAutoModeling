# Objective 資料夾規則

## 規範

- Namespace：`ProjectName.Objective`
- 結尾呼叫 `CreateMaximize()` 或 `CreateMinimize()`（二選一）
- 呼叫後 `Logging.Info("目標函數：...")`

## 係數來源

與 Constraint 相同：係數從 dataload 查詢，禁止 hardcode。

## 範例結構

```csharp
public void Build()
{
    // AddLHS 累加目標項
    engine.CreateMaximize();  // 或 CreateMinimize()
    Logging.Info("目標函數：...");
}
```
