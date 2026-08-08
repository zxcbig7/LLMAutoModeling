# Stage 10: Program.cs variable pipeline (OptimFoundation CPLEX)

## Role

You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task

Generate the `.AddVariables(...)` chain fragment that registers every decision-variable family directly on an `OptModel` in `Program.cs`.

```csharp
var model = new OptModel("Canonical") // model definition; execution belongs to a runner
    .AddVariables(engine => engine.BuildVars<VariableX_Amount>(data.PRODUCT, data.MACHINE))
    .AddVariables(engine => engine.BuildVars<VariableX_Ratio>(data.PRODUCT))
    .AddVariables(engine => engine.BuildVars<VariableB_UseRoute>(data.ORIGIN, data.DEST))
    .AddVariables(engine => engine.BuildVars<VariableI_NumberOf>(data.PRODUCT));
```

Each variable family gets its own `.AddVariables(...)` call. Do not create a class or local function whose only job is to forward these calls.

## Variable type mapping

| Model declaration | Registration (only form allowed) | Bounds |
| --- | --- | --- |
| continuous | `engine.BuildVars<VariableX_X>(sets...)` | `[0, 1E100]`; anything tighter MUST be a `Constraint_*` |
| integer | `engine.BuildVars<VariableI_X>(sets...)` | `[0, 1E100]`; same rule |
| binary | `engine.BuildVars<VariableB_X>(sets...)` | `[0, 1]`, fixed |
| scalar (0-dim) | `engine.BuildVars<VariableX_X>()` | empty parentheses, no set |

## Rules

- `BuildVars<T>` is the **only** allowed form — the `VariableB_` / `VariableX_` / `VariableI_` prefix is the single source of truth for the type. `BuildBVs` / `BuildCVs` / `BuildIVs` are **banned** (see the api-guide §9.3 blacklist).
- `BuildVars` has **no bounds overload**. A bound in Model.md is a line of the model, so it MUST be emitted as its own `Constraint_*` — never hidden inside a build call, or it cannot be verified line by line against Model.md.
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
