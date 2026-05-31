# FactorioOptimization 專案規則

## 問題概述

Balanced Petroleum Processing Optimization（BPPO）
- 煉油廠輸出重油:輕油:天然氣 = 5:9:11，消耗必須維持此比例
- 石油上限：958.3
- 潤滑劑化工廠固定 20 台（c1 = 20）
- 目標：最大化火箭燃料產量

## 問題類型

MIP — 機台台數（VariableI_Machine）為整數，資源流量（VariableX_Resource）為連續

## 執行流程

```
Program.cs
  └─ FactorioOptimizationProblem.Execute()
       ├─ VariableCreate.Build()     建機台（整數）+ 資源（連續）變數
       └─ BuildModel.Build()
            ├─ ObjectiveFunction     max R
            ├─ Constraint_InputCap       C1 石油上限、C2 固定潤滑廠
            ├─ Constraint_ResourceFlowDef C3~C8 資源流量定義
            ├─ Constraint_ResourceCap    C9~C11 資源可用上限
            ├─ Constraint_OilRatio       C12~C13 5:9:11 比例
            └─ Constraint_Downstream     C14 固體燃料消耗
```

## 機台與資源命名

| 變數 | MachineType / ResourceType |
|------|---------------------------|
| x    | Refinery |
| c1   | ChemPlant_Lube |
| c2   | ChemPlant_LightSolid |
| c3   | ChemPlant_GasSolid |
| c4   | ChemPlant_HeavySolid |
| a    | Assembler_Rocket |
| H    | HeavyOil |
| L    | LightOil |
| G    | Gas |
| S    | SolidFuel |
| P    | Lubricant |
| R    | RocketFuel |
