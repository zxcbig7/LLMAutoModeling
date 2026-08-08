# Phase 3 · Foundation Tuning — 調校規範

> 使用者提出才做，不主動建議。先通過 [`phase-2-coding.md`](phase-2-coding.md) 的解驗證協定。

## 硬規則

- 先驗正確，再調效能；調快錯模型沒有價值。
- **本階段只動一樣東西：`CplexConfig`。** 模型與資料在進場時已凍結——NEVER 改 `Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md`。
- 進場前提是**已 feasible**（`Status == Optimal`，或該題預期的合法狀態）。前提破了就不是 tuning 問題。
- 每輪用 `OptExperiment` 記錄 `MipGap`、時間、節點數、Status 與 objective，並回報 before / after。
- 連續 3 輪無實質改善就停止 tuning 並回報，NEVER 自行升級去改模型結構。

## 範圍界線（判錯就是走錯 phase）

| 使用者要的 | 是不是 tuning | 動作 |
| --- | --- | --- |
| timeout / gap 收不下來 / 太慢，模型正確且有解 | ✅ 是 | 調 `CplexConfig`，走下方 paved path |
| 換參數值、換一批資料 | ❌ 否 | 換 `Data/*.csv` 重跑即可，不需要本階段 |
| 加刪約束、改 Big-M、reformulation | ❌ 否 | 退回 Phase 1 改 Model.md，確認後重走轉譯 |
| `Infeasible` / `Unbounded` | ❌ 否 | 前提破裂，見下方退場條件 |

Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，這一輪的實驗證據整批作廢。

## 常用 solver 決策

| 症狀 | 動作 |
| --- | --- |
| timeout | 提 `timeLimit`、試 `mipEmphasis = 1`、調整 threads |
| gap 過大 | 收 `epGap`、試 `mipEmphasis = 2`，再考慮 cuts |
| 記憶體爆 | `treeMemoryLimit` + `nodeFileInd` |
| 要重現 | `parallelMode = 1` + 固定 `randomSeed` + `detTimeLimit` |
| 數值不穩 | `numericalEmphasis = true` |

## Experiment paved path

`Program.cs` 前段已載入一份 `data` 並直接定義 `model`。所有 cell 共用這份資料：

```csharp
var baseline = new CplexConfig
{
    epGap = 0.03,
    timeLimit = 300,
    workThreads = 8,
};
var emphasis = baseline.Clone();
emphasis.Emphasis = 2;
var tighterGap = baseline.Clone();
tighterGap.epGap = 0.01;

var result = new OptExperiment("<project>-tuning-r1", "一次只改一個旋鈕")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=optimal", emphasis)
    .AddConfig("r1-gap=0.01", tighterGap)
    .Run();
```

- `OptExperiment` 預設 solver log OFF、LP/MPS/Sol export OFF、housekeeping OFF。
- 用 `Clone()` 產生具體 variant，NEVER 用 tune delegate 突變共用 config。
- `.AddModel` × `.AddConfig` 自動跑笛卡兒積；單格用 `.AddTrial(model, label, config)`。
- Trial label 自動成為 `ModelName | config-label`；每輪把輪次寫進 experiment name 或 label。
- `OptExperiment` 無 `OnSolved`；不要在掃描中大量輸出 solution。
- 載入後把 `data` 視為唯讀。框架只攔截受控 mutation API，直接寫 public field / mutable list 不保證立即攔截。

## 退場條件（前提破裂就不是本階段的事）

`Status` 不是 `Optimal` / `Feasible` → **停止 tuning，不調任何旋鈕**：

1. `Infeasible`：跑 IIS 拿最小衝突集合，把它當**證據**回報，退回 Phase 1 / Phase 2 修模型或資料。
2. `Unbounded`：某個方向漏了界限，退回 Phase 2 補該變數的上限 constraint。
3. NEVER 在本階段建 soft variant、加 penalty 或放寬限制式來「讓它有解」——那是改模型語意，屬 Phase 1 的決定。

Why: 旋鈕不會把 infeasible 變成 feasible，只會讓你更快確認它 infeasible。

## Good / bad

✅ 正確性 gate → 一次改一個 solver 旋鈕 → `OptExperiment` 留紀錄 → 附 before / after。

❌ 直接把 `<=` 改成 `=`、改 CSV 數值、修改輸入精度、沒有 Experiment 記錄就說變快。

## Fatal

- NEVER 未過正確性 gate 就調效能。
- NEVER 在本階段改資料或模型結構（改了就不是 tuning，退回對應 phase）。
- NEVER 以 tuning 名義移項、改號、翻方向或四捨五入。
- NEVER 用 soft constraint 繞過 infeasible。
- NEVER 不留實驗紀錄就宣稱改善。
