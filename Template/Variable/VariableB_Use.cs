using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>某品項某日是否開工；對應 Model.md 的 Use_{Item,Date}，Binary、LB=0、UB=1。</summary>
    [OptVar]
    [OptDim<string>("Item")]
    [OptDim<DateTime>("Date")]
    public sealed partial class VariableB_Use { }
}
