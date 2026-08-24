using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>牌號單位加工費（元/kg）；對應 Model.md 的 ProcessingCost_{CandyBrand}。
    /// 全格資料語意：每個牌號都必須有加工費，缺格是資料漏了，不是「加工費為 0」；由 CandyBlendingSolution 的資料完整性檢查把關。</summary>
    [OptParam]
    [OptDim<string>("CandyBrand")]
    public sealed partial class Parameter_ProcessingCost { }
}
