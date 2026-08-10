# OptimFoundation API — 端到端開發規範

> **這份文件是什麼**：用 OptimFoundation（封裝 IBM ILOG CPLEX 的 C# 套件）寫一個最佳化專案，從「拿到模型後」到「寫模型程式」到「組模型寫實驗程式」的完整標準流程。
> **給誰看**：第一次用這套套件的人，以及要用它產 code 的 AI。
> **怎麼用**：從 §0 一路往下做，每節結尾的 checklist 過了才進下一節。API 簽名有疑慮 → 查 §9（框架完整簽名表就在本檔內），**NEVER 憑記憶發明方法名**。
> **前置**：數學模型必須先定完並經使用者確認，成果就是 `Model/<Project>_Model.md`。它長什麼樣、缺哪一項就得退回重補，見 §0.0。
> **本檔自足**：讀這一份就能從模型走到可交付的專案，不需要開任何其他文件。凡本檔會用到的外部規則（phase gate、Model.md 契約、線性化 pattern、框架 API 簽名、solver 旋鈕）都已內嵌在對應章節與附錄。
> **本檔是 Phase 2 的唯一文件**：專案結構、命名、注入方式、允許的 API 與框架簽名全在這裡，§9 是簽名權威。

路徑一律**相對本 repo 根**（`.claude/` 的上一層），NEVER 絕對路徑。`$SIB` 指 sibling 的框架 repo `../OptimFoundation/`，只在說明「哪些東西不是 scaffold」時出現，本檔不要求你去讀那底下的任何文件。

### Canonical 邊界（AI MUST 先判斷）

- **本 guide 是專案結構與寫法的唯一權威**。結構、命名、namespace、注入方式、允許的 API 一律以本檔為準。
- **權威順序**：本 guide 的規則 → 本檔 §9 的框架簽名表 → 任何既有專案 code。既有專案與本檔衝突 → 視為待遷移，NEVER 反向修改本檔去迎合它。
- **NEVER 從既有專案推導結構**。`Template/`、`Projects/*`、sibling 的 `$SIB/OptimFoundation/Templates/*` 都**早於本版規範**，含已被禁止的寫法（手寫 `VariableBase`、`Sets.cs` 集中檔、子 namespace、`CreateXxx(rhs, name)`、`ProjectReference`）。它們可讀來理解 API 行為，NEVER 複製其結構。
- **只有一條 paved path**：generator（`[OptSet]` / `[OptParam]` / `[OptVar]` + `[OptDim<T>("Name")]`）。`T` 直接是支援的 C# 基礎型別（`string` / `DateTime` / `int` / `long` / `double` / `decimal`），不引用 `Set_*` 積木。Set 與 Param 共用同一個 Dim→property→CSV 欄位流程；Param 僅在最後固定多 `QTY`。**手寫 `: VariableBase` / `: ParameterBase` 已廢止，沒有後路。**

---

## Set / Param row-data 契約（2026-08-08）

`Set_*` 與 `Parameter_*` 都是 generator 產生的 **row class**。Set 不是積木或
集合物件；載入後的資料列一律保存為 `List<T>`，並使用同一個 reader：

```csharp
var arcs = source.Load<Set_Arc>();
var arcCosts = source.Load<Parameter_ArcCost>();
```

每個 index 都以具順序的基礎型別 `OptDim<T>` 宣告（`string`、`DateTime`、
`int`、`long`、`double` 或 `decimal`）。Set 至少需要一個 Dim；Param
可有零個 Dim，代表 scalar parameter。CSV 一律以表頭對欄：

```csv
# Set_Arc.csv
From,To
A,B

# Parameter_ArcCost.csv
From,To,QTY
A,B,12.5

# Parameter_Alpha.csv（scalar）
QTY
0.95
```

Set 與 Param 都是 row collection，讀取一律使用 `source.Load<T>(sourceName)`，
輸出一律使用 `CsvCtrl.WriteRows(rows, fileName)`。`SetRowBase` 是 generator 產生的 row 基底；專案端只保存與傳遞 `List<Set_*>`。

---

## §0 心智模型

### 0.0 輸入契約 — 你拿到的模型

**三階段 phase gate（天條）**：

| 階段                              | 產物                                      | 本檔涵蓋                             |
| --------------------------------- | ----------------------------------------- | ------------------------------------ |
| 1 · Model Design（建模）          | `Model/<Project>_Model.md`                | 只定義「合格的輸入長什麼樣」（本節） |
| 2 · Foundation Coding（轉譯實作） | 八資料夾專案 + 可求解的 `.cs`             | §1 – §7                              |
| 3 · Foundation Tuning（調校）     | experiment 證據 + promotion 後的 baseline | §8（使用者提出才做）                 |

- NEVER 在使用者明確確認數學模型前產生任何 `.cs` —— ALWAYS 先有 Model.md 並等到「模型確認」或「開始實作」。使用者一句話明說「模型我確認過了，直接寫 code」= 通過 gate。
- **Phase 2 是 Model.md 的純機械轉譯**，不允許任何自行詮釋。發現歧義 → 立即停止，退回 Phase 1 補模型，NEVER 在 code 這一層替使用者做建模決定。

**Model.md 固定八段順序**（數學式一律 LaTeX，`$...$` / `$$...$$`）：

```text
問題描述 → Terminology Mapping Table → SET → PARAM → VAR → CONSTRAINT → OBJ → 已套用假設
```

術語表**內嵌在 Model.md**，NEVER 另開 `Glossary.md`。

**各段必填欄 = Phase 2 的直接輸入**（缺一項，轉譯就得靠猜）：

| 段          | 必填欄                                                                                                                          | 範例                                                                                         | 本檔哪一節在吃它 | 缺了會怎樣                         |
| ----------- | ------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | ---------------- | ---------------------------------- |
| Terminology | Term / 中文語意 / Role / Unit / Derived? / Raw phrase；Role ∈ {parameter, variable, derived, constraint, objective, irrelevant} | `MachineCapacity｜機器產能｜parameter｜hours｜No｜"up to 40 hours"`                          | §1.3 命名總表    | 名稱沒語意，類別名跟著沒語意       |
| SET         | Set 名 / 語意 / 成員範例                                                                                                        | `GlassType｜玻璃種類｜Regular, Tempered`                                                     | §2.1             | 不知道要建哪幾顆積木               |
| PARAM       | Param 名 / 語意 / **Dim** / 值                                                                                                  | `DemandQty｜需求量｜GlassType｜Regular=60`                                                   | §2.2             | `[OptDim]` property 名靠猜         |
| VAR         | Var 名 / 語意 / Dim / **型別** / LB / UB                                                                                        | `Assign｜是否指派｜Employee,Date｜Binary｜0｜1`                                              | §3               | 前綴 `B_/C_/I_` 選錯，型別就錯     |
| CONSTRAINT  | **`LHS op RHS` 原形**（未預先移項）+ **pattern tag** + Dim + 一句中文                                                           | `### Capacity [UB] ∀ machine` + `$$\sum_g UsageRate_g \cdot Produce_g \le MachineCapacity$$` | §4.2             | 對不回去，天條的逐條驗收失效       |
| OBJ         | 方向（min/max）+ 所有項寫在 LHS                                                                                                 | `$$\max \sum_g Profit_g \cdot Produce_g$$`                                                   | §4.3             | 停止 Phase 2（見 §4.3）            |
| 已套用假設  | Phase 1 自行套用的預設清單                                                                                                      | 「未提 LB/UB 者取 `0..INFTY`」                                                               | §7 驗收          | 驗收時分不出哪些是題目、哪些是假設 |

pattern tag 是附錄 A 的 8 類之一；轉譯前先用附錄 A 對一次式子形狀，能在寫 code 前抓出漏 linking constraint 這類建模漏洞。

**進場檢查（開工前逐項對照，任一不過 → 停止 Phase 2，退回 Model Design）**：

- [ ] 八段齊全、順序正確；術語表在 Model.md 內
- [ ] **宣告先於使用**：CONSTRAINT / OBJ 出現的每個符號都已在 SET / PARAM / VAR 宣告
- [ ] **CONSTRAINT / OBJ 內沒有「資料類」裸數字**：容量、比例、penalty、Big-M、3×3 的 3、時間窗的 7 —— 凡是換一批資料會變的，都 MUST 是具名 PARAM。線性化 pattern 自帶的常數（`Σ z = 1` 的 1、`z₁ + z₂ − 1` 的 −1、`= 0`）不在此限，那是式子的形狀不是資料（§4.4）
- [ ] 每個 VAR 標了型別 + LB/UB；每個 PARAM 標了 Dim
- [ ] 每條 CONSTRAINT 是原形（未預先移項 / 化簡 / 翻方向）+ 有 pattern tag + 標了 Dim
- [ ] 有 OBJ 段且標了方向
- [ ] 無單一字母符號（`i`、`x`、`c1`）；符號皆語意命名（`Assign_{Employee,Date}`、`MachineCapacity`）
- [ ] 每個數字都帶單位，單位不一致已換算標明
- [ ] 已列出「已套用的預設假設」

✅ Good：`x <= a * y`（原形）　❌ Bad：`x - a*y <= 0`（Phase 1 就移項，Coding 端 `AddLHS`/`AddRHS` 對不回去）

### 0.1 一條單向鏈

**資料的前提**：`Data/*.csv` 已經按框架標準形狀就位（兩個實體位置與讀取路徑見 §2.0）。`Dataload` 的工作就只是把它們讀成積木——不生成、不補值、不判斷。

```text
① 資料層   Data/*.csv                     input，已就位
           Set_*（維度）+ Parameter_*（係數與結構常數）
           └ Data/Dataload.cs : DataContext
             └ OptData.Load(...)          唯一建構入口 → 驗證不過當場丟例外
② 變數層   VariableB_/C_/I_*              決策變數宣告（前綴決定型別）
             └ engine.BuildVars<T>        把宣告 × sets 展開成實際變數
③ 模型層   ObjectiveFunction              目標式（MUST 先建）
           Constraint_*                   限制式（AddLHS / AddRHS / CreateXxx）
④ 組裝     Program.cs 的 OptModel chain   唯一組裝點，唯一知道 Dataload 的地方
⑤ 執行環境 OptProject                     一個模型 × 一組設定；可掛 OnSolved
           OptExperiment                  m 個模型 × n 組 solver 設定
⑥ 解讀層   Solution/<Project>Solution.cs  ReadAndValidate + Print
```

CSV 不是現成的（來源是 9×9 矩陣、原始報表，或根本還沒有資料、要依題目規格生一批），用 `import` 模式先產出標準 CSV，見 §2.0。那是**選用的前置步驟**，不改變 ① 的前提。

### 0.2 三條不可協商的分工

**組裝只在 `Program.cs`。** 每種變數一行 `.AddVariables(...)`、目標式一行 `.AddObjective(...)`、每條限制式一行 `.AddConstraints(...)`。
Why: pipeline 本身就是模型組成清單，review 從這一個檔就能讀完模型由哪些部分組成；轉呼叫的 helper 或 local function 會把組裝順序藏起來。

**`Dataload` 只准出現在 `Program.cs`。** Objective / Constraint 的建構子逐項列出它實際用到的 Set、Parameter、scalar。
Why: 建構子簽名就是依賴清單，看簽名就知道這條式子碰哪些資料，測試也只需準備那幾樣。

**`OptEngine` 由 `Build(OptEngine engine)` 傳入，NEVER 進建構子。**
Why: 建構子只放資料依賴，engine 是執行期物件；混在一起就分不出「這個類別需要什麼資料」。

`OptData.Load` 完成後把資料視為唯讀。`DataContext.Freeze()` 只保護框架受控的 mutation API；直接修改 public field 或 mutable `List` 不保證立即攔截，因此專案 code MUST 不做這些寫入。

---

## §1 建立專案

### 1.1 專案結構（八個資料夾，NEVER 自創層級）

```text
Projects/<Project>/
├── <Project>.csproj
├── Program.cs        進入點：三態 CLI + 材料 + OptModel 組裝 + config + runner
├── Model/            <Project>_Model.md（含術語表的唯一模型文件）—— 不放 Glossary.md 或 .cs
├── Set/              Set_*.cs（一顆一檔）
├── Parameter/        Parameter_*.cs（一份一檔）
├── Variable/         Variable[B|X|I]_*.cs（一支一檔）
├── Objective/        ObjectiveFunction.cs
├── Constraint/       Constraint_*.cs（一條一檔）
├── Solution/         <Project>Solution.cs
└── Data/             Dataload.cs + Set_*.csv + Parameter_*.csv + raw/（不規則原始資料 / 生成規格，選用）
```

- **NEVER 增減資料夾**。沒有 `Common/`、`Helpers/`、`Utils/`、`Services/`、`Models/`。
- **`Model/` 只放文件**（`.md`），NEVER 放 `.cs`；模型組裝碼在 `Program.cs`。
- **`Dataload.cs` 在 `Data/`**，與它讀的 CSV 同一層。
- **一個型別一個 `.cs` 檔，檔名 = 類別名** —— NEVER 把多顆 Set 塞進 `Sets.cs`、NEVER 把多條限制式塞進一個檔 —— Why: 一檔一型別才能靠檔名直接定位（`Set_Date` 就在 `Set/Set_Date.cs`）；集中檔改一顆就動到全部人的 diff。

### 1.2 Namespace 與型別修飾（全專案統一）

```csharp
// Set/Set_Item.cs
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>可生產的品項；對應 Model.md 的 ITEM。</summary>
    [OptSet]
    [OptDim<string>("Item")]
    public sealed partial class Set_Item { }
}
```

| 規則           | 說明                                                                                                                                                         |
| -------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| namespace      | **全專案一律 `<Project>`，沒有子 namespace** —— NEVER `MyProject.Set` / `MyProject.Data` —— Why: 子 namespace 只會讓每個檔多一排 `using`，分類已由資料夾表達 |
| namespace 寫法 | **block namespace `namespace X { }`** —— NEVER file-scoped `namespace X;`                                                                                    |
| 型別修飾       | **一律 `sealed`**。generator 型別寫成 `sealed partial`（`sealed` 只需寫在手寫那半，generator 產的那半不必重複）                                              |
| `<summary>`    | **每個型別 MUST 有**，一句話寫它對應 Model.md 的哪個符號                                                                                                     |
| attribute      | **逐行獨立寫** —— NEVER 與 class 同一行 —— Why: 加第二個 attribute 時不必重排，宣告與修飾一眼分得開                                                          |

### 1.3 命名總表（Model.md 符號 → code）

| 元素            | 類別名（PascalCase）  | `Dataload` 欄位名（小寫元件開頭） | 檔案                                |
| --------------- | --------------------- | --------------------------------- | ----------------------------------- |
| Set             | `Set_Item`            | `set_Item`                        | `Set/Set_Item.cs`                   |
| Parameter       | `Parameter_Demand`    | `parameter_Demand`                | `Parameter/Parameter_Demand.cs`     |
| Binary 變數     | `VariableB_Assign`    | —                                 | `Variable/VariableB_Assign.cs`      |
| Continuous 變數 | `VariableC_Shortage`  | —                                 | `Variable/VariableC_Shortage.cs`    |
| Integer 變數    | `VariableI_Batch`     | —                                 | `Variable/VariableI_Batch.cs`       |
| 限制式          | `Constraint_Coverage` | —                                 | `Constraint/Constraint_Coverage.cs` |

- NEVER 用 `ITEM` / `ROW` 全大寫欄位名，NEVER 用 `SetA` 這種 PascalCase 欄位名 —— ALWAYS `set_<語意>` / `parameter_<語意>` —— Why: `data.set_Item` 一眼看得出是哪種積木，且與類別名區分得開
- Set 成員字串 PascalCase 單數：`"Truck"` ✅、`"truck"` ❌、`"Trucks"` ❌

### 1.4 csproj（照抄，只改三處名字）

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>MyProject</RootNamespace>
    <AssemblyName>MyProject</AssemblyName>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="Generated/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <None Update="Data\**\*.csv" CopyToOutputDirectory="PreserveNewest" />
  </ItemGroup>

  <ItemGroup>
    <Analyzer Include="..\..\dlls\OptimFoundation.Generators.dll" />
  </ItemGroup>

  <ItemGroup>
    <Reference Include="ILOG.Concert"><HintPath>..\..\dlls\ILOG.Concert.dll</HintPath></Reference>
    <Reference Include="ILOG.CPLEX"><HintPath>..\..\dlls\ILOG.CPLEX.dll</HintPath></Reference>
    <Reference Include="NLog"><HintPath>..\..\dlls\NLog.dll</HintPath></Reference>
    <Reference Include="OptimFoundation.Core"><HintPath>..\..\dlls\OptimFoundation.Core.dll</HintPath></Reference>
    <Reference Include="OptimFoundation.Cplex"><HintPath>..\..\dlls\OptimFoundation.Cplex.dll</HintPath></Reference>
  </ItemGroup>

