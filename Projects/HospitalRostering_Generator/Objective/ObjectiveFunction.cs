using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Objective
{
    /// <summary>
    /// 目標式：最小化七種違規的加權總和（見 Model/HospitalRostering_Model.md §Objective）。
    /// min Σ_{e,d}(w1·s^off1 + w6·s^six + w3·s^mis + w4·s^ntd) + Σ_e(w2·s^dlt + w5·z^avg + w7·z^wkd)
    /// 與 HospitalRostering_Manual 的 ObjectiveFunction 內容逐行相同。
    /// </summary>
    public class ObjectiveFunction
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public ObjectiveFunction(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // 每日 × 每人的違規指示變數（w1/w6/w3/w4）
                dataload.Date.ForEach(d =>
                {
                    dataload.Employee.ForEach(e =>
                    {
                        optEngine.AddLHS(dataload.Penalty_OffOneDay,     new VariableB_Off1Day      { Date = d, Employee = e }); // w1
                        optEngine.AddLHS(dataload.Penalty_SixDay,        new VariableB_SixDayWork   { Date = d, Employee = e }); // w6
                        optEngine.AddLHS(dataload.Penalty_GroupMismatch, new VariableB_GroupMismatch{ Date = d, Employee = e }); // w3
                        optEngine.AddLHS(dataload.Penalty_NightToDay,    new VariableB_NightToDay   { Date = d, Employee = e }); // w4
                    });
                });

                // 每人層級（w2/w5/w7）
                dataload.Employee.ForEach(e =>
                {
                    optEngine.AddLHS(dataload.Penalty_DoubleOffLT2, new VariableB_DoubleOffLT2 { Employee = e }); // w2
                    optEngine.AddLHS(dataload.Penalty_BelowAVG,     new VariableX_BelowAVG     { Employee = e }); // w5
                    optEngine.AddLHS(dataload.Penalty_Weekend4Day,  new VariableX_WeekendLT4   { Employee = e }); // w7
                });

                optEngine.CreateMinimize();
                Logging.Info("目標函數建構完成");
            }
            catch (Exception) { throw; }
        }
    }
}
