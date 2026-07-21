# Stage 11: BuildModel.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `BuildModel` class that instantiates and calls each Constraint class plus the `ObjectiveFunction`, under `Constraint/` (`namespace <ProjectName>.Constraint`).

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using <ProjectName>.Set;
using <ProjectName>.Objective;

namespace <ProjectName>.Constraint
{
    public class BuildModel
    {
        private readonly Dataload _dataload;
        private readonly OptEngine _engine;

        public BuildModel(Dataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine = engine;
        }

        public void Build()
        {
            // Instantiate and call Build() on the ObjectiveFunction, then each Constraint_XXX
        }
    }
}
```

## Pattern

```csharp
public void Build()
{
    new ObjectiveFunction(_dataload, _engine).Build();

    new Constraint_BudgetConstraint(_dataload, _engine).Build();
    new Constraint_MinimumInvestment(_dataload, _engine).Build();
    // ... one line per constraint class
}
```

- `BuildModel` only calls into `ObjectiveFunction` / `Constraint_Xxx`; NEVER write raw `AddLHS`/`AddRHS` calls directly in this class.

---

## Inputs

### Constraint Classes (from Stage 8)
{{ConstraintClasses}}

Return code only (no explanation).
