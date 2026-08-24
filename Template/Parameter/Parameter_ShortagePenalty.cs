using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>缺一單位的懲罰成本；對應 Model.md 的 ShortagePenalty。
    /// 零維 scalar：CSV 只有 QTY 欄且 MUST 恰好一列；取值一律 .Single().QTY（NEVER First / FirstOrDefault）。</summary>
    [OptParam]
    public sealed partial class Parameter_ShortagePenalty { }
}
