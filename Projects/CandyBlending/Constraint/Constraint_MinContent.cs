using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>[C3] MinContent [Proportional] ∀ (material, brand) ∈ dom(MinContentRatio)：
    /// Blend_{material,brand} ≥ MinContentRatio_{material,brand} · Produce_brand
    /// 有含量下限的組合：該原料在該牌號中的投入量，不得低於該牌號產量的指定比例。
    /// 迭代範圍就是 Model.md 宣告的 dom(MinContentRatio)——沒有列 = 該組合沒有下限，不建立限制式。</summary>
    public sealed class Constraint_MinContent : ConstraintBase
    {
        private readonly List<Parameter_MinContentRatio> minContentRatios;

        public Constraint_MinContent(List<Parameter_MinContentRatio> minContentRatios)
        {
            this.minContentRatios = minContentRatios;
        }

        public void Build(OptEngine engine)
        {
            foreach (var ratio in minContentRatios)
            {
                // 左式：Blend_{material,brand}
                engine.AddLHS(1.0, new VariableC_Blend
                {
                    RawMaterial = ratio.RawMaterial,
                    CandyBrand = ratio.CandyBrand,
                });

                // 右式：MinContentRatio_{material,brand} · Produce_brand
                engine.AddRHS(ratio.QTY, new VariableC_Produce { CandyBrand = ratio.CandyBrand });

                engine.CreateGreatEqual(this, ratio.RawMaterial, ratio.CandyBrand);
            }
        }
    }
}
