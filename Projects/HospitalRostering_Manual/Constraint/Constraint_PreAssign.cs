using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Constraint
{
    /// <summary>C3 預排班固定：y[e,d,g] = 1，∀(e,d,g)∈PA。（CreateEqual）</summary>
    public class Constraint_PreAssign : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_PreAssign(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // TODO（逐步實作）：實作 C3，固定 dataload.parameter_PreAssign。
                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
