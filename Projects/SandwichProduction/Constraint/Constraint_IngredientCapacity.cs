using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandwichProduction.Set;
using SandwichProduction.Variable;

namespace SandwichProduction.Constraint
{
    /// <summary>
    /// [C1] 食材庫存限制
    ///   ∀ j:  Σ_i  r[j][i] · x[i]  ≤  A[j]
    /// </summary>
    public class Constraint_IngredientCapacity : ConstraintBase
    {
        private const string Name = "C1_IngredientCapacity";

        private readonly SandwichDataload dataload;
        private readonly OptEngine optEngine;
        public new int ConstraintCount = 0;

        public Constraint_IngredientCapacity(SandwichDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            dataload.parameter_IngredientStock.ForEach(stock =>
            {
                dataload.parameter_IngredientReq
                    .Where(r => r.Ingredient == stock.Ingredient)
                    .ToList()
                    .ForEach(req =>
                        optEngine.AddLHS(req.Required, new VariableX_Sandwich { SandwichType = req.SandwichType }));

                optEngine.AddRHS(stock.Stock);
                optEngine.CreateLessEqual($"{Name}@{stock.Ingredient}");
                ConstraintCount++;
            });

            Logging.Info($"[{Name}] {ConstraintCount}");
        }
    }
}
