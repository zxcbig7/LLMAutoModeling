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
    ///   Σ_b  VariableB_ABC[a][b][c]  ≤  AssignMax
    /// </summary>
    public class Constraint_LessEqual : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly Set_A setA;
        private readonly Set_B setB;
        private readonly Set_C setC;
        private readonly double assignMax;

        public Constraint_LessEqual(OptEngine engine, Set_A setA, Set_B setB, Set_C setC, double assignMax)
        {
            this.engine = engine;
            this.setA = setA;
            this.setB = setB;
            this.setC = setC;
            this.assignMax = assignMax;
        }

        public void Build()
        {
            foreach (var a in setA)
            {
                foreach (var c in setC)
                {
                    foreach (var b in setB)
                        engine.AddLHS(1, new VariableB_ABC { A = a, B = b, C = c });

                    engine.AddRHS(assignMax);
                    engine.CreateLessEqual($"{ConstraintName}@{a}@{c:yyyy_MM_dd}");
                    ConstraintCount++;
                }
            }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
