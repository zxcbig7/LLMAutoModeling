using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>本月投入某原料到某牌號的重量（kg）；對應 Model.md 的 Blend_{RawMaterial,CandyBrand}，Continuous、LB=0、UB=INFTY。</summary>
    [OptVar]
    [OptDim<string>("RawMaterial")]
    [OptDim<string>("CandyBrand")]
    public sealed partial class VariableC_Blend { }
}
