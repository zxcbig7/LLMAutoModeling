using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 滑動時間視窗限制式（CreateLessEqual）
    ///
    /// ∀ a ∈ SetA, c ∈ SetC（視窗期數足夠時）：
    ///   Σ_{c' ∈ [c-W+1..c]}  VariableB_AC[a][c']  ≤  WindowMax
    ///
    /// 重點：窗口不足時（期初）return 跳過，不建立該限制式。
    /// </summary>
    public class Constraint_Window : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_Window(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                int    windowSize = 7;
                double windowMax  = 5;

                dataload.SetC.ForEach(c =>
                {
                    var window = dataload.SetC
                        .Where(sd => c.AddDays(-(windowSize - 1)) <= sd && sd <= c)
                        .ToList();

                    if (window.Count < windowSize) return;  // 視窗不足 → 跳過

                    dataload.SetA.ForEach(a =>
                    {
                        window.ForEach(wc =>
                            optEngine.AddLHS(1, new VariableB_AC { A = a, C = wc }));

                        optEngine.AddRHS(windowMax);
                        optEngine.CreateLessEqual($"{ConstraintName}@{a}@{c:yyyy_MM_dd}");
                        ConstraintCount++;
                    });
                });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
