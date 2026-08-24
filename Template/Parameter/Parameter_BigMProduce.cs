using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>Produce 的 Big-M 上界；對應 Model.md 的 BigMProduce。
    /// 取值＝該式最緊的合法上界（= 單日最大產能），由題目數據推導而來，NEVER magic number。
    /// 零維 scalar，示範「Big-M 也是資料、必須從 CSV 進來」。</summary>
    [OptParam]
    public sealed partial class Parameter_BigMProduce { }
}
