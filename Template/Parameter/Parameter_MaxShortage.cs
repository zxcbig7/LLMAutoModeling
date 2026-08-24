using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>各品項可容忍的最大缺口；對應 Model.md 的 MaxShortage_{Item}。全格資料語意。</summary>
    [OptParam]
    [OptDim<string>("Item")]
    public sealed partial class Parameter_MaxShortage { }
}
