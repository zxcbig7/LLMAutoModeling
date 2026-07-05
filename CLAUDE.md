# AI Modeling — Claude Code 入口（router）

> 本 repo 的操作規範**單一來源**在 [`AGENTS.md`](AGENTS.md) → [`interactive/`](interactive/) / [`automated/`](automated/)。
> 本檔只做導引與最硬的天條；細則一律以那些檔為準，不在此重複。

## 這是什麼

自然語言最佳化題目 → 可求解的 OptimFoundation CPLEX C# 專案。兩條路線：

| 路線 | 位置 | 何時用 |
| --- | --- | --- |
| **interactive**（有 gate，預設） | [`interactive/README.md`](interactive/README.md) | 一般開發：Modeling → Coding → Tuning 三階段 phase gate |
| **automated**（全自動 16-stage） | [`automated/CLAUDE.md`](automated/CLAUDE.md) | 大量量產 / 不需人在迴路：`00 → 14` 一路生到底 |

**先讀 [`AGENTS.md`](AGENTS.md)**，再依當前 phase 讀對應細則。API 簽名權威來源：[`CPLEX_API_REFERENCE.md`](CPLEX_API_REFERENCE.md)。

## 換機器設置（clone 後唯一要做的事）

DLL 不進版控（商用 CPLEX + 建置產物）。clone 後在 repo 根跑一次：

```powershell
powershell -File scripts/setup-dlls.ps1
```

自動偵測本機 CPLEX 安裝與 sibling `../OptimFoundation/` 建置輸出，把 6 個 DLL 就位。細節與手動步驟見 [`dlls/README.md`](dlls/README.md)。

## 天條（最硬的，細則見 interactive/README.md）

- NEVER 模型未經使用者確認就產任何 `.cs`（interactive 路線）
- NEVER 移項 / 改號 / 翻轉比較方向 / 四捨五入數值
- NEVER 在 Constraint / Objective 出現裸數字（一律 Parameter 的 QTY）
- NEVER 呼叫 `CPLEX_API_REFERENCE.md` 沒列的 API（憑記憶發明 API）
- NEVER 改 OptimFoundation 框架本體（唯讀）——擴充在專案端寫 helper
- NEVER 用絕對路徑（`C:/Users/...`）——ALWAYS 相對本 repo
