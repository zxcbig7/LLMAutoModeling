using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>
    /// C4 連續工作 6 天指示（滑動視窗 W6(d)，上下界兩組式）：
    ///   上界 ∀τ：s^six[e,d] ≤ 1 - y[e,τ,O]
    ///   下界    ：s^six[e,d] ≥ 1 - Σ_τ y[e,τ,O]
    /// </summary>
    public class Constraint_SixDayWork : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_SixDayWork(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                const int duration = 6;
                dataload.Date.ForEach(d =>
                {
                    dataload.Employee.ForEach(e =>
                    {
                        var window = dataload.Date.Where(sd => d.AddDays(-duration) < sd && sd <= d).ToList();
                        if (window.Count < duration) return;

                        // 上界：每個視窗日一條 s^six ≤ 1 - y[e,τ,O]
                        window.ForEach(sd =>
                        {
                            optEngine.AddLHS(1, new VariableB_SixDayWork { Date = d, Employee = e });
                            optEngine.AddRHS(1);
                            optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = sd, Employee = e, Group = "O" });
                            optEngine.CreateLessEqual($"{ConstraintName}_ub@{d:yyyy_MM_dd}@{e}@{sd:yyyy_MM_dd}");
                            ConstraintCount++;
                        });

                        // 下界：s^six ≥ 1 - Σ_τ y[e,τ,O]
                        optEngine.AddLHS(1, new VariableB_SixDayWork { Date = d, Employee = e });
                        optEngine.AddRHS(1);
                        window.ForEach(sd =>
                            optEngine.AddRHS(-1, new VariableB_ShiftAssign { Date = sd, Employee = e, Group = "O" }));
                        optEngine.CreateGreatEqual($"{ConstraintName}_lb@{d:yyyy_MM_dd}@{e}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
