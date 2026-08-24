using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>原料每月限制用量（kg/month）；對應 Model.md 的 MonthlySupplyLimit_{RawMaterial}。
    /// 全格資料語意：每種原料都必須有限量值，缺格是資料漏了，不是「限量為 0」；由 CandyBlendingSolution 的資料完整性檢查把關。</summary>
    [OptParam]
    [OptDim<string>("RawMaterial")]
    public sealed partial class Parameter_MonthlySupplyLimit { }
}
