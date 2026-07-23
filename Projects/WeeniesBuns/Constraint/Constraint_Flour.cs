using System.Collections.Generic;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Parameter;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C1] 麵粉產能：Σ_{i} FlourPerUnit[i] · x[i]  ≤  FlourCapacity (200 lbs)
    /// </summary>
    public class Constraint_Flour : ConstraintBase
    {
        private readonly List<Parameter_ProductSpec> _spec;
        private readonly double _flourCap;
        private readonly OptEngine _engine;
        public new int ConstraintCount = 0;

        public Constraint_Flour(List<Parameter_ProductSpec> spec, double flourCap, OptEngine engine)
        {
            _spec = spec;
            _flourCap = flourCap;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var spec in _spec)
                _engine.AddLHS(spec.FlourPerUnit, new VariableX_Production { ProductType = spec.ProductType });

            _engine.AddRHS(_flourCap);
            _engine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
