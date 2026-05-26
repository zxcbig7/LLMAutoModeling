using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Variable;

namespace GlassFactory.Objective
{
    /// <summary>
    /// 目標函數：最大化總利潤
    /// max  Σ_i  Profit[i] · x[i]
    /// </summary>
    public class ObjectiveFunction
    {
        private OptEngine     optEngine;
        private GlassDataload dataload;

        public ObjectiveFunction(GlassDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_GlassSpec.ForEach(spec =>
                optEngine.AddLHS(spec.Profit, new VariableX_Production { GlassType = spec.GlassType }));

            optEngine.CreateMaximize();
            Logging.Info("目標函數：max Σ Profit[i]·x[i]");
        }
    }
}
