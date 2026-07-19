using OptimFoundation.Modeling;
using HospitalRostering_Manual.Set;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>預排班 PA=(e,d,g)（純 key，無 QTY）。body 由 AutoSetsGenerator 生成。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Date>("Date")]
    [OptDim<Set_Employee>("Employee")]
    [OptDim<Set_Group>("Group")]
    public partial class Parameter_PreAssign { }
}
