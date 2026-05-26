using OptimFoundation.Core;

namespace Template.Parameter
{
    /// <summary>
    /// 參數類別空白範本：展示所有支援的 Set 型別。
    /// 一般情況用 object initializer；需要動態建構時才加建構子（見下方）。
    /// </summary>
    public class Parameter_Template : ParameterBase
    {
        public string   Set1 { get; set; } = string.Empty;
        public double   Set2 { get; set; }
        public int      Set3 { get; set; }
        public DateTime Set4 { get; set; }
        public double   QTY  { get; set; }

        // 動態建構（需要時取消註解）：
        // public Parameter_Template(params object[] Sets)
        // {
        //     InitClassBySets(Sets);
        // }
    }
}
