using System;
using System.Collections.Generic;

using OptimFoundation.Cplex;
using OptimFoundation.Core;

using SandBox.Data;
using SandBox.Constraints;
using SandBox.VariablesClass;

namespace SandBox
{
    /// <summary>
    /// Hospital Rostering 的參數調整實驗：同一個模型掃多組 solver 設定，
    /// 每組跑一次 <see cref="Trial.Capture"/> 記錄「完整設定 + 收斂數據」，
    /// 累積成 <see cref="Experiment"/> 後輸出 Experiments/&lt;name&gt;.csv + .json。
    ///
    /// 執行：dotnet run -- experiment
    /// </summary>
    public static class ExperimentRunner
    {
        // 掃描時的共用基準（關 log / 關 exports 以加速；timeLimit 確保每個 Trial 都會結束）
        private const double BaseTimeLimit = 60;
        private const double BaseEpGap     = 0.03;
        private const int    BaseThreads   = 10;

        public static void Run()
        {
            Logging.SetLogFileName("HospitalRostering_Experiment");

            var exp = new Experiment(
                "hospital-rostering-tuning",
                "掃描 mipEmphasis / varSel / nodeSelect / epGap / threads / seed 對求解時間與 gap 的影響");

            // 每組：(label, 在基準 config 上套用的調整)。
            // 抽象旋鈕（config.Emphasis / config.Seed）跨引擎一致；camelCase 欄位為 CPLEX 專屬。
            var variants = new (string label, Action<CplexConfig> tune)[]
            {
                ("baseline",            _ => { }),
                ("emphasis=feasible",   c => c.Emphasis    = 1),   // 抽象旋鈕 → mipEmphasis
                ("emphasis=optimal",    c => c.Emphasis    = 2),
                ("emphasis=bestbound",  c => c.Emphasis    = 3),
                ("varsel=strong",       c => c.varSel      = 3),   // CPLEX 專屬：強分支
                ("nodesel=bestbound",   c => c.nodeSelect  = 1),   // CPLEX 專屬：best-bound 節點選擇
                ("gap=0.01",            c => c.epGap       = 0.01),
                ("threads=4",           c => c.workThreads = 4),
                ("seed=20260619",       c => c.Seed        = 20260619),
            };

            int i = 0;
            foreach (var (label, tune) in variants)
            {
                i++;
                var config = new CplexConfig
                {
                    epGap       = BaseEpGap,
                    timeLimit   = BaseTimeLimit,
                    workThreads = BaseThreads,
                    enableLog   = false,   // 掃描時關 solver log
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

                var m = trial.Metrics;
                Logging.Info(
                    $"[Experiment] ({i}/{variants.Length}) {label}: Status={m.Status} " +
                    $"Obj={m.ObjectiveValue:G6} Gap={m.MipGap:P2} Time={m.WallTimeMs:F0}ms " +
                    $"Nodes={m.NodeCount} Vars={m.VarCount} Cons={m.ConstraintCount} " +
                    $"Traj={m.Convergence.Count}");
            }

            exp.Save();   // → Experiments/hospital-rostering-tuning.csv + .json
            Logging.Info(
                $"[Experiment] 完成：{exp.Trials.Count} 個 Trial 已寫入 " +
                $"{FolderDir.Experiment.GetPath()}");
        }
    }
}
