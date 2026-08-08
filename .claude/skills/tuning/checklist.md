# tuning · 交付前自檢

> 規則本體在 `.claude/workflows/interactive/phase-3-tuning.md` 與 `.claude/rules/AGENTS.md`。
> 本檔只是交付前的勾選面。**沒有實驗證據的項目不准打勾。**

## 進場 gate

- [ ] 使用者主動提出調校需求（不是我自己起意）
- [ ] `dotnet build` 通過
- [ ] `Status == Optimal`（或該題預期的合法狀態）—— feasible 是前提，不是本階段的工作
- [ ] `coding` 的解驗證協定四步已重新確認過（不只是看 `status.json`）

## 範圍界線

- [ ] 這輪確實是「模型與資料固定、只嫌慢」的情境
- [ ] 要換資料 / 改結構的訴求已被擋在門外並退回對應 phase，不是在本階段順手做掉
- [ ] `Infeasible` / `Unbounded` 沒有被當成 tuning 題目處理（已附 IIS 或缺界證據退回）

## 實驗設計

- [ ] 以現行 production baseline 為起點，variants 全部用 `Clone()` 產生
- [ ] **一輪只改一個旋鈕**，多旋鈕比較 = 多個 variant
- [ ] 所有 trial 共用同一份已載入的 `data`，載入後未被修改
- [ ] experiment 名帶輪次（`<project>-tuning-r<N>`）
- [ ] 需要穩健結論時有 warm-up trial / 多 seed / 輪替順序

## 證據

- [ ] 每個 Trial 都有 `Status`、objective、`MipGap`、時間、節點數
- [ ] before / after 對照表已整理成可讀的形式
- [ ] champion 的勝幅超出 performance variability（差距在雜訊內視同平手，不 promote）
- [ ] 紀錄不只存在 `bin/Experiments`（會被清掉）

## Promotion 閉環

- [ ] champion 的**完整設定**已明確寫回 `Program.cs` 的 production baseline
- [ ] baseline 上方 provenance 註解已更新（來源 experiment、Trial label、config diff）
- [ ] `TuningHistory.md` 已追加本輪紀錄（含證據與決策理由）
- [ ] promotion 後**重新 build + 跑無參數 production**，通過驗收
- [ ] 若結論是保留原 baseline：`TuningHistory.md` 一樣記了 retain 決策與證據

## 一致性

- [ ] 沒有以 tuning 名義移項 / 改號 / 翻轉方向 / 四捨五入 / 改輸入精度
- [ ] `Data/*.csv`、`Dataload`、`Constraint_*`、`Objective`、`Model.md` 全程未被改動
- [ ] 沒有為了「讓它有解」而加 soft constraint 或 penalty
- [ ] `git diff` 只有 `Program.cs` 與 `TuningHistory.md` 兩個檔

## 停損

- [ ] 連續 3 輪無實質改善 → 已停止並回報，沒有自行升級去動模型結構

## 交付

- [ ] 回報含 variants 清單、before/after、champion 或 retain 決策 + 理由、production 驗證結果
- [ ] `status.json` 已更新（`tuningRound` / `productionBaseline` / `promotionVerified`）
