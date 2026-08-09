using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>s^off1[e,d]：做一休一做指示。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<DateTime>("Date")]
    [OptDim<string>("Employee")]
    public partial class VariableB_Off1Day { }
}
