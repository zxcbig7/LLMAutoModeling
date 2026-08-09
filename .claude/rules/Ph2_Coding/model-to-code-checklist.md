# Model.md → Code 驗收 Checklist（Phase 2）

完整規範與 API 以同層 [`optimfoundation-api-guide.md`](optimfoundation-api-guide.md) 為準。本表只把現行規則轉成可驗證項目。

## 0. Phase gate

- [ ] `Model/<Project>_Model.md` 已由使用者確認。
- [ ] SET、PARAM、VAR、CONSTRAINT、OBJ 皆已宣告，名稱與維度無歧義。
- [ ] 每條限制式保留原始 LHS、比較符號與 RHS，沒有預先移項。
- [ ] 每個 Variable 有 Binary / Continuous / Integer 型別與 bounds 說明。
- [ ] 資料性常數都已建成 Parameter，不是散落在 code 的 magic number。

## A. 結構與命名

- [ ] 類別一型別一檔，檔名等於類別名。
- [ ] 命名使用 `Set_`、`Parameter_`、`VariableB_` / `VariableC_` / `VariableI_`、`Constraint_`。
- [ ] generator 類別是 `partial`，只用 `[OptSet]` / `[OptParam]` / `[OptVar]` + `[OptDim<T>("Name")]` 宣告。
- [ ] `OptDim<T>` 的 `T` 是 `string`、`DateTime`、`int`、`long`、`double` 或 `decimal`。
- [ ] 沒有手寫 generator 會建立的 base class 或 property。
- [ ] csproj 的 framework、analyzer 與資料複製設定正確，`dotnet build` 能載入 generator。

## B. Set / Parameter / CSV

- [ ] 每個 Set 至少一維；每個維度依序對應一個生成 property。
- [ ] 多維 Set 是一個 row class 掛多個使用 framework 支援基礎型別的 `OptDim`。
- [ ] Parameter 可零到多維，最後固定有 `QTY`。
- [ ] 使用 `CsvDataSource` 時，每份模型資料 CSV 都有表頭；Set 表頭是所有維度欄，Parameter 再加 `QTY`。使用 DB 時不建立假的 CSV。
- [ ] scalar Parameter CSV 只有 `QTY` 表頭，且專案已驗證資料列數符合語意。
- [ ] 表頭名稱與 public property 對得上，資料列欄數一致。
- [ ] Template 日期一律是 `yyyy-MM-dd`，數值使用 `.` 小數點與 invariant culture；沒有依賴 mapper 額外接受的其他日期格式。
- [ ] Set 與 Parameter 的多維 key 沒有重複；這兩類重複都由 `DataContext` 自動驗證。
- [ ] Set-driven Parameter lookup 使用 `FindParameterOrLog`；缺值後採 0、跳過或其他預設值的政策已由專案明確決定。

## C. Dataload / IDataSource

- [ ] `Dataload` 宣告為 `public sealed partial class Dataload : DataContext`。
- [ ] Set 與 Parameter 欄位都是 `List<T>`。
- [ ] `Dataload(IDataSource source)` 對兩者一律使用 `source.Load<T>(sourceName)`；CSV 檔名不必等於 row class 名稱，DB 則傳完整 SQL。
- [ ] CSV `sourceName` 對應執行檔目錄下的 `Data/{sourceName}`，可傳有或沒有 `.csv`；省略時才使用 `typeof(T).Name`。
- [ ] 若提供 CSV 預設來源，無參數 constructor 只有一個，並以 `: this(new CsvDataSource())` 委派至 source constructor；其他來源由 `Dataload(IDataSource source)` 注入。
- [ ] CSV / InMemory / DB 都以同一個 `IDataSource` 契約進入 Dataload。
- [ ] DB 的 `Load<T>` 第一引數是完整 SQL，欄名不一致時使用 `AS` 對到 property。
- [ ] 正式載入走 `OptData.Load(() => new Dataload(...))`。
- [ ] import 寫出 Set 與 Parameter 都使用 `CsvCtrl.WriteRows`，輸出可由 `Load<T>` round-trip 讀回。
- [ ] 沒有把資料生成、補值或隨機化混進 `Dataload(IDataSource source)`。

## D. Variable

- [ ] Model.md 型別逐一對上：B=Binary、C=Continuous、I=Integer。
- [ ] Variable 只用 `[OptVar]` + framework 支援的基礎型別 `[OptDim]` 宣告。
- [ ] 一般建立入口是 `BuildVars<T>(sets...)`。
- [ ] `BuildVars` 傳入資料的總欄寬與 Variable property 數量一致，順序相同。
- [ ] 多維 Set list 是稀疏 domain；多個一維 Set list 才是笛卡兒積。
- [ ] 零維 Variable 用 `BuildVars<T>()`，不塞虛構的一元素 Set。
- [ ] 若直接使用 `BuildBVs` / `BuildCVs` / `BuildIVs`，前綴與 builder 型別一致，且有使用理由。
- [ ] 同一 Variable 型別沒有重複建立。

