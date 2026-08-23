# coding · 交付前驗收（Model.md → Code 一致性契約）

> **AI enforcement spec，不是給使用者勾選的文件。** 任何 agent 產生、補修或驗證 `Projects/<Project>/` 時，MUST 以本檔機械檢查輸出。未通過 = 繼續修正，不得宣告 Phase 2 完成。
> 規則本體在同層 [`optimfoundation-api-guide.md`](optimfoundation-api-guide.md)（API 在 §9），天條在 [`../AGENTS.md`](../AGENTS.md)；本檔只把它們轉成可驗證的檢查項，**不解釋選擇、不重述規則、不取代 Model.md**。
> **逐條對照回 Model.md**，不是掃一眼 code 就打勾。

## 0. Canonical 專案架構（所有產出完全一致）

**source-controlled 專案投影 MUST 恰為下列結構。**`bin/`、`obj/`、`Generated/` 是 build 產物，不計入手寫專案架構；除它們外，AI 不得建立未列出的資料夾或自訂分層。

```text
Projects/<Project>/
├── <Project>.csproj
├── Program.cs
├── Model/
│   └── <Project>_Model.md
├── Set/
│   └── Set_*.cs
├── Parameter/
│   └── Parameter_*.cs
├── Variable/
│   └── VariableB_*.cs / VariableC_*.cs / VariableI_*.cs
├── Objective/
│   └── ObjectiveFunction.cs
├── Constraint/
│   └── Constraint_*.cs
├── Solution/
│   └── <Project>Solution.cs
└── Data/
    ├── Dataload.cs
    ├── Set_*.csv
    └── Parameter_*.csv
```

