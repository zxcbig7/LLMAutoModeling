using OptimFoundation.Core;

namespace WeeniesBuns.Variable
{
    /// <summary>決策變數：每種產品的週產量（Continuous, ≥ 0）</summary>
    public class VariableX_Production : VariableBase
    {
        public string ProductType { get; set; } = string.Empty;
    }
}
