using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Parameter
{
    /// <summary>
    /// 參數類別空白範本：展示所有支援的 Set 元素型別（string / double / int / DateTime）+ QTY。
    /// body 由 AutoSetsGenerator 生成；純 key 參數（無 QTY）於 OptParam attribute 設 HasValue = false。
    /// Set2/Set3 引用的 Set_D/Set_E 僅供本範本示範型別，非實際模型維度。
    /// </summary>
    [OptParam]
    [OptDim<Set_A>("Set1")]
    [OptDim<Set_D>("Set2")]
    [OptDim<Set_E>("Set3")]
    [OptDim<Set_C>("Set4")]
    public partial class Parameter_Template { }
}