- [ ] 專案在 `Projects/<Project>/`，從 `Template/` 長出來。
- [ ] 八個固定資料夾 **全部存在且名稱、大小寫完全一致**：`Model`、`Set`、`Parameter`、`Variable`、`Objective`、`Constraint`、`Solution`、`Data`。
- [ ] 專案根只有 `<Project>.csproj`、`Program.cs` 與上述固定資料夾作為 **Phase 2** 手寫產物；執行期 `status.json` 可存在，但不是 AI 自創的架構層。
- [ ] `Model/` 只放 `<Project>_Model.md`；`Dataload.cs` 在 `Data/`，與 CSV 同層。
- [ ] 若專案已進入 Phase 3，唯一合法延伸由 [`../tuning/solver-tuning-guide.md`](../tuning/solver-tuning-guide.md) 定義：專案根的 `TuningHistory.md`、`status.json` 與 `Experiments/`。`Experiments/` 只可包含 `<Project>-tuning-r<N>.csv`、`.json`、`-trajectory.csv` 三件 archive；不得以此為由新增其他資料夾。
- [ ] NEVER 建立 `Common/`、`Helpers/`、`Utils/`、`Services/`、`Models/`、`Infrastructure/`、`Domain/`、`Data/import/` 或任何依專案而變的資料夾。
- [ ] 每個手寫型別一型別一檔，檔名 = 類別名，且只放入此型別責任所屬的固定資料夾；不得集中成 `Sets.cs`、`Variables.cs`、`Constraints.cs` 或通用 helper 檔。
- [ ] 所有 `.cs` 使用**唯一** block namespace `namespace <Project> { }`；資料夾只做檔案分類，NEVER 形成子 namespace。
- [ ] `<Project>` 資料夾名、`.csproj` 名、`RootNamespace`、`AssemblyName`、`ProjectConfig.ProjectName` 與 Model 檔名中的 `<Project>` 必須逐字一致。
- [ ] DLL 走 `<Reference>` + HintPath `..\..\dlls\`；generator 走 `<Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />`。
- [ ] csproj 有 `<Compile Remove="Generated/**/*.cs" />`。
- [ ] 沒有任何絕對路徑。
- [ ] AI 在 build 前自行列出目錄樹並比對本節；任何多餘／缺少／錯名路徑 MUST 先修正，不能以「功能可 build」放行架構偏差。

## 1. Model phase gate

- [ ] `Model/<Project>_Model.md` 存在且已由使用者確認。
- [ ] SET、PARAM、VAR、CONSTRAINT、OBJ 皆已宣告，名稱與維度無歧義。
- [ ] 每條限制式保留原始 LHS、比較符號與 RHS，沒有預先移項。
- [ ] 每個 Variable 有 Binary / Continuous / Integer 型別與 bounds 說明。
- [ ] 資料性常數都已建成 Parameter，不是散落在 code 的 magic number。
- [ ] 轉譯過程中沒有「自己決定」的模型解釋（有歧義應該已回 `modeling`）。

## 2. 型別配置與命名一致性

- [ ] 命名使用 `Set_`、`Parameter_`、`VariableB_` / `VariableC_` / `VariableI_`、`Constraint_`。
- [ ] generator 類別是 `partial`，只用 `[OptSet]` / `[OptParam]` / `[OptVar]` + `[OptDim<T>("Name")]` 宣告。
- [ ] `OptDim<T>` 的 `T` 是 `string`、`DateTime`、`int`、`long`、`double` 或 `decimal`。
- [ ] 沒有手寫 generator 會建立的 base class 或 property。
- [ ] csproj 的 framework、analyzer 與資料複製設定正確，`dotnet build` 能載入 generator。

## 3. Set / Parameter / CSV 一致性

- [ ] 每個 Set 至少一維；每個維度依序對應一個生成 property。
- [ ] 多維 Set 是一個 row class 掛多個使用 framework 支援基礎型別的 `OptDim`。
- [ ] Parameter 可零到多維，最後固定有 `QTY`。
- [ ] 使用 `CsvDataSource` 時，每份模型資料 CSV 都有表頭；Set 表頭是所有維度欄，Parameter 再加 `QTY`。使用 DB 時不建立假的 CSV。
- [ ] scalar Parameter CSV 只有 `QTY` 表頭，且專案已驗證資料列數符合語意。
- [ ] 表頭名稱與 public property 對得上，資料列欄數一致。
- [ ] Template 日期一律是 `yyyy-MM-dd`，數值使用 `.` 小數點與 invariant culture；沒有依賴 mapper 額外接受的其他日期格式。
- [ ] Set 與 Parameter 的多維 key 沒有重複；這兩類重複都由 `DataContext` 自動驗證。
- [ ] Set-driven Parameter lookup 使用 `FindParameterOrLog`；缺值後採 0、跳過或其他預設值的政策已由專案明確決定。

## 4. Dataload / IDataSource 一致性

- [ ] `Dataload` 宣告為 `public sealed partial class Dataload : DataContext`。
- [ ] Set 與 Parameter 欄位都是 `List<T>`。
- [ ] `Dataload(IDataSource source)` 對兩者一律使用 `source.Load<T>(sourceName)`；CSV 檔名不必等於 row class 名稱，DB 則傳完整 SQL。
- [ ] CSV `sourceName` 對應執行檔目錄下的 `Data/{sourceName}`，可傳有或沒有 `.csv`；省略時才使用 `typeof(T).Name`。
- [ ] 無參數 constructor 固定只有一個，並以 `: this(new CsvDataSource())` 委派至 source constructor；`Dataload(string rawFile)` 固定存在給 Program.cs 的 import 段使用，其他來源由 `Dataload(IDataSource source)` 注入。即使此專案目前只讀 canonical CSV，亦不得省略 raw-file constructor。
- [ ] CSV / InMemory / DB 都以同一個 `IDataSource` 契約進入 Dataload。
- [ ] DB 的 `Load<T>` 第一引數是完整 SQL，欄名不一致時使用 `AS` 對到 property。
- [ ] 正式載入走 `OptData.Load(() => new Dataload(...))`。
- [ ] import 寫出 Set 與 Parameter 都使用 `CsvCtrl.WriteRows`，輸出可由 `Load<T>` round-trip 讀回。
- [ ] 沒有把資料生成、補值或隨機化混進 `Dataload(IDataSource source)`。
- [ ] `Dataload` 欄位名「小寫元件 + PascalCase 語意」：`set_Item` / `parameter_Demand`。

## 5. Variable 一致性

- [ ] Model.md 型別逐一對上：B=Binary、C=Continuous、I=Integer。
- [ ] Variable 只用 `[OptVar]` + framework 支援的基礎型別 `[OptDim]` 宣告。
- [ ] 一般建立入口是 `BuildVars<T>(sets...)`。
- [ ] `BuildVars` 傳入資料的總欄寬與 Variable property 數量一致，順序相同。
- [ ] 多維 Set list 是稀疏 domain；多個一維 Set list 才是笛卡兒積。
- [ ] 零維 Variable 用 `BuildVars<T>()`，不塞虛構的一元素 Set。
- [ ] 若直接使用 `BuildBVs` / `BuildCVs` / `BuildIVs`，前綴與 builder 型別一致，且有自訂 bounds 或維護既有型別建構的理由。
- [ ] 同一 Variable 型別沒有重複建立。
- [ ] 變數的 LB / UB 是**一條 constraint**，不是藏在 build 參數裡。

## 6. Objective / Constraint 逐條對照（每條都做一次）

- [ ] Model.md 的每條 `[Cn]` 都找得到對應的 `Constraint_*.cs`，且 `///` 註記寫了條號。
- [ ] 每個 Constraint class 對應一個 Model.md 命名限制式或明確標示的同族式群組；群組內每條式子都有逐項對照。
- [ ] 左式的項全在 `AddLHS`、右式的項全在 `AddRHS`——**沒有任何移項**、沒有翻方向。
- [ ] `=` / `≤` / `≥` 分別使用 `CreateEqual` / `CreateLessEqual` / `CreateGreatEqual`。
- [ ] 係數與 Model.md 完全一致（沒有四捨五入、沒有合併化簡）。
- [ ] 新 code 使用 owner overload：`CreateXxx(this, dims...)`，且傳入的 dims 足以讓每條限制式名稱唯一。
- [ ] 沒有手工串接 `ConstraintName`、日期或 `@` key。
- [ ] 沒有混用 `AddRHS(value)` 與會覆蓋 RHS 常數的 `CreateXxx(rhs, name)`。
- [ ] 沒有手動修改 `ConstraintCount`。
- [ ] Objective 方向與 Model.md 相同，使用 `CreateMinimize()` 或 `CreateMaximize()`，且是 OBJ 段的逐項轉譯。
- [ ] Objective 在 constraints 前加入 `OptModel`。
- [ ] Objective / Constraint constructor 只收實際依賴（Set / Parameter / scalar），不收整包 `Dataload`。

