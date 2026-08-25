# tuning · 交付前自檢

> 規則本體在同層 `solver-tuning-guide.md` 與上層 `../AGENTS.md`。
> **本 skill 不依賴任何外部腳本**：所有驗證都是這份清單裡「開檔、讀值、比對」的動作，沒有 `.ps1` / `.py` 要跑。
> 本檔只是交付前的勾選面。**沒有實驗證據的項目不准打勾。**
> ★ = 錯了不會報錯，但結論整批作廢。

## 進場 gate

- [ ] 使用者主動提出調校需求（不是我自己起意）
- [ ] `dotnet build` 通過
- [ ] Status 落在可進場的三態（`Optimal` / `Feasible` / `TimeLimit`）—— **沒有因為「不是 `Optimal`」就退回 `coding`**
- [ ] ★ **進場情境已判定**（A / B / C）並寫進 `TuningHistory.md` 契約區塊
- [ ] ★ **主指標已依情境選定**：A = runtime `sgm`；B = endGap 平均；C = 找到解的 seed 數 → `t_feas`
- [ ] 情境 C 時，`status.json` 的 `verifiedOn` 是 `small-instance:*`（模型至少被某個 instance 驗過）
- [ ] `coding` 的解驗證協定四步已**實跑**確認過（不只是看 `status.json`）
- [ ] ★ 已記錄 Phase 2 結果基線 `phase2Status` / `phase2Objective` / `phase2Bound` / `phase2Gap` / `verifiedOn`
- [ ] exp 分支已是 Phase 2 交付的 R0-ready 形狀（名稱 / `r0-` label / marker / baseline × 5 seeds），**未重寫**；不符處已記成 finding 並就地補正

## 範圍界線

- [ ] 這輪確實是「模型與資料固定、只嫌慢或收斂不了」的情境
- [ ] 要換資料 / 改結構的訴求已被擋在門外並退回對應 phase
- [ ] `Infeasible` / `Unbounded` 沒有被當成 tuning 題目處理（已附 IIS 或缺界證據退回）
- [ ] 沒有為了「讓它變 `Optimal`」而放寬 `MipGap` 或加大 `TimeLimit`（那是動停止契約，要使用者拍板）

## 契約與環境（S0 / S1）

- [ ] 契約區塊已寫進 `TuningHistory.md` 開頭：停止契約 / 環境契約 / 量測契約 / Phase 2 基線
- [ ] ★ **停止條件**（`MipGap` `TimeLimit` `NodeLimit` `IntegerSolutionLimit` 容差，共 27 顆）整期固定，**未進 variant 池**
- [ ] ★ **執行資源**（`Threads` `ParallelMode` `MemoryLimitMb` `NodeFileStrategy`，共 9 顆）已定版並凍結——同一台實機**沿用 Phase 2 的值**，只有換機 / 換 CPLEX 版本 / Phase 2 未明設才重跑 S1 sizing
- [ ] `ParallelMode = 1` 已明設（不依賴 CPLEX 預設）
- [ ] experiment 期間 LP / MPS / Sol export 全關（計時不含檔案 I/O）
- [ ] ★ 每輪確認 solver log **無 dynamic search 停用 warning**
- [ ] 發現契約可能訂錯時，只記成 finding 回報，**未自行變更**

## R0 校準（S2，硬 gate）

- [ ] ★ 執行 R0 前已刪 `bin/.../Experiments/<Project>-tuning-r0.*`（Phase 2 驗證管線時跑過，同名是 append 不是覆寫）
- [ ] ★ **R0 已完成，θ 已算出確切值**並記錄（非「≥ 某值」的下限），**單位與主指標一致**（A / C 是比值，B 是絕對百分點）
- [ ] 瓶頸剖面已從 `-trajectory.csv` 分類（No-incumbent / Dual-bound / Primal-search / Node-cost / 數值不穩 / Variability-dominated）
- [ ] R0 的 K 個 seed 通過 §0.1.1 對應那一套不變式（A 嚴格相等；B / C 不退步 + bound 不越線）
- [ ] 情境 C 的「K 個 seed 全部無解」**沒有被誤當成早停條件**（那是本輪要打的目標）
- [ ] 已指定 3 個 holdout seeds，且全程未參與調參
- [ ] 契約健檢探針（`MipGap = 0`）已跑並記成 finding，未進排名；情境 B / C 若放大了 `TimeLimit`，倍數已記錄
- [ ] 總預算已估算並記錄

## 實驗設計（S3）

- [ ] 以現行 production baseline 為起點，variants 全部用 `Clone()` 產生
- [ ] ★ **一輪只改一個旋鈕**
- [ ] ★ 候選**只從剖面對應那一類**取，沒有查全表盲掃
- [ ] ★ `Seed` 是共同因子，**未當成 variant 排名**
- [ ] 所有 trial 共用同一份已載入的 `data`，載入後未被修改
- [ ] experiment 名帶輪次（`<Project>-tuning-r<N>`），與歷史不重名
- [ ] 有 warm-up 排除 / 順序輪替

