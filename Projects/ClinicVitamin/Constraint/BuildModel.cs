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
            this.dataload  = dataload;
            this.optEngine = optEngine;
        }

        public void Build()
        {
            new ObjectiveFunction(dataload, optEngine).Build();

            var c1 = new Constraint_VitaminCapacity(dataload, optEngine); c1.Build();
            var c2 = new Constraint_PillsGTShots(dataload, optEngine);    c2.Build();
            var c3 = new Constraint_MaxShots(dataload, optEngine);         c3.Build();

            Logging.Info($"[BuildModel] 限制式總數：{c1.ConstraintCount + c2.ConstraintCount + c3.ConstraintCount}");
        }
    }
}
