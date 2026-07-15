using OptimFoundation.Core;
using OptimFoundation.Cplex;

using MaxWeightIndependentSet.Model;

namespace MaxWeightIndependentSet.Project
{
    public class VariableCreate
    {
        private readonly OptEngine _engine;
        private readonly Dataload _data;

        public VariableCreate(Dataload data, OptEngine engine)
        {
            _data = data;
            _engine = engine;
        }

        public void Build()
        {
            // x_i ∈ {0,1} ∀ i∈V
            _engine.BuildBVs<VariableY_Select>(_data.NODE);
            Logging.Info($"Variables created: {_engine.varCount}");
        }
    }
}
