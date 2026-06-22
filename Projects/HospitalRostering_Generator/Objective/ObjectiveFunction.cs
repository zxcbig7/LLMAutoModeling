using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Objective
{
    /// <summary>
    /// 目標式：最小化七種違規的加權總和（見 Model/HospitalRostering_Model.md §Objective）。
    /// min Σ_{e,d}(w1·s^off1 + w6·s^six + w3·s^mis + w4·s^ntd) + Σ_e(w2·s^dlt + w5·z^avg + w7·z^wkd)
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
