using OptimFoundation.Core;
using GlassFactory;

using (var problem = new GlassFactoryProblem())
{
    bool ok = problem.Execute();
    Logging.Info("整體運作時間:", problem.totalTimer);
}
