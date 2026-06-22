using OptimFoundation.Core;
using OptimFoundation.Cplex;

using SandBox;
using SandBox.Data;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 唯一進入點：solve / experiment 兩模式
            if (args.Length > 0 && args[0] == "experiment")
            {
                ExperimentRunner.Run();
                return;
            }

            // Composition Root：Fluent OptModel。變數/限制式組裝抽到 ModelComposer，
            // 與 ExperimentRunner 共用，確保兩模式模型一致。
            var data = new Dataload();

            using var problem = new OptModel("RosteringProblem_new")
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
                .AddVariables(e => ModelComposer.BuildVariables(e, data))
                .AddModel(e => ModelComposer.BuildModel(e, data))
                .OnSolved(e => data.WriteToCSV(e));

            problem.Execute();

            Logging.Info("整體運作時間:", problem.totalTimer);
        }
    }
}
