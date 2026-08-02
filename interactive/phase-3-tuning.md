# Phase 3 · Foundation Tuning — 調校規範

> 使用者提出才做，不主動建議。先通過 [`phase-2-coding.md`](phase-2-coding.md) 的解驗證協定。

## 硬規則

- 先驗正確，再調效能；調快錯模型沒有價值。
- 依 solver / data / structure 三類觸發走對應入口，NEVER 混路。
- 模型已在 Phase 1 定版；「太慢 / gap 下不去」先試可逆的 `CplexConfig` 旋鈕，連續 3 輪無改善才升級到 structure。建模當下的「模型優先」不等於定版後直接改數學結構。
- 每輪用 `OptExperiment` 記錄 `MipGap`、時間、節點數、Status 與 objective，並回報 before / after。
- 影響模型語意的變更同步更新 Model.md。
- solver 層連續 3 輪無改善就升級方向；structure 層 2 輪仍不達標就停下回報。

## 三類入口

| 觸發 | 動作 |
| --- | --- |
| solver：timeout / gap 大 / 太慢，模型正確 | 只調 `CplexConfig` |
| data：使用者改參數值 | 只改輸入資料與 Dataload 載入 |
| structure：加刪約束、Big-M、infeasible、reformulation | 回 Model.md，確認後重走對應轉譯 |

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

## Infeasible 流程

1. 先用 IIS 找最小衝突集合。
2. 若使用者明確同意模型結構實驗，另建 soft variant；canonical hard model 保持不動。
3. 使用 `CreateLeSoft` / `CreateGeSoft` / `CreateEqSoft`，penalty 必須來自 Parameter。
4. 記錄違反量，並把語意變更同步到該 variant 的 Model.md 說明。

## Good / bad

✅ 正確性 gate → 一次改一個 solver 旋鈕 → `OptExperiment` 留紀錄 → 附 before / after。

❌ 直接把 `<=` 改成 `=`、修改輸入精度、沒有 Experiment 記錄就說變快。

## Fatal

- NEVER 未過正確性 gate 就調效能。
- NEVER 以 tuning 名義移項、改號、翻方向或四捨五入。
- NEVER 改模型語意卻不更新 Model.md。
- NEVER 不留實驗紀錄就宣稱改善。
