using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>[C3] ProduceOnlyWhenUsed [BigM] ∀ (item, date) ∈ AllowedSlot：
    /// Produce_{item,date} ≤ BigMProduce · Use_{item,date}
    /// 沒開工就不能生產。示範三件事：多維 Set 直接當迭代 domain、**變數出現在右式**（AddRHS(係數, 變數)，
    /// 框架自動移項），以及 Big-M 一律由具名 Parameter 從 CSV 進來（取值＝單日最大產能，NEVER magic number）。</summary>
    public sealed class Constraint_ProduceOnlyWhenUsed : ConstraintBase
    {
        private readonly List<Set_AllowedSlot> allowedSlots;
        private readonly double bigMProduce;

        public Constraint_ProduceOnlyWhenUsed(List<Set_AllowedSlot> allowedSlots, double bigMProduce)
        {
            this.allowedSlots = allowedSlots;
            this.bigMProduce = bigMProduce;
        }

        public void Build(OptEngine engine)
        {
            foreach (var slot in allowedSlots)
            {
                engine.AddLHS(1.0, new VariableI_Produce { Item = slot.Item, Date = slot.Date });
                engine.AddRHS(bigMProduce, new VariableB_Use { Item = slot.Item, Date = slot.Date });

                engine.CreateLessEqual(this, slot.Item, slot.Date);
            }
        }
    }
}
