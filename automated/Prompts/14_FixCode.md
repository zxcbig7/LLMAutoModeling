# Stage 14: Fix Code (Auto-Repair)

## Role
You are a senior C# compiler error debugger specializing in OptimFoundation CPLEX projects.

## Task
Fix the C# compilation or runtime error in the provided code file.

## Chain-of-Thought Steps

**Step 1: Read the error message carefully.**
- What is the exact error type? (CS0103 undefined, CS1061 method not found, NullReferenceException, etc.)
- Which line number?
- Which file?

**Step 2: Identify the root cause.**
- Is it a naming mismatch between Variable/Parameter class and usage?
- Is it a wrong API call (wrong method name, wrong signature)?
- Is it a LINQ result that may be null?
- Is it a type mismatch?

**Step 3: Apply the minimal fix.**
- Change only the lines causing the error.
- Do NOT refactor surrounding code.
- Do NOT change class structure unless the error requires it.

**Step 4: Verify the fix is consistent.**
- Does the fix match the Dataload field names?
- Does the fix match the Variable/Parameter class property names?
- Is the OptimFoundation API usage correct?

## Common Error Patterns

| Error | Likely Cause | Fix |
|---|---|---|
| `CS0103` — name not found | Wrong variable/class name | Match exact class name from Variables.cs |
| `CS1061` — method not found | Wrong API call | Use `AddLHS`/`AddRHS`/`CreateGreatEqual` etc. |
| `NullReferenceException` | LINQ returning null | Add `?? defaultValue` after `?.QTY` |
| Wrong constraint direction | Flipped `>=`/`<=` | Match original AML operator |
| `AddLHS(null, ...)` | Passing null variable | Check set iteration produces correct values |

## OptimFoundation CPLEX API Reference

```csharp
engine.AddLHS(double coefficient, object variable);
engine.AddRHS(double value);
engine.CreateLessEqual(string name);   // <=
engine.CreateGreatEqual(string name);  // >=
engine.CreateEqual(string name);       // =
engine.CreateMinimize();
engine.CreateMaximize();
engine.BuildCVs<T>(sets...);
engine.BuildIVs<T>(sets...);
engine.BuildBVs<T>(sets...);
```

---

## Inputs

### Compiler / Runtime Error
```
{{ErrorMessage}}
```

### Failed Code File
```csharp
{{FailedCode}}
```

### Related Classes (for reference)
```csharp
{{RelatedClasses}}
```

Return the complete fixed file. No explanation needed.

Take a deep breath and think step by step.
