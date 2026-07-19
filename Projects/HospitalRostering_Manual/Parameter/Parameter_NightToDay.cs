using OptimFoundation.Modeling;
using HospitalRostering_Manual.Set;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>不良班別轉換成本 R=(g',g)。body（PreGroup/Group/QTY + ctor）由 AutoSetsGenerator 生成。
    /// PreGroup 與 Group 兩個維度皆指向同一個 Set_Group（同 set 多維度，各自具名）。</summary>
    [OptParam]
    [OptDim<Set_Group>("PreGroup")]
    [OptDim<Set_Group>("Group")]
    public partial class Parameter_NightToDay { }
}
