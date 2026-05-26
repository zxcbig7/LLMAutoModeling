using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Variable;

namespace GlassFactory.Constraint
{
    /// <summary>
    /// [C1] 加熱機產能：Σ_i h[i]·x[i] ≤ H
    /// </summary>
    public class Constraint_Heating : ConstraintBase
    {
        private OptEngine     optEngine;
        private GlassDataload dataload;
        public new int ConstraintCount = 0;

        public Constraint_Heating(GlassDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_GlassSpec.ForEach(spec =>
                optEngine.AddLHS(spec.HeatingTime, new VariableX_Production { GlassType = spec.GlassType }));

            optEngine.AddRHS(dataload.HeatingCapacity);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
