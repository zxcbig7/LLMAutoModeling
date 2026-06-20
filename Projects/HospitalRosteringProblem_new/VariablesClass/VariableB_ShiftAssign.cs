using OptimModeling;

namespace SandBox.VariableClass
{
    // 宣告式：屬性（Date/Employee/Group）由 AutoSetsGenerator 於編譯期生成。
    // 順序必須對應 BuildBVs<VariableB_ShiftAssign>(data.Date, data.Employee, data.Group)。
    [OptVar(VarType.Binary, "Date:DateTime", "Employee", "Group")]
    public partial class VariableB_ShiftAssign { }
}
