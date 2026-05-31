using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Parameter;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Data
{
    public class FactorioOptimizationDataload
    {
        // ── 全域上限 ───────────────────────────────────────────────────────
        public double CrudeOilCap     = 958.3;
        public double FixedLubePlants = 20;

        // ── 機台參數 ───────────────────────────────────────────────────────
        public List<Parameter_Machine> parameter_Machine = new()
        {
            new() { MachineName = "Refinery",             CraftingTime = 1.0  },
            new() { MachineName = "ChemPlant_Lube",       CraftingTime = 1.0  },
            new() { MachineName = "ChemPlant_LightSolid", CraftingTime = 1.0  },
            new() { MachineName = "ChemPlant_GasSolid",   CraftingTime = 1.0  },
            new() { MachineName = "ChemPlant_HeavySolid", CraftingTime = 1.0  },
            new() { MachineName = "Assembler_Rocket",     CraftingTime = 15.0 },
        };

        // ── 配方輸入參數 ───────────────────────────────────────────────────
        public List<Parameter_RecipeInput> parameter_RecipeInput = new()
        {
            new() { MachineName = "Refinery",             ResourceName = "CrudeOil",  QTY = 20 },
            new() { MachineName = "Refinery",             ResourceName = "Water",     QTY = 10 },
            new() { MachineName = "ChemPlant_Lube",       ResourceName = "HeavyOil",  QTY = 10 },
            new() { MachineName = "ChemPlant_LightSolid", ResourceName = "LightOil",  QTY = 10 },
            new() { MachineName = "ChemPlant_GasSolid",   ResourceName = "Gas",       QTY = 20 },
            new() { MachineName = "ChemPlant_HeavySolid", ResourceName = "HeavyOil",  QTY = 20 },
            new() { MachineName = "Assembler_Rocket",     ResourceName = "SolidFuel", QTY = 10 },
            new() { MachineName = "Assembler_Rocket",     ResourceName = "LightOil",  QTY = 10 },
        };

        // ── 配方輸出參數 ───────────────────────────────────────────────────
        public List<Parameter_RecipeOutput> parameter_RecipeOutput = new()
        {
            new() { MachineName = "Refinery",             ResourceName = "HeavyOil",   QTY = 5  },
            new() { MachineName = "Refinery",             ResourceName = "LightOil",   QTY = 9  },
            new() { MachineName = "Refinery",             ResourceName = "Gas",        QTY = 11 },
            new() { MachineName = "ChemPlant_Lube",       ResourceName = "Lubricant",  QTY = 10 },
            new() { MachineName = "ChemPlant_LightSolid", ResourceName = "SolidFuel",  QTY = 1  },
            new() { MachineName = "ChemPlant_GasSolid",   ResourceName = "SolidFuel",  QTY = 1  },
            new() { MachineName = "ChemPlant_HeavySolid", ResourceName = "SolidFuel",  QTY = 1  },
            new() { MachineName = "Assembler_Rocket",     ResourceName = "RocketFuel", QTY = 1  },
        };

        // ── Sets ──────────────────────────────────────────────────────────
        public List<string> MachineTypes  => parameter_Machine.Select(m => m.MachineName).ToList();
        public List<string> ResourceTypes => new()
        {
            "HeavyOil", "LightOil", "Gas", "SolidFuel", "Lubricant", "RocketFuel"
        };

        // ── Helper Methods ────────────────────────────────────────────────
        private double CraftingTime(string machine)
            => parameter_Machine.First(m => m.MachineName == machine).CraftingTime;

        public double InputRate(string machine, string resource)
        {
            var p = parameter_RecipeInput.FirstOrDefault(r => r.MachineName == machine && r.ResourceName == resource);
            return p == null ? 0 : p.QTY / CraftingTime(machine);
        }

        public double OutputRate(string machine, string resource)
        {
            var p = parameter_RecipeOutput.FirstOrDefault(r => r.MachineName == machine && r.ResourceName == resource);
            return p == null ? 0 : p.QTY / CraftingTime(machine);
        }

        public List<string> ConsumerMachines(string resource)
            => parameter_RecipeInput.Where(r => r.ResourceName == resource).Select(r => r.MachineName).ToList();

        public List<string> ProducerMachines(string resource)
            => parameter_RecipeOutput.Where(r => r.ResourceName == resource).Select(r => r.MachineName).ToList();

        // ── 輸出結果 ───────────────────────────────────────────────────────
        public void WriteToCSV(OptEngine engine)
        {
            Logging.Info("═══════════════════════════════════════════");
            Logging.Info("       Factorio 生產最佳化結果");
            Logging.Info("═══════════════════════════════════════════");

            Logging.Info("── 機台台數 ──────────────────────────────");
            foreach (var kvp in engine.GetSetVarValues<VariableI_Machine>())
                Logging.Info($"  {kvp.Key.Split('@').Last(),-25}: {kvp.Value,8:F0}");

            Logging.Info("── 資源流量 ──────────────────────────────");
            foreach (var kvp in engine.GetSetVarValues<VariableX_Resource>())
                Logging.Info($"  {kvp.Key.Split('@').Last(),-25}: {kvp.Value,8:F4}");

            Logging.Info("── 原料消耗比例（實際 vs 目標 5:9:11）──");
            double h    = engine.GetVariableValue("VariableI_Machine@Refinery") > 0
                          ? engine.GetVariableValue("VariableX_Resource@HeavyOil") : 0;
            double l    = engine.GetVariableValue("VariableX_Resource@LightOil");
            double g    = engine.GetVariableValue("VariableX_Resource@Gas");
            double norm = h > 0 ? h / 5.0 : 1.0;
            Logging.Info($"  實際 = {h / norm:F4} : {l / norm:F4} : {g / norm:F4}");
            Logging.Info($"  目標 = 5.0000 : 9.0000 : 11.0000");

            Logging.Info("───────────────────────────────────────────");
            Logging.Info($"  火箭燃料產量 = {engine.GetObjectiveValue():F4}");
            Logging.Info("═══════════════════════════════════════════");

            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableI_Machine> (engine, "FactorioOptimization", "USER");
            CsvCtrl.SaveSolutionToCSV<VariableX_Resource>(engine, "FactorioOptimization", "USER");
        }
    }
}
