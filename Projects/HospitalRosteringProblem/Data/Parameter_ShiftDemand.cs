using OptimFoundation.Core;

namespace SandBox.Data
{
    public class Parameter_ShiftDemand : ParameterBase
    {
        public DateTime Date { get; set; }
        public string Group { get; set; } = string.Empty;
        public double QTY { get; set; }
    }
}
