using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>每日總產能；對應 Model.md 的 DailyCapacity_{Date}。
    /// 全格資料語意：每一天都必須有產能值，缺格是資料漏了、不是「產能為 0」；由 TemplateSolution.ValidateData 把關。</summary>
    [OptParam]
    [OptDim<DateTime>("Date")]
    public sealed partial class Parameter_DailyCapacity { }
}
