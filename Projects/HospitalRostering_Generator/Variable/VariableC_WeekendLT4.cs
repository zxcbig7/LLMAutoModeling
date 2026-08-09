using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>z^wkd[e] ≥ 0：週末休假比 4 天少的天數。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<string>("Employee")]
    public partial class VariableC_WeekendLT4 { }
}
