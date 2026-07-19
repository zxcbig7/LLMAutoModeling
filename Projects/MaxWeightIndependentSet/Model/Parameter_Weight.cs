using OptimFoundation.Modeling;

namespace MaxWeightIndependentSet.Model
{
    // 節點權重 w_i（Model Parameter）。NODE 為 index-set（由 [OptDim<Set_Node>] 生成），值在 QTY。
    [OptParam]
    [OptDim<Set_Node>("NODE")]
    public partial class Parameter_Weight { }
}
