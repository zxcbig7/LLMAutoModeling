using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C7 做一休一做指示：s^off1[e,d] ≥ (1-y[e,d-2,O]) + y[e,d-1,O] + (1-y[e,d,O]) - 2。（CreateGreatEqual）</summary>
    public class Constraint_OffOneDay : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_OffOneDay(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // TODO（逐步實作）：實作 C7（前天上班、昨天休、今天上班）。
                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
