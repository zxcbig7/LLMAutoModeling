# 需手動刪除舊資料清單

> ✅ **A / B / C / E 已於 2026-06-22 執行完畢**（死 Problem.cs、重複 Data/、舊 Hospital、stale 子資料夾 CLAUDE.md 皆已刪）。
> D（build 產物）保留。本檔留作刪除紀錄。
>
> 產生：2026-06-21　關聯：[spec](2026-06-21-claudeai-spec-refresh.md)
> 背景：重構時 `rm` 受權限限制無法由我執行，故所有「舊檔刪除」集中於此清單，由你手動處理。
> 下列 dead 檔皆已確認**無任何 `new`/繼承引用**（移除後不影響 build）。

## A. 已被 Fluent OptModel 取代的手寫 Problem 類別（可直接刪）

| 檔案 | 取代者 |
|------|--------|
| `Template_CPLEX/TemplateProblem.cs` | `Template_CPLEX/Program.cs` + `ExperimentRunner.cs` |
| `Projects/WeeniesBuns/WeeniesBunsProblem.cs` | `Program.cs` + `ExperimentRunner.cs` |
| `Projects/SandwichProduction/SandwichProblem.cs` | 同上 |
| `Projects/ClinicVitamin/ClinicVitaminProblem.cs` | 同上 |
| `Projects/GlassFactory/GlassFactoryProblem.cs` | 同上 |
| `Projects/FactorioOptimization/FactorioOptimizationProblem.cs` | 同上（已 tombstone，原檔含 mojibake） |

## B. 重複 / 棄用資料夾（整個刪）

| 路徑 | 原因 |
|------|------|
| `Projects/FactorioOptimization/Data/` | 內容已遷移至 `Set/`（namespace `.Data`→`.Set`），`Data/Dataload.cs` 已 tombstone |
| `claudemdTemplate/Data/` | 已更名為 `claudemdTemplate/Set/`，`Data/CLAUDE.md` 已轉址 |

## C. 建議整個刪除的專案（需你拍板）

| 專案 | 原因 |
|------|------|
| ~~`Projects/HospitalRosteringProblem/`（舊版）~~ | ✅ **已刪除**（你已移除，留 `HospitalRosteringProblem_new/`） |

## D. Build 產物（可隨時刪、會重生）

各專案的 `bin/`、`obj/`、`.vs/`、`packages/`。

## E. 各專案「子資料夾」的 stale CLAUDE.md（重複 master，建議刪）

各專案子資料夾（`Set/`、`Data/`、`Variable/`、`VariablesClass/`、`Constraint/`、`Constraints/`、`Objective/`、`Parameter/`、`Model/`）內的 `CLAUDE.md` 是 master `claudemdTemplate/` 的舊副本，且已 drift（例：`WeeniesBuns/Set/CLAUDE.md` 標題仍為「Data 資料夾規則」、註解寫反）。`Projects/CLAUDE.md` 已宣告「資料夾規則以 `claudemdTemplate/` 為單一來源」，故這些副本可刪。

> **保留**：各專案**根目錄**的 `CLAUDE.md`（`Projects/<Proj>/CLAUDE.md`，專案概述）——不要刪。

## F. `HospitalRosteringProblem_new/`（⚠️ 移植完成後才刪，非立即）

關聯：[2026-06-22 dual-architecture spec](2026-06-22-dual-architecture-tutorial.md)。

`_new` 被 `Projects/HospitalRostering_Generator/`（清乾淨 namespace 的 generator 版）取代。但**目前不可刪**：兩個新專案（`_Generator` / `_Manual`）的 Constraint / Objective / Dataload **仍是 stub**，`_new` 是唯一保有完整實作的版本。

**刪除前提**：spec 的「逐步實作」把 `_new` 的限制式 / 目標式 / Dataload 邏輯移植進 `_Generator`（並鏡像到 `_Manual`）、本機 build + 求解驗證通過後，才可刪 `_new`。屆時其髒命名（`SandBox` / `MyApp` namespace、`Data/` `VariablesClass/` `Constraints/` 資料夾）一併移除。

```powershell
# F.（移植 + 驗證完成後才執行）
# Remove-Item "$root\Projects\HospitalRosteringProblem_new" -Recurse
```

---

## 刪除指令（PowerShell，確認後再跑）

```powershell
$root = "c:\Users\zxcbi\Desktop\Projects\OptimizationFramework\ClaudeAIAssistant"

# A. 手寫 Problem 類別
Remove-Item "$root\Template_CPLEX\TemplateProblem.cs"
Remove-Item "$root\Projects\WeeniesBuns\WeeniesBunsProblem.cs"
Remove-Item "$root\Projects\SandwichProduction\SandwichProblem.cs"
Remove-Item "$root\Projects\ClinicVitamin\ClinicVitaminProblem.cs"
Remove-Item "$root\Projects\GlassFactory\GlassFactoryProblem.cs"
Remove-Item "$root\Projects\FactorioOptimization\FactorioOptimizationProblem.cs"

# B. 重複資料夾
Remove-Item "$root\Projects\FactorioOptimization\Data" -Recurse
Remove-Item "$root\claudemdTemplate\Data" -Recurse

# C.（自行決定）刪舊版 Hospital
# Remove-Item "$root\Projects\HospitalRosteringProblem" -Recurse

# E. 各專案子資料夾的 stale CLAUDE.md（保留專案根目錄的）
Get-ChildItem "$root\Projects" -Recurse -Filter CLAUDE.md |
    Where-Object { $_.Directory.Parent.Name -ne 'Projects' } |
    Remove-Item
```
