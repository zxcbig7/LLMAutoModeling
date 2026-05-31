using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Objective
{
    /// <summary>?®æ??½æ•¸ï¼šmax Rï¼ˆç«ç®­ç??™ç”¢?ï?</summary>
    public class ObjectiveFunction
    {
        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public ObjectiveFunction(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            _engine.AddLHS(1.0, new VariableX_Resource { ResourceType = "RocketFuel" });
            _engine.CreateMaximize();
            Logging.Info("?®æ??½æ•¸ï¼šmax Rï¼ˆRocketFuelï¼?);
        }
    }
}
