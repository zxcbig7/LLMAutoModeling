using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Parameter;
using ClinicVitamin.Variable;

namespace ClinicVitamin.Set
{
    public class ClinicDataload
    {
        // ── Parameters ────────────────────────────────────────────────────
        public List<Parameter_ProductSpec> parameter_ProductSpec = new()
        {
            new() { ProductType = "Shots", PeopleSupply = 10 },
            new() { ProductType = "Pills", PeopleSupply = 7  }
        };

        public List<Parameter_VitaminReq> parameter_VitaminReq = new()
        {
            new() { Vitamin = "C", ProductType = "Shots", Required = 30 },
            new() { Vitamin = "C", ProductType = "Pills", Required = 50 },
            new() { Vitamin = "D", ProductType = "Shots", Required = 40 },
            new() { Vitamin = "D", ProductType = "Pills", Required = 30 }
        };

        public List<Parameter_VitaminStock> parameter_VitaminStock = new()
        {
            new() { Vitamin = "C", Stock = 1200 },
            new() { Vitamin = "D", Stock = 1500 }
        };

        // ── 其他限制參數 ──────────────────────────────────────────────────
        public double MaxShots = 10;

        // ── Sets（由 Parameters 衍生） ────────────────────────────────────
        public List<string> Products => parameter_ProductSpec.Select(s => s.ProductType).ToList();
        public List<string> Vitamins  => parameter_VitaminStock.Select(s => s.Vitamin).ToList();

        public void WriteToCSV(OptEngine engine)
        {
            Logging.Info("═══════════════════════════════════");
            Logging.Info("       最佳維生素生產計畫");
            Logging.Info("═══════════════════════════════════");

            int totalPeople = 0;
            var solution = engine.GetSetVarValues<VariableX_Production>();
            foreach (var kvp in solution)
            {
                string label   = kvp.Key.Contains('@') ? kvp.Key.Split('@').Last() : kvp.Key;
                double supply  = parameter_ProductSpec.FirstOrDefault(s => s.ProductType == label)?.PeopleSupply ?? 0;
                int    people  = (int)(kvp.Value * supply);
                totalPeople   += people;
                Logging.Info($"  {label,-6}: {kvp.Value,6:F1} 批   供應 {people} 人");
            }

            Logging.Info("───────────────────────────────────");
            Logging.Info($"  最大供應人數 = {engine.GetObjectiveValue():F0} 人");
            Logging.Info("═══════════════════════════════════");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableX_Production>(engine, "ClinicVitamin", "USER");
            Logging.Info("Results saved: Solution/VariableX_Production.csv");
        }
    }
}
