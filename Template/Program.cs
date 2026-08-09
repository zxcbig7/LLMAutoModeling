using System;
using System.Linq;
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using Template.Set;
using Template.Variable;
using Template.Objective;
using Template.Constraint;

// ══════════════════════════════════════════════════════════════════════════
//  Template — OptimFoundation / CPLEX 專案唯一進入點
//
//  執行方式
//    dotnet run                 求解一次（solve 模式，§1）
//    dotnet run -- experiment   參數掃描（experiment 模式，§4）
//
//  層級關係（整條鏈都在本檔可見，沒有額外的包裝類別）
//
//    ① 資料  Set/Set_*.cs           [OptSet] + [OptDim<T>]   維度積木（一顆一個檔）
//            Parameter/Parameter_*  [OptParam]    係數（值一律放 QTY 欄位）
//            Set/Dataload.cs        DataContext   載入資料 + 輸出解
//              └ OptData.Load(...)  唯一建構入口：註冊 + 聚合驗證，壞資料當場丟例外
//    ② 變數  Variable/VariableB_|X_|I_*  [OptVar]  決策變數宣告（前綴決定型別）
//              └ CreateVariables()  本檔 §2：engine.BuildBVs / BuildCVs / BuildIVs
//    ③ 模型  Objective/ObjectiveFunction  目標式（MUST 先建）
//            Constraint/Constraint_*      限制式（一條或一組邏輯相關 = 一個檔）
//              └ BuildObjective() / BuildConstraints()  本檔 §3：逐一 .Build()
//    ④ 引擎  ProjectConfig（輸出）+ CplexConfig（求解）
//              └ NewSolverConfig()  本檔 §5：兩模式共用的求解器設定
//    ⑤ 執行  solve       OptProject（單模型×單設定，本檔 §1）
//            experiment  OptExperiment（單模型×多設定，本檔 §4）
//
//  兩個模式共用 §2 §3：建模碼只有一份，掃參數不可能跟求解跑到不同的模型。
//  天條：Constraint / Objective 內不得出現裸數字，係數一律經 dataload 取得。
// ══════════════════════════════════════════════════════════════════════════

// ── §1 solve 模式 ─────────────────────────────────────────────────────────
// OptData.Load 是資料層唯一建構入口：跑參照完整性 / 重複 key / 數值 sanity 驗證，
// 失敗丟 DataValidationException（fail-fast，NEVER try/catch 吞掉）。
var data = OptData.Load(() => new Dataload());

// OptModel 只定義可重用的三階段模型；實際引擎生命週期交給 runner。
var model = new OptModel("Template")
    .AddVariables(engine => CreateVariables(data, engine))
    .AddObjective(engine => BuildObjective(data, engine))
    .AddConstraints(engine => BuildConstraints(data, engine));

if (args.Contains("experiment"))
{
    RunExperiment(model);
    return;
}

var projectConfig = new ProjectConfig
{
    ProjectName = "Template",
    EnableSolverLog = true,
    ExportSol = true,
    ExportLP = true,
    ExportMPS = true,
};
var solverConfig = NewSolverConfig(timeLimit: 300);

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => data.WriteToCSV(engine));

bool ok = project.Execute();
Logging.Info($"求解結果：{(ok ? "成功" : "失敗")} Status={project.optEngine.Status}");

// Infeasible → 框架已自動跑 IIS，這裡讀回最小衝突限制式集合
if (!ok && project.optEngine.Status == SolveStatus.Infeasible)
{
    var conflicts = project.optEngine.GetConflictConstraints();
    Logging.Info($"衝突限制式（{conflicts.Count}）：{string.Join(", ", conflicts)}");
}

return;

// ── §2 變數層 ─────────────────────────────────────────────────────────────
// 型別由類別名前綴決定：VariableB_ = Binary、VariableX_ = Continuous、VariableI_ = Integer。
// 傳入 sets 的順序 MUST 對齊該變數 [OptDim] 的宣告順序，否則索引會錯位而不報錯。
static void CreateVariables(Dataload data, OptEngine engine)
{
    engine.BuildBVs<VariableB_ABC>(data.SetA, data.SetB, data.SetC);
    engine.BuildBVs<VariableB_AC>(data.SetA, data.SetC);
    engine.BuildBVs<VariableB_A>(data.SetA);

    engine.BuildCVs<VariableX_A>(data.SetA);
    engine.BuildCVs<VariableX_AB>(data.SetA, data.SetB);

    // 有界整數 [0, 10]；連續有界同理：BuildCVs<VariableX_A>(0, 100, data.SetA)
    engine.BuildIVs<VariableI_A>(0, 10, data.SetA);

    Logging.Info($"變數建立完成：{engine.varCount}");
}

