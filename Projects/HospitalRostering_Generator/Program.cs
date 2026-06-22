using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;
using HospitalRostering_Generator.Constraint;

// 架構 B（Generator + 注入 Action）唯一進入點：solve / experiment 兩模式
//   dotnet run                → 一般求解（Fluent OptModel，建模步驟以 Action<OptEngine> 注入）
//   dotnet run -- experiment  → 參數掃描（ExperimentRunner，與 solve 共用 build-step）
if (args.Contains("experiment"))
{
    ExperimentRunner.Run();
    return;
}

var dataload = new Dataload();

using (var m = new OptModel("HospitalRostering_Generator")
    .UseConfig(() => new CplexConfig
    {
        epGap       = 0.03,
        timeLimit   = 100,
        workThreads = 10,
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
}
