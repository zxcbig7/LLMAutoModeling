using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Objective
{
    /// <summary>
    /// 目標式：最小化七種違規的加權總和（見 Model/HospitalRostering_Model.md §Objective）。
    /// 與 HospitalRostering_Generator 的 ObjectiveFunction 邏輯相同。
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
                // TODO（逐步實作）：依 Penalty_* 權重累加 7 個違規項（s^off1/s^six/s^mis/s^ntd/s^dlt/z^avg/z^wkd）
                //                  係數一律來自 dataload.Penalty_*，禁止寫死裸數字。
                optEngine.CreateMinimize();
                Logging.Info("目標函數建構完成（stub）");
            }
            catch (Exception) { throw; }
        }
    }
}
