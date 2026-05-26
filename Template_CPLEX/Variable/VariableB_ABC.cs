using OptimFoundation.Core;

namespace Template.Variable
{
    /// <summary>Binary 變數：三維 SetA × SetB × SetC</summary>
    public class VariableB_ABC : VariableBase
    {
        public string   A { get; set; } = string.Empty;
        public string   B { get; set; } = string.Empty;
        public DateTime C { get; set; }
    }
}
