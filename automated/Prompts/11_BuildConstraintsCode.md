# Stage 11: BuildConstraints.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `BuildConstraints` class that instantiates and calls each Constraint class.

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Model
{
    public class BuildConstraints
    {
        private Dataload dataload;
        private OptEngine engine;

        public BuildConstraints(Dataload dataload, OptEngine engine)
        {
            this.dataload = dataload;
            this.engine = engine;
        }

        public void Build()
        {
            // Instantiate and call Build() on each Constraint_XXX
        }
    }
}
```

## Pattern

```csharp
public void Build()
{
    new Constraint_BudgetConstraint(dataload, engine).Build();
    new Constraint_MinimumInvestment(dataload, engine).Build();
    // ... one line per constraint class
}
```

---

## Inputs

### Constraint Classes (from Stage 8)
{{ConstraintClasses}}

Return code only (no explanation).
