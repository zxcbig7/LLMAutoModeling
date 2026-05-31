using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C2] 豬肉供應：Σ_{i} PorkPerUnit[i] · x[i]  ≤  PorkCapacity (800 lbs)
    /// </summary>
    public class Constraint_Pork : ConstraintBase
    {
        private OptEngine           optEngine;
        private WeeniesBunsDataload dataload;
        public new int ConstraintCount = 0;

        public Constraint_Pork(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_ProductSpec.ForEach(spec =>
                optEngine.AddLHS(spec.PorkPerUnit, new VariableX_Production { ProductType = spec.ProductType }));

            optEngine.AddRHS(dataload.PorkCapacity);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
