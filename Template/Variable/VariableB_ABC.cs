using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Variable
{
    /// <summary>Binary 變數：三維 SetA × SetB × SetC。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<string>("A")]
    [OptDim<string>("B")]
    [OptDim<DateTime>("C")]
    public partial class VariableB_ABC { }
}
