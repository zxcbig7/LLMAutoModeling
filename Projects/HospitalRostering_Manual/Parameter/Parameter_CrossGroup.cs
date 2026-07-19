using OptimFoundation.Modeling;
using HospitalRostering_Manual.Set;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>跨組別支援成本 CG_e。body（Employee/Group/QTY + ctor）由 AutoSetsGenerator 生成。</summary>
    [OptParam]
    [OptDim<Set_Employee>("Employee")]
    [OptDim<Set_Group>("Group")]
    public partial class Parameter_CrossGroup { }
}
