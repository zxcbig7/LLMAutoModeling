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
    /// 重點：窗口不足時（期初）跳過，不建立該限制式。
    /// </summary>
    public class Constraint_Window : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly IReadOnlyList<Set_A> setA;
        private readonly IReadOnlyList<Set_C> setC;
        private readonly int windowSize;
        private readonly double windowMax;

        public Constraint_Window(OptEngine engine, IReadOnlyList<Set_A> setA, IReadOnlyList<Set_C> setC, int windowSize, double windowMax)
        {
            this.engine = engine;
            this.setA = setA;
            this.setC = setC;
            this.windowSize = windowSize;
            this.windowMax = windowMax;
        }

        public void Build()
        {
            foreach (var c in setC)
            {
                var window = setC
                    .Where(sd => c.C.AddDays(-(windowSize - 1)) <= sd.C && sd.C <= c.C)
                    .ToList();

                if (window.Count < windowSize) continue; // 視窗不足 → 跳過

                foreach (var a in setA)
                {
                    foreach (var wc in window)
                        engine.AddLHS(1, new VariableB_AC { A = a.A, C = wc.C });

                    engine.AddRHS(windowMax);
                    engine.CreateLessEqual(this, a, c);
                }
            }
        }
    }
}
