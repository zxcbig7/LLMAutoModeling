using OptimFoundation.Core;

namespace HospitalRostering_Manual.Parameter
{
    /// <summary>員工 Backup 班別（純 key，無 QTY；手寫版）。</summary>
    public class Parameter_BackupGroup : ParameterBase
    {
        public string Employee { get; set; } = string.Empty;
        public string Group    { get; set; } = string.Empty;
    }
}
