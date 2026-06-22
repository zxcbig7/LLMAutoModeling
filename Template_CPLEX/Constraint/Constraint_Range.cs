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
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_Range(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                double lb = dataload.RangeLB;
                double ub = dataload.RangeUB;

                dataload.SetA.ForEach(a =>
                {
                    dataload.SetC.ForEach(c =>
                        optEngine.AddLHS(1, new VariableB_AC { A = a, C = c }));

                    optEngine.CreateRange(lb, ub, $"{ConstraintName}@{a}");
                    ConstraintCount++;
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
