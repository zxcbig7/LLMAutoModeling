# AI Modeling — Claude Code 入口（router）

> 本 repo 的操作規範**單一來源**在 [`AGENTS.md`](AGENTS.md) → [`interactive/`](interactive/) / [`automated/`](automated/)。
> 本檔只做導引（router）；**天條與細則一律以 AGENTS.md 為準，不在此重複**。

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

## 天條

全部天條（含數值保真、API 白名單、框架唯讀、相對路徑、DLL 引用規則）唯一權威在 [`AGENTS.md`](AGENTS.md#天條全流程通用唯一權威在本檔其他文件只引用不重複)。動手前先讀。
