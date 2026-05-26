using OptimFoundation.Core;
using Template;

using (var problem = new TemplateProblem())
{
    problem.Execute();
    Logging.Info("整體運作時間:", problem.totalTimer);
}
