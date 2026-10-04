using ModelTuner;
using OptimFoundation.Core;
using OptimFoundation.Cplex;

// Cap6000：read-model 宿主專案，對 MIPLIB cap6000（Instances/tune/cap6000.mps.gz）做 Phase 3 solver tuning。
// 模型檔凍結在 Instances/（instances.lock 記 SHA-256）；調參期間只改 productionBaseline 與 exp 區塊。
// runner（Tuning/）原樣複製自 OptimFoundation/Templates/ModelTuner，tuning 期間唯讀。
//   dotnet run → production：用 productionBaseline 求解 Instances/tune 的每個模型
//   dotnet run -- lock → S0：把模型檔的 SHA-256 記到 instances.lock
//   dotnet run -- sizing → S1：Threads 定版（§2.3）
//   dotnet run -- probe → R0 產出 C：MipGap = 0 契約健檢探針（不進排名）
//   dotnet run -- exp <N> → 執行下方 R<N> 區塊，archive 到 Experiments/ 並產生 facts
//   dotnet run -- holdout <N> <label> → 用 holdout seeds 比較 R<N> baseline 與 champion
//   dotnet run -- facts <N> → 從 archive 重產 TUNING-FACTS 與彙總
//   dotnet run -- cplex-tune [秒] → S2.5：CPLEX 內建 tune

// ── 1. 模型檔與量測設定 ──
const string ProjectName = "Cap6000";
int[] tuningSeeds = { 11, 22, 33, 44, 55 };
int[] holdoutSeeds = { 66, 77, 88 };

var workspace = TunerWorkspace.Open(new OptProject(ProjectName));
string mode = args.Length > 0 ? args[0] : "production";

// ── 2. 正式設定 productionBaseline：實驗用 Clone() 複製；確認新設定較好後，只更新這裡 ──
// 停止契約（MipGap / TimeLimit）由使用者 2026-10-03 定案；環境契約見 TuningHistory.md。
// baseline provenance:
//   來源 experiment: initial
//   champion Trial : initial
//   promotion 日期 : -
//   diff : -
var productionBaseline = new CplexConfig
{
    MipGap = 1e-4,
    TimeLimit = 300,
    Threads = 8,
    ParallelMode = 1,
    Seed = 11,
};

switch (mode)
{
    case "lock":
        return workspace.WriteLock();
    case "facts":
        if (args.Length < 2)
        {
            TunerCli.PrintUsage();
            return 2;
        }
        return RoundFacts.Report(workspace, RoundFacts.ResolveExperimentName(workspace, args[1]));
    case "cplex-tune":
        if (!TunerCli.TryParseTune(args, out double budgetSeconds, out int repeat)) return 2;
        if (!workspace.VerifyLock(required: true)) return 3;
        return CplexTuner.Run(workspace, productionBaseline, budgetSeconds, repeat);
    case "sizing":
    {
        // S1 — Cap6000-sizing
        // §2.3：ParallelMode = 1 固定，只掃 Threads；候選 = 實體核心 12 / 11 / 10，對照現行值，3 個 seed。
        if (!workspace.VerifyLock(required: true) || !workspace.PrepareRun("sizing")) return 3;
        var instance = workspace.Tune[0];
        int[] sizingSeeds = { 11, 22, 33 };
        var cells = new (string Name, int Threads)[]
        {
            ($"threads{productionBaseline.Threads}-baseline", productionBaseline.Threads!.Value),
            ("threads12", 12),
            ("threads11", 11),
            ("threads10", 10),
        };

        var sizing = workspace.Project.Experiment("sizing", "S1 sizing：Threads 12/11/10 vs 現行值，3 seeds");
        Warmup(instance, productionBaseline, sizingSeeds[0]);
        var model = OptModel.ReadModel(instance.FullPath, instance.Name);
        for (int k = 0; k < sizingSeeds.Length; k++)
            for (int j = 0; j < cells.Length; j++)
            {
                var (name, threads) = cells[(j + k) % cells.Length];
                var cell = productionBaseline.Clone();
                cell.Threads = threads;
                cell.Seed = sizingSeeds[k];
                sizing.AddTrial(model, $"s1-{name}-s{sizingSeeds[k]}", cell);
            }
        sizing.Run();
        workspace.Archive("sizing");
        return 0;
    }
    case "probe":
    {
        // R0 產出 C — Cap6000-probe-mipgap0
        // §3.0：MipGap = 0 探針取真最佳值與現行契約的品質損失；不是 variant，不進排名。
        if (!workspace.VerifyLock(required: true) || !workspace.PrepareRun("probe-mipgap0")) return 3;
        var instance = workspace.Tune[0];
        var probe = productionBaseline.Clone();
        probe.MipGap = 0;
        workspace.Project.Experiment("probe-mipgap0", "R0 產出 C：MipGap = 0 契約健檢探針")
            .AddTrial(OptModel.ReadModel(instance.FullPath, instance.Name), $"probe-mipgap0-s{probe.Seed}", probe)
            .Run();
        workspace.Archive("probe-mipgap0");
        return 0;
    }
}

