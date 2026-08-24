using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // 1. import 段落（固定保留；只在 import 模式執行）
            // 模式 1：import —— 攤平或生成，產出標準 CSV（CSV 還不存在時才需要）
            if (args.Length >= 2 && args[0] == "import")
            {
                string rawFile = args[1];
                OptData.Load(() => new Dataload(rawFile)).Export();
                return 0;
            }

            // 2. 模型段落（固定保留：材料 + canonical OptModel 組裝）
            // exp 的 log 檔名 MUST 在第一次寫入前設定，整次執行才收在同一包
            bool isExperiment = args.Any(arg => string.Equals(arg, "exp", StringComparison.OrdinalIgnoreCase));
            if (isExperiment)
                Logging.SetLogFileName("CandyBlending_exp");

            // ── 材料 ───────────────────────────────────────────────
            var data = OptData.Load(() => new Dataload());
            CandyBlendingSolution.ValidateData(data); // 資料驗收：全格矩陣 / 跨表關聯；MUST 緊接 Load 之後、建模之前

            var projectConfig = new ProjectConfig
            {
                ProjectName = "CandyBlending",
                EnableSolverLog = true,
                ExportLP = true,
                ExportSol = true,
                DataId = "CandyBlending",
                UserId = "SYSTEM",
            };
            // 唯一 production baseline/champion；experiment clone 它，prod 直接使用它。
            var productionBaseline = new CplexConfig
            {
                MipGap = 1e-6,
                TimeLimit = 300,
                Threads = 8,
                ParallelMode = 1,
                Seed = 11,
            };

            // ── 模型 ───────────────────────────────────────────────
            var model = new OptModel("Canonical")
                .AddVariables(engine => engine.BuildVars<VariableC_Blend>(data.set_RawMaterial, data.set_CandyBrand))
                .AddVariables(engine => engine.BuildVars<VariableC_Produce>(data.set_CandyBrand))
                .AddObjective(engine => new ObjectiveFunction(
                    data.set_RawMaterial,
                    data.set_CandyBrand,
                    data.parameter_SellingPrice,
                    data.parameter_ProcessingCost,
                    data.parameter_MaterialCost).Build(engine))
                .AddConstraints(engine => new Constraint_MaterialAvailability(
                    data.set_RawMaterial,
                    data.set_CandyBrand,
                    data.parameter_MonthlySupplyLimit).Build(engine))
                .AddConstraints(engine => new Constraint_BrandMassBalance(
                    data.set_RawMaterial,
                    data.set_CandyBrand).Build(engine))
                .AddConstraints(engine => new Constraint_MinContent(
                    data.parameter_MinContentRatio).Build(engine))
                .AddConstraints(engine => new Constraint_MaxContent(
                    data.parameter_MaxContentRatio).Build(engine));

            // 3. 實驗段落（固定保留；只在 exp 模式執行）
            // 模式 2：exp —— 掃 solver 設定，不做正式求解
            if (isExperiment)
            {
                // R0 — CandyBlending-tuning-r0
                var exp = new OptExperiment("CandyBlending-tuning-r0", "R0 校準：baseline × 5 seeds");
                exp.AddModel(model);

                foreach (var seed in new[] { 11, 22, 33, 44, 55 })
                {
                    var config = productionBaseline.Clone();
                    config.Seed = seed;
                    exp.AddConfig($"r0-baseline-s{seed}", config);
                }

                var result = exp.Run();

                foreach (var trial in result.Trials)
                    Logging.Info($"[Experiment] {trial.Label} status={trial.Metrics.Status} runTimeMs={trial.Metrics.RunTimeMs:F0}");
                return 0;
            }

            // 4. 正式跑段落（固定保留；無參數時執行）
            // 模式 3（預設）：正式求解
            using var project = new OptProject(model)
                .UseConfig(() => projectConfig)
                .UseConfig(() => productionBaseline)
                .OnSolved(engine => CandyBlendingSolution.ReadAndValidate(engine, data).Print());

            bool solved = project.Execute();
            return solved ? 0 : 1;
        }
    }
}
