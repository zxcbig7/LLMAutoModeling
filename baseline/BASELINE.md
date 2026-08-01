# 遷移前基準值（Packet 0 ③）

> 採集時間：2026-08-01
> 採集時的框架版本：`dlls/` 尚未套用 dual-config / runner-symmetry 改動（pre-migration）
> 用途：**Packet 4（專案遷移）的唯一驗收依據**。遷移後每個專案重跑一次，目標值需與本表一致。

## 採集方式

```powershell
cd "C:\Users\zxcbi\Desktop\Projects\OptimizationFramework\AI-Modeling"
Get-ChildItem Projects -Directory | ForEach-Object {
    $sub = Get-ChildItem $_.FullName -Recurse -Depth 1 -Filter *.csproj | Select-Object -First 1
    if ($sub) {
        Push-Location $sub.DirectoryName
        dotnet run --no-build 2>&1 | Tee-Object "..\..\baseline\$($_.Name).txt"
        Pop-Location
    }
}
```

原始輸出保存在同資料夾的 `<專案名>.txt`（含完整 CPLEX log 與各限制式條數）。

## 基準表

| 專案 | Status | 目標值 | 原始輸出 |
| --- | --- | --- | --- |
| ClinicVitamin | Optimal | 226.00000000000003 | `ClinicVitamin.txt` |
| FactorioOptimization | Optimal | 4.999999999999985 | `FactorioOptimization.txt` |
| GlassFactory | Optimal | 480 | `GlassFactory.txt` |
| HospitalRostering_Generator | Optimal | 3.6999999999999997 | `HospitalRostering_Generator.txt` |
| HospitalRostering_Manual | Optimal | 3.6999999999999997 | `HospitalRostering_Manual.txt` |
| MaxWeightIndependentSet | Optimal | 1332 | `MaxWeightIndependentSet.txt` |
| SandwichProduction | Optimal | 60 | `SandwichProduction.txt` |
| WeeniesBuns | Optimal | 3212 | `WeeniesBuns.txt` |

`Projects/WoodworkingShop/` **無 `.csproj`**（空殼資料夾），不列入遷移範圍。

## 驗收規則（Packet 4 用）

遷移後對每個專案跑 `dotnet build` + `dotnet run`，逐項比對：

1. `Status` MUST 仍為 `Optimal`
2. 目標值 MUST 與本表一致（浮點誤差容忍 `1e-6` 相對差；上表的尾數雜訊是 CPLEX 原樣輸出，不要四捨五入後比對）
3. 各限制式條數 MUST 與 `<專案名>.txt` 內的 `[Constraint_*] N` 行一致

任一項不符 → **停下回報，NEVER 自行調整模型或容差把它「喬」成一致**。目標值變了代表遷移改到了數學內容，那是 bug 不是預期。

## 一致性交叉檢查

`HospitalRostering_Generator` 與 `HospitalRostering_Manual` 共用同一份數學模型，目標值本應相同（實測皆為 `3.6999999999999997`）。遷移後若兩者出現分歧，代表其中一個被改壞了。
