using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;

namespace HospitalRostering_Generator.Variable
{
    /// <summary>
    /// 統一建立所有決策變數（solve 與 experiment 兩模式共用）。
    /// 屬性順序對應 set 傳入順序；變數 class body 由 AutoSetsGenerator 依 [OptVar] 生成。
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
                optEngine.BuildVars<VariableB_ShiftAssign>(dataload.Date, dataload.Employee, dataload.Group); // y[e,d,g]
                optEngine.BuildVars<VariableB_GroupMismatch>(dataload.Date, dataload.Employee);               // s^mis
                optEngine.BuildVars<VariableB_NightToDay>(dataload.Date, dataload.Employee);                  // s^ntd
                optEngine.BuildVars<VariableB_DoubleOffFlag>(dataload.Date, dataload.Employee);               // s^dfl
                optEngine.BuildVars<VariableB_DoubleOffLT2>(dataload.Employee);                               // s^dlt
                optEngine.BuildVars<VariableB_Off1Day>(dataload.Date, dataload.Employee);                     // s^off1
                optEngine.BuildVars<VariableB_SixDayWork>(dataload.Date, dataload.Employee);                  // s^six
                optEngine.BuildVars<VariableC_BelowAVG>(dataload.Employee);                                   // z^avg
                optEngine.BuildVars<VariableC_WeekendLT4>(dataload.Employee);                                 // z^wkd

                Logging.Info($"Variables created: {optEngine.VariableCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
