using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C3] 人工產能：Σ_{i} LaborPerUnit[i] · x[i]  ≤  LaborCapacity (12000 min)
    /// </summary>
    public class Constraint_Labor : ConstraintBase
    {
        private OptEngine           optEngine;
        private WeeniesBunsDataload dataload;
        public new int ConstraintCount = 0;

        public Constraint_Labor(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_ProductSpec.ForEach(spec =>
                optEngine.AddLHS(spec.LaborPerUnit, new VariableX_Production { ProductType = spec.ProductType }));

            optEngine.AddRHS(dataload.LaborCapacity);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
