using OptimFoundation.Core;

namespace ClinicVitamin.Parameter
{
    /// <summary>
    /// 每種產品的供應人數規格。
    /// </summary>
    public class Parameter_ProductSpec : ParameterBase
    {
        public string ProductType    { get; set; } = string.Empty;
        public double PeopleSupply   { get; set; }
    }
}
