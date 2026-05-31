using OptimFoundation.Core;
using WeeniesBuns;

using (var problem = new WeeniesBunsProblem())
{
    bool ok = problem.Execute();
    Logging.Info("整體運作時間:", problem.totalTimer);
}
