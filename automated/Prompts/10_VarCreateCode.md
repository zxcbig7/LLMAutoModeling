# Stage 10: Program.cs variable pipeline (OptimFoundation CPLEX)

## Role

You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task

Generate the `.AddVariables(...)` chain fragment that registers every decision-variable family directly on an `OptModel` in `Program.cs`.

```csharp
var model = new OptModel("Canonical") // model definition; execution belongs to a runner
    .AddVariables(engine => engine.BuildVars<VariableX_Amount>(data.PRODUCT, data.MACHINE))
    .AddVariables(engine => engine.BuildCVs<VariableX_Ratio>(0, 1, data.PRODUCT))
    .AddVariables(engine => engine.BuildVars<VariableB_UseRoute>(data.ORIGIN, data.DEST))
    .AddVariables(engine => engine.BuildVars<VariableI_NumberOf>(data.PRODUCT));
```

Each variable family gets its own `.AddVariables(...)` call. Do not create a class or local function whose only job is to forward these calls.

## Variable type mapping

| Model declaration | Default registration | Custom bounds |
| --- | --- | --- |
| continuous | `engine.BuildVars<VariableX_X>(sets...)` | `engine.BuildCVs<VariableX_X>(lb, ub, sets...)` |
| integer | `engine.BuildVars<VariableI_X>(sets...)` | `engine.BuildIVs<VariableI_X>(lb, ub, sets...)` |
| binary | `engine.BuildVars<VariableB_X>(sets...)` | `engine.BuildBVs<VariableB_X>(sets...)` |
| scalar | `engine.BuildVars<VariableX_X>()` | `engine.BuildCVs<VariableX_X>(lb, ub)` |

## Rules

- Prefer `BuildVars<T>` because the `VariableB_` / `VariableX_` / `VariableI_` prefix determines the type. Use an explicit `Build*Vs<T>` overload only for custom bounds.
- Use exact Set field names from `Dataload`.
- The Set argument order MUST equal the variable's `[OptDim<...>]` declaration order.
- Match generated variable class names exactly.
- The variable fragment belongs in `Program.cs` and precedes `.AddObjective(...)` and `.AddConstraints(...)`.
- Do not add solver configuration or execution here; later stages assemble those sections.

## Do not emit

- A forwarding class or helper method for variable registration.
- `VariableY_` types; that prefix does not exist.
- Bounds guessed from a variable's business role.

---

## Inputs

### Model variable declarations

{{Model}}

### Variable classes

```csharp
{{VarClasses}}
```

### Dataload class

```csharp
{{DataloadCode}}
```

Return the fluent chain fragment only, with no explanation.

Take a deep breath and think step by step.
