# 求解器調參：實務工作手冊

> 適用於模型、資料與商業規則都已確認正確，但**整數最佳化模型**（MIP/MIQP）求解太慢、gap 收不下來，或很難找到第一個可行解的情況。這裡的 **gap** 是「目前找到的答案」與「理論上還可能做到多好」之間的差距。以 CPLEX 與 `CplexConfig` 為例；方法論同樣適用於 Gurobi、SCIP 等求解器。
>
> **一句話結論：** 一流團隊不是「多試幾個參數」，而是照一套有規則的比較流程做事：先確認問題、固定比較條件、看求解過程提出假設、小範圍測試，再用沒看過的資料驗證，最後才放到正式環境。

---

## 先看這裡：這份文件的名詞小抄

英文或符號第一次出現時，先回來查這張表。不必背術語；理解它在回答哪個問題就夠了。

| 詞 | 白話意思 |
| --- | --- |
| **MIP / MIQP** | 有些變數必須是整數的最佳化問題；MIQP 另外允許目標或限制式有二次項。這類問題通常比純線性問題難解。 |
| **Status** | 求解器最後回報的狀態，例如 `Optimal`（已證明最佳）、`Feasible`（有可行解但還沒證明最佳）、`TimeLimit`（時間到）、`Infeasible`（根本無解）、`Unbounded`（目標值可以無限變好）。 |
| **objective** | 目標值，也就是模型想要最小化或最大化的數字；是否比較好要依模型方向判斷。 |
| **runtime** | 這次求解實際花的時間。只有所有設定都在相同完成標準下，runtime 才能直接比較。 |
| **instance** | 一筆完整的測試資料，例如一個月份、一個客戶、一張排班表。不要只用一筆 instance 就宣稱設定更好。 |
| **seed** | 隨機種子：讓同一種測試可以重跑的起點編號。換 seed 是同一設定再量一次，不是換了一種策略。 |
| **`ParallelMode`** | CPLEX 的平行搜尋模式。`-1` 是機會式平行：各核心自由搶工作，通常優先追求速度，但每次搜尋路徑可能不同；`0` 是自動：交給 CPLEX 決定；`1` 是決定論平行：盡量讓相同輸入、設定和機器走出可重現的搜尋路徑。做參數比較時固定用 `1`；正式環境則依「可重現」或「實際速度」的需求，在固定環境下實測後選擇，且不要和實驗用不同模式混著比較。 |
| **incumbent** | 到目前為止已找到、而且真的符合所有限制的最佳答案；也可理解為「目前手上的可行解」。 |
| **best bound / bound** | 求解器依數學推導得到的理論界限，用來判斷答案還有多少進步空間；它不是另一個可直接使用的解。 |
| **gap** | incumbent 和 best bound 之間還差多少。CPLEX 的相對 gap 是 `|incumbent − best bound| / (1e-10 + |incumbent|)`；例如目前可行解是 120、best bound 是 100，gap 約為 `(120 − 100) / 120 = 16.67%`。gap 越小，代表越接近證明最佳。沒有 incumbent 時，gap 沒有意義，通常會是 `NaN`。 |
| **`MipGap` / `TimeLimit`** | CPLEX 的停止條件：前者指定 gap 小到什麼程度可停，後者指定最多可跑多久。它們要在同一輪比較中固定。 |
| **IIS / Conflict** | 當模型無解時，求解器找出的「互相衝突的最小一組限制」。它用來找模型或資料哪裡打架，不是用來調參。 |
| **Big-M / soft constraint** | Big-M 是用一個很大的數字來控制邏輯限制；soft constraint 是允許違反限制、但在目標值加罰分。兩者都會改數學模型，不是調參。 |
| **R0** | 第 0 輪基準測試：什麼設定都不改，先量原本設定在多個 seed 上的表現和波動。 |
| **`t_feas`** | *time to feasible* 的縮寫：從開始求解到第一次找到 incumbent 所花的時間，也就是「找到第一個可行解要多久」。 |
| **`θ`** | 「原本設定自然會有多大波動」的門檻，從 R0 同一設定在多個 seed 的結果算出來。假設 runtime 是 92、100、108 秒：最大值是 108，最小值是 92；**不是拿其中某一次當分母**，而是先算這三次的「典型時間」。典型時間的正式定義是 `T = ((x₁ + s) × (x₂ + s) × … × (xₙ + s))^(1/n) − s`，其中 `x₁…xₙ` 是所有 R0 的時間，`s` 是事先固定的小常數（秒級問題可取 `s = 1` 秒）。本例 `T = ((92 + 1) × (100 + 1) × (108 + 1))^(1/3) − 1 ≈ 99.8` 秒。再算 `(108 − 92) / 99.8 ≈ 0.16`，表示自然波動約 16%。這個算法稱為 *shifted geometric mean*：先加 `s` 再相乘開 n 次方根，最後扣回 `s`；它用來避免很短的時間把平均扭曲。若比 gap，則直接用「最大 gap − 最小 gap」，單位是百分點。候選設定的改善沒有超過 `θ`，就不能確定是真的變好。 |
| **warm-up** | 第一次執行常會受程式載入、快取建立影響；這次只用來預熱，不放進比較。 |
| **保留測試** | 一開始故意不拿來挑設定的 seed 或 instance，最後才用來驗收，避免只是在原本資料上碰巧表現好。 |
| **shifted geometric mean** | 一種平均時間的算法，較不會被少數極端慢的結果帶偏；`shifted` 是先加一個小常數，避免很短的時間讓比率失真。 |
| **PAR10** | timeout 的計分方式：逾時不算成剛好跑滿時限，而是記成時限的 10 倍，避免把逾時設定看起來表現太好。 |
| **`NaN`** | 沒有數值的意思。例如完全沒找到可行解時，objective 和 gap 可能是 `NaN`；它不是 0。 |
| **root / node** | root 是求解一開始的第一個問題；node 是求解器分支後產生的一個子問題。node 很多或每個 node 很慢，都可能拖慢整體。 |
| **cuts** | 求解器自動加上的有效限制式，用來縮小「看起來可行、其實不會是整數解」的範圍。加太多也可能讓每一步變慢。 |
| **Probe（探測）** | 正式分支前，先暫時把一個 binary 變數各設成 0 與 1，檢查限制式會連帶逼出什麼結果：可能發現其中一邊立刻矛盾、其他變數被固定，或界限變好。它用前期計算時間換取後面較少的搜尋。 |
| **DFS、RINS、diving** | 都是幫助更快找到可行解的搜尋方式：DFS 先沿一條分支往下找；RINS 在目前答案附近再找更好解；diving 則快速固定一些變數、往下探索。 |
| **presolve** | 正式搜尋前先化簡模型，例如刪除重複資訊或固定部分變數。通常應保持開啟。 |
| **LP relaxation** | 暫時把「必須是整數」放寬成可以是小數後的版本。它若太鬆，求解器通常要花更多時間分支。 |
| **容量／背包型限制** | 有一個總上限，挑選的項目不能超過它，例如 `8x₁ + 6x₂ + 5x₃ ≤ 10`：三個項目分別占 8、6、5 單位空間，總容量只有 10。卡車載重、倉庫空間、預算、工時和設備產能都常是這種限制。 |
| **node file** | 記憶體不夠時，把部分搜尋狀態寫到磁碟。可避免記憶體爆掉，但通常會變慢。 |

