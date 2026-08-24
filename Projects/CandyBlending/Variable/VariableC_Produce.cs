using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>某牌號本月的產量（kg）；對應 Model.md 的 Produce_{CandyBrand}，Continuous、LB=0、UB=INFTY。</summary>
    [OptVar]
    [OptDim<string>("CandyBrand")]
    public sealed partial class VariableC_Produce { }
}
