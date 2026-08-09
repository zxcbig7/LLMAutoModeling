using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Parameter;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 等式限制式（CreateEqual）
    ///
    /// ∀ b ∈ SetB, c ∈ SetC：
    ///   Σ_a  VariableB_ABC[a][b][c]  =  Demand[b]
    ///
    /// 建構子只收本條式子用得到的 Set row 清單與參數，NEVER 收整包 Dataload。
    /// </summary>
    public class Constraint_Equality : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly IReadOnlyList<Set_A> setA;
        private readonly IReadOnlyList<Set_B> setB;
        private readonly IReadOnlyList<Set_C> setC;
        private readonly List<Parameter_AB> demand;

        public Constraint_Equality(OptEngine engine, IReadOnlyList<Set_A> setA, IReadOnlyList<Set_B> setB, IReadOnlyList<Set_C> setC, List<Parameter_AB> demand)
        {
            this.engine = engine;
            this.setA = setA;
            this.setB = setB;
            this.setC = setC;
            this.demand = demand;
        }

        public void Build()
        {
            foreach (var b in setB)
            {
                foreach (var c in setC)
                {
                    foreach (var a in setA)
                        engine.AddLHS(1, new VariableB_ABC { A = a.A, B = b.B, C = c.C });

                    double qty = demand.FirstOrDefault(p => p.B == b.B)?.QTY ?? 0;
                    engine.AddRHS(qty);

                    engine.CreateEqual(this, b, c);
                }
            }
        }
    }
}
