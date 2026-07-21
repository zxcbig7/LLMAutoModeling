using OptimFoundation.Core;

namespace MaxWeightIndependentSet.Model
{
    // x_i ∈ {0,1}：節點 i 是否選入獨立集（二元 → BuildBVs）
    public class VariableB_Select : VariableBase
    {
        public string Node { get; set; }
    }
}
