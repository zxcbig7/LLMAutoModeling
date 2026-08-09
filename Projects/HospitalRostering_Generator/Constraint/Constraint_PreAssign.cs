using OptimFoundation.Cplex;
using OptimFoundation.Core;
using HospitalRostering_Generator.Set;
using HospitalRostering_Generator.Variable;

namespace HospitalRostering_Generator.Constraint
{
    /// <summary>C3 預排班固定：y[e,d,g] = 1，∀(e,d,g)∈PA。（CreateEqual）</summary>
    public class Constraint_PreAssign : ConstraintBase
    {
        private readonly OptEngine optEngine;
        private readonly Dataload  dataload;

        public Constraint_PreAssign(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                foreach (var p in dataload.PRE_ASSIGN)
                {
                    optEngine.AddLHS(1, new VariableB_ShiftAssign { Date = p.Date, Employee = p.Employee, Group = p.Group });
                    optEngine.AddRHS(1);
                    optEngine.CreateEqual(this, p.Date, p.Employee, p.Group);
                }
            }
            catch (Exception) { throw; }
        }
    }
}
