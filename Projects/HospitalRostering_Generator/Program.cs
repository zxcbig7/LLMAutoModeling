using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;
using HospitalRostering_Generator.Constraint;
using HospitalRostering_Generator.Objective;

// 架構 B（Generator + 注入 Action）唯一進入點：solve / experiment 兩模式
//   dotnet run                → 一般求解（OptProject）
//   dotnet run -- experiment  → 參數掃描（OptExperiment，與 solve 共用 data / model）
var data = OptData.Load(() => new Dataload());

var projectConfig = new ProjectConfig
{
    ProjectName = "HospitalRostering_Generator",
    EnableSolverLog = true,
    ExportSol = true,
    ExportLP = true,
    ExportMPS = true,
};
// 停止契約：2026-08-09 使用者定案，詳見 TuningHistory.md 契約區塊
//   MipGap = 0.03 — 接受 2.7% 品質損失換取約 7s；production 穩定停在 obj 3.7（真最佳為 3.6）
//   TimeLimit = 180 — 3 分鐘；實際求解 5–17s，尚未觸及
// 環境契約（workThreads / parallelMode）尚未定版，待 S1 sizing
var baseline = new CplexConfig
{
    MipGap = 0.03,
    TimeLimit = 180,
    Threads = 10,
};

var model = new OptModel("HospitalRostering_Generator")
    .AddVariables(e => new VariableCreate(data, e).Build())
    .AddObjective(e => new ObjectiveFunction(data, e).Build())
    .AddConstraints(e =>
    {
        new Constraint_OneGroup(data, e).Build();       // C1
        new Constraint_FullfillDemand(data, e).Build(); // C2
        new Constraint_PreAssign(data, e).Build();      // C3
        new Constraint_SixDayWork(data, e).Build();     // C4
        new Constraint_CrossGroup(data, e).Build();     // C5
        new Constraint_NightToDay(data, e).Build();     // C6
        new Constraint_OffOneDay(data, e).Build();      // C7
        new Constraint_DoubleOffLT2(data, e).Build();   // C8 + C9
        new Constraint_BelowAVG(data, e).Build();       // C10
        new Constraint_WeekendLT4(data, e).Build();     // C11
    });

if (args.Contains("experiment"))
{
    var emphasis = baseline.Clone(); emphasis.Emphasis = 2;
    var varSel = baseline.Clone(); varSel.VariableSelect = 3;
    var nodeSelect = baseline.Clone(); nodeSelect.NodeSelect = 1;
    var gap = baseline.Clone(); gap.MipGap = 0.01;
    var threads = baseline.Clone(); threads.Threads = 4;
    var seed = baseline.Clone(); seed.Seed = 20260622;

    var result = new OptExperiment(
            "hospital-generator-tuning",
            "比較 emphasis / varSel / nodeSelect / gap / threads / seed 對求解的影響")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", emphasis)
        .AddConfig("varsel=strong", varSel)
        .AddConfig("nodesel=bestbound", nodeSelect)
        .AddConfig("gap=0.01", gap)
        .AddConfig("threads=4", threads)
        .AddConfig("seed=20260622", seed)
        .Run();

    Logging.Info($"[Experiment] 完成：{result.Trials.Count} 個 Trial");
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => baseline)
    .OnSolved(e => data.WriteToCSV(e));

bool ok = project.Execute();
Logging.Info($"求解結果：{(ok ? "成功" : "失敗")}  Status={project.Engine.Status}");
