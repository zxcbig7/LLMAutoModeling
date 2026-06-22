using OptimFoundation.Core;

namespace HospitalRostering_Manual.Variable
{
    /// <summary>y[e,d,g]：核心決策，員工 e 在 d 排班別 g（手寫版）。屬性順序＝BuildBVs 傳入順序。</summary>
    public class VariableB_ShiftAssign : VariableBase
    {
        public DateTime Date     { get; set; }
        public string   Employee { get; set; } = string.Empty;
        public string   Group    { get; set; } = string.Empty;
    }
}
