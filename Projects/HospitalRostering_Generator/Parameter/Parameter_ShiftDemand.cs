using OptimFoundation.Modeling;
using HospitalRostering_Generator.Set;

namespace HospitalRostering_Generator.Parameter
{
    /// <summary>每日各工作班別人力需求 Demand[d,g]。body（Date/Group/QTY + ctor）由 AutoSetsGenerator 生成。</summary>
    [OptParam]
    [OptDim<System.DateTime>("Date")]
    [OptDim<string>("Group")]
    public partial class Parameter_ShiftDemand { }
}
