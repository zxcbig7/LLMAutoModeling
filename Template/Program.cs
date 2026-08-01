using System;
using System.Linq;
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using Template.Set;
using Template.Variable;
using Template.Objective;
using Template.Constraint;

// ══════════════════════════════════════════════════════════════════════════
//  Template — OptimFoundation / CPLEX 專案唯一進入點
//
//  執行方式
//    dotnet run                 求解一次（solve 模式，§1）
//    dotnet run -- experiment   參數掃描（experiment 模式，§4）
//
//  層級關係（整條鏈都在本檔可見，沒有額外的包裝類別）
//
//    ① 資料  Set/Set_*.cs           [OptSet<T>]   維度積木（一顆一個檔）
//            Parameter/Parameter_*  [OptParam]    係數（值一律放 QTY 欄位）
//            Set/Dataload.cs        DataContext   載入資料 + 輸出解
//              └ OptData.Load(...)  唯一建構入口：註冊 + 聚合驗證，壞資料當場丟例外
//    ② 變數  Variable/VariableB_|X_|I_*  [OptVar]  決策變數宣告（前綴決定型別）
//              └ CreateVariables()  本檔 §2：engine.BuildBVs / BuildCVs / BuildIVs
//    ③ 模型  Objective/ObjectiveFunction  目標式（MUST 先建）
//            Constraint/Constraint_*      限制式（一條或一組邏輯相關 = 一個檔）
//              └ BuildModel()  本檔 §3：逐一 .Build()，呼叫順序 = 組裝順序
//    ④ 引擎  CplexConfig → OptEngine：②③ 建出來的東西全部進這裡
//              └ NewConfig()  本檔 §5：兩模式共用的求解器設定
//    ⑤ 執行  solve       OptModel fluent（本檔 §1）
//            experiment  OptEngine + Trial / Experiment（本檔 §4）
//
//  兩個模式共用 §2 §3：建模碼只有一份，掃參數不可能跟求解跑到不同的模型。
//  天條：Constraint / Objective 內不得出現裸數字，係數一律經 dataload 取得。
// ══════════════════════════════════════════════════════════════════════════

if (args.Contains("experiment"))
{
    RunExperiment();
    return;
}

// ── §1 solve 模式 ─────────────────────────────────────────────────────────
// OptData.Load 是資料層唯一建構入口：跑參照完整性 / 重複 key / 數值 sanity 驗證，
// 失敗丟 DataValidationException（fail-fast，NEVER try/catch 吞掉）。
var dataload = OptData.Load(() => new Dataload());

// OptModel = composition root，保證 AddVariables 先於 AddModel，內建 build / solve 計時。
using (var model = new OptModel("Template")
    .UseConfig(() => NewConfig(timeLimit: 300, verbose: true))
    .AddVariables(engine => CreateVariables(dataload, engine))
    .AddModel(engine => BuildModel(dataload, engine))
    .OnSolved(engine => dataload.WriteToCSV(engine)))
{
    bool ok = model.Execute();
    Logging.Info($"求解結果：{(ok ? "成功" : "失敗")} Status={model.optEngine.Status}");

    // Infeasible → 框架已自動跑 IIS，這裡讀回最小衝突限制式集合
    if (!ok && model.optEngine.Status == SolveStatus.Infeasible)
    {
        var conflicts = model.optEngine.GetConflictConstraints();
        Logging.Info($"衝突限制式（{conflicts.Count}）：{string.Join(", ", conflicts)}");
    }
}

return;

// ── §2 變數層 ─────────────────────────────────────────────────────────────
// 型別由類別名前綴決定：VariableB_ = Binary、VariableX_ = Continuous、VariableI_ = Integer。
// 傳入 sets 的順序 MUST 對齊該變數 [OptDim] 的宣告順序，否則索引會錯位而不報錯。
static void CreateVariables(Dataload data, OptEngine engine)
{
    engine.BuildBVs<VariableB_ABC>(data.SetA, data.SetB, data.SetC);
    engine.BuildBVs<VariableB_AC>(data.SetA, data.SetC);
    engine.BuildBVs<VariableB_A>(data.SetA);

    engine.BuildCVs<VariableX_A>(data.SetA);
    engine.BuildCVs<VariableX_AB>(data.SetA, data.SetB);

    // 有界整數 [0, 10]；連續有界同理：BuildCVs<VariableX_A>(0, 100, data.SetA)
    engine.BuildIVs<VariableI_A>(0, 10, data.SetA);

    Logging.Info($"變數建立完成：{engine.varCount}");
}

