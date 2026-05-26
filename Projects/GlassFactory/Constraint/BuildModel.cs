using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Objective;

namespace GlassFactory.Constraint
{
    public class BuildModel
    {
        private OptEngine     engine;
        private GlassDataload dataload;

        public BuildModel(GlassDataload dataload, OptEngine engine)
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
                new Constraint_Heating(dataload, engine).Build();
                new Constraint_Cooling(dataload, engine).Build();
            }
            catch (Exception) { throw; }
        }
    }
}
