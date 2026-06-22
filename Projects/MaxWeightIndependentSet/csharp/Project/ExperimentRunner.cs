using System;
using System.Linq;

using OptimFoundation.Core;
using OptimFoundation.Cplex;

using MaxWeightIndependentSet.Model;

namespace MaxWeightIndependentSet.Project
{
    // 效能 tuning sweep（solver 層）：固定模型 + 數值，只掃 CplexConfig 旋鈕。
    // 用內建 Exp API（Trial.Capture / Experiment.Save）落地 Experiments/mwis-tuning.csv + .json。
    // 正確性不變量：所有 trial 的 ObjectiveValue 必須相同（否則代表動到模型/數值）。
    public static class ExperimentRunner
    {
        public static void Run()
        {
            Logging.SetLogFileName("MWIS-tuning");

            var exp = new Experiment(
                "mwis-tuning",
                "MWIS solver-層 tuning：掃 mipEmphasis / clique cuts / probe / 決定論，比較 WallTime/Nodes/Gap");

            var variants = new (string label, Action<CplexConfig> tune)[]
            {
                ("baseline-emphasis1",   c => { c.mipEmphasis = 1; }),
                ("emphasis0-balanced",   c => { c.mipEmphasis = 0; }),
                ("emphasis2-optimality", c => { c.mipEmphasis = 2; }),
                ("emphasis3-bestbound",  c => { c.mipEmphasis = 3; }),
                ("clique-aggressive",    c => { c.mipEmphasis = 0; c.cliqueCuts = 2; }),
                ("probe-aggressive",     c => { c.mipEmphasis = 0; c.probe = 3; }),
                ("clique+probe",         c => { c.mipEmphasis = 0; c.cliqueCuts = 2; c.probe = 2; }),
                ("deterministic-seed",   c => { c.mipEmphasis = 0; c.parallelMode = 1; c.randomSeed = 20260621; }),
            };

            Console.WriteLine($"{"variant",-22} {"Status",-9} {"Obj",10} {"Gap",8} {"Time(ms)",10} {"Nodes",10}");
            Console.WriteLine(new string('-', 74));

            foreach (var (label, tune) in variants)
            {
                var config = new CplexConfig
                {
                    timeLimit = 300,
                    epGap = 1e-9,      // 逼到證明最佳，讓 node/time 差異看得出來
                    workThreads = 8,
                    enableLog = false, // 掃描時關 solver log
                };
                tune(config);

                // 每個 Trial 全新 engine，避免跨 trial 狀態污染
                var data = new Dataload();
                using var engine = new OptEngine(config);
                engine.EnableTrajectory();
                engine.Build();
                new VariableCreate(data, engine).Build();
                new BuildConstraints(data, engine).Build();

                var trial = Trial.Capture(engine, label, () => engine.Solve());
                exp.AddTrial(trial);

                var m = trial.Metrics;
                Console.WriteLine($"{label,-22} {m.Status,-9} {m.ObjectiveValue,10:F1} {m.MipGap,8:P2} {m.WallTimeMs,10:F0} {m.NodeCount,10}");
            }

            exp.Save();   // → Experiments/mwis-tuning.csv + .json
            Console.WriteLine();
            Console.WriteLine($"Saved {exp.Trials.Count} trials → Experiments/mwis-tuning.csv + .json");
        }
    }
}
