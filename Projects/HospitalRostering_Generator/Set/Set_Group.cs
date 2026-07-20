using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>班別群組（string set）。Parameter_ShiftDemand / Parameter_PreAssign / Parameter_NightToDay
    /// （PreGroup 與 Group 兩個維度皆指向本 Set）/ Parameter_CrossGroup / Parameter_BackupGroup 的
    /// Group index-set，供框架資料驗證用（成員與 Dataload.Group 一致，見 ctor 的 GROUP.LoadFrom）。</summary>
    [OptSet<string>]
    public partial class Set_Group { }
}
