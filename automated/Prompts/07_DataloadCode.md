# Stage 7: Dataload.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Set_*` set-brick classes and the `Dataload` class that load all sets and parameters from the problem description. Output goes to `Set/` (`namespace <ProjectName>.Set`).

```csharp
using OptimFoundation.Core;
using OptimFoundation.Modeling;
using <ProjectName>.Parameter;

namespace <ProjectName>.Set
{
    <Set_* brick declarations + Dataload class here>
}
```

## Structure Rules

- **Sets** → one `Set_<Name>` brick per set: `[OptSet<T>] public partial class Set_<Name> { }`, where `T` is the set's element type (`string` / `DateTime` / `int` / ...). NEVER a raw `public List<string> SET_NAME = new();` field — that skips framework validation entirely.
- **Parameters** → `public List<Parameter_XXX> parameter_xxx = new();` (always `List<>`, even for scalars) — using the classes from Stage 5.
- `Dataload` MUST be declared `public partial class Dataload : DataContext` — both `partial` and `: DataContext` are required. The generator emits a hidden partial method (`RegisterAll`) that registers every `Set_*` and `List<Parameter_*>` field it finds for validation; it only fires when the class is `partial` and inherits `DataContext`.
- Construction MUST go through `OptData.Load(() => new Dataload())` (see Stage 13 / Program.cs) — that is what triggers registration + validation. A bare `new Dataload()` still compiles but silently skips all validation, so never present it as the way to construct the class.
- Constructor populates every set (`.LoadInline(...)` for literal members, or `.LoadFrom(...)` when a set's membership is derived from parameter data already built earlier in the same constructor) and every parameter (object-initializer `new Parameter_X { ... }` list literals).

## Data Rules (CRITICAL)

1. Every numeric value must **exactly match** the original problem description. No rounding, no estimation, no placeholders.
2. Every declared set and parameter must be populated. `LoadInline`/`LoadFrom` throw immediately on an empty sequence, so an empty set is a hard failure, not a silent gap.
3. If data is not explicitly stated, infer from context (categories, units, described entities).
4. **NEVER hand-write validation logic** in the Dataload constructor or anywhere else — no `ValidateSetsCoverParameters()`, no manual "does every parameter reference an existing set member" loop, no manual duplicate-key check. `DataContext` does all of this automatically once construction goes through `OptData.Load`: it aggregates referential-integrity, duplicate-key, and numeric-sanity checks and throws one `DataValidationException` listing every issue at once. A hand-written version duplicates framework logic and will drift from it.
5. Any ratio **derived from data** (e.g. a Big-M bound computed from capacity/hours) MUST go through `Numeric.SafeRatio(numerator, denominator, context: "...")` instead of a raw `/` — it guards divide-by-zero, non-finite results, and unreasonably large derived constants.

## Constructor Syntax

```csharp
// Set bricks — declare once, populate with LoadInline (literal members straight from the problem description)
public Set_Product PRODUCT = new();
public Set_Machine MACHINE = new();

public Dataload()
{
    PRODUCT.LoadInline("Condos", "DetachedHouse");
    MACHINE.LoadInline("Cutting", "Assembly");

    // Scalar parameter (no index) — still a List<>, exactly one entry
    parameter_Budget.Add(new Parameter_Budget { QTY = 760000.0 });

    // 1D indexed parameter
    parameter_Profit.AddRange(new[]
    {
        new Parameter_Profit { Product = "Condos", QTY = 0.5 },
        new Parameter_Profit { Product = "DetachedHouse", QTY = 1.0 },
    });

    // 2D indexed parameter
    parameter_Cost.AddRange(new[]
    {
        new Parameter_Cost { Origin = "CityA", Destination = "CityB", QTY = 10.0 },
        new Parameter_Cost { Origin = "CityA", Destination = "CityC", QTY = 15.0 },
    });

    // Set derived from parameter data already built above (no independent literal list)
    // ITEM.LoadFrom(parameter_Demand.Select(p => p.Item).Distinct());
}
```

- Use the **object-initializer** form (`new Parameter_X { PropName = value, ... }`) — property names are the PascalCase names declared via `[OptDim<...>("Name")]` in Stage 5, not positional constructor arguments.
- `LoadInline(...)` / `LoadFrom(...)` throw immediately on empty input or duplicate members — that already covers "every set is populated with unique members"; don't add your own check on top.

## Identifier Naming

- Set members: CamelCase, first letter uppercase (`"Condos"`, `"DetachedHouse"`)
- ❌ `"condos"` / `"detached_house"` / `"Trucks"` / `"Vans"`
- ✅ Collections: `"TruckFleet"`, `"VehicleGroup"`
- All numeric literals must include decimal point (`760000.0` not `760000`)
- Percentages as decimals (`0.15` not `15%`)

## Full Example

```csharp
using OptimFoundation.Core;
using OptimFoundation.Modeling;
using InvestmentPlan.Parameter;

namespace InvestmentPlan.Set
{
    [OptSet<string>]
    public partial class Set_InvestmentType { }

    public partial class Dataload : DataContext
    {
        public Set_InvestmentType INVESTMENT_TYPE = new();

        public List<Parameter_Budget> parameter_Budget = new();
        public List<Parameter_Profit> parameter_Profit = new();

        public Dataload()
        {
            INVESTMENT_TYPE.LoadInline("Condos", "DetachedHouse");

            parameter_Budget.Add(new Parameter_Budget { QTY = 760000.0 });

            parameter_Profit.AddRange(new[]
            {
                new Parameter_Profit { InvestmentType = "Condos", QTY = 0.5 },
                new Parameter_Profit { InvestmentType = "DetachedHouse", QTY = 1.0 },
            });
        }
    }
}
```

Note: there is no `ValidateXxx()` method anywhere in this class. Construction always happens via `OptData.Load(() => new Dataload())` in Program.cs (Stage 13), which runs the framework's validation right after this constructor returns.

## Do NOT emit — outdated API

- ❌ `public class Dataload` (missing `partial` and `: DataContext`) — the generator's registration hook never fires, so the class silently gets zero validation.
- ❌ `new Dataload()` presented as the way to construct it anywhere in application code — always `OptData.Load(() => new Dataload())`.
- ❌ Hand-rolled validation methods (`ValidateSetsCoverParameters`, manual dangling-reference loops, manual duplicate-key checks) — the framework already does this; do not re-implement it.
- ❌ Raw `public List<string> SET_NAME = new();` for sets — always a `Set_<Name>` brick (`[OptSet<T>]`).

---

## Inputs

### Problem Description
{{ProblemDescription}}

### Model
{{Model}}

### Parameter Classes
```csharp
{{ParamClasses}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
