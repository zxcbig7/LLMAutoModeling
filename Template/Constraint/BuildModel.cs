using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Objective;

namespace Template.Constraint
{
    /// <summary>
    /// 統一呼叫目標函數與所有限制式。
    /// 新增限制式時在此加一行 new Constraint_Xxx(dataload, engine).Build()。
    /// </summary>
    public class BuildModel
    {
        private OptEngine engine;
        private Dataload  dataload;

        public BuildModel(Dataload dataload, OptEngine engine)
        {
            this.engine   = engine;
            this.dataload = dataload;
        }

        public void Build()
        {
            try
            {
                Logging.Info("【建構目標式】");
                new ObjectiveFunction(dataload, engine).Build();

                Logging.Info("【建構限制式】");
                new Constraint_Equality(dataload, engine).Build();
                new Constraint_LessEqual(dataload, engine).Build();
                new Constraint_GreatEqual(dataload, engine).Build();
                new Constraint_Window(dataload, engine).Build();
                new Constraint_VarOnRHS(dataload, engine).Build();
                new Constraint_Range(dataload, engine).Build();    // CreateRange 區間限制式
                new Constraint_Soft(dataload, engine).Build();     // 軟性限制式（penalty 自動入目標）
            }
            catch (Exception) { throw; }
        }
    }
}