---

## 1. 專家真正相信的事

### 1.1 先處理對的層級

資深最佳化工程師通常先問「慢在哪一層」，不會先問「哪個參數最快」。改善槓桿大致依下列順序排列：

1. **模型與資料正確性**：錯的模型再快也沒有價值；`Infeasible`、`Unbounded`、數值警告都不是 tuning 題目。
2. **數學 formulation**：界限、Big-M、鬆弛品質、對稱性、分解與初始解，通常比參數有更大數量級的影響。
3. **執行環境**：可用記憶體、執行緒、平行模式、版本與硬體決定量測是否可信。
4. **求解策略**：emphasis、branching、cuts、heuristics 等，才是狹義的 solver tuning。

因此，模型和資料一旦凍結，Phase 3 只能改 `CplexConfig`。若需要改約束、變數界限、Big-M、目標、資料或 soft constraint，應退回建模階段；那可能是正確的解法，但不應偽裝成調參。

### 1.2 預設值是嚴肅的基準，不是對手

現代商用求解器的預設值，已濃縮大量同類問題的經驗。專家的起點不是假設預設值很差，而是以它建立可重現的基準設定，證明某個改動在**代表性的問題組**上穩定地更好。單一案例、單一次牆鐘時間或「看起來 node 少了」都不足以正式採用。

### 1.3 速度不是唯一、也不一定是最重要的指標

「跑滿時限」的兩個 trial，runtime 都一樣；此時拿 runtime 排名沒有資訊。先依現況決定主指標：

| 目前狀態 | 真正要改善的事 | 主要比較方式 |
| --- | --- | --- |
| 已達 `Optimal`，只是想更快 | 用同樣品質更快完成 | runtime 的穩健彙總 |
| 有 incumbent 但撞時限（`Feasible`） | 固定預算下證明得更好 | end gap；輔助看 bound 與 incumbent |
| 撞時限且沒有 incumbent（`TimeLimit`） | 先穩定地找到可行解 | 找到解的比例，其次是 `t_feas`（首解時間） |
| `Infeasible` / `Unbounded` / `Error` | 修正模型、資料或界限 | **先停止調參** |

這也是專家會先固定「完成標準」的原因：`MipGap`、`TimeLimit`、`NodeLimit`、`IntegerSolutionLimit` 和數值容差，決定了什麼叫做完成；它們不是可以跟速度一起隨便比較的普通參數。改其中任何一項，就像把終點線移動，先前的測試便不能直接比較。

### 1.4 gap 怎麼算？

先確認兩個數字：

- **incumbent**：目前已找到的可行解目標值。
- **best bound**：求解器尚未排除的理論最好可能值。

CPLEX 的相對 gap（也就是 `MipGap` 主要使用的比例）為：

```text
relative gap = |incumbent − best bound| / (1e-10 + |incumbent|)
```

`1e-10` 是一個極小的保護值，避免 incumbent 剛好是 0 時除以 0。實務上可以把公式理解成「兩者相差多少，除以目前可行解的絕對值」。

**最小化範例：**目前可行解是 120，best bound 是 100。意思是目前知道可以做到 120，但理論上仍可能做到 100。

```text
gap = (120 − 100) / 120 = 0.1667 = 16.67%
```

**最大化範例：**目前可行解是 100，best bound 是 120。意思是目前已做到 100，但理論上仍可能做到 120。

```text
gap = (120 − 100) / 100 = 0.20 = 20%
```

所以不論最小化或最大化，都可以先取兩者的絕對差值；方向不同只會改變誰比較大。若 `MipGap = 0.05`，代表 gap 降到 5% 以下時，CPLEX 可以停止並回報「已達這個品質要求」。若沒有 incumbent，沒有可比較的 gap，通常顯示為 `NaN`；此時應先處理「如何找到第一個可行解」，而不是比較 gap。

---

## 2. 開始之前：先把比較條件說清楚

### 2.1 不可跳過的開始前檢查

以下任一項未成立，先不要調參：

