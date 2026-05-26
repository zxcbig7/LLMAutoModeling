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
    ///    + Σ_a       Penalty_4 · VariableX_A[a]
    ///    + Σ_{a,b}   Penalty_5 · VariableX_AB[a][b]
    /// </summary>
    public class ObjectiveFunction
    {
        private OptEngine optEngine;
        private Dataload  dataload;

        public ObjectiveFunction(Dataload dataload, OptEngine engine)
        {
            this.optEngine = engine;
            this.dataload  = dataload;
        }

        public void Build()
        {
            try
            {
                dataload.SetC.ForEach(c =>
                    dataload.SetA.ForEach(a =>
                        dataload.SetB.ForEach(b =>
                            optEngine.AddLHS(dataload.Penalty_1,
                                new VariableB_ABC { A = a, B = b, C = c }))));

                dataload.SetA.ForEach(a =>
                    dataload.SetC.ForEach(c =>
                        optEngine.AddLHS(dataload.Penalty_2,
                            new VariableB_AC { A = a, C = c })));

                dataload.SetA.ForEach(a =>
                    optEngine.AddLHS(dataload.Penalty_3, new VariableB_A { A = a }));

                dataload.SetA.ForEach(a =>
                    optEngine.AddLHS(dataload.Penalty_4, new VariableX_A { A = a }));

                dataload.SetA.ForEach(a =>
                    dataload.SetB.ForEach(b =>
                        optEngine.AddLHS(dataload.Penalty_5,
                            new VariableX_AB { A = a, B = b })));

                optEngine.CreateMinimize();
                // 最大化改用：optEngine.CreateMaximize();

                Logging.Info("目標函數建構完成");
            }
            catch (Exception) { throw; }
        }
    }
}
