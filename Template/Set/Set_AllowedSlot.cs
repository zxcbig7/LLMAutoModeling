using OptimFoundation.Modeling;

namespace Template
{
    /// <summary>允許生產的 (品項, 日期) 組合；對應 Model.md 的 SET AllowedSlot。
    /// 這是**多維 Set**：成員本身就是 tuple，語意是「這些組合存在」而不是「這些組合值多少」。
    /// 它同時是 Produce / Use 兩支變數的 domain——沒列出來的組合連變數都不會建立。</summary>
    [OptSet]
    [OptDim<string>("Item")]
    [OptDim<DateTime>("Date")]
    public sealed partial class Set_AllowedSlot { }
}
