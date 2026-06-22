using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;
using HospitalRostering_Manual.Constraint;

namespace HospitalRostering_Manual
{
    /// <summary>
    /// 架構 A 的 composition root（手寫）：自行 new/Build/Dispose OptEngine，
    /// 手動依序呼叫建變數 → 建模型 → 求解 → 寫 CSV。與架構 B 的差異只在「誰驅動引擎」，
    /// 數學模型（VariableCreate / BuildModel）與 B 完全相同。
    /// </summary>
    public sealed class HospitalRosteringProblem : IDisposable
    {
        private readonly Dataload _data = new();
        private OptEngine? _engine;

        public bool Execute()
        {
            var config = new CplexConfig
            {
                epGap       = 0.03,
                timeLimit   = 100,
                workThreads = 10,
                enableLog   = true,
                exportSol   = true,
                exportLP    = true,
                exportMPS   = true,
            };

            _engine = new OptEngine(config);
            _engine.Build();                                  // 1. 初始化 CPLEX 環境
            new VariableCreate(_data, _engine).Build();       // 2. 建變數（與 experiment 共用）
            new BuildModel(_data, _engine).Build();           // 3. 建目標式 + 限制式（與 experiment 共用）
            bool ok = _engine.Solve();                        // 4. 求解（★ 必須接收 bool）

            Logging.Info($"求解結果：{(ok ? "成功" : "失敗")}  Status={_engine.Status}");
            if (ok) _data.WriteToCSV(_engine);                // 5. 輸出
            return ok;
        }

        public void Dispose() => _engine?.Dispose();
    }
}
