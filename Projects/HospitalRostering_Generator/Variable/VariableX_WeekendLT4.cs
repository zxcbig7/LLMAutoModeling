using OptimModeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>z^wkd[e] ≥ 0：週末休假比 4 天少的天數。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar(VarType.Continuous, "Employee")]
    public partial class VariableX_WeekendLT4 { }
}
