using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>[C4] MaxContent [Proportional] ∀ (material, brand) ∈ dom(MaxContentRatio)：
    /// Blend_{material,brand} ≤ MaxContentRatio_{material,brand} · Produce_brand
    /// 有含量上限的組合：該原料在該牌號中的投入量，不得高於該牌號產量的指定比例。
    /// 迭代範圍就是 Model.md 宣告的 dom(MaxContentRatio)——沒有列 = 該組合沒有上限，不建立限制式。</summary>
    public sealed class Constraint_MaxContent : ConstraintBase
    {
        private readonly List<Parameter_MaxContentRatio> maxContentRatios;

        public Constraint_MaxContent(List<Parameter_MaxContentRatio> maxContentRatios)
        {
            this.maxContentRatios = maxContentRatios;
        }

        public void Build(OptEngine engine)
        {
            foreach (var ratio in maxContentRatios)
            {
                // 左式：Blend_{material,brand}
                engine.AddLHS(1.0, new VariableC_Blend
                {
                    RawMaterial = ratio.RawMaterial,
                    CandyBrand = ratio.CandyBrand,
                });

                // 右式：MaxContentRatio_{material,brand} · Produce_brand
                engine.AddRHS(ratio.QTY, new VariableC_Produce { CandyBrand = ratio.CandyBrand });

                engine.CreateLessEqual(this, ratio.RawMaterial, ratio.CandyBrand);
            }
        }
    }
}