## 每輪 archive 逐項驗收（S3 每輪結束、進入裁決前）

> **沒有腳本可跑——逐項自己開檔比對。**
> 每一列都寫了「怎麼查」與「期望值」；查不到、對不上一律 FAIL，FAIL 未修正前**不得 promotion、不得開始下一輪**。
> `<P>` = 專案名，`<N>` = 本輪輪次，`<base>` = `<P>-tuning-r<N>`。

**A. 檔案齊備**

- [ ] 專案根有 `Experiments/`、`TuningHistory.md`、`Program.cs` 三者。
- [ ] `Experiments/` 下本輪的檔齊全且檔名逐字正確：`<base>.csv`（主表，一列一 trial）、`<base>-meta.csv`（說明檔，放模型大小/環境/基準完整設定）、`<base>.json`（累積與重讀用）。`<base>-trajectory.csv` **只有在真的收集到收斂軌跡時才會產生**——純 LP 或求解太快時沒有，這是正常的，看主表的 `TrajectoryPoints` 欄是不是 0 就知道。
- [ ] `Experiments/` 下**每一個** `<P>-tuning-r*.json` 都符合 `<P>-tuning-r<數字>.json`；沒有 `test`、`exp1`、`tmp` 這類命名。
- [ ] 歷史輪次的 archive 仍在（每輪永久保留，NEVER 因為「舊的沒用了」刪除）。

**B. 三處交叉引用對得上**

- [ ] `Program.cs` 的 exp 分支有本輪 marker，**整行逐字**是 `// R<N> — <base>`（破折號是 `—`，不是 `-`）。
- [ ] `TuningHistory.md` 有 `## R<N>` 標題（行首、`##` 一級）。
- [ ] 該節內文有引用 archive 路徑字串 `Experiments/<base>.json`。

**C. `TUNING-FACTS` 與 archive JSON 逐欄比對**（§6.2.2）

- [ ] `TuningHistory.md` 的 R<N> 節有成對的 `<!-- TUNING-FACTS:R<N>:BEGIN -->` / `:END -->`，中間是可解析的 ```json``` 區塊。
- [ ] facts 的 `experiment` 值**逐字**等於 `<base>`（大小寫敏感）。
- [ ] facts 的 `archive.csv` / `archive.json` / `archive.trajectory` 三個路徑指向 A 節那三個實際存在的檔。
- [ ] facts 的 `trials` 陣列長度 **等於** archive JSON `trials` 陣列長度。
- [ ] ★ **逐個 trial、逐個欄位對照** archive JSON（順序相同，一個都不能跳）：

  | facts 欄位 | 在 archive JSON 的來源 | 比對方式 |
  | --- | --- | --- |
  | `label` | `trials[i].label` | 逐字相同 |
  | `seed` | `trials[i].config.tunable.Seed`（找不到再看 `config.solverSpecific.Seed`） | 數值相同 |
  | `status` | `trials[i].metrics.status` | 逐字相同 |
  | `objectiveValue` | `trials[i].metrics.objectiveValue` | 逐字相同（含 `NaN`，NEVER 改寫成 0） |
  | `bestBound` | `trials[i].metrics.bestBound` | 同上 |
  | `mipGap` | `trials[i].metrics.mipGap` | 同上 |
  | `runTimeMs` | `trials[i].metrics.runTimeMs` | 同上 |
  | `configDiffFromBaseline` | `trials[i].config` 與本輪 baseline config 的差集 | 只列真正不同的欄位，**Seed 除外**（seed 是重複量測條件，不是策略差異） |

- [ ] 對照時發現任何一格不符 → **先修 facts 或重出 archive**，NEVER 用敘事文字掩蓋，也 NEVER 手改 archive 檔。

**D. label 與跨輪去重**

- [ ] archive JSON 每個 `trials[i].label` 都以 `| r<N>-` 開頭的 config 段結尾（本輪前綴正確，沒有沿用上一輪的 `r<N-1>-`）。
- [ ] ★ 把本輪每個 **非 baseline、非 replica** candidate 的 `configDiffFromBaseline` 拿去比對 `TuningHistory.md` 的**已否證清單**與歷史各輪的 facts：**不得與任何歷史 candidate 的有效設定完全相同**（忽略 Seed 後仍相同 = 重跑舊實驗）。
- [ ] label 含 `-replica-of-r<M>` 的 replica candidate，History 本輪節**明確寫了為什麼要重跑**（replication 理由）；沒寫理由的 replica 視為重複實驗。

**E. 數字只能從 facts 來**