- `dotnet build` 通過，且模型已跑過獨立的解驗證與商業規則驗證。
- 模型與資料在實驗期間凍結；所有 trial 使用相同 instance、資料版本與資源上限。
- 已明確記錄基準設定的 Status、objective、best bound、gap、runtime、seed、CPLEX 版本與機器資訊。
- 若 `Infeasible`，先取 IIS/Conflict；若 `Unbounded`，先找遺漏的界限。不可用 penalty 或 soft constraint 把問題「調成有解」。

### 2.2 四類設定，四種處理方式

把設定混在一起掃，是最常見也最昂貴的錯誤。

| 類別 | 例子 | 專家怎麼處理 |
| --- | --- | --- |
| **完成標準** | `MipGap`、`TimeLimit`、容差、node/solution limit | 這一輪調參期間固定；若要改，先和需求方確認，再從頭建立基準 |
| **執行環境** | `Threads`、`ParallelMode`、記憶體與 node file | 在 R0 前先決定，之後不再改；不要和搜尋策略放在同一輪比較 |
| **量測條件** | `Seed`、clock、機器、solver 版本 | 用來重複測量，不是要挑選的策略 |
| **搜尋策略** | emphasis、cuts、heuristics、node/variable selection、LP algorithm | 只有這一類可以拿來當候選設定 |

`Threads` 特別容易被誤當成搜尋策略。它會改變記憶體壓力、同步成本和搜尋路徑，連測試結果的波動都會一起改變；所以要先單獨決定。`Seed` 更不是一個「會贏的參數」：它只代表同一設定再跑一次。

### 2.3 量測環境要先安靜下來

- 固定 CPLEX 版本、OS、CPU、可用 RAM、資料與 instance 版本；不要在共享、負載飄動的機器上宣告 3% 的改善。
- 實驗時關閉 LP/MPS/SOL export、詳細 solver log 與非必要 I/O；這些只在最後的正式流程驗證時開啟。
- 做參數比較時使用 `ParallelMode = 1`（決定論平行）並固定 seed 集；這是為了減少每次走不同搜尋路徑造成的干擾。若要跨機器或跨次比較，採 `DeterministicTimeLimit` 更可靠。即使是模式 `1`，牆鐘時間仍可能受機器負載影響，所以仍要重跑多個 seed。
- 先做一次 thread sizing：同一個基準設定在幾個 thread 數各跑多個 seed。差距小時取較少的 thread，通常結果波動較小，也能為其他工作留出資源。
- 若記憶體壓力明顯，先處理樹記憶體與 node file，再談搜尋策略。此框架中設定 `MemoryLimitMb` 會將 `MIP.Strategy.File` 重設為 0，因此必須**後設** `NodeFileStrategy = 2/3`，避免前者覆蓋後者。

### 2.4 `ParallelMode`：多核心怎麼一起解題

`Threads` 決定「最多使用幾個核心」；`ParallelMode` 決定「這些核心如何分工」。兩者是不同設定。

| 值 | 名稱 | 適合什麼情況 | 要注意什麼 |
| --- | --- | --- | --- |
| `-1` | 機會式平行 | 只重視實際速度，且可以接受每次走不同搜尋路徑 | 核心會自由搶工作；結果與時間的波動通常較大，不適合拿來做細微的參數比較 |
| `0` | 自動 | 希望交給 CPLEX 決定 | 行為由求解器決定；要納入比較時，整個測試期間都必須固定為 `0` |
| `1` | 決定論平行 | 調參、除錯、需要重現結果 | 相同輸入、設定、機器下，盡量維持相同搜尋路徑；有時可能比機會式平行慢一些 |

**實務選法：**

- 做 R0 和所有調參比較時，固定 `ParallelMode = 1`，並固定同一組 seed。這樣不同設定的差異比較不會被平行時序掩蓋。
- 正式環境需要結果可重現時，也使用 `1`。
- 正式環境只追求速度時，可在代表性的真實資料上分別測 `-1`、`0`、`1`；選定後固定使用。這是「環境設定」的選擇，選完就重新做 R0，不可和舊結果混著比較。
- 即使選 `1`，牆鐘時間仍會受機器負載影響，故仍要多個 seed 重跑；若要更穩定地限制計算量，可加用 `DeterministicTimeLimit`。

### 2.5 第 0 輪（R0）：什麼都不改，先量基準

R0 是先量基準，不是白跑。用完全相同的正式設定，在 3–5 個 tuning seeds 上重跑；另外保留至少 3 個 seed，完全不參與選擇，最後才拿來驗收。R0 會得到三樣東西：

1. **平均表現與自然波動 `θ`**：後續改善沒有超過 `θ`，就當作平手，不能正式採用。
2. **求解過程的變化**：從 solver log 或 trajectory 觀察 incumbent、best bound 與 gap 隨時間的變化。
3. **下一步的判斷**：明確寫下這次慢的是找第一個可行解、推進 bound、單一 node 太貴、數值不穩，還是結果波動太大。

推薦的彙總方式：已解完的 runtime 用 shifted geometric mean（timeout 用 PAR10 罰分）；固定時限下的 gap 用算術平均（無 incumbent 視為 100%）；首解問題先比較找到 incumbent 的 seed 數，再比較其 `t_feas`。不要把 `NaN` 的 objective 或 gap 當成 0。

---

## 3. 先看問題卡在哪裡，再決定要改什麼

### 3.1 讀的是收斂行為，不是參數表

專家看 log/trajectory 會先回答四個問題：

1. **首個 incumbent 何時出現？** 很晚或完全沒有，代表 primal search 有問題。
2. **best bound 是否持續改善？** 很早停住，代表 dual bound／relaxation／cut 面向可能是瓶頸。
3. **gap 的哪一側卡住？** incumbent 沒變與 bound 沒變的處方不同。
4. **每個 node 是否昂貴？** 若軌跡稀疏、root 或 node LP 特別久，先懷疑 cuts、presolve 或 LP algorithm，而不是盲目加更多 cuts。

