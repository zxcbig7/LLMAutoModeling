# Stage 5: Parameter Classes (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Convert the Model `param` declarations into C# Parameter classes, one file per parameter, under `Parameter/` (`namespace <ProjectName>.Parameter`).

```csharp
using OptimFoundation.Modeling;
using <ProjectName>.Set;

namespace <ProjectName>.Parameter
{
    <Design Parameter classes here>
}
```

## Rules

- Only generate code for `param` declarations (ignore `set`, `var`, objective, constraints).
- Class name format: `Parameter_<ParamName>` in PascalCase (full word `Parameter_`, not `Param_`).
- **Default path (paved path — use this unless told otherwise)**: source generator. Declare a bare `[OptParam]` + one `[OptDim<Set_X>("Name")]` attribute per index set; the generator emits the properties, `QTY`, and constructors. Do NOT hand-write the class body.
  - `[OptDim<TSet>("Name")]`'s generic argument MUST reference an existing `Set_<Name>` brick (`[OptSet<T>]`, defined in Stage 7's `Set/`).
  - Property name = the string passed to `[OptDim<...>("Name")]`, in PascalCase (e.g. `"Product"`, `"MachineId"`) — NOT `ALL_CAPS_WITH_UNDERSCORES`.
  - Missing `[OptParam]` on a `Parameter_*` class surfaces as **compile error `OPTF006`** once that class is referenced as a `List<Parameter_*>` field in `Dataload : DataContext` (Stage 7) — which it always will be in this pipeline — so always include it rather than relying on the error to catch it later.
  - `[FullGrid]` is **opt-in**: add it only when the parameter's semantics require a value for every combination of its index sets (e.g. a capacity that must be known for every machine × date × shift). Most MILP parameters are sparse by design — do NOT add `[FullGrid]` unless the problem explicitly needs full coverage.
- **Fallback** (only when the generator genuinely doesn't fit, e.g. index sets not known until runtime): hand-write `: ParameterBase`, PascalCase properties matching set names, **no constructor** (the framework builds the parameter's lookup key via reflection off property declaration order), `QTY` last, `= string.Empty` default on string properties to satisfy nullable-reference warnings.

### Property Naming
- One property per index set, named by the **dimension name** passed to `[OptDim<Set_X>("Name")]` (PascalCase).
- `double QTY` — always the last member; the generator adds it automatically (default `HasValue = true`, so you never need to write it yourself).

## Class Structure (paved path — source generator)

```csharp
using OptimFoundation.Modeling;
using <ProjectName>.Set;

namespace <ProjectName>.Parameter
{
    [OptParam]
    [OptDim<Set_SetName1>("SetName1")]
    [OptDim<Set_SetName2>("SetName2")]
    public partial class Parameter_ParamName { }
}
```

## Class Structure (fallback — hand-written)

```csharp
using OptimFoundation.Core;

namespace <ProjectName>.Parameter
{
    public class Parameter_ParamName : ParameterBase
    {
        public string SetName1 { get; set; } = string.Empty;
        public int    SetName2 { get; set; }
        public double QTY      { get; set; }
    }
}
```

## Example

**Model**: `param CostPerProduct {ProductType, MachineID};`

**C#** (assumes `Set_ProductType` / `Set_MachineId` bricks already exist from Stage 7):
```csharp
using OptimFoundation.Modeling;
using <ProjectName>.Set;

namespace <ProjectName>.Parameter
{
    [OptParam]
    [OptDim<Set_ProductType>("ProductType")]
    [OptDim<Set_MachineId>("MachineId")]
    public partial class Parameter_CostPerProduct { }
}
```

## Do NOT emit — outdated API

- ❌ `Param_XXX` class name, or a hand-rolled constructor `Param_XXX(params object[] sets) => InitClassBySets(sets)` — superseded naming/pattern.
- ❌ `ALL_CAPS_WITH_UNDERSCORES` property names (`PRODUCT_TYPE`, `MACHINE_ID`) — current convention is PascalCase dimension names.
- `[OptParam("Date:DateTime", "Group")]` (string-form) / `[OptParam<Set_Date, Set_Group>]` (multi-generic form, arity 1..6) — **not deprecated, not errors**: the generator still fully supports both and they remain legal, compilable code. This pipeline simply always emits the `[OptParam]` + `[OptDim<TSet>("Name")]` form instead, for consistent per-dimension role naming — do not describe either form as outdated/removed/wrong if you encounter them in existing code.

---

## Input

### Model
```
{{Model}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
