using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>對應 Model.md 的 OBJ：
    /// max Σ_{brand} SellingPrice_brand · Produce_brand
    ///   − Σ_{brand} ProcessingCost_brand · Produce_brand
    ///   − Σ_{material} Σ_{brand} MaterialCost_material · Blend_{material,brand}
    /// 最大化月獲利＝售價收入 − 加工費 − 原料成本。</summary>
    public sealed class ObjectiveFunction
    {
        private readonly List<Set_RawMaterial> rawMaterials;
        private readonly List<Set_CandyBrand> candyBrands;
        private readonly List<Parameter_SellingPrice> sellingPrices;
        private readonly List<Parameter_ProcessingCost> processingCosts;
        private readonly List<Parameter_MaterialCost> materialCosts;

        public ObjectiveFunction(
            List<Set_RawMaterial> rawMaterials,
            List<Set_CandyBrand> candyBrands,
            List<Parameter_SellingPrice> sellingPrices,
            List<Parameter_ProcessingCost> processingCosts,
            List<Parameter_MaterialCost> materialCosts)
        {
            this.rawMaterials = rawMaterials;
            this.candyBrands = candyBrands;
            this.sellingPrices = sellingPrices;
            this.processingCosts = processingCosts;
            this.materialCosts = materialCosts;
        }

        public void Build(OptEngine engine)
        {
            // 第一項：+ Σ_{brand} SellingPrice_brand · Produce_brand
            foreach (var brand in candyBrands)
            {
                double sellingPrice = sellingPrices
                    .FindParameterOrLog(row => row.CandyBrand == brand.CandyBrand, brand.CandyBrand)?.QTY ?? 0.0;

                engine.AddLHS(sellingPrice, new VariableC_Produce { CandyBrand = brand.CandyBrand });
            }

            // 第二項：− Σ_{brand} ProcessingCost_brand · Produce_brand
            foreach (var brand in candyBrands)
            {
                double processingCost = processingCosts
                    .FindParameterOrLog(row => row.CandyBrand == brand.CandyBrand, brand.CandyBrand)?.QTY ?? 0.0;

                engine.AddLHS(-processingCost, new VariableC_Produce { CandyBrand = brand.CandyBrand });
            }

            // 第三項：− Σ_{material} Σ_{brand} MaterialCost_material · Blend_{material,brand}
            foreach (var material in rawMaterials)
            {
                double materialCost = materialCosts
                    .FindParameterOrLog(row => row.RawMaterial == material.RawMaterial, material.RawMaterial)?.QTY ?? 0.0;

                foreach (var brand in candyBrands)
                    engine.AddLHS(-materialCost, new VariableC_Blend
                    {
                        RawMaterial = material.RawMaterial,
                        CandyBrand = brand.CandyBrand,
                    });
            }

            engine.CreateMaximize();
        }
    }
}
