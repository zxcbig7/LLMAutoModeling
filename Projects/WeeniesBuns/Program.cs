using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;
using WeeniesBuns.Constraint;

// 唯一進入點：solve / experiment 兩模式
//   dotnet run                → 一般求解（Fluent OptModel）
//   dotnet run -- experiment  → 參數掃描（ExperimentRunner）
if (args.Contains("experiment"))
{
    ExperimentRunner.Run();
    return;
}

var dataload = new WeeniesBunsDataload();

using (var m = new OptModel("WeeniesBuns")
    .UseConfig(() => new CplexConfig
    {
        epGap       = 0.0,
        timeLimit   = 60,
        workThreads = 4,
        enableLog   = true,
        exportSol   = true,
        exportLP    = true,
        exportMPS   = false,
    })
    .AddVariables(e => new VariableCreate(dataload, e).Build())
    .AddModel(e => new BuildModel(dataload, e).Build())
    .OnSolved(e => dataload.WriteToCSV(e)))
{
    bool ok = m.Execute();
    Logging.Info($"求解結果：{(ok ? "成功" : "失敗")}");
}
