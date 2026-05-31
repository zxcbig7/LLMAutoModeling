using OptimFoundation.Core;

namespace FactorioOptimization.Parameter
{
    public class Parameter_Machine : ParameterBase
    {
        public string MachineName  { get; set; } = string.Empty;
        public double CraftingTime { get; set; } = 1.0;
    }
}
