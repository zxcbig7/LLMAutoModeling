using OptimFoundation.Core;
using OptimFoundation.Core.IO;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>兩個驗證入口：建模前的 ValidateData 資料驗收，與求解後的 ReadAndValidate 解驗證。
    /// 解讀層是唯一允許接收整包 Dataload 的地方——兩個驗證本來就要對照所有原始資料。</summary>
    public sealed class TemplateSolution
    {
        private const double Tolerance = 1e-6;

        private readonly Dictionary<string, double> produce;
        private readonly Dictionary<string, double> use;
        private readonly Dictionary<string, double> shortage;

        private TemplateSolution(
            Dictionary<string, double> produce,
            Dictionary<string, double> use,
            Dictionary<string, double> shortage)
        {
            this.produce = produce;
            this.use = use;
            this.shortage = shortage;
        }

        /// <summary>建模前資料驗收（Program.cs 模型段呼叫，緊接 OptData.Load 之後）。
        /// framework 只驗 key 重複與數值 sanity；全格矩陣與跨表關聯一律是專案責任，而且 MUST 在建模之前跑完。</summary>
        public static void ValidateData(Dataload data)
        {
            foreach (var item in data.set_Item)
            {
                RequireRow(data.parameter_Demand.Any(row => row.Item == item.Item), "Parameter_Demand", item.Item);
                RequireRow(data.parameter_MinActiveDays.Any(row => row.Item == item.Item), "Parameter_MinActiveDays", item.Item);
                RequireRow(data.parameter_MaxShortage.Any(row => row.Item == item.Item), "Parameter_MaxShortage", item.Item);
            }

            foreach (var date in data.set_Date)
                RequireRow(data.parameter_DailyCapacity.Any(row => row.Date == date.Date),
                    "Parameter_DailyCapacity", $"{date.Date:yyyy-MM-dd}");

            // 跨表關聯：AllowedSlot 的每個維度值都要落在對應的 Set 名單上
            foreach (var slot in data.set_AllowedSlot)
            {
                RequireRow(data.set_Item.Any(row => row.Item == slot.Item), "Set_Item", slot.Item);
                RequireRow(data.set_Date.Any(row => row.Date == slot.Date), "Set_Date", $"{slot.Date:yyyy-MM-dd}");
            }

            // scalar 一定要恰好一列；.Single() 在 0 列或 2 列時當場丟例外
            _ = data.parameter_ShortagePenalty.Single().QTY;
            _ = data.parameter_BigMProduce.Single().QTY;

            Logging.Info("[ValidateData] 全格 Parameter 與跨表關聯檢查通過。");
        }

        private static void RequireRow(bool exists, string source, string key)
        {
            if (!exists)
                throw new InvalidOperationException($"[Data] {source} 缺少 {key}——缺格代表資料漏了，不是「值為 0」。");
        }

        public static TemplateSolution ReadAndValidate(OptEngine engine, Dataload data)
        {
            Logging.Info($"Status={engine.Status} Obj={engine.GetObjectiveValue():F4} " +
                         $"BestBound={engine.LastMetrics.BestBound:F4} MIPGap={engine.LastMetrics.MipGap:P2}");

            var produce = engine.GetSetVarValues<VariableI_Produce>();
            var use = engine.GetSetVarValues<VariableB_Use>();
            var shortage = engine.GetSetVarValues<VariableC_Shortage>();

            ValidateRules(produce, use, shortage, data);

            CsvCtrl.WriteSolution<VariableI_Produce>(engine, "Template", "SYSTEM");
            CsvCtrl.WriteSolution<VariableB_Use>(engine, "Template", "SYSTEM");
            CsvCtrl.WriteSolution<VariableC_Shortage>(engine, "Template", "SYSTEM");
            return new TemplateSolution(produce, use, shortage);
        }

        /// <summary>逐條把解值代回 Model.md 的 [C1]–[C5]；不成立就丟例外，NEVER 只記 log 繼續。
        /// ★ 寫完這段後 MUST 故意改壞一個 key 或一個比較，確認它真的會 throw（api-guide §6）。</summary>
        private static void ValidateRules(
            Dictionary<string, double> produce,
            Dictionary<string, double> use,
            Dictionary<string, double> shortage,
            Dataload data)
        {
            foreach (var item in data.set_Item)
            {
                var slots = data.set_AllowedSlot.Where(row => row.Item == item.Item).ToList();

                // [C1] Sum Produce + Shortage = Demand
                double produced = slots.Sum(slot => ProduceOf(produce, slot.Item, slot.Date));
                double gap = ShortageOf(shortage, item.Item);
                double demand = data.parameter_Demand
                    .FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;

                if (Math.Abs(produced + gap - demand) > Tolerance)
                    throw new InvalidOperationException(
                        $"[C1] {item.Item} 違反 DemandCoverage：{produced:F6} + {gap:F6} != {demand:F6}。");

                // [C4] Sum Use >= MinActiveDays
                double activeDays = slots.Sum(slot => UseOf(use, slot.Item, slot.Date));
                double floor = data.parameter_MinActiveDays
                    .FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;

                if (activeDays < floor - Tolerance)
                    throw new InvalidOperationException(
                        $"[C4] {item.Item} 違反 MinActiveDays：開工 {activeDays:F6} < 下限 {floor:F6}。");

                // [C5] 0 <= Shortage <= MaxShortage
                double cap = data.parameter_MaxShortage
                    .FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;

                if (gap < -Tolerance || gap > cap + Tolerance)
                    throw new InvalidOperationException(
                        $"[C5] {item.Item} 違反 ShortageCap：缺口 {gap:F6} 不在 [0, {cap:F6}]。");
            }

            // [C2] 每天：Sum Produce <= DailyCapacity
            foreach (var date in data.set_Date)
            {
                double dayTotal = data.set_AllowedSlot
                    .Where(row => row.Date == date.Date)
                    .Sum(slot => ProduceOf(produce, slot.Item, slot.Date));
                double capacity = data.parameter_DailyCapacity
                    .FindParameterOrLog(row => row.Date == date.Date, date.Date)?.QTY ?? 0.0;

                if (dayTotal > capacity + Tolerance)
                    throw new InvalidOperationException(
                        $"[C2] {date.Date:yyyy-MM-dd} 違反 DailyCapacity：{dayTotal:F6} > {capacity:F6}。");
            }

            // [C3] 每個 slot：Produce <= BigMProduce * Use
            double bigM = data.parameter_BigMProduce.Single().QTY;
            foreach (var slot in data.set_AllowedSlot)
            {
                double produced = ProduceOf(produce, slot.Item, slot.Date);
                double allowed = bigM * UseOf(use, slot.Item, slot.Date);

                if (produced > allowed + Tolerance)
                    throw new InvalidOperationException(
                        $"[C3] ({slot.Item}, {slot.Date:yyyy-MM-dd}) 違反 ProduceOnlyWhenUsed：{produced:F6} > {allowed:F6}。");
            }

            Logging.Info("[Validate] [C1]-[C5] 全數成立。");
        }

        // 組 key 一律明寫 row 的 property；NEVER 直接內插 row 物件（多維 row 沒有隱式字串轉換，會查不到）
        private static double ProduceOf(Dictionary<string, double> produce, string item, DateTime date) =>
            produce.TryGetValue($"VariableI_Produce@{item}@{date:yyyy_MM_dd}", out double value) ? value : 0.0;

        private static double UseOf(Dictionary<string, double> use, string item, DateTime date) =>
            use.TryGetValue($"VariableB_Use@{item}@{date:yyyy_MM_dd}", out double value) ? value : 0.0;

        private static double ShortageOf(Dictionary<string, double> shortage, string item) =>
            shortage.TryGetValue($"VariableC_Shortage@{item}", out double value) ? value : 0.0;

        public void Print()
        {
            Console.WriteLine("=== 生產計畫 ===");
            foreach (var kv in produce.Where(kv => kv.Value > Tolerance).OrderBy(kv => kv.Key))
                Console.WriteLine($"{kv.Key} = {kv.Value:F0}");

            Console.WriteLine("=== 缺口 ===");
            foreach (var kv in shortage.OrderBy(kv => kv.Key))
                Console.WriteLine($"{kv.Key} = {kv.Value:F4}");
        }
    }
}
