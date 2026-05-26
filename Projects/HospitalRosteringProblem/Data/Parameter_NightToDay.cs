using OptimFoundation.Core;

namespace SandBox.Data
{
    public class Parameter_NightToDay : ParameterBase
    {
        public string PreGroup { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public double QTY { get; set; }
    }
}
