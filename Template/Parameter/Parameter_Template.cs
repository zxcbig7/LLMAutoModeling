using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Parameter
{
    /// <summary>
    /// 參數類別空白範本：展示所有支援的 Set 元素型別（string / double / int / DateTime）+ QTY。
    /// body 由 AutoSetsGenerator 生成；Parameter 一律含 QTY，無值組合請改用多維 Set。
    /// Set2/Set3 引用的 Set_D/Set_E 僅供本範本示範型別，非實際模型維度。
    /// </summary>
    [OptParam]
    [OptDim<string>("Set1")]
    [OptDim<double>("Set2")]
    [OptDim<int>("Set3")]
    [OptDim<DateTime>("Set4")]
    public partial class Parameter_Template { }
}
