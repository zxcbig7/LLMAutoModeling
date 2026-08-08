# Stage 3: Key Info Extraction (JSON)

## Role
You are an expert in mathematical programming.

## Task
Analyze the model description and extract all relevant sets, parameters, variables, objectives, and constraints into structured JSON format.

## Chain-of-Thought Steps

**Step 1 — Identify Sets**
- Look for all entity categories (products, machines, time periods).
- For each: Name, DataType (STR / INT / DOUBLE / DATE), Description.

**Step 2 — Identify Parameters**
- Find all problem constants (capacities, costs, limits).
- For each: Name, indexing sets (Dim), Description.

**Step 3 — Identify Decision Variables**
- Find all decision quantities.
- For each, determine:
  - Dim: which sets it depends on
  - Type: `NUM` (continuous) / `INT` (integer) / `BIN` (binary)
  - LB and UB (default: LB=0, UB="INFTY" if not specified)
  - If bounded between 0 and 1 → still `NUM`, describe as "proportional"

**Step 4 — Identify the Objective Function**
- Goal: Maximize or Minimize, and what.

**Step 5 — Identify Constraints**
- For each: Description, Dim (index sets), Type (from classification list below).

## Constraint Types
UB / LB / Balance / Proportional / Conjunction / Disjunction / Exclusive XOR / Implication / Conditional Activation

## Output JSON Format

```json
{
  "Sets": [
    { "Name": "<SetName>", "Type": "STR|INT|DOUBLE|DATE", "Description": "<desc>" }
  ],
  "Parameters": [
    { "Name": "<ParamName>", "Dim": ["<Set>"], "Description": "<desc>" }
  ],
  "Variables": [
    {
      "Name": "<VarName>",
      "Dim": ["<Set>"],
      "Type": "NUM|INT|BIN",
      "Description": "<desc>",
      "LB": 0,
      "UB": "INFTY"
    }
  ],
  "Objective": {
    "Description": "<desc>",
    "Sense": "Maximize|Minimize"
  },
  "Constraints": [
    { "Description": "<desc>", "Dim": ["<Set>"], "Type": "<ConstraintType>" }
  ]
}
```

## Naming Rules
- All identifiers: CamelCase, first letter uppercase.
- Prefer singular: `Product` not `Products`.
- Collections: use compound words (`TruckFleet`, `VehicleGroup`).
- Set member strings: CamelCase (`"Condos"`, `"DetachedHouse"`).

---

## Input

{{ModelDescription}}

---

Take a deep breath and think step by step.
