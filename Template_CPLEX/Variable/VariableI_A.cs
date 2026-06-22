using OptimModeling;

namespace Template.Variable
{
    /// <summary>
    /// 整數變數：A → 整數量（示範 BuildIVs / GetIVSolution）。body 由 AutoSetsGenerator 生成。
    /// 整數界限在 VariableCreate 以 BuildIVs(lb, ub, sets…) 指定，VarType 僅作標記。
    /// </summary>
    [OptVar(VarType.Integer, "A")]
    public partial class VariableI_A { }
}
