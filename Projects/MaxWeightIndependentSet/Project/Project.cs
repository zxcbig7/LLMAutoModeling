using System;
using System.Linq;

using OptimFoundation.Core;
using OptimFoundation.Cplex;

using MaxWeightIndependentSet.Model;

namespace MaxWeightIndependentSet.Project
{
    // 單次求解（正確性 gate）。ProblemType = IP → 預設 mipEmphasis=1。
    // composition 走框架的 Fluent OptModel；engine 由 OptModel 持有，Report() 在求解後讀解。
    public class MwisProject : IDisposable
    {
        public OptEngine? engine;
        public Dataload dataload;
        private const string ProjectName = "MaxWeightIndependentSet";

        public MwisProject()
        {
            dataload = new Dataload();
            Logging.SetLogFileName(ProjectName);
        }

        public bool Execute()
        {
            using var m = new OptModel(ProjectName)
                .UseConfig(() => new CplexConfig
                {
                    mipEmphasis = 1,     // IP 預設
                    timeLimit   = 1800,
                    epGap       = 1e-4,
                    workThreads = 8,
                    enableLog   = true,
                    exportLP    = true,
                    exportSol   = true,
                })
                .AddVariables(e => new VariableCreate(dataload, e).Build())
                .AddModel(e => new BuildConstraints(dataload, e).Build());

            bool ok = m.Execute();
            engine = m.optEngine;   // 供 Report() 讀解（仍在 m 的 using 範圍內，engine 尚存活）
            Report();
            return ok;
        }

        private void Report()
        {
            if (engine == null) return;

            Console.WriteLine($"Nodes={dataload.NODE.Count}  Edges={dataload.EDGE.Count}  Status={engine.Status}");

            if (engine.Status != SolveStatus.Optimal && engine.Status != SolveStatus.Feasible)
                return;

            double obj = engine.GetObjectiveValue();
            var sol = engine.GetSetVarValues<VariableY_Select>();
            int picked = sol.Count(kvp => kvp.Value > 0.5);

            Logging.Info($"Objective (total weight) = {obj:F2}");
            Logging.Info($"Selected nodes = {picked} / {dataload.NODE.Count}");
            Console.WriteLine($"Objective (total weight) = {obj:F2}");
            Console.WriteLine($"Selected nodes = {picked} / {dataload.NODE.Count}");
        }

        // engine 由 OptModel 的 using 範圍持有並 Dispose；此處不重複擁有。
        public void Dispose() { }
    }
}
