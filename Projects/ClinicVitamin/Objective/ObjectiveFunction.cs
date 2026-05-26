using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;
using ClinicVitamin.Variable;

namespace ClinicVitamin.Objective
{
    /// <summary>
    /// 目標函數：max Σ_p s[p]·x[p]
    /// </summary>
    public class ObjectiveFunction
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;

        public ObjectiveFunction(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            dataload.parameter_ProductSpec.ForEach(spec =>
                optEngine.AddLHS(spec.PeopleSupply,
                    new VariableX_Production { ProductType = spec.ProductType }));

            optEngine.CreateMaximize();
        }
    }
}
