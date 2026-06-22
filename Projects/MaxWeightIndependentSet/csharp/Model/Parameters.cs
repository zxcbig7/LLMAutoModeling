using OptimFoundation.Core;

namespace MaxWeightIndependentSet.Model
{
    // 節點權重 w_i（AML Parameter）
    public class Param_Weight : ParameterBase
    {
        public string NODE { get; set; }
        public double QTY { get; set; }
    }
}
