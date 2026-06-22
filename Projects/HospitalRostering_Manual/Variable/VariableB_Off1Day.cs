using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>s^off1[e,d]：做一休一做指示（手寫版）。</summary>
    public class VariableB_Off1Day : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
    }
}
