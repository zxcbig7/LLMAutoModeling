using System;
using System.Collections.Generic;

using MaxWeightIndependentSet.Model;

namespace MaxWeightIndependentSet.Project
{
    // 實例資料：G(n,p) 隨機圖，固定種子 → 完全可重現。
    // 數值保真：權重直接由種子產生，不四捨五入、不填佔位符。
    public class Dataload
    {
        // ── 實例旋鈕（structure/data tuning 時改這裡；可用環境變數 MWIS_N / MWIS_P / MWIS_SEED 覆寫）──
        public static readonly int NodeCount = EnvInt("MWIS_N", 250);
        public static readonly double EdgeProbability = EnvDouble("MWIS_P", 0.30);
        public static readonly int Seed = EnvInt("MWIS_SEED", 42);

        // Sets
        public List<string> NODE = new List<string>();
        public List<(string I, string J)> EDGE = new List<(string, string)>();

        // Parameters（一律 List<Param_XXX>）
        public List<Param_Weight> param_Weight = new List<Param_Weight>();

        private static int EnvInt(string key, int fallback)
            => int.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : fallback;

        private static double EnvDouble(string key, double fallback)
            => double.TryParse(Environment.GetEnvironmentVariable(key), out var v) ? v : fallback;

        public Dataload()
        {
            var rng = new Random(Seed);

            for (int i = 1; i <= NodeCount; i++)
                NODE.Add($"N{i}");

            foreach (var n in NODE)
                param_Weight.Add(new Param_Weight { NODE = n, QTY = rng.Next(1, 101) });

            for (int i = 0; i < NodeCount; i++)
                for (int j = i + 1; j < NodeCount; j++)
                    if (rng.NextDouble() < EdgeProbability)
                        EDGE.Add((NODE[i], NODE[j]));
        }
    }
}
