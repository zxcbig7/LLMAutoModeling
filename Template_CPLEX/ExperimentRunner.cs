using System;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;
using Template.Constraint;

namespace Template
{
    /// <summary>
    /// 參數掃描實驗：同一個模型套多組 CplexConfig，每組跑一次 <see cref="Trial.Capture"/>
    /// 記錄「完整設定快照 + 收斂數據」，累積成 <see cref="Experiment"/> 後輸出
    /// Experiments/&lt;name&gt;.csv + .json。
    ///
    /// 與 solve 模式共用 <see cref="VariableCreate"/> / <see cref="BuildModel"/>，建模邏輯不重複。
    /// 執行：dotnet run -- experiment
    /// </summary>
    public static class ExperimentRunner
    {
        public static void Run()
        {
            Logging.SetLogFileName("Template_Experiment");

            var exp = new Experiment(
                "template-tuning",
                "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響");

            // 每組：(label, 在基準 config 上套用的調整)。
            // 抽象旋鈕（config.Emphasis / config.Seed，來自 ITunableConfig）跨引擎一致；
            // camelCase 欄位（varSel / nodeSelect / workThreads）為 CPLEX 專屬。
            var variants = new (string label, Action<CplexConfig> tune)[]
            {
                ("baseline",          _ => { }),
                ("emphasis=optimal",  c => c.Emphasis    = 2),
                ("varsel=strong",     c => c.varSel      = 3),
                ("nodesel=bestbound", c => c.nodeSelect  = 1),
                ("gap=0.01",          c => c.epGap       = 0.01),
                ("threads=4",         c => c.workThreads = 4),
                ("seed=20260621",     c => c.Seed        = 20260621),
            };

            int i = 0;
            foreach (var (label, tune) in variants)
            {
                i++;
                var config = new CplexConfig
                {
                    epGap       = 0.03,
                    timeLimit   = 60,
                    workThreads = 8,
                    enableLog   = false,   // 掃描時關 solver log 以加速
                    exportLP    = false,
                    exportMPS   = false,
                    exportSol   = false,
                };
                tune(config);

                // 每個 Trial 用全新 Dataload + engine，避免狀態跨 Trial 污染
                var dataload = new Dataload();
                using var engine = new OptEngine(config);
                engine.Build();
                new VariableCreate(dataload, engine).Build();
                new BuildModel(dataload, engine).Build();

                Logging.Info($"[Experiment] ({i}/{variants.Length}) 求解中：{label} …");

                // 套件化單次擷取：抓這一 run 的完整設定 + 收斂數據（CPLEX 自動含收斂軌跡）
                var trial = Trial.Capture(engine, label, () => engine.Solve());
                exp.AddTrial(trial);

                var mtr = trial.Metrics;
                Logging.Info(
                    $"[Experiment] ({i}/{variants.Length}) {label}: Status={mtr.Status} " +
                    $"Obj={mtr.ObjectiveValue:G6} Gap={mtr.MipGap:P2} Time={mtr.WallTimeMs:F0}ms " +
                    $"Nodes={mtr.NodeCount} Vars={mtr.VarCount} Cons={mtr.ConstraintCount} " +
                    $"Traj={mtr.Convergence.Count}");   // 收斂軌跡點數（CPLEX 自動擷取）
            }

            exp.Save();   // → Experiments/template-tuning.csv + .json
            Logging.Info($"[Experiment] 完成：{exp.Trials.Count} 個 Trial 已寫入 {FolderDir.Experiment.GetPath()}");
        }
    }
}