- [ ] 本輪敘事、彙總表與裁決引用的每個 Trial 數字，都能指回 `TUNING-FACTS` 的某個 label／欄位。
- [ ] 聚合值（`sgm`、PAR10、平均 endGap、θ）有寫明「用了哪幾個 facts + 哪個公式」，讀者照著能自己算一次。
- [ ] 沒有任何數字是憑印象、憑對話記憶或憑 `bin/` 裡已被清掉的檔寫出來的。

## 判定與證據

- [ ] 每個 Trial 都有 `Status`、objective、`MipGap`、runtime（`TimeLimit` 的 trial 數值欄為 `NaN` 是正常的）
- [ ] ★ **未「只憑」`NodeCount` / `IterationCount` 做判定** —— 自 2026-08-25 起框架會填實際值，但 node 少不等於快（實測 `VariableSelect=3` node 更多且慢 4 倍、`Emphasis=3` node 更少也慢 2.4 倍），MUST 與 runtime 一起判讀
- [ ] ★ **未把 `NaN` 的 objective / `MipGap` 當 0 參與彙總**
- [ ] ★ 排名用的是**該情境的主指標**，且用了 §4.3 指定的彙總法；已寫明方法與計算值
- [ ] ★ 情境 B / C **沒有拿 runtime 排名**（全部跑滿時限，排出來必然全部平手）
- [ ] ★ 情境 C **沒有把「無可行解」當淘汰理由**（那會連 baseline 一起淘汰光）
- [ ] ★ champion 的勝幅**超過 θ**（差距在雜訊內視同平手，不 promote）
- [ ] ★ 每個 trial 都通過 §0.1.1 不變式；違反者已淘汰並記錄理由
- [ ] 每個被淘汰的 candidate 都寫了淘汰理由
- [ ] 紀錄不只存在 `bin/Experiments`（會被清掉）

## Hold-out（S4）

- [ ] ★ champion 已用 3 個未參與調參的 seed 重跑
- [ ] ★ holdout **只用來估計，未用來挑選 config**
- [ ] holdout 上**主指標**改善消失者已退回 retain 並記為 over-tuning
- [ ] 情境 C：holdout 的 3 個 seed **各自都找到 incumbent**（只在 tuning seed 上找得到 = over-tuning）

## Promotion 閉環（S5）

- [ ] champion 的**完整設定**已明確寫回 `Program.cs` 的 production baseline
- [ ] baseline 上方 provenance 註解已更新（來源 experiment、Trial label、日期、config diff）
- [ ] `TuningHistory.md` 已追加本輪紀錄（**先寫紀錄再驗證**）
- [ ] promotion 後**重新 build + 跑無參數 production**（export 開回來），通過驗收
- [ ] ★ production 通過該情境的 PASS 條件（A = objective 等於 `phase2Objective`；B = objective 不差於基線且 endGap 改善；C = 確實產出 incumbent）；不通過則已撤銷 promotion 並記 `rejected`
- [ ] 情境 C promotion PASS 後已升級為情境 B：重跑 R0 重定主指標與 θ，並記「情境轉換」分隔線
- [ ] 若結論是保留原 baseline：`TuningHistory.md` 一樣記了 retain 決策與證據

## 紀錄格式

- [ ] 每輪四段齊全：假設 / 預測 / 實測 / 裁決
- [ ] ★ **「預測」是在跑實驗之前寫的**，不是事後補的
- [ ] 「已否證」清單跨輪累積，未重置
- [ ] 歷史節未被改寫，只有追加

## 一致性（越界檢查）

- [ ] ★ `git diff --name-only` 只有 `Program.cs`、`TuningHistory.md`（+ `status.json`）
- [ ] ★ `Program.cs` 內部 diff **只有** baseline 值、provenance 註解、exp 分支 variant 定義 —— model chain 未出現在 diff
- [ ] `Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Solution/`、`Model.md`、csproj 全程未被改動
- [ ] 沒有以 tuning 名義移項 / 改號 / 翻轉方向 / 四捨五入 / 改輸入精度
- [ ] 沒有為了「讓它有解」而加 soft constraint 或 penalty

## 停損與交付

- [ ] 停止原因明確對應規範 §7.1 的 A–I 其中一條，並已寫進回報
- [ ] 連續 3 輪**主指標**無實質改善時已停止，沒有自行升級去動模型結構
- [ ] 情境 C 候選耗盡仍無 incumbent（條件 I）時，**沒有自行加大 `TimeLimit` 交差**——已把放寬契約 / 改模型當建議交還使用者
- [ ] 回報含：進場情境與主指標、輪次數、每輪一行摘要、θ 與剖面、champion 或 retain + 理由、停止原因、production 驗證結果
- [ ] `status.json` 已更新（`tuningRound` / `productionBaseline` / `promotionVerified` 等），未覆寫其他階段欄位
