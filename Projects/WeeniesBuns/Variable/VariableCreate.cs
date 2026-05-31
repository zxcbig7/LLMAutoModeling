using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;

namespace WeeniesBuns.Variable
{
    public class VariableCreate
    {
        private OptEngine          optEngine;
        private WeeniesBunsDataload dataload;

        public VariableCreate(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            optEngine.BuildCVs<VariableX_Production>(dataload.ProductTypes);
            Logging.Info($"Variables created: {optEngine.varCount}");
        }
    }
}
