using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>排程日期（DateTime set）。Parameter_ShiftDemand / Parameter_PreAssign 的 Date index-set，
    /// 供框架資料驗證用（成員與 Dataload.Date 一致，見 ctor 的 DATE.LoadFrom）。</summary>
    [OptSet<System.DateTime>]
    public partial class Set_Date { }
}
