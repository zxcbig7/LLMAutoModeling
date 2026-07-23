using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 等式限制式（CreateEqual）
    ///
    /// ∀ b ∈ SetB, c ∈ SetC：
    ///   Σ_a  VariableB_ABC[a][b][c]  =  Demand[b]
    /// </summary>
    public class Constraint_Equality : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_Equality(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                foreach (var b in dataload.SetB)
                {
                    foreach (var c in dataload.SetC)
                    {
                        foreach (var a in dataload.SetA)
                            optEngine.AddLHS(1, new VariableB_ABC { A = a, B = b, C = c });

                        double demand = dataload.parameter_AB
                            .FirstOrDefault(p => p.B == b)?.QTY ?? 0;
                        optEngine.AddRHS(demand);

                        optEngine.CreateEqual($"{ConstraintName}@{b}@{c:yyyy_MM_dd}");
                        ConstraintCount++;
                    }
                }

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
