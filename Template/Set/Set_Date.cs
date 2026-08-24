using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>規劃期間的每一天；對應 Model.md 的 SET Date。示範非 string 維度（型別由 [OptDim<T>] 決定，不必自己 parse）。</summary>
    [OptSet]
    [OptDim<DateTime>("Date")]
    public sealed partial class Set_Date { }
}
