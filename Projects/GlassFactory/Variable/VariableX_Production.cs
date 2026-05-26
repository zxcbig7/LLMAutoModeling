using OptimFoundation.Core;

namespace GlassFactory.Variable
{
    /// <summary>決策變數：每種玻璃的生產數量（Continuous, ≥ 0）</summary>
    public class VariableX_Production : VariableBase
    {
        public string GlassType { get; set; } = string.Empty;
    }
}
