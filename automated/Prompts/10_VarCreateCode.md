# Stage 10: VariableCreate.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `VariableCreate` class that calls OptimFoundation API to create decision variables in the CPLEX solver.

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace Model
{
    public class VariableCreate
    {
        private Dataload dataload;
        private OptEngine engine;

        public VariableCreate(Dataload dataload, OptEngine engine)
        {
            this.dataload = dataload;
            this.engine = engine;
        }

        public void Build()
        {
            // Call BuildCVs<> / BuildIVs<> / BuildBVs<> for each variable type
        }
    }
}
```

## Variable Type Mapping

| Model Declaration | C# Method |
|---|---|
| `var X{Set} >= 0;` (continuous) | `engine.BuildCVs<VariableX_X>(dataload.SET)` |
| `var X{Set} >= 0, <= 1;` (bounded) | `engine.BuildCVs<VariableX_X>(0, 1, dataload.SET)` |
| `var X{Set} integer >= 0;` | `engine.BuildIVs<VariableY_X>(dataload.SET)` |
| `var X{Set} binary;` | `engine.BuildBVs<VariableY_X>(dataload.SET)` |
| No index set | `engine.BuildCVs<VariableX_X>()` |

## Rules

- Use the EXACT set field names from the provided Dataload class (e.g., `dataload.PRODUCT_INDEX`)
- The order of sets in `BuildCVs<>` must match the property order in the Variable class
- Match variable class names exactly from the provided Variable classes

## Example

```csharp
public void Build()
{
    // Continuous: Amount[i, j]
    engine.BuildCVs<VariableX_Amount>(dataload.PRODUCT_INDEX, dataload.MACHINE_ID);

    // Binary: UseRoute[i, j]
    engine.BuildBVs<VariableY_UseRoute>(dataload.ORIGIN, dataload.DESTINATION);

    // Integer: NumberOf[i]
    engine.BuildIVs<VariableY_NumberOf>(dataload.PRODUCT_INDEX);
}
```

---

## Inputs

### Model (variable declarations section)
{{Model}}

### Variable Classes (use EXACT class names from here)
```csharp
{{VarClasses}}
```

### Dataload Class (use EXACT field names from here)
```csharp
{{DataloadCode}}
```

---

Return code only (no explanation).

Take a deep breath and think step by step.