## E. Objective / Constraint

- [ ] Objective 方向與 Model.md 相同，使用 `CreateMinimize()` 或 `CreateMaximize()`。
- [ ] Objective 在 constraints 前加入 `OptModel`。
- [ ] 每個 Constraint class 對應一個 Model.md 命名限制式或明確標示的同族式群組；群組內每條式子都有逐項對照。
- [ ] 左側項只進 `AddLHS`，右側項只進 `AddRHS`，沒有自行移項或翻方向。
- [ ] `=` / `≤` / `≥` 分別使用 `CreateEqual` / `CreateLessEqual` / `CreateGreatEqual`。
- [ ] 新 code 使用 owner overload：`CreateXxx(this, dims...)`。
- [ ] 傳入 owner overload 的 dims 足以讓每條限制式名稱唯一。
- [ ] 沒有手工串接 `ConstraintName`、日期或 `@` key。
- [ ] 沒有混用 `AddRHS(value)` 與會覆蓋 RHS 常數的 `CreateXxx(rhs, name)`。
- [ ] 沒有手動修改 `ConstraintCount`。
- [ ] Objective / Constraint constructor 只收實際依賴，不收整包 `Dataload`。

## F. 模型名稱與日期

- [ ] 模型名稱 token 不含空白、`@` 或保留運算字元。
- [ ] 變數、參數與限制式名稱的日期都由 framework 統一成 `yyyy_MM_dd`。
- [ ] CSV 日期維持 `yyyy-MM-dd`，沒有把 CSV 格式手工拿來拼模型名稱。
- [ ] Solution key 與 `ModelElementBase.ToString()` / `BuildVars` 格式一致。

## G. Logging 與錯誤

- [ ] 框架主動判定的 error 在 throw 前有 Error Log。
- [ ] Log 含事件代碼、context、錯誤值、原因與 `result=aborted`。
- [ ] 公開 API 邊界對外部/未預期例外記錄後原樣 rethrow。
- [ ] 同一 exception 不會在底層與邊界重複記錄。
- [ ] 命名、CSV、變數 arity/type、限制式與 DataContext edge cases 有測試。

## H. Build / Solve / Output

- [ ] `dotnet build` 無 error。
- [ ] unit tests 全過。
- [ ] 小型已知答案資料可求解，狀態、目標值與關鍵變數符合預期。
- [ ] `Status` 已依七個 `SolveStatus` 值分流（api-guide §7.1）：`Optimal` / `Feasible` 有可用解；`TimeLimit` 無解時改用小 instance 驗證；`Infeasible` / `Unbounded` / `Error` / `NotSolved` 不得當成功交付。
- [ ] `Feasible` 交付時已一併記錄 `MipGap` 與 `BestBound`，且**未**以放寬 `CplexConfig.MipGap` / 加大 `CplexConfig.TimeLimit` 的方式把它「湊成 `Optimal`」。
- [ ] `status.json` 的 `solveStatus` 與 `verifiedOn` 已如實填寫（Phase 3 靠這兩欄決定進場情境）。
- [ ] 變數與限制式實際數量符合預期。
- [ ] 使用最新版公開名稱：`project.Engine`、`engine.VariableCount`、`engine.RegisteredVariableCount`、`CplexConfig.TimeLimit` / `MipGap` / `Threads`。
- [ ] `ISolutionSink` 或 `GetSolution` 輸出欄位、列數與 key 正確。
- [ ] 換另一批合法 CSV 不需要修改模型 code。

## I. 最終靜態掃描

```powershell
$project = "Projects/<Project>"

# 宣告面：逐一確認 OptDim 泛型參數都是 framework 支援的基礎型別
rg -n 'OptDim<' $project

# Dataload：逐一確認 Set / Parameter 都由 source.Load<T> 讀取
rg -n 'source\.Load<' "$project/Data"

# 限制式：新 code 不手工組完整名稱
rg -n -e 'ConstraintName\s*\+' `
      -e 'Create(Equal|LessEqual|GreatEqual)\(\s*\x22' `
      -e 'CreateRange\([^\r\n]*\x22' `
      -e 'Create(Le|Ge|Eq)Soft\([^\r\n]*\x22' `
      "$project/Constraint"
```
