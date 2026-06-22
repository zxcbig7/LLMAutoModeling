using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Set;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Objective
{
    /// <summary>目標函數：max R（火箭燃料產量）</summary>
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
            Logging.Info("目標函數：max R（RocketFuel）");
        }
    }
}
