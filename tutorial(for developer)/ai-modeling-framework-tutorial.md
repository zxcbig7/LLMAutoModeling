# OptimFoundation 新版開發教學

本教學描述 generator 與 row-data 架構的標準開發方式。

## 1. 先走三個 phase gate

1. **Modeling**：先完成 `Model/<Project>_Model.md`，並取得使用者確認。
2. **Coding**：把模型逐條轉成程式，完成資料驗證、build 與解驗證。
3. **Tuning**：只有模型與資料已凍結、且使用者要求時，才調整 solver 設定。

新專案固定使用八個資料夾：`Model/`、`Set/`、`Parameter/`、`Variable/`、`Objective/`、`Constraint/`、`Solution/`、`Data/`。`Program.cs` 是唯一的組裝點，`Data/Dataload.cs` 是資料載入點。

## 2. Set 與 Parameter：同一個 row-data 語法

兩者都由一個空的 partial class 宣告，每個維度各寫一個 `[OptDim<T>]`。`T` 只能是 C# 基礎型別：`string`、`DateTime`、`int`、`long`、`double`、`decimal`。

```csharp
using OptimFoundation.Modeling;

namespace RoutePlanning
{
    // 多維 Set：一列是一條可用弧 (From, To)
    [OptSet]
    [OptDim<string>("From")]
    [OptDim<string>("To")]
    public sealed partial class Set_Arc { }

    // 多維 Parameter：一列是一個 (From, To) 的成本 QTY
    [OptParam]
    [OptDim<string>("From")]
    [OptDim<string>("To")]
    public sealed partial class Parameter_ArcCost { }

    // 零維 Parameter：scalar，只有 QTY
    [OptParam]
    public sealed partial class Parameter_FixedCost { }
}
```

Set 與 Parameter 都是**資料列型別**，不是 collection，也沒有「Set brick」概念。載入後的型別是 `List<Set_Arc>` 或 `List<Parameter_ArcCost>`。

- Set 必須至少一個 `OptDim`；寫成零維必須視為宣告錯誤。
- Parameter 可以零維，代表 scalar parameter；它不會把 `QTY` 當成維度名稱。
- 多維的順序就是 attribute 的宣告順序，也是 CSV key 欄的邏輯順序。
- `OptDim` 不引用 `Set_*` 類別；它只描述該欄的資料型別與名稱。

## 3. CSV 形狀：忠實對應欄位

CSV 一律有表頭。讀取器依欄名轉換成適當型別，不靠欄位位置猜測資料。

| 宣告 | CSV 表頭與資料 |
| --- | --- |
| 一維 Set `Set_Row` / `OptDim<string>("Row")` | `Row`<br>`R1`<br>`R2` |
| 多維 Set `Set_Arc` | `From,To`<br>`A,B`<br>`B,C` |
| 多維 Parameter `Parameter_ArcCost` | `From,To,QTY`<br>`A,B,12.5`<br>`B,C,8` |
| 零維 Parameter `Parameter_FixedCost` | `QTY`<br>`1000` |

Set 的每列表示「此維度組合存在」；它沒有 `QTY`。Parameter 的每列表示「此維度組合的數值」，故最後固定多一欄 `QTY`。多維 Set 的 `(A,B)` 是一個 tuple 成員；若模型只允許既有弧，變數與限制式應以這份 `Set_Arc` 展開。

## 4. 一套讀取與輸出 API

`Dataload` 只把已就位的資料讀成 row list，不做補值、推導或商業規則。CSV、記憶體與資料庫皆透過 `IDataSource` 注入；呼叫端不因 Set 或 Parameter 而換 API。

```csharp
using OptimFoundation.Core;
using OptimFoundation.Core.IO;

namespace RoutePlanning
{
    public sealed partial class Dataload : DataContext
    {
        public List<Set_Arc> arcs { get; }
        public List<Parameter_ArcCost> arcCosts { get; }
        public List<Parameter_FixedCost> fixedCosts { get; }

        public Dataload() : this(new CsvDataSource()) { }

        public Dataload(IDataSource source)
        {
            arcs = source.Load<Set_Arc>();
            arcCosts = source.Load<Parameter_ArcCost>();
            fixedCosts = source.Load<Parameter_FixedCost>();
        }
    }
}
```

資料庫的 SQL、連線與型別對應應封裝在 `DbDataSource` 的設定或實作中；`Dataload` 仍只表達「要哪一種 row」，不把 SQL 字串混進模型資料結構。輸出也同樣統一：

```csharp
CsvCtrl.WriteRows(arcs);
CsvCtrl.WriteRows(arcCosts);
```

輸出忠實保留 row 的欄位：Set 輸出 Dim 欄；Parameter 輸出 Dim 欄加 `QTY`。

## 5. 變數與模型

變數同樣使用 `OptDim<T>`，其類名前綴決定型別：`VariableB_` 是 binary、`VariableX_` 是 continuous、`VariableI_` 是 integer。以 tuple Set 建變數時，將 row list 直接交給 `BuildVars<T>`：

```csharp
[OptVar]
[OptDim<string>("From")]
[OptDim<string>("To")]
public sealed partial class VariableB_UseArc { }

// Program.cs 的模型組裝段
engine.BuildVars<VariableB_UseArc>(data.arcs);
```

限制式與目標式依照數學式原貌建構：左側項加入 `AddLHS`，右側項加入 `AddRHS`，再用正確的 `CreateLessEqual`、`CreateGreatEqual` 或 `CreateEqual` 結束。不要自行移項、改號或化簡。模型數值一律取自 `Parameter_*.QTY`。

## 6. 實作完成前檢查

- 每個 Set / Parameter 一個 `.cs` 檔與一份對應 CSV。
- Set 至少一個 Dim；scalar 僅能是 Parameter。
- CSV 表頭與 `OptDim` 名稱一致；Parameter 的 `QTY` 必為最後欄。
- `Dataload` 只使用 `source.Load<T>()`；沒有 Set/Param 分流的載入函式。
- 只使用 `[OptSet]`、`[OptParam]`、`[OptVar]`、`[OptDim<T>]` 與 `BuildVars<T>`。
- 解出後仍須驗證每一條限制式、單位與量級，以及 LP bound sanity，才可視為 Coding 完成。

實際下指令的模板見 [prompt-guide.md](prompt-guide.md)。完整流程規則見 [`.claude/rules/AGENTS.md`](../.claude/rules/AGENTS.md)；框架 API 的最新狀態以 source 與 `OptimFoundation` 的 developer guide 為準。
