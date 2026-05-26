using OptimFoundation.Core;

namespace SandBox.VariableClass
{
    /// <summary>
    /// Continuous 變數範本：每個 Item 的寬鬆量（>= 0 的連續值）。
    /// 用 BuildCVs&lt;T&gt;() 建立。
    /// </summary>
    public class VariableX_Slack : VariableBase
    {
        public string Item { get; set; } = string.Empty;
    }
}
