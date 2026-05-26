using OptimFoundation.Core;

namespace Template.Parameter
{
    /// <summary>三鍵參數：A × B × C → QTY</summary>
    public class Parameter_ABC : ParameterBase
    {
        public string   A   { get; set; } = string.Empty;
        public string   B   { get; set; } = string.Empty;
        public DateTime C   { get; set; }
        public double   QTY { get; set; }
    }
}
