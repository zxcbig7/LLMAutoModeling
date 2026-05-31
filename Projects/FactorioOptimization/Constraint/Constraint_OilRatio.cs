using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Constraint
{
    /// <summary>
    /// [C12] OutputRate(Refinery, LightOil) * H = OutputRate(Refinery, HeavyOil) * L
    /// [C13] OutputRate(Refinery, Gas)      * L = OutputRate(Refinery, LightOil) * G
    /// </summary>
    public class Constraint_OilRatio : ConstraintBase
    {
        public new int ConstraintCount = 0;

        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public Constraint_OilRatio(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            double hRate = _dataload.OutputRate("Refinery", "HeavyOil");
            double lRate = _dataload.OutputRate("Refinery", "LightOil");
            double gRate = _dataload.OutputRate("Refinery", "Gas");

            // C12: lRate * H = hRate * L  ?? H:L = hRate:lRate = 5:9
            _engine.AddLHS(lRate, new VariableX_Resource { ResourceType = "HeavyOil" });
            _engine.AddRHS(hRate, new VariableX_Resource { ResourceType = "LightOil" });
            _engine.CreateEqual($"{ConstraintName}@HeavyLight");
            ConstraintCount++;

            // C13: gRate * L = lRate * G  ?? L:G = lRate:gRate = 9:11
            _engine.AddLHS(gRate, new VariableX_Resource { ResourceType = "LightOil" });
            _engine.AddRHS(lRate, new VariableX_Resource { ResourceType = "Gas" });
            _engine.CreateEqual($"{ConstraintName}@LightGas");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
