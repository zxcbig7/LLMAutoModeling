using OptimFoundation.Cplex;
using OptimFoundation.Core;
using ClinicVitamin.Objective;
using ClinicVitamin.Set;

namespace ClinicVitamin.Constraint
{
    public class BuildModel
    {
        private readonly ClinicDataload dataload;
        private readonly OptEngine optEngine;

        public BuildModel(ClinicDataload dataload, OptEngine optEngine)
        {
            this.dataload = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            new ObjectiveFunction(dataload, optEngine).Build();

            new Constraint_VitaminCapacity(dataload, optEngine).Build(); // [C1] ≤
            new Constraint_PillsGTShots(dataload, optEngine).Build(); // [C2] ≥
            new Constraint_MaxShots(dataload, optEngine).Build(); // [C3] ≤
        }
    }
}
