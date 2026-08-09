using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Parameter
{
    /// <summary>
    /// 三鍵參數：A × B × C → QTY。body（A/B/C/QTY + 無參數 ctor + params object[] ctor）
    /// 全由 AutoSetsGenerator 依 [OptParam] 生成；Parameter 一律含 QTY。
    /// </summary>
    [OptParam]
    [OptDim<string>("A")]
    [OptDim<string>("B")]
    [OptDim<DateTime>("C")]
    public partial class Parameter_ABC { }
}
