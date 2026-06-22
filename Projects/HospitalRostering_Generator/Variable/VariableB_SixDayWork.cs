using OptimModeling;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>s^six[e,d]：連續工作 6 天指示。body 由 AutoSetsGenerator 生成。</summary>
    [OptVar(VarType.Binary, "Date:DateTime", "Employee")]
    public partial class VariableB_SixDayWork { }
}
