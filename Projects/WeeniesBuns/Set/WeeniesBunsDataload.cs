using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Parameter;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Set
{
    public class WeeniesBunsDataload
    {
        // ── Resource capacities ───────────────────────────────────────────
        public double FlourCapacity  = 200;     // lbs / week
        public double PorkCapacity   = 800;     // lbs / week
        public double LaborCapacity  = 12000;   // minutes / week

        // ── Parameters ────────────────────────────────────────────────────
        public List<Parameter_ProductSpec> parameter_ProductSpec = new()
        {
            new() { ProductType = "Frankfurter", FlourPerUnit = 0,    PorkPerUnit = 0.25, LaborPerUnit = 3, Profit = 0.88 },
            new() { ProductType = "Bun",         FlourPerUnit = 0.1,  PorkPerUnit = 0,    LaborPerUnit = 2, Profit = 0.33 }
        };

        // ── Sets（由 Parameters 衍生） ────────────────────────────────────
        public List<string> ProductTypes => parameter_ProductSpec.Select(p => p.ProductType).ToList();

        public void WriteToCSV(OptEngine engine)
        {
            Logging.Info("═══════════════════════════════════════════");
            Logging.Info("       最佳生產計畫（Weenies and Buns）");
            Logging.Info("═══════════════════════════════════════════");

            var solution = engine.GetSetVarValues<VariableX_Production>();
            foreach (var kvp in solution)
            {
                string label  = kvp.Key.Contains('@') ? kvp.Key.Split('@').Last() : kvp.Key;
                var    spec   = parameter_ProductSpec.FirstOrDefault(p => p.ProductType == label);
                double profit = kvp.Value * (spec?.Profit ?? 0);
                Logging.Info($"  {label,-15}: {kvp.Value,8:F1} 個   利潤 = ${profit:F2}");
            }

            Logging.Info("───────────────────────────────────────────");
            Logging.Info($"  最大總利潤 = ${engine.GetObjectiveValue():F2}");
            Logging.Info("═══════════════════════════════════════════");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableX_Production>(engine, "WeeniesBuns", "USER");
            Logging.Info("Results saved: Solution/VariableX_Production.csv");
        }
    }
}
