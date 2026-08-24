using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>[C2] BrandMassBalance [Balance] ∀ brand ∈ CandyBrand：
    /// Produce_brand = Σ_{material ∈ RawMaterial} Blend_{material,brand}
    /// 每個牌號的產量等於投入該牌號的各原料重量之和（加工無質量損耗）；這條同時定義了含量百分比的分母。</summary>
    public sealed class Constraint_BrandMassBalance : ConstraintBase
    {
        private readonly List<Set_RawMaterial> rawMaterials;
        private readonly List<Set_CandyBrand> candyBrands;

        public Constraint_BrandMassBalance(
            List<Set_RawMaterial> rawMaterials,
            List<Set_CandyBrand> candyBrands)
        {
            this.rawMaterials = rawMaterials;
            this.candyBrands = candyBrands;
        }

        public void Build(OptEngine engine)
        {
            foreach (var brand in candyBrands)
            {
                // 左式：Produce_brand
                engine.AddLHS(1.0, new VariableC_Produce { CandyBrand = brand.CandyBrand });

                // 右式：Σ_{material} Blend_{material,brand}
                foreach (var material in rawMaterials)
                    engine.AddRHS(1.0, new VariableC_Blend
                    {
                        RawMaterial = material.RawMaterial,
                        CandyBrand = brand.CandyBrand,
                    });

                engine.CreateEqual(this, brand.CandyBrand);
            }
        }
    }
}
