# Stage 12: Project.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Project` class that wires everything together: builds an `OptModel` composition root (config, variables, model, solved-callback), runs it, and reports the result.

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using <ProjectName>.Set;
using <ProjectName>.Variable;
using <ProjectName>.Constraint;

namespace <ProjectName>
{
    public class Project
    {
        private readonly Dataload _dataload;
        private readonly CplexConfig _config;

        public Project(Dataload dataload, CplexConfig config)
        {
            _dataload = dataload;
            _config = config;
        }

        public bool Run()
        {
            using var model = new OptModel("<ProjectName>")
                .UseConfig(() => _config)
                .AddVariables(e => new VariableCreate(_dataload, e).Build())
                .AddModel(e => new BuildModel(_dataload, e).Build())
                .OnSolved(e =>
                {
                    CsvCtrl.WriteSolution<VariableX_XXX>(e, "<ProjectName>", "User");
                    Logging.Info($"Objective: {e.GetObjectiveValue()}");
                });

            bool ok = model.Execute();
            Logging.Info($"[{model.optEngine.GetType().Name}] success={ok}, status={model.optEngine.Status}");
            return ok;
        }
    }
}
```

## Rules

- `Project` is a plain composition-root class — it does **NOT** inherit from `OptimFoundation.Cplex.OptEngine`. The engine's `Build()`/`Solve()` are framework-owned template methods (they run a pre-solve scale guard internally); a project can no longer subclass `OptEngine` and override `Build()`.
- Build the model with the Fluent `OptModel` API: `new OptModel("<ProjectName>").UseConfig(...).AddVariables(...).AddModel(...).OnSolved(...)`, then call `.Execute()`.
- `AddVariables` receives an `Action<OptEngine>` that calls `new VariableCreate(dataload, engine).Build()`.
- `AddModel` receives an `Action<OptEngine>` that calls `new BuildModel(dataload, engine).Build()` (that class already builds the objective **and** every constraint — see Stage 11).
- `OnSolved` receives an `Action<OptEngine>` that runs only when the solve succeeds — write solutions here with `CsvCtrl.WriteSolution<TVariableClass>(engine, dataId, userId)` for each variable type, and log `engine.GetObjectiveValue()`.
- Check `model.optEngine.Status` (`Optimal` / `Feasible` / `Infeasible` / `Unbounded` / `TimeLimit`) after `Execute()`, whether or not it returned `true`.
- `using var model = ...` — `OptModel` is `IDisposable`; a `using` declaration disposes it (and the underlying engine) automatically, no manual `Dispose()` call needed.

---

## Do NOT emit — outdated API

- ❌ `public class Project : OptimFoundation.Cplex.OptEngine` with `public override void Build() { base.Build(); ... }` — `Build()` is no longer a virtual method on `OptEngine`; this pattern does not compile against the current framework.
- ❌ Manually calling `Dispose()` — use `using var model = new OptModel(...)`.
- ❌ `engine.GetVarSol(...)` / `engine.GetSetVarSol<T>()` / `CsvCtrl.SaveToCSV<T>(...)` — none of these exist; use `engine.GetSetVarValues<T>()`, `engine.GetObjectiveValue()`, `CsvCtrl.WriteSolution<T>(engine, dataId, userId)`.

---

## Inputs

### Variable Classes
{{VarClasses}}

### Model (for context)
{{Model}}

Return code only (no explanation).
