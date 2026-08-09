# 為什麼使用 OptimFoundation

OptimFoundation 的目的不是替使用者猜數學模型，而是把「已確認的數學模型」穩定地翻譯、驗證與交給 solver。新版設計把資料與模型分成明確、可檢查的層次。

## 它解決的工程問題

| 痛點 | 框架做法 |
| --- | --- |
| 資料表與模型類別容易不同步 | `OptDim<T>` 同時定義 property、CSV 欄名與維度順序 |
| Set 與 Parameter 有兩條不同資料管線 | 都是 row class，統一 `Load<T>()` 與 `WriteRows` |
| 多維資料需要自行維護 tuple 與欄位對照 | 多個 primitive `OptDim` 直接表示一列多維資料 |
| scalar 常數混進程式 | 零維 `Parameter_*` 只讀取一筆 `QTY` |
| 手寫變數 key 與 solver API 容易出錯 | generator、`BuildVars<T>` 與 Pool API 收斂重複工作 |
| 限制式改寫時符號出錯 | LHS / RHS Pool 保留數學式左右兩側，框架處理移項 |

## Source generator 的角色

開發者只寫意圖：元素是 Set、Parameter 或 Variable，以及它有哪些 primitive 維度。generator 產生對應 property、row 行為與 Parameter 的 `QTY`。

```csharp
[OptParam]
[OptDim<string>("Customer")]
[OptDim<DateTime>("Date")]
public sealed partial class Parameter_Demand { }
```

這個宣告同時是 C# 型別契約與 CSV 契約。它沒有「Set 是 type、Parameter 才是資料」的雙重概念：兩者都是資料列，差別僅在 Parameter 有數值欄。

## 模型建構的護欄

框架要求限制式保持原式：左側加入 `AddLHS`，右側加入 `AddRHS`，再選擇正確的比較運算。這降低人為移項、改號與維度接錯的風險。

完整流程仍保留人為 gate：模型先經確認，再進程式；求解後必須做解驗證；效能問題最後才進 tuning。框架讓這些步驟更機械、更可追溯，但不取代對問題語意的判斷。
