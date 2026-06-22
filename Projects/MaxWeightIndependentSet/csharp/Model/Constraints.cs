using System.Collections.Generic;

using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace MaxWeightIndependentSet.Model
{
    // (C1) Edge conflict：相鄰兩點不可同時入選  x_i + x_j ≤ 1  ∀(i,j)∈E
    // LHS = x_i + x_j（係數皆 +1），RHS = 1，方向 ≤。不得移項/改號/翻轉方向。
    public class Constraint_EdgeConflict : ConstraintBase
    {
        private readonly OptEngine _engine;
        private readonly List<(string I, string J)> _edges;

        public Constraint_EdgeConflict(List<(string I, string J)> edges, OptEngine engine)
        {
            _edges = edges;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var (i, j) in _edges)
            {
                _engine.AddLHS(1, new VariableY_Select { NODE = i });
                _engine.AddLHS(1, new VariableY_Select { NODE = j });
                _engine.AddRHS(1);
                _engine.CreateLessEqual($"{ConstraintName}@{i}@{j}");
                ConstraintCount++;
            }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
