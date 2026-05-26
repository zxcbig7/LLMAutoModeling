using OptimFoundation.Core;

namespace SandBox.Data
{
    /// <summary>
    /// 簡單參數：屬性即為 Key，QTY 為值。不需要建構子。
    /// </summary>
    public class Parameter_GenericDemand : ParameterBase
    {
        public string   Item   { get; set; } = string.Empty;
        public DateTime Period { get; set; }
        public double   QTY    { get; set; }
    }
}
