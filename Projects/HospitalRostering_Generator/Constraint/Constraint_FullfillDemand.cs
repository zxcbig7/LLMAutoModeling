using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C2 每日各班別人力需求：Σ_e y[e,d,g] = Demand[d,g]，∀d, g∈G⁺。（CreateEqual）</summary>
    public class Constraint_FullfillDemand : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_FullfillDemand(Dataload dataload, OptEngine engine)
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
                    dataload.Group.Where(g => g != "O").ToList().ForEach(g =>
                    {
                        dataload.Employee.ForEach(e =>
                            optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = d, Employee = e, Group = g }));

                        double demand = dataload.parameter_ShiftDemand
                            .FirstOrDefault(x => x.Date == d && x.Group == g)?.QTY ?? 0;
                        optEngine.AddRHS(demand);
                        optEngine.CreateEqual($"{ConstraintName}@{d:yyyy_MM_dd}@{g}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
