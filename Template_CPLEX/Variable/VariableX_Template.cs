using OptimModeling;

namespace Template.Variable
{
    /// <summary>
    /// 變數空白範本：展示所有支援的 Set 型別（string / double / int / DateTime）。
    /// set 字串順序對應 BuildBVs / BuildCVs 傳入 set 的順序；body 由 AutoSetsGenerator 生成。
    /// 預設用此宣告式；若需手寫見 HospitalRostering_Manual。
    /// </summary>
    [OptVar(VarType.Continuous, "Set1", "Set2:double", "Set3:int", "Set4:DateTime")]
    public partial class VariableX_Template { }
}