切記：實驗 CSV 的 `NodeCount` / `IterationCount` 若框架未填，不可拿空值推導瓶頸；callback 軌跡的末點也不必然是最終解，最終 Status、objective、bound、gap 應以 trial summary 為準。

### 3.2 看到什麼現象，就先試什麼方向

| 看到的現象 | 可能原因 | 優先嘗試的方向（一次一個） | 不要做 |
| --- | --- | --- | --- |
| 一直找不到 incumbent | 搜尋太晚深入可行區 | `Emphasis = 4`，再 `1`；`NodeSelect = 0`（DFS）；diving、RINS、heuristic effort | 先瘋狂加 cuts；它們主要推 bound，還可能讓 node 更貴 |
| 有首解但很慢才變好 | primal improvement 不足 | `Emphasis = 1`；RINS、heuristics、DFS、diving | 把 `MipGap` 放寬後宣稱變快 |
| incumbent 不差但 bound 很早停滯 | dual bound 太弱 | `Emphasis = 3`；有根據地試 Gomory/MIR/cover/clique/flow-cover cuts、probing、symmetry | 誤用 `Emphasis = 2` 當 best-bound 模式 |
| root／每個 node 都很貴 | 每個 LP 或 cut pass 成本過高 | 降低某類 cuts；試 root/node LP algorithm；必要時比較 presolve | 同時加 cuts、heuristics 和 threads，導致無法歸因 |
| numerical warning、結果不穩 | 係數尺度或數值條件差 | `NumericalEmphasis = true` 作診斷與保護 | 靠 tightening tolerance「修好」模型；根因通常在 formulation |
| 記憶體耗盡或 node file thrashing | tree 無法留在可用記憶體 | 降 threads；調整 tree memory 與 node-file 策略 | 把 `WorkMem` 當 process-wide RAM 限額 |
| 多次結果差異極大、無明確模式 | 結果波動大過真正的差異 | 停止，或增加樣本、改善測試環境 | 用最快的一次決定勝出設定 |

**CPLEX MIP emphasis 的正確語意：** `0` balanced；`1` feasibility；`2` optimality；`3` best bound；`4` hidden feasibility。若瓶頸是 bound，應先驗證 `3`，不是把 `2` 想像成「更用力證明最佳」。

### 3.3 一條實務鐵律：先區分「求解器診斷」與「模型診斷」

以下現象常被誤當成「再調幾個參數即可」：LP relaxation 很鬆、Big-M 過大、同質資源造成大量對稱、變數界限過寬、缺少有效初始解、模型超過可接受的規模。這些是 formulation 問題。求解器參數可以緩解症狀，卻通常不能創造數量級改善。專家會把它們寫成退場建議，交回模型擁有者，而不偷改模型。

---

## 4. 怎麼做一輪有意義的測試

### 4.1 每輪只回答一個問題

好實驗不是「測 20 組組合」，而是回答一個問題。例如：

> 我們看到：R0 的前 70% 時間都找不到 incumbent，問題可能在於很難找到可行解。預期：把 `Emphasis` 從 0 改為 1，能讓更多 seed 在固定時限內找到 incumbent，而且不會犧牲原本要求的解品質。

測試原因、預期結果、候選設定與「怎樣算成功」必須在開跑前寫入 `TuningHistory.md`。跑完才補理由，任何結果都能被事後合理化。

### 4.2 一輪只改一件事

每個候選設定都從目前正式設定 `Clone()` 出來；其他欄位不要亂填，讓 CPLEX 用預設值。可以在同一輪比較同一個參數的少量合理數值，例如 `Emphasis = 1` 與 `4`，但不要同時再改 cuts、threads 或 gap。兩個改動一起變好時，沒人能知道真正有用的是哪一個，也無法確認它們在保留測試上會不會互相拖累。

```csharp
var baseline = productionBaseline.Clone();
var feasibility = baseline.Clone();
feasibility.Emphasis = 1; // 此輪唯一改動

var result = new OptExperiment("MyProject-tuning-r1", "R0：首解太晚；驗證 feasibility emphasis")
    .AddModel(model)
    .AddConfig("r1-baseline", baseline)
    .AddConfig("r1-emphasis=feasibility", feasibility)
    .Run();
```

測試名稱使用 `<Project>-tuning-r<N>`，每輪遞增。這個框架若使用同名 experiment，會把舊 trial 加進來；名稱重複會讓舊資料混進本輪，造成誤判。

### 4.3 降噪的最低標準

- 同一輪的所有候選設定，必須使用同一組 seed、instance、完成標準與執行環境。
- 排除 warm-up，並輪替／隨機化候選設定的執行順序，避免基準設定總是承受 cold-start。
- 至少 3–5 個 tuning seeds；時間預算允許時，使用多個代表性訓練 instances，而不是只調一個幸運案例。
- 用穩健彙總而非單次最小值：runtime 常用 shifted geometric mean，timeout 用 PAR10；gap 是比例，通常用算術平均。
- 記錄結果波動範圍。候選設定的改善必須超過 R0 的 `θ`，才有資格說是真的變好。

性能可因 seed、row/column permutation、硬體與平行時序出現劇烈差異；這是 MIP 的已知特性，不是「多跑一次就沒事」。

### 4.4 判斷順序：先看正不正確，再看好不好，最後才看快不快

先淘汰，再排名：

1. **先看結果對不對**：Error、Infeasible、Unbounded、違反商業驗證或原本結果要求者，直接排除。
2. **再看解夠不夠好**：objective、gap、可行解狀態必須達到專案要求；較快但解較差，不能算贏。
3. **條件相同才比較速度**：前兩項相同或都達標時，才用這個情境該看的主要指標和穩健彙總比較。
4. **最後確認不是巧合**：改善必須超過 `θ`，而且在保留測試上還存在；否則保留原本設定。

