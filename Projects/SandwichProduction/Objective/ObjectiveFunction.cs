using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandwichProduction.Set;
using SandwichProduction.Variable;

namespace SandwichProduction.Objective
{
    /// <summary>
    /// 目標函數：max Σ_i Profit[i]·x[i]
    /// </summary>
    public class ObjectiveFunction
    {
        private readonly SandwichDataload dataload;
        private readonly OptEngine optEngine;

        public ObjectiveFunction(SandwichDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            dataload.parameter_SandwichSpec.ForEach(spec =>
                optEngine.AddLHS(spec.Profit, new VariableX_Sandwich { SandwichType = spec.SandwichType }));

            optEngine.CreateMaximize();
        }
    }
}
