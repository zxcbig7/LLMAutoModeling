using OptimFoundation.Core;

namespace ClinicVitamin.Parameter
{
    /// <summary>
    /// 生產一批產品所需各維生素的用量。
    /// </summary>
    public class Parameter_VitaminReq : ParameterBase
    {
        public string Vitamin     { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public double Required    { get; set; }
    }
}
