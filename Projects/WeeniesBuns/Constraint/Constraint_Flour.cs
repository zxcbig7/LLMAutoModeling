using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C1] 麵粉產能：Σ_{i} FlourPerUnit[i] · x[i]  ≤  FlourCapacity (200 lbs)
    /// </summary>
    public class Constraint_Flour : ConstraintBase
    {
        private OptEngine           optEngine;
        private WeeniesBunsDataload dataload;
        public new int ConstraintCount = 0;

        public Constraint_Flour(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_ProductSpec.ForEach(spec =>
                optEngine.AddLHS(spec.FlourPerUnit, new VariableX_Production { ProductType = spec.ProductType }));

            optEngine.AddRHS(dataload.FlourCapacity);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
