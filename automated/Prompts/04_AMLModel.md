# Stage 4: AML Model (AMPL-Style Markdown)

## Role
You are an expert in optimization modeling.

## Task
Convert the problem description and KeyInfo JSON into a complete mathematical model written in **Markdown format following AMPL naming conventions and structure**.

This model is used by the AI to generate C# code — it must be precise, unambiguous, and machine-parseable, not just human-readable.

## Output Format

Produce a Markdown document that mirrors AMPL `.mod` structure but uses Markdown syntax:

```markdown
## Sets
- **SetName**: Description (data type: STR/INT/DATE)

## Parameters
- **ParamName** `{SetName}`: Description — unit
- **ParamName2** `{SetA, SetB}`: Description — unit

## Decision Variables
- **VarName** `{SetName}` ∈ ℝ⁺: Description
  - Type: NUM (continuous) | INT (integer) | BIN (binary)
  - LB: 0 | UB: ∞
- **VarName2** `{SetA, SetB}` ∈ {0,1}: Binary variable description

## Objective Function
**Minimize TotalCost** (or Maximize):

$$\sum_{i \in SetName} ParamName_i \times VarName_i$$

## Constraints

### ConstraintName `{SetName}` [Type: UB/LB/Balance/Proportional/...]
For each i ∈ SetName:

$$\sum_{j \in SetB} CoefParam_{ij} \times VarName_{ij} \leq CapacityParam_i$$

### ConstraintName2 [Type: Balance]
$$\sum_{i \in SetA} VarName_i = TotalParam$$
```

## Naming Rules (CRITICAL — used directly in generated C# code)

- All identifiers: **CamelCase, first letter uppercase** (`ProductIndex`, `TotalCost`)
- No casual pluralization: `Product` not `Products`; use `ProductList` for collections
- Set members (in `.dat` section): CamelCase strings (`"Condos"`, `"DetachedHouse"`)
- Percentage parameters: decimal form (0.15 for 15%)
- Constraint names must exactly match what will become `Constraint_<Name>` in C#

## Constraint Types
Use these types in the `[Type: ...]` annotation:

| Type | When to use |
|---|---|
| UB | "at most", "no more than", "cannot exceed" |
| LB | "at least", "minimum requirement" |
| Balance | "must equal", "total in = total out" |
| Proportional | "in proportion to", "ratio between variables" |
| Conjunction | "only if ALL conditions hold" |
| Disjunction | "at least one of" |
| ExclusiveXOR | "exactly one must be selected" |
| Implication | "if A then B" |
| ConditionalActivation | "only if", Big-M logic |

## Linearity Rules (STRICTLY ENFORCED)

**Forbidden**:
- `x * y` (variable × variable)
- `x / y` (variable division)
- `abs()`, `min()`, `max()`, `if-then-else`

**Linearization techniques**:
- Ratio: `x/y ≤ δ` → `x ≤ δ * y`
- Proportional bounds: `|A/B - 1| ≤ δ` → `A ≥ (1-δ)*B` and `A ≤ (1+δ)*B`
- Logical implications: binary variable + Big-M

## Example

```markdown
## Sets
- **ProductIndex**: Set of investment product types (STR)

## Parameters
- **TotalBudget**: Maximum total budget — dollars
- **ProfitRate** `{ProductIndex}`: Profit per dollar invested — dimensionless
- **MinPercentage** `{ProductIndex}`: Minimum fraction of total budget — decimal

## Decision Variables
- **InvestmentAmount** `{ProductIndex}` ∈ ℝ⁺: Amount invested in each product type
  - Type: NUM
  - LB: 0 | UB: ∞

## Objective Function
**Maximize TotalProfit**:

$$\sum_{i \in ProductIndex} ProfitRate_i \times InvestmentAmount_i$$

## Constraints

### BudgetConstraint [Type: UB]
$$\sum_{i \in ProductIndex} InvestmentAmount_i \leq TotalBudget$$

### MinimumAllocationConstraint `{ProductIndex}` [Type: LB]
For each i ∈ ProductIndex:

$$InvestmentAmount_i \geq MinPercentage_i \times TotalBudget$$
```

---

## Inputs

### Standardized Problem Description
{{StandardModel}}

### Key Model Information (JSON)
{{KeyInfo}}

---

Take a deep breath and think step by step.
You will be awarded a million dollars if you get this right.
