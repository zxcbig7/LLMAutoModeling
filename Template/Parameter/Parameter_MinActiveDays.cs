using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>各品項至少要開工的天數；對應 Model.md 的 MinActiveDays_{Item}。全格資料語意。</summary>
    [OptParam]
    [OptDim<string>("Item")]
    public sealed partial class Parameter_MinActiveDays { }
}
