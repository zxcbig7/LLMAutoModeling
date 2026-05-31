using OptimFoundation.Core;

namespace FactorioOptimization.Parameter
{
    public class Parameter_RecipeInput : ParameterBase
    {
        public string MachineName  { get; set; } = string.Empty;
        public string ResourceName { get; set; } = string.Empty;
        public double QTY     { get; set; }
    }
}
