using OptimFoundation.Modeling;

namespace HospitalRostering_Generator.Set
{
    /// <summary>班別群組資料列；內容由 Dataload 的既有 primitive Group list 建立。</summary>
    [OptSet]
    [OptDim<string>("Group")]
    public partial class Set_Group { }
}
