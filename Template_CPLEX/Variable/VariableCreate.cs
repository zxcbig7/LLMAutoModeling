using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;

namespace Template.Variable
{
    /// <summary>
    /// 統一建立所有決策變數。
    /// BuildBVs → Binary；BuildCVs → Continuous。
    /// 屬性順序對應 set 傳入順序。
    /// </summary>
    public class VariableCreate
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public VariableCreate(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // Binary — 3 sets
                optEngine.BuildBVs<VariableB_ABC>(dataload.SetA, dataload.SetB, dataload.SetC);

                // Binary — 2 sets
                optEngine.BuildBVs<VariableB_AC>(dataload.SetA, dataload.SetC);

                // Binary — 1 set
                optEngine.BuildBVs<VariableB_A>(dataload.SetA);

                // Continuous — 1 set
                optEngine.BuildCVs<VariableX_A>(dataload.SetA);

                // Continuous — 2 sets
                optEngine.BuildCVs<VariableX_AB>(dataload.SetA, dataload.SetB);

                Logging.Info($"Variables created: {optEngine.varCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
