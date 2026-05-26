using OptimFoundation.Core;

namespace SandBox.VariableClass
{
    /// <summary>
    /// Binary 輔助變數：標記某 Item 在某 Period 是否有超額（用於目標式罰分）。
    /// </summary>
    public class VariableB_Overflow : VariableBase
    {
        public string   Item   { get; set; } = string.Empty;
        public DateTime Period { get; set; }
    }
}
