using OptimFoundation.Modeling;
using Template.Set;

namespace Template.Variable
{
    /// <summary>
    /// 變數空白範本：展示所有支援的 Set 元素型別（string / double / int / DateTime）。
    /// 維度宣告順序 = BuildBVs / BuildCVs 傳入 set 的順序；body 由 AutoSetsGenerator 生成。
    /// Set2/Set3 引用的 Set_D/Set_E 僅供本範本示範型別，非實際模型維度。
    /// </summary>
    [OptVar]
    [OptDim<string>("Set1")]
    [OptDim<double>("Set2")]
    [OptDim<int>("Set3")]
    [OptDim<DateTime>("Set4")]
    public partial class VariableX_Template { }
}
