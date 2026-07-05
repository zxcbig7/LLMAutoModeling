# Stage 12: Project.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Project` class that wires everything together: sets solver config, builds variables and constraints, solves, and outputs results.

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using System;
using System.Collections.Generic;

namespace Model
{
    public class Project : OptimFoundation.Cplex.OptEngine
    {
        private Dataload dataload;

        public Project(Dataload dataload, CplexConfig config) : base(config)
        {
            this.dataload = dataload;
        }

        public override void Build()
        {
            base.Build();
            new VariableCreate(dataload, this).Build();
            new BuildConstraints(dataload, this).Build();
            new ObjectiveFunction(dataload, this).Build();
        }

        public void Run()
        {
            Build();
            bool success = Solve();
            if (success)
            {
                // Output results to CSV using CsvCtrl
                CsvCtrl.SaveSolutionToCSV<VariableX_XXX>(this, "ProjectName", "User");
                Console.WriteLine($"Objective: {GetObjectiveValue()}");
            }
            else
            {
                Console.WriteLine($"Solve failed. Status: {Status}");
            }
            Dispose();
        }
    }
}
```

## Rules

- Inherit from `OptimFoundation.Cplex.OptEngine`
- Override `Build()` to call `VariableCreate`, `BuildConstraints`, `ObjectiveFunction`
- `Run()` calls `Build()`, `Solve()`, then outputs results
- Use `CsvCtrl.SaveSolutionToCSV<>` for each variable type
- Check `engine.Status` (Optimal / Feasible / Infeasible / Unbounded / TimeLimit)

---

## Inputs

### Variable Classes
{{VarClasses}}

### AML Model (for context)
{{AMLModel}}

Return code only (no explanation).