// ── §3 模型層 ─────────────────────────────────────────────────────────────
// 目標式 MUST 先建：Constraint_Soft 的 penalty 是掛進既有目標式的，順序反了會掛空。
// 新增一條限制式 = Constraint/ 新增一個檔 + 這裡加一行，沒有別的地方要改。
//
// 這裡是唯一知道 Dataload 的地方：Constraint / Objective 的建構子只收自己用得到的
// 積木、Parameter 清單與界限值，NEVER 收整包 Dataload——換資料結構不必動任何式子。
static void BuildModel(Dataload data, OptEngine engine)
{
    Logging.Info("【建構目標式】");
    new ObjectiveFunction(
        engine, data.SetA, data.SetB, data.SetC,
        penaltyABC: data.Penalty_1,
        penaltyAC: data.Penalty_2,
        penaltyA: data.Penalty_3,
        penaltyXA: data.Penalty_4,
        penaltyXAB: data.Penalty_5,
        penaltyIA: data.Penalty_6).Build();

    Logging.Info("【建構限制式】");
    new Constraint_Equality(engine, data.SetA, data.SetB, data.SetC, data.parameter_AB).Build();
    new Constraint_LessEqual(engine, data.SetA, data.SetB, data.SetC, data.AssignMax).Build();
    new Constraint_GreatEqual(engine, data.SetA, data.SetC, data.GreatEqualLB).Build();
    new Constraint_Window(engine, data.SetA, data.SetC, data.WindowSize, data.WindowMax).Build();
    new Constraint_VarOnRHS(engine, data.SetA, data.SetC).Build();
    new Constraint_Range(engine, data.SetA, data.SetC, data.RangeLB, data.RangeUB).Build();
    new Constraint_Soft(engine, data.SetA, data.SetB, data.SoftTarget, data.Penalty_Soft).Build();
}

// ── §4 experiment 模式 ────────────────────────────────────────────────────
// 同一個模型套多組 CplexConfig，每組跑一次 Trial.Capture（設定快照 + 收斂數據），
// 累積成 Experiment 後輸出 Experiments/<name>.csv + .json。
static void RunExperiment()
{
    Logging.SetLogFileName("Template_Experiment");

    var experiment = new Experiment(
        "template-tuning",
        "掃描 emphasis / varSel / nodeSelect / gap / threads / seed 對求解時間與 gap 的影響");

    // 每組 = (label, 在基準 config 上套用的調整)。
    // 抽象旋鈕（Emphasis / Seed，來自 ITunableConfig）跨引擎一致；
    // camelCase 欄位（varSel / nodeSelect / workThreads）為 CPLEX 專屬。
    var variants = new (string label, Action<CplexConfig> tune)[]
    {
        ("baseline", _ => { }),
        ("emphasis=optimal", c => c.Emphasis = 2),
        ("varsel=strong", c => c.varSel = 3),
        ("nodesel=bestbound", c => c.nodeSelect = 1),
        ("gap=0.01", c => c.epGap = 0.01),
        ("threads=4", c => c.workThreads = 4),
        ("seed=20260621", c => c.Seed = 20260621),
    };

    int i = 0;
    foreach (var (label, tune) in variants)
    {
        i++;
        var config = NewConfig(timeLimit: 60, verbose: false);
        tune(config);

        // 每個 Trial 用全新 dataload + engine，避免狀態跨 Trial 污染
        var data = OptData.Load(() => new Dataload());
        using var engine = new OptEngine(config);
        engine.Build();

        CreateVariables(data, engine); // ← 與 solve 模式同一份建模碼（§2）
        BuildModel(data, engine); // ← 同上（§3）

        Logging.Info($"[Experiment] ({i}/{variants.Length}) 求解中：{label} …");

        // 套件化單次擷取：抓這一 run 的完整設定 + 收斂數據（CPLEX 自動含收斂軌跡）
        var trial = Trial.Capture(engine, label, () => engine.Solve());
        experiment.AddTrial(trial);

        var metrics = trial.Metrics;
        Logging.Info(
            $"[Experiment] ({i}/{variants.Length}) {label}: Status={metrics.Status} " +
            $"Obj={metrics.ObjectiveValue:G6} Gap={metrics.MipGap:P2} Time={metrics.RunTimeMs:F0}ms " +
            $"Nodes={metrics.NodeCount} Vars={metrics.VarCount} Cons={metrics.ConstraintCount} " +
            $"Traj={metrics.Convergence.Count}");
    }

    experiment.Save(); // → Experiments/template-tuning.csv + .json
    Logging.Info($"[Experiment] 完成：{experiment.Trials.Count} 個 Trial 已寫入 {FolderDir.Experiment.GetPath()}");
}

// ── §5 求解器設定（兩模式共用）────────────────────────────────────────────
// verbose = false 時關掉 solver log 與 LP/MPS/Sol 匯出，掃描才不會被 I/O 拖慢。
// 完整欄位與 tuning 策略見 ../tuning/CLAUDE.md。
static CplexConfig NewConfig(double timeLimit, bool verbose) => new CplexConfig
{
    epGap = 0.03,
    timeLimit = timeLimit,
    workThreads = 8,
    enableLog = verbose,
    exportSol = verbose,
    exportLP = verbose,
    exportMPS = verbose,
};
