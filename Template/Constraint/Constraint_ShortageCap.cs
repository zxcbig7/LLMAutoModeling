using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>[C5] ShortageCap [Range] ∀ item ∈ Item：
    /// 0 ≤ Shortage_item ≤ MaxShortage_item
    /// 缺口不得超過可容忍上限。示範 CreateRange(lb, ub, owner, dims)——它**只吃 LHS**，
    /// 上下界是兩個純量引數，所以 RHS 池不參與。</summary>
    public sealed class Constraint_ShortageCap : ConstraintBase
    {
        private readonly List<Set_Item> items;
        private readonly List<Parameter_MaxShortage> maxShortages;

        public Constraint_ShortageCap(List<Set_Item> items, List<Parameter_MaxShortage> maxShortages)
        {
            this.items = items;
            this.maxShortages = maxShortages;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
            {
                engine.AddLHS(1.0, new VariableC_Shortage { Item = item.Item });

                double upperBound = maxShortages
                    .FindParameterOrLog(row => row.Item == item.Item, item.Item)?.QTY ?? 0.0;

                engine.CreateRange(0.0, upperBound, this, item.Item);
            }
        }
    }
}
