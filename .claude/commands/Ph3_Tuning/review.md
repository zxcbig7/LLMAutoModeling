# /review — Phase 3 Foundation Tuning

審查調校工作是否先通過正確性 gate，並能以 baseline 與量測證據支持結論。依 [`../../rules/AGENTS.md`](../../rules/AGENTS.md) 與 [`../../workflows/interactive/phase-3-tuning.md`](../../workflows/interactive/phase-3-tuning.md) 檢查觸發原因、求解狀態、資料與時間限制、每次改動的可歸因性、目標／可行性比較，以及是否意外更動模型語意。

輸出 `Blocker`、`Finding`、`Next experiment`、`Verified` 四節。每項附證據、影響、最小下一步；若問題屬於模型或資料語意，標示需退回的 phase，不以調參掩蓋。
