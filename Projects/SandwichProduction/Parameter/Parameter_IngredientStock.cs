using OptimFoundation.Core;

namespace SandwichProduction.Parameter
{
    /// <summary>
    /// 每種食材的庫存總量。
    /// </summary>
    public class Parameter_IngredientStock : ParameterBase
    {
        public string Ingredient { get; set; } = string.Empty;
        public double Stock      { get; set; }
    }
}
