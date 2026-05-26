using OptimFoundation.Cplex;
using OptimFoundation.Core;
using GlassFactory.Set;
using GlassFactory.Variable;

namespace GlassFactory.Constraint
{
    /// <summary>
    /// [C2] 冷卻機產能：Σ_i c[i]·x[i] ≤ C
    /// </summary>
    public class Constraint_Cooling : ConstraintBase
    {
        private OptEngine     optEngine;
        private GlassDataload dataload;
        public new int ConstraintCount = 0;

        public Constraint_Cooling(GlassDataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            dataload.parameter_GlassSpec.ForEach(spec =>
                optEngine.AddLHS(spec.CoolingTime, new VariableX_Production { GlassType = spec.GlassType }));

            optEngine.AddRHS(dataload.CoolingCapacity);
            optEngine.CreateLessEqual($"{ConstraintName}");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
