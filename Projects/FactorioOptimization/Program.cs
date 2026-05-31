using OptimFoundation.Core;
using FactorioOptimization;

using (var problem = new FactorioOptimizationProblem())
{
    bool ok = problem.Execute();
    Logging.Info("?´é??‹ä??‚é?:", problem.totalTimer);
}
