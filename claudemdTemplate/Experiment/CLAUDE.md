# Experiment 資料夾規則（Tuning 架構）

可實際跑的參數掃描，建構於框架的 `Experiment` / `Trial.Capture` / `ITunableConfig` harness（皆在 OptimFoundation DLL 內）。

## ExperimentRunner 規範

- 檔名 `ExperimentRunner.cs`（專案根目錄），Namespace：`ProjectName`
- `public static void Run()`，由 `Program.cs` 在 `dotnet run -- experiment` 時呼叫
- 與 solve 模式**共用** `VariableCreate` / `BuildModel`，不重複建模邏輯
- 每個 variant 用全新 `Dataload` + `OptEngine`，避免狀態跨 Trial 污染
- 掃描時 `enableLog=false`、關 LP/MPS/Sol export 以加速；設 `timeLimit` 確保每個 Trial 都會結束

## 樣板

```csharp
var exp = new Experiment("projectname-tuning", "掃 emphasis / varSel / nodeSelect / gap / threads / seed");

// (label, 在基準 config 上套用的調整)
var variants = new (string label, Action<CplexConfig> tune)[]
{
    ("baseline",          _ => { }),
    ("emphasis=optimal",  c => c.Emphasis    = 2),   // ITunableConfig 抽象旋鈕（跨引擎一致）
    ("varsel=strong",     c => c.varSel      = 3),   // CPLEX 專屬欄位
    ("nodesel=bestbound", c => c.nodeSelect  = 1),
    ("seed=20260621",     c => c.Seed        = 20260621),
};

foreach (var (label, tune) in variants)
{
    var config = new CplexConfig { epGap = 0.03, timeLimit = 60, workThreads = 8, enableLog = false };
    tune(config);

    var dataload = new ProjectNameDataload();
    using var engine = new OptEngine(config);
    engine.Build();
    new VariableCreate(dataload, engine).Build();   // ← 與 solve 模式共用
    new BuildModel(dataload, engine).Build();       // ← 與 solve 模式共用

    exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
}

exp.Save();   // → Experiments/projectname-tuning.csv + .json
```

## 旋鈕來源

- **抽象旋鈕（跨引擎）**：`ITunableConfig` 的 `Seed / Emphasis / FeasibilityTol / OptimalityTol / RootAlgorithm / Presolve / HeuristicEffort / MemoryLimitMb`
- **CPLEX 專屬欄位**與完整 ✅/❌ 對照：見 `truning/CLAUDE.md`
- **輸出**：`Experiments/<name>.csv + .json`（同名實驗 append，以 RunAt+Label 去重）

## 黃金順序

先做模型優化（結構、變數型態、邊界、對稱性），再掃 solver 旋鈕。詳見 `truning/CLAUDE.md` §1 → §2。
