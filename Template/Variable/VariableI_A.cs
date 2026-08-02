using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Variable
{
    /// <summary>
    /// 整數變數：A → 整數量（示範 BuildIVs / GetIVSolution）。body 由 AutoSetsGenerator 生成。
    /// 整數界限在 Program.cs 的 CreateVariables 以 BuildIVs(lb, ub, sets…) 指定，VarType 僅作標記。
    /// </summary>
    [OptVar]
    [OptDim<Set_A>("A")]
    public partial class VariableI_A { }
}
