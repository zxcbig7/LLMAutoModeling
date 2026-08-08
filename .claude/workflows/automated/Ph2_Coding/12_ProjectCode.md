# Stage 12: OptModel and runner composition (OptimFoundation CPLEX)

## Role

You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task

Generate the `Program.cs` composition block that defines one reusable `OptModel`, then selects `OptProject` for one production solve or `OptExperiment` for a configuration scan.

```csharp
var model = new OptModel("Canonical") // model definition; execution belongs to a runner
    // Insert the Stage 10 variable calls.
    // Insert the Stage 11 objective and constraint calls.
    ;

if (args.Contains("experiment"))
{
    var baseline = solverConfig.Clone();
    var emphasis = baseline.Clone();
    emphasis.Emphasis = 2;

    new OptExperiment("myproject-tuning", "baseline vs emphasis")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", emphasis)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => data.WriteToCSV(engine));

bool ok = project.Execute();
Logging.Info($"Status={project.optEngine.Status} success={ok}");
```

## Rules

- `OptModel` is a reusable model definition only. It owns no engine and is not disposable.
- `OptProject` is the single-solve runner. Configuration and `OnSolved` belong here.
- `OptExperiment` is the n-config × m-model runner. It has no `OnSolved`; it saves the `Experiment` automatically.
- Reuse the one `data` instance produced by `OptData.Load`. Treat it as immutable after loading. `Freeze()` guards framework-controlled mutation APIs; direct writes to public fields or mutable lists are not guaranteed to be intercepted immediately.
- Create tuning variants with `Clone()` and concrete objects. Do not mutate a shared baseline through `Action<CplexConfig>` delegates.
- Model name identifies a formulation in trial labels. Project identity belongs in `ProjectConfig.ProjectName`.
- Do not create a separate `Project.cs` composition facade.

## Inputs

### Stage 10 variable chain

{{VariablePipeline}}

### Stage 11 objective and constraint chain

{{ModelPipeline}}

### Variable classes

{{VarClasses}}

Return the composition block only, with no explanation.
