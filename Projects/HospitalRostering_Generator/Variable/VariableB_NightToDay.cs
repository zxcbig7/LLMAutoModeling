using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>s^ntd[e,d]：不良班別轉換指示。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<DateTime>("Date")]
    [OptDim<string>("Employee")]
    public partial class VariableB_NightToDay { }
}
