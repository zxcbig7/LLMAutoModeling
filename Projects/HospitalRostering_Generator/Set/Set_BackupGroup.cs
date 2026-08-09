using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>Backup 班別的（員工、班別）tuple。</summary>
    [OptSet]
    [OptDim<string>("Employee")]
    [OptDim<string>("Group")]
    public partial class Set_BackupGroup { }
}
