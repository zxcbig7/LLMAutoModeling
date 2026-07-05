# Stage 7: Dataload.cs (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Generate the `Dataload` class that loads all sets and parameters from the problem description.

```csharp
using OptimFoundation.Core;
using System.Collections.Generic;

namespace Model
{
    <Dataload class here>
}
```

## Structure Rules

- **Sets** → `public List<string> SET_NAME = new List<string>();`
- **Parameters** → `public List<Param_XXX> param_xxx = new List<Param_XXX>();` (always List, even for scalars)
- Constructor populates all sets and parameters.

## Data Rules (CRITICAL)

1. Every numeric value must **exactly match** the original problem description. No rounding, no estimation, no placeholders.
2. Every declared set and parameter must be populated. No empty `AddRange()`.
3. If data is not explicitly stated, infer from context (categories, units, described entities).

## Constructor Syntax

```csharp
// Sets
PRODUCT_INDEX.AddRange(new[] { "Condos", "DetachedHouse" });

// Scalar parameter (no index)
param_Budget.Add(new Param_Budget(760000.0));

// 1D indexed parameter
param_Profit.AddRange(new[]
{
    new Param_Profit("Condos", 0.5),
    new Param_Profit("DetachedHouse", 1.0)
});

// 2D indexed parameter
param_Cost.AddRange(new[]
{
    new Param_Cost("CityA", "CityB", 10.0),
    new Param_Cost("CityA", "CityC", 15.0)
});
```

## Identifier Naming

- Set members: CamelCase, first letter uppercase (`"Condos"`, `"DetachedHouse"`)
- ❌ `"condos"` / `"detached_house"` / `"Trucks"` / `"Vans"`
- ✅ Collections: `"TruckFleet"`, `"VehicleGroup"`
- All numeric literals must include decimal point (`760000.0` not `760000`)
- Percentages as decimals (`0.15` not `15%`)

## Full Example

```csharp
using OptimFoundation.Core;
using System.Collections.Generic;

namespace Model
{
    public class Dataload
    {
        #region Set Declarations
        public List<string> INVESTMENT_TYPE = new List<string>();
        #endregion

        #region Parameter Declarations
        public List<Param_Budget> param_Budget = new List<Param_Budget>();
        public List<Param_Profit> param_Profit = new List<Param_Profit>();
        #endregion

        public Dataload()
        {
            // Sets
            INVESTMENT_TYPE.AddRange(new[] { "Condos", "DetachedHouse" });

            // Parameters
            param_Budget.Add(new Param_Budget(760000.0));

            param_Profit.AddRange(new[]
            {
                new Param_Profit("Condos", 0.5),
                new Param_Profit("DetachedHouse", 1.0)
            });
        }
    }
}
```

---

## Inputs

### Problem Description
{{ProblemDescription}}

### AML Model
{{AMLModel}}

### Parameter Classes
```csharp
{{ParamClasses}}
```

Return code only (no explanation).

Take a deep breath and think step by step.
