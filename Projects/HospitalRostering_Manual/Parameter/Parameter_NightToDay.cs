using OptimFoundation.Core;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>不良班別轉換成本 R=(g',g)（手寫版）。</summary>
    public class Parameter_NightToDay : ParameterBase
    {
        public string PreGroup { get; set; } = string.Empty;
        public string Group    { get; set; } = string.Empty;
        public double QTY      { get; set; }
    }
}
