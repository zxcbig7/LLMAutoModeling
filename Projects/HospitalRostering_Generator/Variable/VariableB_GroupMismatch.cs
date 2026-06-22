using OptimModeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>s^mis[e,d]：跨組別支援指示。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar(VarType.Binary, "Date:DateTime", "Employee")]
    public partial class VariableB_GroupMismatch { }
}
