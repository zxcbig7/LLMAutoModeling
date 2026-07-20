using OptimFoundation.Modeling;

namespace FactorioOptimization.Set
{
    /// <summary>機台種類（string set）。Parameter_Machine / Parameter_RecipeInput / Parameter_RecipeOutput 的
    /// MachineName index-set，供框架資料驗證用（成員與 parameter_Machine 一致，見 Dataload ctor）。</summary>
    [OptSet<string>]
    public partial class Set_Machine { }
}
