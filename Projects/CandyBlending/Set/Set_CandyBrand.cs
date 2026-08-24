using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>糖果牌號；對應 Model.md 的 SET CandyBrand（成員 BrandJia / BrandYi / BrandBing）。</summary>
    [OptSet]
    [OptDim<string>("CandyBrand")]
    public sealed partial class Set_CandyBrand { }
}
