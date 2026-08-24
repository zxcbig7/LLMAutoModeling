using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>原料種類；對應 Model.md 的 SET RawMaterial（成員 MaterialA / MaterialB / MaterialC）。</summary>
    [OptSet]
    [OptDim<string>("RawMaterial")]
    public sealed partial class Set_RawMaterial { }
}
