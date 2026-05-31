using OptimFoundation.Core;

namespace WeeniesBuns.Parameter
{
    public class Parameter_ProductSpec : ParameterBase
    {
        public string ProductType   { get; set; } = string.Empty;
        public double FlourPerUnit  { get; set; }
        public double PorkPerUnit   { get; set; }
        public double LaborPerUnit  { get; set; }
        public double Profit        { get; set; }
    }
}
