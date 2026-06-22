using OptimModeling;

namespace HospitalRostering_Generator.Parameter
{
    /// <summary>不良班別轉換成本 R=(g',g)。body（PreGroup/Group/QTY + ctor）由 AutoSetsGenerator 生成。</summary>
    [OptParam("PreGroup", "Group")]
    public partial class Parameter_NightToDay { }
}
