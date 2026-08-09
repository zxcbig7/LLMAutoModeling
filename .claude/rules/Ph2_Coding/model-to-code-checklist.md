# Model.md → Code 驗收 Checklist（Phase 2 交付後，人工核對用）

<system_context>
Phase 2（Foundation Coding）AI 宣稱完成後，你（人）拿這份表逐項核對，抓「AI 說過了、實際沒過」的情況。
天條全文見 [`../AGENTS.md`](../AGENTS.md)；完整規範與 API 見同層 [`optimfoundation-api-guide.md`](optimfoundation-api-guide.md)——它是 Phase 2 的**唯一標準**，本表的 §x.y 一律指該檔章節。
本表只把規則轉成可勾選的檢查點 —— NEVER 只看 AI 的文字回報就結案，要求打開檔案實際看。
</system_context>

## 用法

三層驗法由快到慢：**M 段 grep 掃一遍 → 0–K 逐檔肉眼 → L 實跑**。任一項打不了勾 → 退回 AI 重做該項，NEVER 因為「大概率是對的」跳過。

★ = 錯了**不會報錯**：build 過、solve 過、`Status = Optimal`、數字看起來合理，但解的是另一題。這些優先查。

---

## 0. 進場 — Phase 1 有沒有偷跑（§0.0）

Model.md 不合格，Phase 2 一定是猜的。發現任一項不過 → 退回 Model Design，不是叫 AI 在 code 補。

- [ ] `Model/<Project>_Model.md` 存在且八段齊全順序正確：問題描述 → Terminology → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
- [ ] 術語表內嵌在 Model.md，沒有另開 `Glossary.md`
- [ ] 每條 CONSTRAINT 是**原形**（未預先移項 / 化簡 / 翻方向）+ 有 pattern tag + 標了 Dim
- [ ] 有 OBJ 段且標了 min/max —— 沒有 OBJ 段而 AI 自創零係數目標式 = 直接打回（§4.3）
- [ ] ★ CONSTRAINT / OBJ 內沒有「資料類」裸數字（容量、比例、penalty、Big-M、3×3 的 3、時間窗的 7）；線性化 pattern 自帶的 `= 1` / `− 1` / `= 0` 不在此限
- [ ] 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
- [ ] 無單一字母符號（`i`、`x`、`c1`）
- [ ] 「已套用假設」段有列出來，驗收時分得出哪些是題目、哪些是 AI 的預設

## A. 結構 / 命名 / csproj（§1）

