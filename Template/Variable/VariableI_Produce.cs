using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>某品項某日的生產量（整數）；對應 Model.md 的 Produce_{Item,Date}，Integer、LB=0、UB=INFTY。
    /// domain 是 Set_AllowedSlot（稀疏），不是 Item × Date 的完整笛卡兒積。</summary>
    [OptVar]
    [OptDim<string>("Item")]
    [OptDim<DateTime>("Date")]
    public sealed partial class VariableI_Produce { }
}
