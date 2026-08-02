# Experiment 規則（Tuning 架構）

實驗直接在 `Program.cs` 使用 `OptExperiment`；不建立額外 facade 或手寫 engine / trial 迴圈。

## 物件分工

- `OptModel` 是可重用模型定義：variables、objective、constraints。
- `OptProject` 是一次正式求解；只有它提供 `OnSolved`。
- `OptExperiment` 展開 model × config，擷取 Trial 並自動 `Save()`。
- `CplexConfig` 只含 solver 旋鈕。專案輸出規則屬 `ProjectConfig`。

## 樣板

```csharp
// data 與 model 已在 Program.cs 前段各宣告一次。
var baseline = new CplexConfig
{
    epGap = 0.03,
    timeLimit = 60,
    workThreads = 8,
};
var emphasis = baseline.Clone();
emphasis.Emphasis = 2;
var strongBranching = baseline.Clone();
strongBranching.varSel = 3;

var result = new OptExperiment("projectname-tuning-r1", "baseline / emphasis / strong branching")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=optimal", emphasis)
    .AddConfig("r1-varsel=strong", strongBranching)
    .Run();
```

輸出為 `Experiments/<name>.csv + .json`；CSV 給人比較，JSON 給後續分析。

> 同名 experiment 是 append。`Run()` 會在 `Save()` 時讀回既有 JSON，並把歷史 trials 合併進回傳的 `result.Trials`。若只想處理本輪結果，使用唯一 experiment name（輪次或日期後綴）；刻意沿用同名時，`result.Trials` 必須視為累積資料，不能拿來當「本次 trial 清單」重複輸出。

## 規則

- `OptExperiment` 預設 solver log OFF、LP/MPS/Sol export OFF、housekeeping OFF；一般掃描不需要額外設定輸出開關。
- 所有 cell 共用同一份 `OptData.Load` 結果。載入後把資料視為唯讀；框架只保護受控 mutation API，不能依賴它攔截直接 public field / mutable list 寫入。
- variant 一律由 `baseline.Clone()` 產生具體物件。NEVER 用 `Action<CplexConfig>` 逐輪突變設定。
- `.AddModel(model)` × `.AddConfig(label, config)` 自動跑完整笛卡兒積；只跑單格時用 `.AddTrial(model, label, config)`。
- Trial label 為 `ModelName | config-label`。迭代輪次放入 label 或 experiment name，避免不同 baseline 同名。
- `Experiment.Save()` 以 `RunAt + Label` 去重後 append；同名重跑不會清空歷史。
- 同一輪一次只改一個旋鈕，否則無法歸因。
- `OptExperiment` 沒有 `OnSolved`；不要在參數掃描中大量寫 solution 檔。
- 若確實需要覆寫 experiment 的 project-level 設定，可呼叫 `.UseConfig(() => projectConfig)`；factory 每個 cell 都會執行。一般情況保留 experiment 預設。

## 旋鈕來源

- 抽象旋鈕：`Seed / Emphasis / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / HeuristicEffort / MemoryLimitMb`。
- CPLEX 專屬欄位與完整對照：見 `tuning/CLAUDE.md`。

先通過正確性 gate，再做 solver tuning。
