using OptimFoundation.Core;
using OptimFoundation.Cplex;

using MaxWeightIndependentSet.Model;

namespace MaxWeightIndependentSet.Project
{
    public class BuildConstraints
    {
        private readonly OptEngine _engine;
        private readonly Dataload _data;

        public BuildConstraints(Dataload data, OptEngine engine)
        {
            _data = data;
            _engine = engine;
        }

        public void Build()
        {
            Logging.Info("【建構目標式】");
            new ObjectiveFunction(_data.NODE, _data.param_Weight, _engine).Build();

            Logging.Info("【建構限制式】");
            new Constraint_EdgeConflict(_data.EDGE, _engine).Build();
        }
    }
}
