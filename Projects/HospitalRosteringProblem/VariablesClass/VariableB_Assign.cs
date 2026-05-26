using OptimFoundation.Core;

namespace SandBox.VariableClass
{
    /// <summary>
    /// Binary 變數範本：表示 Item 在 Period 是否被指派到 Machine（0 或 1）。
    /// 屬性順序對應 BuildBVs&lt;T&gt;() 傳入 sets 的順序。
    /// </summary>
    public class VariableB_Assign : VariableBase
    {
        public string   Item    { get; set; } = string.Empty;
        public string   Machine { get; set; } = string.Empty;
        public DateTime Period  { get; set; }
    }
}
