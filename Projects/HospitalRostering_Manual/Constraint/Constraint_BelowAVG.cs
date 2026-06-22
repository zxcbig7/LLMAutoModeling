using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Constraint
{
    /// <summary>C10 休假不低於平均：Σ_d y[e,d,O] + z^avg[e] ≥ AVGOFF，∀e。（CreateGreatEqual）</summary>
    public class Constraint_BelowAVG : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_BelowAVG(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // AVGOFF：(總可排時段 - 總工作需求) / 人數，向下取整再保留 1 天彈性
                double totalEmp  = dataload.Employee.Count;
                double allShift  = dataload.Employee.Count * dataload.Date.Count;
                double allDemand = dataload.parameter_ShiftDemand.Where(w => w.Group != "O").Sum(s => s.QTY);
                double avgOff    = Math.Floor((allShift - allDemand) / totalEmp) - 1;

                dataload.Employee.ForEach(e =>
                {
                    dataload.Date.ForEach(d =>
                        optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = "O" }));
                    optEngine.AddLHS(1, new VariableX_BelowAVG { Employee = e });
                    optEngine.AddRHS(avgOff);
                    optEngine.CreateGreatEqual($"{ConstraintName}@{e}");
                    ConstraintCount++;
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}  (AVGOFF={Math.Floor((allShift - allDemand) / totalEmp) - 1})");
            }
            catch (Exception) { throw; }
        }
    }
}
