# Stage 8: Constraint Code (OptimFoundation CPLEX)

## Role
You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task
Convert the mathematical constraints from the Model into **C# Constraint classes** using the `OptimFoundation.Cplex` API, under `Constraint/` (`namespace <ProjectName>.Constraint`).

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;
using <ProjectName>.Set;
using <ProjectName>.Parameter;
using <ProjectName>.Variable;

namespace <ProjectName>.Constraint
{
    <Design the constraints here>
}
```

---

## Step 1: Model Constraint Analysis (MANDATORY — do before coding)

Before writing any C# code, list out LHS and RHS terms for each constraint:

```
# Constraint: <ConstraintName>
LHS Terms:
- term 1 (variable × coefficient)
- term 2

RHS Terms:
- constant or parameter value
```

---

## Step 2: Constraint Class Structure

```csharp
public class Constraint_<ConstraintName> : ConstraintBase
{
    private readonly Dataload _dataload;
    private readonly OptEngine _engine;

    public Constraint_<ConstraintName>(Dataload dataload, OptEngine engine)
    {
        _dataload = dataload;
        _engine = engine;
    }

    public void Build()
    {
        // LHS terms → engine.AddLHS(coef, variable)
        // RHS terms → engine.AddRHS(value)
        // CreateGreatEqual / CreateLessEqual / CreateEqual
        // ConstraintCount++ once per created constraint
        // Logging.Info($"[{ConstraintName}] {ConstraintCount}") at the end
    }
}
```

**Class name must exactly match the constraint name in the model.**

---

## LHS / RHS Rules (CRITICAL)

- Model left-hand side → `engine.AddLHS(coefficient, new VariableX_Name { ... })`
- Model right-hand side → `engine.AddRHS(value)`
- **ABSOLUTELY NO**: moving terms across sides, negating coefficients, or merging expressions

### Constraint Direction

| Model operator | C# method |
|---|---|
| `>=` | `engine.CreateGreatEqual("Name")` |
| `<=` | `engine.CreateLessEqual("Name")` |
| `=` | `engine.CreateEqual("Name")` |

**Never guess or flip the comparison direction.**

---

## Loop Structure

Match Model index sets. Iterate `Set_*` bricks with plain `foreach` — they are `IReadOnlyList<T>` / `IEnumerable<T>`, **not** `List<T>`, so `.ForEach(...)` is not available on them:

```csharp
// Model: subject to C1 {i in Set1, j in Set2}: ...
foreach (var i in _dataload.SET1)
{
    foreach (var j in _dataload.SET2)
    {
        var coef = _dataload.parameter_Coef.FirstOrDefault(x => x.Set1 == i && x.Set2 == j)?.QTY ?? 0.0;
        _engine.AddLHS(coef, new VariableX_Amount { Set1 = i, Set2 = j });
    }
    _engine.AddRHS(rhsValue);
    _engine.CreateLessEqual($"{ConstraintName}@{i}");
    ConstraintCount++;
}
```

- Variable instances are always built with the **object-initializer** form (`new VariableX_Amount { Set1 = i, Set2 = j }`), matching the properties the Variable class declares via `[OptDim<...>("Name")]` — never a positional constructor.
- `DateTime`-typed index values format as `{date:yyyy-MM-dd}` in constraint-name strings (e.g. `$"{ConstraintName}@{date:yyyy-MM-dd}"`).
- **If no index set → no loop, write terms directly.**

---

## Parameter Retrieval Rules

- ALWAYS retrieve parameter values via LINQ into a local variable FIRST.
- NEVER embed LINQ inside `AddLHS(...)` or `AddRHS(...)`.

```csharp
// ✅ Correct
var budget = _dataload.parameter_Budget.FirstOrDefault(x => x.Set1 == i)?.QTY ?? 0.0;
_engine.AddRHS(budget);

// ❌ Wrong
_engine.AddRHS(_dataload.parameter_Budget.FirstOrDefault(x => x.Set1 == i)?.QTY ?? 0.0);
```

Default values:
- Missing lower bound → `?? 0.0`
- Missing upper bound → `?? 1E100`
- Missing ratio → `?? 1.0`

---

## String Matching

Set member strings must be **CamelCase** in comparisons:

```csharp
// ✅ Correct
x.ResourceType == "NitrousOxide"

// ❌ Wrong
x.RESOURCE_TYPE == "Nitrous Oxide"
```

Property names on Parameter/Variable classes are always the PascalCase dimension name declared via `[OptDim<...>("Name")]` in Stage 5/6 (`x.ResourceType`, `x.Product`) — never `ALL_CAPS_WITH_UNDERSCORES` (`x.RESOURCE_TYPE`).

---

## Zero / Infinity Rules

- Do NOT add `AddLHS(0.0, ...)` — skip terms with zero coefficients.
- Do NOT add `AddRHS(1E100)` — skip unconstrained upper bounds.
- Do NOT pass `null` as a variable to `AddLHS` or `AddRHS`.

---

## Do NOT emit — outdated API

- ❌ `dataload.Set1.ForEach(i => { ... })` — `Set_*` bricks don't have `.ForEach`; use `foreach (var i in dataload.SET1)`.
- ❌ `new VariableX_Amount(i, j)` positional constructor — always the object-initializer form with PascalCase property names.
- ❌ `x.SET1 == i` / `x.RESOURCE_TYPE` ALL-CAPS property references.

---

## Inputs

### Model
{{Model}}

### Dataload Class
```csharp
{{DataloadClass}}
```

### Parameter Classes
```csharp
{{ParamClasses}}
```

### Variable Classes
```csharp
{{VarClasses}}
```

---

Return only clean, complete C# code. No explanation.

Take a deep breath and think step by step.
