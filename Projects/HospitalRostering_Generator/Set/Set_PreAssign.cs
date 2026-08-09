using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>預排班的（日期、員工、班別）可行 tuple。</summary>
    [OptSet]
    [OptDim<System.DateTime>("Date")]
    [OptDim<string>("Employee")]
    [OptDim<string>("Group")]
    public partial class Set_PreAssign { }
}
