using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>s^dfl[e,d]：連休 2 天旗標（手寫版）。</summary>
    public class VariableB_DoubleOffFlag : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
    }
}
