using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 區間限制式（CreateRange）：lb ≤ Σ ≤ ub 用單一窗口建立，不拆成兩條 ≤ / ≥。
    ///
    /// ∀ a ∈ SetA：  RangeLB ≤ Σ_c VariableB_AC[a][c] ≤ RangeUB
    ///
    /// 注意：CreateRange 只吃 LHS 累加項（AddRHS 不參與）；常數項 AddLHS(const) 會併入界限平移。
    /// </summary>
    public class Constraint_Range : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly Set_A setA;
        private readonly Set_C setC;
        private readonly double lb;
        private readonly double ub;

        public Constraint_Range(OptEngine engine, Set_A setA, Set_C setC, double lb, double ub)
        {
            this.engine = engine;
            this.setA = setA;
            this.setC = setC;
            this.lb = lb;
            this.ub = ub;
        }

        public void Build()
        {
            foreach (var a in setA)
            {
                foreach (var c in setC)
                    engine.AddLHS(1, new VariableB_AC { A = a, C = c });

                engine.CreateRange(lb, ub, $"{ConstraintName}@{a}");
            }
        }
    }
}
