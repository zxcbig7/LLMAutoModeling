using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Objective;

namespace WeeniesBuns.Constraint
{
    public class BuildModel
    {
        private OptEngine           engine;
        private WeeniesBunsDataload dataload;

        public BuildModel(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.engine   = engine;
            this.dataload = dataload;
        }

        public void Build()
        {
            Logging.Info("【建構目標式】");
            new ObjectiveFunction(dataload, engine).Build();

            Logging.Info("【建構限制式】");
            new Constraint_Flour(dataload, engine).Build();
            new Constraint_Pork (dataload, engine).Build();
            new Constraint_Labor(dataload, engine).Build();
        }
    }
}
