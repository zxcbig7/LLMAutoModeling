using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>[C2] DailyCapacity [UB] ∀ date ∈ Date：
    /// Σ_{(item,date) ∈ AllowedSlot} Produce_{item,date} ≤ DailyCapacity_date
    /// 每天所有品項的產量合計不得超過當日產能。示範 CreateLessEqual 與 DateTime 維度。</summary>
    public sealed class Constraint_DailyCapacity : ConstraintBase
    {
        private readonly List<Set_Date> dates;
        private readonly List<Set_AllowedSlot> allowedSlots;
        private readonly List<Parameter_DailyCapacity> dailyCapacities;

        public Constraint_DailyCapacity(
            List<Set_Date> dates,
            List<Set_AllowedSlot> allowedSlots,
            List<Parameter_DailyCapacity> dailyCapacities)
        {
            this.dates = dates;
            this.allowedSlots = allowedSlots;
            this.dailyCapacities = dailyCapacities;
        }

        public void Build(OptEngine engine)
        {
            foreach (var date in dates)
            {
                foreach (var slot in allowedSlots.Where(row => row.Date == date.Date))
                    engine.AddLHS(1.0, new VariableI_Produce { Item = slot.Item, Date = slot.Date });

                double capacity = dailyCapacities
                    .FindParameterOrLog(row => row.Date == date.Date, date.Date)?.QTY ?? 0.0;
                engine.AddRHS(capacity);

                // 原始維度值直接交給框架組名（DateTime 會自動格式化成 yyyy_MM_dd）——NEVER 手拼日期或 @ key
                engine.CreateLessEqual(this, date.Date);
            }
        }
    }
}
