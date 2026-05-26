using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;

namespace ClinicVitamin.Variable
{
    public class VariableCreate
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;

        public VariableCreate(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            optEngine.BuildCVs<VariableX_Production>(dataload.Products);
            Logging.Info($"Variables created: {optEngine.varCount}");
        }
    }
}
