// ?¸å­¸æ¨¡å?è¦?Model/FactorioOptimization_Model.md

using System.Diagnostics;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;
using FactorioOptimization.Constraint;

namespace FactorioOptimization
{
    public class FactorioOptimizationProblem : IDisposable
    {
        public OptEngine?                   optEngine;
        public FactorioOptimizationDataload dataload;

        public Stopwatch buildModelTimer = new Stopwatch();
        public Stopwatch totalTimer      = new Stopwatch();

        public FactorioOptimizationProblem()
        {
            dataload = new FactorioOptimizationDataload();
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
            Logging.Info("?å»ºæ§‹è??¸å??ã€?, buildModelTimer);

            new BuildModel(dataload, optEngine).Build();
            Logging.Info("?å»ºæ§‹æ¨¡?‹å??ã€?, buildModelTimer);

            buildModelTimer.Stop();

            bool isSuccess = optEngine.Solve();
            if (isSuccess) dataload.WriteToCSV(optEngine);

            totalTimer.Stop();
            Logging.Info("?´é??‹ä??‚é?:", totalTimer);
            return isSuccess;
        }

        public void Dispose() => optEngine?.Dispose();
    }
}
