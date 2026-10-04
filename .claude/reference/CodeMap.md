---
title: AI-Modeling architecture map
updated: 2026-09-28
---

# CodeMap

## Dependency graph

```mermaid
flowchart TD
    CSV[CSV / DB / InMemory] --> Source[IDataSource]
    Source --> Load[Load T]
    Load --> Data[Dataload : DataContext]
    Data --> Guard[OptData.Load]
    Guard --> Model[OptModel]
    Model --> Vars[BuildVars]
    Model --> Objective[ObjectiveFunction]
    Model --> Constraints[Constraint classes]
    Project[OptProject] -->|Solve model, config| Solve[OptEngine.RunModel]
    Model --> Solve
    Project -->|Experiment name| Experiment[OptExperiment]
    Model --> Experiment
    Solve --> Solution[ISolutionSink / Trial]
    Experiment -->|Run| Metrics[Experiment / Trial / SolveMetrics]
```

`OptData.Load` 自動驗證 Set／Parameter duplicate key 與 Parameter numeric sanity，再 freeze framework-controlled registration。Parameter→Set relation 由開發者掌握；`FindParameterOrLog` 在缺值時留下 Warning。

## Project structure

```text
Projects/<Project>/
├── Model/<Project>_Model.md
├── Set/Set_*.cs
├── Parameter/Parameter_*.cs
├── Variable/Variable[B|C|I]_*.cs
├── Objective/ObjectiveFunction.cs
├── Constraint/Constraint_*.cs
├── Solution/<Project>Solution.cs
├── Data/Dataload.cs
├── Data/*.csv
├── Program.cs
└── <Project>.csproj
```

## Data API

| Symbol | Role |
| --- | --- |
| `[OptSet]` + primitive `OptDim` | 一到多維 Set row |
| `[OptParam]` + primitive `OptDim` | 零到多維 Parameter row + QTY |
| `IDataSource.Load<T>` | Set/Parameter 共用 typed load |
| `CsvCtrl.WriteRows<T>` | Set/Parameter 共用 canonical CSV output |
| `CsvDataSource` | `Input/{name}.csv`（= `FolderDir.Input`，執行檔目錄下） |
| `InMemoryDataSource` | `AddRows` 註冊資料 |
| `DbDataSource` | query-only source；名稱引數是 SQL |

## Model API

| Symbol | Role |
| --- | --- |
| `[OptVar]` + B/C/I prefix | generated variable row 與型別 |
| `BuildVars<T>` | 一般 variable build 入口 |
| `BuildBVs/CVs/IVs` | 明確型別與 bounds 進階入口 |
| `AddLHS` / `AddRHS` | 保留原數學式左右側 |
| `CreateXxx(this, dims...)` | canonical constraint naming |
| `OptModel` | Variables → Objective → Constraints 模型定義 |

## Runner/config API

| Symbol | Role |
| --- | --- |
| `ProjectConfig` | 專案輸出開關（`EnableSolverLog` / `Export*`），不含專案身分 |
| `CplexConfig : ISolverConfig` | solver 行為（182 顆旋鈕） |
| `OptProject` | 專案（名稱、FolderDir 全部資料夾、log、保留期）；`LoadConfig` + `Solve(model, config, onSolved, beforeSolve)` 跑一次，留 `Trial` |
| `OptExperiment` | 由 `project.Experiment(name)` 建立；model × config trials，`Run()` 後 `Experiment.Save()` 輸出 CSV |

## Repository references

| Path | Role |
| --- | --- |
| `.claude/skills/AGENTS.md` | phase gates 與治理 |
| `.claude/skills/coding/optimfoundation-api-guide.md` | Phase 2 API 權威 |
| `.claude/skills/coding/checklist.md` | 驗收 |
| `tutorial(for developer)/` | 人類向教學與 prompt |
| `Template/` | consumer scaffold |
| `Projects/HospitalRostering_Generator/` | 可運作 generator 專案；部分資料建立方式是既有示例，不作 IO scaffold |
| `dlls/` | framework binary snapshot |

AI-Modeling 是 DLL consumer，framework references 使用 repo-local `HintPath`，generator 使用 Analyzer reference；不跨 repository 建 `ProjectReference`。
