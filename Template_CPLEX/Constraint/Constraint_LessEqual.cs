using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 上界限制式（CreateLessEqual）
    ///
    /// ∀ a ∈ SetA, c ∈ SetC：
    ///   Σ_b  VariableB_ABC[a][b][c]  ≤  1
    /// </summary>
    public class Constraint_LessEqual : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_LessEqual(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                dataload.SetA.ForEach(a =>
                {
                    dataload.SetC.ForEach(c =>
                    {
                        dataload.SetB.ForEach(b =>
                            optEngine.AddLHS(1, new VariableB_ABC { A = a, B = b, C = c }));

                        optEngine.AddRHS(1);
                        optEngine.CreateLessEqual($"{ConstraintName}@{a}@{c:yyyy_MM_dd}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