## 7. Hardcode 稽查

- [ ] Constraint / Objective 的**常數位**沒有裸數字（單參數的 `AddRHS(40)` 這種）。
- [ ] 結構常數（宮邊長、時間窗長度）做成 `Set_*` 或 `Parameter_*`，沒有寫成迴圈邊界字面數字。
- [ ] 迴圈一律 `foreach (var x in set)` 或 `set.Count`。
- [ ] 所有數值都經 `Parameter` 的 `QTY` 由 `Dataload` 取得。

## 8. 模型名稱與日期一致性

- [ ] 模型名稱 token 不含空白、`@` 或保留運算字元。
- [ ] 變數、參數與限制式名稱的日期都由 framework 統一成 `yyyy_MM_dd`。
- [ ] CSV 日期維持 `yyyy-MM-dd`，沒有把 CSV 格式手工拿來拼模型名稱。
- [ ] Solution key 與 `ModelElementBase.ToString()` / `BuildVars` 格式一致。

## 9. Logging 與錯誤一致性

- [ ] 框架主動判定的 error 在 throw 前有 Error Log。
- [ ] Log 含事件代碼、context、錯誤值、原因與 `result=aborted`。
- [ ] 公開 API 邊界對外部/未預期例外記錄後原樣 rethrow。
- [ ] 同一 exception 不會在底層與邊界重複記錄。
- [ ] 命名、CSV、變數 arity/type、限制式與 DataContext edge cases 有測試。

## 10. Program.cs 四段不可變模板與組裝

