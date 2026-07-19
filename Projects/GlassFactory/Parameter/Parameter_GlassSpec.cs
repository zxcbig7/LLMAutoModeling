using OptimFoundation.Modeling;
using GlassFactory.Set;

namespace GlassFactory.Parameter
{
    /// <summary>
    /// 每種玻璃的規格參數。GlassType 為 index-set（由 [OptParam<Set_GlassType>] 生成），
    /// 三個值欄位非單一 QTY，故 HasValue = false，改手寫額外欄位。
    /// </summary>
    [OptParam<Set_GlassType>(HasValue = false)]
    public partial class Parameter_GlassSpec
    {
        public double HeatingTime { get; set; }
        public double CoolingTime { get; set; }
        public double Profit { get; set; }
    }
}