這套順序把「求得不同品質的答案」與「更快求得同等答案」分開，是從技術展示走向可上線決策的分界。

---

## 5. 從候選設定到正式使用：最後不能省的幾步

### 5.1 沒看過的測試資料，才是正式驗收

調參時用過的 instances 和 seeds 就像訓練資料。選出的勝出設定，至少要在 3 個沒有參與選擇的保留 seed 上重跑；最好也在沒看過的 instances 上驗證。保留測試只能用來驗收，不能驗收後再改挑第二名，否則它也變成訓練資料。

若改善只在調參資料上出現，正確做法是保留原本設定，並記錄為 over-tuning；不是「多試一輪，直到它贏為止」。

### 5.2 正式採用前，留下可以追查與還原的紀錄

1. 將勝出設定的**完整設定值**明確寫回 `Program.cs` 的 `productionBaseline`；不可讓程式執行時，從歷史上最快的一筆資料自動挑設定。
2. 在原本設定上方記下來源：日期、experiment 名、trial label、前後差異、主要比較結果與保留測試結果。
3. 先更新 `TuningHistory.md`，再 `dotnet build`、跑無參數的正式流程，驗證 Status、objective、bound/gap、`ValidateRules` 與輸出。
4. 正式流程驗證失敗，就立刻保留／還原原本設定，標記 `rejected`；實驗結果不能代替正式流程驗證。

`bin/Experiments/*.json` 可被 clean 移除，不能是唯一證據。每輪的歷史至少應包含：

```text
日期 / round / experiment name
完成標準、解品質要求、執行環境、量測方式
R0 基準設定與本輪候選設定的完整差異
instances / seeds / 保留測試 / 彙總方法
為何測試 → 預期什麼 → 實際結果 → 最後決定
Status / objective / bound / gap / 主要比較指標 / θ
正式採用 | 保留原設定 | 不採用，以及正式流程驗證結果
```

### 5.3 先訂好何時停止，避免一直調下去

停止不是失敗，是專業節制。建議命中任一條就收尾並回報：

- 連續 3 輪、或完整診斷類別已無改善超過 `θ`。
- 保留測試上的改善消失。
- 量測變異主導，無法從噪訊辨別策略效果。
- 已達業務 SLA／品質目標。
- 發現根因在模型結構、數值尺度、資料或硬體資源，而非求解策略。
- 已耗盡事先同意的計算預算。

交付「保留現有設定，並建議收緊 Big-M／加 MIP start／重新建模」常比交付一組碰巧較快的參數更有價值。

---

## 6. 自動調參：何時有用、何時危險

CPLEX 內建 tuning tool 很適合作為低成本的參考：匯出 `.lp`，用 Interactive Optimizer 或 IDE 對一個或多個代表性 run configurations 產生建議。它可以找出值得測的設定，但不能直接把建議放到正式環境；每個建議仍要拆成單獨的候選設定，照 R0、保留測試與品質檢查的流程驗證。

當手動診斷已鎖定瓶頸、但少量候選仍無勝者，才考慮 algorithm configuration 工具，例如 irace 或 SMAC。專業使用方式是：

- 用 instance portfolio，而非一個 instance；明確分 train / validation / test。
- 一開始只開少量、和目前問題相符的搜尋策略；不要把數十個參數全部交給 optimizer。
- 把 timeout、記憶體限制與錯誤狀態納入 cost function，並採 racing/early stopping 節省無望候選的預算。
- 把硬體、solver 版本、完成標準、instance 分布視為測試條件的一部分；它們改變後，要重做基準測試。
- 自動工具只負責「提出候選設定」；最後要不要採用，仍由獨立的保留測試與正式流程驗證決定。

依每個 instance 用 ML 選參數，是研究等級、也需要較高投資的做法：需要足夠的歷史 instances、特徵、線上監控與漂移處理。沒有這些條件時，可靠的基準設定加上少量可解釋實驗，通常更有商業價值。

---

## 7. 速查：常見 CPLEX 策略旋鈕

以下表格用來根據現象找方向，不是讓你盲目把每個選項都掃一遍。每次只挑一項，其他設定保留預設值。

| 目的 | 可驗證的候選 | 注意事項 |
| --- | --- | --- |
| 更早找到可行解 | `Emphasis = 1/4`、RINS、heuristic effort、DFS、diving | 可能犧牲 bound；用首解率／`t_feas` 衡量 |
| 更快收 dual bound | `Emphasis = 3`、選定的 cuts、probing、symmetry | cut 過量可能讓 node 變慢；不要同時全開 |
| 降低 node 成本 | 降 cuts、`RootAlgorithm`、`NodeAlgorithm`、presolve 比較 | presolve 通常應開啟；關閉主要用於診斷 |
| 數值穩定 | `NumericalEmphasis = true` | 若改善有限，回 formulation 檢查係數尺度和 Big-M |
| 緩解記憶體 | `TreeMemoryLimitMb`、`NodeFileStrategy = 2/3`、較少 threads | node file 是延緩 OOM 的取捨，常以 I/O 換時間 |
| 可重現性 | `ParallelMode = 1`、固定 seed、決定論時間 | 計時仍可能受機器負載影響，需重複量測 |

下列設定不要拿來和搜尋策略混著比：`MipGap`、`TimeLimit`、`DeterministicTimeLimit`、`NodeLimit`、`IntegerSolutionLimit`、數值容差、`Threads`、`ParallelMode`、記憶體限制與 `Seed`。它們決定完成標準、執行環境或量測方式，要另外管理。

---

## 8. `CplexConfig` 全部旋鈕：意思、怎麼試、可能的影響

以下以本專案實際的 `CplexConfig` 欄位為準。`null` 代表不特別設定，交給 CPLEX 選預設值。除非表格明說是「完成標準」或「執行環境」，否則每一輪只挑一個欄位改動，再和基準設定比較。

