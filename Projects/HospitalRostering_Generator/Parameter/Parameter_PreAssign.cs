using OptimModeling;

namespace HospitalRostering_Generator.Parameter
{
    /// <summary>預排班 PA=(e,d,g)（純 key，無 QTY）。body 由 AutoSetsGenerator 生成。</summary>
    [OptParam("Date:DateTime", "Employee", "Group", HasValue = false)]
    public partial class Parameter_PreAssign { }
}
