# Stage 10: VariableCreate.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `VariableCreate` class that calls OptimFoundation API to create decision variables in the CPLEX solver, under `Variable/` (`namespace <ProjectName>.Variable`).

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using <ProjectName>.Set;

namespace <ProjectName>.Variable
{
    public class VariableCreate
    {
        private readonly Dataload _dataload;
        private readonly OptEngine _engine;

        public VariableCreate(Dataload dataload, OptEngine engine)
        {
            _dataload = dataload;
            _engine = engine;
        }

        public void Build()
        {
            // Call BuildVars<> for each variable type (default), or BuildCVs<>/BuildIVs<>/BuildBVs<> for custom bounds
            Logging.Info($"Variables created: {_engine.varCount}");
        }
    }
}
```

## Variable Type Mapping

| Model Declaration | C# Method (default — prefix-driven) | C# Method (custom bounds) |
|---|---|---|
| `var X{Set} >= 0;` (continuous) | `engine.BuildVars<VariableX_X>(dataload.SET)` | `engine.BuildCVs<VariableX_X>(dataload.SET)` |
| `var X{Set} >= 0, <= 1;` (bounded continuous) | — (bounds need explicit call) | `engine.BuildCVs<VariableX_X>(0, 1, dataload.SET)` |
| `var X{Set} integer >= 0;` | `engine.BuildVars<VariableI_X>(dataload.SET)` | `engine.BuildIVs<VariableI_X>(dataload.SET)` |
| `var X{Set} binary;` | `engine.BuildVars<VariableB_X>(dataload.SET)` | `engine.BuildBVs<VariableB_X>(dataload.SET)` |
| No index set | `engine.BuildVars<VariableX_X>()` | `engine.BuildCVs<VariableX_X>()` |

## Rules

- Use `engine.BuildVars<T>(...)` by default — it infers Binary/Continuous/Integer purely from the class name prefix (`VariableB_`/`VariableX_`/`VariableI_`), so there is nothing to get wrong. Only fall back to the explicit `BuildCVs<T>`/`BuildIVs<T>`/`BuildBVs<T>` when the variable needs custom bounds (`lb`, `ub`) other than the type's default.
- Use the EXACT set field names from the provided Dataload class (e.g., `dataload.PRODUCT`, a `Set_Product` brick)
- The order of sets passed in must match the `[OptDim<...>("Name")]` attribute order on the Variable class
- Match variable class names exactly from the provided Variable classes

## Example

```csharp
public void Build()
{
    // Continuous: Amount[i, j]
    _engine.BuildVars<VariableX_Amount>(_dataload.PRODUCT, _dataload.MACHINE);

    // Binary: UseRoute[i, j]
    _engine.BuildVars<VariableB_UseRoute>(_dataload.ORIGIN, _dataload.DEST);

    // Integer: NumberOf[i]
    _engine.BuildVars<VariableI_NumberOf>(_dataload.PRODUCT);

    Logging.Info($"Variables created: {_engine.varCount}");
}
```

## Do NOT emit — outdated API

- ❌ `engine.BuildCVs<VariableY_X>(...)` — the `VariableY_` prefix no longer exists (see Stage 6).
- ❌ Guessing bounds by variable "role" instead of by class name prefix — `BuildVars<T>` already reads the prefix; don't second-guess it with a manually-picked `BuildCVs`/`BuildIVs`/`BuildBVs` unless custom bounds are actually needed.

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
