using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Constraint
{
    /// <summary>
    /// [C3]  H = Î£ InputRate(m, HeavyOil) * m
    /// [C4]  L = Î£ InputRate(m, LightOil) * m
    /// [C5]  G = Î£ InputRate(m, Gas)      * m
    /// [C6]  S = Î£ OutputRate(m, SolidFuel)  * m
    /// [C7]  P = Î£ OutputRate(m, Lubricant)  * m
    /// [C8]  R = Î£ OutputRate(m, RocketFuel) * m
    /// </summary>
    public class Constraint_ResourceFlowDef : ConstraintBase
    {
        public new int ConstraintCount = 0;

        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public Constraint_ResourceFlowDef(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            // C3~C5ï¼šæ??—å´è³‡æ?ï¼ˆH, L, Gï¼? Î£ ?„æ??°æ??—é€Ÿç?
            foreach (var res in new[] { "HeavyOil", "LightOil", "Gas" })
            {
                _engine.AddLHS(1.0, new VariableX_Resource { ResourceType = res });
                _dataload.ConsumerMachines(res).ForEach(m =>
                    _engine.AddRHS(_dataload.InputRate(m, res), new VariableI_Machine { MachineType = m }));
                _engine.CreateEqual($"{ConstraintName}@{res}");
                ConstraintCount++;
            }

            // C6~C8ï¼šç??¢å´è³‡æ?ï¼ˆS, P, Rï¼? Î£ ?„æ??°ç??¢é€Ÿç?
            foreach (var res in new[] { "SolidFuel", "Lubricant", "RocketFuel" })
            {
                _engine.AddLHS(1.0, new VariableX_Resource { ResourceType = res });
                _dataload.ProducerMachines(res).ForEach(m =>
                    _engine.AddRHS(_dataload.OutputRate(m, res), new VariableI_Machine { MachineType = m }));
                _engine.CreateEqual($"{ConstraintName}@{res}");
                ConstraintCount++;
            }

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
