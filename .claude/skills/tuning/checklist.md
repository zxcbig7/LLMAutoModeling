# tuning · 交付前自檢

> 規則本體在 `.claude/rules/Ph3_Tuning/solver-tuning-guide.md` 與 `.claude/rules/AGENTS.md`。
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

## 範圍界線

- [ ] 這輪確實是「模型與資料固定、只嫌慢或收斂不了」的情境
- [ ] 要換資料 / 改結構的訴求已被擋在門外並退回對應 phase
- [ ] `Infeasible` / `Unbounded` 沒有被當成 tuning 題目處理（已附 IIS 或缺界證據退回）
- [ ] 沒有為了「讓它變 `Optimal`」而放寬 `MipGap` 或加大 `TimeLimit`（那是動停止契約，要使用者拍板）

## 契約與環境（S0 / S1）

- [ ] 契約區塊已寫進 `TuningHistory.md` 開頭：停止契約 / 環境契約 / 量測契約 / Phase 2 基線
- [ ] ★ **契約旋鈕**（`MipGap` `TimeLimit` `NodeLimit` `IntegerSolutionLimit` 容差）整期固定，**未進 variant 池**
- [ ] ★ **環境旋鈕**（`Threads` `ParallelMode` `MemoryLimitMb` `NodeFileStrategy`）已由 S1 sizing 定版並凍結
- [ ] `ParallelMode = 1` 已明設（不依賴 CPLEX 預設）
- [ ] experiment 期間 LP / MPS / Sol export 全關（計時不含檔案 I/O）
- [ ] ★ 每輪確認 solver log **無 dynamic search 停用 warning**
- [ ] 發現契約可能訂錯時，只記成 finding 回報，**未自行變更**

## R0 校準（S2，硬 gate）

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

## 判定與證據

- [ ] 每個 Trial 都有 `Status`、objective、`MipGap`、runtime（`TimeLimit` 的 trial 數值欄為 `NaN` 是正常的）
- [ ] ★ **未使用 `NodeCount` / `IterationCount` 做判定**（框架不填，值為空）
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
