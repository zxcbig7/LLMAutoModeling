using OptimFoundation.Modeling;

namespace CandyBlending
{
    /// <summary>某原料在某牌號中的含量下限比例（0..1）；對應 Model.md 的 MinContentRatio_{RawMaterial,CandyBrand}。
    /// 稀疏語意：只有題目表格填了下限的組合才有列；沒有列 = 該組合沒有含量下限，不是「下限為 0」的資料缺漏。
    /// Model.md 的 [C3] 就是以本參數的 domain 為迭代範圍。</summary>
    [OptParam]
    [OptDim<string>("RawMaterial")]
    [OptDim<string>("CandyBrand")]
    public sealed partial class Parameter_MinContentRatio { }
}
