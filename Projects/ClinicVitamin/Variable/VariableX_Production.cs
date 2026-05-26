using OptimFoundation.Core;

namespace ClinicVitamin.Variable
{
    /// <summary>
    /// x[p] ≥ 0（連續）
    /// 生產產品 p 的批次數。
    /// 變數名稱格式：VariableX_Production@{ProductType}
    /// </summary>
    public class VariableX_Production : VariableBase
    {
        public string ProductType { get; set; } = string.Empty;
    }
}
