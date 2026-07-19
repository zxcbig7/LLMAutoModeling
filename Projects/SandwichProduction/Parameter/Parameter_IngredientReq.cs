using OptimFoundation.Modeling;
using SandwichProduction.Set;

namespace SandwichProduction.Parameter
{
    /// <summary>製作各三明治所需的食材用量。Ingredient / SandwichType 為 index-set。
    /// Required 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Ingredient>("Ingredient")]
    [OptDim<Set_SandwichType>("SandwichType")]
    public partial class Parameter_IngredientReq
    {
        public double Required { get; set; }
    }
}