- [ ] 八資料夾都在且沒有多的：`Model/` `Set/` `Parameter/` `Variable/` `Objective/` `Constraint/` `Solution/` `Data/`（沒有 `Common/` `Helpers/` `Utils/` `Services/`）
- [ ] 一型別一檔、檔名 = 類別名；沒有 `Sets.cs` 這種集中檔，沒有一檔塞多條限制式
- [ ] `Model/` 只有 `.md`；`Dataload.cs` 在 `Data/`，與 CSV 同層
- [ ] namespace 全專案單一層 `<Project>`，block 寫法 `namespace X { }`（沒有 `MyProject.Data`、沒有 file-scoped `namespace X;`）
- [ ] 每個型別 `sealed`（generator 型別 `sealed partial`），且有 `<summary>` 寫明對應 Model.md 哪個符號
- [ ] attribute 逐行獨立寫，沒有跟 class 擠同一行
- [ ] 命名對得上 §1.3 總表：`Set_X` / `Parameter_X` / `Variable[B|X|I]_X` / `Constraint_X`；`Dataload` 欄位是 `set_X` / `parameter_X`（不是 `ITEM`、不是 `SetA`）
- [ ] Set 成員字串 PascalCase 單數（`"Truck"`，不是 `"truck"` / `"Trucks"`）
- [ ] csproj：`TargetFramework` net8.0、`RootNamespace` / `AssemblyName` = `<Project>`、`Analyzer` 掛 generator dll
- [ ] csproj：五顆 DLL 全走 `..\..\dlls\` HintPath —— 出現 `ProjectReference` 或 NuGet 或 CPLEX Studio 安裝路徑 = 打回
- [ ] csproj：有 `Data\**\*.csv` 的 `PreserveNewest`，且 `Generated/**` 有 `Compile Remove`
- [ ] `Generated/` 底下的 `.g.cs` 沒有被手改過（下次 build 會被覆蓋，改了等於沒改）

## B. 資料層 — Set / Parameter / CSV（§2.0–§2.3）

- [ ] Model.md 的每顆 SET / PARAM 都能對到一個 `.cs` + 一個 CSV + 一行載入敘述，數量三邊相等
- [ ] Set CSV：檔名 `Set_<名>.csv`、單欄、**無表頭**、保序、不重複
- [ ] Parameter CSV：檔名 = 類別名、表頭 = 各 `[OptDim]` 名 + 最後一欄 `QTY`、同一維度組合只出現一次
- [ ] 只有 key、沒有值的 tuple 做成**多維 Set**而不是 Parameter（`Parameter` 一律含 `QTY`）
- [ ] scalar（零維）Parameter 的 CSV 恰好一列資料
- [ ] ★ key 欄的值全部出現在對應的 `Set_*.csv` 裡（否則載入報 `Dangling`）
- [ ] 數值用 `.` 當小數點、沒有千分位逗號；`DateTime` 一律 `yyyy-MM-dd`
- [ ] ★ `[OptSet]` 是**裸寫**且至少掛一個 `[OptDim<資料型別>("Name")]`；沒有拿 `List<string>` 當 Set
- [ ] Parameter 只用 object initializer 建構；沒有位置式 ctor、沒有自寫 ctor；數值欄一律叫 `QTY`
- [ ] ★ 每個 `[OptDim<T>` 的 `T` 都是**資料型別**（`string` / `DateTime` / `int` / …）——填成別的東西不會 compile error，但 property 型別會錯
- [ ] ★ Parameter 每個維度名與它該對應的 Set 維度名**逐字相同**；取了角色名（`LotFrom` vs `Lot`）的維度已知會脫離參照檢查，且該處有註記說明為什麼可以接受
- [ ] `[FullGrid]` 判準對：缺一格代表**資料有問題**才加（例 `Capacity{Machine,Date}`），缺一格代表**組合不存在**就不加（例 `PreAssign{Item}`、`Distance{From,To}`）；有加的都在 `<summary>` 寫了為什麼必須全格
- [ ] ★ 結構常數（3×3 的 3、時間窗 7、班別數）做成了 `Set_*` / `Parameter_*`，迴圈邊界一律 `foreach (var x in set)` 或 `set.Count`，沒有 `for (i = 0; i < 3; i++)`
- [ ] 換一批合法 CSV 後不必改任何 `.cs`（拿實際的第二組資料試，不要用想像的）

## C. Dataload（§2.4）

- [ ] ★ 宣告是 `public sealed partial class Dataload : DataContext` —— `partial` 與繼承缺一，generator 直接跳過、無任何錯誤訊息，這顆 Dataload 一輩子不受驗證
- [ ] `Dataload(IDataSource source)` 內**只有兩種句子**：`set_X.Load(source, "Set_X")` 與 `parameter_X = source.LoadParam<Parameter_X>("Parameter_X")`
- [ ] 沒有 `for` / `foreach` / `Enumerable.Range` / `Random` / enum 掃描 / 日期運算 / `if` / 補值 / 預設值
- [ ] ★ 沒有 `LoadFrom(parameter_X.Select(...).Distinct())` 這種從參數反推 set（會讓題目允許但這批資料沒用到的成員靜默消失）
- [ ] Set 載入一律 `Load(source, "Set_X")` 顯式帶名稱，沒有省略名的 `Load(source)`
- [ ] 建構走 `OptData.Load(() => new Dataload())`，沒有裸 `new Dataload()` 當終點
- [ ] penalty / capacity / bound / Big-M 都是 Parameter CSV，不是 Dataload 的 hardcode public field
- [ ] 沒有手寫驗證邏輯（`ValidateSetsCoverParameters()` 這種）、沒有 `try/catch` 吞 `DataValidationException`
- [ ] 由資料推導的比值走 `Numeric.SafeRatio(分子, 分母, context: "BigM")`，不是裸除法

## D. import 模式（只有寫了 `Dataload(string rawFile)` 才檢查，§2.0 / §2.4）

- [ ] import ctor 與 `Export()` 都在**同一個** `Data/Dataload.cs`，沒有另開 `DataGenerator.cs` 或 `Data/import/`
- [ ] ★ `Export()` 寫出的每個名稱，都在 `Dataload(IDataSource)` 找得到同名的 `Load` / `LoadParam`，且**數量相等** —— 少一顆時 import 照樣 exit 0，要到下次求解才炸
- [ ] `Export()` 末尾有 log 產出清單
- [ ] rawFile 實際存在於 `Data/raw/`；生成假 instance 的情境也有（規模 / seed / 分佈寫成檔案），否則這批資料不可重現
- [ ] 產出的 CSV 若要當正式 input，已從 `bin/.../Data/` 搬回專案 `Data/`（留在 bin 會被 `dotnet clean` 清掉）
- [ ] import 與求解是分兩次命令跑的，沒有做成「import 後自動 solve」的複合模式

## E. 變數層（§3）

- [ ] ★ 前綴與 Model.md `VAR` 段型別逐一對上：`VariableB_` = Binary、`VariableX_` = Continuous、`VariableI_` = Integer（前綴是型別宣告，不是形容詞）
- [ ] 只用 `[OptVar]` + `[OptDim]` 宣告，沒有手寫 `: VariableBase` 或自己宣告 property
- [ ] ★ `BuildVars<T>(sets...)` 傳入 sets 的順序 = `[OptDim]` 宣告順序（順序錯不報錯，只是索引接反）
- [ ] 每種變數在 `Program.cs` 各占一行 `.AddVariables(...)`，沒有同一型別呼叫兩次（會追加不是覆蓋），也沒有在 Constraint / Objective 內 `BuildVars`
- [ ] 0 維變數只掛光桿 `[OptVar]`、建立時 `BuildVars<T>()` 空括號，沒有硬塞一顆只有一個成員的 Set
- [ ] 界限一律寫成獨立的 `Constraint_*`，沒有藏進 `BuildCVs(lb, ub, ...)`
- [ ] 同源多維變數（`Lot × Lot`）的對角線排除有寫成 constraint，不是靠「不建那幾顆」暗示

## F. 模型層 — Constraint / Objective（§4）

- [ ] 每個 `Constraint_*` 繼承 `ConstraintBase`，class `<summary>` 貼了 Model.md 對應的數學式
- [ ] 限制式命名是 `ConstraintName@index1@index2`；`DateTime` 索引用 `{date:yyyy_MM_dd}`（★ 底線，與變數名的 `yyyy-MM-dd` 連字號不同）
- [ ] 建構子逐項列出實際依賴，型別是框架實際型別（`Set_Item` / `List<Parameter_X>` / `double`），沒有 `IReadOnlyList<int>` 這種退化型別
- [ ] 建構子沒有收整包 `Dataload`，也沒有另造 `ModelContext` 繞過
- [ ] `OptEngine` 只從 `Build(OptEngine engine)` 進來，沒有進建構子
- [ ] 參數查詢先存局部變數再傳入，沒有把 LINQ 內嵌進 `AddLHS` / `AddRHS`
- [ ] ★ 只用單參數 `CreateEqual(name)` / `CreateLessEqual(name)` / `CreateGreatEqual(name)`；右側常數一律 `AddRHS(常數)` —— `CreateXxx(rhs, name)` 是**覆蓋**先前 `AddRHS` 不是累加
- [ ] 沒有手動 `ConstraintCount++`、沒有自己印限制式計數
- [ ] Phase 2 的 `Constraint_*.cs` 全是 hard constraint，沒有任何 `CreateLeSoft` / `CreateGeSoft` / `CreateEqSoft`
- [ ] `ObjectiveFunction` 用 `CreateMinimize()` / `CreateMaximize()`，方向與 Model.md OBJ 段一致
- [ ] `.AddObjective(...)` 排在所有 `.AddConstraints(...)` 之前

## G. 逐條對照 Model.md（最花時間，也最重要）

- [ ] 每個 `Constraint_*` 都能在 Model.md 找到對應式子，**數量一致**（沒漏、沒多造）
- [ ] ★ 比較方向沒被翻轉：`<=` → `CreateLessEqual`、`>=` → `CreateGreatEqual`、`=` → `CreateEqual`
- [ ] ★ 沒有移項 / 改號 / 合併化簡：Model.md 左邊的項全在 `AddLHS`、右邊的項全在 `AddRHS`，排列一模一樣
- [ ] 目標式是 OBJ 段的逐項轉譯，沒有偷加項、漏項、改權重、係數對調
- [ ] 每條式子的 pattern tag 與附錄 A 的形狀對得上；形狀對不上多半是 Phase 1 漏了 linking constraint（例 fixed-charge 只加成本沒加 `Produce ≤ M · Open`）
- [ ] ★ Big-M 是題目數據推導出的最緊上界、定義成 `Parameter_*` 從 CSV 讀入，不是 `99999` 這種 magic number

## H. 禁止 hardcode（§4.4）

- [ ] ★ 常數位（單參數形式如 `AddRHS(40)`）沒有「資料類」裸數字，全部來自 `Parameter.QTY`
- [ ] 係數位的字面數字只有 `Σ x` 的 identity `1.0`，或 pattern 自帶的常數（`= 1`、`− 1`、`= 0`）—— 對照 Model.md 那一格寫的是符號還是數字
- [ ] 反過來也沒有：把式子本身的常數包裝成假 Parameter
- [ ] Model.md 寫具名符號的地方，code 沒有反具名化成數字（`AddRHS(40.0)` 但 Model.md 寫 `MachineCapacity`）

## I. Program.cs — 組裝 + 三態 CLI + config（§5）

- [ ] 只有 `Program.cs` 知道 `Dataload`（除了 `Solution/`），平坦分三段：材料 → 模型 → 環境
- [ ] ★ `OptData.Load` 整個程式只呼叫一次，production 與 experiment 共用同一份 `data`
- [ ] 材料段：scalar 用 `.Single().QTY` 取成區域變數 —— 沒有 `First()` / `FirstOrDefault()?.QTY ?? 0.0` / `Sum()`（0 列或 2 列時會靜默給你 0.0 或吃掉第二筆）
- [ ] 材料段沒有運算 / 篩選 / 排序 / 轉換，沒有 `new OptEngine(...)`
- [ ] 模型段是一條 `new OptModel("Canonical")` chain，每種變數一行、目標式一行、每條限制式一行；沒有 `AddLHS` / `AddRHS` 直接出現在 chain 裡
- [ ] 沒有純轉呼叫的 helper / local function / config factory 包裝組裝順序或設定內容
- [ ] 環境段沒有改模型、沒有改資料、沒有第二次 `OptData.Load`
- [ ] 三態 CLI 齊全且互斥：`import <raw>` / `exp` / 無參數；沒有 import 後自動 solve 的複合模式
- [ ] `Logging.SetLogFileName` 只在 exp 模式呼叫，且在 `OptData.Load` **之前**
- [ ] `ProjectConfig.ProjectName` 有明設 = `<Project>`；`ExportLP` 開著（infeasible 時要靠 `.lp` 查）
- [ ] `CplexConfig` 整個檔只有一顆具名 `productionBaseline`；`timeLimit` 有明設不是 `null`；`workThreads` 依實機核心設定
- [ ] 同一項設定沒有抽象旋鈕與 camelCase 兩邊都寫（`Seed` 與 `randomSeed`）；其餘旋鈕留 `null`
- [ ] exit code：求解成功 0、失敗 1、import 完成 0

## J. Solution 解讀層（§6）

- [ ] `Solution/<Project>Solution.cs` 存在且掛在 `OnSolved`（`OnSolved` 只掛在 `OptProject`）
- [ ] ★ 手拼的變數名格式是 `ClassName@dim1@dim2`，分隔符 `@` 不是 `|`，維度順序 = `[OptDim]` 順序，`DateTime` 用 `yyyy-MM-dd`
- [ ] ★ `ValidateRules` 拿掉某條規則或故意改一個值後**真的會 throw** —— 沒實測過就等於沒驗（`TryGetValue` 拼錯只回 false，接著 `?? 0.0` 整段靜默通過）
- [ ] 驗證失敗是 throw，不是記 log 繼續
- [ ] 寫檔前有 `FolderDir.Solution.CreateFolder()`
- [ ] `CsvCtrl.WriteSolution<T>(engine, dataId, userId)` 傳的兩個字串與 `ProjectConfig.DataId` / `UserId` 一致
- [ ] 沒有重印 framework 已經記的 Status / objective / IIS 路徑

## K. API 黑名單（出現任一直接打回，§9.3）

- [ ] 不存在的 API：`GetVarSol` / `GetSetVarSol<T>` / `CsvCtrl.SaveToCSV` / `CSVCtrl`（大寫 V）/ `FolderDir.Result` / `OptEngineConfig` / `AddPool` / `AddPoolRHS`
- [ ] 已禁用的 API：`BuildBVs|BuildCVs|BuildIVs` / `CreateXxx(rhs, name)` / Parameter 位置式 ctor / 手寫 `: VariableBase`｜`: ParameterBase` / `ConstraintCount++`
- [ ] 沒有 `override Build()` / `override Solve()`（已非 virtual，要覆寫是 `BuildCore()` / `SolveCore()`）
- [ ] 沒有改動 OptimFoundation 框架本體（`dlls/` 唯讀）

## L. 實跑驗收（`dotnet run` 之後，§7）

- [ ] `dotnet build` 成功；fix loop 沒有超過 5 次（超過還在硬修 = 打回）
- [ ] ★ 資料載入摘要不是 `Sets（0）` / `Parameters（0）`，且各 Set 成員數、各 Parameter 列數對得上題目
- [ ] Status 三分診斷有做：`Optimal` 才往下；`Infeasible` 有去讀 `bin/Debug/net8.0/IISs/*.ilp`（不是猜哪條錯、更不是改成 soft 繞過）；`Unbounded` 有去補漏掉的上限 constraint
- [ ] ★ 解有代回**每一條** constraint 驗可行，不是只看目標值
- [ ] 目標值與關鍵變數的單位、量級跟題目對得上（利潤 = 錢、產量 = 件、工時 = 時）
- [ ] LP bound sanity：max 整數解 ≤ LP bound、min 反之；或有手算小例對照
- [ ] 沒有因為 `Status == Optimal` 就宣稱完成

## M. 一分鐘 grep 掃描（PowerShell，先跑這段）

有輸出不等於錯，但每一筆都要能講出為什麼合法。

```powershell
# 在 repo 根執行；<Project> 換成實際專案名
$proj = "Projects/<Project>"
$cs = Get-ChildItem $proj -Recurse -Filter *.cs | Where-Object { $_.FullName -notmatch '\\(bin|obj|Generated)\\' }

# 黑名單 API + 手寫 base + soft constraint：有輸出即打回
$cs | Select-String 'GetVarSol|GetSetVarSol|SaveToCSV|CSVCtrl|BuildBVs|BuildCVs|BuildIVs|FolderDir\.Result|OptEngineConfig|AddPool|ConstraintCount|:\s*(VariableBase|ParameterBase)|Create(Le|Ge|Eq)Soft'

# 宣告法：attribute MUST 裸寫，維度全走 [OptDim<資料型別>("Name")]；有輸出即打回
$cs | Select-String 'OptSet<|OptDim<Set_|OptVar\("|OptParam\("'

# 常數位裸數字（單參數 Add）：只有 pattern 自帶的 1 / -1 / 0 合法
$cs | Select-String 'Add(LHS|RHS)\(\s*-?[0-9.]+\s*\)'

# 迴圈邊界寫死 + 兩參數 CreateXxx + 位置式 Parameter ctor
$cs | Select-String 'for\s*\(|Create(Equal|LessEqual|GreatEqual)\([^)]*,|new Parameter_\w+\('

# namespace 違規（子 namespace / file-scoped）
$cs | Select-String 'namespace\s+\w+\s*;|namespace\s+\w+\.'

# Dataload 外洩：應只出現在 Program.cs、Data/Dataload.cs、Solution/
$cs | Select-String 'Dataload' | Where-Object { $_.Path -notmatch 'Program\.cs|Dataload\.cs|Solution' }

# scalar 取值走錯 API
$cs | Select-String 'data\.parameter_\w+\.(First|FirstOrDefault|Sum)\('

# Dataload 白名單：這裡除了 Load / LoadParam / ctor 不該有別的敘述
Get-ChildItem "$proj/Data/Dataload.cs" | Select-String 'for|foreach|if|Random|Enumerable|AddDays|LoadFrom'

# csproj：要有 dlls HintPath + Analyzer + Data copy，不能有 ProjectReference
Select-String -Path "$proj/*.csproj" -Pattern 'ProjectReference|HintPath|Analyzer|Data\\'
```

---

**打回門檻**：★ 項目任一不過 → 一律退回重做（這類錯誤不會在執行期被抓到）。非 ★ 項目不過 → 當場修，修完重跑 L 段。
