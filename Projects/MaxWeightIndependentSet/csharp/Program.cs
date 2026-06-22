using MaxWeightIndependentSet.Project;

namespace MaxWeightIndependentSet
{
    internal class Program
    {
        // 預設：單次求解（正確性 gate）。  dotnet run -- experiment：跑 tuning sweep。
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "experiment")
            {
                ExperimentRunner.Run();
                return;
            }

            using var project = new MwisProject();
            project.Execute();
        }
    }
}
