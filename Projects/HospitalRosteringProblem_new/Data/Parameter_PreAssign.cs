using OptimModeling;

namespace SandBox.Data
{
    // 純 key 參數（無 QTY）：HasValue = false。Date/Employee/Group + 建構子由 generator 生成。
    [OptParam("Date:DateTime", "Employee", "Group", HasValue = false)]
    public partial class Parameter_PreAssign { }
}
