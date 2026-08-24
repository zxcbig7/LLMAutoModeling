using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>[C1] MaterialAvailability [UB] ∀ material ∈ RawMaterial：
    /// Σ_{brand ∈ CandyBrand} Blend_{material,brand} ≤ MonthlySupplyLimit_material
    /// 每種原料本月投入到所有牌號的總量，不得超過該原料的每月限制用量。</summary>
    public sealed class Constraint_MaterialAvailability : ConstraintBase
    {
        private readonly List<Set_RawMaterial> rawMaterials;
        private readonly List<Set_CandyBrand> candyBrands;
        private readonly List<Parameter_MonthlySupplyLimit> monthlySupplyLimits;

        public Constraint_MaterialAvailability(
            List<Set_RawMaterial> rawMaterials,
            List<Set_CandyBrand> candyBrands,
            List<Parameter_MonthlySupplyLimit> monthlySupplyLimits)
        {
            this.rawMaterials = rawMaterials;
            this.candyBrands = candyBrands;
            this.monthlySupplyLimits = monthlySupplyLimits;
        }

        public void Build(OptEngine engine)
        {
            foreach (var material in rawMaterials)
            {
                foreach (var brand in candyBrands)
                    engine.AddLHS(1.0, new VariableC_Blend
                    {
                        RawMaterial = material.RawMaterial,
                        CandyBrand = brand.CandyBrand,
                    });

                double supplyLimit = monthlySupplyLimits
                    .FindParameterOrLog(row => row.RawMaterial == material.RawMaterial, material.RawMaterial)?.QTY ?? 0.0;
                engine.AddRHS(supplyLimit);

                engine.CreateLessEqual(this, material.RawMaterial);
            }
        }
    }
}
