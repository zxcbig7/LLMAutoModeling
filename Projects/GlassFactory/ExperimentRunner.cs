using System;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Variable;
using GlassFactory.Constraint;

namespace GlassFactory
{
    /// <summary>
    /// 參數掃描實驗（dotnet run -- experiment）。與 solve 模式共用 VariableCreate / BuildModel，
    /// 每組 CplexConfig 一次 Trial.Capture，累積成 Experiment 輸出 Experiments/&lt;name&gt;.csv + .json。
    /// </summary>
    public static class ExperimentRunner
    {
        public static void Run()
        {
            Logging.SetLogFileName("GlassFactory_Experiment");

            var exp = new Experiment(
                "glassfactory-tuning",
                "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響");

            var variants = new (string label, Action<CplexConfig> tune)[]
            {
                ("baseline",          _ => { }),
                ("emphasis=optimal",  c => c.Emphasis    = 2),
                ("varsel=strong",     c => c.varSel      = 3),
                ("nodesel=bestbound", c => c.nodeSelect  = 1),
                ("gap=0.01",          c => c.epGap       = 0.01),
                ("threads=2",         c => c.workThreads = 2),
                ("seed=20260621",     c => c.Seed        = 20260621),
            };

            int i = 0;
            foreach (var (label, tune) in variants)
            {
                i++;
                var config = new CplexConfig
                {
                    epGap       = 0.0,
                    timeLimit   = 60,
                    workThreads = 4,
                    enableLog   = false,
                    exportLP    = false,
                    exportMPS   = false,
                    exportSol   = false,
                };
                tune(config);

                var dataload = new GlassDataload();
                using var engine = new OptEngine(config);
                engine.Build();
                new VariableCreate(dataload, engine).Build();
                new BuildModel(dataload, engine).Build();

                Logging.Info($"[Experiment] ({i}/{variants.Length}) 求解中：{label} …");

                var trial = Trial.Capture(engine, label, () => engine.Solve());
                exp.AddTrial(trial);

                var mtr = trial.Metrics;
                Logging.Info(
                    $"[Experiment] ({i}/{variants.Length}) {label}: Status={mtr.Status} " +
                    $"Obj={mtr.ObjectiveValue:G6} Gap={mtr.MipGap:P2} Time={mtr.WallTimeMs:F0}ms " +
                    $"Nodes={mtr.NodeCount} Vars={mtr.VarCount} Cons={mtr.ConstraintCount}");
            }

            exp.Save();
            Logging.Info($"[Experiment] 完成：{exp.Trials.Count} 個 Trial 已寫入 {FolderDir.Experiment.GetPath()}");
        }
    }
}
