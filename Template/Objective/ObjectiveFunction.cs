using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Template
{
    /// <summary>對應 Model.md 的 OBJ：min Σ_{item} ShortagePenalty · Shortage_item —— 最小化總缺口懲罰。
    /// 建構子只收這條式子實際用到的東西（Set row 清單 + scalar），NEVER 收整包 Dataload。</summary>
    public sealed class ObjectiveFunction
    {
        private readonly List<Set_Item> items;
        private readonly double shortagePenalty;

        public ObjectiveFunction(List<Set_Item> items, double shortagePenalty)
        {
            this.items = items;
            this.shortagePenalty = shortagePenalty;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
                engine.AddLHS(shortagePenalty, new VariableC_Shortage { Item = item.Item });

            engine.CreateMinimize(); // 最大化改用 engine.CreateMaximize()
        }
    }
}
