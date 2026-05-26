using OptimFoundation.Core;

namespace SandBox.Data
{
    public class Parameter_PreAssign : ParameterBase
    {
        public DateTime Date { get; set; }
        public string Employee { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }
}
