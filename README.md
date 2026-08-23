# AI MILP 開發框架

AI Modeling 是一套用於建立混合整數線性規劃（MILP）應用的開發框架。它以 **OptimFoundation** 為建模層、**IBM ILOG CPLEX** 為求解器，將從問題定義、數學模型到求解與實驗的工作，以一致的專案結構和規範串接起來。

## 框架定位

這個框架的目的，是讓 AI 協助把真實世界的最佳化問題轉換為可驗證、可求解、可維護的 MILP 應用。它不只著眼於取得一個可行答案，也重視模型假設、資料品質、求解設定與結果可追溯性。

適合處理的情境包括：

- 生產與產能規劃
- 排班與人力配置
- 資源指派與選址
- 配送、庫存與供應鏈規劃
- 預算配置與組合最佳化

## 核心能力

- **結構化建模**：以集合、參數、決策變數、目標函數與限制式，清楚表達 MILP 問題。
- **AI 開發流程**：提供由問題理解、模型設計到實作驗證的自動化階段流程，降低需求描述與最佳化模型之間的落差。
- **框架化實作**：OptimFoundation 提供一致的資料、模型、求解與結果輸出抽象；source generator 協助維持模型元件的規格一致。
- **求解與實驗管理**：以 CPLEX 執行求解，並支援不同求解設定的比較、基準結果與後續調校。
- **開發護欄**：將資料驗證、模型檢核、API 使用規則與專案慣例集中管理，讓 AI 與開發者能在相同契約下協作。

## 整體架構

```text
業務問題
  → MILP 數學模型
  → OptimFoundation 模型元件
  → CPLEX 求解
  → 解答、指標與實驗結果
```

主要組成如下：

| 元件 | 角色 |
| --- | --- |
| `Projects/` | 各個最佳化案例與應用專案。 |
| `Template/` | 新專案共用的結構與基礎設定。 |
| `.claude/` | AI 開發規範、自動化流程與驗證資源。 |
| `dlls/` | CPLEX 與 OptimFoundation 的執行相依元件。 |
| `dataset/` | 範例與測試資料。 |

## 技術基礎

| 技術 | 用途 |
| --- | --- |
| .NET 8 | 應用程式執行環境。 |
| OptimFoundation | 最佳化模型與專案生命週期框架。 |
| IBM ILOG CPLEX 22.1.1 | MILP 求解器。 |
| C# Source Generator | 模型宣告與框架元件的自動產生支援。 |

## 延伸文件

- [總流程：天條與三階段契約](.claude/skills/AGENTS.md)
- [Phase 1 建模規範](.claude/skills/modeling/model-design-guide.md)
- [Phase 2 OptimFoundation API 指引](.claude/skills/coding/optimfoundation-api-guide.md)
- [Phase 3 調校規範](.claude/skills/tuning/solver-tuning-guide.md)
- [開發者教材](tutorial%28for%20developer%29/)
