using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Objective;

namespace FactorioOptimization.Constraint
{
    public class BuildModel
    {
        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public BuildModel(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            Logging.Info("?êÂª∫ÊßãÁõÆÊ®ôÂ???);
            new ObjectiveFunction(_dataload, _engine).Build();

            Logging.Info("?êÂª∫ÊßãÈ??∂Â???);
            new Constraint_InputCap       (_dataload, _engine).Build();
            new Constraint_ResourceFlowDef(_dataload, _engine).Build();
            new Constraint_ResourceCap    (_dataload, _engine).Build();
            new Constraint_OilRatio       (_dataload, _engine).Build();
            new Constraint_Downstream     (_dataload, _engine).Build();
        }
    }
}
