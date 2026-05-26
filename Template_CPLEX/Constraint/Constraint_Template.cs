using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>限制式空白範本。複製此檔後重命名為 Constraint_Xxx.cs。</summary>
    public class Constraint_Template : ConstraintBase
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public Constraint_Template(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                // dataload.SetA.ForEach(a =>
                // {
                //     dataload.SetC.ForEach(c =>
                //     {
                //         optEngine.AddLHS(1, new VariableB_ABC { A = a, B = "B1", C = c });
                //         optEngine.AddRHS(1);
                //         optEngine.CreateLessEqual($"{ConstraintName}@{a}@{c:yyyy_MM_dd}");
                //         ConstraintCount++;
                //     });
                // });

                Logging.Info($"[{ConstraintName}] {ConstraintCount}");
            }
            catch (Exception) { throw; }
        }
    }
}
