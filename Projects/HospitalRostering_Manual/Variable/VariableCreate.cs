using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Set;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>
    /// 統一建立所有決策變數（solve 與 experiment 兩模式共用）。
    /// 屬性順序對應 set 傳入順序。與 HospitalRostering_Generator 的 VariableCreate **逐行相同**。
    /// </summary>
    public class VariableCreate
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public VariableCreate(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                optEngine.BuildBVs<VariableB_ShiftAssign>(dataload.Date, dataload.Employee, dataload.Group); // y[e,d,g]
                optEngine.BuildBVs<VariableB_GroupMismatch>(dataload.Date, dataload.Employee);               // s^mis
                optEngine.BuildBVs<VariableB_NightToDay>(dataload.Date, dataload.Employee);                  // s^ntd
                optEngine.BuildBVs<VariableB_DoubleOffFlag>(dataload.Date, dataload.Employee);               // s^dfl
                optEngine.BuildBVs<VariableB_DoubleOffLT2>(dataload.Employee);                               // s^dlt
                optEngine.BuildBVs<VariableB_Off1Day>(dataload.Date, dataload.Employee);                     // s^off1
                optEngine.BuildBVs<VariableB_SixDayWork>(dataload.Date, dataload.Employee);                  // s^six
                optEngine.BuildCVs<VariableX_BelowAVG>(dataload.Employee);                                   // z^avg
                optEngine.BuildCVs<VariableX_WeekendLT4>(dataload.Employee);                                 // z^wkd

                Logging.Info($"Variables created: {optEngine.varCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
