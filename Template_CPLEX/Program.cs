using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template;
using Template.Set;
using Template.Variable;
using Template.Constraint;

// 唯一進入點：solve / experiment 兩模式
//   dotnet run                → 一般求解（Fluent OptModel）
//   dotnet run -- experiment  → 參數掃描（ExperimentRunner）
if (args.Contains("experiment"))
{
    ExperimentRunner.Run();
    return;
}

var dataload = new Dataload();

using (var m = new OptModel("Template")
    .UseConfig(() => new CplexConfig
    {
        epGap       = 0.03,
        timeLimit   = 300,
        workThreads = 8,
        enableLog   = true,
        exportSol   = true,
        exportLP    = true,
        exportMPS   = true,
    })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
    Logging.Info($"求解結果：{(ok ? "成功" : "失敗")}  Status={m.optEngine.Status}");

    // Infeasible → 框架已自動跑 IIS，讀回最小衝突限制式集合
    if (!ok && m.optEngine.Status == SolveStatus.Infeasible)
    {
        var conflicts = m.optEngine.GetConflictConstraints();
        Logging.Info($"衝突限制式（{conflicts.Count}）：{string.Join(", ", conflicts)}");
    }
}