目前 `CplexConfig` 預先填入的值是：`Threads = 32`、`RowRead = 30000`、`MemoryLimitMb = 2048` MB、`MipGap = 1e-4`、`OptimalityTol = 1e-6`、`FeasibilityTol = 1e-6`。其他欄位通常是 `null`，也就是 CPLEX 預設。

### 8.1 完成標準與數值判定：不要拿來宣稱「變快」

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `MipGap` | 相對 gap 小到多少即可停止 | `0.05` 是 5%，`0.01` 是 1%，`0` 代表要求證明最佳 | 值越大越早停，但答案可能較不接近最佳；這是品質要求，不是加速策略。改了它要重新建立基準。 |
| `AbsoluteMipGap` | 允許的**絕對**目標差距 | 目標值很接近 0、或業務用金額／件數的絕對誤差更有意義時才設 | 可避免相對 gap 在目標值接近 0 時不好解讀；和 `MipGap` 一樣是品質要求。 |
| `TimeLimit` | 最多跑幾秒（牆鐘時間） | 依 SLA 設定，例如 300 秒 | 時間到仍可能回傳當下可行解；改時限等於改考題，不能拿不同時限的 runtime 排名。 |
| `DeterministicTimeLimit` | 最多允許多少 CPLEX 決定論 ticks | 要跨次比較、希望每次在相同計算進度停下時使用 | 比牆鐘時間更適合可重現實驗；不是秒，實際換算速度因機器而異。 |
| `NodeLimit` | 最多探索多少 branch-and-bound node | 只在預算需要限制搜尋量時設定 | node 到上限就停止；不同策略每個 node 的成本不同，所以它不等於固定秒數。 |
| `IntegerSolutionLimit` | 找到幾個整數可行解就停 | 只想很快拿第一個解時可設 `1`；要多個替代解才設較大值 | 會犧牲最佳性證明；不能和一般求到 `MipGap` 的結果比速度。 |
| `IntegralityTolerance` | 變數多接近整數才算整數 | 通常維持預設；只有數值問題且有明確理由才調 | 放寬可能接受看似整數、其實有小數誤差的值；收緊可能變慢或引發數值問題。 |
| `OptimalityTol` | LP simplex 判定最佳性的數值容忍度 | 通常維持 `1e-6` | 收緊可能提高精度但變慢；它不是改善 MIP gap 的工具。 |
| `FeasibilityTol` | 限制式允許多大的數值違反 | 通常維持 `1e-6` | 收緊可能讓原本接近邊界的解變成不可行；若常要改，先回頭查係數尺度與 Big-M。 |
| `ClockType` | solver log 的時間基準 | `1` CPU time、`2` wall-clock；整輪固定 | 影響記時報表，不直接改搜尋策略。一般 SLA 看 wall-clock。 |

### 8.2 執行環境與記憶體：先決定，再做策略測試

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `Threads` | 最多用幾個求解執行緒 | 先單獨比較幾個合理值，再固定 | 核心多不一定快；同步與記憶體壓力可能反而變大。改後要重跑 R0。 |
| `ParallelMode` | 多核心如何協作 | `-1` 機會式、`0` 自動、`1` 決定論；調參固定用 `1` | `-1` 可能較快但路徑較不穩；`1` 較好重現。改模式後要重跑 R0。 |
| `MemoryLimitMb` | CPLEX 工作記憶體上限（MB） | 依可用 RAM 設定，保留給 OS 與其他程式足夠空間 | 不是整個程式的 RAM 上限。本框架設它時會把 node-file 策略重設為 `0`，所以若需要寫檔，必須後設 `NodeFileStrategy`。 |
| `TreeMemoryLimitMb` | 搜尋樹可用記憶體上限（MB） | 搜尋樹大、記憶體壓力高時設定 | 設太低會更早寫 node file 或停止；設太高可能擠爆 RAM。 |
| `NodeFileStrategy` | node 太多時，搜尋狀態放哪裡 | `0` 不存、`1` 壓縮放記憶體、`2` 放磁碟、`3` 壓縮放磁碟 | `2/3` 用磁碟換記憶體，通常較慢但可避免 OOM；在本框架必須放在 `MemoryLimitMb` 設定之後。 |
| `RowRead` | 讀取模型檔時允許的限制式數上限 | 只有讀入 LP/MPS 檔而碰到列數上限時才提高 | 幾乎不影響記憶體中已建好的模型，不是一般調參旋鈕。 |

### 8.3 可重現與安全診斷：用來量測，不是用來選勝者

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `Seed` | 隨機搜尋的起點 | 一輪內所有設定跑同一組 seed；調參時搭配 `ParallelMode = 1` | 改 seed 只是重新量一次，不代表策略更好；不可把 seed 當候選設定排名。 |
| `NumericalEmphasis` | 是否優先穩定性而非速度 | log 有數值警告、結果不穩、係數尺度差異很大時設 `true` | 可能變慢，但可降低數值風險；若有效，仍應回模型檢查 Big-M、單位與係數大小。 |
| `Presolve` / `PreIndicator` | 是否在求解前先化簡模型 | 一般保持開啟；`Presolve = 0` 只用於診斷或找 IIS | 關閉通常明顯變慢；`Presolve` 與 `PreIndicator` 是同一個設定的兩種寫法，不要兩邊都設。 |
| `Symmetry` | 對稱性消除強度 | `-1` 自動、`0` 關閉、`1..5` 越積極；同質機器、人員、車輛多時可試 | 可避免探索等價答案；偵測／處理本身有成本，無對稱的模型未必有益。 |

