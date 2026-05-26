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

            parameter_ABC.Add(new Parameter_ABC { A = "A1", B = "B1", C = new DateTime(2026, 1, 1), QTY = 1 });
            parameter_ABC.Add(new Parameter_ABC { A = "A2", B = "B2", C = new DateTime(2026, 1, 5), QTY = 1 });

            // ③ CSV 讀取（取消註解以啟用）
            // SetA = CSVCtrl.ReadStrSet("Set_A.csv");
            // SetB = CSVCtrl.ReadStrSet("Set_B.csv");
            // SetC = CSVCtrl.ReadDateSet("Set_C.csv");
            // parameter_AB  = CSVCtrl.BuildParameter<Parameter_AB>("Param_AB");
            // parameter_ABC = CSVCtrl.BuildParameter<Parameter_ABC>("Param_ABC");
        }

        public void WriteToCSV(OptEngine engine)
        {
            // ── 存 CSV → Solution/VariableName.csv ───────────────────────────
            // 注意：必須先 CreateFolder()，ProjFolder 建構子不自動建目錄
            FolderDir.Solution.CreateFolder();
            CsvCtrl.SaveSolutionToCSV<VariableB_ABC>(engine, "V1", "USER");
            CsvCtrl.SaveSolutionToCSV<VariableX_A>  (engine, "V1", "USER");

            // ── 目標函數值 ───────────────────────────────────────────────────
            // double obj = engine.GetObjectiveValue();

            // ── 指定型別全部解值（key = "ClassName@prop1@prop2"）────────────
            // var sol = engine.GetSetVarValues<VariableB_ABC>();
            // foreach (var kvp in sol)
            // {
            //     string label = kvp.Key.Split('@').Last();
            //     Logging.Info($"{label} = {kvp.Value}");
            // }
        }
    }
}
