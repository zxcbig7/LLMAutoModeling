using OptimFoundation.Modeling;

namespace Template.Set
{
    // Set 積木：[OptSet<T>] 宣告元素型別，命名對齊現有維度語意（A/B/C）。
    [OptSet<string>]
    public partial class Set_A { }

    [OptSet<string>]
    public partial class Set_B { }

    [OptSet<DateTime>]
    public partial class Set_C { }

    // 以下兩顆僅供 VariableX_Template / Parameter_Template 空白範本展示 double / int 元素型別，非實際模型維度。
    [OptSet<double>]
    public partial class Set_D { }

    [OptSet<int>]
    public partial class Set_E { }
}
