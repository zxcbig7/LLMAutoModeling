using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandwichProduction.Constraint;
using SandwichProduction.Set;
using SandwichProduction.Variable;

namespace SandwichProduction
{
    // 數學模型見 Model/SandwichProduction_Model.md
    public class SandwichProblem : IDisposable
    {
        public OptEngine? optEngine;
        private readonly SandwichDataload dataload;

        public SandwichProblem()
        {
            dataload = new SandwichDataload();
            Logging.SetLogFileName(GetType().Name);
        }

        public bool Execute()
        {
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

            new VariableCreate(dataload, optEngine).Build();   // 1. 建變數
            new BuildModel(dataload, optEngine).Build();        // 2. 建模型（目標 + 限制）
            bool isSuccess = optEngine.Solve();                 // 3. 求解

            if (isSuccess) dataload.WriteToCSV(optEngine);
            return isSuccess;
        }

        public void Dispose()
        {
            optEngine?.Dispose();
        }
    }
}
