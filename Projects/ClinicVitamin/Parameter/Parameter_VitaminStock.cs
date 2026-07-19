using OptimFoundation.Modeling;
using ClinicVitamin.Set;

namespace ClinicVitamin.Parameter
{
    /// <summary>每種維生素的庫存總量。Vitamin 為 index-set（Set_Vitamin），
    /// Stock 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_Vitamin>("Vitamin")]
    public partial class Parameter_VitaminStock
    {
        public double Stock { get; set; }
    }
}
