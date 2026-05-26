using OptimFoundation.Core;

namespace SandBox.Data
{
    public class Parameter_CrossGroup : ParameterBase
    {
        public string Employee { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public double QTY { get; set; }
    }
}
