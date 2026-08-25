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
    // 2026-08-25 調參結論：保留原設定。
    // 試過兩顆旋鈕都明顯更差（強分支慢 4 倍、重界限慢 2.4 倍），沒有值得採用的贏家。
    // 停止條件還原成原本的值；另外補上 ParallelMode 與 Seed —— 這兩顆本來就該明設，
    // 沒設的話每次跑的路徑都不一樣，之後想比較任何東西都沒有基準。
    MipGap = 0.03,
    TimeLimit = 180,
    Threads = 10,
    ParallelMode = 1,
    Seed = 11,
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
    // R2 — HospitalRostering-tuning-r2
    // R1 已否證：強分支 VariableSelect=3 反而慢 4 倍、node 更多，一個種子還撞時限。
    // 重新看 R0 的數字：首解 0.4 秒就有了，之後全部時間都花在「證明這就是最佳解」，
    // 也就是把界往上推那 0.30。所以這輪改用重視界限的搜尋重點。
    var exp = new OptExperiment(
        "HospitalRostering-tuning-r2",
        "R2：時間都花在證明最佳性 → 試 Emphasis=3（重視界限）");
    exp.AddModel(model);

    foreach (var s0 in new[] { 11, 22, 33, 44, 55 })
    {
        var b = baseline.Clone();
        b.Seed = s0;
        exp.AddConfig($"r2-baseline-s{s0}", b);

        var e = baseline.Clone();
        e.Seed = s0;
        e.Emphasis = 3;
        exp.AddConfig($"r2-Emphasis=3-s{s0}", e);
    }

    var result = exp.Run();

    Logging.Info($"[Experiment] 完成：{result.Trials.Count} 個 Trial");
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => baseline)
    .OnSolved(e => data.WriteToCSV(e));

bool ok = project.Execute();
Logging.Info($"求解結果：{(ok ? "成功" : "失敗")}  Status={project.Engine.Status}");
