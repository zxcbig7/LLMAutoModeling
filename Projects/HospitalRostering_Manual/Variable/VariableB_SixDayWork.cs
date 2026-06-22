using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>s^six[e,d]：連續工作 6 天指示（手寫版）。</summary>
    public class VariableB_SixDayWork : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
    }
}
