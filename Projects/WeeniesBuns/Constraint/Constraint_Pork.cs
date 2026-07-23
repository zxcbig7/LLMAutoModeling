using System.Collections.Generic;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Parameter;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C2] 豬肉供應：Σ_{i} PorkPerUnit[i] · x[i]  ≤  PorkCapacity (800 lbs)
    /// </summary>
    public class Constraint_Pork : ConstraintBase
    {
        private readonly List<Parameter_ProductSpec> _spec;
        private readonly double _porkCap;
        private readonly OptEngine _engine;
        public new int ConstraintCount = 0;

        public Constraint_Pork(List<Parameter_ProductSpec> spec, double porkCap, OptEngine engine)
        {
            _spec = spec;
            _porkCap = porkCap;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var spec in _spec)
                _engine.AddLHS(spec.PorkPerUnit, new VariableX_Production { ProductType = spec.ProductType });

            _engine.AddRHS(_porkCap);
            _engine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
