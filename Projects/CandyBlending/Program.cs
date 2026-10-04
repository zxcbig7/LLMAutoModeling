using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    // CLI：import-data 是獨立的資料前處理；其餘為「模型來源 × 執行方式」兩軸自由組合
    //   模型來源：預設讀 FolderDir.Input 的 CSV 建構 canonical；read-model <file> 改讀既有模型檔（.lp / .mps / .sav），不讀 CSV
    //   執行方式：預設正式求解；exp 做實驗
    //   例：dotnet run、dotnet run -- exp、dotnet run -- read-model <file>、dotnet run -- read-model <file> exp、dotnet run -- import-data <raw>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // 1. import-data 段落（固定保留；只在 import-data 模式執行）
            // 攤平或生成，產出標準 CSV 到 FolderDir.Input（CSV 還不存在時才需要）
            if (args.Length >= 2 && args[0] == "import-data")
            {
                string rawFile = args[1];
                OptData.Load(() => new Dataload(rawFile)).Export();
                return 0;
            }

            bool isExperiment = args.Any(arg => string.Equals(arg, "exp", StringComparison.OrdinalIgnoreCase));
            int readModelAt = Array.IndexOf(args, "read-model");
            string? modelFile = readModelAt >= 0 && readModelAt + 1 < args.Length ? args[readModelAt + 1] : null;
            if (readModelAt >= 0 && modelFile == null)
            {
                Console.Error.WriteLine("read-model 需要模型檔路徑，例：dotnet run -- read-model <file> [exp]");
                return 2;
            }

            // 專案：名稱、log、FolderDir 資料夾、保留期都由它管；exp 與正式求解都從它出發
            using var project = new OptProject("CandyBlending");

            // 2. 設定段落（固定保留）
            var projectConfig = new ProjectConfig
            {
                EnableSolverLog = true,
                ExportLP = true,
                ExportSol = true,
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

            // 3. 模型來源段落（固定保留）
            // read-model：模型從檔案來（相對路徑以 FolderDir.Model 為基準），不讀 CSV；否則材料 → canonical。
            // 之後的 exp 與正式求解都只拿 model，不管它從哪來。
            Dataload? data = null;
            OptModel model;
            if (modelFile != null)
            {
                model = OptModel.ReadModel(modelFile);
            }
            else
            {
                data = OptData.Load(() => new Dataload());
                CandyBlendingSolution.ValidateData(data); // 資料驗收：全格矩陣 / 跨表關聯；MUST 緊接 Load 之後、建模之前
                model = BuildModel(data);
            }

            // 4. 環境段落（固定保留）
            // exp —— 掃 solver 設定，不做正式求解
            if (isExperiment)
            {
                // R0 — CandyBlending-tuning-r0（紀錄接在 Experiment/{專案名}-trial.csv 等累積檔，Experiment 欄 = 實驗名）
                // read-model 的實驗另外命名，累積檔裡跟 canonical 同一輪分得開
                string experimentName = modelFile == null ? "tuning-r0" : $"tuning-r0-{model.Name}";
                var exp = project.Experiment(experimentName, "R0 校準：baseline × 5 seeds");
                exp.AddModel(model);

                foreach (var seed in new[] { 11, 22, 33, 44, 55 })
                {
                    var config = productionBaseline.Clone();
                    config.Seed = seed;
                    exp.AddConfig($"r0-baseline-s{seed}", config);
                }

                var result = exp.Run();

                foreach (var trial in result.Trials)
                    Logging.Info($"[Experiment] {trial.Label} status={trial.Metrics.Status} solveTimeMs={trial.Metrics.SolveTimeMs:F0}");
                return 0;
            }

            // 預設：正式求解（read-model 沒有資料，不跑解驗證）
            project.LoadConfig(projectConfig);
            bool solved = project.Solve(model, productionBaseline,
                onSolved: data == null ? null : engine => CandyBlendingSolution.ReadAndValidate(engine, data).Print());
            return solved ? 0 : 1;
        }

        /// <summary>canonical 模型：材料 → 模型，只從資料建構；read-model 時不會呼叫。</summary>
        private static OptModel BuildModel(Dataload data)
        {
            return new OptModel("Canonical")
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
        }
    }
}
