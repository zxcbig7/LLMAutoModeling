# Stage 13: Program.cs (OptimFoundation CPLEX)

## Role

You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task

Assemble the complete top-level `Program.cs`: materials, reusable model definition, then runner selection.

## Required shape

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
// project namespaces

// 1. Materials
var data = OptData.Load(() => new Dataload());
var projectConfig = new ProjectConfig
{
    ProjectName = "<ProjectName>",
    EnableSolverLog = true,
    ExportLP = true,
    ExportSol = true,
};
var solverConfig = new CplexConfig
{
    workThreads = 8,
    timeLimit = 3600,
    epGap = 1e-4,
};
// Extract scalar Parameter values here.

// 2. Model
var model = new OptModel("Canonical") // model definition; execution belongs to a runner
    // Stage 10: one AddVariables call per variable family
    // Stage 11: one AddObjective, then one AddConstraints per constraint
    ;

// 3. Environment
if (args.Contains("experiment"))
{
    var baseline = solverConfig.Clone();
    var variant = baseline.Clone();
    variant.Emphasis = 2;

    new OptExperiment("<project>-tuning", "baseline vs variant")
        .AddModel(model)
        .AddConfig("baseline", baseline)
        .AddConfig("emphasis=optimal", variant)
        .Run();
    return;
}

using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => data.WriteToCSV(engine));

bool ok = project.Execute();
```

## Rules

- Keep the three sections flat and visible in this order: materials → model → environment.
- Construct `Dataload` only through `OptData.Load`; never swallow `DataValidationException`.
- Use `ProjectConfig` for project identity, retention, solver-log visibility, and LP/MPS/Sol export. Use `CplexConfig` only for solver knobs.
- The default `ProjectConfig.EnableSolverLog` is `true`; set output choices explicitly when the project requires them.
- Choose `workThreads`, `timeLimit`, `epGap`, and `mipEmphasis` from `../pipeline-rules.md` §Stage 00 for `{{ProblemType}}`; do not blindly copy the example.
- Register each variable, objective, and constraint directly on `OptModel`; do not add forwarding helpers.
- `OnSolved` belongs only to `OptProject`.
- `OptExperiment` defaults to solver log off, exports off, and no housekeeping. Use one loaded data instance for all cells and concrete cloned configs.
- Objective and Constraint constructors receive explicit Set/Parameter/scalar dependencies, never the whole `Dataload`.
- Do not add business logic or a separate composition-root class.

## Inputs

### Problem type

{{ProblemType}}

### Stage 10 variable pipeline

{{VariablePipeline}}

### Stage 11 model pipeline

{{ModelPipeline}}

### Stage 12 runner composition

{{RunnerComposition}}

Return code only, with no explanation.
