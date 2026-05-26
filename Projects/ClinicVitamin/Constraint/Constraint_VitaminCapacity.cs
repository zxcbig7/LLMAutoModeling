using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;
using ClinicVitamin.Variable;

namespace ClinicVitamin.Constraint
{
    /// <summary>
    /// [C1] 維生素原料上限
    ///   ∀ v:  Σ_p  r[v][p] · x[p]  ≤  A[v]
    /// </summary>
    public class Constraint_VitaminCapacity : ConstraintBase
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;
        public new int ConstraintCount = 0;

        public Constraint_VitaminCapacity(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            dataload.parameter_VitaminStock.ForEach(stock =>
            {
                dataload.parameter_VitaminReq
                    .Where(r => r.Vitamin == stock.Vitamin)
                    .ToList()
                    .ForEach(req =>
                        optEngine.AddLHS(req.Required,
                            new VariableX_Production { ProductType = req.ProductType }));

                optEngine.AddRHS(stock.Stock);
                optEngine.CreateLessEqual($"{ConstraintName}@{stock.Vitamin}");
                ConstraintCount++;
            });

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
