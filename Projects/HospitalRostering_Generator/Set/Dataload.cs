using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Parameter;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Set
{
    /// <summary>
    /// Sets + Parameters + 罰分權重 + 結果輸出。架構 A / B 共用同一份資料語意。
    /// （stub：欄位/簽名已就位；實際資料載入見 spec「逐步實作」與 HospitalRosteringProblem_new/Data/Dataload.cs）
    /// </summary>
    public class Dataload
    {
        // 罰分權重（w1..w7，對應 Model Objective）—— TODO: 逐步實作填入正式權重
        public double Penalty_OffOneDay     = 0.1;  // w1
        public double Penalty_DoubleOffLT2  = 0.1;  // w2
        public double Penalty_GroupMismatch = 0.2;  // w3
        public double Penalty_NightToDay    = 0.2;  // w4
        public double Penalty_BelowAVG      = 0.1;  // w5
        public double Penalty_SixDay        = 1.0;  // w6
        public double Penalty_Weekend4Day   = 0.1;  // w7
        public double Penalty_BackupGroup   = 0.0;

        // Sets
        public List<string>   Employee = new();
        public List<string>   Group    = new();
        public List<DateTime> Date     = new();

        // Parameters（建模查詢用）
        public List<Parameter_ShiftDemand> parameter_ShiftDemand = new();
        public List<Parameter_PreAssign>   parameter_PreAssign   = new();
        public List<Parameter_NightToDay>  parameter_NightToDay  = new();
        public List<Parameter_CrossGroup>  parameter_CrossGroup  = new();
        public List<Parameter_BackupGroup> parameter_BackupGroup = new();

        public Dataload()
        {
            // TODO（逐步實作）：建 Group/Employee/Date 集合、ShiftDemand/PreAssign/NightToDay/CrossGroup/BackupGroup
            //                  與 AVGOFF 推算。資料語意與 HospitalRosteringProblem_new/Data/Dataload.cs 一致。
        }

        public void WriteToCSV(OptEngine engine)
        {
            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableB_ShiftAssign>(engine, "V1", "VIC");
            CsvCtrl.SaveSolutionToCSV<VariableX_WeekendLT4> (engine, "V1", "VIC");
            CsvCtrl.SaveSolutionToCSV<VariableX_BelowAVG>   (engine, "V1", "VIC");
            CsvCtrl.SaveSolutionToCSV<VariableB_Off1Day>    (engine, "V1", "VIC");
        }
    }
}
