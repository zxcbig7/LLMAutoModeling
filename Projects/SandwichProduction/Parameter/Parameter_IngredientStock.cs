using OptimFoundation.Modeling;
using SandwichProduction.Set;

namespace SandwichProduction.Parameter
{
    /// <summary>每種食材的庫存總量。Ingredient 為 index-set（Set_Ingredient），
    /// Stock 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Ingredient>("Ingredient")]
    public partial class Parameter_IngredientStock
    {
        public double Stock { get; set; }
    }
}
