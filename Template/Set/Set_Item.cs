using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>可生產的品項；對應 Model.md 的 SET Item。</summary>
    [OptSet]
    [OptDim<string>("Item")]
    public sealed partial class Set_Item { }
}
