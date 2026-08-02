# Stage 11: Program.cs objective and constraint pipeline (OptimFoundation CPLEX)

## Role

You are an expert in C# for OptimFoundation CPLEX optimization projects.

## Task

Generate the `.AddObjective(...)` and `.AddConstraints(...)` chain fragment for `Program.cs`.

```csharp
    .AddObjective(engine =>
        new ObjectiveFunction(engine, data.ITEM, shortagePenalty).Build())
    .AddConstraints(engine =>
        new Constraint_MaxDays(engine, data.ITEM, data.DATE, data.parameter_MaxDays).Build())
    .AddConstraints(engine =>
        new Constraint_Coverage(engine, data.ITEM, data.DATE, data.parameter_Demand).Build());
```

The objective MUST be registered before every constraint. The framework applies all registered phases in `variables → objective → constraints` order.

## Dependency rule

- `ObjectiveFunction` and every `Constraint_Xxx` constructor receive only the `OptEngine`, Set bricks, Parameter lists, scalar values, and bounds they actually use.
- Never pass the whole `Dataload` object to an Objective or Constraint.
- Extract scalar Parameters once in the material section of `Program.cs`, for example `var shortagePenalty = data.parameter_ShortagePenalty.Single().QTY;`, then pass the scalar explicitly.
- Each Objective or Constraint gets its own fluent call. Do not combine multiple calls in a block lambda and do not add a forwarding helper.
- The fluent fragment only instantiates classes and calls `Build()`; raw `AddLHS` / `AddRHS` calls stay inside the Objective or Constraint class.

## Inputs

### Objective class

{{ObjectiveClass}}

### Constraint classes

{{ConstraintClasses}}

### Dataload class

{{DataloadCode}}

Return the scalar extractions plus fluent chain fragment only, with no explanation.
