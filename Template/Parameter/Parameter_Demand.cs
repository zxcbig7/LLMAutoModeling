using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>各品項的期間總需求量；對應 Model.md 的 Demand_{Item}。
    /// 全格資料語意：每個品項都必須有需求值，缺格是資料漏了、不是「需求為 0」；由 TemplateSolution.ValidateData 把關。</summary>
    [OptParam]
    [OptDim<string>("Item")]
    public sealed partial class Parameter_Demand { }
}