### 8.4 找第一個可行解：首解太晚時才試

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `Emphasis` | CPLEX 把時間偏向找解或證明最佳 | `0` 平衡、`1` 偏找可行解、`2` 偏最佳性、`3` 偏推 best bound、`4` 專找很難找到的可行解 | 首解太晚先試 `1`，仍找不到再試 `4`；這些設定常會讓 gap 收得較慢。 |
| `NodeSelect` | 下一個要展開哪個 node | `0` DFS、`1` best-bound、`2` best-estimate、`3` 交替 best-estimate | 首解太晚可試 `0` 或 `2`；要收 bound 通常保留 `1`。DFS 不一定快，只是更早往深處找。 |
| `DiveType` | 是否主動固定部分變數、快速往下找可行解 | `0` 自動、`1` 傳統、`2` 探測、`3` 引導 | 找不到首解時依序試；可能更快得到解，也可能花很多時間在不好的分支。 |
| `RinsHeuristicFrequency` | 多久做一次 RINS 搜尋 | `-1` 關閉、`0` 自動、`N` 代表每 N 個 node 嘗試；N 越小越常跑 | 對已有可行解、想改善它時較有機會；太頻繁會拖慢主要搜尋。 |
| `HeuristicEffort` | 啟發式找解投入多少力氣 | `0` 關閉、`1` 預設、`>1` 更積極 | 首解或改善 incumbent 困難時可提高；花更多時間找解，通常會少一些時間推 bound。 |
| `BranchDirection` | 分支時先嘗試較小或較大的值 | `-1` 先向下、`0` 自動、`1` 先向上 | 只有明確知道「選 0／選 1」哪邊較容易可行時才試；大多數情況保留自動。 |
| `VariableSelect` | 優先對哪個變數分支 | `-1` min-infeas、`0` 自動、`1` max-infeas、`2` pseudo cost、`3` strong branching、`4` pseudo reduced cost | 強分支可能減 node，但每次選變數很貴；這類欄位通常在已知道 branching 是瓶頸時才碰。 |

### 8.5 推進理論界限：有好解、但 gap 很難收時才試

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `Emphasis` | 同上 | gap 主要卡在 best bound 時，試 `Emphasis = 3` | `3` 是推 best bound；`2` 只是較偏最佳性，不要混為一談。 |
| `Probe` | 正式分支前，對 binary 變數做多強的「先假設、再檢查」 | `-1` 關、`0` 自動、`1..3` 越積極；難題才從 `1` 開始單獨比較 | 對某個 `y`，CPLEX 會先試 `y=0`、再試 `y=1`，並把限制式能推出的結果一路往下算。例如 `y=1` 若立刻和其他限制矛盾，就知道 `y` 必須是 0；若 `y=1` 會逼 `x1=0`、`x2=0`，這兩個變數也不必再猜。好處是後面少走很多分支；代價是 root 前期可能花很久。值越大，前期時間可能大幅增加，也可能大幅減少總時間。 |
| `CutsFactor` | 全部 cuts 的總量上限倍數 | 從小幅提高或降低開始，例如只測一個相鄰值 | cuts 多通常有助於收 bound，但 root/node 可能變慢、記憶體變大。 |
| `CutPasses` | 最多做幾輪 cut 生成 | `-1` 不做、`0` 自動、正整數為上限 | 增加回合可能強化 relaxation，也可能只增加前處理時間。 |
| `GomoryCuts` | Gomory fractional cut 強度 | `-1` 關、`0` 自動、`1/2` 逐步增加 | 對某些整數結構可收緊界限；太積極可能使 LP 變慢。 |
| `CoverCuts` | cover cut 強度 | `-1` 關、`0` 自動、`1..3` 越積極 | 常和容量／背包型限制相關。例如 `8x₁ + 6x₂ + 5x₃ ≤ 10` 中，`x₁` 和 `x₂` 不能同時選，這種「不可能一起選」的資訊可形成 cover cut。沒有這類限制時，通常只增加成本。 |
| `CliqueCuts` | clique cut 強度 | `-1` 關、`0` 自動、`1..3` 越積極 | 互斥、衝突圖結構明顯時較值得試；可能增加前處理與記憶體。 |
| `MirCuts` | MIR（mixed-integer rounding）cut 強度 | `-1` 關、`0` 自動、`1/2` 逐步增加 | 對混合整數線性模型常有幫助；過強仍可能讓 node LP 變慢。 |
| `FlowCoverCuts` | flow-cover cut 強度 | `-1` 關、`0` 自動、`1/2` 逐步增加 | 流量、容量、固定成本結構時較可能有效；不要因名稱就套到不相關模型。 |

### 8.6 每個 node 很慢：先減成本，再換 LP 演算法

| 欄位 | 它控制什麼 | 怎麼設／何時改 | 預期影響與注意事項 |
| --- | --- | --- | --- |
| `RootAlgorithm` | root node 的 LP 演算法 | `0` 自動、`1` primal、`2` dual、`3` network、`4` barrier、`5` sifting、`6` concurrent | root 花了大部分時間時才比較；network 只適合網路結構，concurrent 會占用多個 thread。 |
| `NodeAlgorithm` | root 之後各 node 的 LP 演算法 | `0` 自動、`1` primal、`2` dual、`3` network、`4` barrier、`5` sifting | 只有 node LP 真的是瓶頸時才試；沒有 concurrent 值 `6`。 |
| `BarrierAlgorithm` | barrier 類 LP 演算法的選擇 | `0` 預設，`1..3` 是 CPLEX 的不同 barrier 方法 | 只有已選 barrier、且 root LP 很重時才測；一般 MIP 不應把它當第一個旋鈕。 |
| `SimplexIterationLimit` | simplex 最多做幾次迭代 | 只在想診斷 LP 卡住、或必須限制 LP 工作量時設定 | 到上限會提早停止 LP，可能讓整個 MIP 解得更差；不是一般加速方式。 |
| `MipSearch` | 使用哪種 branch-and-cut 搜尋架構 | `0` 自動、`1` 傳統、`2` 動態 | 一般保留自動／動態；只有除錯、相容性或已知需求才改成傳統。控制型 callback 可能迫使 CPLEX 使用傳統搜尋。 |
| `PolishAfterTime` | 跑滿多久後，改為專心改善已有答案 | 設秒數；只在「可行解品質比證明最佳更重要」時使用 | 可能得到更好 incumbent，但會放棄部分證明最佳的努力；把它視為業務策略，不是免費加速。 |

