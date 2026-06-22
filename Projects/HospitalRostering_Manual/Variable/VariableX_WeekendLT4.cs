using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>z^wkd[e] ≥ 0：週末休假比 4 天少的天數（手寫版）。</summary>
    public class VariableX_WeekendLT4 : VariableBase
    {
        public string Employee { get; set; } = string.Empty;
    }
}
