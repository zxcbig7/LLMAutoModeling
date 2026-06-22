using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>z^avg[e] ≥ 0：休假比 AVGOFF 少的天數（手寫版）。</summary>
    public class VariableX_BelowAVG : VariableBase
    {
        public string Employee { get; set; } = string.Empty;
    }
}
