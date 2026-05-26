// 數學模型見 Model/ClinicVitamin_Model.md

using System.Diagnostics;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Set;
using ClinicVitamin.Variable;
using ClinicVitamin.Constraint;

namespace ClinicVitamin
{
    public class ClinicVitaminProblem : IDisposable
    {
        public OptEngine?    optEngine;
        public ClinicDataload dataload;
        public Stopwatch     totalTimer = new();

        public ClinicVitaminProblem()
        {
            dataload = new ClinicDataload();
            Logging.SetLogFileName(GetType().Name);
        }

        public bool Execute()
        {
            totalTimer.Restart();

            CplexConfig config = new CplexConfig
            {
                epGap       = 0.0,
                timeLimit   = 60,
                workThreads = 4,
                enableLog   = true,
                exportSol   = true,
                exportLP    = true,
                exportMPS   = false
            };

            optEngine = new OptEngine(config);
            optEngine.Build();

            new VariableCreate(dataload, optEngine).Build();
            new BuildModel(dataload, optEngine).Build();

            bool ok = optEngine.Solve();
            if (ok) dataload.WriteToCSV(optEngine);

            totalTimer.Stop();
            Logging.Info("整體運作時間:", totalTimer);
            return ok;
        }

        public void Dispose() => optEngine?.Dispose();
    }
}
