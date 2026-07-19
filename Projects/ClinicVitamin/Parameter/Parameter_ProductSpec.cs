using OptimFoundation.Modeling;
using ClinicVitamin.Set;

namespace ClinicVitamin.Parameter
{
    /// <summary>每種產品的供應人數規格。ProductType 為 index-set（Set_ProductType），
    /// PeopleSupply 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_ProductType>("ProductType")]
    public partial class Parameter_ProductSpec
    {
        public double PeopleSupply { get; set; }
    }
}
