using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>原料單位成本（元/kg）；對應 Model.md 的 MaterialCost_{RawMaterial}。
    /// 全格資料語意：每種原料都必須有成本值，缺格是資料漏了，不是「成本為 0」；由 CandyBlendingSolution 的資料完整性檢查把關。</summary>
    [OptParam]
    [OptDim<string>("RawMaterial")]
    public sealed partial class Parameter_MaterialCost { }
}
