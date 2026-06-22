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
                dataload.Employee.ForEach(e =>
                {
                    optEngine.AddLHS(1, new VariableX_WeekendLT4 { Employee = e });
                    optEngine.AddRHS(4);
                    dataload.Date
                        .Where(w => w.DayOfWeek == DayOfWeek.Saturday || w.DayOfWeek == DayOfWeek.Sunday)
                        .ToList()
                        .ForEach(d => optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = "O" }));
                    optEngine.CreateGreatEqual($"{ConstraintName}@{e}");
                    ConstraintCount++;
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
