using OptimFoundation.Core;

namespace ClinicVitamin.Parameter
{
    /// <summary>
    /// 每種維生素的庫存總量。
    /// </summary>
    public class Parameter_VitaminStock : ParameterBase
    {
        public string Vitamin { get; set; } = string.Empty;
        public double Stock   { get; set; }
    }
}
