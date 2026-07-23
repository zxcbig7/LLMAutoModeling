using System.Collections.Generic;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Parameter;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Objective
{
    /// <summary>
    /// 目標函數：最大化總利潤
    /// max  Σ_{i ∈ PRODUCT}  Profit[i] · x[i]
    /// </summary>
    public class ObjectiveFunction
    {
        private readonly List<Parameter_ProductSpec> _spec;
        private readonly OptEngine _engine;

        public ObjectiveFunction(List<Parameter_ProductSpec> spec, OptEngine engine)
        {
            _spec = spec;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var spec in _spec)
                _engine.AddLHS(spec.Profit, new VariableX_Production { ProductType = spec.ProductType });

            _engine.CreateMaximize();
            Logging.Info("目標函數：max Σ Profit[i]·x[i]");
        }
    }
}