</Project>
```

- 三處要改：`RootNamespace`、`AssemblyName`、資料夾名與 `.csproj` 檔名 → `<Project>`
- `Generated/` 是 generator 產碼落地供人檢視，MUST `Compile Remove`，否則與編譯期輸出撞名
- `Data\**\*.csv` MUST 複製到輸出，否則執行時在 bin 讀不到 input
- DLL 唯一來源是 repo 根的 `dlls/` —— NEVER 用 NuGet、NEVER 用 `ProjectReference` 指向框架 src、NEVER 從 CPLEX Studio 安裝路徑抓 DLL —— Why: `ProjectReference` 只在框架 repo 內部成立，`Projects/` 底下編不過；指向安裝路徑則綁死本機環境。**限定例外**：`$SIB/OptimFoundation/Templates/*` 位於框架 repo 與 solution 內，MUST 保留 framework source / generator 的 `ProjectReference` 及 solver 的 `$(CplexDir)` HintPath，讓 template 隨框架 API 一起 build；例外不得複製到 `Projects/*`。

**✅ 過關條件**：`dotnet build` 成功（此時還沒寫模型，只驗環境）。

### 1.5 generator 做了什麼 — 撰寫 vs 產出對照

你只寫「宣告」，generator 在編譯期補「實作」。產物落在 `Generated/` 底下（csproj 已設 `EmitCompilerGeneratedFiles`），可以打開來看，但 NEVER 手改——下次 build 就被覆蓋。

**Set**：`{類別名}.AutoSets.g.cs`

```csharp
// 你寫的
[OptSet]
[OptDim<string>("Item")]
public sealed partial class Set_Item { }
```

```csharp
// generator 產的
public partial class Set_Item : global::OptimFoundation.Core.SetRowBase
{
}
```

**Parameter**：`{類別名}.AutoSets.g.cs`

```csharp
// 你寫的
[OptParam]
[OptDim<string>("Item")]
[OptDim<DateTime>("Date")]
public sealed partial class Parameter_Demand { }
```

```csharp
// generator 產的
public partial class Parameter_Demand : global::OptimFoundation.Core.ParameterBase
{
    public string Item { get; set; } = string.Empty;
    public global::System.DateTime Date { get; set; }
    public double QTY { get; set; }

    public Parameter_Demand(params object[] sets) => InitClassBySets(sets); // ← 位置式，NEVER 用
    public Parameter_Demand() { } // ← object initializer 走這個
}
```

**Variable**：`{類別名}.AutoSets.g.cs`

```csharp
// 你寫的
[OptVar]
[OptDim<string>("Item")]
[OptDim<DateTime>("Date")]
public sealed partial class VariableB_Assign { }
```

```csharp
// generator 產的
// [VarType=Binary（由類別名前綴決定）] —— 由 AutoSetsGenerator 生成
public partial class VariableB_Assign : global::OptimFoundation.Core.VariableBase
{
    public string Item { get; set; } = string.Empty;
    public global::System.DateTime Date { get; set; }
}
```

**Dataload**：`{類別名}.DataContextRegister.g.cs`（**這是唯一靠繼承關係掃出來的，不靠 attribute**）

```csharp
// 你寫的
public sealed partial class Dataload : DataContext
{
    public List<Set_Item> set_Item = new();
    public List<Parameter_Demand> parameter_Demand = new();
    …
}
```

```csharp
// generator 產的：把你宣告的每個積木欄位登記進 DataContext，供載入時驗證
public partial class Dataload
{
    protected override void RegisterAll()
    {
        RegisterSet("Set_Item", set_Item);
        RegisterSet("Set_Date", set_Date);
        RegisterParam(parameter_Demand, new[] { "Item", "Date" },
            r => new object[] { r.Item, r.Date },
            r => new (string, double)[] { ("QTY", r.QTY) },
            fullGrid: false);
    }
}
```

`RegisterAll()` 就是 §2.4 說的「註冊碼」。它是 `OptData.Load` 呼叫的第一步（`Initialize()` = `RegisterAll()` + `ValidateData()`）。`indexOf` / `numbersOf` 這兩個 lambda 是編譯期產的萃取器——驗證時不必反射，因此 Set／Parameter key 重複與 Parameter 數值 sanity 檢查不影響載入速度。

| 誰            | base                     | 屬性                 | `QTY`  | 建構子                                |
| ------------- | ------------------------ | -------------------- | ------ | ------------------------------------- |
| `Set_*`       | `SetRowBase`                | `List<Set_*>`        | —      | —                                     |
| `Parameter_*` | `ParameterBase`          | 每個 `[OptDim]` 一個 | 一律有 | 位置式 + 無參數各一                   |
| `Variable*_*` | `VariableBase`           | 每個 `[OptDim]` 一個 | —      | **不產**（只能用 object initializer） |
| `Dataload`    | 你自己寫的 `DataContext` | —                    | —      | —                                     |

**除錯用法**：`Generated/` 裡找不到對應的 `.g.cs`，就是這個類別沒被 generator 認到——Set / Parameter / Variable 檢查 attribute 與 `partial`，`Dataload` 檢查 `partial` 與 `: DataContext`（見 §2.4 的無訊號坑）。

### 1.6 新 code 唯一宣告法

**Set 與 Parameter 用同一套宣告法**：光桿 `[OptSet]` / `[OptParam]` / `[OptVar]`，加上每維一個 `[OptDim<T>("Name")]`，其中 **`T` 是該維度的資料型別**（`string` / `DateTime` / `int` / `long` / `double` / `decimal`），不是 Set 積木。`Name` 就是生成 property 名、CSV key 欄名，也是 Set 與 Parameter 之間的連結依據；宣告順序就是 key 順序。

```csharp
// Set：一顆成員名單。維度數 ≥ 1；多維即 tuple Set
[OptSet]
[OptDim<string>("Lot")]
public sealed partial class Set_Lot { }

// Parameter：同一套 [OptDim]，generator 一律再補一欄 QTY
[OptParam]
[OptDim<string>("LotFrom")]
[OptDim<string>("LotTo")]
public sealed partial class Parameter_ChangeoverCost { }
```

- **`[OptSet]` / `[OptParam]` / `[OptVar]` 一律裸寫**，維度全部靠 `[OptDim]` 表達
- **`[OptDim<T>` 的 `T` 是該維度的資料型別**：`string` / `DateTime` / `int` / `long` / `double` / `decimal` —— ★ 這個泛型參數不做型別檢查，填錯不會 compile error，generator 會直接拿它當 property 型別，所以填的必須是資料型別本身
- **Set 至少要有一個 `[OptDim]`**；Parameter 可以零維（scalar），且一律自動補 `QTY`
- 全專案只用這一套宣告法，NEVER 混用其他形式

**維度名稱是 Set ↔ Parameter 的唯一連結**。generator 產出的註冊碼把兩者都按名字登記：

```csharp
RegisterSet("Date", set_Date);                              // Set_Date 的 [OptDim<DateTime>("Date")]
RegisterParam(parameter_Demand, new[] { "Item", "Date" },   // Parameter_Demand 的兩個 [OptDim]
              …, fullGrid: false);
```

所以 Parameter 某個維度要能被驗證（值必須存在於某顆 Set），它的 `[OptDim]` 名稱 MUST 與那顆 Set 的 `[OptDim]` 名稱**逐字相同**。名稱對不上不會 compile error，那個維度只是不受參照完整性保護。

---

## §2 資料層 — Set / Parameter / Dataload

轉譯順序固定：**Set → Parameter → Dataload**。

**Dataload 的唯一目標：把已就位的 CSV 讀成可驗證、可直接注入模型的積木。** 它是資料來源與模型之間的 adapter，不是資料生成器、不是商業邏輯層、不是第二個建模層。

### 2.0 input CSV 的形狀

`Data/*.csv` 是模型輸入的事實來源，形狀由 Model.md 的維度決定。**表頭不是裝飾，也不只是區分第一筆資料：它是 CSV 欄位與積木維度對齊的契約。** Set 的表頭對齊該 Set 積木的成員維度；Parameter 的表頭對齊各 `[OptDim]` 與值欄。**Set 是「有哪些東西」，Parameter 是「這些東西的組合各自值多少」** —— 所以 Parameter 的每個 key 欄，都必須指得到某份 Set 名單上的成員。

**Set = 一份成員名單。** 檔名固定 `Set_<名>.csv`，第一列 MUST 是對齊 Set 成員維度的表頭、其後一列一個成員，保序且不可重複。表頭 MUST 等於各 `[OptDim]` 的名稱（例如 `Set_Item` 掛 `[OptDim<string>("Item")]`，欄名就是 `Item`）；型別由 `[OptDim<T>` 的 `T` 決定，不必自己轉。

```text
Set_Item.csv
Item
ItemA
ItemB
ItemC

Set_Date.csv
Date
2026-01-01
2026-01-02
```

**多維 Set = 由單維 Set 組成的一份 tuple 成員名單。** 每列是一個完整組合，欄名使用 tuple 分量名稱；同一組合不可重複。這適合「合法弧／可行路徑」這類組合本身就是模型集合的情況。

```text
Set_Arc.csv
From,To
Taipei,Taichung
Taichung,Kaohsiung
Kaohsiung,Taipei
```

`Set_Arc` 的成員是三個有序 pair：`(Taipei, Taichung)`、`(Taichung, Kaohsiung)`、`(Kaohsiung, Taipei)`。讀取時依完整欄名對齊維度，**不依 CSV 欄位位置**；因此欄位順序可交換，但每個值仍由其欄名決定角色。若只是「`Item × Date` 各自對應一個需求量」，仍應使用兩個單維 Set 加二維 `Parameter_Demand`，不要把它建成多維 Set。

**Parameter = key 欄 + 值欄的一張表。** 檔名 = 類別名，表頭以欄名對齊每個 `[OptDim]` 的名字與最後一欄 `QTY`，一列一個維度組合、同組合只能出現一次。

```text
Parameter_Demand.csv
ITEM,DATE,QTY
ItemA,2026-01-01,10
ItemA,2026-01-02,12
ItemB,2026-01-01,8
```

```csharp
[OptParam]
[OptDim<string>("Item")] // → 欄 ITEM
[OptDim<DateTime>("Date")] // → 欄 DATE
public sealed partial class Parameter_Demand { } // → 欄 QTY（generator 自動補）
```

**變形一：scalar（零維）** —— 不掛任何 `[OptDim]`，CSV 只有 `QTY` 欄，且 MUST 恰好一列。

```text
Parameter_ShortagePenalty.csv
QTY
1.5
```

```csharp
[OptParam]
public sealed partial class Parameter_ShortagePenalty { } // 無 [OptDim] → 只有欄 QTY
```

**無值組合不是 Parameter。** 只有 key、語意是「這些 tuple 存在」時，MUST 使用多維 Set；`Parameter` 一律有 `QTY`。例如已預先指派的品項：

```text
Set_PreAssign.csv
PreAssign_1
ItemA
ItemC
```

```csharp
[OptSet]
[OptDim<string>("Item")]
public sealed partial class Set_PreAssign { }
```

- MUST 先用維度思考：CSV 每個 key 欄對應一個 `[OptDim<string>("Name")]`；數值欄才是 `QTY`
- Set／Parameter 的跨表關聯是專案資料語意；framework 不會自動推導或驗證 `ItemZ` 是否出現在另一份 Set CSV。需要此規則時，MUST 由資料驗收或查找邏輯明確處理。
- MUST 讓 CSV 成為模型輸入的可檢視事實來源；換一批資料只換 CSV，**Dataload、Objective、Constraint 一行都不改**

**兩個容易混的資料責任，先分清楚**（細節見 §2.4）：

|                | 在問什麼                      | 例                                              | 預設                           |
| -------------- | ----------------------------- | ----------------------------------------------- | ------------------------------ |
| **資料列 key 唯一性** | 同一份 Set／Parameter 的 key 有沒有重複？ | 同一組 `Item,Date` 重複兩列 | framework 一律驗證 |
| **跨表關聯與完整矩陣** | key 是否在指定 Set、是否寫滿所有組合？ | `ItemZ` 不在 Set、或 3 × 2 只給 4 列 | 專案資料語意，明確驗收 |

MILP 的 parameter 本來就多半稀疏——沒列到的組合代表「這組合不存在」，不是資料漏了。因此 framework 只做 key 重複與數值 sanity；跨表關聯或完整矩陣不得靠 schema 猜測。

**框架怎麼讀這些檔（Parameter）**

| 規則       | 行為                                                                                |
| ---------- | ----------------------------------------------------------------------------------- |
| 表頭       | **MUST 存在**；欄名用來對齊 `[OptDim]` 與值欄，資料從第二列開始                     |
| 按欄名對位 | 大小寫不敏感（`ITEM` = `Item`）、欄序可以與 property 順序不同、**多餘的欄自動忽略** |
| 缺欄       | 丟 `InvalidDataException`，訊息列出「這個類別需要哪些欄、檔內表頭有哪些」           |
| 無表頭     | 丟 `InvalidDataException`；表頭是必要的，欄位一律按名稱對位                         |
| 數值       | 小數點用 `.`；NEVER 寫千分位逗號（逗號是欄位分隔符）                                |
| `DateTime` | 標準輸入契約用 `yyyy-MM-dd`；mapper 以 invariant culture 的 `DateTime.Parse` 轉型，可能容忍其他 invariant 表示法，但新資料與教學不得依賴這個容忍度 |
| 檔名       | 省略 `Load<T>("...")` 的參數時 = 型別名；本規範一律**顯式帶上**，與類別名相同  |

- **所有 input CSV MUST 寫表頭**：Set 與 Parameter 以表頭對齊積木維度；以 `LoadData` 讀取的 raw CSV 則以表頭定義其獨立 `DataTable` 欄位。
- Set 的 CSV 是單欄、表頭為 Set 名、保序；多維 Set 的欄名是 attribute 指定的分量名（例如 `From`,`To`），讀取時依完整欄名對齊積木維度，不依 CSV 欄位位置，檔名固定 `Set_<名>.csv`。
- `Solution/*.csv` 的表頭欄名就是 property 名，所以在欄位相容時可由 `Load` 讀回當下一輪的輸入（多出來的 `VAR_TYPE` 欄會被自動忽略）。

**culture 與日期的邊界**：數值一律採 invariant culture、小數點為 `.`、不寫千分位；日期正式契約一律 `yyyy-MM-dd`。parser 額外接受的 invariant 日期格式只屬實作容忍度，不可作為 CSV schema 或範例的依據，避免不同機器／資料來源產生不可重現的解析結果。

**少寫表頭會怎樣**

不再做「第一行最後一欄能否轉數字」的猜測。第一列一律被視為表頭；任一 input CSV 少寫表頭都會在後續 schema 驗證失敗，且原本的第一筆資料不會成為資料列。

❌ Bad —— `Parameter_PreAssign.csv` 沒有表頭

```text
ItemA
ItemC
```

```text
InvalidDataException:
[CsvCtrl] Parameter_PreAssign.csv 表頭缺少欄位 'Item'。Parameter_PreAssign 需要：Item；檔內表頭：ITEMA
```

★ **判讀訣竅**：先確認第一列是否為欄名；框架不會再從資料內容推測表頭。

✅ Good

```text
ITEM
ItemA
ItemC
```

含 `QTY` 的 Parameter 也同樣 MUST 有表頭；欄位只按名稱對位，不能依 property 宣告順序省略表頭。

```text
Parameter_Demand.csv
ITEM,DATE,QTY
ItemA,2026-01-01,10
```

**「已就位」是哪裡**：`Data/` 這個名字對到兩個實體位置，執行期讀的永遠是後者。

| 位置                        | 誰放進去                         | 角色                                                                        |
| --------------------------- | -------------------------------- | --------------------------------------------------------------------------- |
| 專案 `Data/*.csv`           | 你手維護，或從 bin 搬回          | 版控裡的事實來源；csproj `Data\**\*.csv` PreserveNewest 在 build 時複製過去 |
| `bin/<Config>/net8.0/Data/` | build 複製，或 `Export()` 直接寫 | **`CsvDataSource` 實際讀的資料夾**                                          |

`FolderDir.Data` 的路徑根是 `AppDomain.BaseDirectory`（執行檔目錄），不是專案目錄。`new CsvDataSource()` 建構時就會把該資料夾建好，所以缺檔會是明確的 `FileNotFoundException`，不是 `DirectoryNotFoundException`。

**CSV 還不存在 → `import` 模式（選用）**：寫一個 `Dataload(string rawFile)` 建構子，跑一次 import 產出上表的標準 CSV。兩種情境都走這條，寫在同一個建構子裡：

| 情境              | rawFile 是什麼                   | 建構子內做什麼 |
| ----------------- | -------------------------------- | -------------- |
| 不規則來源        | 9×9 矩陣、原始報表               | 攤平成積木     |
| 生成測試 instance | 生成規格（規模、seed、比例分佈） | 依規格算出積木 |

生成情境也 MUST 有 rawFile：把「用什麼條件生的」寫成 `Data/raw/<instance>.csv`，否則這批資料無法重現。

```powershell
dotnet run -- import raw/Puzzle_SHC279
```

原始檔放 `Data/raw/`，產出的 CSV 落在 `bin/.../Data/`（`FolderDir.Data`）。**這是資料準備工序，不是求解流程的一部分**——CSV 一旦就位，求解模式就只讀不算。

⚠️ `Export()` 只寫到 bin 的輸出目錄，`dotnet clean` 或換 configuration 就沒了。這批 CSV 要長期當 input（進版控、之後每次求解都用同一份）→ MUST 手動搬回專案 `Data/`，讓 csproj 的 `Data\**\*.csv` copy 規則接手；只是這一輪拋棄式試跑就留在 bin。

### 2.1 Set 積木 — 一顆一個檔

```csharp
// Set/Set_Item.cs
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>可生產的品項；對應 Model.md 的 ITEM。</summary>
    [OptSet]
    [OptDim<string>("Item")]
    public sealed partial class Set_Item { }
}
```

```csharp
// Set/Set_Date.cs
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>規劃期間的每一天；對應 Model.md 的 DATE。</summary>
    [OptSet]
    [OptDim<DateTime>("Date")]
    public sealed partial class Set_Date { }
}
```

**多維 Set** 用來表示「存在的 tuple 本身就是集合成員」，例如可行弧 `ARC ⊆ NODE × NODE`。每個 tuple 一列，只有列出的組合存在；它不是把兩個單維 Set 做完整笛卡兒積。

```csharp
// Set/Set_Arc.cs
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>可行的有向弧（來源節點、目的節點）；對應 Model.md 的 ARC。</summary>
    [OptSet]
    [OptDim<string>("From")]
    [OptDim<string>("To")]
    public sealed partial class Set_Arc { }
}
```

generator 會產生繼承 `SetRowBase` 的 row class；載入結果為 `List<Set_Arc>`。因此迭代時可直接使用具名 property 或 deconstruct：

```csharp
foreach (var arc in set_Arc)
{
    string from = arc.From;
    string to = arc.To;
}
```

```text
Set_Arc.csv
From,To
Taipei,Taichung
Taichung,Kaohsiung
Kaohsiung,Taipei
```

- 多維 Set 就是掛多個 `[OptDim]`，寫法與單維、與 Parameter 完全一致；沒有上限以外的特別語法
- CSV 欄名固定為各 `[OptDim]` 的名稱；讀取時按欄名映射，所以 CSV 實體欄位可調換，但每列的值必須跟著其欄名
- `(Taipei, Taichung)` 與 `(Taichung, Taipei)` 是不同成員；完全相同的 tuple 重複才會報錯
- 無值、只代表存在的組合 MUST 用多維 Set；若每組還有需求量、成本等數值，改用「單維 Set + 多維 `Parameter` 的 `QTY`」

**Parameter 不會去驗證多維 Set 的 tuple**。Parameter 的每個 `[OptDim]` 是獨立維度，各自按名稱對回某顆 Set；框架不會檢查「這一組 `(From, To)` 是否存在於 `Set_Arc`」。要讓組合本身受約束，就在建模時只對 `Set_Arc` 的成員展開變數與限制式，不要指望資料驗證幫你擋。

```csharp
// Parameter/Parameter_ArcCost.cs
[OptParam]
[OptDim<string>("From")]
[OptDim<string>("To")]
public sealed partial class Parameter_ArcCost { } // From, To, QTY
```

```text
Data/Set_Node.csv                 # 單維 Set_Node
Node
Taipei
Taichung
Kaohsiung

Data/Set_Arc.csv                  # 多維 Set_Arc：無值、可行 arc 名單
FROM,TO
Taipei,Taichung
Taichung,Kaohsiung
Kaohsiung,Taipei

Data/Parameter_ArcCost.csv        # Parameter：有值、每個 key 對應單維 Set_Node
FROM,TO,QTY
Taipei,Taichung,120
Taichung,Kaohsiung,85
Kaohsiung,Taipei,140
```

| 資料                | C# 宣告                                                            | CSV key 欄               | 驗證範圍                          |
| ------------------- | ------------------------------------------------------------------ | ------------------------ | --------------------------------- |
| `Set_Arc`           | `[OptSet]` + `[OptDim<string>("From")]` + `[OptDim<string>("To")]` | `FROM`, `TO`             | tuple 不可重複                    |
| `Parameter_ArcCost` | `[OptParam]` + 同樣兩個 `[OptDim<string>]`                         | `FROM`, `TO`，另加 `QTY` | 各維度按**名稱**對回同名 Set 維度 |

因此 `Set_Arc.csv` 與 `Parameter_ArcCost.csv` 是**兩份不同 schema 的檔案**，目前不能共用。資料準備時 MUST 確保 `Parameter_ArcCost` 只列 `Set_Arc` 中存在的 pair；框架尚未自動驗證這個跨積木子集合關係。

- `[OptDim<T>` 的合法型別：`string` / `DateTime` / `int` / `long` / `double` / `decimal`
- **`[OptSet]` 一律裸寫**，元素型別寫在 `[OptDim<T>]`
- NEVER 用裸 `List<string>` 當 Set —— Why: 積木化才會被 `DataContext` 註冊進驗證，裸 List 永遠驗不到

Set row collection 一律以 `List<Set_*>` 保存，可直接 `foreach` / LINQ / 傳進 `BuildVars`。框架驗證 Set 維度 key 不可重複；資料是否為空與資料語意由專案的驗收規則判定。

### 2.2 Parameter — 一份一個檔

```csharp
// Parameter/Parameter_Demand.cs — 含值參數：generator 補 Item / Date / QTY
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>各品項各日的需求量；對應 Model.md 的 DemandQty_{Item,Date}。</summary>
    [OptParam]
    [OptDim<string>("Item")]
    [OptDim<DateTime>("Date")]
    public sealed partial class Parameter_Demand { }
}
```

```csharp
// Set/Set_PreAssign.cs — 純 key tuple（只有存在與否，不帶數值）
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>已預先指派的品項；對應 Model.md 的 PREASSIGN。</summary>
    [OptSet]
    [OptDim<string>("Item")]
    public sealed partial class Set_PreAssign { }
}
```

```csharp
// Parameter/Parameter_ShortagePenalty.cs — 零維 scalar：generator 只補 QTY
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>短缺一單位的懲罰成本；對應 Model.md 的 ShortagePenalty。</summary>
    [OptParam]
    public sealed partial class Parameter_ShortagePenalty { }
}
```

| 規則           | 說明                                                                                                                     |
| -------------- | ------------------------------------------------------------------------------------------------------------------------ |
| 數值欄位名     | **一律 `QTY`**，NEVER `Quantity` / `Amount`（generator 生成的就叫 QTY）                                                  |
| property 名    | `[OptDim<T>("Name")]` 的字串，PascalCase，NEVER `ALL_CAPS`                                                               |
| attribute 順序 | = key 組成順序，改順序 = 改 key                                                                                          |
| 建構物件       | **只允許 object initializer**：`new Parameter_Demand { Item = item, Date = date, QTY = 5 }`                              |
| 位置式建構子   | generator 有產，但 **NEVER 使用** —— Why: 改 `[OptDim]` 順序時 `new Parameter_Demand(a, b)` 不會報錯，只會靜默把維度接錯 |
| 自寫建構子     | **NEVER** —— 框架用 reflection 讀屬性順序組 key                                                                          |
| 全格資料語意       | framework 不提供自動全格覆蓋檢查；若題目要求完整矩陣，MUST 由專案驗證規則明確檢查並在 `<summary>` 說明判準              |

**全格資料語意判準**：它的目的是把「資料缺了」和「值真的是 0」分開。framework 不會自行枚舉笛卡兒積；題目要求完整矩陣時，專案 MUST 在資料驗收中檢查缺格並明講缺哪一格，不能把缺列默認成 0。

- 缺一格代表**資料有問題** → 加。例：`Parameter_Capacity{Machine,Date}`（每台機每天都該有產能值）
- 缺一格代表**這組合不存在** → 不加。例：`Parameter_PreAssign{Item}`（只列有預先指派的）、`Parameter_Distance{From,To}`（只列連通的點對）——這類標了會整片誤報，逼你補假資料

**何時要報錯**：資料驗收在 `OptData.Load` 後、建模前執行。例：`Machine` 2 個 × `Date` 31 天 = 62 格，CSV 只有 60 列時，專案驗收 MUST 報出缺少的 key；格數過大時至少回報預期與實際列數。稀疏 parameter 不做這項檢查。

```csharp
/// <summary>各機台各日產能；對應 Model.md 的 Capacity_{Machine,Date}。
/// 全格資料語意：排程期間每台機每天都必須有產能值，缺格是資料漏了，不是「產能為 0」。</summary>
[OptParam]
[OptDim<string>("Machine")]
[OptDim<DateTime>("Date")]
public sealed partial class Parameter_Capacity { }
```

零維 scalar Parameter 沒有 `[OptDim]`，CSV 只有 `QTY` 欄且 MUST 恰好一筆。它仍然是 `List<Parameter_X>`（`Load` 一律回 List），所以取值要自己收斂成一個數：

```csharp
// Program.cs 材料段 —— ✅ Good
double shortagePenalty = data.parameter_ShortagePenalty.Single().QTY;
// 再逐項注入
.AddObjective(engine => new ObjectiveFunction(data.set_Item, shortagePenalty).Build(engine));
```

```csharp
// ❌ Bad
double p1 = data.parameter_ShortagePenalty.FirstOrDefault()?.QTY ?? 0.0;
double p2 = data.parameter_ShortagePenalty.First().QTY;
double p3 = data.parameter_ShortagePenalty.Sum(x => x.QTY);
```

**NEVER 用 `FirstOrDefault` / `First` / `Sum`** —— Why: scalar 的兩種資料錯誤都必須當場炸開：

| CSV 實際內容                 | `.Single()`                              | `.FirstOrDefault()?.QTY ?? 0.0`                                           |
| ---------------------------- | ---------------------------------------- | ------------------------------------------------------------------------- |
| 0 列（只有表頭，或檔案空的） | 丟 `InvalidOperationException`，當場知道 | 得到 **0.0** → 目標式那一項係數變 0 **整項消失**，solver 照樣回 `Optimal` |
| 2 列（複製貼上多一行）       | 丟 `InvalidOperationException`           | 靜默取第一筆，另一筆的存在永遠沒人發現                                    |

`.Single()` 的例外訊息不會告訴你是哪個檔，所以看到它先去 `Data/Parameter_*.csv` 數行數。

**同一顆 Set 當多個維度（`LotFrom` / `LotTo`）**

換線成本、距離矩陣、前後關係這類「同一份名單自己對自己」的資料，兩個維度都來自同一顆 Set，只是角色不同。`[OptDim<T>("Name")]` 的字串就是拿來取角色名的——**這是它被定為唯一 paved path 的理由**。

```csharp
// Set/Set_Lot.cs
[OptSet]
[OptDim<string>("Lot")]
public sealed partial class Set_Lot { }
```

```csharp
// Parameter/Parameter_SetupCost.cs
/// <summary>從前一批換到後一批的換線成本；對應 Model.md 的 SetupCost_{LotFrom,LotTo}。</summary>
[OptParam]
[OptDim<string>("LotFrom")] // → property LotFrom（string），欄 LOTFROM
[OptDim<string>("LotTo")] // → property LotTo（string），欄 LOTTO
public sealed partial class Parameter_SetupCost { }
```

```csharp
// Variable/VariableB_Sequence.cs —— 變數同理，兩維同源
/// <summary>是否緊接在該批之後生產；對應 Model.md 的 Sequence_{LotFrom,LotTo} ∈ {0,1}。</summary>
[OptVar]
[OptDim<string>("LotFrom")]
[OptDim<string>("LotTo")]
public sealed partial class VariableB_Sequence { }
```

```text
Data/Parameter_SetupCost.csv
LOTFROM,LOTTO,QTY
LotA,LotB,30
LotB,LotA,45
```

```csharp
// 建立與使用：兩個維度都傳同一顆 Set
engine.BuildVars<VariableB_Sequence>(lots, lots);
engine.AddLHS(cost, new VariableB_Sequence { LotFrom = from, LotTo = to });
```

- ★ **角色名會使該維度脫離參照檢查**：維度靠**名稱**對回 Set（§1.6），`LotFrom` / `LotTo` 與 `Set_Lot` 的維度名 `Lot` 不同字，所以框架**不會**檢查 `LotZ` 是否存在於 `Set_Lot.csv`。要保住這道檢查，就讓 Parameter 的維度名與 Set 的維度名逐字相同（兩維都叫 `Lot` 則無法區分角色），或另外建一顆維度名為 `LotFrom` 的 Set。**取角色名 = 用可讀性換掉一道靜默錯誤的防線，取捨要自己想清楚**
- 不要求寫滿 `8 × 8 = 64` 列，稀疏是常態；題目若要求完整矩陣，應由專案資料驗收明確檢查。
- 對角線（`LotA,LotA`）要不要排除是**建模決定**，寫成一條 constraint，NEVER 靠「CSV 裡不放那幾列」暗示

### 2.3 結構常數也是 Parameter（天條）

模型裡的維度／結構常數（Sudoku 的 3×3、時間窗長度 7、班別數）**一律做成 Set 或 Parameter** —— NEVER 寫成迴圈邊界的字面數字。

❌ Bad

```csharp
for (int blockRow = 0; blockRow < 3; blockRow++)
    for (int columnOffset = 1; columnOffset <= 3; columnOffset++)
        engine.AddLHS(1.0, new VariableB_CellDigit { Row = blockRow * 3 + columnOffset, Column = column, Digit = digit });
```

✅ Good

```csharp
// Set/Set_Block.cs + Parameter/Parameter_BlockCell.cs（{Block,Row,Column}），從 CSV 讀進來
foreach (int block in blocks)
{
    foreach (var cell in blockCells.Where(c => c.Block == block))
        engine.AddLHS(1.0, new VariableB_CellDigit { Row = cell.Row, Column = cell.Column, Digit = digit });
}
```

Why: 寫死 3 的模型換成 4×4 或 16×16 就得改 code，違反「換資料不改 code」；做成資料後換 CSV 即可。迴圈邊界一律用 `foreach (var x in set)` 或 `set.Count`。

### 2.4 Dataload — 讀已就位的 CSV

```csharp
// Data/Dataload.cs
using OptimFoundation.Core;
using OptimFoundation.Core.IO;

namespace MyProject
{
    /// <summary>資料唯一入口：把 Data/*.csv 讀成積木，再由 DataContext 驗證。</summary>
    public sealed partial class Dataload : DataContext
    {
        public List<Set_Item> set_Item = new();
        public List<Set_Date> set_Date = new();
        public List<Parameter_Demand> parameter_Demand = new();
        public List<Parameter_ShortagePenalty> parameter_ShortagePenalty = new();

        /// <summary>無參數建構子只做一件事：把預設來源餵給下面真正讀檔的建構子。</summary>
        public Dataload() : this(new CsvDataSource()) { }

        /// <summary>標準接口：一行載一顆，只讀不算。換 CSV / Oracle / 記憶體只換 source。</summary>
        public Dataload(IDataSource source)
        {
            set_Item = source.Load<Set_Item>("Set_Item");
            set_Date = source.Load<Set_Date>("Set_Date");
            parameter_Demand = source.Load<Parameter_Demand>("Parameter_Demand");
            parameter_ShortagePenalty = source.Load<Parameter_ShortagePenalty>("Parameter_ShortagePenalty");
        }
    }
}
```

**constructor chaining 規則**：`Dataload()` 只負責建立預設 `CsvDataSource` 後轉交 `Dataload(IDataSource source)`；所有讀取敘述只放在後者，因此 CSV、DB 與 `InMemoryDataSource` 共用同一條 row-loading 流程。同一類別不得宣告兩個簽名相同的無參數建構子；需要替換來源時，測試或呼叫端應直接傳入另一個 `IDataSource`。

**`Dataload(IDataSource source)` 的敘述白名單 —— 只允許兩種句子，沒有第三種**：

「白名單」= 只有名單上的可以出現，其他一律不行（不是「列出來的不能用、其他隨意」）。一顆積木一行：

1. `set_X = source.Load<Set_X>("sourceName");`
2. `parameter_X = source.Load<Parameter_X>("Parameter_X");`

❌ 以下全部禁止，一個都不行：

```csharp
// ❌ 迴圈生資料
foreach (var offset in Enumerable.Range(0, 7))
    generatedDates.Add(new Set_Date { Date = startDate.AddDays(offset) });

// ❌ 判斷 / 補值 / 預設值
if (parameter_Demand.Count == 0)
    parameter_Demand.Add(new Parameter_Demand { Item = "ItemA", QTY = 0 });

// ❌ 從別的資料反推 set
set_Item = importedItemRows;

// ❌ 邊讀邊算
parameter_Cost = raw.Select(r => new Parameter_Cost { QTY = r.Price * 1.05 }).ToList();
```

- NEVER 在這個建構子出現 `for` / `foreach` / `Enumerable.Range` / `Random` / enum 掃描 / 日期運算 / `if` / 補值 / 預設值
  ALWAYS 把這些搬到 §2.0 的 import 階段，先產出 CSV
  Why: 這個建構子的職責是**搬運，不是加工**。一旦裡面有迴圈或判斷，那批數字就只存在於 code 裡——你打開 `Data/*.csv` 看到的東西，跟 solver 實際拿到的東西不一樣。後果是換一批資料要改 `.cs`、review 看不出實際跑的是什麼數字、對不上時也無從比對

- MUST `public sealed partial class Dataload : DataContext`（`partial` + 繼承缺一不可）—— Why: generator 是靠「有 `partial` 且繼承 `DataContext`」兩個條件掃出這個類別的，少任一個它**連被掃到都沒有**——不是報錯，是直接跳過。沒有 compile error、沒有 warning、執行時也不會抱怨，這顆 Dataload 就是一輩子不受驗證。這是本規範裡唯一完全沒有訊號的坑
- MUST 建構走 `OptData.Load(() => new Dataload())` —— NEVER 把裸 `new Dataload()` 當終點 —— Why: 裸 new 仍可編譯，但會跳過 Set／Parameter key 重複與 Parameter 數值 sanity 驗證，壞資料可能直接進 solver 產出「看起來最佳」的錯答案
- MUST 把 penalty、capacity、bound、Big-M 輸入等模型數值都做成 Parameter CSV —— NEVER 寫成 Dataload 的 hardcode public field
- framework 已處理 key 重複與數值 sanity；跨 Set 關聯、完整矩陣與業務 domain 則必須由專案驗收明確實作，不能假設 framework 會自動推導
- NEVER `try/catch` 吞 `DataValidationException` —— 它刻意一次列出全部問題後 fail-fast
- 由資料推導的比值（Big-M 之類）用 `Numeric.SafeRatio(分子, 分母, context: "BigM")`，NEVER 手寫裸除法

真的需要加工 → 搬到 **import 建構子**（`Dataload(string rawFile)`），先跑一次把結果 `Export()` 成標準 CSV，再由上面這個建構子原封讀進來。加工的痕跡就落在檔案上，看得見也比得了：

```powershell
dotnet run --project <project.csproj> -- import raw/<file>   # 加工 → 產 CSV
dotnet run --project <project.csproj>                        # 只讀，不算
```

一句話：**`Dataload(IDataSource)` 只讀不算，要算的都得先變成檔案。**

**怎麼確認驗證真的有跑**：`OptData.Load` 成功後一定會印「資料載入摘要」，列出每顆 Set 幾個成員、每個 Parameter 幾列。**摘要印出 `Sets（0）` / `Parameters（0）` = 註冊碼沒產生**（多半就是漏了 `partial` 或漏了 `: DataContext`），此時驗證等於空跑。第一次跑起來 MUST 看這段數字對不對得上題目，這是唯一能抓到上面那個無訊號坑的地方。

**為什麼一定要驗（MILP 特有的失敗模式）**

一般程式壞資料會 crash；**MILP 的壞資料不會 crash，它會回一個可行、最佳、但答錯的解**。solver 只保證「它拿到的那個模型」被解對，不保證那個模型 = 你的題目。framework 的三項自動檢查與專案資料驗收各自負責不同風險：

| 檢查                                                                                   | 預設                           | 沒驗會發生什麼                                                                                                               |
| -------------------------------------------------------------------------------------- | ------------------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| **Set／Parameter key 唯一性** | 一律做 | 同一 key 兩筆，查找可能取到非預期的第一筆，模型使用錯的係數 |
| **數值 sanity**（`NaN` / `±Infinity` / 量級 > `1e15`） | 一律做 | `NaN` 進係數讓 CPLEX 行為未定義；超大係數讓 LP relaxation 數值不穩 |
| **跨表關聯與完整矩陣** | 專案明確驗收 | 缺列被採 0、略過或其他預設時，模型可能靜默變成另一題 |

★ 前兩項是 framework 一律執行、關不掉；跨表關聯與完整矩陣一律由專案資料驗收明確決定。**MILP 的 parameter 資料預設就是稀疏的**——沒列到的組合通常代表「不存在」，不是缺漏，所以 framework 不預設要求寫滿。

**Parameter 查找**：Set-driven loop 要查 sparse Parameter 時，一律使用 `FindParameterOrLog`。它找不到資料會寫 `PARAMETER_NOT_FOUND` warning 並回傳 `null`；採 0、跳過或其他預設值由該模型的資料語意決定。

```csharp
var row = parameter_Demand.FindParameterOrLog(
    parameter => parameter.Item == item,
    item);
double demand = row?.QTY ?? 0.0;
```

### 2.4.1 子集合迭代與 Parameter 查找

**Set 決定迭代 domain；Parameter 提供該 domain 成員的係數。** `FindParameterOrLog` 不建立子集合，也不會替模型決定哪些變數或限制式存在；它只針對目前迭代到的 key 查找一筆 Parameter row。

| 情境 | 寫法 | 原因 |
| --- | --- | --- |
| 子集合本身是題目資料，例如可行弧、可指派組合 | 建成獨立 `Set_*`，直接 `foreach` | 子集合決定 variable／constraint 的 domain，必須是可檢視、可載入的 input 事實 |
| 子集合只屬於某一條式子的暫時條件 | 對既有 `List<Set_*>` 使用 `Where(...)` 後迭代 | 不另增模型元素，限制範圍只存在於該條 Constraint／Objective |
| 需要子集合成員的成本、需求或容量 | 在迭代內呼叫 `FindParameterOrLog` | 查找與缺值 warning 保持可追蹤，缺值策略由模型明確決定 |

例如 `Set_Arc` 是可行弧集合；某一條限制式只處理從指定節點出發的弧時，先篩出暫時子集合，再以該弧的兩個維度查成本：

```csharp
var outgoingArcs = arcs.Where(arc => arc.From == node);

foreach (var arc in outgoingArcs)
{
    var cost = arcCosts.FindParameterOrLog(
        row => row.From == arc.From && row.To == arc.To,
        arc.From, arc.To);

    double unitCost = cost?.QTY ?? 0.0;
    engine.AddLHS(unitCost, new VariableB_UseArc { From = arc.From, To = arc.To });
}
```

若「可行弧」本身決定要不要建立 `VariableB_UseArc`，建變數時也 MUST 使用同一份 `Set_Arc`：

```csharp
engine.BuildVars<VariableB_UseArc>(arcs);
```

不要反過來從 Parameter 的存在與否推導 Set 子集合；這會讓缺少係數、資料缺漏與「該組合本來不存在」混為一談。需要完整矩陣或強制係數存在時，應在專案資料驗收中明確檢查。

前兩項都會產生「build 過、solve 過、Status = Optimal、數字看起來合理」的結果，事後極難回溯。驗證是唯一能在 solve 之前把它變成 fail-fast 的機制，所以它綁在 `OptData.Load` 而不是可選步驟。

**Set 載入方式**：

| 方式                     | 寫法                                 | 何時用                                                                                     |
| ------------------------ | ------------------------------------ | ------------------------------------------------------------------------------------------ |
| **`Load<T>(sourceName)`** | `set_Item = source.Load<Set_Item>("Set_Item");` | 唯一模型 row 讀取 API；名稱可顯式指定，省略時才用型別名 |

- `sourceName` 可與類別名不同；省略時才使用 `typeof(T).Name`。改名或複用來源時應明確指定名稱。
- NEVER 從 parameter 反推 Set；應由 input CSV、DB query 或 import 階段明確產生 `List<Set_X>`，避免遺漏題目允許但當期沒出現的成員。
- 元素型別非 string（`DateTime` / `int` / `double`…）不必自己 parse，`Load<T>` 會依 property 型別自動轉型。

**import 建構子與 `Export()`（CSV 還不存在時才需要）**

位置固定在**同一個 `Data/Dataload.cs`**，就在 `Dataload(IDataSource)` 旁邊當第二個建構子 —— NEVER 另開 `DataGenerator.cs` 或 `Data/import/` 資料夾（違反八資料夾與一型別一檔）。生成邏輯再長也留在這裡：它是同一顆積木的另一種來源，分檔只會讓「這批 CSV 哪來的」變難追。

```csharp
/// <summary>import 模式：攤平 Data/raw/ 的不規則來源，或依生成規格產出 instance。rawFile 相對於 Data/、不帶副檔名。</summary>
public Dataload(string rawFile)
{
    var grid = new CsvDataSource().LoadData(rawFile);
    // 依題目形狀攤平／依規格產生 List<Set_*> / List<Parameter_*>；這是全專案唯一允許出現迴圈、Random 與日期運算的地方
}

/// <summary>把積木寫成標準 CSV，成為求解模式的 input。檔名 MUST 與上面讀取時一致。</summary>
public void Export()
{
    CsvCtrl.WriteRows(set_Item, "Set_Item");
    CsvCtrl.WriteRows(parameter_Demand, "Parameter_Demand");
    Logging.Info("[import] exported: Set_Item, Parameter_Demand"); // 產出清單當場可見
}
```

**檔名對齊靠人**：`Export()` 寫的每個名稱都要在 `Dataload(IDataSource)` 對應 `source.Load<T>("sourceName")`。compiler 不會驗證這件事；少寫一顆時 import 仍可能 exit 0，所以 MUST 保留產出清單 log，並在交付 checklist 逐一核對。

寫出位置是 `FolderDir.Data`（bin 下的輸出目錄），不是專案 `Data/`。要保留成正式 input 就把產物搬回專案 `Data/`（同名覆蓋），下次 build 由 copy 規則帶回 bin；不搬 = 這批資料隨 bin 一起丟掉。

**✅ 過關條件**：

- `OptData.Load(() => new Dataload())` 不丟例外，且各 Set 的 `Count` 與題目一致
- Model.md 的每顆 Set / Parameter 都能對到一個 CSV 與一行載入敘述
- `Dataload(IDataSource)` 內只有白名單那兩種句子
- 換一批合法 CSV 後，不必修改任何 `.cs`

---

## §3 變數層 — 一支一個檔

```csharp
// Variable/VariableB_Assign.cs
using OptimFoundation.Modeling;

namespace MyProject
{
    /// <summary>品項是否在該日生產；對應 Model.md 的 Assign_{Item,Date} ∈ {0,1}。</summary>
    [OptVar]
    [OptDim<string>("Item")]
    [OptDim<DateTime>("Date")]
    public sealed partial class VariableB_Assign { }
}
```

**前綴是 load-bearing，不是命名慣例**：

| 前綴         | 型別       | `BuildVars<T>` 給的界限                                                      |
| ------------ | ---------- | ---------------------------------------------------------------------------- |
| `VariableB_` | Binary     | `[0, 1]`                                                                     |
| `VariableC_` | Continuous | `[0, 1E100]`（1E100 = 框架的「無上限」慣用值，不是 solver 的 infinity 常數） |
| `VariableI_` | Integer    | `[0, 1E100]`                                                                 |

**前綴被讀兩次，兩次都會影響變數怎麼被建出來**：

| 何時   | 誰讀                       | 做什麼                                                              | 前綴非法會怎樣                                        |
| ------ | -------------------------- | ------------------------------------------------------------------- | ----------------------------------------------------- |
| 編譯期 | generator（`[OptVar]`）    | 依前綴決定產出的變數型別與 base                                     | compile error `OPTF001`，訊息教正確取名               |
| 執行期 | `engine.BuildVars<T>(...)` | 讀 `typeof(T).Name` 的前綴決定 `VarType` 與 lb/ub，再展開成實際變數 | 丟 `ArgumentException`，log `[VARIABLE_TYPE_UNKNOWN]` |

所以**改類別名就是改數學模型**：把 `VariableC_Produce` 改名成 `VariableB_Produce`，沒有任何一行 code 需要跟著改、IDE rename 也不會警告，但那個變數當場從「連續產量」變成「0/1 開關」，模型換成另一題。

- MUST 前綴與 Model.md `VAR` 段標的型別逐一對上（`Binary` → `B_`、`Continuous` → `C_`、`Integer` → `I_`）
- NEVER 為了語意好聽改前綴（`VariableB_Quantity` 這種）—— 前綴不是形容詞，是型別宣告

- **一般情況優先 `engine.BuildVars<T>(sets...)`**：它由類名前綴判定型別，是最簡潔且最不易重複表達型別的入口。
- **`BuildBVs<T>` / `BuildCVs<T>` / `BuildIVs<T>` 都是有效的明確 builder**；在維護既有明確型別建構程式，或 continuous／integer variable 需要自訂 lb/ub 時使用。型別前綴與選用的 builder 必須一致，否則 framework 會記錄 `VARIABLE_TYPE_MISMATCH` 後拋例外。
- Model.md 的一般容量／需求界限仍應忠實寫成 `Constraint_*`；只有 variable 的技術性 bounds 才使用 `BuildCVs<T>(lb, ub, sets)` 或 `BuildIVs<T>(lb, ub, sets)`。
- **NEVER 手寫 `: VariableBase` 並自己宣告 property** —— ALWAYS 用 `[OptVar]` + `[OptDim]` —— Why: 手寫的 property 順序與 `[OptDim]` 沒有單一真相，接錯不報錯
- 前綴非 `B_`/`X_`/`I_` → compile error `OPTF001`（錯誤訊息會教正確取名）
- `[OptVar]` 裸寫，型別完全由前綴決定，attribute 不帶任何參數

**MUST 傳入 sets 的順序 = `[OptDim]` 宣告順序** —— Why: 順序錯了不會報錯，只會靜默把索引接錯，最後解出一個「別的題目」的答案。

**怎麼建立出來 —— 宣告只是型別，`BuildVars` 才真的產生變數**

建立寫在 `Program.cs` 的模型段，**每種變數一行**（不在 Constraint 裡建）：

```csharp
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.set_Item, data.set_Date))
    .AddVariables(engine => engine.BuildVars<VariableC_Shortage>(data.set_Item))
    …
```

`BuildVars` 做的是**笛卡兒積展開**：3 個 Item × 2 個 Date → 一次建出 6 顆變數，名字是 `類別名@維度值@維度值`。

**runtime error code 與對策**：

| Error Log | 原因 | 對策 |
| --- | --- | --- |
| `VARIABLE_ARITY_MISMATCH` | 傳入 Set row 展開後的維度數，與 Variable 的 `[OptDim]` 數量或順序不一致 | 逐項比對 Variable 的 attribute 順序與 builder 傳入集合的展開寬度 |
| `VARIABLE_TYPE_MISMATCH` | `VariableB_`／`VariableC_`／`VariableI_` 前綴和明確 builder 不一致 | 改用 `BuildVars<T>`，或選擇與前綴相符的 `BuildBVs`／`BuildCVs`／`BuildIVs` |
| `VARIABLE_NOT_FOUND` | Constraint 引用的 variable key 未被建立，或 property／domain 與建立時不一致 | 核對 `[OptDim]`、`BuildVars` domain、object initializer property 與索引順序 |

```text
VariableB_Assign@ItemA@2026-01-01
VariableB_Assign@ItemA@2026-01-02
VariableB_Assign@ItemB@2026-01-01
…
```

★ **限制式裡的 `new VariableB_Assign { ... }` 不是在建變數，是在引用已經建好的那一顆**：

```csharp
// Constraint 的 Build(engine) 內
engine.AddLHS(1.0, new VariableB_Assign { Item = item, Date = date });
```

框架拿這個物件的 `ToString()` 當 key 去查已建立的變數表。所以：

| 情況                                                        | 結果                                       |
| ----------------------------------------------------------- | ------------------------------------------ |
| 這組維度值有被 `BuildVars` 建過                             | 正常對到那顆變數                           |
| 沒建過（Set 裡沒有這個成員，或整個型別忘了 `AddVariables`） | 丟 `KeyNotFoundException`                  |
| 同一個型別 `BuildVars` 呼叫兩次                             | 變數會**追加**，不是覆蓋 —— NEVER 重複呼叫 |

- MUST 先建變數才建目標式與限制式 —— 框架永遠依 **variables → objective → constraints** 執行，與 `.AddXxx` 的註冊順序無關
- MUST 每種變數在 `Program.cs` 各占一行；NEVER 在 Constraint 或 Objective 內呼叫 `BuildVars`

**三種型別 × 各種維度的完整對照**

宣告（`Variable/` 一支一檔，`namespace` / `using` 省略）：

```csharp
/// <summary>是否在該日生產該品項；對應 Assign_{Item,Date} ∈ {0,1}。</summary>
[OptVar]
[OptDim<string>("Item")]
[OptDim<DateTime>("Date")]
public sealed partial class VariableB_Assign { }

/// <summary>是否開設該廠；對應 Open_{Facility} ∈ {0,1}。</summary>
[OptVar]
[OptDim<string>("Facility")]
public sealed partial class VariableB_Open { }

/// <summary>是否由前一批接續到後一批；對應 Sequence_{LotFrom,LotTo} ∈ {0,1}（同一顆 Set 兩個角色）。</summary>
[OptVar]
[OptDim<string>("LotFrom")]
[OptDim<string>("LotTo")]
public sealed partial class VariableB_Sequence { }

/// <summary>各品項產量；對應 Produce_{Item} ≥ 0（連續）。</summary>
[OptVar]
[OptDim<string>("Item")]
public sealed partial class VariableC_Produce { }

/// <summary>所有工單的完工時間；對應 Makespan ≥ 0（連續、0 維）。</summary>
[OptVar]
public sealed partial class VariableC_Makespan { }

/// <summary>各品項在各機台的生產批數；對應 Batch_{Item,Machine} ∈ Z⁺（整數）。</summary>
[OptVar]
[OptDim<string>("Item")]
[OptDim<string>("Machine")]
public sealed partial class VariableI_Batch { }
```

建立（`Program.cs` 模型段，每種一行；傳入順序 = `[OptDim]` 順序）：

```csharp
.AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.set_Item, data.set_Date))
.AddVariables(engine => engine.BuildVars<VariableB_Open>(data.set_Facility))
.AddVariables(engine => engine.BuildVars<VariableB_Sequence>(data.set_Lot, data.set_Lot))
.AddVariables(engine => engine.BuildVars<VariableC_Produce>(data.set_Item))
.AddVariables(engine => engine.BuildVars<VariableC_Makespan>())
.AddVariables(engine => engine.BuildVars<VariableI_Batch>(data.set_Item, data.set_Machine))
```

引用已建立變數（Constraint / Objective 的 `Build(engine)` 內，填滿每個維度）：

```csharp
engine.AddLHS(1.0, new VariableB_Assign { Item = item, Date = date });
engine.AddLHS(fixedCost, new VariableB_Open { Facility = facility });
engine.AddLHS(setupCost, new VariableB_Sequence { LotFrom = from, LotTo = to });
engine.AddLHS(profit, new VariableC_Produce { Item = item });
engine.AddLHS(1.0, new VariableC_Makespan()); // 0 維：括號留空
engine.AddRHS(1.0, new VariableI_Batch { Item = item, Machine = machine });
```

| 類別                 | 型別與界限              | 維度              | 建出來的名字                        | 顆數                 |
| -------------------- | ----------------------- | ----------------- | ----------------------------------- | -------------------- |
| `VariableB_Assign`   | Binary `[0, 1]`         | Item × Date       | `VariableB_Assign@ItemA@2026-01-01` | Item 數 × Date 數    |
| `VariableB_Open`     | Binary `[0, 1]`         | Facility          | `VariableB_Open@F1`                 | Facility 數          |
| `VariableB_Sequence` | Binary `[0, 1]`         | Lot × Lot（同源） | `VariableB_Sequence@LotA@LotB`      | Lot 數的平方         |
| `VariableC_Produce`  | Continuous `[0, 1E100]` | Item              | `VariableC_Produce@ItemA`           | Item 數              |
| `VariableC_Makespan` | Continuous `[0, 1E100]` | 無                | `VariableC_Makespan`（**無 `@`**）  | 1                    |
| `VariableI_Batch`    | Integer `[0, 1E100]`    | Item × Machine    | `VariableI_Batch@ItemA@M1`          | Item 數 × Machine 數 |

**0 維的額外注意事項**

`makespan`、總成本、最大負載，以及線性化時加的輔助變數 `t`（附錄 A 的 min-of-max / abs recipe）都是 0 維：不隨任何 Set 展開，整個模型就一顆。

| 規則 | 說明                                                                                    |
| ---- | --------------------------------------------------------------------------------------- |
| 宣告 | 只掛光桿 `[OptVar]`，**不加任何 `[OptDim]`**                                            |
| 建立 | `BuildVars<T>()` 括號留空；NEVER 為了「湊一個維度」硬塞一顆只有一個成員的 Set           |
| 查解 | `GetVariableValue("VariableC_Makespan")`；`GetSetVarValues<T>()` 也能用，但回傳只有一筆 |

**同源多維的額外注意事項**（`VariableB_Sequence`）

`BuildVars<VariableB_Sequence>(data.set_Lot, data.set_Lot)` 會建出**完整平方**，含 `LotA→LotA` 這種對角線。要不要禁止自己接自己是**建模決定**，MUST 寫成一條 constraint（例：`∀ lot: Sequence_{lot,lot} = 0`），NEVER 靠「不建那幾顆」暗示——`BuildVars` 也沒有跳過特定組合的能力。

---

## §4 模型層 — Objective 與 Constraint

### 4.1 Pool API（整個套件的核心手感）

`AddLHS` / `AddRHS` 是「往池子裡丟」，可以反覆呼叫；`CreateXxx` 是「出池」，出完自動清空、接著寫下一條。

```csharp
engine.AddLHS(coef, new VariableC_Produce { Item = item }); // LHS 變數項
engine.AddLHS(constant); // LHS 常數項
engine.AddRHS(coef, new VariableB_Open { Item = item }); // RHS 變數項（框架自動移項）
engine.AddRHS(constant); // RHS 常數

engine.CreateLessEqual(this, item); // <=
engine.CreateGreatEqual(this, item); // >=
engine.CreateEqual(this, item); // ==
engine.CreateRange(lb, ub, this, item); // lb <= LHS <= ub（只吃 LHS）
```

**新模型的 paved path 是 owner overload：`CreateXxx(this, dims...)`。** 字串命名與 RHS overload 仍是公開介面；但 RHS overload 會覆蓋已累積的 RHS 常數，所以一般模型一律先用 `AddRHS(常數)`，再以 owner overload 出池，避免靜默覆蓋。

**天條：Model.md 左邊的項進 `AddLHS`、右邊的項進 `AddRHS`，`>=` 就用 `CreateGreatEqual`。NEVER 自行移項 / 改號 / 翻轉方向 / 合併化簡** —— Why: 轉譯必須能逐條對回 Model.md 驗證，動過手腳就驗不了。

限制式群組與實際建立數量由 framework 自動記錄。`ConstraintBase` 上沒有專案端計數欄位 —— NEVER 手動累加或另維護第二份計數，避免與 framework build summary 脫節。

### 4.2 一條限制式 = 一個檔

```csharp
// Constraint/Constraint_MaxDays.cs
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace MyProject
{
    /// <summary>∀ item ∈ ITEM：Σ_date Assign[item][date] ≤ MaxDays[item]</summary>
    public sealed class Constraint_MaxDays : ConstraintBase
    {
        private readonly Set_Item items;
        private readonly Set_Date dates;
        private readonly List<Parameter_MaxDays> maxDaysByItem;

        public Constraint_MaxDays(
            Set_Item items,
            Set_Date dates,
            List<Parameter_MaxDays> maxDaysByItem)
        {
            this.items = items;
            this.dates = dates;
            this.maxDaysByItem = maxDaysByItem;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
            {
                foreach (var date in dates)
                    engine.AddLHS(1.0, new VariableB_Assign { Item = item, Date = date });

                var maxDays = maxDaysByItem.FindParameterOrLog(p => p.Item == item, item)?.QTY ?? 0.0;
                engine.AddRHS(maxDays);

                engine.CreateLessEqual(this, item);
            }
        }
    }
}
```

- 繼承 `ConstraintBase` 取得 `ConstraintName`（= 類別名）
- class 開頭 `<summary>` MUST 貼上 Model.md 對應的數學式 —— Why: 驗收時逐條對照靠這行
- 限制式命名 MUST `ConstraintName@index1@index2`；`DateTime` 索引用 `{date:yyyy_MM_dd}`；複合索引用底線（`@{block}_{digit}`）
- 建構子 MUST 逐項列出實際依賴，型別用**框架實際型別**（`Set_Item`、`List<Parameter_MaxDays>`、scalar `double`）
  NEVER 用 `IReadOnlyList<int>` 這種退化型別 —— Why: 型別即文件，`IReadOnlyList<int>` 看不出是哪顆 Set，傳錯了編譯器不會擋
  NEVER 接收 `Dataload`，也 NEVER 另造 `ModelContext` 之類的整包物件繞過這條規則
- **`OptEngine` 只從 `Build(OptEngine engine)` 進來，NEVER 進建構子**
- 依賴新增或刪除時，建構子簽名與 `Program.cs` 的 call site MUST 同步改

### 4.3 目標式

```csharp
// Objective/ObjectiveFunction.cs
using OptimFoundation.Cplex;

namespace MyProject
{
    /// <summary>min Σ_item ShortagePenalty × Shortage[item]</summary>
    public sealed class ObjectiveFunction
    {
        private readonly Set_Item items;
        private readonly double shortagePenalty;

        public ObjectiveFunction(Set_Item items, double shortagePenalty)
        {
            this.items = items;
            this.shortagePenalty = shortagePenalty;
        }

        public void Build(OptEngine engine)
        {
            foreach (var item in items)
                engine.AddLHS(shortagePenalty, new VariableC_Shortage { Item = item });

            engine.CreateMinimize(); // 或 CreateMaximize()
        }
    }
}
```

**目標式 MUST 是 Model.md `OBJ` 段的逐項機械轉譯。**

- NEVER 自創目標式、NEVER 自行加減項、NEVER 改權重、NEVER 改 min/max 方向
- Model.md 沒有 OBJ 段（純可行性問題）→ **停止 Phase 2，退回 Model Design 補目標式** —— NEVER 自行發明零係數目標式、NEVER 省略 `.AddObjective(...)` —— Why: 天條規定 Coding 是純機械轉譯；Model.md 缺什麼就回去補什麼，不在 code 這一層代替使用者做建模決定
- Objective MUST 先於所有 constraints 建立（框架依 variables → objective → constraints 階段順序套用）

### 4.4 係數與常數的來源（天條）

**判準只有一條：看 Model.md 那一格寫的是什麼。**

| Model.md 寫的                                                   | Code 寫的                                              |
| --------------------------------------------------------------- | ------------------------------------------------------ |
| 具名符號（`MachineCapacity`、`ShortagePenalty`、`UsageRate_g`） | 從 `Parameter` 取值後注入                              |
| 字面數字（`= 1`、`− 1`、`≥ 0`）                                 | **直接寫那個數字**                                     |
| 字面數字，但它其實是題目資料（`≤ 40` 的產能）                   | **停止 Phase 2，退回 Model Design** 讓它變成具名 PARAM |

「這個數字該不該具名」是 **Phase 1 的決定**，§0.0 進場檢查的「CONSTRAINT / OBJ 內無裸數字」已經擋在前面。Phase 2 是逐字轉譯，NEVER 在 code 這一層自行加工——既不把具名符號寫死成數字，也不把式子本身的常數包裝成假 Parameter。

✅ Good —— 具名符號走 Parameter

```csharp
engine.AddLHS(1.0, new VariableB_Assign { Item = item, Date = date }); // Σ x 的 identity 係數
var profit = profitByItem.FindParameterOrLog(p => p.Item == item, item)?.QTY ?? 0.0;
engine.AddLHS(profit, new VariableC_Produce { Item = item }); // 係數來自 Parameter
engine.AddRHS(capacityValue); // 常數也來自 Parameter
```

✅ Good —— 式子本身的常數直接寫

```csharp
// Model: Σ_s Select_s = 1（恰好選一）
foreach (var s in options) engine.AddLHS(1.0, new VariableB_Select { Option = s });
engine.AddRHS(1.0);
engine.CreateEqual(this);

// Model: Product_{z1,z2} ≥ Z1 + Z2 − 1（二元乘積線性化，附錄 A）
engine.AddLHS(1.0, new VariableB_Product { … });
engine.AddRHS(1.0, new VariableB_Z1 { … });
engine.AddRHS(1.0, new VariableB_Z2 { … });
engine.AddRHS(-1.0); // ← pattern 自帶的常數，Model.md 就是這樣寫
engine.CreateGreatEqual(this);

// Model: Sequence_{lot,lot} = 0（禁止自己接自己）
engine.AddLHS(1.0, new VariableB_Sequence { LotFrom = lot, LotTo = lot });
engine.AddRHS(0.0);
engine.CreateEqual(this, lot);
```

❌ Bad

```csharp
engine.AddRHS(40.0); // Model.md 寫的是 MachineCapacity，卻在 code 反具名化成數字
engine.AddLHS(8.0, new VariableC_Produce { Item = item }); // 同上，8.0 是 UsageRate 的值
engine.AddLHS(profitByItem.First(p => p.Item == item).QTY, new VariableC_Produce { }); // LINQ 內嵌
```

Why: 具名符號寫死成數字，「改一個係數」就變成全專案搜尋，也對不回 Model.md 的符號表；LINQ 內嵌則是中間值 debug 不出來、`First` 沒資料直接炸。

**兩個容易搞混的邊界**

- **`1 − z` 這類互補式**：展開成 `M − M·z` 後，`M` 走 Parameter、展開產生的正負號直接寫。式子形狀來自 pattern，不是資料。
- **維度／結構常數**（3×3 宮的 3、時間窗長度 7、班別數）：這些**隨題目規模改變**，MUST 做成 `Set_*` / `Parameter_*`（§2.3），NEVER 當成「式子自帶的常數」直接寫。
  分辨法：**換一批資料它會不會變？** 會變 → 資料，要具名；不會變（改了就是換一個 pattern、換一題） → 直接寫。

### 4.5 Phase 2 禁止 soft constraint

正式模型的 `Constraint_*.cs` MUST 只使用 hard constraint API：`CreateLessEqual` / `CreateGreatEqual` / `CreateEqual` / `CreateRange`。

- NEVER 在 Phase 2 或 canonical 組裝 pipeline 使用 `CreateLeSoft` / `CreateGeSoft` / `CreateEqSoft`
- NEVER 因為模型 infeasible、需求不確定或想讓 solver「比較容易有解」就自行改成 soft；這會改變數學模型，不是實作細節
- Soft constraint 是**建模決定**：使用者明確要求放鬆某條限制式 → 回 Phase 1 寫進 Model.md（含 penalty 的具名 PARAM 與被放鬆的條目），確認後由 Phase 2 照常轉譯。Phase 3 只調 solver 旋鈕，NEVER 由它引入 soft
- soft 版模型不得覆蓋、取代或混入 canonical hard model；它是 Model.md 上的另一個版本，報告時 MUST 標出被放鬆的限制式、penalty 與違反量

換句話說：**要不要 soft 是 Model.md 說了算；寫 code 時照抄，調參時不碰。**

---

## §5 Program.cs — 唯一組裝點 + 三態 CLI

所有層級關係直接寫在這一個檔，平坦分成三段：**材料 → 模型 → 環境**。每種變數、目標式與每條限制式各占一個 fluent call。

```csharp
using OptimFoundation.Core;
using OptimFoundation.Cplex;

namespace MyProject
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            // 模式 1：import —— 攤平或生成，產出標準 CSV（CSV 還不存在時才需要）
            if (args.Length >= 2 && args[0] == "import")
            {
                string rawFile = args[1];
                OptData.Load(() => new Dataload(rawFile)).Export();
                return 0;
            }

            // exp 的 log 檔名 MUST 在第一次寫入前設定，整次執行才收在同一包
            bool isExperiment = args.Any(arg => string.Equals(arg, "exp", StringComparison.OrdinalIgnoreCase));
            if (isExperiment)
                Logging.SetLogFileName("MyProject_exp");

            // ── 1. 材料 ────────────────────────────────────────────
            var data = OptData.Load(() => new Dataload());
            double shortagePenalty = data.parameter_ShortagePenalty.Single().QTY;

            var projectConfig = new ProjectConfig
            {
                ProjectName = "MyProject",
                EnableSolverLog = true,
                ExportLP = true,
                ExportSol = true,
                DataId = "MyProject",
                UserId = "SYSTEM",
            };
            // 唯一 production baseline/champion；experiment clone 它，prod 直接使用它。
            var productionBaseline = new CplexConfig
            {
                MipGap = 0.03,
                TimeLimit = 300,
                Threads = 8,
            };

            // ── 2. 模型 ────────────────────────────────────────────
            var model = new OptModel("Canonical")
                .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.set_Item, data.set_Date))
                .AddVariables(engine => engine.BuildVars<VariableC_Shortage>(data.set_Item))
                .AddObjective(engine => new ObjectiveFunction(data.set_Item, shortagePenalty).Build(engine))
                .AddConstraints(engine => new Constraint_MaxDays(data.set_Item, data.set_Date, data.parameter_MaxDays).Build(engine))
                .AddConstraints(engine => new Constraint_Coverage(data.set_Item, data.set_Date, data.parameter_Demand).Build(engine));

            // ── 3. 環境 ────────────────────────────────────────────
            // 模式 2：exp —— 掃 solver 設定，不做正式求解
            if (isExperiment)
            {
                var baseline = productionBaseline.Clone();
                var emphasis = baseline.Clone();
                emphasis.Emphasis = 2;

                var result = new OptExperiment("MyProject-tuning-r1", "baseline vs emphasis")
                    .AddModel(model)
                    .AddConfig("r1-baseline", baseline)
                    .AddConfig("r1-emphasis=optimal", emphasis)
                    .Run();

                foreach (var trial in result.Trials)
                    Logging.Info($"[Experiment] {trial.Label} status={trial.Metrics.Status} runTimeMs={trial.Metrics.RunTimeMs:F0}");
                return 0;
            }

            // 模式 3（預設）：正式求解
            using var project = new OptProject(model)
                .UseConfig(() => projectConfig)
                .UseConfig(() => productionBaseline)
                .OnSolved(engine => MyProjectSolution.ReadAndValidate(engine, data).Print());

            bool solved = project.Execute();
            return solved ? 0 : 1;
        }
    }
}
```

**三段的內容邊界（MUST 照這個分，NEVER 互相跨界）**

三段都在 `Main` 內、都平坦寫，順序固定，前面只有三態分派：

| 段         | 只能放                                                                                                                                              | NEVER 放                                                                                     |
| ---------- | --------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| **1 材料** | `OptData.Load(() => new Dataload())` **一次**；scalar 用 `.Single().QTY` 取出；具名的 `ProjectConfig`；具名的 `productionBaseline`（`CplexConfig`） | 任何運算、篩選、排序、轉換（那是 Model.md 或 import 階段的事）；`new OptEngine(...)`；建變數 |
| **2 模型** | 一條 `new OptModel("Canonical")` fluent chain：每種變數一行 `.AddVariables`、目標式一行 `.AddObjective`、每條限制式一行 `.AddConstraints`           | 讀資料；建 config；`AddLHS` / `AddRHS`（數學式留在各 Constraint / Objective 類別內）         |
| **3 環境** | `OptProject`（production）或 `OptExperiment`（exp），掛 `.UseConfig` / `.OnSolved`，執行並回傳 exit code                                            | 改模型；改資料；再 `OptData.Load` 第二次                                                     |

- MUST 材料段的 `OptData.Load` **整個程式只呼叫一次**，production 與 experiment 共用同一份 `data` —— Why: 兩份資料會在中途漂移，實驗結果就不能拿來回答 production 的問題
- MUST 每個 config 都在材料段具名宣告，內容直接看得到 —— NEVER 用 factory helper 把設定藏起來
- MUST scalar 在材料段取成區域變數再逐項傳進第 2 段 —— NEVER 在 fluent chain 裡才 `.Single().QTY`
- 材料段之前只准有三態分派與 `Logging.SetLogFileName`（exp 模式限定，且 MUST 在 `OptData.Load` 之前）

**兩層 config 怎麼填**

職責切乾淨：`ProjectConfig` = 這個專案**怎麼輸出**，`CplexConfig` = solver **怎麼解**。實驗快照只擷取 solver 那層，所以輸出開關不會混進 tuning 記錄。

| `ProjectConfig` 欄位      | 規範                                                                                                                                               |
| ------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ProjectName`             | **MUST 明設**，值 = `<Project>`。它是 log / LP / MPS / SOL / IIS 的檔名前綴；不設會退回用模型名，多個專案的輸出就分不開                            |
| `EnableSolverLog`         | 預設 `true`。開發與驗收期保持開啟；只有輸出太吵時才關，關掉只影響 Console，framework log 照寫                                                      |
| `ExportLP`                | 建議開。`Infeasible` / 數值可疑時要靠 `.lp` 肉眼查式子（§10）                                                                                      |
| `ExportMPS` / `ExportSol` | 預設關，有交換檔或存解的需求才開                                                                                                                   |
| `DataId` / `UserId`       | 專案 metadata。★ **框架不會自動帶進 `CsvCtrl.WriteSolution`**——那支方法的兩個字串要自己傳，MUST 與這裡的值一致，否則同一次執行的解檔會標成不同來源 |
| `RetentionDays`           | 不設 = 30 天。輸出檔保留策略，一般照預設                                                                                                           |

| `CplexConfig`                 | 規範                                                                                                      |
| ----------------------------- | --------------------------------------------------------------------------------------------------------- |
| 顆數                          | 整個 `Program.cs` **只有一顆具名 `productionBaseline`**；experiment 的 variant 一律從它 `Clone()`（§8.2） |
| `TimeLimit`                   | MUST 明設，NEVER 留 `null`（無限）—— Why: 沒有停點的 production 執行沒辦法納入流程，也無從比較            |
| `MipGap`                       | 依專案可接受的品質明設；預設 `1e-4` 對多數排程題偏嚴，會白花時間收最後那點 gap                            |
| `Threads`                 | 預設 32 通常過大，MUST 依實機核心數設定並實測                                                             |
| `Seed` / `ParallelMode` | 要做 tuning 比較就 MUST 固定（`ParallelMode = 1` + 固定 seed），否則兩次執行不可比                        |
| `ITunableConfig` 與 CPLEX 設定 | `CplexConfig` 仍實作 `ITunableConfig`，但公開設定一律是同一套 PascalCase property；不再有 camelCase 對應欄位 |
| 其餘旋鈕                      | 一律留 `null` 用 CPLEX 預設 —— NEVER 一開始就塞滿參數，那會讓 Phase 3 分不出是哪個旋鈕造成差異            |

### 5.1 CLI 命令、`args` 與優先順序

`dotnet run` 後面的 `--` 是分隔符：`--` 前面屬於 `dotnet`，後面才會原樣傳入 `Main(string[] args)`；`--` 本身不會出現在 `args`。

三種正式支援的命令如下，一次只選一種：

| 模式       | 命令                                                    | `args`                | 結果                                                                                    |
| ---------- | ------------------------------------------------------- | --------------------- | --------------------------------------------------------------------------------------- |
| production | `dotnet run --project <project.csproj>`                 | `[]`                  | 載入 canonical CSV，正式求解並執行 `OnSolved`                                           |
| experiment | `dotnet run --project <project.csproj> -- exp`          | `["exp"]`             | 建立同一份 canonical model，執行 `OptExperiment`，不再執行 production runner            |
| import     | `dotnet run --project <project.csproj> -- import <raw>` | `["import", "<raw>"]` | 讀不規則來源或生成規格、`Export()` canonical CSV，完成後立即 `return 0`，不建模也不求解 |

來源路徑含空白時 MUST 加引號，例如 `-- import "raw/My Puzzle.csv"`，它仍是單一 `args[1]`。

判斷順序是程式行為的一部分：

1. 先檢查 `args.Length >= 2 && args[0] == "import"`；命中後執行 import 並立即結束。
2. 未命中 import 才掃描所有參數是否含不分大小寫的 `exp`。
3. 前兩者都未命中才進 production。

因此非標準組合的實際結果如下；這些寫法不屬正式 CLI contract，NEVER 用它們串接工作流程：

| 寫法                  | 實際結果          | 原因／正確替代                                                                         |
| --------------------- | ----------------- | -------------------------------------------------------------------------------------- |
| `-- import <raw> exp` | **只 import**     | import 分支先命中且 `return 0`；若要接著實驗，分兩次執行 `-- import <raw>` 與 `-- exp` |
| `-- exp import <raw>` | **只 experiment** | `args[0]` 不是 `import`，但掃描到 `exp`；其餘參數不會成為 import 輸入                  |
| `-- EXP` / `-- Exp`   | experiment        | `exp` 使用 `OrdinalIgnoreCase` 比較                                                    |
| `-- Import <raw>`     | production        | `import` 判斷目前區分大小寫；正式寫法固定使用小寫 `import`                             |
| `-- import`           | production        | 少了 `args[1]`，不符合 import 條件；正式寫法 MUST 提供來源                             |
| `-- <unknown>`        | production        | 未命中 import 或 exp；正式工作流程不要傳未定義參數                                     |

三態沒有「import 後自動 solve／experiment」的複合模式。需要連續操作時 MUST 分成兩次命令，讓第一步產出的 canonical CSV 成為第二步明確可檢視的輸入：

```powershell
dotnet run --project <project.csproj> -- import <raw>
dotnet run --project <project.csproj> -- exp
```

| 規則                                            | 說明                                                                                                                                                                        |
| ----------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 三態互斥                                        | `import <raw>` / `exp` / 無參數，一次只做一件事                                                                                                                             |
| exit code                                       | 求解成功 0、失敗 1；import 完成 0                                                                                                                                           |
| `Logging.SetLogFileName`                        | 只在 exp 模式呼叫，且 MUST 在 `OptData.Load` 之前 —— Why: `OptExperiment` 自己不換 log 檔名，晚設會留下只有載入摘要的殘檔。格式固定 `{name}_{時間戳}.txt`，時間戳只能在最後 |
| 每個組成各占一行                                | 每種變數一行 `.AddVariables(...)`；Objective 一行 `.AddObjective(...)`；每條 Constraint 一行 `.AddConstraints(...)`                                                         |
| 順序固定                                        | variables → objective → constraints                                                                                                                                         |
| 新增一條限制式                                  | `Constraint/` 加一檔 + pipeline 加一行，沒有第三個地方要改                                                                                                                  |
| scalar 取值                                     | 材料段用 `.Single().QTY` 取出，再逐項傳入                                                                                                                                   |
| `OptModel` 名稱                                 | canonical 模型一律 `"Canonical"`；Phase 3 的 variant 另取名                                                                                                                 |
| 兩層 config 具名宣告                            | `ProjectConfig` 管專案身分與輸出、`CplexConfig` 只管 solver 旋鈕 —— NEVER 用 config factory helper 把內容藏起來                                                             |
| `OptProject`                                    | production runner，套兩層 config、執行一次；`OnSolved` 只掛在這裡                                                                                                           |
| `OptExperiment`                                 | experiment runner，展開 model × config，自動儲存                                                                                                                            |
| NEVER 在 fluent call 直接寫 `AddLHS` / `AddRHS` | pipeline 只列組成；數學式留在各 Objective / Constraint class                                                                                                                |
| NEVER 把 `Dataload` 傳進 Objective / Constraint | pipeline 逐項傳它實際使用的 Set / Parameter / scalar                                                                                                                        |
| NEVER 包裝組裝流程                              | 禁止只負責轉呼叫的類別／函式／local function；review 必須能從這一條 chain 讀完模型組成                                                                                      |
| 不重印 framework log                            | Status / objective / IIS 路徑與 conflict names 由 `OptEngine` 自動記錄；專案端只印業務語意訊息                                                                              |

`productionBaseline` 是 tuning promotion 的唯一寫回點。AI 跑完實驗後若 champion 通過 §8 promotion gate，更新這顆 initializer 並重跑 production；NEVER 另建一份只給 prod 的 config，否則下一輪 experiment 會從過期 baseline 出發。

`ProjectConfig.EnableSolverLog` 預設 `true`，三個 export 預設 `false`；`DataId` / `UserId` 是 solution metadata 的預設值。`CplexConfig` 仍實作 `ITunableConfig`，但 solver 設定與抽象旋鈕共用同一套 PascalCase property，例如 `Seed`、`Emphasis`、`FeasibilityTol` 與 `RootAlgorithm`。

---

## §6 Solution — 取解、驗證、輸出

`Solution/<Project>Solution.cs` 是 MUST 的固定檔，掛在 `OnSolved`。**解驗證用一般 C# 程式做，NEVER 只憑 solver status 宣稱成功。**

解讀層是唯一允許接收整包 `Dataload` 的地方——`ValidateRules` 本來就要對照所有原始資料逐條驗證。

```csharp
// Solution/MyProjectSolution.cs
using OptimFoundation.Core;
using OptimFoundation.Core.IO;
using OptimFoundation.Cplex;

namespace MyProject
{
    /// <summary>讀回解、以題目規則驗證、列印並輸出 CSV。</summary>
    public sealed class MyProjectSolution
    {
        private readonly Dictionary<string, double> assign;

        private MyProjectSolution(Dictionary<string, double> assign) => this.assign = assign;

        public static MyProjectSolution ReadAndValidate(OptEngine engine, Dataload data)
        {
            Logging.Info($"Status={engine.Status} Obj={engine.GetObjectiveValue():F4} " +
                         $"BestBound={engine.LastMetrics.BestBound:F4} MIPGap={engine.LastMetrics.MipGap:P2}");

            var assign = engine.GetSetVarValues<VariableB_Assign>();
            ValidateRules(assign, data);

            // `CsvCtrl.WriteSolution` 會自行建立 Solution/；不需手動建資料夾
            CsvCtrl.WriteSolution<VariableB_Assign>(engine, "MyProject", "SYSTEM");
            return new MyProjectSolution(assign);
        }

        /// <summary>逐條把解代回 Model.md 的限制式；不成立就丟例外，NEVER 只記 log 繼續。</summary>
        private static void ValidateRules(Dictionary<string, double> assign, Dataload data)
        {
            foreach (var item in data.set_Item)
            {
                var maxDays = data.parameter_MaxDays.FindParameterOrLog(p => p.Item == item, item)?.QTY ?? 0.0;
                double used = data.set_Date.Sum(date =>
                    assign.TryGetValue($"VariableB_Assign@{item}@{date:yyyy-MM-dd}", out var value) ? value : 0.0);

                if (used > maxDays + 1e-6)
                    throw new InvalidOperationException($"{item} 違反 MaxDays：{used} > {maxDays}。");
            }
        }

        public void Print()
        {
            foreach (var kv in assign.Where(kv => kv.Value > 0.5))
                Console.WriteLine(kv.Key);
        }
    }
}
```

| 方法                                                      | 回傳                                           |
| --------------------------------------------------------- | ---------------------------------------------- |
| `GetObjectiveValue()`                                     | `double`                                       |
| `GetVariableValue(fullName)`                              | `double`；`fullName` 含 `@`                    |
| `GetSetVarValues<T>()`                                    | `Dictionary<string, double>`，key = 完整變數名 |
| `GetSolution(typeName = null)`                            | `IReadOnlyDictionary`；`null` = 全部變數       |
| `GetBVSolution()` / `GetCVSolution()` / `GetIVSolution()` | 依型別分類的全部解                             |
| `GetSetVarNames<T>()` / `GetAllVarNames()`                | `string[]`                                     |

**手寫 key 的規則（這一節是全專案唯一要自己拼變數名的地方）**

建模層從不碰字串——`AddLHS(coef, new VariableB_Assign { … })` 傳的是物件，框架自己呼叫 `ToString()`。只有這裡查解時要自己拼：

```csharp
assign.TryGetValue($"VariableB_Assign@{item}@{date:yyyy-MM-dd}", out var value);
```

格式是 `ClassName@dim1@dim2`，由框架寫死（沒有任何設定可調），拼錯不會報錯：

| 規則                                         | 說明                                                                                                                              |
| -------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------- |
| 分隔符**一律 `@`**                           | NEVER `\|`。`\|` 在這套框架另有用途——Trial label 是 `"Canonical \| r1-baseline"`、log 行是 `時間 \| INFO \| [caller] msg`——別抓錯 |
| 維度順序 = `[OptDim]` 宣告順序               | 與 `BuildVars` 傳入 sets 的順序同一份真相                                                                                         |
| `DateTime` 維度**用 `yyyy-MM-dd`**（連字號） | ★ 與**限制式名**的 `{date:yyyy_MM_dd}`（底線）不同。變數名的格式是 `ToString()` 寫死的，限制式名是你自己取的，兩者不通用          |

Why 這裡特別要小心：`TryGetValue` 拼錯只會回 `false`，接著 `?? 0.0` 把它當成「這個變數是 0」，`ValidateRules` 於是**整段靜默通過**，你以為驗過了。驗證邏輯寫完後 MUST 故意改一個值確認它真的會 throw。

輸出資料夾（都在 `bin/Debug/net8.0/` 底下）：`Data`（輸入）、`Solution`、`Logs`、`Models`（LP/MPS）、`Sols`、`IISs`、`Experiments`。
**`new ProjFolder(...)` 不會自動建資料夾**。標準 `CsvCtrl.WriteSolution` 已自行建立 `Solution/`；只有直接操作低階檔案輸出時才由呼叫端明確建立目標資料夾。

---

## §7 驗收 — 解驗證協定

`dotnet run` 有輸出 ≠ 會動。MUST 依序四步，任一步不過就停下回報，NEVER 宣稱完成：

1. **SolveStatus 七態分流**
   - `Optimal`／`Feasible` → 有 incumbent，進第 2 步；`Feasible` 仍須記錄 `LastMetrics.MipGap` 與 `LastMetrics.BestBound`
   - `TimeLimit` → 沒有任何可用解，不能做解驗證；改用小 instance 求到 `Optimal` 再完成驗收
   - `Infeasible` → **MUST 去讀 `bin/Debug/net8.0/IISs/*.ilp`**（或 `GetConflictConstraints()`）拿最小衝突集；先自查 Big-M 是否太小、有無互斥硬約束 —— NEVER 靠猜、NEVER 改成 soft constraint 繞過
   - `Unbounded` → 某個方向漏了界；查該變數缺哪條上限 constraint
   - `Error`／`NotSolved` → 不得進入解驗證；先修正執行或 lifecycle 問題
2. **可行性代回**：在 `Solution/<Project>Solution.cs` 的 `ValidateRules` 逐條把解值代回**每一條** constraint，確認 `LHS op RHS` 成立 —— Why: solver 回 Optimal 只保證「它解的那個模型」可行，不保證那個模型 = 你的題目
3. **單位一致性**：目標值與關鍵變數的單位跟題目一致（利潤 = 錢、產量 = 件、工時 = 時），量級合理
4. **LP bound sanity**：max 問題整數解 ≤ LP relaxation bound；min 問題 ≥。差太離譜 → 疑 Big-M 或係數錯

✅ Good：四步都過，且目標值對得上 Model.md 的手算小例
❌ Bad：看到 `Status == Optimal` 就回報「解出來了」

build 失敗走 fix loop：擷取 compiler error → 修 → 重 build，**至多 5 次**；仍失敗停下回報，NEVER 硬掰。

---

## §8 寫實驗 — 參數掃描

### 8.0 進場條件與範圍界線

**MUST 先過 §7 正確性 gate 才做 tuning。** 對錯的模型調參數只會更快得到錯答案。資料驗證（`OptData.Load` 自動跑）→ §7 四步解驗證 → 才輪到本節。

**Phase 3 只動一樣東西：`CplexConfig`。** 進場時模型與資料已凍結、已 feasible；`Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 全程唯讀。落在下表哪一格決定要不要留在本節：

| 使用者要的                                       | 是不是 tuning | 動作                                                                          |
| ------------------------------------------------ | ------------- | ----------------------------------------------------------------------------- |
| 模型正確且有解，但 timeout / gap 收不下來 / 太慢 | ✅ 是         | 調 `CplexConfig`（§8.2 閉環）；連續 3 輪無實質改善就停止回報                  |
| 換參數值、換一批資料                             | ❌ 否         | 換 `Data/*.csv` 重跑即可，`.cs` 一行都不動，不需要本節                        |
| 加刪約束、改 Big-M、reformulation                | ❌ 否         | **回 Model.md 改，經確認後重走 §1–§7 轉譯**                                   |
| `Infeasible` / `Unbounded`                       | ❌ 否         | 前提破裂：`Infeasible` 讀 IIS 取證後退回；`Unbounded` 補漏掉的界限 constraint |

- Why: tuning 的全部價值建立在「模型與資料固定」上——動了其中任何一項，before / after 就不可比，該輪實驗證據整批作廢。
- NEVER 用 soft constraint / penalty 讓 infeasible 模型「有解」—— 旋鈕不會把 infeasible 變 feasible，只會讓你更快確認它 infeasible。
- NEVER 以 tuning 名義移項 / 改號 / 翻方向 / 四捨五入；只改求解策略，不改數學意義。
- NEVER 沒有 `OptExperiment` 記錄就宣稱改善。

**常用 solver 決策**（欄位對照見附錄 B）：

| 症狀         | 動作                                                                                                                  |
| ------------ | --------------------------------------------------------------------------------------------------------------------- |
| timeout      | 提 `TimeLimit`、試 `Emphasis = 1`（重可行解）、實測篩 `Threads`                                                |
| gap 大       | 加切割（`GomoryCuts` / `MirCuts` / `CoverCuts`）→ `NodeSelect = 1`（best-bound）→ 收 `MipGap` / `Emphasis = 2`      |
| 可行解難找   | `NodeSelect = 0`（DFS）→ `IntegerSolutionLimit` / `NodeLimit` → 放寬 `MipGap` → `Emphasis = 1`（僅限模型本身有解、只是難找） |
| 記憶體爆     | `TreeMemoryLimitMb` + `NodeFileStrategy = 2/3`（**順序有雷**，見附錄 B 註記）                                                |
| 數值不穩     | `NumericalEmphasis = true`                                                                                            |
| 要可重現     | `ParallelMode = 1` + 固定 `Seed` + 用 `DeterministicTimeLimit` 取代牆鐘 `TimeLimit`                                       |
| `Infeasible` | **不是 tuning 題目**：讀 IIS（`bin/.../IISs/*.ilp`）取最小衝突集當證據，退回 Phase 1 / Phase 2 修模型或資料           |

### 8.1 實驗 runner

`OptExperiment` 將已定義的模型與具體 solver configs 展開成笛卡兒積，擷取 Trial，最後輸出 `Experiments/<name>.csv + .json`。同一份 `OptModel` 可先交給 `OptProject` 正式求解，再交給實驗 runner。

**同名實驗是 append，不是覆寫。** `Run()` 內的 `Save()` 會先讀既有 JSON，再把歷史 trials 合併回傳值的 `result.Trials`。因此 experiment 命名 MUST 用 `<Project>-tuning-r<N>`，每輪 N 加一 —— NEVER 重複用同一個名字 —— Why: 重跑同名 experiment 後直接 `foreach (result.Trials)` 會再次看到歷史資料，把舊 trial 誤報成本輪結果。

```csharp
var baseline = solverConfig.Clone();
var emphasis = baseline.Clone();
emphasis.Emphasis = 2;
var tighterGap = baseline.Clone();
tighterGap.MipGap = 0.01;
var threads4 = baseline.Clone();
threads4.Threads = 4;

var result = new OptExperiment("MyProject-tuning-r1", "一次只改一個 solver 旋鈕")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=optimal", emphasis)
    .AddConfig("r1-gap=0.01", tighterGap)
    .AddConfig("r1-threads=4", threads4)
    .Run();
```

| 規則                | 說明                                                                                |
| ------------------- | ----------------------------------------------------------------------------------- |
| 共用一份 data       | 所有 cell 引用同一份 `OptData.Load` 結果，載入後視為唯讀                            |
| experiment 預設安靜 | solver log、LP/MPS/Sol export 與 housekeeping 預設都 OFF                            |
| 具體 config         | 用 `Clone()` 建 variant，NEVER 用 tune delegate 突變共用 baseline                   |
| 笛卡兒積            | `.AddModel` × `.AddConfig` 自動展開；單一 cell 用 `.AddTrial(model, label, config)` |
| label               | 自動成為 `ModelName \| config-label`；輪次前綴（`r1-`）寫進 config label            |
| 命名                | experiment name 一律 `<Project>-tuning-r<N>`，每輪遞增                              |
| baseline 可重現     | 固定 `Threads` / `ParallelMode` / `Seed`，讓每次跑的停點一致              |
| `OnSolved` 邊界     | 只屬 `OptProject`；實驗不應大量寫 solution                                          |
| 一次只動一個旋鈕    | 同時改兩個就分不出是哪個造成差異                                                    |

### 8.2 AI 閉環：Trial → champion → production baseline

`OptExperiment` 本身只負責執行與保存證據，不會改傳入的 config，也不會替 production 選 winner。完成 tuning 的責任在 AI workflow：

```text
productionBaseline
  └─ Clone variants
       └─ OptExperiment.Run()
            └─ Trial.Config + Trial.Metrics
                 └─ AI promotion gate
                      └─ 更新 productionBaseline
                           └─ build + production + ValidateRules
```

每輪 MUST：

1. 使用新實驗名 `<Project>-tuning-r<N>`，只分析本輪 Trial；同名 append 的歷史結果不得混進本輪排名。
2. 先淘汰錯誤、Infeasible、Unbounded、無可行解或未達 objective / gap 品質門檻的 candidate。
3. 在相同 instance、seed 與資源限制下比較；正式 promotion 用 3–5 seeds 與 hold-out instances。第一個 solve 作 warm-up 並排除，variants 跨 seed 輪替或隨機化執行順序，避免固定讓 baseline 承擔 cold-start。runtime 用 shifted geometric mean，timeout 用 PAR10，改善幅度須大於 baseline variability。
4. 依序比較解的正確性與品質，再比較 runtime；node / iteration 只作診斷或 tie-break。NEVER 讓一個比較快但解較差的 trial 勝出。
5. 從 champion 的 `Trial.Config.Settings` 取得完整設定快照，由 AI 明確更新 `Program.cs` 的 `productionBaseline` initializer。同步更新 initializer 上方 provenance，至少寫來源 experiment + Trial label。NEVER 讓執行中的程式修改 source，NEVER 讓 prod 每次動態挑歷史上最快的一列。
6. 在專案根 `TuningHistory.md` 新增本輪紀錄：experiment、baseline / champion Trial、seeds / instances、彙總方法、Status / objective / gap、before/after config diff、決策與理由。該檔 MUST 納入 source control；`bin/Experiments/*.json` 會被清除，不能作唯一 provenance。
7. promotion 後 MUST `dotnet build` 並跑無參數 production；`OnSolved` / `ValidateRules`、Status、objective、gap 與輸出全數通過才完成升級，並把結果補回同一筆 history。失敗則保留原 baseline並記錄 rejected。
8. 成功 promotion 的 champion 成為下一輪 baseline；沒有可靠勝者也記錄 retain。連續 3 輪無實質改善就停止 tuning 並回報，是否回頭改模型結構由使用者決定。

若沒有 candidate 可靠勝過 baseline，正確結果是保留既有 production baseline，而不是硬挑本輪牆鐘時間最小者。完成報告與 `TuningHistory.md` MUST 寫明 baseline、champion（若有）、before / after、seeds / instances、promotion 或不 promotion 的理由。

**評分採 lexicographic gate**，NEVER 把不同品質的解只用 runtime 混排：

1. 正確且可接受的 Status / 解品質（錯誤、Infeasible、Unbounded、無可行解一律淘汰）。
2. 達成專案要求的 objective 與 MipGap。
3. 前兩者相同或都達標時，才比較穩健彙總後的 runtime；node / iteration 只作診斷與 tie-break。

若需要覆寫 experiment 的 project-level defaults，可使用 `.UseConfig(() => projectConfig)`；factory 每個 cell 都會執行。一般 solver 掃描保留預設即可。

Soft constraint 不屬 solver tuning。Phase 3 的模型是凍結的——使用者要放鬆限制式就是回 Phase 1 改 Model.md（§4.5），NEVER 在調參途中把 canonical hard model 換成 soft 版來「讓它有解」。

### 8.3 `TuningHistory.md`（專案根，MUST 納入 source control）

`bin/Experiments/*.json` 會隨 clean 消失，不能當唯一 provenance。每輪一節，至少包含：

```text
日期 / round / experiment name
baseline Trial ｜ champion 或 candidate Trial
instances / seeds / 彙總方法（shifted geometric mean、PAR10）
Status / objective / gap
before → after config diff
決策：promotion ｜ retain ｜ rejected + 理由
production 驗證：dotnet build 結果、ValidateRules 結果
```

這是決策索引；完整逐 Trial 數值仍由 experiment JSON 保存。即使本輪沒有勝者也要記 retain 與證據，避免下一輪重做同一組實驗。

---

## §9 API 速查卡與框架簽名

### 9.1 這條流程用得到的呼叫

```csharp
// 材料（Program.cs §1 段）
var data = OptData.Load(() => new Dataload());
double penalty = data.parameter_ShortagePenalty.Single().QTY;

var projectConfig = new ProjectConfig
{
    ProjectName = "MyProject",
    EnableSolverLog = true,
    ExportLP = true,
};
var solverConfig = new CplexConfig { MipGap = 0.0, TimeLimit = 60, Threads = 4 };

// 模型（Program.cs §2 段）
var model = new OptModel("Canonical")
    .AddVariables(engine => engine.BuildVars<VariableB_Assign>(data.set_Item, data.set_Date))
    .AddVariables(engine => engine.BuildVars<VariableC_Shortage>(data.set_Item))
    .AddObjective(engine => new ObjectiveFunction(data.set_Item, penalty).Build(engine))
    .AddConstraints(engine => new Constraint_Capacity(data.set_Item, data.parameter_Capacity).Build(engine));

// 環境（Program.cs §3 段）
using var project = new OptProject(model)
    .UseConfig(() => projectConfig)
    .UseConfig(() => solverConfig)
    .OnSolved(engine => MyProjectSolution.ReadAndValidate(engine, data).Print());
bool ok = project.Execute();

// Objective / Constraint 的 Build(engine) 內
engine.AddLHS(coef, new VariableC_Produce { Item = item });
engine.AddLHS(constant);
engine.AddRHS(coef, new VariableB_Open { Item = item });
engine.AddRHS(constant);
engine.CreateLessEqual(this, item);
engine.CreateGreatEqual(this, item);
engine.CreateEqual(this, item);
engine.CreateRange(lb, ub, this, item);
engine.CreateLeSoft(rhs, penalty, this, item); // 只在 Model.md 明寫 soft 時
engine.CreateGeSoft(rhs, penalty, this, item); // 同上
engine.CreateEqSoft(rhs, penalty, this, item); // 同上

// 目標式
engine.AddLHS(coef, new VariableC_Shortage { Item = item });
engine.CreateMinimize(); // 或 CreateMaximize()

// 取解（Solution/<Project>Solution.cs）
double obj = engine.GetObjectiveValue();
var dict = engine.GetSetVarValues<VariableB_Assign>();
var metrics = project.Engine.LastMetrics; // gap / bound / node·iter / 軌跡

// 資料 I/O（Data/Dataload.cs）
set_Item = source.Load<Set_Item>("Set_Item");
parameter_Demand = source.Load<Parameter_Demand>("Parameter_Demand");
CsvCtrl.WriteRows(set_Item, "Set_Item"); // 只在 import 模式
CsvCtrl.WriteRows(parameter_Demand, "Parameter_Demand"); // 只在 import 模式
// Solution/ 由 CsvCtrl.WriteSolution 自動建立
CsvCtrl.WriteSolution<VariableB_Assign>(engine, "MyProject", "SYSTEM");

// 日誌：OptProject 會自動設定檔名；framework lifecycle 不在專案端重複印
Logging.Info("業務語意或解驗證訊息");
```

### 9.2 框架完整簽名（有疑慮查這裡，NEVER 憑記憶發明方法名）

以下是本流程會碰到的全部框架 public 介面。標 ❌ 者為框架有提供但**本規範禁用**，列出來是為了辨識，不是給你用的。

**組件與 using**

```text
OptimFoundation.Core（不相依 CPLEX）
├── DesignBases.cs ModelElementBase / VariableBase / ParameterBase / ConstraintBase / SetRowBase
├── DataContext.cs DataContext（註冊 + 驗證 + 框架受控凍結）、OptData、Numeric
├── DataValidator.cs 四類資料檢查與 DataIssue
├── EngineBase.cs EngineBase<TModel,TVar,TExpr,TConstr> : ISolverEngine
├── VariableBuilder.cs 變數名稱展開（Set → 字串 → 笛卡兒積）
├── Enums.cs / ISolverEngine.cs / Config/ProjectConfig.cs
├── Experiment.cs Experiment / ITrajectorySource；Experiments/ Trial、ConfigSnapshot、SolveMetrics、writers
├── IO/ CsvCtrl、IDataSource、CsvDataSource、InMemoryDataSource、DbDataSource、ISolutionSink
├── Infrastructure/FolderDir.cs
└── Logging/Logging.cs

OptimFoundation.Cplex（相依 Core + ILOG.Concert + ILOG.CPLEX）
└── CplexConfig / OptEngine / OptModel / OptProject / OptExperiment
```

```csharp
using OptimFoundation.Core; // ProjectConfig, DataContext, OptData, Numeric, CsvCtrl, FolderDir, Logging, enums
using OptimFoundation.Core.IO; // IDataSource, CsvDataSource, DbDataSource, ISolutionSink
using OptimFoundation.Modeling; // generator attributes：OptSet / OptParam / OptVar / OptDim
using OptimFoundation.Cplex; // OptEngine, OptModel, OptProject, OptExperiment, CplexConfig
```

**9.2.1 元素基底與變數名稱規則**

```csharp
public abstract class ModelElementBase
{
    public void InitClassBySets(params object[] sets); // 按屬性順序賦值，型別自動 Convert.ChangeType
    public override string ToString(); // ClassName@p1@p2@...
}
public abstract class VariableBase : ModelElementBase { protected string VariableName => ElemName; }
public abstract class ParameterBase : ModelElementBase { protected string ParameterName => ElemName; }
public abstract class ConstraintBase : ModelElementBase
{
    protected string ConstraintName => GetType().Name; // = 類別名，限制式命名前綴
}
```

- 變數 / 參數的 key = `ClassName@{p1}@{p2}@…`，分隔符 `@`；`DateTime` 一律格式化為 `yyyy-MM-dd`
- 屬性順序 = `[OptDim]` 宣告順序 = 傳入 sets 的順序，三者 MUST 一致（接錯不報錯）
- 例：`new VariableB_Assign { Employee = "E1", Date = new DateTime(2026,1,1) }` → `"VariableB_Assign@E1@2026-01-01"`
- ★ **`@` 是保留字元，資料值不得含它**：Set 成員或維度值出現 `@`，框架在建變數 / 賦值當下丟 `ArgumentException`（訊息指出是哪個 Set 或哪個 property）—— Why: 它是 key 的維度分隔符，混進資料會讓兩顆不同變數組出同一個 key。CSV 裡的品項代號、員工編號含 `@` 時 MUST 在 import 階段換掉
- `InitClassBySets` 型別轉不過丟 `InvalidCastException`（訊息含第幾個參數與期望型別）；屬性數 ≠ 參數數丟 `ArgumentException`

**9.2.2 generator attributes（唯一 paved path）**

```csharp
[OptSet] // 裸寫；標在 partial class Set_*；MUST 至少一個 [OptDim]。多維即 tuple Set
[OptParam] // 產 ParameterBase 並自動補 QTY；可零維（scalar）；無值 tuple 改用 [OptSet]
[OptVar] // 產 VariableBase；型別由類名前綴 VariableB_/C_/I_ 決定
[OptDim<T>("Name")] // 每維一個，可重複；T 是該維度的資料型別
                    // T ∈ string / DateTime / int / long / double / decimal —— NEVER 是 Set 積木
                    // "Name" = property 名 = CSV 欄名 = Set↔Parameter 的連結鍵
// framework 不提供完整矩陣 attribute；完整矩陣需求由專案資料驗收明確檢查
```

診斷碼（compile error，訊息會教正確寫法）：

| 碼        | 意義                                                                      |
| --------- | ------------------------------------------------------------------------- |
| `OPTF001` | `[OptVar]` 類名前綴不是 `VariableB_` / `VariableC_` / `VariableI_`        |
| `OPTF002` | `[OptParam]` 類名前綴不是 `Parameter_`                                    |
| `OPTF004` | `[OptDim<T>]` 的 `T` 不是合法元素型別                                     |
| `OPTF006` | 被 `Dataload : DataContext` 引用的 `Set_*` / `Parameter_*` 漏掛 attribute |

★ `[OptDim<T>]` 的泛型參數不受型別約束，填錯不會有 compile error——generator 直接拿它當 property 型別。`T` MUST 是資料型別本身（見 §1.6）。

❌ 禁用：generator 產的位置式建構子（見 §1.5）。

**9.2.3 `SetRowBase` 與 row collection**

```csharp
// generator 對 [OptSet] 產生的 row 基底
public abstract class SetRowBase : ModelElementBase { }

// 專案端唯一保存、讀取與傳遞方式
List<Set_Item> set_Item = source.Load<Set_Item>("items.csv");
List<Set_Arc> set_Arc = source.Load<Set_Arc>("arcs.csv");
```

Set 不再是集合積木物件；每個 `Set_*` 是一列資料，載入後永遠是 `List<Set_*>`。一維 row 可隱式轉為它的維度型別；多維 row 使用 property 或 deconstruct。`BuildVars` 接受這些 row list，並依 row 的所有 `[OptDim]` 展開 key。

```csharp
foreach (string item in set_Item) { }
foreach (var (from, to) in set_Arc) { }
```

資料驗證檢查 Set 維度 key 重複；集合是否為空、是否涵蓋題目 domain，屬專案資料語意與解驗證責任。

**9.2.4 資料層：`IDataSource` / `DataContext` / `OptData` / 驗證**

> ⚠️ **本節、§2.4 的資料驗證與 §9.2.3 的 row collection 寫法以現行 DLL 核實為準**。資料層是框架目前活躍改動的區域，所以每次依 `dlls/README.md` 回填 `dlls/` 後，MUST 重驗本節簽名與範例。

```csharp
public interface IDataSource
{
    DataTable LoadData(string sourceName); // 獨立 raw table
    List<T> Load<T>(string sourceName = null) where T : ModelElementBase, new();
}
public sealed class CsvDataSource : IDataSource { } // 讀 AppDomain.BaseDirectory/Data 下的 CSV
public sealed class InMemoryDataSource : IDataSource { } // 測試用，可用 AddRows 註冊 row list
public sealed class DbDataSource : IDataSource
{
    public List<T> Load<T>(string sql, params (string name, object value)[] parameters)
        where T : ModelElementBase, new(); // SQL 的欄位以 AS 對齊 property
}

public static class OptData
{
    public static T Load<T>(Func<T> factory) where T : DataContext; // 唯一多載
}

// OptData.Load 會驗證 Set key 重複、Parameter key 重複與 Parameter 數值 sanity。
// Parameter 對 Set 的關聯與完整笛卡兒積不是 framework schema 驗證的一部分。

public static class Numeric
{
    public static double SafeRatio(double numerator, double denominator,
        double magnitudeCeiling = 1e9, string context = null);
}
```

- `OptData.Load` 一次做完：new → generator 註冊 → 四類驗證 → 凍結框架受控 mutation API
- 現行驗證只有三項：Set 維度 key 重複、Parameter 維度 key 重複、Parameter 數值 sanity（`NaN` / `±Infinity` / 絕對值 > `1e15`）。Parameter 對 Set 的關聯與完整矩陣需求由專案資料驗收處理。
- `SafeRatio` 的量級門檻預設 `1e9`（衍生值如 Big-M，較嚴）與驗證器的 `1e15`（原始資料，較寬）**刻意不同，NEVER 統一**
- 凍結只保護框架受控入口；直接寫 public field 或 `List.Add` 攔不到，因此專案 code MUST 把載入後的資料視為唯讀

**9.2.5 `EngineBase` — 建變數、Pool、限制式、目標式**

```csharp
public virtual void BuildVars<T>(params object[] sets); // 預設入口，型別由類名前綴決定
public virtual void BuildBVs<T>(params object[] sets); // 有效的明確 binary builder
public virtual void BuildCVs<T>(params object[] sets); // 有效的明確 continuous builder
public virtual void BuildCVs<T>(double lb, double ub, params object[] sets); // 有效的 bounds overload
public virtual void BuildIVs<T>(params object[] sets); // 有效的明確 integer builder
public virtual void BuildIVs<T>(double lb, double ub, params object[] sets); // 有效的 bounds overload

public bool AddLHS(double coeff, object varSpec); // LHS 變數項
public bool AddLHS(double constant); // LHS 常數項
public bool AddRHS(double coeff, object varSpec); // RHS 變數項，框架自動移項
public bool AddRHS(double constant); // RHS 常數項
public bool HasPool { get; }
public void ClearPool();

public bool CreateLessEqual(ConstraintBase owner, params object[] dims); // ✅ <=
public bool CreateGreatEqual(ConstraintBase owner, params object[] dims); // ✅ >=
public bool CreateEqual(ConstraintBase owner, params object[] dims); // ✅ ==
public bool CreateRange(double lb, double ub, ConstraintBase owner, params object[] dims); // ✅ lb <= LHS <= ub
public bool CreateLessEqual(double rhs, string name); // 有效；會覆蓋既有 RHS 常數
public bool CreateGreatEqual(double rhs, string name); // 有效；會覆蓋既有 RHS 常數
public bool CreateEqual(double rhs, string name); // 有效；會覆蓋既有 RHS 常數

public void CreateMinimize(); // 把 pool 的 LHS 設為目標式
public void CreateMaximize();

public virtual bool CreateLeSoft(double rhs, double penalty, ConstraintBase owner, params object[] dims); // Model.md 明寫 soft 時使用
public virtual bool CreateGeSoft(double rhs, double penalty, ConstraintBase owner, params object[] dims); // 同上
public virtual bool CreateEqSoft(double rhs, double penalty, ConstraintBase owner, params object[] dims); // 同上
public virtual bool SupportsSoftConstraints { get; }

public int VariableCount { get; }
public int RegisteredVariableCount { get; }
public string[] GetAllVarNames();
public string[] GetSetVarNames<T>();
public void VarSetsReset();
```

- `varSpec` 傳物件初始化器建的變數實例（`new VariableC_Produce { Item = item }`），框架取它的 `ToString()`
- `BuildVars` 接受 row list 或其他 `IEnumerable`；Set row 的每個 `[OptDim]` 依序展開為 key token。
- 出池公式：`(Σ lhsTerms − Σ rhsTerms) sense (rhsConst − lhsConst)`
- `CreateXxx` 會自動 `ClearPool()`，下一條不必手動清
- soft 版的機制（Model.md 有寫才會用到）：加彈性變數 + 建限制式 + 在目標式加 penalty。`CreateLeSoft` → `Surplus_{name}`；`CreateGeSoft` → `Deficit_{name}`；`CreateEqSoft` → `Delta_Neg_{name}` / `Delta_Pos_{name}`。違反量可用 `GetVariableValue("Deficit_…")` 取得

**9.2.6 `OptEngine` 與求解**

```csharp
public OptEngine(CplexConfig config);
public OptEngine(CplexConfig config, ProjectConfig projectConfig);
public OptEngine();

public void Build(); // 非 virtual template method；要自訂改覆寫 protected BuildCore()
public bool Solve(); // 非 virtual；要自訂改覆寫 protected SolveCore()
public void Dispose();
public void SetModelName(string name); // LP/MPS/Sol/IIS 檔名前綴；OptProject 會自動代入

public SolveStatus Status { get; }
public SolveMetrics LastMetrics { get; } // 未求解為 null
// Best bound 與 MIP gap 讀取 engine.LastMetrics.BestBound / engine.LastMetrics.MipGap。

public override double GetObjectiveValue();
public override double GetVariableValue(string name); // name 為含 @ 的完整變數名
public Dictionary<string, double> GetSetVarValues<T>();
public IReadOnlyDictionary<string, double> GetSolution(string varTypeName = null); // null = 全部
public IReadOnlyDictionary<string, double> GetBVSolution();
public IReadOnlyDictionary<string, double> GetCVSolution();
public IReadOnlyDictionary<string, double> GetIVSolution();
public List<string> GetConflictConstraints(); // Infeasible 時的衝突集
```

`Solve()` 的行為序列：

1. `PreSolveGuard()`：`RegisteredVariableCount` 超過 `ScaleWarnThreshold`（預設 `10,000,000`）→ `Logging.Warn` 一則，**只警告不中止**
2. `ExportLP` / `ExportMPS` 為 true → 寫 `Models/{ModelName}_LP_{timestamp}.lp` / `.mps`
3. 求解，設定 `Status`
4. 解可行且 `ExportSol` → 寫 `Sols/{ModelName}_Solution_{timestamp}.sol`
5. `Infeasible` → 自動 `RefineConflict`，寫 `IISs/{ModelName}_IIS_{timestamp}.ilp`
6. 印 `ObjVal / LastMetrics.BestBound / LastMetrics.MipGap`，回填 `LastMetrics`

```csharp
public enum VarType { Continuous, Integer, Binary }
public enum ConstraintSense { LessEqual, Equal, GreaterEqual }
public enum ObjectiveSense { Minimize, Maximize }
public enum SolveStatus { NotSolved, Optimal, Feasible, Infeasible, Unbounded, TimeLimit, Error }
```

`Solve()` 回傳 `true` 的條件：`Status == Optimal || Status == Feasible`。CPLEX 的 `InfeasibleOrUnbounded` 會映成 `Infeasible`；其餘未知狀態映成 `Error`。

**9.2.7 兩層設定**

```csharp
public sealed class ProjectConfig // OptimFoundation.Core，PascalCase property
{
    public string ProjectName { get; set; } // 未指定時 OptProject 用模型名
    public int? RetentionDays { get; set; } // 未指定時視為 30
    public bool EnableSolverLog { get; set; } = true;
    public bool ExportLP { get; set; } = false;
    public bool ExportMPS { get; set; } = false;
    public bool ExportSol { get; set; } = false;
    public string DataId { get; set; }
    public string UserId { get; set; }
    public ProjectConfig Clone();
}

public sealed class CplexConfig : ISolverConfig, ITunableConfig
{
    public CplexConfig Clone(); // 建立 tuning variant 的唯一方式

    // 唯一設定面：全部使用 PascalCase public property，null = CPLEX 預設。
    public int? Threads { get; set; }
    public int? RowRead { get; set; }
    public double? MemoryLimitMb { get; set; }
    public double? MipGap { get; set; }
    public double? AbsoluteMipGap { get; set; }
    public double? OptimalityTol { get; set; }
    public double? FeasibilityTol { get; set; }
    public double? IntegralityTolerance { get; set; }
    public double? TimeLimit { get; set; }
    public double? DeterministicTimeLimit { get; set; }
    public double? PolishAfterTime { get; set; }
    public int? Emphasis { get; set; }
    public int? NodeSelect { get; set; }
    public int? VariableSelect { get; set; }
    public int? RootAlgorithm { get; set; }
    public int? NodeFileStrategy { get; set; }
    public int? Seed { get; set; }
    public int? ParallelMode { get; set; }
    public long? NodeLimit { get; set; }
    public long? IntegerSolutionLimit { get; set; }
    public bool? NumericalEmphasis { get; set; }
    public int? NodeAlgorithm { get; set; }
    public bool? PreIndicator { get; set; }
    public int? Presolve { get; set; } // `PreIndicator` 的 ITunableConfig int 視角
    public double? HeuristicEffort { get; set; }
}
```

`EnableSolverLog = false` 只關 Console 的 CPLEX progress，framework log 照寫。`DataId` / `UserId` 是專案 metadata，`CsvCtrl.WriteSolution` 不會自動讀，呼叫端仍要明確傳。

**9.2.8 `OptModel` / `OptProject` / `OptExperiment`**

```csharp
public sealed class OptModel
{
    public string Name { get; }
    public OptModel(string name = "Model");
    public OptModel AddVariables(Action<OptEngine> build);
    public OptModel AddObjective(Action<OptEngine> build);
    public OptModel AddConstraints(Action<OptEngine> build);
}

public sealed class OptProject : IDisposable
{
    public OptProject(OptModel model, string projectName = null, int retentionDays = 30);
    public OptProject UseConfig(Func<ProjectConfig> configFactory);
    public OptProject UseConfig(Func<CplexConfig> configFactory);
    public OptProject OnSolved(Action<OptEngine> handler);
    public bool Execute();
    public OptEngine Engine { get; }
    public bool IsSuccess { get; }
    public TimeSpan TotalElapsed { get; } // Execute() 結束後填入
    public TimeSpan BuildModelElapsed { get; } // 建模階段耗時
}

public sealed class OptExperiment
{
    public OptExperiment(string name, string description);
    public OptExperiment UseConfig(Func<ProjectConfig> configFactory);
    public OptExperiment AddModel(OptModel model);
    public OptExperiment AddConfig(string label, CplexConfig config);
    public OptExperiment AddTrial(OptModel model, string label, CplexConfig config);
    public Experiment Run(); // 先 Save() 再回傳
}

public sealed class Trial
{
    public string Label { get; set; } // "{model.Name} | {configLabel}"
    public DateTime RunAt { get; set; } // 與 Label 合起來是 Save() 的去重鍵
    public ConfigSnapshot Config { get; set; } // 唯一 PascalCase 設定快照，可重現設定
    public SolveMetrics Metrics { get; set; } // 評分依據，欄位見下
    public string Note { get; set; }
    // 自寫 runner 才需要；走 OptExperiment 時框架自動呼叫
    public static Trial Capture(ISolverEngine engine, string label, Func<bool> solveAction, string note = null);
}
public sealed class ConfigSnapshot
{
    public string Solver { get; set; }
    public Dictionary<string, object> Settings { get; set; } // 唯一 PascalCase CplexConfig 設定快照
}
public sealed class SolveMetrics // = engine.LastMetrics
{
    public SolveStatus Status { get; set; }
    public double ObjectiveValue { get; set; }
    public double BestBound { get; set; }
    public double MipGap { get; set; }
    public double RunTimeMs { get; set; }
    public long? NodeCount { get; set; }
    public long? IterationCount { get; set; }
    public int VarCount { get; set; }
}
public class Experiment
{
    public Experiment(string name, string description);
    public List<Trial> Trials { get; set; }
    public void AddTrial(Trial trial); // 自寫 runner 用
    public void Save(); // 跨 run append，依 RunAt + Label 去重
    public static Experiment Load(string name);
}
```

- 建模階段永遠依 **variables → objective → constraints** 執行，與註冊順序無關；同階段可註冊多次並保留該階段內順序
- `OnSolved` **只存在於 `OptProject`**；`OptExperiment` 沒有
- `OptProject` MUST dispose（用 `using var`）；`OptExperiment` 自行 dispose 每個 cell 的 engine
- 兩個 `UseConfig` 可交換順序；同型別重複設定時最後一個 factory 生效
- `OptExperiment` 未呼叫 `UseConfig` 時：solver log OFF、LP/MPS/SOL 全不匯出、不做 housekeeping；solver config 在 cell 開始時先 `Clone()`
- Trial label 重複（cross × cross、cross × explicit、explicit × explicit）→ `Run()` 在建任何 engine 前丟 `InvalidOperationException`；重複的 `AddConfig` label 在加入當下丟 `ArgumentException`
- 輸出：CSV 一列一 Trial（給人 / Excel）、JSON 巢狀含 `config` / `metrics` / `convergence[]`（給 LLM）

**9.2.9 I/O：`CsvCtrl` / `ISolutionSink` / `FolderDir` / `Logging`**

```csharp
public static class CsvCtrl
{
    public static void WriteRows<T>(IReadOnlyList<T> rows, string fileName = null)
        where T : ModelElementBase;
    public static void WriteSolution<T>(ISolverEngine engine, string dataId, string userId);
}

public interface ISolutionSink
{
    void WriteSolution<TVariableClass>(ISolverEngine engine, string dataId = null, string userId = null);
    ISolutionBatch BeginBatch(string dataId = null, string userId = null);
}
public interface ISolutionBatch : IDisposable
{
    void Write<TVariableClass>(ISolverEngine engine);
    void Commit(); // 未 Commit 就 Dispose = rollback
}

public class FolderDir // 路徑根 = AppDomain.BaseDirectory，即 bin/Debug/net8.0/
{
    public static ProjFolder Data; // 輸入 CSV
    public static ProjFolder Solution; // 解 CSV
    public static ProjFolder Log; // Logs
    public static ProjFolder Model; // LP / MPS
    public static ProjFolder IIS; // .ilp
    public static ProjFolder Sol; // CPLEX .sol
    public static ProjFolder Experiment; // Experiments CSV / JSON
    public class ProjFolder
    {
        public void CreateFolder(); // 低階自訂寫檔可呼叫；CsvCtrl.WriteSolution 會自行建立 Solution/
        public string GetPath();
        public string GetFilePath(string fileName);
        public bool TryCreateFile(string fileName);
    }
}

public static class Logging
{
    public static void Info(string message);
    public static void Debug(string message);
    public static void Warn(string message);
    public static void Error(string message);
    public static void Info(string message, Stopwatch sw); // 印完自動 sw.Restart()
    public static void SetLogFileName(string name); // 之後寫到 Logs/{name}_{timestamp}.txt
    public static void ClearLogs();
}
```

`CsvSolutionSink` 是 `ISolutionSink` 的標準 CSV 實作；單筆輸出與批次輸出都會寫入 `Solution/`。CSV batch 的每次 `Write` 都立即落檔，`Commit()` 是為了與可交易的 DB sink 保持相同介面而保留的 no-op。

```csharp
ISolutionSink sink = new CsvSolutionSink();
sink.WriteSolution<VariableB_Assign>(engine, "MyProject", "SYSTEM");

using var batch = sink.BeginBatch("MyProject", "SYSTEM");
batch.Write<VariableB_Assign>(engine);
batch.Write<VariableC_Shortage>(engine);
batch.Commit();
```

| 操作                              | 是否需要呼叫端先建資料夾？              |
| --------------------------------- | -------------------------------------- |
| `CsvCtrl.WriteSolution`           | ❌ 自行建立 `Solution/`                |
| `Logging.*`                       | ❌ 內部自建                            |
| `Solve()` 寫 LP / MPS / Sol / IIS | ❌ 框架已處理                          |

log 格式：`2026-05-26 23:33:51.4433 | INFO | [Namespace.Of.Caller] message`。標準流程由 `OptProject.Execute()` 依 `ProjectConfig.ProjectName` 自動設定 log 檔名；只有 §5 的 exp 模式與自管低階 `OptEngine` 才需要自己 `SetLogFileName`。

**9.2.10 內部機制與 protected API（本流程用不到，讀舊 code 或除錯時查）**

照本規範寫的專案 **NEVER 直接呼叫本節任何東西**。列出來是為了兩件事：看得懂變數名怎麼被組出來，以及碰到「明明資料對卻報錯」時知道是哪一層在擋。

**`VariableBuilder`（`BuildVars` 內部就是它）**

```csharp
public static class VariableBuilder
{
    public static IEnumerable<string> GetVarNames<TVariable>(object[] sets); // 主入口，直接組字串不建實例
    public static IEnumerable<string> GenVarCombinations(params List<string>[] lists); // 笛卡兒積
    public static List<string>[] ConvertSetsToStringLists(params object[] lists); // Set → 字串，規則見下
    public static void BuildVars<TVariable>(Action<object> createVarMethod, object[] sets);
}
```

`ConvertSetsToStringLists` 的轉換規則（決定變數名長什麼樣，也決定你在 `Solution` 手拼 key 時要寫什麼）：

| Set 元素型別         | 轉字串方式                                 |
| -------------------- | ------------------------------------------ |
| `string`             | 原樣                                       |
| `DateTime`           | `ToString("yyyy-MM-dd")`                   |
| `int` / `long`       | `ToString()`                               |
| `double` / `decimal` | `ToString(CultureInfo.InvariantCulture)`   |
| enum                 | `ToString()`（列舉名，不是數值）           |
| 其他                 | 丟 `ArgumentException`，訊息列出支援的型別 |

- 單獨傳一個 `string[]` 時 C# 陣列共變會把它誤攤平成多個 set，框架會自動還原成單一 set，不必自己包一層
- 傳入裸 `string`（不是集合）丟 `ArgumentException`，訊息點名「集合與裸 string 混傳」
- 笛卡兒積展開**之前**先逐成員跑 `@` 保留字元檢查（§9.2.1），所以驗的是 N 個成員而不是展開後的 N^k 個變數名

**`EngineBase` / `OptEngine` 的 protected 成員（只有繼承 engine 才碰得到）**

```csharp
// EngineBase：載入後調界限；本規範一律改用 constraint 表達界限，NEVER 走這裡
protected void SetVarLB(object searchData, double lb);
protected void SetVarUB(object searchData, double ub);
protected void SetVarRange(object searchData, double lb, double ub);

// OptEngine：直接對 ILOG 物件操作的低階入口，Pool API 底下就是它們
protected INumVar CreateVar(string name, double lb = 0, double ub = double.MaxValue, VarType type = VarType.Continuous);
protected ILinearNumExpr Expr(IEnumerable<(double coef, INumVar var)> terms);
protected IRange AddLE(string name, ILinearNumExpr lhs, double rhs);
protected IRange AddGE(string name, ILinearNumExpr lhs, double rhs);
protected IRange AddEQ(string name, ILinearNumExpr lhs, double rhs);
protected void Minimize(ILinearNumExpr expr);
protected void Maximize(ILinearNumExpr expr);

// 要自訂 engine 行為就覆寫這兩個——Build() / Solve() 已非 virtual
protected override void BuildCore();
protected override bool SolveCore();
```

**收斂軌跡（experiment 的 `convergence[]` 來源）**

```csharp
public interface ITrajectorySource
{
    bool SupportsTrajectory { get; } // CPLEX 支援；EngineBase 預設不支援
    void EnableTrajectory(); // MUST 在 Solve() 之前呼叫；不支援的 engine 為 no-op
    IReadOnlyList<ConvergencePoint> Trajectory { get; } // 未開啟或不支援時為空清單
}
```

### 9.3 黑名單 — 這些寫了必爛，或已被本規範禁止

| ❌ 禁用                                       | ✅ 正確替代                                        | 原因                                                                              |
| --------------------------------------------- | -------------------------------------------------- | --------------------------------------------------------------------------------- |
| `engine.GetVarSol(name)`                      | `engine.GetVariableValue(name)`                    | 不存在                                                                            |
| `engine.GetSetVarSol<T>()`                    | `engine.GetSetVarValues<T>()`                      | 不存在                                                                            |
| `CSVCtrl.SaveToCSV<T>(...)`                   | `CsvCtrl.WriteSolution<T>(engine, dataId, userId)` | 不存在                                                                            |
| `CSVCtrl.xxx`（大寫 V）                       | `CsvCtrl.xxx`                                      | 大小寫錯                                                                          |
| `FolderDir.Result`                            | `FolderDir.Solution`                               | 不存在                                                                            |
| `new OptEngineConfig { ... }`                 | `new CplexConfig { ... }`                          | 不存在                                                                            |
| `engine.AddPool` / `AddPoolRHS`               | `engine.AddLHS` / `AddRHS`                         | 不存在                                                                            |
| `BuildBVs(typeof(T), sets)`                   | `BuildBVs<T>(sets)`                                | 非泛型型別引數寫法不存在                                                          |
| 一般 B/C/I variable 建立                      | `BuildVars<T>(sets)`                               | 預設入口由前綴判定型別                                                            |
| 需要明確型別或自訂 bounds                      | `BuildBVs<T>` / `BuildCVs<T>` / `BuildIVs<T>`       | 公開且有效；前綴與 builder 必須一致                                                |
| `CreateEqual(rhs, name)` 等 RHS overload      | `AddRHS(rhs)` + `CreateEqual(this, dims)`           | RHS overload 仍有效但覆蓋既有 RHS 常數；一般模型採 owner overload                |
| `ConstraintCount++` / 自印計數                | 不寫，`EngineBase` 自動統計                        | `ConstraintBase` 上沒有這個欄位，第二份計數也會對不上                             |
| 手寫 `: VariableBase` / `: ParameterBase`     | `[OptVar]` / `[OptParam]` + `[OptDim]`             | 已廢止，property 順序無單一真相                                                   |
| `new Parameter_X(a, b)` 位置式                | `new Parameter_X { A = a, B = b }`                 | 改 `[OptDim]` 順序會靜默接錯                                                      |
| `source.Load<T>()` 省略來源名稱               | `source.Load<Set_X>("sourceName")`                 | 來源名與類別名脫鉤時應明確指定                                                   |
| `namespace MyProject.Data` 子 namespace       | `namespace MyProject`                              | 全專案單一 namespace                                                              |
| file-scoped `namespace X;`                    | block `namespace X { }`                            |                                                                                   |
| `Model/` 放 `.cs`                             | 只放 `.md`；組裝碼在 `Program.cs`                  |                                                                                   |
| 假設變數名分隔符是 `\|`                       | 是 `@`                                             |                                                                                   |
| 假設 `ProjFolder` 建構子會建資料夾            | `CsvCtrl.WriteSolution` 會自行建立 `Solution/`    | 自訂低階輸出才自行管理資料夾                                                      |
| `override Build()` / `override Solve()`       | 已非 virtual；改覆寫 `BuildCore()` / `SolveCore()` |                                                                                   |
| `ProjectReference` 指向框架 src               | `..\..\dlls\` HintPath                             | 只允許 `$SIB/OptimFoundation/Templates/*` 內部 template；AI-Modeling 專案一律禁用 |

---

## §10 常見錯誤與反模式

| 症狀                                         | 真正原因                                                                               | 修法                                                                    |
| -------------------------------------------- | -------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| `CS0246` 找不到型別                          | csproj `HintPath` 層數錯                                                               | `Projects/` 底下一律 `..\..\dlls\`                                      |
| generator 沒產碼                             | 類別漏了 `partial`，或 `Set_*` 漏掛 `[OptSet]`                                         | 補上；漏掛且被 Dataload 引用會報 `OPTF006`                              |
| 載入摘要顯示 `Sets（0）` / `Parameters（0）` | `Dataload` 漏了 `partial` 或 `: DataContext` —— generator 掃不到它，**無任何錯誤訊息** | 補齊兩者；這是唯一的偵測訊號，見 §2.4                                   |
| `OPTF001`                                    | 變數前綴不是 `VariableB_/C_/I_`                                                        | 依前綴表改名                                                            |
| 找不到 CSV                                   | csproj 漏 `Data\**\*.csv` 的 copy 設定，或 CSV 根本沒放進 `Data/`                      | 補 copy 設定；不規則來源先跑 `dotnet run -- import raw/<file>`          |
| import 產的 CSV 過一陣子不見了               | `Export()` 只寫 bin 下的 `FolderDir.Data`，被 `dotnet clean` 或換 configuration 清掉   | 要當正式 input 就搬回專案 `Data/`                                       |
| `DirectoryNotFoundException`                 | 自訂低階輸出未建立目標資料夾                                                          | `CsvCtrl.WriteSolution` 無需處理；低階輸出明確建立目標資料夾           |
| 解出來但數值離譜                             | 變數 sets 傳入順序與 `[OptDim]` 不一致                                                 | 逐一對齊順序                                                            |
| 限制式右側值莫名消失                         | RHS overload 覆蓋掉先前 `AddRHS`                                                      | 改回 `AddRHS(...)` + `CreateEqual(this, dims)`                          |
| `Unbounded`                                  | 界限沒寫成 constraint（`BuildVars` 上界是 1E100）                                      | 補該變數的上限 `Constraint_*`                                           |
| Infeasible 但模型看起來對                    | Big-M 太小、或兩條硬約束互斥                                                           | 讀 `IISs/*.ilp`；以 `ProjectConfig.ExportLP = true` 開 `.lp` 檔用肉眼查 |
| 改一個係數要改好幾個檔                       | 係數 hardcode 在 Constraint 裡                                                         | 全部移進 Parameter 的 `QTY`                                             |
| 換資料還要改 `.cs`                           | `Dataload(IDataSource)` 裡有迴圈／判斷／補值                                           | 搬到 import 建構子，先產 CSV                                            |
| 重跑實驗看到舊資料                           | 同名 experiment append                                                                 | 改用 `<Project>-tuning-r<N>` 遞增命名                                   |

### 反模式

❌ `try { ... } catch (Exception) { throw; }` —— 什麼都沒做卻讓每個方法多兩層縮排，直接刪掉
❌ 在 Variable / Parameter 類別寫建構子 —— 框架用 reflection 組 key，自己寫只會干擾
❌ Objective / Constraint 建構子接收 `Dataload` —— 改成只注入該式子使用的 Set / Parameter / scalar（`Solution` 不在此限）
❌ 把 `OptEngine` 放進建構子 —— engine 只從 `Build(OptEngine engine)` 進來
❌ 在 Constraint class 手動 `ConstraintCount++` 或自印計數 —— `EngineBase` 已自動統計
❌ 用純轉呼叫 helper／local function 包裝模型組裝 —— 每個組成必須直接列在 `Program.cs` 的 chain
❌ 用 config factory helper 隱藏設定內容 —— 在 `Program.cs` 獨立建立具名 `ProjectConfig` 與 `CplexConfig`
❌ 在正式 `Constraint_*.cs` 使用 `CreateLeSoft` / `CreateGeSoft` / `CreateEqSoft` —— 除非 Model.md 本身就寫了 soft（那是 Phase 1 的決定，見 §4.5）
❌ 在 `Dataload(IDataSource)` 用迴圈、`Random`、enum 或日期運算製造模型輸入 —— 移到 import 階段先產 CSV
❌ 迴圈邊界寫字面數字（`for (int i = 0; i < 3; i++)`）—— 結構常數做成 Set / Parameter，用 `foreach` 或 `set.Count`
❌ 改 OptimFoundation 框架本體（`dlls/` 是唯讀的）—— 缺方法就在專案端寫 helper / extension

---

## 附錄 A · 線性化 pattern 對照（讀 Model.md 的 CONSTRAINT 段用）

Model.md 每條 constraint 都標了一個 pattern tag（§0.0）。轉譯前先用本附錄核對式子形狀：形狀對不上，多半是 Phase 1 漏了某條 linking constraint，該退回 Model Design 而不是在 code 裡補。符號一律語意命名，`M` 為 Big-M。

**A.1 八類 canonical form**

| #   | Pattern                                      | 語言線索                           | 原形                                                                                 |
| --- | -------------------------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------ |
| 1   | **UB / LB（Range）**                         | at most / no more than / at least  | $\sum_i Coef_i \cdot x_i \le CapacityUB$；$\sum_i Coef_i \cdot x_i \ge RequireLB$    |
| 2   | **Balance**                                  | input = output / must equal        | $\sum_{i \in In} Flow_i = \sum_{j \in Out} Flow_j$                                   |
| 3   | **Proportional**                             | at least X times / no more than Y% | $A \le Ratio \cdot B$（**交叉相乘**）—— ❌ NEVER $A / B \le Ratio$，變數相除即非線性 |
| 4   | **Implication**（A 開 ⇒ B 開，兩者 binary）  | if A then B / implies              | $z_A \le z_B$                                                                        |
| 5   | **Conjunction**（全部同時成立）              | only if all / must all be active   | $\sum_{s \in S} z_s = \lvert S \rvert$                                               |
| 6   | **Disjunction**（至少 k 個）                 | at least one of / a minimum of     | $\sum_{s \in S} z_s \ge k$                                                           |
| 7   | **Exclusive XOR**（恰好一個）                | exactly one / mutually exclusive   | $\sum_{s \in S} z_s = 1$                                                             |
| 8   | **Conditional Activation**（Big-M 條件啟動） | only if / can be used when         | $x \le M \cdot z$；門檻觸發版 $Input \ge Threshold - M \cdot (1 - z)$                |

**A.2 非線性 → 線性 recipe**

| 情境                                             | 作法                                                                                                                                                 |
| ------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| `abs`：$\lvert expr \rvert \le b$                | 拆兩條 $expr \le b$、$-expr \le b$（NEVER 用 `abs()`）。目標裡 $\min \lvert expr \rvert$ → 加輔助變數 $t$：$t \ge expr$、$t \ge -expr$，目標 `min t` |
| 目標裡壓一個 max                                 | 加 $t$，$t \ge e_i\ \forall i$，目標 `min t`                                                                                                         |
| 目標裡抬一個 min                                 | 加 $t$，$t \le e_i\ \forall i$，目標 `max t`（方向必須對：min-of-max 用 `t >=`，max-of-min 用 `t <=`）                                               |
| **Fixed-charge**（設置成本 / 開了才能用）        | $Produce \le M \cdot Open$ + 目標加 $FixedCost \cdot Open$。❌ 只加成本卻漏 linking，等於可白吃產能                                                  |
| **Either-Or**（兩約束至少一條成立）              | 加 binary $y$：$g_1(x) \le b_1 + M(1-y)$、$g_2(x) \le b_2 + M \cdot y$                                                                               |
| **二元 × 二元** $w = z_1 z_2$                    | $w \le z_1$、$w \le z_2$、$w \ge z_1 + z_2 - 1$、$w \in \{0,1\}$                                                                                     |
| **二元 × 連續** $w = z \cdot c$（$c \in [0,U]$） | $w \le U \cdot z$、$w \le c$、$w \ge c - U(1-z)$、$w \ge 0$                                                                                          |

**A.3 Big-M 鐵律（最容易靜默出錯）**

- ALWAYS `M` = 被約束式的**最緊合法上界**，由題目數據推導（總產能、最大需求…），且 MUST 定義成 `Parameter_*` 從 CSV 讀入
- NEVER magic number（`99999`）：M **太小** → 砍掉合法解，solver 靜默回一個錯的「最佳解」，build / solve 都不報錯，最難抓；M **太大** → LP relaxation 鬆、B&B 節點爆增
- 由資料推導時用 `Numeric.SafeRatio(分子, 分母, context: "BigM")`，NEVER 裸除法 —— 除零 / 非有限 / 超過量級門檻會當場丟例外，不讓壞的 M 溜進模型
- ✅ Good：`Parameter_BigMProduce.QTY = TotalCapacity`　❌ Bad：`engine.AddRHS(1000000)`

---

## 附錄 B · `CplexConfig` 全旋鈕對照（§8 tuning 用）

✅ = 框架已提供且已接線；❌ = 框架尚未提供（要用得改框架本體並重編 Core + Cplex DLL，屬框架維護，不在本流程內）。欄位為 `null` 即採用 CPLEX 預設，tuning 時只設要動的那一個。

| 用途                                | CPLEX 參數                                        | `CplexConfig` 欄位                  | 取值 / 預設                                                                                        |
| ----------------------------------- | ------------------------------------------------- | ----------------------------------- | -------------------------------------------------------------------------------------------------- |
| 執行緒數                            | `Param.Threads`                                   | ✅ `Threads`                    | 整數，預設 32；實測比較核心數 / -1 / -2                                                            |
| 平行模式                            | `Param.Parallel`                                  | ✅ `ParallelMode`                   | -1 機會式 / 0 自動 / 1 決定論                                                                      |
| 限制式讀取上限                      | `Param.Read.Constraints`                          | ✅ `RowRead`                        | 整數，預設 30000                                                                                   |
| 工作記憶體                          | `IntParam.WorkMem`                                | ✅ `MemoryLimitMb`                     | MB，預設 2048                                                                                      |
| 樹記憶體上限                        | `Param.MIP.Limits.TreeMemory`                     | ✅ `TreeMemoryLimitMb`                | MB                                                                                                 |
| 節點檔策略                          | `Param.MIP.Strategy.File`                         | ✅ `NodeFileStrategy`                    | 0 不存 / 1 記憶體壓縮（預設） / 2 磁碟 / 3 磁碟壓縮                                                |
| 節點選擇                            | `Param.MIP.Strategy.NodeSelect`                   | ✅ `NodeSelect`                     | 0 DFS / 1 best-bound（預設） / 2 best-estimate / 3 交替                                            |
| 分支變數選擇                        | `Param.MIP.Strategy.VariableSelect`               | ✅ `VariableSelect`                         | -1 min-infeas / 0 自動 / 1 max-infeas / 2 pseudo cost / 3 strong branching / 4 pseudo reduced cost |
| 分支方向                            | `Param.MIP.Strategy.Branch`                       | ✅ `BranchDirection`                      | -1 向下 / 0 自動 / 1 向上                                                                          |
| 潛降策略                            | `Param.MIP.Strategy.Dive`                         | ✅ `DiveType`                       | 0 自動 / 1 傳統 / 2 探測 / 3 引導                                                                  |
| 搜尋模式                            | `Param.MIP.Strategy.Search`                       | ✅ `MipSearch`                      | 0 自動 / 1 傳統 B&C / 2 動態 B&C                                                                   |
| 探測強度                            | `Param.MIP.Strategy.Probe`                        | ✅ `Probe`                          | -1..3                                                                                              |
| RINS 頻率                           | `Param.MIP.Strategy.RINSHeur`                     | ✅ `RinsHeuristicFrequency`                       | -1 關 / 0 自動 / N                                                                                 |
| 啟發式投入                          | `Param.MIP.Strategy.HeuristicEffort`              | ✅ `HeuristicEffort`                | 倍率                                                                                               |
| MIP emphasis                        | `Param.Emphasis.MIP`                              | ✅ `Emphasis`（= `Emphasis`）    | 0 平衡 / 1 重可行解 / 2 重最佳性 / 3 best bound / 4 hidden                                         |
| 數值穩定                            | `Param.Emphasis.Numerical`                        | ✅ `NumericalEmphasis`              | bool                                                                                               |
| Cut 數量倍數                        | `Param.MIP.Limits.CutsFactor`                     | ✅ `CutsFactor`                     | 倍率                                                                                               |
| Cut 回合數                          | `Param.MIP.Limits.CutPasses`                      | ✅ `CutPasses`                      | -1 / 0 / N                                                                                         |
| Gomory 切割                         | `Param.MIP.Cuts.Gomory`                           | ✅ `GomoryCuts`                     | -1 關 / 0 自動 / 1..3 漸積極                                                                       |
| 覆蓋切割                            | `Param.MIP.Cuts.Covers`                           | ✅ `CoverCuts`                      | -1 / 0 / 1..3                                                                                      |
| 團切割                              | `Param.MIP.Cuts.Cliques`                          | ✅ `CliqueCuts`                     | -1 / 0 / 1..3                                                                                      |
| MIR 切割                            | `Param.MIP.Cuts.MIRCut`                           | ✅ `MirCuts`                        | -1 / 0 / 1..3                                                                                      |
| Flow cover 切割                     | `Param.MIP.Cuts.FlowCovers`                       | ✅ `FlowCoverCuts`                  | -1 / 0 / 1..3                                                                                      |
| MIP gap（相對）                     | `Param.MIP.Tolerances.MIPGap`                     | ✅ `MipGap`（= `MipGap`）            | 預設 1e-4                                                                                          |
| MIP gap（絕對）                     | `Param.MIP.Tolerances.AbsMIPGap`                  | ✅ `AbsoluteMipGap`                         | 數值                                                                                               |
| 整數容差                            | `Param.MIP.Tolerances.Integrality`                | ✅ `IntegralityTolerance`                          | 數值                                                                                               |
| 最佳性容差                          | `Param.Simplex.Tolerances.Optimality`             | ✅ `OptimalityTol`（= `OptimalityTol`）     | 預設 1e-6                                                                                          |
| 可行性容差                          | `Param.Simplex.Tolerances.Feasibility`            | ✅ `FeasibilityTol`（= `FeasibilityTol`）    | 預設 1e-6                                                                                          |
| 時間限制（牆鐘）                    | `Param.TimeLimit`                                 | ✅ `TimeLimit`（= `TimeLimit`）     | 秒                                                                                                 |
| 決定論時間                          | `Param.DetTimeLimit`                              | ✅ `DeterministicTimeLimit`                   | ticks，可重現實驗首選                                                                              |
| 計時方式                            | `Param.ClockType`                                 | ✅ `ClockType`                      | 1 CPU / 2 wall                                                                                     |
| 節點上限                            | `Param.MIP.Limits.Nodes`                          | ✅ `NodeLimit`                      | 整數                                                                                               |
| 整數解上限                          | `Param.MIP.Limits.Solutions`                      | ✅ `IntegerSolutionLimit`                    | 找到 N 個整數解即停                                                                                |
| Solution polishing                  | `Param.MIP.PolishAfter.Time`                      | ✅ `PolishAfterTime`                | 秒                                                                                                 |
| 隨機種子                            | `Param.RandomSeed`                                | ✅ `Seed`（= `Seed`）         | 整數                                                                                               |
| 預處理                              | `Param.Preprocessing.Presolve`                    | ✅ `Presolve`                       | bool；一般保持開啟，只有 debug 才關                                                                |
| Root 演算法                         | `IntParam.RootAlgorithm`                          | ✅ `RootAlgorithm`（= `RootAlgorithm`） | 0 自動 / 1 primal / 2 dual / 3 network / 4 barrier / 5 sifting / 6 concurrent                      |
| 節點 LP 演算法                      | `IntParam.NodeAlg`                                | ✅ `NodeAlgorithm`                  | 0..6                                                                                               |
| Simplex 迭代上限                    | `Param.Simplex.Limits.Iterations`                 | ✅ `SimplexIterationLimit`               | 整數                                                                                               |
| Barrier 演算法                      | `Param.Barrier.Algorithm`                         | ✅ `BarrierAlgorithm`               | 0..3                                                                                               |
| ZeroHalf / Disjunctive 切割         | `Param.MIP.Cuts.ZeroHalfCut` / `.Disjunctive`     | ❌                                  | —                                                                                                  |
| 對稱性消除                          | `Param.Preprocessing.Symmetry`                    | ✅ `Symmetry`                       | −1 auto / 0 off / 1..5 逐步提高強度                                                                |
| 進階 presolve                       | `Preprocessing.Aggregator` / `NumPass` / `Reduce` | ❌                                  | —                                                                                                  |
| 記憶體 emphasis                     | `Param.Emphasis.MemUsage`                         | ❌                                  | —                                                                                                  |
| 分支優先級                          | `Cplex.SetPriority` / order file                  | ❌                                  | 只能間接用 `VariableSelect` 影響                                                                           |
| MIP start（初始解注入）             | `Cplex.AddMIPStart` / `SetVectors`                | ❌                                  | 等效手段：`Emphasis = 1` + `RinsHeuristicFrequency` + `HeuristicEffort`                                       |
| 自動調參                            | `Cplex.TuneParam`                                 | ❌                                  | —                                                                                                  |
| Heuristic / Lazy / UserCut callback | 對應 callback                                     | ❌                                  | —                                                                                                  |

> ★ **`MemoryLimitMb` 與 `NodeFileStrategy` 的順序雷**：框架的 `Configuration()` 在設定 `MemoryLimitMb` 時會強制 `MIP.Strategy.File = 0`。要做「記憶體爆 → 溢寫節點檔」，MUST 在設 `MemoryLimitMb` **之後**再設 `NodeFileStrategy = 2/3`，否則被覆蓋成 0。
>
> ★ **單一 PascalCase 設定面**：`config.Seed = 7`、`config.MipGap = 0.01` 等 public property 是唯一寫法；不再區分抽象旋鈕與 solver 專屬欄位，也不可混用兩套命名。
