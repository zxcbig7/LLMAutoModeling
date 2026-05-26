using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace SandBox.Data
{
    /// <summary>
    /// 通用資料載入範本。
    /// Sets = 索引集合；Parameters = 模型參數；Penalties = 目標式罰分權重。
    /// </summary>
    public class GenericDataload
    {
        // ── 罰分權重（目標式係數） ─────────────────────────────────────
        public double Penalty_Overflow   = 1.0;   // 超額懲罰
        public double Penalty_Underflow  = 0.5;   // 不足懲罰
        public double Penalty_Slack      = 0.1;   // 寬鬆懲罰

        // ── Sets（索引集合） ──────────────────────────────────────────
        public List<string>   Items    = new();   // 品項 / 人員 / 任務
        public List<string>   Machines = new();   // 資源 / 班別 / 機台
        public List<DateTime> Periods  = new();   // 時間軸

        // ── Parameters（模型參數） ────────────────────────────────────
        public List<Parameter_GenericDemand> parameter_Demand = new(); // 每期需求
        public List<Parameter_GenericCost>   parameter_Cost   = new(); // 品項×機台成本

        public GenericDataload()
        {
            // ① 建立 Sets
            Items.AddRange(["Item_A", "Item_B", "Item_C"]);
            Machines.AddRange(["Machine_1", "Machine_2"]);

            int year = 2026, month = 1;
            int days = DateTime.DaysInMonth(year, month);
            for (int d = 1; d <= days; d++)
                Periods.Add(new DateTime(year, month, d));

            // ② 建立 Parameters
            var rng = new Random(42);
            foreach (var p in Periods)
            {
                foreach (var i in Items)
                    parameter_Demand.Add(new Parameter_GenericDemand
                    {
                        Item   = i,
                        Period = p,
                        QTY    = rng.Next(2, 6)
                    });
            }

            foreach (var i in Items)
                foreach (var m in Machines)
                    parameter_Cost.Add(new Parameter_GenericCost
                    {
                        Item    = i,
                        Machine = m,
                        QTY     = rng.NextDouble() * 10
                    });

            // ③ CSV 讀取（需要時取消註解）
            // Items    = CSVCtrl.ReadStrSet("Set_Items.csv");
            // Machines = CSVCtrl.ReadStrSet("Set_Machines.csv");
            // Periods  = CSVCtrl.ReadDateSet("Set_Periods.csv");
            // parameter_Demand = CSVCtrl.BuildParameter<Parameter_GenericDemand>("Param_Demand");
        }

        public void WriteToCSV(OptEngine engine)
        {
            // 依需要取消註解
            // CSVCtrl.SaveToCSV<VariableB_Assign>(engine.GetSetVarSol<VariableB_Assign>(),    DATA_ID: "V1", USER_ID: "VIC");
            // CSVCtrl.SaveToCSV<VariableB_Overflow>(engine.GetSetVarSol<VariableB_Overflow>(), DATA_ID: "V1", USER_ID: "VIC");
            // CSVCtrl.SaveToCSV<VariableX_Slack>(engine.GetSetVarSol<VariableX_Slack>(),       DATA_ID: "V1", USER_ID: "VIC");
        }
    }
}
