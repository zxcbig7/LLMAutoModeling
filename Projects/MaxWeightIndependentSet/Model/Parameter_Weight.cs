using OptimFoundation.Modeling;

namespace MaxWeightIndependentSet.Model
{
    // 節點權重 w_i（Model Parameter）。Node 為 index-set（由 [OptDim<Set_Node>] 生成），值在 QTY。
    [OptParam]
    [OptDim<Set_Node>("Node")]
    public partial class Parameter_Weight { }
}
