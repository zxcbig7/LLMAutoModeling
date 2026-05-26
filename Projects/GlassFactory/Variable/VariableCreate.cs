using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;

namespace GlassFactory.Variable
{
    public class VariableCreate
    {
        private OptEngine      optEngine;
        private GlassDataload  dataload;

        public VariableCreate(GlassDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                optEngine.BuildCVs<VariableX_Production>(dataload.GlassTypes);
                Logging.Info($"Variables created: {optEngine.varCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
