using System;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;
using HospitalRostering_Generator.Constraint;

namespace HospitalRostering_Generator
{
    /// <summary>
    /// 參數掃描實驗（dotnet run -- experiment）。與 solve 模式共用 VariableCreate / BuildModel，
    /// 每組 CplexConfig 一次 Trial.Capture，累積成 Experiment 輸出 Experiments/&lt;name&gt;.csv + .json。
    /// 註：tuning 不能用 OptModel（一次性、自建/Dispose engine），故手接 OptEngine —— 與 Manual 版形狀一致。
    /// </summary>
    public static class ExperimentRunner
    {
        public static void Run()
        {
            Logging.SetLogFileName("HospitalRostering_Generator_Experiment");

            var exp = new Experiment(
                "hospital-generator-tuning",
                "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響");

            // 與 Manual 版**完全相同**的 variants（公平對比）
            var variants = new (string label, Action<CplexConfig> tune)[]
            {
                ("baseline",          _ => { }),
                ("emphasis=optimal",  c => c.Emphasis    = 2),
                ("varsel=strong",     c => c.varSel      = 3),
                ("nodesel=bestbound", c => c.nodeSelect  = 1),
                ("gap=0.01",          c => c.epGap       = 0.01),
                ("threads=4",         c => c.workThreads = 4),
                ("seed=20260622",     c => c.Seed        = 20260622),
            };

            int i = 0;
            foreach (var (label, tune) in variants)
            {
                i++;
                var config = new CplexConfig
                {
                    epGap       = 0.03,
                    timeLimit   = 100,
                    workThreads = 10,
                    enableLog   = false,
                    exportLP    = false,
                    exportMPS   = false,
                    exportSol   = false,
                };
                tune(config);

                var data = new Dataload();                       // 每 Trial fresh，避免狀態污染
                using var engine = new OptEngine(config);
                engine.Build();
                new VariableCreate(data, engine).Build();        // ← 與 solve 模式共用同一 build-step
                new BuildModel(data, engine).Build();            // ← 與 solve 模式共用

                Logging.Info($"[Experiment] ({i}/{variants.Length}) 求解中：{label} …");
                exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
            }

            exp.Save();   // → Experiments/hospital-generator-tuning.csv + .json
        }
    }
}
