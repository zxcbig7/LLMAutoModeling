using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;
using ClinicVitamin.Variable;

namespace ClinicVitamin.Constraint
{
    /// <summary>
    /// [C3] 注射液批次上限
    ///   x[Shots] ≤ M
    /// </summary>
    public class Constraint_MaxShots : ConstraintBase
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;
        public new int ConstraintCount = 0;

        public Constraint_MaxShots(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            optEngine.AddLHS(1, new VariableX_Production { ProductType = "Shots" });
            optEngine.AddRHS(dataload.MaxShots);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
