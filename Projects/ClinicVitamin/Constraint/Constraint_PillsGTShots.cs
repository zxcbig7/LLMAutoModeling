using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;
using ClinicVitamin.Variable;

namespace ClinicVitamin.Constraint
{
    /// <summary>
    /// [C2] 藥丸批次 ≥ 注射液批次
    ///   x[Pills] ≥ x[Shots]
    ///   → x[Pills] - x[Shots] ≥ 0
    /// </summary>
    public class Constraint_PillsGTShots : ConstraintBase
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;
        public new int ConstraintCount = 0;

        public Constraint_PillsGTShots(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            optEngine.AddLHS(1, new VariableX_Production { ProductType = "Pills" });
            optEngine.AddRHS(1, new VariableX_Production { ProductType = "Shots" });
            optEngine.CreateGreatEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
