using OptimFoundation.Modeling;

namespace HospitalRostering_Manual.Set
{
    /// <summary>員工（string set）。Parameter_PreAssign / Parameter_CrossGroup / Parameter_BackupGroup 的
    /// Employee index-set，供框架資料驗證用（成員與 Dataload.Employee 一致，見 ctor 的 EMPLOYEE.LoadFrom）。</summary>
    [OptSet<string>]
    public partial class Set_Employee { }
}
