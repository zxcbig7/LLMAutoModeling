using OptimModeling;

namespace Template.Parameter
{
    /// <summary>
    /// 三鍵參數：A × B × C → QTY。body（A/B/C/QTY + 無參數 ctor + params object[] ctor）
    /// 全由 AutoSetsGenerator 依 [OptParam] 生成；純 key（無 QTY）參數設 HasValue = false。
    /// </summary>
    [OptParam("A", "B", "C:DateTime")]
    public partial class Parameter_ABC { }
}
