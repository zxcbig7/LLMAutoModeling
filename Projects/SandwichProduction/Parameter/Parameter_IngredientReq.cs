using OptimFoundation.Core;

namespace SandwichProduction.Parameter
{
    /// <summary>
    /// 製作各三明治所需的食材用量。
    /// </summary>
    public class Parameter_IngredientReq : ParameterBase
    {
        public string Ingredient   { get; set; } = string.Empty;
        public string SandwichType { get; set; } = string.Empty;
        public double Required     { get; set; }
    }
}
