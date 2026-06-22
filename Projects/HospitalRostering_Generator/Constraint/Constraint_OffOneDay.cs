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
                const int duration = 3;
                dataload.Date.ForEach(d =>
                {
                    dataload.Employee.ForEach(e =>
                    {
                        var window = dataload.Date.Where(sd => d.AddDays(-duration) < sd && sd <= d).ToList();
                        if (window.Count < duration) return;

                        var preD    = d.AddDays(-1); // 昨天
                        var prepreD = d.AddDays(-2); // 前天

                        optEngine.AddLHS(1, new VariableB_Off1Day { Date = d, Employee = e });
                        optEngine.AddRHS(1);
                        optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = d,       Employee = e, Group = "O" }); // (1 - 今天休)
                        optEngine.AddRHS(1,  new VariableB_ShiftAssign { Date = preD,    Employee = e, Group = "O" }); // 昨天休
                        optEngine.AddRHS(1);
                        optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = prepreD, Employee = e, Group = "O" }); // (1 - 前天休)
                        optEngine.AddRHS(-(duration - 1));
                        optEngine.CreateGreatEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
