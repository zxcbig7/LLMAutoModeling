using OptimFoundation.Modeling;
using ClinicVitamin.Set;

namespace ClinicVitamin.Parameter
{
    /// <summary>生產一批產品所需各維生素的用量。Vitamin / ProductType 為 index-set。
    /// Required 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Vitamin>("Vitamin")]
    [OptDim<Set_ProductType>("ProductType")]
    public partial class Parameter_VitaminReq
    {
        public double Required { get; set; }
    }
}
