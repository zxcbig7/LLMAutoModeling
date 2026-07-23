# 模型撰寫引導：怎麼寫數學模型，才能逐段機械轉譯成 code

> 核心信念：**數學模型是唯一真相，code 只是它的機械轉譯。**
> 你在 `Model/<Name>_Model.md` 寫好的每一段，都對應到一個固定位置的 code 檔——寫模型時就照這個對應寫，轉譯時就不用動腦。
> 全程 worked example = `Projects/WeeniesBuns`（2 變數 3 限制式的最小 LP，麵包工廠）。

## 一句話對應表（先記這個）

| 數學模型寫的 | 落到哪個 code | 規則 |
|---|---|---|
| 一個集合 $\mathcal{I}$ | `Set/Set_<Name>.cs`（一集合一檔） | `[OptSet<T>]` partial class |
| 一組參數 $c_i, f_i, \dots$ | `Parameter/Parameter_<Name>.cs` | 每個 index 一個 property + 每個係數一個 property |
| 一個決策變數 $x_i$ | `Variable/Variable{X/B/I}_<Name>.cs` | 前綴定型別（X連續/B二元/I整數）+ 每維一個 `[OptDim]` |
| 目標式 $\max \sum \dots$ | `Objective/ObjectiveFunction.cs` | 每一項 → 一句 `AddLHS(係數, 變數)` |
| 每條限制式 `[Cn]` | `Constraint/Constraint_<名>.cs`（一條一檔） | 每一項 → `AddLHS`/`AddRHS`，方向 → `Create{Less/Great}Equal` |
| 整個模型組裝 | `Model/<Name>Model.cs` | 把上面全部串起來 plug 進 `OptModel` |

**寫模型時的關鍵動作：給每條限制式編號 `[C1]`、`[C2]`…** —— 這個編號會變成 code 的檔名與註記，是模型與 code 對得上的錨點。

## 逐段對照（WeeniesBuns）

### ① 集合 → `Set/`

模型寫：
$$\mathcal{I} = \{\text{Frankfurter},\ \text{Bun}\}$$

code（`Set/Set_ProductType.cs`）：
```csharp
[OptSet<string>] public partial class Set_ProductType { }
```
> 一個集合 = 一顆積木一個檔。元素型別（string/DateTime/int）在 `[OptSet<T>]` 顯式寫出。

### ② 參數 → `Parameter/`

模型寫（一張表，每個產品有多個係數）：

| 符號 | 說明 |
|---|---|
| $f_i$ | 每單位麵粉用量 |
| $p_i$ | 每單位豬肉用量 |
| $l_i$ | 每單位人工 |
| $c_i$ | 每單位利潤 |

code（`Parameter/Parameter_ProductSpec.cs`）——**同一個 index $i$ 上的多個係數，包成一個 Parameter 類**：
```csharp
[OptParam]
[OptDim<Set_ProductType>("ProductType")]   // ← index i
public partial class Parameter_ProductSpec  // 係數：FlourPerUnit / PorkPerUnit / LaborPerUnit / Profit
```
> 寫模型的訣竅：**同一個下標的係數放同一張表**，轉譯時就是同一個 Parameter 類，一個 index 一個 `[OptDim]`。

### ③ 決策變數 → `Variable/`

模型寫：
$$x_i \geq 0, \quad \forall i \in \mathcal{I} \quad(\text{連續})$$

code（`Variable/VariableX_Production.cs`）：
```csharp
[OptVar]
[OptDim<Set_ProductType>("ProductType")]
public partial class VariableX_Production { }   // X_ = 連續；B_ = 二元；I_ = 整數
```
> 變數的**型別由前綴決定**，寫模型時就註明「連續/二元/整數」，轉譯直接對到 `VariableX_`/`VariableB_`/`VariableI_`。

### ④ 目標式 → `Objective/`

模型寫：
$$\max \quad \sum_{i \in \mathcal{I}} c_i \, x_i$$

