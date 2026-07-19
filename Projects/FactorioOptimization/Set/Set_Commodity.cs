using OptimFoundation.Modeling;

namespace FactorioOptimization.Set
{
    /// <summary>物料／資源種類（string set）。Parameter_RecipeInput / Parameter_RecipeOutput 的
    /// ResourceName index-set，供框架資料驗證用。範圍刻意比 <see cref="FactorioOptimizationDataload.ResourceTypes"/>
    /// （6 項、只含流量變數化的中間／終端產物）更廣：需額外涵蓋 CrudeOil / Water 這兩個「只當配方輸入、
    /// 不建 VariableX_Resource」的原料，否則 Parameter_RecipeInput 的 dangling 檢查會誤報。</summary>
    [OptSet]
    public partial class Set_Commodity { }
}
