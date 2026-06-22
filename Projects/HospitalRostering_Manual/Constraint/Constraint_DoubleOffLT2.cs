using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Constraint
{
    /// <summary>
    /// C8 連休 2 天旗標：s^dfl[e,d] ≥ y[e,d,O] + y[e,d-1,O] + (1-y[e,d-2,O]) - 2（視窗 2 天時退化為前兩項）；
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
                const int duration = 3;

                // C8：每日連休 2 天旗標
                dataload.Date.ForEach(d =>
                {
                    var window = dataload.Date.Where(sd => d.AddDays(-duration) < sd && sd <= d).ToList();
                    if (window.Count < 2) return;

                    dataload.Employee.ForEach(e =>
                    {
                        var preD = d.AddDays(-1);
                        if (window.Count == 2)
                        {
                            optEngine.AddLHS(1, new VariableB_DoubleOffFlag { Date = d, Employee = e });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = d,    Employee = e, Group = "O" });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = preD, Employee = e, Group = "O" });
                            optEngine.AddRHS(-(2 - 1));
                        }
                        else // window.Count >= 3
                        {
                            var prepreD = d.AddDays(-2);
                            optEngine.AddLHS(1, new VariableB_DoubleOffFlag { Date = d, Employee = e });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = d,    Employee = e, Group = "O" });
                            optEngine.AddRHS(1, new VariableB_ShiftAssign { Date = preD, Employee = e, Group = "O" });
                            optEngine.AddRHS(1);
                            optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = prepreD, Employee = e, Group = "O" });
                            optEngine.AddRHS(-(3 - 1));
                        }
                        optEngine.CreateGreatEqual($"{ConstraintName}_a@{d:yyyy_MM_dd}@{e}");
                        ConstraintCount++;
                    });
                });

                // C9：每人整月至少一次連休 2 天
                dataload.Employee.ForEach(e =>
                {
                    dataload.Date.ForEach(d =>
                        optEngine.AddLHS(1, new VariableB_DoubleOffFlag { Date = d, Employee = e }));
                    optEngine.AddLHS(2, new VariableB_DoubleOffLT2 { Employee = e });
                    optEngine.AddRHS(2);
                    optEngine.CreateGreatEqual($"{ConstraintName}_b@{e}");
                    ConstraintCount++;
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
