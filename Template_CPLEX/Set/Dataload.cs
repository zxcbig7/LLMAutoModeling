using OptimFoundation.Cplex;
using OptimFoundation.Core;
using Template.Parameter;
using Template.Variable;

namespace Template.Set
{
    /// <summary>
    /// Sets 定義 + 載入。
    /// 負責：罰分權重、Set 集合建立、Parameter 實體載入、CSV I/O。
    /// </summary>
    public class Dataload
    {
        // ── 罰分權重（目標式係數） ─────────────────────────────────────────
        public double Penalty_1 = 1.0;
        public double Penalty_2 = 0.5;
        public double Penalty_3 = 0.2;
        public double Penalty_4 = 0.1;
        public double Penalty_5 = 0.1;

        // ── 區間限制式界限（Constraint_Range）/ 軟性限制式（Constraint_Soft） ──
        public double RangeLB      = 0;
        public double RangeUB;            // 於建構子設為 SetC.Count（恆可行示範值）
        public double SoftTarget   = 1.0;
        public double Penalty_Soft = 0.5;

        // ── Sets ──────────────────────────────────────────────────────────
        public List<string>   SetA = new();   // 第一維索引
        public List<string>   SetB = new();   // 第二維索引
        public List<DateTime> SetC = new();   // 時間軸

        // ── Parameters（實體由 Parameter/ 資料夾的類別承載） ──────────────
        public List<Parameter_AB>  parameter_AB  = new();
        public List<Parameter_ABC> parameter_ABC = new();

        public Dataload()
        {
            // ① Sets 建立
            SetA.AddRange(["A1", "A2", "A3", "A4", "A5"]);
            SetB.AddRange(["B1", "B2", "B3"]);

            int year = 2026, month = 1;
            for (int d = 1; d <= DateTime.DaysInMonth(year, month); d++)
                SetC.Add(new DateTime(year, month, d));

            // ② Parameters 建立
            var rng = new Random(42);
            foreach (var a in SetA)
                foreach (var b in SetB)
                    parameter_AB.Add(new Parameter_AB { A = a, B = b, QTY = rng.Next(1, 5) });

            // 寫法 A：object initializer
            parameter_ABC.Add(new Parameter_ABC { A = "A1", B = "B1", C = new DateTime(2026, 1, 1), QTY = 1 });
            // 寫法 B：位置式建構（generator 自動產生的 params object[] 建構子；依屬性順序 A,B,C,QTY）
            parameter_ABC.Add(new Parameter_ABC("A2", "B2", new DateTime(2026, 1, 5), 1));

            RangeUB = SetC.Count;   // 區間上界 = 期數（恆可行示範值）

            // ③ CSV 讀取（取消註解以啟用）
            // SetA = CSVCtrl.ReadStrSet("Set_A.csv");
            // SetB = CSVCtrl.ReadStrSet("Set_B.csv");
            // SetC = CSVCtrl.ReadDateSet("Set_C.csv");
            // parameter_AB  = CSVCtrl.BuildParameter<Parameter_AB>("Param_AB");
            // parameter_ABC = CSVCtrl.BuildParameter<Parameter_ABC>("Param_ABC");
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
            CsvCtrl.SaveSolutionToCSV<VariableB_ABC>(engine, "V1", "USER");
            CsvCtrl.SaveSolutionToCSV<VariableX_A>  (engine, "V1", "USER");
            CsvCtrl.SaveSolutionToCSV<VariableI_A>  (engine, "V1", "USER");
        }
    }
}
