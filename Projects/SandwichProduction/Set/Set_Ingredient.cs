using OptimFoundation.Modeling;

namespace SandwichProduction.Set
{
    /// <summary>原料種類（string set）。Parameter_IngredientReq / Parameter_IngredientStock 的
    /// Ingredient index-set，供框架資料驗證用。</summary>
    [OptSet<string>]
    public partial class Set_Ingredient { }
}
