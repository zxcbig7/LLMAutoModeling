using OptimFoundation.Core;

namespace GlassFactory.Parameter
{
    /// <summary>
    /// 每種玻璃的規格參數。
    /// </summary>
    public class Parameter_GlassSpec : ParameterBase
    {
        public string GlassType    { get; set; } = string.Empty;
        public double HeatingTime  { get; set; }
        public double CoolingTime  { get; set; }
        public double Profit       { get; set; }
    }
}
