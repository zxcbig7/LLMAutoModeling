using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C6 不良班別轉換指示：s^ntd[e,d] ≥ y[e,d-1,g'] + y[e,d,g] - 1，∀(g',g)∈R。（CreateGreatEqual）</summary>
    public class Constraint_NightToDay : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_NightToDay(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                const int duration = 2;
                dataload.Date.ForEach(d =>
                {
                    dataload.Employee.ForEach(e =>
                    {
                        var window = dataload.Date.Where(sd => d.AddDays(-duration) < sd && sd <= d).ToList();
                        if (window.Count < duration) return;

                        var preD = d.AddDays(-1);
                        dataload.parameter_NightToDay.ForEach(rule =>
                        {
                            optEngine.AddLHS(1, new VariableB_NightToDay { Date = d, Employee = e });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = preD, Employee = e, Group = rule.PreGroup });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = d,    Employee = e, Group = rule.Group });
                            optEngine.AddRHS(-1);
                            optEngine.CreateGreatEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{e}@{rule.PreGroup}_{rule.Group}");
                            ConstraintCount++;
                        });
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
