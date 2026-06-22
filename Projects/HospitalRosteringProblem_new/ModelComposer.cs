using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandBox.Data;
using SandBox.Constraints;
using SandBox.VariableClass;

namespace SandBox
{
    /// <summary>
    /// 變數 + 目標式/限制式組裝，供 solve（OptModel）與 experiment（ExperimentRunner）兩模式共用，
    /// 確保兩模式產生完全相同的模型。
    /// </summary>
    public static class ModelComposer
    {
        public static void BuildVariables(OptEngine e, Dataload data)
        {
            e.BuildBVs<VariableB_ShiftAssign>(data.Date, data.Employee, data.Group);
            e.BuildBVs<VariableB_GroupMismatch>(data.Date, data.Employee);
            e.BuildBVs<VariableB_NightToDay>(data.Date, data.Employee);
            e.BuildBVs<VariableB_DoubleOffFlag>(data.Date, data.Employee);
            e.BuildBVs<VariableB_DoubleOffLT2>(data.Employee);
            e.BuildBVs<VariableB_Off1Day>(data.Date, data.Employee);
            e.BuildBVs<VariableB_SixDayWork>(data.Date, data.Employee);
            e.BuildCVs<VariableX_BelowAVG>(data.Employee);
            e.BuildCVs<VariableX_WeekendLT4>(data.Employee);
        }

        public static void BuildModel(OptEngine e, Dataload data)
        {
            new ObjectiveFunction(data, e).Build();

            // 基本限制式
            new Constraint_FullfillDemand(data, e).Build();
            new Constraint_OneGroup(data, e).Build();
            new Constraint_PreAssign(data, e).Build();

            // 進階限制式
            new Constraint_SixDayWork(data, e).Build();
            new Constraint_NightToDay(data, e).Build();
            new Constraint_OffOneDay(data, e).Build();
            new Constraint_CrossGroup(data, e).Build();
            new Constraint_BelowAVG(data, e).Build();
            new Constraint_WeekendLT4(data, e).Build();
            new Constraint_DoubleOffLT2(data, e).Build();
        }
    }
}
