using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Set;
using Template.Variable;

namespace Template.Objective
{
    /// <summary>
    /// 目標函數：最小化所有罰分項的加權總和。
    ///
    /// min  Σ_{a,b,c} Penalty_1 · VariableB_ABC[a][b][c]
    ///    + Σ_{a,c}   Penalty_2 · VariableB_AC[a][c]
    ///    + Σ_a       Penalty_3 · VariableB_A[a]
    ///    + Σ_a       Penalty_4 · VariableC_A[a]
    ///    + Σ_{a,b}   Penalty_5 · VariableC_AB[a][b]
    ///    + Σ_a       Penalty_6 · VariableI_A[a]
    ///
    /// 每個建出來的變數都必須至少出現在目標式或某條限制式一次，否則 CPLEX 不會認得它，
    /// 取解時（GetIVSolution / GetCVSolution）會丟 UnknownObjectException。
    ///
    /// 與 Constraint 同規則：建構子只收 Set row 清單與權重，NEVER 收整包 Dataload。
    /// </summary>
    public class ObjectiveFunction
    {
        private readonly OptEngine engine;
        private readonly IReadOnlyList<Set_A> setA;
        private readonly IReadOnlyList<Set_B> setB;
        private readonly IReadOnlyList<Set_C> setC;
        private readonly double penaltyABC;
        private readonly double penaltyAC;
        private readonly double penaltyA;
        private readonly double penaltyCA;
        private readonly double penaltyCAB;
        private readonly double penaltyIA;

        public ObjectiveFunction(
            OptEngine engine,
            IReadOnlyList<Set_A> setA,
            IReadOnlyList<Set_B> setB,
            IReadOnlyList<Set_C> setC,
            double penaltyABC,
            double penaltyAC,
            double penaltyA,
            double penaltyCA,
            double penaltyCAB,
            double penaltyIA)
        {
            this.engine = engine;
            this.setA = setA;
            this.setB = setB;
            this.setC = setC;
            this.penaltyABC = penaltyABC;
            this.penaltyAC = penaltyAC;
            this.penaltyA = penaltyA;
            this.penaltyCA = penaltyCA;
            this.penaltyCAB = penaltyCAB;
            this.penaltyIA = penaltyIA;
        }

        public void Build()
        {
            foreach (var c in setC)
                foreach (var a in setA)
                    foreach (var b in setB)
                        engine.AddLHS(penaltyABC, new VariableB_ABC { A = a.A, B = b.B, C = c.C });

            foreach (var a in setA)
                foreach (var c in setC)
                    engine.AddLHS(penaltyAC, new VariableB_AC { A = a.A, C = c.C });

            foreach (var a in setA)
                engine.AddLHS(penaltyA, new VariableB_A { A = a.A });

            foreach (var a in setA)
                engine.AddLHS(penaltyCA, new VariableC_A { A = a.A });

            foreach (var a in setA)
                foreach (var b in setB)
                    engine.AddLHS(penaltyCAB, new VariableC_AB { A = a.A, B = b.B });

            foreach (var a in setA)
                engine.AddLHS(penaltyIA, new VariableI_A { A = a.A });

            engine.CreateMinimize();
            // 最大化改用：engine.CreateMaximize();

            Logging.Info("目標函數建構完成");
        }
    }
}
