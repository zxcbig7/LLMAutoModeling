# Stage 2: Standard Model

## Role
You are an expert in mathematical programming.

## Task
Convert the simplified problem description into a standardized general optimization model description.

## Requirements

### 0. Translation and Standardization
- Translate to English (if needed), ensuring accuracy and clarity.
- Replace real-world labels (e.g., "January", "Product A") with generic indexed notation (e.g., "Period 1", "Product 1").

### 1. Preserve Original Association
- Keep together any numerical value and its associated context/unit.
- Do NOT infer derived rates unless explicitly stated.

### 2. Extract Both Raw and Structural Information
For each numeric concept:
- Raw phrasing as-is
- Unit structure (dollars/shift, hours/shift)
- Associated dimension (time, quantity, money)
- Role: parameter / decision variable / derived variable / conversion factor

### 3. Avoid Automatic Computation
- Never compute derived values unless explicitly stated.
- If derivation is required, create a "derived" row with traceable note.

### 4. Semantic Discrimination
Every sentence must be classified as:
- **Parameter** (input constant)
- **Variable** (decision quantity)
- **Derived** (calculated from others)
- **Constraint** (limitation or requirement)
- **Objective** (what to optimize)

### 5. Constraint Classification
Identify and classify each constraint:
- **UB / LB**: "at most", "at least", "no more than"
- **Balance**: "must equal", "total input = total output"
- **Proportional**: "at least X times", "in proportion to"
- **Conjunction**: "only if all", "must all be active"
- **Disjunction**: "at least one of"
- **Exclusive XOR**: "exactly one must be selected"
- **Implication**: "if... then..."
- **Conditional Activation**: "only if", "governed by trigger"

## Output Format

**(1) Terminology Mapping Table**

| Generalized Term | Type | Unit | Derived? | Raw Description | Inference Note |
|---|---|---|---|---|---|
| ... | param / var / set | ... | Yes/No | ... | ... |

**(2) General Model Description**
- **Objective**: Full paragraph
- **Elements**: Full paragraph
- **Constraints**: Consolidated constraint descriptions using classification categories

---

## Input

{{ModelDescription}}

---

Take a deep breath and think step by step.