code（`Objective/ObjectiveFunction.cs`）——**Σ 的每一項 → 一句 `AddLHS`**：
```csharp
foreach (var spec in _spec)
    _engine.AddLHS(spec.Profit, new VariableX_Production { ProductType = spec.ProductType }); // c_i · x_i
_engine.CreateMaximize();   // max
```
> `Σ_i` → `foreach`，`c_i·x_i` → `AddLHS(係數, 變數)`，`max` → `CreateMaximize()`。一對一，不移項、不合併。

### ⑤ 限制式 → `Constraint/`（一條一檔，檔名帶語意）

模型寫（編號 `[C1]`）：
$$[C1]\quad \sum_{i} f_i\, x_i \ \le\ F \qquad(\text{麵粉產能})$$

code（`Constraint/Constraint_Flour.cs`）：
```csharp
/// [C1] 麵粉產能：Σ_i FlourPerUnit[i]·x[i] ≤ FlourCapacity
foreach (var spec in _spec)
    _engine.AddLHS(spec.FlourPerUnit, new VariableX_Production { ProductType = spec.ProductType }); // f_i · x_i（LHS）
_engine.AddRHS(_flourCap);        // F（RHS）
_engine.CreateLessEqual($"{ConstraintName}");   // ≤
```
> **左式的項 → `AddLHS`，右式的項 → `AddRHS`，比較符號 → `Create{Less/Great}Equal`。**
> `[C1]` 這個編號同時是：模型的條號、`Constraint_Flour` 的 `///` 註記、驗證時對答案的錨點。

### ⑥ 組裝 → `Model/<Name>Model.cs`

把上面全部串起來（`Model/WeeniesBunsModel.cs`）——**限制式順序照模型的 `[C1][C2][C3]`**：
```csharp
public void CreateModel(OptEngine engine)
{
    new ObjectiveFunction(_d.parameter_ProductSpec, engine).Build();
    new Constraint_Flour(_d.parameter_ProductSpec, _d.FlourCapacity, engine).Build(); // [C1] ≤
    new Constraint_Pork(_d.parameter_ProductSpec, _d.PorkCapacity, engine).Build();   // [C2] ≤
    new Constraint_Labor(_d.parameter_ProductSpec, _d.LaborCapacity, engine).Build(); // [C3] ≤
}
```
> 每條限制式**只拿它需要的積木**（`parameter_ProductSpec` + 那條的容量上限），不整包傳 Dataload——這樣一看 ctor 就知道這條限制式依賴什麼。

## 寫模型時就做對的 checklist（讓轉譯零思考）

1. **每條限制式編號 `[Cn]`**，寫清楚語意 + 涉及哪些下標 —— 編號會變檔名與註記
2. **同下標的係數併成一張參數表** —— 對到一個 Parameter 類
3. **變數註明型別**（連續/二元/整數）—— 對到 `X_/B_/I_` 前綴
4. **限制式寫成標準式**：所有變數項在左、常數在右、中間一個比較符號 —— 對到 `AddLHS`/`AddRHS`/`Create*Equal`，**不要先自己移項化簡**（那會讓 code 對不回模型）
5. **數值一律給符號名**（$F$、$c_i$），不要在式子裡寫死數字 —— 對到 `Parameter.QTY` / Dataload 欄位，NEVER hardcode
6. 寫完 code **跑一次，比對目標值**與模型手算/已知解是否一致 —— 不一致代表某段沒對上

## 為什麼這樣寫（框架精神）

這個框架刻意把「發揮空間」關掉：你不是在「寫程式解問題」，是在「把已經寫好的數學模型逐段抄成 code」。模型寫得越貼近上面的對應規則，轉譯越像機械翻譯、越不會出錯，接手的人也越容易用 code 反推回模型驗證。

> 相關：組裝架構規範見 `claudemdTemplate/Model/CLAUDE.md`；限制式逐項對照註記見 `claudemdTemplate/Constraint/CLAUDE.md`；完整可運作範例見 `Projects/WeeniesBuns`。
