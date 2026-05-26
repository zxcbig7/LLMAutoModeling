using OptimFoundation.Core;

namespace SandwichProduction.Parameter
{
    /// <summary>
    /// 每種三明治的利潤規格。
    /// </summary>
    public class Parameter_SandwichSpec : ParameterBase
    {
        public string SandwichType { get; set; } = string.Empty;
        public double Profit       { get; set; }
    }
}
