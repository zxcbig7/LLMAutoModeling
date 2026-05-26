using System.Diagnostics;

using OptimFoundation.Cplex;
using OptimFoundation.Core;

using SandBox.Data;
using SandBox.Constraints;
using SandBox.VariablesClass;

namespace SandBox
{
    /// <summary>
    /// 通用問題範本（不含特定業務邏輯）。
    /// 複製這個類別到新專案後，將 GenericDataload 換成自己的 Dataload 即可。
    ///
    /// 使用方式（Program.cs）：
    ///   using (var p = new GenericProblem()) { p.Execute(); }
    /// </summary>
    public class GenericProblem : IDisposable
    {
        public OptEngine? optEngine;
        public GenericDataload dataload;

        public Stopwatch buildModelTimer = new Stopwatch();
        public Stopwatch totalTimer      = new Stopwatch();
        public TimeSpan  totalTimeSpan   = new TimeSpan();

        private bool   _isSuccess;
        private string _projectName => GetType().Name;

        public GenericProblem()
        {
            dataload   = new GenericDataload();
            _isSuccess = false;
            Logging.SetLogFileName(_projectName);
        }

        public bool Execute()
        {
            totalTimer.Restart();

            // ── 求解器設定 ────────────────────────────────────────────────
            CplexConfig config = new CplexConfig
            {
                epGap       = 0.03,  // MIP gap 容忍度（3%）
                timeLimit   = 100,   // 最長求解秒數
                workThreads = 10,    // 平行執行緒
                enableLog   = true,  // 顯示 CPLEX log
                exportSol   = true,  // 匯出 .sol
                exportLP    = true,  // 匯出 .lp（可讀格式，便於除錯）
                exportMPS   = true   // 匯出 .mps
            };

            optEngine = new OptEngine(config);
            optEngine.Build();

            // ── 建構模型 ──────────────────────────────────────────────────
            buildModelTimer.Restart();

            new GenericVariableCreate(dataload, optEngine).Build();
            Logging.Info("【建構變數完成】", buildModelTimer);

            new GenericBuildModel(dataload, optEngine).Build();
            Logging.Info("【建構模型完成】", buildModelTimer);

            buildModelTimer.Stop();

            // ── 求解 ──────────────────────────────────────────────────────
            _isSuccess = optEngine.Solve();

            if (_isSuccess)
                dataload.WriteToCSV(optEngine);

            totalTimeSpan = totalTimer.Elapsed;
            totalTimer.Stop();

            Logging.Info("【整體運作時間】", totalTimer);
            return _isSuccess;
        }

        public void Dispose()
        {
            optEngine?.Dispose();
        }
    }
}
