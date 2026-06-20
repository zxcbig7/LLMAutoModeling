using OptimFoundation.Core;
using OptimFoundation.Cplex;

using SandBox.Data;
using SandBox.Constraints;
using SandBox.VariableClass;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // ── 組裝點（Composition Root）─────────────────────────────────────
            // 完整 Hospital Rostering 模型，透過 registry 組裝。
            // 變數/約束/目標式的順序與原 VariableCreate / BuildModel 逐一對應，
            // 因此產生的模型與原 RosteringProblem 完全相同（結果一模一樣）。
            // OptModel 不認得以下任何具體型別。
            var data = new Dataload();

            using var problem = new OptModel("RosteringProblem_new")
                .UseConfig(() => new CplexConfig
                {
                    epGap = 0.03,
                    timeLimit = 100,
                    workThreads = 10,
                    enableLog = true,
                    exportSol = true,
                    exportLP = true,
                    exportMPS = true
                })

                // ── 註冊變數 class（對應 VariableCreate.Build）────────────────
                .AddVariables(e => e.BuildBVs<VariableB_ShiftAssign>(data.Date, data.Employee, data.Group))
                .AddVariables(e => e.BuildBVs<VariableB_GroupMismatch>(data.Date, data.Employee))
                .AddVariables(e => e.BuildBVs<VariableB_NightToDay>(data.Date, data.Employee))
                .AddVariables(e => e.BuildBVs<VariableB_DoubleOffFlag>(data.Date, data.Employee))
                .AddVariables(e => e.BuildBVs<VariableB_DoubleOffLT2>(data.Employee))
                .AddVariables(e => e.BuildBVs<VariableB_Off1Day>(data.Date, data.Employee))
                .AddVariables(e => e.BuildBVs<VariableB_SixDayWork>(data.Date, data.Employee))
                .AddVariables(e => e.BuildCVs<VariableX_BelowAVG>(data.Employee))
                .AddVariables(e => e.BuildCVs<VariableX_WeekendLT4>(data.Employee))

                // ── 註冊目標式 + 限制式 class（對應 BuildModel.Build）─────────
                .AddModel(e => new ObjectiveFunction(data, e).Build())

                // 基本限制式
                .AddModel(e => new Constraint_FullfillDemand(data, e).Build())
                .AddModel(e => new Constraint_OneGroup(data, e).Build())
                .AddModel(e => new Constraint_PreAssign(data, e).Build())

                // 進階限制式
                .AddModel(e => new Constraint_SixDayWork(data, e).Build())
                .AddModel(e => new Constraint_NightToDay(data, e).Build())
                .AddModel(e => new Constraint_OffOneDay(data, e).Build())
                .AddModel(e => new Constraint_CrossGroup(data, e).Build())
                .AddModel(e => new Constraint_BelowAVG(data, e).Build())
                .AddModel(e => new Constraint_WeekendLT4(data, e).Build())
                .AddModel(e => new Constraint_DoubleOffLT2(data, e).Build())

                // ── 求解成功後輸出 ───────────────────────────────────────────
                .OnSolved(e => data.WriteToCSV(e));

            problem.Execute();

            Logging.Info("整體運作時間:", problem.totalTimer);
        }
    }
}
