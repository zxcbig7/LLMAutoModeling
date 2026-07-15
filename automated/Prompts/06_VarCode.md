# Stage 6: Variables.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Convert Model `var` declarations into C# Variable classes.

```csharp
using OptimFoundation.Core;

namespace Model
{
    <Design Variable classes here>
}
```

## Rules

- Only process `var` declarations (ignore `set`, `param`, objective, constraints).
- Each var → class inheriting from `VariableBase`.
- Class name:
  - Continuous (`>= 0` or bounded) → `VariableX_<Name>`
  - Integer or binary → `VariableY_<Name>`
- Each index set → one property named after the **set name** (ALL UPPERCASE with underscores).
- No `QTY`, `Value`, or data properties — variables hold no data.
- If no index set → class with no properties except constructor.

## Class Structure

```csharp
public class VariableX_<VarName> : VariableBase
{
    #region Members
    public string SET_NAME_1 { get; set; }
    public string SET_NAME_2 { get; set; }
    #endregion

    #region Constructors
    public VariableX_<VarName>(params object[] sets) => InitClassBySets(sets);
    public VariableX_<VarName>() { }
    #endregion
}
```

## Examples

**Model**: `var UnitsShipped {CABIN, PRODUCT} integer >= 0;`
```csharp
public class VariableY_UnitsShipped : VariableBase
{
    #region Members
    public string CABIN { get; set; }
    public string PRODUCT { get; set; }
    #endregion

    #region Constructors
    public VariableY_UnitsShipped(params object[] sets) => InitClassBySets(sets);
    public VariableY_UnitsShipped() { }
    #endregion
}
```

**Model**: `var TotalCost >= 0;` (no index set)
```csharp
public class VariableX_TotalCost : VariableBase
{
    #region Constructors
    public VariableX_TotalCost(params object[] sets) => InitClassBySets(sets);
    public VariableX_TotalCost() { }
    #endregion
}
```

---

## Input

### Model
```
{{Model}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
