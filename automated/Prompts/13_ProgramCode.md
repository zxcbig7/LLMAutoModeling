# Stage 13: Program.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Program.cs` entry point that initializes `Dataload` and `CplexConfig`, then runs the project.

## Pattern

```csharp
using OptimFoundation.Cplex;
using Model;

var dataload = new Dataload();

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
project.Run();
```

## Rules
- Keep it minimal — just instantiate and run.
- `CplexConfig` values should match the problem scale (larger MILP → longer timeLimit).
- Do NOT add business logic here.

---

Return code only (no explanation).
