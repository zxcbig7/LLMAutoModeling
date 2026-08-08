# Foundation Tuning — 回寫程式規則

只有通過正確性 gate 且使用者要求調校時，才能修改程式。完整入口與診斷流程見 [`../../../workflows/interactive/phase-3-tuning.md`](../../../workflows/interactive/phase-3-tuning.md)。

## 可修改範圍

- **只有 `Program.cs` 的 `CplexConfig`（production baseline 與 exp 分支的 variants）可以動**；每項改動須記錄假設、預期影響與量測結果。
- 模型與資料凍結：`Data/*.csv`、`Data/Dataload.cs`、`Constraint_*.cs`、`Objective/`、`Model/<Project>_Model.md` 本階段唯讀。
- 若調校發現模型語意或資料定義錯誤，立即停在問題點並退回 Modeling／Coding；不可把錯誤掩蓋成 penalty、放寬或 magic number。
- 不改 OptimFoundation 本體、不改 API 合約、不把專案 DLL 引用改成跨 repo `ProjectReference`。

## 回寫與驗證

1. 每次只改一組可歸因的設定。
2. 保留 baseline、輸入資料、硬體／時間限制與求解狀態，才能比較。
3. 重新執行 build、解驗證與目標／可行性比較；有退化時回復上一個已驗證版本。
4. 將有效設定及無效嘗試記錄在專案的 tuning 結果，而非只留在對話中。
