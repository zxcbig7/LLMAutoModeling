using OptimFoundation.Core;

namespace Template.Parameter
{
    /// <summary>二鍵參數：A × B → QTY</summary>
    public class Parameter_AB : ParameterBase
    {
        public string A   { get; set; } = string.Empty;
        public string B   { get; set; } = string.Empty;
        public double QTY { get; set; }
    }
}
