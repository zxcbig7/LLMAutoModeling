using OptimFoundation.Core;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>預排班 PA=(e,d,g)（純 key，無 QTY；手寫版）。</summary>
    public class Parameter_PreAssign : ParameterBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
        public string   Group    { get; set; } = string.Empty;
    }
}
