using OptimFoundation.Modeling;
using SandwichProduction.Set;

namespace SandwichProduction.Parameter
{
    /// <summary>每種三明治的利潤規格。SandwichType 為 index-set（Set_SandwichType），
    /// Profit 非單一 QTY，故 HasValue = false，改手寫額外欄位。</summary>
    [OptParam(HasValue = false)]
    [OptDim<Set_SandwichType>("SandwichType")]
    public partial class Parameter_SandwichSpec
    {
        public double Profit { get; set; }
    }
}
