using OptimFoundation.Core;

namespace Template.Variable
{
    /// <summary>
    /// 變數空白範本：展示所有支援的 Set 型別。
    /// 屬性順序對應 BuildBVs / BuildCVs 傳入 set 的順序。
    /// </summary>
    public class VariableX_Template : VariableBase
    {
        public string   Set1 { get; set; } = string.Empty;
        public double   Set2 { get; set; }
        public int      Set3 { get; set; }
        public DateTime Set4 { get; set; }
    }
}
