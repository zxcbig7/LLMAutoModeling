using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>y[e,d,g]：核心決策，員工 e 在 d 排班別 g。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar]
    [OptDim<DateTime>("Date")]
    [OptDim<string>("Employee")]
    [OptDim<string>("Group")]
    public partial class VariableB_ShiftAssign { }
}