### 8.7 R0 不會叫你放棄；它只告訴你先從哪裡開始

R0 的作用是量出原本設定的表現、自然波動與主要問題，**不是**「R0 沒改善就停止」。R0 本來就沒有改任何設定，所以不可能因為 R0 沒變快而放棄。

真正該停止的情況只有：模型本身有錯、已達業務目標、結果波動大到量不出差異，或已用完事先同意的計算預算。只要 R0 顯示「還沒達目標，而且量得出差異」，就應繼續測候選設定。

### 8.8 專家常用的三種測法：依預算與複雜度選

「一次只改一個旋鈕」是**低風險、好解釋的第一階段**，不是唯一標準作法。它的目的，是先找出哪些方向有訊號；當確定有訊號後，專家會視時間與參數交互作用，採下列其中一種方式。

| 方法 | 怎麼做 | 何時適合 | 優點與限制 |
| --- | --- | --- | --- |
| **A. 逐項篩選** | 每次只改一項；先從 R0 診斷對應的 3–5 個候選開始 | 計算預算小、需要清楚解釋為何採用 | 最容易知道哪一項有用；看不到兩個設定一起才有效的情況 |
| **B. 先單項、再組合** | 先用 A 找出 2–3 個有效方向，再只測少量合理組合；組合一定要和它的每個單項、基準設定一起比較 | 最常見的實務折衷 | 能檢查交互作用，又不會掉進全部組合的爆炸式搜尋 |
| **C. 自動搜尋組合** | 使用 CPLEX tuning tool，或 irace／SMAC；固定完成標準與環境，拿多個代表性 instance 搜尋組合 | 有足夠 instances、計算預算與重複使用價值 | 能找人手不易想到的組合；結果仍必須做保留測試，且較難解釋每一項的單獨貢獻 |

**推薦的實務流程是 B：**

1. R0 發現問題，例如「固定時限內很少找到可行解」。
2. 先做小範圍篩選：`Emphasis = 1`、`Emphasis = 4`、`NodeSelect = 2`、`DiveType = 2` 等，每一項各自和基準設定比。這一步不是只試一個就放棄，而是試**同一問題方向的少量合理候選**。
3. 取真正超過 `θ` 的 1–2 項，再測合理組合，例如「找解較積極的設定 + RINS」；組合的結果必須也超過兩個單項，而不只是贏過基準設定。
4. 用沒參與挑選的保留 seed／instance 驗收。若組合沒有在保留測試變好，就回到單項勝者或保留原本設定。

例如首解困難時，`Emphasis = 1` 無效，**不代表**「找可行解這條路沒有希望」，更不代表停止。它只否定「feasibility emphasis 單獨有效」這個猜測；接著仍可測 `Emphasis = 4`、`NodeSelect = 2`、`DiveType = 2`、RINS，或在已有訊號後測少量組合。

不要做的是一開始把 RINS、diving、threads、所有 cuts 全部打開。那不是在測交互作用，而是在沒有假設、沒有預算控制下盲搜；即使剛好變快，也很難在其他資料上重現。

## 9. 一頁式操作清單

1. 確認模型正確、有可比較的資料；`Infeasible` / `Unbounded` 先退回修模型。
2. 固定完成標準、資料、版本與硬體；分清完成標準、執行環境、量測方式與搜尋策略。
3. 先把 threads、平行模式與記憶體策略決定好，然後跑 R0 的多 seed 基準測試。
4. 從 trajectory 判斷該看什麼指標、問題卡在哪裡；先寫下預期結果與成功條件。
5. 一輪只改一個搜尋策略，所有候選設定都從基準設定 clone 出來。
6. 用相同 seeds/instances、穩健的平均方式與 `θ` 評估；先看解的品質，再看速度。
7. 勝出設定要在沒看過的保留測試上驗收；不通過就保留原本設定。
8. 寫回完整的正式設定、留下來源紀錄，重新 build 並跑正式流程驗證。
9. 達停止條件就結束；若是 formulation 問題，明確退回建模階段。

---

## 參考依據與延伸閱讀

- Ed Klotz、Alexandra M. Newman, [*Practical Guidelines for Solving Difficult Mixed Integer Linear Programs* (2013)](https://doi.org/10.1016/j.sorms.2012.12.001)：以 CPLEX 實務經驗整理困難 MILP 的診斷與處置方向。
- Andrea Lodi、Andrea Tramontani, [*Performance Variability in Mixed-Integer Programming* (INFORMS, 2013)](https://doi.org/10.1287/educ.2013.0112)：說明 MIP 的平台、排列與隨機性造成的性能變異，支持多次量測與穩健比較。
- Katharina Eggensperger et al., [*Pitfalls and Best Practices in Algorithm Configuration* (2017)](https://arxiv.org/abs/1705.06058)：實驗設計、訓練／測試分離與自動配置的常見陷阱。
- [IBM ILOG CPLEX Optimization Studio Documentation](https://www.ibm.com/docs/en/icos)：CPLEX parameter reference 與 performance tuning tool 的官方語意。

本專案的欄位名稱、框架限制與完整 `CplexConfig` 對照，請以同層的 `solver-tuning-guide.md` 與實際程式碼為準；不要憑記憶把其他 solver 的參數語意直接套到 CPLEX。
