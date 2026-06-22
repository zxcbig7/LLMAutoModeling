using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>
    /// C8 連休 2 天旗標：s^dfl[e,d] ≥ y[e,d,O] + y[e,d-1,O] + (1-y[e,d-2,O]) - 2；
    /// C9 每月至少一次連休 2 天：Σ_d s^dfl[e,d] + 2·s^dlt[e] ≥ 2，∀e。（CreateGreatEqual）
    /// </summary>
    public class Constraint_DoubleOffLT2 : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_DoubleOffLT2(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // TODO（逐步實作）：實作 C8 旗標連動 + C9 每月至少一次。
                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
