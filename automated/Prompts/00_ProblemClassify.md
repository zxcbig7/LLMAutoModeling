# Stage 0: Problem Classification

## Role
You are an expert in mathematical programming problem classification.

## Task
Classify the optimization problem into one of three types: **LP**, **IP**, or **MILP**.

## Definitions

- **LP** (Linear Program): All decision variables are continuous. No integer or binary variables.
- **IP** (Integer Program): All decision variables are integers or binary. No continuous variables.
- **MILP** (Mixed-Integer Linear Program): Mix of continuous and integer/binary variables.

## Chain-of-Thought Steps

**Step 1: Identify all decision variables mentioned.**
- What quantities does the problem ask us to decide?

**Step 2: Determine the type of each variable.**
- Is it a quantity that can take fractional values? → Continuous
- Is it a count of whole items? → Integer
- Is it a yes/no decision (use or not use)? → Binary

**Step 3: Classify.**
- All continuous → LP
- All integer/binary → IP
- Mix → MILP

## Output Format

Return a single JSON object:

```json
{
  "ProblemType": "LP|IP|MILP",
  "Reasoning": "One sentence explaining why."
}
```

## Impact on Implementation

The classification affects `CplexConfig` settings in the generated project:

- **LP**: `workThreads=4`, `timeLimit=300`, `mipEmphasis=0`, no MIP-specific settings needed
- **IP**: `workThreads=8`, `timeLimit=1800`, `epGap=1e-4`, `mipEmphasis=1`
- **MILP**: `workThreads=8`, `timeLimit=3600`, `epGap=1e-4`, `mipEmphasis=2`

（此表與 `../CLAUDE.md` §Stage 00 表格為同一份設定，MUST 同步——`../CLAUDE.md` 為權威。）

---

## Problem Description

{{ProblemDescription}}

---

Return the JSON only. No explanation outside the JSON.
