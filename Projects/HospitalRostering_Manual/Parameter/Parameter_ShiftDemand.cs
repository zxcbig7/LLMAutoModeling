using OptimFoundation.Core;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>每日各工作班別人力需求 Demand[d,g]（手寫版）。</summary>
    public class Parameter_ShiftDemand : ParameterBase
    {
        public DateTime Date  { get; set; }
        public string   Group { get; set; } = string.Empty;
        public double   QTY   { get; set; }
    }
}
