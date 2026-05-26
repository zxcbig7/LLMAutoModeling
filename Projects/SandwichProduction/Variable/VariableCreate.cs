using OptimFoundation.Cplex;
using SandwichProduction.Set;

namespace SandwichProduction.Variable
{
    public class VariableCreate
    {
        private readonly SandwichDataload dataload;
        private readonly OptEngine optEngine;

        public VariableCreate(SandwichDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            // x[i]：連續變數，對 SandwichTypes 展開
            optEngine.BuildCVs<VariableX_Sandwich>(
                [dataload.SandwichTypes]
            );
        }
    }
}
