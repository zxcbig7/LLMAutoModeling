using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Set;

namespace FactorioOptimization.Variable
{
    public class VariableCreate
    {
        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public VariableCreate(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            _engine.BuildIVs<VariableI_Machine> (_dataload.MachineTypes);
            _engine.BuildCVs<VariableX_Resource>(_dataload.ResourceTypes);
            Logging.Info($"Variables created: {_engine.varCount}");
        }
    }
}