// ── §3 模型層 ─────────────────────────────────────────────────────────────
// 目標式 MUST 先建：Constraint_Soft 的 penalty 是掛進既有目標式的，順序反了會掛空。
// 新增一條限制式 = Constraint/ 新增一個檔 + 這裡加一行，沒有別的地方要改。
//
// 這裡是唯一知道 Dataload 的地方：Constraint / Objective 的建構子只收自己用得到的
// 積木、Parameter 清單與界限值，NEVER 收整包 Dataload——換資料結構不必動任何式子。
static void BuildObjective(Dataload data, OptEngine engine)
{
    Logging.Info("【建構目標式】");
    new ObjectiveFunction(
        engine, data.SetA, data.SetB, data.SetC,
        penaltyABC: data.Penalty_1,
        penaltyAC: data.Penalty_2,
        penaltyA: data.Penalty_3,
        penaltyXA: data.Penalty_4,
        penaltyXAB: data.Penalty_5,
        penaltyIA: data.Penalty_6).Build();
}

static void BuildConstraints(Dataload data, OptEngine engine)
{
    Logging.Info("【建構限制式】");
    new Constraint_Equality(engine, data.SetA, data.SetB, data.SetC, data.parameter_AB).Build();
    new Constraint_LessEqual(engine, data.SetA, data.SetB, data.SetC, data.AssignMax).Build();
    new Constraint_GreatEqual(engine, data.SetA, data.SetC, data.GreatEqualLB).Build();
    new Constraint_Window(engine, data.SetA, data.SetC, data.WindowSize, data.WindowMax).Build();
    new Constraint_VarOnRHS(engine, data.SetA, data.SetC).Build();
    new Constraint_Range(engine, data.SetA, data.SetC, data.RangeLB, data.RangeUB).Build();
    new Constraint_Soft(engine, data.SetA, data.SetB, data.SoftTarget, data.Penalty_Soft).Build();
}

// ── §4 experiment 模式 ────────────────────────────────────────────────────
// 同一個 OptModel 套多組 CplexConfig，由 OptExperiment 建立與釋放每個 cell 的 engine。
// 所有 cell 共用同一份已載入 data，輸出 Experiments/<name>.csv + .json。
static void RunExperiment(OptModel model)
{
    var baseline = NewSolverConfig(timeLimit: 60);
    var emphasis = baseline.Clone(); emphasis.Emphasis = 2;
    var varSel = baseline.Clone(); varSel.varSel = 3;
    var nodeSelect = baseline.Clone(); nodeSelect.nodeSelect = 1;
    var gap = baseline.Clone(); gap.epGap = 0.01;
    var threads = baseline.Clone(); threads.workThreads = 4;
    var seed = baseline.Clone(); seed.Seed = 20260621;

    var result = new OptExperiment(
            "template-tuning",
            "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", emphasis)
        .AddConfig("varsel=strong", varSel)
        .AddConfig("nodesel=bestbound", nodeSelect)
        .AddConfig("gap=0.01", gap)
        .AddConfig("threads=4", threads)
        .AddConfig("seed=20260621", seed)
        .Run();

    foreach (var trial in result.Trials)
    {
        var metrics = trial.Metrics;
        Logging.Info(
            $"[Experiment] {trial.Label}: Status={metrics.Status} " +
            $"Obj={metrics.ObjectiveValue:G6} Gap={metrics.MipGap:P2} Time={metrics.RunTimeMs:F0}ms " +
            $"Nodes={metrics.NodeCount} Vars={metrics.VarCount} Cons={metrics.ConstraintCount} " +
            $"Traj={metrics.Convergence.Count}");
    }

    Logging.Info($"[Experiment] 完成：{result.Trials.Count} 個 Trial 已寫入 {FolderDir.Experiment.GetPath()}");
}

// ── §5 求解器設定（兩模式共用）────────────────────────────────────────────
// solver 調校與輸出策略分離；solve 的匯出開關在 ProjectConfig，experiment 採框架的靜默預設。
// 完整欄位與 tuning 策略見 ../tuning/CLAUDE.md。
static CplexConfig NewSolverConfig(double timeLimit) => new CplexConfig
{
    epGap = 0.03,
    timeLimit = timeLimit,
    workThreads = 8,
};
