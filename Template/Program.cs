using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
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
                Logging.SetLogFileName("Template_exp");

            // ── 材料 ───────────────────────────────────────────────
            var data = OptData.Load(() => new Dataload());
            TemplateSolution.ValidateData(data); // 資料驗收：全格矩陣 / 跨表關聯；MUST 緊接 Load 之後、建模之前

            double shortagePenalty = data.parameter_ShortagePenalty.Single().QTY;
            double bigMProduce = data.parameter_BigMProduce.Single().QTY;

            var projectConfig = new ProjectConfig
            {
                ProjectName = "Template",
                EnableSolverLog = true,
                ExportLP = true,
                ExportSol = true,
                DataId = "Template",
                UserId = "SYSTEM",
            };
            // 唯一 production baseline/champion；experiment clone 它，prod 直接使用它。
            // Seed 與 ParallelMode 無條件明設——這兩顆是交棒給 Phase 3 的環境契約。
            var productionBaseline = new CplexConfig
            {
                MipGap = 0.01,
                TimeLimit = 300,
                Threads = 8,
                ParallelMode = 1,
                Seed = 11,
            };

            // ── 模型 ───────────────────────────────────────────────
            // 每種變數、目標式、每條限制式各占一個 fluent call；AddObjective MUST 在 AddConstraints 之前。
            // Produce / Use 的 domain 是稀疏的 Set_AllowedSlot，不是 Item × Date 的完整笛卡兒積。
            var model = new OptModel("Canonical")
                .AddVariables(engine => engine.BuildVars<VariableI_Produce>(data.set_AllowedSlot))
                .AddVariables(engine => engine.BuildVars<VariableB_Use>(data.set_AllowedSlot))
                .AddVariables(engine => engine.BuildVars<VariableC_Shortage>(data.set_Item))
                .AddObjective(engine => new ObjectiveFunction(data.set_Item, shortagePenalty).Build(engine))
                .AddConstraints(engine => new Constraint_DemandCoverage(
                    data.set_Item, data.set_AllowedSlot, data.parameter_Demand).Build(engine))
                .AddConstraints(engine => new Constraint_DailyCapacity(
                    data.set_Date, data.set_AllowedSlot, data.parameter_DailyCapacity).Build(engine))
                .AddConstraints(engine => new Constraint_ProduceOnlyWhenUsed(
                    data.set_AllowedSlot, bigMProduce).Build(engine))
                .AddConstraints(engine => new Constraint_MinActiveDays(
                    data.set_Item, data.set_AllowedSlot, data.parameter_MinActiveDays).Build(engine))
                .AddConstraints(engine => new Constraint_ShortageCap(
                    data.set_Item, data.parameter_MaxShortage).Build(engine));

            // 3. 實驗段落（固定保留；只在 exp 模式執行）
            // 模式 2：exp —— 交棒給 Phase 3 的 R0 形狀，NEVER 在這裡混掃多顆旋鈕
            if (isExperiment)
            {
                // R0 — Template-tuning-r0
                var exp = new OptExperiment("Template-tuning-r0", "R0 校準：baseline x 5 seeds");
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
                .OnSolved(engine => TemplateSolution.ReadAndValidate(engine, data).Print());

            bool solved = project.Execute();
            return solved ? 0 : 1;
        }
    }
}
