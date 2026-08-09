using OptimFoundation.Modeling;

namespace Template.Set
{
    /// <summary>
    /// 稀疏有向弧集合；每個成員是一組 (From, To) tuple。
    /// CSV：Set_Arc.csv，表頭為 From,To；例如 A,B。
    /// </summary>
    [OptSet]
    [OptDim<string>("From")]
    [OptDim<string>("To")]
    public partial class Set_Arc { }
}
