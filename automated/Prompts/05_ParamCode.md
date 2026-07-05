# Stage 5: Parameters.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Convert the AML `param` declarations into C# Parameter classes.

```csharp
using OptimFoundation.Core;

namespace Model
{
    <Design Parameters classes here>
}
```

## Rules

- Only generate code for `param` declarations (ignore `set`, `var`, objective, constraints).
- Each parameter → C# class inheriting from `ParameterBase`.
- Class name format: `Param_<AMLParamName>` in PascalCase.

### Property Naming
- One property per index set, named by the **set name** (not element values).
- Format: **ALL UPPERCASE with underscores** (e.g., `PRODUCT_INDEX`, `CROP_TYPE`).
- `double QTY` — always the last property, stores the parameter value.

### Constructor
```csharp
public Param_<Name>(params object[] sets) => InitClassBySets(sets);
public Param_<Name>() { }
```

Do NOT include `InitClassBySets` method body — it's inherited from `ParameterBase`.

## Class Structure

```csharp
public class Param_<ParamName> : ParameterBase
{
    #region Members
    public string SET_NAME_1 { get; set; }
    public string SET_NAME_2 { get; set; }
    public double QTY { get; set; }
    #endregion

    #region Constructors
    public Param_<ParamName>(params object[] sets) => InitClassBySets(sets);
    public Param_<ParamName>() { }
    #endregion
}
```

## Example

**AML**: `param CostPerProduct {ProductType, MachineID};`

**C#**:
```csharp
public class Param_CostPerProduct : ParameterBase
{
    #region Members
    public string PRODUCT_TYPE { get; set; }
    public int MACHINE_ID { get; set; }
    public double QTY { get; set; }
    #endregion

    #region Constructors
    public Param_CostPerProduct(params object[] sets) => InitClassBySets(sets);
    public Param_CostPerProduct() { }
    #endregion
}
```

---

## Input

### AML Model
```
{{AMLModel}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
