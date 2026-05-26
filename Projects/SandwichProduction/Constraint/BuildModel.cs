using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandwichProduction.Objective;
using SandwichProduction.Set;

namespace SandwichProduction.Constraint
{
    public class BuildModel
    {
        private readonly SandwichDataload dataload;
        private readonly OptEngine optEngine;

        public BuildModel(SandwichDataload dataload, OptEngine optEngine)
        {
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            // ── 目標函數 ──────────────────────────────────────────────────
            new ObjectiveFunction(dataload, optEngine).Build();

            // ── 限制式 ────────────────────────────────────────────────────
            var c1 = new Constraint_IngredientCapacity(dataload, optEngine);
            c1.Build();

            Logging.Info($"[BuildModel] 限制式總數：{c1.ConstraintCount}");
        }
    }
}
