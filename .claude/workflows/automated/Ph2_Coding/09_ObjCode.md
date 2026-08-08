# Stage 9: ObjectiveFunction.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `ObjectiveFunction` class that builds the objective function using the OptimFoundation CPLEX API, under `Objective/` (`namespace <ProjectName>.Objective`).

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using <ProjectName>.Set;
using <ProjectName>.Variable;

namespace <ProjectName>.Objective
{
    public class ObjectiveFunction
    {
        private readonly Dataload _dataload;
        private readonly OptEngine _engine;

        public ObjectiveFunction(Dataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine = engine;
        }

        public void Build()
        {
            // Accumulate terms with engine.AddLHS(...)
            // End with engine.CreateMinimize() or engine.CreateMaximize()
        }
    }
}
```

## Rules

- Read the objective direction from Model (`Minimize` → `CreateMinimize()`, `Maximize` → `CreateMaximize()`)
- Accumulate ALL objective terms first, then call `CreateMinimize()`/`CreateMaximize()` once at the end
- Retrieve parameter values via LINQ into local variables FIRST — never inline LINQ inside `AddLHS`
- Match the Model's index sets exactly with `foreach` loops over the `Set_*` bricks (they are `IEnumerable<T>`, not `List<T>` — no `.ForEach(...)`)
- Property names on Parameter/Variable classes are the PascalCase dimension names declared via `[OptDim<...>("Name")]` in Stage 5/6 — field names in LINQ must match exactly what's declared in the provided Dataload/Parameter/Variable classes

## API

```csharp
engine.AddLHS(double coefficient, object variable);
engine.CreateMinimize();   // or
engine.CreateMaximize();
```

## Example (Minimize total cost)

```csharp
public void Build()
{
    foreach (var p in _dataload.PRODUCT)
    {
        var cost = _dataload.parameter_Cost
            .FirstOrDefault(x => x.Product == p)?.QTY ?? 0.0;
        _engine.AddLHS(cost, new VariableX_Amount { Product = p });
    }
    _engine.CreateMinimize();
    Logging.Info("目標函數：min Σ cost·Amount");
}
```

## Do NOT emit — outdated API

- ❌ `dataload.PRODUCT_INDEX.ForEach(p => ...)` — `Set_*` bricks use `foreach`, not `.ForEach`.
- ❌ `x.PRODUCT_INDEX` ALL-CAPS property reference — always the PascalCase `[OptDim]` name (e.g. `x.Product`).
- ❌ `new VariableX_Amount(p)` positional constructor — always the object-initializer form.

---

## Inputs

### Model (objective function section)
{{Model}}

### Parameter Classes
```csharp
{{ParamClasses}}
```

### Variable Classes
```csharp
{{VarClasses}}
```

### Dataload Class (use EXACT field names from here)
```csharp
{{DataloadCode}}
```

### Constraints Code (for context — avoid duplicating logic)
```csharp
{{ConstraintCode}}
```

---

Return code only (no explanation).

Take a deep breath and think step by step.
