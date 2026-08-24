using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>某品項的需求缺口；對應 Model.md 的 Shortage_{Item}，Continuous、LB=0、UB=INFTY。</summary>
    [OptVar]
    [OptDim<string>("Item")]
    public sealed partial class VariableC_Shortage { }
}
