using OptimModeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>z^avg[e] ≥ 0：休假比 AVGOFF 少的天數。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar(VarType.Continuous, "Employee")]
    public partial class VariableX_BelowAVG { }
}
