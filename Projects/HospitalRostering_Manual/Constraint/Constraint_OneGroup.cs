using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Constraint
{
    /// <summary>C1 每人每天恰一個班別：Σ_{g∈G} y[e,d,g] = 1，∀e,d。（CreateEqual）</summary>
    public class Constraint_OneGroup : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_OneGroup(Dataload dataload, OptEngine engine)
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
                        dataload.Group.ForEach(g =>
                            optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = g }));
                        optEngine.AddRHS(1);
                        optEngine.CreateEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
