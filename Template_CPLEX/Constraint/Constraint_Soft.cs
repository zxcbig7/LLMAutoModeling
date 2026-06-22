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
    /// 示範（∀ a ∈ SetA）：Σ_b VariableX_AB[a][b] ≥ SoftTarget（軟性；不足就罰 penalty·短缺量）
    ///
    /// 前提：① 目標式必須已建立（BuildModel 先建 Objective 再建本限制式）。
    ///       ② penalty 一律從 Parameter / Dataload 取得，不得 hardcode。
    /// </summary>
    public class Constraint_Soft : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_Soft(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                if (!optEngine.SupportsSoftConstraints) return;

                double target  = dataload.SoftTarget;
                double penalty = dataload.Penalty_Soft;

                dataload.SetA.ForEach(a =>
                {
                    dataload.SetB.ForEach(b =>
                        optEngine.AddLHS(1, new VariableX_AB { A = a, B = b }));

                    // 軟性 ≥：加 Deficit 變數，短缺多少罰多少
                    optEngine.CreateGeSoft(target, penalty);
                    ConstraintCount++;

                    // 其他軟性方向：
                    // optEngine.CreateLeSoft(target, penalty);              // 軟性 ≤（加 Surplus）
                    // optEngine.CreateEqSoft(target, penalty, "Name@idx");  // 軟性 =（加 Delta_Pos/Neg）
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
