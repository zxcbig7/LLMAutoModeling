using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C11 週末休假彈性：z^wkd[e] ≥ 4 - Σ_{d∈W} y[e,d,O]，∀e。（CreateGreatEqual）</summary>
    public class Constraint_WeekendLT4 : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_WeekendLT4(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // TODO（逐步實作）：實作 C11，W = 週末日集合。
                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
