using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>s^ntd[e,d]：不良班別轉換指示（手寫版）。</summary>
    public class VariableB_NightToDay : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
    }
}
