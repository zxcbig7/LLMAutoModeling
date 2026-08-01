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
        private readonly OptEngine engine;
        private readonly Set_A setA;
        private readonly Set_C setC;

        public Constraint_VarOnRHS(OptEngine engine, Set_A setA, Set_C setC)
        {
            this.engine = engine;
            this.setA = setA;
            this.setC = setC;
        }

        public void Build()
        {
            // ① VariableB_AC[a][c] ≤ VariableB_A[a]
            foreach (var a in setA)
            {
                foreach (var c in setC)
                {
                    engine.AddLHS(1, new VariableB_AC { A = a, C = c });
                    engine.AddRHS(1, new VariableB_A { A = a }); // 正係數 RHS

                    engine.CreateLessEqual($"{ConstraintName}_1@{a}@{c:yyyy_MM_dd}");
                    ConstraintCount++;
                }
            }

            // ② 前後期關聯：含正/負係數
            foreach (var a in setA)
            {
                foreach (var c in setC)
                {
                    var prevC = setC.FirstOrDefault(sd => sd == c.AddDays(-1));
                    if (prevC == default) continue; // 第一期無前期 → 跳過

                    engine.AddLHS(1, new VariableB_AC { A = a, C = c });

                    engine.AddRHS(1, new VariableB_AC { A = a, C = prevC }); // 正係數
                    engine.AddRHS(1, new VariableB_A { A = a }); // 正係數
                    engine.AddRHS(-1); // 負常數

                    engine.CreateGreatEqual($"{ConstraintName}_2@{a}@{c:yyyy_MM_dd}");
                    ConstraintCount++;
                }
            }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
