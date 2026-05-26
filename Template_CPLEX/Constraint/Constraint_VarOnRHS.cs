using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 變數移項到 RHS 的限制式（AddRHS with variable）
    ///
    /// ① LessEqual + 正係數：
    ///   ∀ a, c：VariableB_AC[a][c] ≤ VariableB_A[a]
    ///   → AddLHS(1, VariableB_AC)
    ///     AddRHS(1, VariableB_A)      ← 變數放 RHS
    ///     CreateLessEqual
    ///
    /// ② GreatEqual + 正/負係數組合：
    ///   ∀ a, c（c > 第一期）：
    ///     VariableB_AC[a][c] ≥ VariableB_AC[a][prev_c] + VariableB_A[a] - 1
    ///   → AddLHS(1, VariableB_AC[a][c])
    ///     AddRHS( 1, VariableB_AC[a][prev_c])
    ///     AddRHS( 1, VariableB_A[a])
    ///     AddRHS(-1)                  ← 負常數
    ///     CreateGreatEqual
    /// </summary>
    public class Constraint_VarOnRHS : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_VarOnRHS(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // ① VariableB_AC[a][c] ≤ VariableB_A[a]
                dataload.SetA.ForEach(a =>
                {
                    dataload.SetC.ForEach(c =>
                    {
                        optEngine.AddLHS(1, new VariableB_AC { A = a, C = c });
                        optEngine.AddRHS(1, new VariableB_A  { A = a });          // 正係數 RHS

                        optEngine.CreateLessEqual($"{ConstraintName}_1@{a}@{c:yyyy_MM_dd}");
                        ConstraintCount++;
                    });
                });

                // ② 前後期關聯：含正/負係數
                dataload.SetA.ForEach(a =>
                {
                    dataload.SetC.ForEach(c =>
                    {
                        var prevC = dataload.SetC.FirstOrDefault(sd => sd == c.AddDays(-1));
                        if (prevC == default) return;  // 第一期無前期 → 跳過

                        optEngine.AddLHS(1, new VariableB_AC { A = a, C = c });

                        optEngine.AddRHS( 1, new VariableB_AC { A = a, C = prevC }); // 正係數
                        optEngine.AddRHS( 1, new VariableB_A  { A = a });             // 正係數
                        optEngine.AddRHS(-1);                                          // 負常數

                        optEngine.CreateGreatEqual($"{ConstraintName}_2@{a}@{c:yyyy_MM_dd}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
