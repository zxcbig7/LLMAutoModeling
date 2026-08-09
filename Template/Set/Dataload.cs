using OptimFoundation.Cplex;
using OptimFoundation.Core;
using OptimFoundation.Core.IO;
using Template.Parameter;
using Template.Variable;

namespace Template.Set
{
    /// <summary>
    /// Sets 定義 + 載入。
    /// 負責：罰分權重、Set 集合建立、Parameter 實體載入、CSV I/O。
    /// </summary>
    public partial class Dataload : DataContext
    {
        // ── 罰分權重（目標式係數） ─────────────────────────────────────────
        public double Penalty_1 = 1.0;
        public double Penalty_2 = 0.5;
        public double Penalty_3 = 0.2;
        public double Penalty_4 = 0.1;
        public double Penalty_5 = 0.1;
        // Penalty_6 對應 VariableI_A：少了這一項該變數不會進模型，CPLEX 不認得它，GetIVSolution 會丟 UnknownObjectException
        public double Penalty_6 = 0.05;

        // ── 各限制式的界限值（限制式類別自己不得寫死數字，一律由此傳入）──────
        // Constraint_LessEqual
        public double AssignMax = 1;
        // Constraint_GreatEqual
        public double GreatEqualLB = 5;
        // Constraint_Window
        public int WindowSize = 7;
        public double WindowMax = 5;
        // Constraint_Range；RangeUB 於建構子設為 SetC.Count（恆可行示範值）
        public double RangeLB = 0;
        public double RangeUB;
        // Constraint_Soft
        public double SoftTarget = 1.0;
        public double Penalty_Soft = 0.5;

        // ── Sets（canonical row collections） ────────────────────────────
        public List<Set_A> SetA = new();   // 第一維索引
        public List<Set_B> SetB = new();   // 第二維索引
        public List<Set_C> SetC = new();   // 時間軸
        public List<Set_Arc> Arc = new();  // 稀疏二維 tuple Set 範例

        // ── Parameters（實體由 Parameter/ 資料夾的類別承載） ──────────────
        public List<Parameter_AB> parameter_AB = new();
        public List<Parameter_ABC> parameter_ABC = new();

        public Dataload()
        {
            // ① Sets 建立
            SetA.AddRange(new[] { "A1", "A2", "A3", "A4", "A5" }
                .Select(value => new Set_A { A = value }));
            SetB.AddRange(new[] { "B1", "B2", "B3" }
                .Select(value => new Set_B { B = value }));
            Arc.AddRange(new[]
            {
                new Set_Arc { From = "A1", To = "B1" },
                new Set_Arc { From = "A1", To = "B3" },
                new Set_Arc { From = "A3", To = "B2" },
            });

            int year = 2026, month = 1;
            var dates = new List<DateTime>();
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                dates.Add(new DateTime(year, month, d));
            SetC.AddRange(dates.Select(value => new Set_C { C = value }));

            // ② Parameters 建立
            var rng = new Random(42);
            foreach (var a in SetA)
                foreach (var b in SetB)
                    parameter_AB.Add(new Parameter_AB { A = a.A, B = b.B, QTY = rng.Next(1, 5) });

            // object initializer（generator 生成的 body 已含位置式 ctor，逃生口不再示範）
            parameter_ABC.Add(new Parameter_ABC { A = "A1", B = "B1", C = new DateTime(2026, 1, 1), QTY = 1 });
            parameter_ABC.Add(new Parameter_ABC { A = "A2", B = "B2", C = new DateTime(2026, 1, 5), QTY = 1 });

            RangeUB = SetC.Count;   // 區間上界 = 期數（恆可行示範值）

            // ③ CSV 讀取改由 IDataSource.Load<T>() 回傳相同的 row list 型別。
        }

        public void WriteToCSV(OptEngine engine)
        {
            // ── 求解摘要（Status / 目標值 / best bound / gap）─────────────────
            Logging.Info($"Status={engine.Status}  Obj={engine.GetObjectiveValue():F4}  " +
                         $"BestBound={engine.BestObjValue:F4}  MIPGap={engine.MIPGap:P2}");

            // ── 依型別取解：GetSetVarValues / GetBVSolution / GetCVSolution / GetIVSolution ──
            var bvByType = engine.GetSetVarValues<VariableB_ABC>();   // 指定型別 → Dictionary<完整名, 值>
            var allBV    = engine.GetBVSolution();                    // 所有二元變數
            var allCV    = engine.GetCVSolution();                    // 所有連續變數
            var allIV    = engine.GetIVSolution();                    // 所有整數變數
            Logging.Info($"解值統計：B={allBV.Count} C={allCV.Count} I={allIV.Count}" +
                         $"（VariableB_ABC 取出 {bvByType.Count} 筆）");

            // ── 取單一變數值（key = "ClassName@prop1@prop2@..."）──────────────
            string? firstKey = engine.GetSetVarNames<VariableB_ABC>().FirstOrDefault();
            if (firstKey != null)
                Logging.Info($"{firstKey} = {engine.GetVariableValue(firstKey)}");

            // ── 軟性限制式違反量（Deficit_* 解值，用前綴過濾 GetSolution）──────
            var deficits = engine.GetSolution()
                .Where(kv => kv.Key.StartsWith("Deficit_") && kv.Value > 1e-6).ToList();
            if (deficits.Count > 0)
                Logging.Info($"軟性短缺項：{deficits.Count}");

            // ── 存 CSV → Solution/<VariableName>.csv（必須先 CreateFolder）────
            FolderDir.Solution.CreateFolder();
            CsvCtrl.WriteSolution<VariableB_ABC>(engine, "V1", "USER");
            CsvCtrl.WriteSolution<VariableC_A>  (engine, "V1", "USER");
            CsvCtrl.WriteSolution<VariableI_A>  (engine, "V1", "USER");
        }
    }
}
