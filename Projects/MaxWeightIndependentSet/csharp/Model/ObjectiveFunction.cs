using System.Collections.Generic;
using System.Linq;

using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace MaxWeightIndependentSet.Model
{
    // 目標：max Σ_{i∈V} w_i x_i
    public class ObjectiveFunction
    {
        private readonly OptEngine _engine;
        private readonly List<string> _nodes;
        private readonly List<Param_Weight> _weights;

        public ObjectiveFunction(List<string> nodes, List<Param_Weight> weights, OptEngine engine)
        {
            _nodes = nodes;
            _weights = weights;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var n in _nodes)
            {
                // 先 LINQ 查到權重存進變數，再傳入 AddLHS（禁止把 LINQ 直接塞進 AddLHS）
                double w = _weights.FirstOrDefault(x => x.NODE == n)?.QTY ?? 0.0;
                _engine.AddLHS(w, new VariableY_Select { NODE = n });
            }

            _engine.CreateMaximize();
            Logging.Info("Objective built (maximize total weight)");
        }
    }
}
