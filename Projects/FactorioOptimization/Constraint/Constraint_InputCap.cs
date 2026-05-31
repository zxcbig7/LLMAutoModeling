using OptimFoundation.Cplex;
using OptimFoundation.Core;
using FactorioOptimization.Data;
using FactorioOptimization.Variable;

namespace FactorioOptimization.Constraint
{
    /// <summary>
    /// [C1] ?³æ²¹ä¸Šé?ï¼šInputRate(Refinery, CrudeOil) * x ??CrudeOilCap
    /// [C2] æ½¤æ??‘å?å·¥å??ºå??°æ•¸ï¼šc1 = FixedLubePlants
    /// </summary>
    public class Constraint_InputCap : ConstraintBase
    {
        public new int ConstraintCount = 0;

        private readonly FactorioOptimizationDataload _dataload;
        private readonly OptEngine                    _engine;

        public Constraint_InputCap(FactorioOptimizationDataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine   = engine;
        }

        public void Build()
        {
            // C1: InputRate(Refinery, CrudeOil) * x ??CrudeOilCap
            _engine.AddLHS(_dataload.InputRate("Refinery", "CrudeOil"), new VariableI_Machine { MachineType = "Refinery" });
            _engine.AddRHS(_dataload.CrudeOilCap);
            _engine.CreateLessEqual($"{ConstraintName}@CrudeOil");
            ConstraintCount++;

            // C2: c1 = FixedLubePlants
            _engine.AddLHS(1.0, new VariableI_Machine { MachineType = "ChemPlant_Lube" });
            _engine.AddRHS(_dataload.FixedLubePlants);
            _engine.CreateEqual($"{ConstraintName}@FixedLube");
            ConstraintCount++;

            Logging.Info($"[{ConstraintName}] {ConstraintCount}");
        }
    }
}
