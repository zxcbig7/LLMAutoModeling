using OptimFoundation.Cplex;
using OptimFoundation.Core;
using SandBox.Data;

namespace SandBox.Constraints
{
    /// <summary>
    /// 通用模型建構入口：依序呼叫目標函數與所有限制式。
    /// 新增限制式時，在此加一行 new Constraint_Xxx(dataload, engine).Build()。
    /// </summary>
    public class GenericBuildModel
    {
        private OptEngine      engine;
        private GenericDataload dataload;

        public GenericBuildModel(GenericDataload dataload, OptEngine engine)
        {
            this.engine   = engine;
            this.dataload = dataload;
        }

        public void Build()
        {
            try
            {
                Logging.Info("【建構目標式】");
                new GenericObjectiveFunction(dataload, engine).Build();

                Logging.Info("【建構限制式】");
                new GenericConstraint_AllSyntax(dataload, engine).Build();

                // 依需求新增：
                // new Constraint_Xxx(dataload, engine).Build();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
