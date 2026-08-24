using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>[C4] MinActiveDays [LB] ∀ item ∈ Item：
    /// Σ_{(item,date) ∈ AllowedSlot} Use_{item,date} ≥ MinActiveDays_item
    /// 每個品項至少要開工的天數。示範 CreateGreatEqual。</summary>
    public sealed class Constraint_MinActiveDays : ConstraintBase
    {
        private readonly List<Set_Item> items;
        private readonly List<Set_AllowedSlot> allowedSlots;
        private readonly List<Parameter_MinActiveDays> minActiveDays;

        public Constraint_MinActiveDays(
            List<Set_Item> items,
            List<Set_AllowedSlot> allowedSlots,
            List<Parameter_MinActiveDays> minActiveDays)
        {
            this.items = items;
            this.allowedSlots = allowedSlots;
            this.minActiveDays = minActiveDays;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
            {
                foreach (var slot in allowedSlots.Where(row => row.Item == item.Item))
                    engine.AddLHS(1.0, new VariableB_Use { Item = slot.Item, Date = slot.Date });

                double floor = minActiveDays
                    .FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;
                engine.AddRHS(floor);

                engine.CreateGreatEqual(this, item.Item);
            }
        }
    }
}
