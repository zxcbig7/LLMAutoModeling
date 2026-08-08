# Stage 6: Variable Classes (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Convert Model `var` declarations into C# Variable classes, one per variable, under `Variable/` (`namespace <ProjectName>.Variable`).

```csharp
using OptimFoundation.Modeling;
using <ProjectName>.Set;

namespace <ProjectName>.Variable
{
    <Design Variable classes here>
}
```

## Rules

- Only process `var` declarations (ignore `set`, `param`, objective, constraints).
- Class name prefix is **load-bearing** — it decides the variable's solver type, not just its name:
  - `VariableB_<Name>` → Binary (0/1)
  - `VariableX_<Name>` → Continuous
  - `VariableI_<Name>` → Integer
  - A prefix outside these three is a generator **compile error `OPTF001`**.
- **Default path (paved path)**: source generator. Declare a bare `[OptVar]` + one `[OptDim<Set_X>("Name")]` per index set (attribute order = key order). Do NOT hand-write the class body.
- No `QTY`, `Value`, or data properties — variables hold no data, only key dimensions.
- No index set → bare `[OptVar]` with no `[OptDim]` at all: a 0-dimensional (scalar) variable, key = class name only, no `@` suffix.
- **Fallback** (only when the generator genuinely doesn't fit): hand-write `: VariableBase`, PascalCase properties, **no constructor** — the framework builds the variable's key via reflection off property declaration order, so a hand-written class must never declare one.

## Class Structure (paved path — source generator)

```csharp
using OptimFoundation.Modeling;
using <ProjectName>.Set;

namespace <ProjectName>.Variable
{
    [OptVar]
    [OptDim<Set_SetName1>("SetName1")]
    [OptDim<Set_SetName2>("SetName2")]
    public partial class VariableX_VarName { }
}
```

## Class Structure (fallback — hand-written)

```csharp
using OptimFoundation.Core;

namespace <ProjectName>.Variable
{
    public class VariableI_VarName : VariableBase
    {
        public string SetName1 { get; set; } = string.Empty;
        public string SetName2 { get; set; } = string.Empty;
    }
}
```

## Examples

**Model**: `var UnitsShipped {CABIN, PRODUCT} integer >= 0;`
```csharp
[OptVar]
[OptDim<Set_Cabin>("Cabin")]
[OptDim<Set_Product>("Product")]
public partial class VariableI_UnitsShipped { }
```

**Model**: `var TotalCost >= 0;` (no index set, continuous)
```csharp
[OptVar]
public partial class VariableX_TotalCost { }
```

**Model**: `var UseRoute {ORIGIN, DEST} binary;`
```csharp
[OptVar]
[OptDim<Set_Origin>("Origin")]
[OptDim<Set_Dest>("Dest")]
public partial class VariableB_UseRoute { }
```

## Do NOT emit — outdated API

- ❌ The old two-prefix scheme `VariableX_<Name>` (continuous) / `VariableY_<Name>` (integer-or-binary) — that scheme no longer exists. Current framework uses **three** prefixes: `VariableB_` (binary) / `VariableX_` (continuous) / `VariableI_` (integer). Never emit a `VariableY_` class.
- ❌ `ALL_CAPS_WITH_UNDERSCORES` property names (`CABIN`, `PRODUCT`) — PascalCase dimension names only.
- ❌ Hand-written constructor `VariableX_VarName(params object[] sets) => InitClassBySets(sets)` — Variable classes never declare a constructor.
- `[OptVar("Date:DateTime")]` (string-form) / `[OptVar<Set_A, Set_B>]` (multi-generic form, arity 1..6) — **not deprecated, not errors**: the generator still fully supports both and they remain legal, compilable code. This pipeline simply always emits the `[OptVar]` + `[OptDim<TSet>("Name")]` form instead, for consistent per-dimension role naming — do not describe either form as outdated/removed/wrong if you encounter them in existing code.

---

## Input

### Model
```
{{Model}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