// ── 3. 實驗：每個已執行的 R<N> 區塊永久保留（§3.3.1），當次只跑指定的那一輪 ──
if (mode is "exp" or "holdout")
{
    if (!TunerCli.TryParseRound(args, out int roundNo)) return 2;
    if (!workspace.VerifyLock(required: true)) return 3;

    TuningRound round;
    if (roundNo == 0)
    {
        // R0 — Cap6000-tuning-r0
        // 基準量測：保持 baseline 設定不變，只更換 5 個調參用 seed；label 自動加上 -s<seed>。
        // 已執行（RunId 20261003-010450），設定已 materialize 成字面值。
        round = new TuningRound(workspace, roundNo, "R0 基準：baseline × 5 seeds，當對照組並看瓶頸剖面", tuningSeeds, holdoutSeeds)
            .Add("r0-baseline", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 });
    }
    else if (roundNo == 1)
    {
        // R1 — Cap6000-tuning-r1
        // 候選來源：S2.5 CPLEX tune 建議 GomoryCuts = -1（§3.6）。
        // 已執行（RunId 20261003-010916，retain），設定已 materialize 成字面值。
        round = new TuningRound(workspace, roundNo, "R1：GomoryCuts=-1（S2.5 tune 建議）vs baseline", tuningSeeds, holdoutSeeds)
            .Add("r1-baseline", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 })
            .Add("r1-GomoryCuts=-1", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11, GomoryCuts = -1 });
    }
    else if (roundNo == 2)
    {
        // R2 — Cap6000-tuning-r2
        // 剖面 Primal-search（§2.2 該列第一顆）。
        // 已執行（RunId 20261003-011054，retain），設定已 materialize 成字面值。
        round = new TuningRound(workspace, roundNo, "R2：Emphasis=1（Primal-search）vs baseline", tuningSeeds, holdoutSeeds)
            .Add("r2-baseline", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 })
            .Add("r2-Emphasis=1", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11, Emphasis = 1 });
    }
    else if (roundNo == 3)
    {
        // R3 — Cap6000-tuning-r3
        // 剖面 Primal-search；RINSHeur 設正值會在 node 0（root）呼叫 RINS。
        // 已執行（RunId 20261003-011218，retain），設定已 materialize 成字面值。
        round = new TuningRound(workspace, roundNo, "R3：RinsHeuristicFrequency=10（Primal-search）vs baseline", tuningSeeds, holdoutSeeds)
            .Add("r3-baseline", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11 })
            .Add("r3-RinsHeuristicFrequency=10", new CplexConfig { MipGap = 1e-4, TimeLimit = 300, Threads = 8, ParallelMode = 1, Seed = 11, RinsHeuristicFrequency = 10 });
    }
    else
    {
        Logging.Error($"[ROUND_BLOCK_MISSING] Program.cs 沒有 R{roundNo} 區塊 | value={roundNo} reason=define_round_block_first result=aborted");
        return 2;
    }

    return mode == "exp" ? round.Run() : round.RunHoldout(args.Length > 2 ? args[2] : "");
}

// ── 4. 正式求解：採用新設定後，也從這裡執行一次確認結果 ──
if (mode != "production")
{
    Console.Error.WriteLine($"參數錯誤：未知模式 '{mode}'。");
    TunerCli.PrintUsage();
    return 2;
}
if (workspace.Tune.Count == 0)
{
    Logging.Error("[INSTANCE_SET_EMPTY] Instances/tune 沒有模型檔（.sav / .lp / .mps，可含 .gz / .bz2）| reason=no_model_file result=aborted");
    return 2;
}

if (!workspace.VerifyLock(required: false)) return 3;

int unsolved = 0;
foreach (var instance in workspace.Tune)
{
    // 每個模型使用不同的專案名稱，讓 log 與 .sol 檔名不同，避免同一秒完成時互相覆蓋。
    using var project = new OptProject($"{ProjectName}-{instance.Name}")
        .LoadConfig(new ProjectConfig { EnableSolverLog = true, ExportSol = true });
    if (!project.Solve(OptModel.ReadModel(instance.FullPath, instance.Name), productionBaseline)) unsolved++;
    Logging.Info(RoundFacts.DescribeProduction(instance, project.Trial.Metrics));
}
return unsolved == 0 ? 0 : 1;

// 第一次求解含 JIT、DLL 載入與快取準備；先用同樣設定解一次暖機，結果不進實驗（同 TuningRound.Warmup）。
static void Warmup(TuningInstance instance, CplexConfig config, int seed)
{
    var cell = config.Clone();
    cell.Seed = seed;
    using var engine = new OptEngine(cell, ProjectConfig.Quiet());
    engine.SetModelName($"{instance.Name}-warmup");
    engine.Build();
    engine.ReadModel(instance.FullPath);
    engine.Solve();
    Logging.Info($"[Warmup] {instance.Name} seed={seed} status={engine.Status} result=excluded_from_experiment");
}
