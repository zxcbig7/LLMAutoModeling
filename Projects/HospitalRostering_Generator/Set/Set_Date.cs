using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>排程日期資料列；內容由 Dataload 的既有 primitive Date list 建立。</summary>
    [OptSet]
    [OptDim<System.DateTime>("Date")]
    public partial class Set_Date { }
}
