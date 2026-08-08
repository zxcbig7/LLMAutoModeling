# Stage 7b: Dataload Verification

## Role
You are a senior C# optimization modeling developer and strict validator.

## Task
Review and fix the generated `Dataload.cs` (and its `Set_*` bricks) to ensure it correctly initializes all sets and parameters matching the `Parameter_XXX` class definitions and the original problem data, and that it stays inside the framework's blessed construction path.

---

## Validation Checklist

### 1. Dataload Shape (framework wiring)
- [ ] Class is declared `public partial class Dataload : DataContext` — both `partial` and `: DataContext` present. Missing either means the generator's registration hook never fires and the class gets **zero** validation.
- [ ] No `Validate*` / manual referential-integrity / duplicate-key method anywhere in the file — that logic belongs to the framework (`DataContext.ValidateData`, invoked by `OptData.Load`), never to the project.
- [ ] Every set is a `Set_<Name>` brick (`[OptSet<T>]`), never a raw `List<string>`/`List<DateTime>` field.

### 2. Object-Initializer Matching (property names, not positional)
For each `Parameter_XXX` class, verify the object-initializer property names match the class's `[OptDim<...>("Name")]` declarations exactly (case-sensitive, PascalCase):

```
[OptDim<Set_Product>("Product")] + [OptDim<Set_Machine>("Machine")] on Parameter_Cost
→ new Parameter_Cost { Product = "ProductA", Machine = "Machine1", QTY = 25.0 }  ✅
→ new Parameter_Cost(25.0)                                                       ❌ (positional ctor — wrong pattern)
→ new Parameter_Cost { PRODUCT_TYPE = "ProductA", QTY = 25.0 }                    ❌ (ALL-CAPS name — doesn't exist)
```

Rules:
- **Scalar** (no index sets): `new Parameter_Budget { QTY = 760000.0 }` — QTY only.
- **1D indexed**: `new Parameter_Profit { Product = "Condos", QTY = 0.5 }`.
- **2D indexed**: `new Parameter_Distance { Origin = "CityA", Destination = "CityB", QTY = 10.0 }`.

### 3. Data Coverage
Every set and parameter declared in the Model must be populated:
- [ ] No empty `LoadInline(...)`/`LoadFrom(...)` call on any `Set_*` brick, and no empty `AddRange()`/`Add()` on any `List<Parameter_XXX>`
- [ ] All sets have members
- [ ] All parameter lists have entries
- [ ] Scalar parameters have exactly one entry

Note: `LoadInline`/`LoadFrom` already throw at runtime on an empty sequence or duplicate members — this checklist item catches it statically, before `dotnet run` ever exercises that runtime guard.

### 4. Data Accuracy
- All numeric values must **exactly match** the original problem description
- No rounding, no estimation, no placeholders (0, 1, 999)
- Percentages as decimals (15% → 0.15)
- All numeric literals with decimal point (760000.0 not 760000)
- Any ratio derived from data (Big-M and similar) goes through `Numeric.SafeRatio(num, den, context: "...")`, never a raw `/`

### 5. Identifier Naming
Set member strings must follow CamelCase:
- ✅ `"Condos"`, `"DetachedHouse"`, `"TruckFleet"`
- ❌ `"condos"`, `"detached_house"`, `"Trucks"`

### 6. No Class Definitions
The `Dataload.cs` output must contain ONLY `Set_*` brick declarations + the `Dataload` class.
- ❌ No `Parameter_XXX` class definitions inside `Dataload.cs`
- ✅ Only object-initializer `new Parameter_XXX { ... }` syntax

---

## Output Format

```
## Validation Result: Valid | Invalid

### Issues Found
- Issue 1: Dataload missing `: DataContext` — validation never runs
- Issue 2: Parameter_Cost object-initializer uses ALL-CAPS property name that doesn't exist
- Issue 3: parameter_Demand is empty — no entries added

### Fixes Applied
- Fix 1: Changed `public class Dataload` to `public partial class Dataload : DataContext`
- Fix 2: Renamed initializer keys to match `[OptDim]` names (`Product`, `Machine`)
- Fix 3: Populated param_Demand from problem description table
```

Then return the **complete corrected Dataload.cs** file.

---

## Inputs

### Original Problem Description
{{ProblemDescription}}

### Model (for reference)
{{Model}}

### Parameter Classes
```csharp
{{ParamClasses}}
```

### Dataload.cs (needs verification)
```csharp
{{DataloadCode}}
```

---

Take a deep breath and think step by step.
