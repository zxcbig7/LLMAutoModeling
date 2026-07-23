using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Variable
{
    /// <summary>Continuous 變數：二維 SetA × SetB。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<Set_A>("A")]
    [OptDim<Set_B>("B")]
    public partial class VariableX_AB { }
}
