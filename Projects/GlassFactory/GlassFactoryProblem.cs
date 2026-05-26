// 數學模型見 Model/GlassFactory_Model.md

using System.Diagnostics;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Variable;
using GlassFactory.Constraint;

namespace GlassFactory
{
    public class GlassFactoryProblem : IDisposable
    {
        public OptEngine?     optEngine;
        public GlassDataload  dataload;

        public Stopwatch buildModelTimer = new Stopwatch();
        public Stopwatch totalTimer      = new Stopwatch();

        public GlassFactoryProblem()
        {
            dataload = new GlassDataload();
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

            buildModelTimer.Restart();

            new VariableCreate(dataload, optEngine).Build();
            Logging.Info("【建構變數完成】", buildModelTimer);

            new BuildModel(dataload, optEngine).Build();
            Logging.Info("【建構模型完成】", buildModelTimer);

            buildModelTimer.Stop();

            bool isSuccess = optEngine.Solve();
            if (isSuccess) dataload.WriteToCSV(optEngine);

            totalTimer.Stop();
            Logging.Info("整體運作時間:", totalTimer);
            return isSuccess;
        }

        public void Dispose() => optEngine?.Dispose();
    }
}
