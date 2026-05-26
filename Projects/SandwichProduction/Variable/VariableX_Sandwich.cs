using OptimFoundation.Core;

namespace SandwichProduction.Variable
{
    /// <summary>
    /// x[i] ≥ 0（連續）
    /// 製作三明治 i 的數量。
    /// 變數名稱格式：VariableX_Sandwich@{SandwichType}
    /// </summary>
    public class VariableX_Sandwich : VariableBase
    {
        public string SandwichType { get; set; } = string.Empty;
    }
}
