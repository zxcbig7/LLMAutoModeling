using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandBox.Data;
using SandBox.VariableClass;

namespace SandBox.Constraints
{
    /// <summary>
    /// 通用目標函數範本。
    ///
    /// 建構方式：
    ///   1. 用 AddLHS(penalty, variable) 累加各罰分項
    ///   2. 最後呼叫 CreateMinimize() 或 CreateMaximize()
    /// </summary>
    public class GenericObjectiveFunction
    {
        private OptEngine      optEngine;
        private GenericDataload dataload;

        public GenericObjectiveFunction(GenericDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // ── 罰分累加 ──────────────────────────────────────────────────
                dataload.Periods.ForEach(p =>
                {
                    dataload.Items.ForEach(i =>
                    {
                        // Binary 輔助變數的罰分
                        optEngine.AddLHS(dataload.Penalty_Overflow,
                            new VariableB_Overflow { Item = i, Period = p });
                    });
                });

                dataload.Items.ForEach(i =>
                {
                    // Continuous 變數的罰分
                    optEngine.AddLHS(dataload.Penalty_Slack,
                        new VariableX_Slack { Item = i });
                });

                // ── 建立最小化目標 ─────────────────────────────────────────────
                optEngine.CreateMinimize();
                // 或最大化：optEngine.CreateMaximize();

                Logging.Info("【目標函數建構完成】");
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
