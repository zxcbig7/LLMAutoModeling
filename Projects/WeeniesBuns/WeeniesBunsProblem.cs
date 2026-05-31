// 數學模型見 Model/WeeniesBuns_Model.md

using System.Diagnostics;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;
using WeeniesBuns.Constraint;

namespace WeeniesBuns
{
    public class WeeniesBunsProblem : IDisposable
    {
        public OptEngine?           optEngine;
        public WeeniesBunsDataload  dataload;

        public Stopwatch buildModelTimer = new Stopwatch();
        public Stopwatch totalTimer      = new Stopwatch();

        public WeeniesBunsProblem()
        {
            dataload = new WeeniesBunsDataload();
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
