using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Constraint
{
    /// <summary>
    /// 限制式空白範本。複製此檔後重命名為 Constraint_Xxx.cs。
    ///
    /// 建構子只列本條式子真正用到的積木、參數與界限值——NEVER 收整包 Dataload，
    /// NEVER 在 Build() 內寫死任何數字（界限值一律從建構子傳進來）。
    /// </summary>
    public class Constraint_Template : ConstraintBase
    {
        private readonly OptEngine engine;
        private readonly Set_A setA;
        private readonly Set_C setC;
        private readonly double upperBound;

        public Constraint_Template(OptEngine engine, Set_A setA, Set_C setC, double upperBound)
        {
            this.engine = engine;
            this.setA = setA;
            this.setC = setC;
            this.upperBound = upperBound;
        }

        public void Build()
        {
            // foreach (var a in setA)
            //     foreach (var c in setC)
            //     {
            //         engine.AddLHS(1, new VariableB_ABC { A = a, B = "B1", C = c });
            //         engine.AddRHS(upperBound);
            //         engine.CreateLessEqual($"{ConstraintName}@{a}@{c:yyyy_MM_dd}");
            //         ConstraintCount++;
            //     }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
