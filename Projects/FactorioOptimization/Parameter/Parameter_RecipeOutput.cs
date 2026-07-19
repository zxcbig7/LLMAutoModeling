using OptimFoundation.Modeling;
using FactorioOptimization.Set;

namespace FactorioOptimization.Parameter
{
    /// <summary>配方輸出參數（每單位加工時間產出量）。MachineName / ResourceName 為 index-set。</summary>
    [OptParam]
    [OptDim<Set_Machine>("MachineName")]
    [OptDim<Set_Commodity>("ResourceName")]
    public partial class Parameter_RecipeOutput { }
}
