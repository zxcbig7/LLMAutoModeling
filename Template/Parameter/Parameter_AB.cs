using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Parameter
{
    /// <summary>二鍵參數：A × B → QTY。body（A/B/QTY + ctor）由 AutoSetsGenerator 生成。</summary>
    [OptParam]
    [OptDim<string>("A")]
    [OptDim<string>("B")]
    public partial class Parameter_AB { }
}