- [ ] `Program.cs` 是唯一組裝點，按順序各有且只有一個逐字標記：`// 1. import 段落`、`// 2. 模型段落`、`// 3. 實驗段落`、`// 4. 正式跑段落`；缺一、重複、改名或重排一律 FAIL。
- [ ] import 段固定含 `args.Length >= 2 && args[0] == "import"`、`new Dataload(rawFile)`、`.Export()` 與 `return 0`；不因現有 CSV 或資料來源是 DB 而省略。
- [ ] 模型段固定含 `isExperiment` 判斷、唯一一次 `OptData.Load(() => new Dataload())`、具名 `ProjectConfig`、唯一具名 `productionBaseline`、唯一 `new OptModel("Canonical")` chain。
- [ ] 實驗段固定含 `if (isExperiment)`、至少一次 `productionBaseline.Clone()`、`new OptExperiment(...)`、`.AddModel(model)`、至少一個 `.AddConfig(...)`、`.Run()` 與 `return 0`；不得以「尚未 tuning」省略。
- [ ] 正式跑段固定含 `using var project = new OptProject(model)`、`ProjectConfig` 與 `productionBaseline` 各一個 `.UseConfig(...)`、`.OnSolved(...)`、`project.Execute()` 與成功 0／失敗 1 的 exit code。
- [ ] 四段直接平坦存在 `Main`；沒有包裝四段或模型組裝的 helper、factory、local function，也沒有替代 CLI 命令或額外 mode。
- [ ] 每個 variable family / Objective / 每條 Constraint 各占一個 fluent call。
- [ ] `OptEngine` 從 `Build(engine)` 或 canonical 第一參數進來，不從別處偷渡。
- [ ] scalar 用 `.Single().QTY` 取出成區域變數再傳入（不是 `First()` / `FirstOrDefault()?.QTY ?? 0`）。
- [ ] 參數查詢先存局部變數，`AddLHS` / `AddRHS` 內沒有內嵌 LINQ。

## 11. API

- [ ] 用到的每個 API 都在 `optimfoundation-api-guide.md` §9 查得到，且沒有被標 ❌。
- [ ] 沒有用 `GetVarSol` / `GetSetVarSol` / `CsvCtrl.SaveToCSV`（不存在）。
- [ ] 使用最新版公開名稱：`project.Engine`、`engine.VariableCount`、`engine.RegisteredVariableCount`、`CplexConfig.TimeLimit` / `MipGap` / `Threads`。

## 12. Build / Solve / Output 一致性

- [ ] `dotnet build` 無 error，fix loop 沒超過 5 次。
- [ ] unit tests 全過。
- [ ] `Status` 已依 `SolveStatus` 分流（api-guide §7.1）：`Optimal` / `Feasible` 有可用解；`TimeLimit` 無解時改用小 instance 驗證；`Infeasible` / `Unbounded` / `Error` / `NotSolved` 不得當成功交付。
- [ ] `Feasible`（撞限制但有解）**不算失敗**——已對 incumbent 完成解驗證協定 2–4 並記錄 `MipGap` 與 `BestBound`，且**未**以放寬 `CplexConfig.MipGap` / 加大 `CplexConfig.TimeLimit` 的方式把它「湊成 `Optimal`」。
- [ ] 解已代回**每條** constraint，LHS op RHS 成立。
- [ ] 目標值與關鍵變數的單位、量級對得上題目。
- [ ] LP bound sanity 檢查過（max：整數解 ≤ LP bound；`Feasible` 時比 `BestBound`）。
- [ ] 與 Model.md 的小例 / 已知解對照過。
- [ ] 變數與限制式實際數量符合預期。
- [ ] `ISolutionSink` 或 `GetSolution` 輸出欄位、列數與 key 正確。
- [ ] 換另一批合法 CSV 不需要修改模型 code。

## 13. Phase 3 交棒（exp 分支 R0-ready，guide §8.4）

- [ ] `productionBaseline` 已明設 `ParallelMode = 1` + 固定 `Seed` + 實測定版的 `Threads`。
- [ ] experiment 名 = `<Project>-tuning-r0`。
- [ ] 每個 config label 帶 `r0-` 前綴。
- [ ] exp 分支開頭有 marker 註解 `// R0 — <Project>-tuning-r0`。
- [ ] ★ r0 內容 = **baseline × 5 個固定 seed**，沒有混掃旋鈕。
- [ ] 已實跑一次 `-- exp` 確認管線可執行（build 綠不等於跑得動）。
- [ ] 沒有把 bin 產物 archive 到專案根（那是 Phase 3 每輪的責任）。

## 14. 交付

- [ ] 回報含 build 結果、目標值、解摘要、輸出檔位置。
- [ ] `status.json` 已更新（`buildOk` / `solveVerified` / `solveStatus` / `verifiedOn`），且 `solveStatus` 與 `verifiedOn` 如實填寫（Phase 3 靠這兩欄決定進場情境）。

