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
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_GreatEqual(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                const double lowerBound = 5;

                dataload.SetA.ForEach(a =>
                {
                    optEngine.AddLHS(1, new VariableX_A { A = a });

                    dataload.SetC.ForEach(c =>
                        optEngine.AddLHS(1, new VariableB_AC { A = a, C = c }));

                    optEngine.AddRHS(lowerBound);
                    optEngine.CreateGreatEqual($"{ConstraintName}@{a}");
                    ConstraintCount++;
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
