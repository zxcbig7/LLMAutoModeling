using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>s^mis[e,d]：跨組別支援指示。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<DateTime>("Date")]
    [OptDim<string>("Employee")]
    public partial class VariableB_GroupMismatch { }
}
