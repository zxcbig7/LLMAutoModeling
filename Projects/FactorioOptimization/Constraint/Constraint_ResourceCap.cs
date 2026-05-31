using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Constraint
{
    /// <summary>
    /// [C9~C11] ?ÑË?Ê∫êÊ??óÈ? ??OutputRate(Refinery, res) * x
    /// </summary>
    public class Constraint_ResourceCap : ConstraintBase
    {
        public new int ConstraintCount = 0;

        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public Constraint_ResourceCap(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            foreach (var res in new[] { "HeavyOil", "LightOil", "Gas" })
            {
                double rate = _dataload.OutputRate("Refinery", res);
                _engine.AddLHS(1.0,  new VariableX_Resource { ResourceType = res });
                _engine.AddRHS(rate, new VariableI_Machine   { MachineType  = "Refinery" });
                _engine.CreateLessEqual($"{ConstraintName}@{res}");
                ConstraintCount++;
            }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
