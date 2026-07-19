using OptimFoundation.Modeling;
using FactorioOptimization.Set;

namespace FactorioOptimization.Parameter
{
    /// <summary>機台加工耗時參數。MachineName 為 index-set（Set_Machine），CraftingTime 非 QTY，
    /// 故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Machine>("MachineName")]
    public partial class Parameter_Machine
    {
        public double CraftingTime { get; set; } = 1.0;
    }
}
