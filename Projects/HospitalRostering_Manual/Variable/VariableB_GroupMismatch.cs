using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>s^mis[e,d]：跨組別支援指示（手寫版）。</summary>
    public class VariableB_GroupMismatch : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
    }
}
