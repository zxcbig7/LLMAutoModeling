using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Set;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Constraint
{
    /// <summary>
    /// [C14] InputRate(Assembler_Rocket, SolidFuel) * a ??S
    /// </summary>
    public class Constraint_Downstream : ConstraintBase
    {
        public new int ConstraintCount = 0;

        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public Constraint_Downstream(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            double sfRate = _dataload.InputRate("Assembler_Rocket", "SolidFuel");
            _engine.AddLHS(sfRate, new VariableI_Machine  { MachineType  = "Assembler_Rocket" });
            _engine.AddRHS(1.0,    new VariableX_Resource { ResourceType = "SolidFuel" });
            _engine.CreateLessEqual($"{ConstraintName}@SolidFuel");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
