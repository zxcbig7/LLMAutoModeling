using OptimModeling;

namespace SandBox.Data
{
    // 宣告式：Date/Group/QTY 屬性 + InitClassBySets 兩個建構子由 AutoSetsGenerator 生成。
    [OptParam("Date:DateTime", "Group")]
    public partial class Parameter_ShiftDemand { }
}
