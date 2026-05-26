using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandwichProduction.Parameter;
using SandwichProduction.Variable;

namespace SandwichProduction.Set
{
    public class SandwichDataload
    {
        // ── Parameters ────────────────────────────────────────────────────
        public List<Parameter_SandwichSpec> parameter_SandwichSpec = new()
        {
            new() { SandwichType = "Regular", Profit = 3 },
            new() { SandwichType = "Special",  Profit = 4 }
        };

        public List<Parameter_IngredientReq> parameter_IngredientReq = new()
        {
            new() { Ingredient = "Eggs",  SandwichType = "Regular", Required = 2 },
            new() { Ingredient = "Eggs",  SandwichType = "Special",  Required = 3 },
            new() { Ingredient = "Bacon", SandwichType = "Regular", Required = 3 },
            new() { Ingredient = "Bacon", SandwichType = "Special",  Required = 5 }
        };

        public List<Parameter_IngredientStock> parameter_IngredientStock = new()
        {
            new() { Ingredient = "Eggs",  Stock = 40 },
            new() { Ingredient = "Bacon", Stock = 70 }
        };

        // ── Sets（由 Parameters 衍生） ────────────────────────────────────
        public List<string> SandwichTypes => parameter_SandwichSpec.Select(s => s.SandwichType).ToList();
        public List<string> Ingredients   => parameter_IngredientStock.Select(s => s.Ingredient).ToList();

        public void WriteToCSV(OptEngine engine)
        {
            Logging.Info("═══════════════════════════════════");
            Logging.Info("       最佳三明治生產計畫");
            Logging.Info("═══════════════════════════════════");

            var solution = engine.GetSetVarValues<VariableX_Sandwich>();
            foreach (var kvp in solution)
            {
                string label  = kvp.Key.Contains('@') ? kvp.Key.Split('@').Last() : kvp.Key;
                double profit = kvp.Value * (parameter_SandwichSpec.FirstOrDefault(s => s.SandwichType == label)?.Profit ?? 0);
                Logging.Info($"  {label,-10}: {kvp.Value,6:F1} 個   利潤 = ${profit:F2}");
            }

            Logging.Info("───────────────────────────────────");
            Logging.Info($"  最大總利潤 = ${engine.GetObjectiveValue():F2}");
            Logging.Info("═══════════════════════════════════");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableX_Sandwich>(engine, "SandwichProduction", "USER");
            Logging.Info("Results saved: Solution/VariableX_Sandwich.csv");
        }
    }
}
