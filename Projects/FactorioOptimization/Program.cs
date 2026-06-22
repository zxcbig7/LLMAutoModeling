using System;
using System.Linq;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization;
using FactorioOptimization.Set;
using FactorioOptimization.Variable;
using FactorioOptimization.Constraint;

// 唯一進入點：solve / experiment 兩模式
if (args.Contains("experiment"))
{
    ExperimentRunner.Run();
    return;
}

var dataload = new FactorioOptimizationDataload();

using (var m = new OptModel("FactorioOptimization")
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
    if (!ok) Console.Error.WriteLine("[ERROR] 求解失敗");
}
