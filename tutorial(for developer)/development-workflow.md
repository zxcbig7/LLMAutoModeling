# 開發流程速查

這份文件是新版專案的工作順序。Set 與 Parameter 均採 row-data 設計，資料讀寫只有一套泛型 API。

## 1. Modeling → Coding → Tuning

| Phase | 允許做什麼 | 結束條件 |
| --- | --- | --- |
| Modeling | 把題目寫成完整 `Model/<Project>_Model.md` | 使用者確認模型 |
| Coding | 建資料、變數、目標式與限制式，並驗證解 | build 與四步解驗證皆通過 |
| Tuning | 只調 solver 設定，模型與資料維持凍結 | champion 回寫並重新驗證 |

模型尚未確認時，不寫 C#；解尚未驗證時，不做 tuning。

## 2. Coding 的建置順序

1. 建 `Set/Set_*.cs`：每個類別一個 row 型別，至少一個 primitive `OptDim`。
2. 建 `Parameter/Parameter_*.cs`：與 Set 相同的 Dim 寫法，最後由 generator 自動補 `QTY`；需要常數時可宣告零維 scalar Parameter。
3. 建 `Data/*.csv` 與 `Data/Dataload.cs`：使用 `source.Load<T>()` 載入 `List<T>`。
4. 建 `Variable/Variable[B|X|I]_*.cs`：Dim 順序與展開資料的順序一致，使用 `BuildVars<T>` 建立。
5. 分別實作 `Objective/` 和 `Constraint/`；每條限制式一個檔。
6. 在 `Program.cs` 依「資料 → 變數 → 目標式 → 限制式 → 求解」組裝。
7. build、求解、驗證每條限制式與輸出。

## 3. 資料層規則

```csharp
var arcs = source.Load<Set_Arc>();
var costs = source.Load<Parameter_ArcCost>();

CsvCtrl.WriteRows(arcs);
CsvCtrl.WriteRows(costs);
```

- Set CSV = Dim 欄；Parameter CSV = Dim 欄 + `QTY`。
- CSV 的第一列一定是表頭；欄名必須對應 `OptDim` 名稱。
- 不在 `Dataload` 補資料、運算或做 SQL；它只向來源要求指定 row 型別。
- `DbDataSource` 的 query / mapping 屬於資料來源設定，不應滲入模型類別或限制式。

## 4. 交付前驗收

- `dotnet build` 成功。
- 每個輸入 CSV 可載入、型別可轉換、沒有重複 key。
- 每條限制式以解值代回後成立。
- 目標值、單位與量級符合題目。
- 變數展開的維度順序與宣告順序一致。

實際下指令的模板見 [prompt-guide.md](prompt-guide.md)。詳細規範請讀 [`.claude/rules/AGENTS.md`](../.claude/rules/AGENTS.md) 與 Phase 2 API guide。
