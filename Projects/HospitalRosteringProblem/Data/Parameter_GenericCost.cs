using OptimFoundation.Core;

namespace SandBox.Data
{
    /// <summary>
    /// 複合鍵參數範本：多個 Set 作為 Key。
    /// 若需用 InitClassBySets 動態建立，加上建構子版本（見 Parameter_Template.cs）。
    /// </summary>
    public class Parameter_GenericCost : ParameterBase
    {
        public string Item    { get; set; } = string.Empty;
        public string Machine { get; set; } = string.Empty;
        public double QTY     { get; set; }
    }
}
