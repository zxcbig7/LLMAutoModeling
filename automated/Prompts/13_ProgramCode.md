# Stage 13: Program.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Program.cs` entry point that initializes `Dataload` (through the framework's blessed construction path) and `CplexConfig`, then runs the project.

## Pattern

```csharp
using OptimFoundation.Cplex;
using OptimFoundation.Core;
using <ProjectName>;
using <ProjectName>.Set;

var dataload = OptData.Load(() => new Dataload());

var config = new CplexConfig
{
    workThreads = 8,
    timeLimit = 3600,
    epGap = 1e-4,
    enableLog = true,
    exportLP = true,
    exportSol = true
};

var project = new Project(dataload, config);
bool ok = project.Run();
```

## Rules
- Keep it minimal — just instantiate and run.
- `Dataload` MUST be constructed via `OptData.Load(() => new Dataload())`, never a bare `new Dataload()`. `OptData.Load` triggers the framework's referential-integrity / duplicate-key / numeric-sanity validation immediately after construction; skipping it means bad data can reach the solver silently and produce a wrong "optimal" answer.
- If validation fails, `OptData.Load` throws `DataValidationException` before `Program.cs` gets to build anything — that is the intended fail-fast behavior, do not wrap it in a try/catch that swallows it.
- `CplexConfig` values should match the problem scale (larger MILP → longer timeLimit).
- Do NOT add business logic here.

## Do NOT emit — outdated API

- ❌ `var dataload = new Dataload();` presented as the entry point's construction call — always `OptData.Load(() => new Dataload())`.

---

Return code only (no explanation).
