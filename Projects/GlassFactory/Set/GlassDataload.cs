using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Parameter;
using GlassFactory.Variable;

namespace GlassFactory.Set
{
    public class GlassDataload
    {
        // ── Parameters ────────────────────────────────────────────────────
        public List<Parameter_GlassSpec> parameter_GlassSpec = new()
        {
            new() { GlassType = "Regular",  HeatingTime = 3, CoolingTime = 5, Profit = 8  },
            new() { GlassType = "Tempered", HeatingTime = 5, CoolingTime = 8, Profit = 10 }
        };

        // ── 機器產能 ──────────────────────────────────────────────────────
        public double HeatingCapacity = 300;
        public double CoolingCapacity = 300;

        // ── Sets（由 Parameters 衍生） ────────────────────────────────────
        public List<string> GlassTypes => parameter_GlassSpec.Select(s => s.GlassType).ToList();

        public void WriteToCSV(OptEngine engine)
        {
            Logging.Info("═══════════════════════════════════");
            Logging.Info("       最佳生產計畫");
            Logging.Info("═══════════════════════════════════");

            var solution = engine.GetSetVarValues<VariableX_Production>();
            foreach (var kvp in solution)
            {
                string label  = kvp.Key.Contains('@') ? kvp.Key.Split('@').Last() : kvp.Key;
                double profit = kvp.Value * (parameter_GlassSpec.FirstOrDefault(s => s.GlassType == label)?.Profit ?? 0);
                Logging.Info($"  {label,-10}: {kvp.Value,6:F1} 片   利潤 = ${profit:F2}");
            }

            Logging.Info("───────────────────────────────────");
            Logging.Info($"  最大總利潤 = ${engine.GetObjectiveValue():F2}");
            Logging.Info("═══════════════════════════════════");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableX_Production>(engine, "GlassFactory", "USER");
            Logging.Info("Results saved: Solution/VariableX_Production.csv");
        }
    }
}
