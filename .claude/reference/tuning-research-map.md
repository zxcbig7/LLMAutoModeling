# Solver Tuning 研究地圖（reference，非規範）

**這一節是文獻地圖，不是規則。** 何時讀：§2 的手動旋鈕掃完仍不達標、要導入自動調參工具、要寫報告引用文獻時。

## C.1 領域名稱

這件事在學界叫 **Algorithm Configuration (AC)**，不是 "parameter tuning"——搜文獻用 AC 才找得到主線。CPLEX 是 AC 領域的標準實驗對象（159 個 user-specifiable parameters），所以「CPLEX 專屬」的研究比想像中多。

| 線 | 問句 | 產出 | 可用度 |
| --- | --- | --- | --- |
| **A. 手動調參方法論** | 看 log 該動哪個旋鈕？ | 診斷 decision tree | ★★★ 直接可用（§2 就是這條線） |
| **B. 自動調參（offline）** | 給一組 instance，最佳參數組是什麼？ | configurator 工具 | ★★☆ 需 instance set |
| **C. Per-instance 配置** | 給「這個」instance 該用什麼參數？ | ML 模型 | ★☆☆ 需訓練資料 |
| **D. 學習取代 solver 內部決策** | branching / cut 規則能不能學？ | 研究原型 | ☆☆☆ 幾乎都綁 SCIP，CPLEX callback 開放度不足，**讀來理解方向、不是可導入的技術** |

## C.2 可用工具

| 工具 | 方法 | 介面 | 備註 |
| --- | --- | --- | --- |
| **CPLEX 內建 tune** | 內部啟發式 | **Interactive Optimizer 讀 `.lp`（§3.6）** | **零成本、不改程式、MUST 先跑**。框架未封裝 `TuneParam`，但 `ExportLP` 產出的 `.lp` 可直接餵給 Interactive Optimizer。**單模型可用 `TuningRepeat` 做 permutation 穩健化**，正好補上單 instance 缺樣本的洞 |
| **irace** | racing + F-race | R，包 CLI wrapper | 最好上手，統計上有 racing 早停 |
| **SMAC3** | Bayesian optimization | Python，包 CLI wrapper | 目前主流，樣本效率最好 |
| ParamILS | iterated local search | Ruby/Perl 老工具 | 歷史意義為主，新專案別選 |
| GGA / GGA+ | gender-based GA | — | 平行度高時有優勢 |
| MPILS | ILS + 統計剪枝 | 論文原型 | 方法可借鑑，未見公開釋出 |

這些 configurator 都是**黑箱包 solver**：只要能用命令列跑一次求解並吐出時間 / gap 就能接。本框架的 `dotnet run -- exp` 已具備這個介面形狀。

**NEVER 一開始就丟幾十個參數給 configurator**——搜索空間爆炸、樣本不夠、結論全是噪音。從小池子開始、動態擴充。

## C.3 落地順序

| 階段 | 做什麼 | 對應本檔 |
| --- | --- | --- |
| 0 | 過正確性 gate | §0.0 |
| 1 | 手動診斷 | §2 |
| 2 | baseline：default 各 3–5 seed，量出 variability | §3.4 |
| 3 | 手動掃 10–20 個旋鈕的少數 variant，一次一個 | §3 |
| 4 | 仍不達標才上 irace / SMAC3 包 `dotnet run -- exp` | 需新寫 CLI wrapper |
| 5 | hold-out instance 驗收，報 shifted geometric mean | §4.3 |

## C.4 引用清單

**手動方法論（A 線）**

- Klotz & Newman, *Practical guidelines for solving difficult mixed integer linear programs*, Surveys in OR & Mgmt Sci 18(1), 2013 — https://www.sciencedirect.com/science/article/abs/pii/S1876735413000020
  （Ed Klotz 是 CPLEX 開發者，這篇等於官方版調參 SOP；§2.1 的症狀表出自這裡）
- Klotz & Newman, *Practical guidelines for solving difficult linear programs*, 2012 — https://people.mines.edu/anewman/wp-content/uploads/sites/158/2019/11/27-LP_practice123112.pdf
- 同作者的 ill-conditioning / numerical instability 專篇，INFORMS TutORials 2014（係數量級跨度過大時必讀）

**自動調參（B 線）**

- Hutter, Hoos, Leyton-Brown, *Automated Configuration of MIP Solvers*, CPAIOR 2010 — https://ml.informatik.uni-freiburg.de/wp-content/uploads/papers/10-CPAIOR-MIP-Config.pdf
  （用 ParamILS 調 CPLEX 76 個參數，特定 instance family 最高 ~52x 加速，且打贏 CPLEX 內建 tuning tool）
- Himmich et al., *MPILS: An Automatic Tuner for MILP Solvers*, C&OR 2023 — https://www.sciencedirect.com/science/article/abs/pii/S0305054823002083
  （維持小參數池的 tuning → learning → evaluation 三步循環；最貼近「一組固定業務問題」的工業情境）

**Per-instance 配置（C 線）**

- Iommazzo et al., *Learning to Configure Mathematical Programming Solvers by Mathematical Programming* — https://arxiv.org/pdf/2401.05041
- *Instance-wise algorithm configuration with graph neural networks* — https://arxiv.org/pdf/2202.04910
- *Automatic MILP Solver Configuration By Learning Problem Similarities* — https://arxiv.org/pdf/2307.00670
- *The Algorithm Configuration Problem*（形式化 survey） — https://arxiv.org/pdf/2403.00898

**ML for solvers（D 線）**

- *Machine Learning Algorithms for Improving Exact Classical Solvers*（2012–2025 survey） — https://arxiv.org/pdf/2508.06906
- awesome-ml4co 論文清單 — https://github.com/Thinklab-SJTU/awesome-ml4co

**實驗方法論（必讀，§3.4 與 §4.3 的依據）**

- Lodi & Tramontani, *Performance Variability in Mixed-Integer Programming*, INFORMS TutORials 2013 — https://pubsonline.informs.org/doi/abs/10.1287/educ.2013.0112
- Danna, *Performance variability in mixed integer programming*, MIP 2008 — https://coral.ise.lehigh.edu/mip-2008/talks/danna.pdf
- Eggensperger et al., *Pitfalls and Best Practices in Algorithm Configuration* — https://arxiv.org/pdf/1705.06058

> LLM 相關研究（OR-LLM-Agent 等）目前集中在**自然語言 → 模型 / 程式碼生成**，不是 solver 參數配置；別把它誤當成本主題的解法。
