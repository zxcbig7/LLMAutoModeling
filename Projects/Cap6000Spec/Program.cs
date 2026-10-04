using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Cap6000Spec
{
    // CLI：「模型來源 × 執行方式」兩軸自由組合；import-data 不適用（本專案沒有 CSV 資料）
    //   模型來源：read-model <file> 讀既有模型檔（相對路徑以 FolderDir.Model 為基準，csproj 會把 Data/cap6000.mps.gz 複製過去）；本專案沒有 canonical 模型，未指定就結束
    //   執行方式：預設正式求解；exp 做實驗
    //   例：dotnet run -- read-model cap6000.mps.gz、dotnet run -- read-model cap6000.mps.gz exp
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // 1. import-data 段落（固定保留）
            // 本專案的模型直接來自 MIPLIB 模型檔，沒有原始資料要攤平成 CSV，這段不做任何事。

            bool isExperiment = args.Any(arg => string.Equals(arg, "exp", StringComparison.OrdinalIgnoreCase));
            int readModelAt = Array.IndexOf(args, "read-model");
            string? modelFile = readModelAt >= 0 && readModelAt + 1 < args.Length ? args[readModelAt + 1] : null;
            if (modelFile == null)
            {
                Console.Error.WriteLine("本專案只有 read-model 一種模型來源，例：dotnet run -- read-model cap6000.mps.gz [exp]");
                return 2;
            }

            // 專案：名稱、log、FolderDir 資料夾、保留期都由它管；exp 與正式求解都從它出發
            using var project = new OptProject("Cap6000Spec");

            // 2. 設定段落（固定保留）
            var projectConfig = new ProjectConfig
            {
                EnableSolverLog = true,
                ExportLP = true,
                ExportSol = true,
            };
            // 唯一 production baseline/champion；experiment clone 它，prod 直接使用它。
            // 停止契約 MipGap / TimeLimit 由使用者 2026-10-03 定案；Threads 由 S1 sizing 定版。
            // baseline provenance:
            //   來源 experiment: initial
            //   champion Trial : initial
            //   promotion 日期 : -
            //   diff : -
            var productionBaseline = new CplexConfig
            {
                MipGap = 1e-4,
                TimeLimit = 300,
                Threads = 8,
                ParallelMode = 1,
                Seed = 11,
            };

            // 3. 模型來源段落（固定保留）
            // read-model：模型從檔案來，不讀 CSV；之後的 exp 與正式求解都只拿 model。
            OptModel model = OptModel.ReadModel(modelFile);

            // 4. 環境段落（固定保留）
            if (isExperiment)
            {
                // 當次只執行 currentExperiment 指定的那一個區塊；已執行的區塊保留供重跑與稽核（tuning guide §3.3.1）
                string currentExperiment = "tuning-r3";
                OptExperiment? exp = null;

                if (currentExperiment == "sizing")
                {
                    // S1 — Cap6000Spec-sizing
                    // tuning guide §2.3：ParallelMode = 1 固定、只掃 Threads；候選 = 實體核心 12 / 11 / 10，對照現行值 8；3 個 seed，順序輪替
                    // 已執行（RunId 20261003-093001，維持 Threads = 8），設定已 materialize 成字面值。
                    exp = project.Experiment("sizing", "S1 sizing：Threads 12 / 11 / 10 vs 現行 8，3 seeds");
                    exp.AddModel(model);
                    exp.AddConfig("s1-warmup-exclude", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });

                    int[] sizingSeeds = { 11, 22, 33 };
                    for (int k = 0; k < sizingSeeds.Length; k++)
                    {
                        var baseline = new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1 };
                        baseline.Seed = sizingSeeds[k];
                        var threads12 = baseline.Clone();
                        threads12.Threads = 12;
                        var threads11 = baseline.Clone();
                        threads11.Threads = 11;
                        var threads10 = baseline.Clone();
                        threads10.Threads = 10;

                        var cells = new (string Name, CplexConfig Config)[]
                        {
                            ("threads8-baseline", baseline),
                            ("threads12", threads12),
                            ("threads11", threads11),
                            ("threads10", threads10),
                        };
                        for (int i = 0; i < cells.Length; i++)
                        {
                            var (name, config) = cells[(i + k) % cells.Length];
                            exp.AddConfig($"s1-{name}-s{sizingSeeds[k]}", config);
                        }
                    }
                }
                else if (currentExperiment == "tuning-r0")
                {
                    // R0 — Cap6000Spec-tuning-r0（紀錄接在 Experiment/{專案名}-trial.csv 等累積檔，Experiment 欄 = 實驗名）
                    // 已執行（RunId 20261003-093037），設定已 materialize 成字面值。
                    exp = project.Experiment("tuning-r0", "R0 校準：baseline × 5 seeds");
                    exp.AddModel(model);

                    foreach (var seed in new[] { 11, 22, 33, 44, 55 })
                    {
                        var config = new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1 };
                        config.Seed = seed;
                        exp.AddConfig($"r0-baseline-s{seed}", config);
                    }
                }
                else if (currentExperiment == "probe-mipgap0")
                {
                    // R0 產出 C — Cap6000Spec-probe-mipgap0
                    // tuning guide §3.0：MipGap = 0 探針，取真最佳值與現行契約的品質損失；不是 variant，不進排名
                    // 已執行（RunId 20261003-093059），設定已 materialize 成字面值。
                    exp = project.Experiment("probe-mipgap0", "R0 產出 C：MipGap = 0 契約健檢探針");
                    exp.AddModel(model);
                    exp.AddConfig("probe-mipgap0-s11", new CplexConfig { MipGap = 0, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });
                }
                else if (currentExperiment == "tuning-r1")
                {
                    // R1 — Cap6000Spec-tuning-r1
                    // 剖面 Primal-search，§2.2 該列第一顆：Emphasis = 1
                    // 已執行（RunId 20261003-093354，hold-out 不通過 → retain），設定已 materialize 成字面值。
                    exp = project.Experiment("tuning-r1", "R1：Emphasis=1（Primal-search）vs baseline × 5 seeds");
                    exp.AddModel(model);
                    exp.AddConfig("r1-warmup-exclude", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });

                    int[] seeds = { 11, 22, 33, 44, 55 };
                    for (int k = 0; k < seeds.Length; k++)
                    {
                        var baseline = new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1 };
                        baseline.Seed = seeds[k];
                        var feasibility = baseline.Clone();
                        feasibility.Emphasis = 1;

                        // 順序輪替（§3.4）：第 k 個 seed 從第 k 個設定開始跑
                        var cells = new (string Name, CplexConfig Config)[] { ("baseline", baseline), ("Emphasis=1", feasibility) };
                        for (int i = 0; i < cells.Length; i++)
                        {
                            var (name, config) = cells[(i + k) % cells.Length];
                            exp.AddConfig($"r1-{name}-s{seeds[k]}", config);
                        }
                    }
                }
                else if (currentExperiment == "tuning-r1-holdout")
                {
                    // R1 hold-out — Cap6000Spec-tuning-r1-holdout
                    // tuning guide §4.6：champion r1-Emphasis=1 與 baseline 用未參與調參的 seed 重跑；關軌跡，跟正式求解同一種跑法
                    // 已執行（RunId 20261003-093515，Losses 1 → 不通過），設定已 materialize 成字面值。
                    exp = project.Experiment("tuning-r1-holdout", "R1 hold-out：Emphasis=1 vs baseline × holdout seeds 66 / 77 / 88");
                    exp.AddModel(model);
                    exp.CaptureTrajectory(false);
                    exp.AddConfig("r1-warmup-exclude", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });

                    int[] holdoutSeeds = { 66, 77, 88 };
                    for (int k = 0; k < holdoutSeeds.Length; k++)
                    {
                        var baseline = new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1 };
                        baseline.Seed = holdoutSeeds[k];
                        var champion = baseline.Clone();
                        champion.Emphasis = 1;

                        var cells = new (string Name, CplexConfig Config)[] { ("baseline", baseline), ("Emphasis=1", champion) };
                        for (int i = 0; i < cells.Length; i++)
                        {
                            var (name, config) = cells[(i + k) % cells.Length];
                            exp.AddConfig($"r1-{name}-s{holdoutSeeds[k]}", config);
                        }
                    }
                }
                else if (currentExperiment == "tuning-r2")
                {
                    // R2 — Cap6000Spec-tuning-r2
                    // 剖面 Primal-search，emphasis 群組已否證 → §2.2 該列下一顆：RinsHeuristicFrequency（正值會在 node 0 = root 呼叫 RINS）
                    // 已執行（RunId 20261003-093617，retain），設定已 materialize 成字面值。
                    exp = project.Experiment("tuning-r2", "R2：RinsHeuristicFrequency=10（Primal-search）vs baseline × 5 seeds");
                    exp.AddModel(model);
                    exp.AddConfig("r2-warmup-exclude", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });

                    int[] seeds = { 11, 22, 33, 44, 55 };
                    for (int k = 0; k < seeds.Length; k++)
                    {
                        var baseline = new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1 };
                        baseline.Seed = seeds[k];
                        var rins = baseline.Clone();
                        rins.RinsHeuristicFrequency = 10;

                        // 順序輪替（§3.4）：第 k 個 seed 從第 k 個設定開始跑
                        var cells = new (string Name, CplexConfig Config)[] { ("baseline", baseline), ("RinsHeuristicFrequency=10", rins) };
                        for (int i = 0; i < cells.Length; i++)
                        {
                            var (name, config) = cells[(i + k) % cells.Length];
                            exp.AddConfig($"r2-{name}-s{seeds[k]}", config);
                        }
                    }
                }

                if (exp == null)
                {
                    Console.Error.WriteLine($"exp 分支沒有 '{currentExperiment}' 區塊");
                    return 2;
                }

                var result = exp.Run();

                foreach (var trial in result.Trials)
                    Logging.Info($"[Experiment] {trial.Label} status={trial.Metrics.Status} solveTimeMs={trial.Metrics.SolveTimeMs:F0}");
                return 0;
            }

            // 預設：正式求解（read-model 沒有資料，不跑解驗證）
            project.LoadConfig(projectConfig);
            bool solved = project.Solve(model, productionBaseline);
            return solved ? 0 : 1;
        }
    }
}
