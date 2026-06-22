using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Manual.Parameter;
using HospitalRostering_Manual.Variable;

namespace HospitalRostering_Manual.Set
{
    /// <summary>
    /// Sets + Parameters + 罰分權重 + 結果輸出。與 HospitalRostering_Generator 的 Dataload 資料語意完全相同
    /// （差異只在 Parameter class 是手寫還是 generator 生成）。
    /// </summary>
    public class Dataload
    {
        // 罰分權重（對應 Model Objective 的 w1..w7）
        public double Penalty_OffOneDay     = 0.1;  // w1：做一休一做
        public double Penalty_DoubleOffLT2  = 0.1;  // w2：整月無連休
        public double Penalty_GroupMismatch = 0.2;  // w3：跨組別支援
        public double Penalty_NightToDay    = 0.2;  // w4：不良班別轉換
        public double Penalty_BelowAVG      = 0.1;  // w5：休假低於平均
        public double Penalty_SixDay        = 1.0;  // w6：連續工作 6 天
        public double Penalty_Weekend4Day   = 0.1;  // w7：週末休假不足
        public double Penalty_PreGroup      = 0.2;  // parameter_NightToDay 標示用（QTY，非目標係數）
        public double Penalty_BackupGroup   = 0.0;  // Backup 班別不罰

        // Sets
        public List<string>   Employee = new();
        public List<string>   Group    = new();
        public List<DateTime> Date     = new();

        // Parameters
        public List<Parameter_ShiftDemand> parameter_ShiftDemand = new();
        public List<Parameter_PreAssign>   parameter_PreAssign   = new();
        public List<Parameter_NightToDay>  parameter_NightToDay  = new();
        public List<Parameter_CrossGroup>  parameter_CrossGroup  = new();
        public List<Parameter_BackupGroup> parameter_BackupGroup = new();

        private enum GroupE { O, D, E, N, C }

        public Dataload()
        {
            // 班別群組：休 O + 工作班別 D/E/N/C
            Group.AddRange(Enum.GetNames(typeof(GroupE)));

            // 人員：各主責班別人數 → 共 16 人（E1..E16）
            var empByGroup = new (int count, string mainGroup)[] { (7, "D"), (5, "E"), (3, "N"), (1, "C") };
            int totalEmp = empByGroup.Sum(s => s.count);
            for (int e = 1; e <= totalEmp; e++) Employee.Add($"E{e}");

            // 跨組別支援集合 CG_e：員工主責 / 休以外的工作班別（QTY 作標記，模型以 Objective 統一罰分）
            int idx = 1;
            foreach (var (count, mainGroup) in empByGroup)
            {
                var crossGroups = Group.Where(g => g != mainGroup && g != "O").ToList();
                for (int j = 0; j < count; j++)
                {
                    foreach (var cg in crossGroups)
                        parameter_CrossGroup.Add(new Parameter_CrossGroup { Employee = $"E{idx}", Group = cg, QTY = Penalty_GroupMismatch });
                    idx++;
                }
            }

            // Backup 班別：E1 的 C 班視為 Backup，跨組別成本歸零
            parameter_BackupGroup.Add(new Parameter_BackupGroup { Employee = "E1", Group = "C" });
            foreach (var backup in parameter_BackupGroup)
            {
                var cg = parameter_CrossGroup.FirstOrDefault(w => w.Employee == backup.Employee && w.Group == backup.Group);
                if (cg != null) cg.QTY = Penalty_BackupGroup;
            }

            // 不良班別轉換對 R（昨天 PreGroup → 今天 Group）
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "N", Group = "D", QTY = Penalty_PreGroup }); // 晚→早
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "N", Group = "E", QTY = Penalty_PreGroup }); // 晚→午
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "N", Group = "C", QTY = Penalty_PreGroup }); // 晚→行
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "E", Group = "D", QTY = Penalty_PreGroup }); // 午→早
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "E", Group = "C", QTY = Penalty_PreGroup }); // 午→行
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "D", Group = "N", QTY = Penalty_PreGroup }); // 早→晚
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "E", Group = "N", QTY = Penalty_PreGroup }); // 午→晚
            parameter_NightToDay.Add(new Parameter_NightToDay { PreGroup = "C", Group = "N", QTY = Penalty_PreGroup }); // 行→晚

            // 排程月份與每日需求。★ 固定 seed：兩專案（與每次執行）必須解「同一個」題目實例，
            //   tuning 對照才公平、結果才可重現（原 _new 用未 seed 的 Random，實例每次不同）。
            int year = 2026, month = 1;
            int daysInMonth = DateTime.DaysInMonth(year, month);
            var random = new Random(20260101);
            for (int day = 1; day <= daysInMonth; day++)
            {
                var d = new DateTime(year, month, day);
                Date.Add(d);
                parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "D", QTY = random.Next(4, 6) }); // 4~5 人
                parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "E", QTY = 3 });
                parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "N", QTY = 2 });
                parameter_ShiftDemand.Add(new Parameter_ShiftDemand { Date = d, Group = "C", QTY = 1 });
            }

            // 預排班 PA（固定指派）
            parameter_PreAssign.Add(new Parameter_PreAssign { Date = new DateTime(2026, 1, 1), Employee = "E1", Group = "E" });
            parameter_PreAssign.Add(new Parameter_PreAssign { Date = new DateTime(2026, 1, 1), Employee = "E3", Group = "O" });
            parameter_PreAssign.Add(new Parameter_PreAssign { Date = new DateTime(2026, 1, 2), Employee = "E2", Group = "D" });
            parameter_PreAssign.Add(new Parameter_PreAssign { Date = new DateTime(2026, 1, 2), Employee = "E3", Group = "E" });
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
