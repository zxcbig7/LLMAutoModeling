
using OptimFoundation.Core;
using SandBox;

namespace MyApp
{
    internal class Program
    {

        static void Main(string[] args)
        {
            // 參數調整實驗：dotnet run -- experiment
            if (args.Length > 0 && args[0] == "experiment")
            {
                ExperimentRunner.Run();
                return;
            }

            using (RosteringProblem project = new RosteringProblem())
            {
                project.Execute();
                Logging.Info($"整體運作時間:", project.totalTimer);
            }
        }
    }
}