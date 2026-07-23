using System.Collections.Generic;
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using WeeniesBuns.Parameter;
using WeeniesBuns.Variable;

namespace WeeniesBuns.Constraint
{
    /// <summary>
    /// [C3] 人工產能：Σ_{i} LaborPerUnit[i] · x[i]  ≤  LaborCapacity (12000 min)
    /// </summary>
    public class Constraint_Labor : ConstraintBase
    {
        private readonly List<Parameter_ProductSpec> _spec;
        private readonly double _laborCap;
        private readonly OptEngine _engine;
        public new int ConstraintCount = 0;

        public Constraint_Labor(List<Parameter_ProductSpec> spec, double laborCap, OptEngine engine)
        {
            _spec = spec;
            _laborCap = laborCap;
            _engine = engine;
        }

        public void Build()
        {
            foreach (var spec in _spec)
                _engine.AddLHS(spec.LaborPerUnit, new VariableX_Production { ProductType = spec.ProductType });

            _engine.AddRHS(_laborCap);
            _engine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
