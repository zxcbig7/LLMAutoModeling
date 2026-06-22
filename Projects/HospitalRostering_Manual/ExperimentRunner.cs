using System;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;
using HospitalRostering_Manual.Constraint;

namespace HospitalRostering_Manual
{
    /// <summary>
    /// 參數掃描實驗（dotnet run -- experiment）。手接 OptEngine，與 solve 模式共用
    /// VariableCreate / BuildModel，每組 CplexConfig 一次 Trial.Capture。
    /// variants 與 HospitalRostering_Generator **完全相同**，兩架構結果可公平並排對比。
    /// </summary>
    public static class ExperimentRunner
    {
        public static void Run()
        {
            Logging.SetLogFileName("HospitalRostering_Manual_Experiment");

            var exp = new Experiment(
                "hospital-manual-tuning",
                "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響");

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

                var data = new Dataload();                       // 每 Trial fresh
                using var engine = new OptEngine(config);
                engine.Build();
                new VariableCreate(data, engine).Build();        // ← 與 solve 模式共用
                new BuildModel(data, engine).Build();

                Logging.Info($"[Experiment] ({i}/{variants.Length}) 求解中：{label} …");
                exp.AddTrial(Trial.Capture(engine, label, () => engine.Solve()));
            }

            exp.Save();   // → Experiments/hospital-manual-tuning.csv + .json
        }
    }
}
