using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Set;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Objective
{
    /// <summary>
    /// 目標函數：最大化總利潤
    /// max  Σ_{i ∈ PRODUCT}  Profit[i] · x[i]
    /// </summary>
    public class ObjectiveFunction
    {
        private OptEngine           optEngine;
        private WeeniesBunsDataload dataload;

        public ObjectiveFunction(WeeniesBunsDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_ProductSpec.ForEach(spec =>
                optEngine.AddLHS(spec.Profit, new VariableX_Production { ProductType = spec.ProductType }));

            optEngine.CreateMaximize();
            Logging.Info("目標函數：max Σ Profit[i]·x[i]");
        }
    }
}
