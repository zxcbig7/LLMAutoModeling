using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C5 跨組別支援指示：y[e,d,g] ≤ s^mis[e,d]，∀g∈CG_e。（CreateLessEqual，變數移 RHS）</summary>
    public class Constraint_CrossGroup : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_CrossGroup(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // TODO（逐步實作）：實作 C5，CG_e 取自 dataload.parameter_CrossGroup。
                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