## 15. AI 最終靜態掃描（完成前必跑）

```powershell
$project = "Projects/<Project>"

# 架構：手寫資料夾只能是 canonical 八個；Phase 3 extension 存在時才允許 Experiments/
$allowedDirs = @('Model','Set','Parameter','Variable','Objective','Constraint','Solution','Data','bin','obj','Generated')
if (Test-Path (Join-Path $project 'TuningHistory.md')) { $allowedDirs += 'Experiments' }
Get-ChildItem -Directory $project |
    Where-Object Name -notin $allowedDirs |
    Select-Object -ExpandProperty FullName

# 架構：canonical 八個資料夾必須全部存在
@('Model','Set','Parameter','Variable','Objective','Constraint','Solution','Data') |
    Where-Object { -not (Test-Path (Join-Path $project $_) -PathType Container) }

# 宣告面：逐一確認 OptDim 泛型參數都是 framework 支援的基礎型別
rg -n 'OptDim<' $project

# Dataload：逐一確認 Set / Parameter 都由 source.Load<T> 讀取
rg -n 'source\.Load<' "$project/Data"

# Program.cs：四段不可變模板。順序、段內責任與必要呼叫均逐段驗證。
$program = Join-Path $project 'Program.cs'
$programText = Get-Content -LiteralPath $program -Raw
$markers = @(
    '// 1. import 段落',
    '// 2. 模型段落',
    '// 3. 實驗段落',
    '// 4. 正式跑段落'
)
$starts = foreach ($marker in $markers) {
    $matches = [regex]::Matches($programText, [regex]::Escape($marker))
    if ($matches.Count -ne 1) { throw "FAIL Program marker [$marker] count=$($matches.Count)" }
    $matches[0].Index
}
if (($starts | Select-Object -Unique).Count -ne 4 -or
    $starts[0] -ge $starts[1] -or $starts[1] -ge $starts[2] -or $starts[2] -ge $starts[3]) {
    throw 'FAIL Program four section markers are missing, duplicated, or out of order'
}
$sections = @(
    $programText.Substring($starts[0], $starts[1] - $starts[0]),
    $programText.Substring($starts[1], $starts[2] - $starts[1]),
    $programText.Substring($starts[2], $starts[3] - $starts[2]),
    $programText.Substring($starts[3])
)
$requiredBySection = @(
    @('args\.Length\s*>=\s*2\s*&&\s*args\[0\]\s*==\s*"import"', 'new Dataload\(rawFile\)', '\.Export\(\)', 'return\s+0\s*;'),
    @('bool\s+isExperiment\s*=', 'OptData\.Load\(\(\)\s*=>\s*new Dataload\(\)\)', 'new ProjectConfig', 'var\s+productionBaseline\s*=\s*new CplexConfig', 'new OptModel\("Canonical"\)'),
    @('if\s*\(isExperiment\)', 'productionBaseline\.Clone\(\)', 'new OptExperiment\(', '\.AddModel\(model\)', '\.AddConfig\(', '\.Run\(\)', 'return\s+0\s*;'),
    @('using\s+var\s+project\s*=\s*new OptProject\(model\)', '\.UseConfig\(', '\.OnSolved\(', 'project\.Execute\(\)', 'return\s+solved\s*\?\s*0\s*:\s*1\s*;')
)
for ($i = 0; $i -lt 4; $i++) {
    foreach ($pattern in $requiredBySection[$i]) {
        if ($sections[$i] -notmatch $pattern) { throw "FAIL Program section $($i + 1) missing [$pattern]" }
    }
}
if (([regex]::Matches($sections[1], 'OptData\.Load\(\(\)\s*=>\s*new Dataload\(\)\)')).Count -ne 1) {
    throw 'FAIL Program model section must load canonical data exactly once'
}

# 限制式：新 code 不手工組完整名稱
rg -n -e 'ConstraintName\s*\+' `
      -e 'Create(Equal|LessEqual|GreatEqual)\(\s*\x22' `
      -e 'CreateRange\([^\r\n]*\x22' `
      -e 'Create(Le|Ge|Eq)Soft\([^\r\n]*\x22' `
      "$project/Constraint"
```
