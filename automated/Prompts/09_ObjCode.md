# Stage 9: ObjectiveFunction.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `ObjectiveFunction` class that builds the objective function using the OptimFoundation CPLEX API.

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Model
{
    public class ObjectiveFunction
    {
        private Dataload dataload;
        private OptEngine engine;

        public ObjectiveFunction(Dataload dataload, OptEngine engine)
        {
            this.dataload = dataload;
            this.engine = engine;
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
- Match the Model's index sets exactly with ForEach loops
- Field names in LINQ must match exactly what's declared in the provided Dataload class

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
    dataload.PRODUCT_INDEX.ForEach(p =>
    {
        var cost = dataload.param_Cost
            .FirstOrDefault(x => x.PRODUCT_INDEX == p)?.QTY ?? 0.0;
        engine.AddLHS(cost, new VariableX_Amount(p));
    });
    engine.CreateMinimize();
}
```

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
