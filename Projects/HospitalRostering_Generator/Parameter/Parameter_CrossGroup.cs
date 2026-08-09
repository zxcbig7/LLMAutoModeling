using OptimFoundation.Modeling;
using HospitalRostering_Generator.Set;

namespace HospitalRostering_Generator.Parameter
{
    /// <summary>跨組別支援成本 CG_e。body（Employee/Group/QTY + ctor）由 AutoSetsGenerator 生成。</summary>
    [OptParam]
    [OptDim<string>("Employee")]
    [OptDim<string>("Group")]
    public partial class Parameter_CrossGroup { }
}
