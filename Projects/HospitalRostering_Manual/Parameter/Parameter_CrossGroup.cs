using OptimFoundation.Core;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>跨組別支援成本 CG_e（手寫版）。</summary>
    public class Parameter_CrossGroup : ParameterBase
    {
        public string Employee { get; set; } = string.Empty;
        public string Group    { get; set; } = string.Empty;
        public double QTY      { get; set; }
    }
}
