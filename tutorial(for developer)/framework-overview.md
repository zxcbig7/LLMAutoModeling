# 框架全景：AI 建模框架 × OptimFoundation

自然語言題目 → AI 建模框架（三階段 phase gate）→ 生成 C# 專案 → 消費底層 OptimFoundation MILP 框架求解。

```mermaid
%%{init: {'theme':'base','themeVariables':{
  'fontFamily':'ui-sans-serif, -apple-system, Segoe UI, Roboto, sans-serif',
  'fontSize':'14px',
  'primaryColor':'#eef2ff',
  'primaryTextColor':'#1e293b',
  'primaryBorderColor':'#6366f1',
  'lineColor':'#94a3b8',
  'secondaryColor':'#f1f5f9',
  'tertiaryColor':'#f8fafc'
},'flowchart':{'curve':'basis','nodeSpacing':50,'rankSpacing':60,'htmlLabels':true}}}%%
flowchart TD
  IN["自然語言<br/>最佳化題目"]

  subgraph AI["AI 建模開發框架 · AI-Modeling"]
    direction TB
    subgraph INT["唯一路線：三階段 phase gate"]
      direction LR
      M1["Phase 1 建模<br/>Model Design"] --> M2["Phase 2 轉譯<br/>Coding"] --> M3["Phase 3 調校<br/>Tuning"]
    end
  end

  CS["生成的 C# 專案<br/>Model/ + Project/ + Program.cs<br/>繼承 OptEngine"]

  subgraph OF["OptimFoundation · solver-agnostic MILP 框架"]
    direction TB
    CORE["Core<br/>EngineBase・ISolverEngine<br/>VariableBuilder・Experiments"]
    subgraph BACK["Solver 後端（可插拔）"]
      direction LR
      CPX["Cplex<br/>OptEngine・CplexConfig"]
      GRB["Gurobi<br/>OptEngine・GurobiConfig"]
      SLV["Solver<br/>變體"]
    end
    GEN["Generators<br/>AutoSetsGenerator"]
    DBO["Db.Oracle<br/>OracleDBCtrl"]
    CORE --> BACK
    CORE --> GEN
    CORE --> DBO
  end

  IN --> INT
  INT --> CS
  CS -->|"繼承 / 呼叫 Pool API"| CORE
  CS -.->|"目標後端"| CPX

  classDef primary fill:#eef2ff,stroke:#6366f1,stroke-width:2px,color:#3730a3;
  classDef success fill:#ecfdf5,stroke:#10b981,stroke-width:2px,color:#065f46;
  classDef accent fill:#eff6ff,stroke:#3b82f6,stroke-width:2px,color:#1e40af;
  classDef muted fill:#f8fafc,stroke:#cbd5e1,stroke-width:1px,color:#64748b;

  class IN primary;
  class M1,M2,M3 primary;
  class CS success;
  class CORE primary;
  class CPX,GRB,SLV accent;
  class GEN,DBO muted;
```

## 圖例

| 顏色 | 含義 |
| --- | --- |
| 靛藍（primary） | 入口 / 三階段主流程 / 核心模組 |
| 藍（accent） | 可插拔 Solver 後端 |
| 綠（success） | 產出物：生成的 C# 專案 |
| 灰（muted） | 支援模組（source generator、Oracle 資料層） |

## 兩層各含什麼

**AI 建模開發框架（上層）** — 把自然語言題目變成可求解 C# 專案。**唯一路線是三階段 phase gate**，依序推進、每階段之間有 gate：

- **Phase 1 建模**：自然語言 → `Model/<Project>_Model.md`，停在使用者確認
- **Phase 2 轉譯**：Model.md 逐條機械翻譯成 C#，build 綠 + 解驗證協定四步全過
- **Phase 3 調校**：模型與資料凍結，只調 solver 旋鈕（使用者提出才做）

沒有免 gate 的全自動量產路線——跳過 gate 等於放棄整條 pipeline 的驗證能力。

**OptimFoundation（下層）** — solver-agnostic MILP 框架（C# / .NET 8）：
- **Core**：EngineBase、ISolverEngine、VariableBuilder、Experiments、Csv/Db/Logging 工具
- **Solver 後端**：Cplex、Gurobi、Solver 變體（同一套 API、可換後端）
- **Generators**：AutoSetsGenerator（source generator）
- **Db.Oracle**：OracleDBCtrl（Oracle 資料存取）

**銜接**：生成的 C# 專案繼承 `OptEngine`、用 Pool API（`AddLHS`/`AddRHS`）建模，目標後端為 CPLEX。
