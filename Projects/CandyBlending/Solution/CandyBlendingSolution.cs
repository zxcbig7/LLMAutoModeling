using OptimFoundation.Core;
using OptimFoundation.Core.IO;
using OptimFoundation.Cplex;

namespace CandyBlending
{
    /// <summary>兩個驗證入口：建模前的 <see cref="ValidateData"/> 資料驗收，與求解後的 <see cref="ReadAndValidate"/> 解驗證。</summary>
    public sealed class CandyBlendingSolution
    {
        private const double Tolerance = 1e-6;

        private readonly Dictionary<string, double> blend;
        private readonly Dictionary<string, double> produce;

        private CandyBlendingSolution(Dictionary<string, double> blend, Dictionary<string, double> produce)
        {
            this.blend = blend;
            this.produce = produce;
        }

        /// <summary>建模前資料驗收（Program.cs 模型段呼叫，緊接 OptData.Load 之後）。
        /// 全格語意的四個 Parameter 缺格代表資料漏了、不是「值為 0」，MUST 當場丟例外；
        /// MinContentRatio / MaxContentRatio 屬稀疏語意（沒有列 = 沒有這條限制），不在此檢查。</summary>
        public static void ValidateData(Dataload data)
        {
            foreach (var material in data.set_RawMaterial)
            {
                if (!data.parameter_MaterialCost.Any(row => row.RawMaterial == material.RawMaterial))
                    throw new InvalidOperationException($"[Data] Parameter_MaterialCost 缺少 {material.RawMaterial}。");

                if (!data.parameter_MonthlySupplyLimit.Any(row => row.RawMaterial == material.RawMaterial))
                    throw new InvalidOperationException($"[Data] Parameter_MonthlySupplyLimit 缺少 {material.RawMaterial}。");
            }

            foreach (var brand in data.set_CandyBrand)
            {
                if (!data.parameter_SellingPrice.Any(row => row.CandyBrand == brand.CandyBrand))
                    throw new InvalidOperationException($"[Data] Parameter_SellingPrice 缺少 {brand.CandyBrand}。");

                if (!data.parameter_ProcessingCost.Any(row => row.CandyBrand == brand.CandyBrand))
                    throw new InvalidOperationException($"[Data] Parameter_ProcessingCost 缺少 {brand.CandyBrand}。");
            }

            Logging.Info("[ValidateData] 全格 Parameter 覆蓋完整。");
        }

        public static CandyBlendingSolution ReadAndValidate(OptEngine engine, Dataload data)
        {
            Logging.Info($"Status={engine.Status} Obj={engine.GetObjectiveValue():F4} " +
                         $"BestBound={engine.LastMetrics.BestBound:F4} MIPGap={engine.LastMetrics.MipGap:P2}");

            var blend = engine.GetSetVarValues<VariableC_Blend>();
            var produce = engine.GetSetVarValues<VariableC_Produce>();

            ValidateRules(blend, produce, data);

            CsvCtrl.WriteSolution<VariableC_Blend>(engine, "CandyBlending", "SYSTEM");
            CsvCtrl.WriteSolution<VariableC_Produce>(engine, "CandyBlending", "SYSTEM");
            return new CandyBlendingSolution(blend, produce);
        }

        /// <summary>逐條把解值代回 Model.md 的 [C1]–[C4]；不成立就丟例外，NEVER 只記 log 繼續。</summary>
        private static void ValidateRules(
            Dictionary<string, double> blend,
            Dictionary<string, double> produce,
            Dataload data)
        {
            // [C1] ∀ material：Σ_brand Blend ≤ MonthlySupplyLimit
            foreach (var material in data.set_RawMaterial)
            {
                double used = data.set_CandyBrand.Sum(brand => BlendOf(blend, material.RawMaterial, brand.CandyBrand));
                double supplyLimit = data.parameter_MonthlySupplyLimit
                    .FindParameterOrLog(row => row.RawMaterial == material.RawMaterial, material.RawMaterial)?.QTY ?? 0.0;

                if (used > supplyLimit + Tolerance)
                    throw new InvalidOperationException(
                        $"[C1] {material.RawMaterial} 違反 MaterialAvailability：投入 {used:F6} > 限量 {supplyLimit:F6}。");
            }

            // [C2] ∀ brand：Produce = Σ_material Blend
            foreach (var brand in data.set_CandyBrand)
            {
                double blended = data.set_RawMaterial.Sum(material => BlendOf(blend, material.RawMaterial, brand.CandyBrand));
                double produced = ProduceOf(produce, brand.CandyBrand);

                if (Math.Abs(produced - blended) > Tolerance)
                    throw new InvalidOperationException(
                        $"[C2] {brand.CandyBrand} 違反 BrandMassBalance：產量 {produced:F6} ≠ 投入合計 {blended:F6}。");
            }

            // [C3] ∀ (material, brand) ∈ dom(MinContentRatio)：Blend ≥ Ratio · Produce
            foreach (var ratio in data.parameter_MinContentRatio)
            {
                double actual = BlendOf(blend, ratio.RawMaterial, ratio.CandyBrand);
                double required = ratio.QTY * ProduceOf(produce, ratio.CandyBrand);

                if (actual < required - Tolerance)
                    throw new InvalidOperationException(
                        $"[C3] ({ratio.RawMaterial}, {ratio.CandyBrand}) 違反 MinContent：投入 {actual:F6} < 下限 {required:F6}。");
            }

            // [C4] ∀ (material, brand) ∈ dom(MaxContentRatio)：Blend ≤ Ratio · Produce
            foreach (var ratio in data.parameter_MaxContentRatio)
            {
                double actual = BlendOf(blend, ratio.RawMaterial, ratio.CandyBrand);
                double allowed = ratio.QTY * ProduceOf(produce, ratio.CandyBrand);

                if (actual > allowed + Tolerance)
                    throw new InvalidOperationException(
                        $"[C4] ({ratio.RawMaterial}, {ratio.CandyBrand}) 違反 MaxContent：投入 {actual:F6} > 上限 {allowed:F6}。");
            }

            Logging.Info("[Validate] [C1]–[C4] 全數成立。");
        }

        private static double BlendOf(Dictionary<string, double> blend, string rawMaterial, string candyBrand) =>
            blend.TryGetValue($"VariableC_Blend@{rawMaterial}@{candyBrand}", out double value) ? value : 0.0;

        private static double ProduceOf(Dictionary<string, double> produce, string candyBrand) =>
            produce.TryGetValue($"VariableC_Produce@{candyBrand}", out double value) ? value : 0.0;

        public void Print()
        {
            Console.WriteLine("=== 每月產量（kg）===");
            foreach (var kv in produce.OrderBy(kv => kv.Key))
                Console.WriteLine($"{kv.Key} = {kv.Value:F4}");

            Console.WriteLine("=== 原料投入（kg）===");
            foreach (var kv in blend.Where(kv => kv.Value > Tolerance).OrderBy(kv => kv.Key))
                Console.WriteLine($"{kv.Key} = {kv.Value:F4}");
        }
    }
}
