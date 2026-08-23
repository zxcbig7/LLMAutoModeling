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
4. 建 `Variable/Variable[B|C|I]_*.cs`：Dim 順序與展開資料的順序一致，使用 `BuildVars<T>` 建立。
5. 分別實作 `Objective/` 和 `Constraint/`；每條限制式一個檔。
6. 在 `Program.cs` 依「資料 → 變數 → 目標式 → 限制式 → 求解」組裝。
7. build、求解、驗證每條限制式與輸出。

## 3. 資料層規則

```csharp
var arcs = source.Load<Set_Arc>();
var costs = source.Load<Parameter_ArcCost>("arc-costs-2026.csv");

CsvCtrl.WriteRows(arcs);
CsvCtrl.WriteRows(costs);
```

- Set CSV = Dim 欄；Parameter CSV = Dim 欄 + `QTY`。
- CSV 的第一列一定是表頭；欄名必須對應 `OptDim` 名稱。
- CSV 檔名不必等於 row class 名稱；省略 `sourceName` 時才預設使用 `typeof(T).Name`。
- 不在 Template CSV 的 `Dataload` constructor 補資料或做商業運算；它只向來源要求指定 row 型別。
- `DbDataSource.Load<T>` 的名稱引數就是完整 SQL；SQL 留在資料載入邊界，不進 Objective 或 Constraint。

## 4. 交付前驗收

- `dotnet build` 成功。
- 每個輸入 CSV 可載入、型別可轉換；Set／Parameter duplicate key 由 DataContext 檢查。Parameter→Set 關聯由開發者掌握，Set-driven lookup 使用 `FindParameterOrLog`。
- 每條限制式以解值代回後成立。
- 目標值、單位與量級符合題目。
- 變數展開的維度順序與宣告順序一致。

實際下指令的模板見 [prompt-guide.md](prompt-guide.md)。詳細規範請讀 [`.claude/skills/AGENTS.md`](../.claude/skills/AGENTS.md) 與 Phase 2 API guide。
