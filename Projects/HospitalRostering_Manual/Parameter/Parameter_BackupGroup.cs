using OptimFoundation.Modeling;
using HospitalRostering_Manual.Set;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>員工 Backup 班別（純 key，無 QTY）。body 由 AutoSetsGenerator 生成。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Employee>("Employee")]
    [OptDim<Set_Group>("Group")]
    public partial class Parameter_BackupGroup { }
}
