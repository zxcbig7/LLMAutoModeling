using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 軟性限制式（CreateGeSoft / CreateLeSoft / CreateEqSoft）。
    /// 框架自動加彈性變數（Deficit / Surplus / Delta）並把 penalty 併入目標式，
    /// 不需手動改 Objective。違反量 = 彈性變數的解值（Deficit_* / Surplus_* / Delta_*）。
    ///
    /// 示範（∀ a ∈ SetA）：Σ_b VariableC_AB[a][b] ≥ SoftTarget（軟性；不足就罰 penalty·短缺量）
    ///
    /// 前提：目標式必須已建立（Program.cs 的 BuildModel 先建 Objective 再建本限制式）。
    /// target / penalty 由建構子傳入，本類別不得寫死任何數字。
    /// </summary>
    public class Constraint_Soft : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly IReadOnlyList<Set_A> setA;
        private readonly IReadOnlyList<Set_B> setB;
        private readonly double target;
        private readonly double penalty;

        public Constraint_Soft(OptEngine engine, IReadOnlyList<Set_A> setA, IReadOnlyList<Set_B> setB, double target, double penalty)
        {
            this.engine = engine;
            this.setA = setA;
            this.setB = setB;
            this.target = target;
            this.penalty = penalty;
        }

        public void Build()
        {
            if (!engine.SupportsSoftConstraints) return;

            foreach (var a in setA)
            {
                foreach (var b in setB)
                    engine.AddLHS(1, new VariableC_AB { A = a.A, B = b.B });

                // 軟性 ≥：加 Deficit 變數，短缺多少罰多少
                engine.CreateGeSoft(target, penalty, this, a);

                // 其他軟性方向：
                // engine.CreateLeSoft(target, penalty, this, a); // 軟性 ≤（加 Surplus）
                // engine.CreateEqSoft(target, penalty, this, a); // 軟性 =（加 Delta_Pos/Neg）
            }
        }
    }
}
