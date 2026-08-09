using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>員工資料列；內容由 Dataload 的既有 primitive Employee list 建立。</summary>
    [OptSet]
    [OptDim<string>("Employee")]
    public partial class Set_Employee { }
}
