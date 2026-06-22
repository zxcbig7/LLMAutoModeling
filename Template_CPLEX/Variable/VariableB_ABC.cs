using OptimModeling;

namespace Template.Variable
{
    /// <summary>Binary 變數：三維 SetA × SetB × SetC。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar(VarType.Binary, "A", "B", "C:DateTime")]
    public partial class VariableB_ABC { }
}
