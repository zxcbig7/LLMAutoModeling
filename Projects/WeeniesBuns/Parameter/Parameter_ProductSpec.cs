using OptimFoundation.Modeling;
using WeeniesBuns.Set;

namespace WeeniesBuns.Parameter
{
    /// <summary>
    /// 產品規格。ProductType 為 index-set（由 [OptParam<Set_ProductType>] 生成），
    /// 四個值欄位非單一 QTY，故 HasValue = false，改手寫額外欄位。
    /// </summary>
    [OptParam<Set_ProductType>(HasValue = false)]
    public partial class Parameter_ProductSpec
    {
        public double FlourPerUnit { get; set; }
        public double PorkPerUnit { get; set; }
        public double LaborPerUnit { get; set; }
        public double Profit { get; set; }
    }
}
