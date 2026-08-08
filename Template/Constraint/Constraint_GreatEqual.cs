using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 下界限制式（CreateGreatEqual）— 含 Continuous 變數
    ///
    /// ∀ a ∈ SetA：
    ///   VariableX_A[a]  +  Σ_c  VariableB_AC[a][c]  ≥  LowerBound
    /// </summary>
    public class Constraint_GreatEqual : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly Set_A setA;
        private readonly Set_C setC;
        private readonly double lowerBound;

        public Constraint_GreatEqual(OptEngine engine, Set_A setA, Set_C setC, double lowerBound)
        {
            this.engine = engine;
            this.setA = setA;
            this.setC = setC;
            this.lowerBound = lowerBound;
        }

        public void Build()
        {
            foreach (var a in setA)
            {
                engine.AddLHS(1, new VariableX_A { A = a });

                foreach (var c in setC)
                    engine.AddLHS(1, new VariableB_AC { A = a, C = c });

                engine.AddRHS(lowerBound);
                engine.CreateGreatEqual($"{ConstraintName}@{a}");
            }
        }
    }
}
