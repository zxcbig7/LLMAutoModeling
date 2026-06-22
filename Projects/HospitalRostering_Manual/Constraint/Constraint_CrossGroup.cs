using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Constraint
{
    /// <summary>C5 跨組別支援指示：y[e,d,g] ≤ s^mis[e,d]，∀e,d, g∈CG_e。（CreateLessEqual，變數移 RHS）</summary>
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
                dataload.Date.ForEach(d =>
                {
                    dataload.Employee.ForEach(e =>
                    {
                        var crossGroups = dataload.parameter_CrossGroup.Where(p => p.Employee == e).ToList();
                        foreach (var cg in crossGroups)
                        {
                            optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = cg.Group });
                            optEngine.AddRHS(1, new VariableB_GroupMismatch { Date = d, Employee = e });
                            optEngine.CreateLessEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}@{cg.Group}");
                            ConstraintCount++;
                        }
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
