using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>[C1] DemandCoverage [Balance] ∀ item ∈ Item：
    /// Σ_{(item,date) ∈ AllowedSlot} Produce_{item,date} + Shortage_item = Demand_item
    /// 每個品項的總產量加上缺口，等於它的需求。示範 CreateEqual 與「同一條式子裡兩種變數」。</summary>
    public sealed class Constraint_DemandCoverage : ConstraintBase
    {
        private readonly List<Set_Item> items;
        private readonly List<Set_AllowedSlot> allowedSlots;
        private readonly List<Parameter_Demand> demands;

        public Constraint_DemandCoverage(
            List<Set_Item> items,
            List<Set_AllowedSlot> allowedSlots,
            List<Parameter_Demand> demands)
        {
            this.items = items;
            this.allowedSlots = allowedSlots;
            this.demands = demands;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
            {
                // 子集合只屬於這一條式子 → 對既有 row list 用 Where 篩，不另建 Set（api-guide §2.4.1）
                foreach (var slot in allowedSlots.Where(row => row.Item == item.Item))
                    engine.AddLHS(1.0, new VariableI_Produce { Item = slot.Item, Date = slot.Date });

                engine.AddLHS(1.0, new VariableC_Shortage { Item = item.Item });

                double demand = demands.FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;
                engine.AddRHS(demand);

                engine.CreateEqual(this, item.Item);
            }
        }
    }
}
