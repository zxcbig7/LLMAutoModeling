# Solver Tuning 研究地圖 — CPLEX 參數調校的文獻與方法論

<system_context>
「CPLEX 引擎怎麼調」的學界研究整理（2026-08 盤點）。定位是 **reference，不是規範**——
操作規範看 `Foundation Tuning/CLAUDE.md`（範圍界線、Experiment API、stop conditions），
本檔回答的是「這件事在學界叫什麼、有哪些方法、實驗怎麼做才不會自欺」。

何時讀：Phase 3 手動旋鈕掃完仍不達標、要導入自動調參工具、要寫論文/報告引用文獻時。
</system_context>

<critical_notes>
- 這領域的正式名稱是 **Algorithm Configuration (AC)**，不是 "parameter tuning"——搜文獻用 AC 才找得到主線
- CPLEX 是 AC 領域的標準實驗對象（159 個 user-specifiable parameters），所以「CPLEX 專屬」的研究比想像中多
- MUST 任何 tuning 實驗前先認識 **performance variability**：換機器、置換 row/column 順序、換 random seed 都可能讓求解時間差數倍，根因是 branch-and-cut 的 imperfect tie-breaking —— Why: 只跑一個 seed 就宣稱「這個參數有效」，結論通常是噪音而非效果
- MUST train / test instance 分開，否則得到的是 **over-tuning**（在訓練 instance 上漂亮、換一批就崩）
- 匯總多個 instance 的時間 ALWAYS 用 **shifted geometric mean**，NEVER 用算術平均 —— Why: 算術平均被少數慢 instance 綁架，快 instance 的改善看不見
</critical_notes>

<file_map>
本檔 - 文獻地圖（四條研究線 + 工具 + 實驗方法論）
Foundation Tuning/CLAUDE.md - Phase 3 操作規範（實際要調的時候讀這個）
Model Design/linearization-patterns.md - 結構層手段的數學基礎（tighten Big-M / valid inequalities）
</file_map>

<patterns>
## 四條研究線

| 線 | 問句 | 產出 | 對我們的可用度 |
| --- | --- | --- | --- |
| A. 手動調參方法論 | 看 log 該動哪個旋鈕？ | 診斷 decision tree | ★★★ 直接可用 |
| B. 自動調參（offline） | 給一組 instance，最佳參數組是什麼？ | configurator 工具 | ★★☆ 需 instance set |
| C. Per-instance 配置 | 給「這個」instance，該用什麼參數？ | ML 模型 | ★☆☆ 需訓練資料 |
| D. 學習取代 solver 內部決策 | branching / cut 規則能不能學？ | 研究原型 | ☆☆☆ CPLEX 難落地 |

### A. 手動調參方法論（CP 值最高）

**Klotz & Newman (2013), "Practical guidelines for solving difficult MILP"**，*Surveys in OR & Management Science* 18(1):18–32。Ed Klotz 是 CPLEX 開發者，這篇等於官方版的調參 SOP。

核心是「**先診斷瓶頸、再對應旋鈕**」，不是盲搜：

| log 症狀 | 判斷 | 對應方向 |
| --- | --- | --- |
| LP relaxation 與最佳解差距大 | bound 太弱 | cuts、tighten Big-M、valid inequalities |
| 一直找不到可行解 | primal 側弱 | `MIPEmphasis=1`（重可行解）、heuristics、warm start |
| node 數暴衝但 gap 不動 | 搜索無方向 | branching 策略、`Probe`、symmetry breaking |
| node throughput 低（每秒處理 node 少） | 單 node 太貴 | 關掉部分 cuts、`NodeSelect`、記憶體/node file |
| 數值警告、解不穩定 | ill-conditioning | `NumericalEmphasis`、係數 scaling（見下方鐵則） |

同作者另有 LP 版與 **ill-conditioning / numerical instability** 專篇（INFORMS TutORials 2014），係數量級跨度過大（如 Big-M 用 1e9）時必讀。

> 這條線與 `Foundation Tuning/CLAUDE.md` 的「症狀 → 動作」表是同一套邏輯，本檔提供的是它背後的出處與更細的診斷維度。

### B. 自動調參（offline，針對一整組 instance）

**里程碑**：Hutter, Hoos, Leyton-Brown, *Automated Configuration of Mixed Integer Programming Solvers*（CPAIOR 2010）。用 ParamILS 調 CPLEX 76 個參數，特定 instance family 最高 **~52x** 加速，且**打贏 CPLEX 內建的 tuning tool**。所有後續工作的 baseline。

**MPILS (2023)**，*Computers & Operations Research*（Himmich, Er Raqabi, El Hachemi, El Hallaoui, Metrane, Soumis）。針對前者「搜索空間太大」的改良：不搜整個空間，維持一個小參數池，三步循環——

1. **Tuning**：在當前池上跑 iterated local search
2. **Learning**：統計學習剪掉明顯變差的參數值
3. **Evaluation**：把新的有潛力參數插進池子

用 MIPLIB + 一個真實大規模工業問題驗證，明確以 CPLEX 為對象。**這是目前最貼近「工業界要調一組固定業務問題」情境的方法。**

### C. Per-instance / instance-specific configuration

給每個 instance 各配一組參數，而不是全體共用一組。

- **Iommazzo, D'Ambrosio, Frangioni, Liberti**, *Learning to Configure MP Solvers by MP*（LION 2020；完整版 arXiv 2401.05041）。兩階段：先學 (instance feature, config) → performance 的映射，再把「參數之間的相依/一致性限制」寫成**顯式的最佳化問題**求解。關鍵洞見：一般監督式學習吐不出滿足 hard constraint 的參數組合（例如某些旋鈕組合互斥），所以要用 MP 收尾。
- **GNN-based instance-wise AC**（arXiv 2202.04910）：用圖神經網路吃 MILP 的 bipartite graph 表示直接預測配置。
- **Learning Problem Similarities**（arXiv 2307.00670）：靠 instance 相似度做配置遷移。
- 祖宗輩：**ISAC**（先 cluster 再 per-cluster 配置）、**Hydra-MIP**（portfolio + 配置）。

### D. 學習取代 solver 內部決策

不是調參數，而是換掉 branching / cut selection / node selection / primal heuristic scheduling 的規則本身。入門看 Bengio–Lodi–Prouvost 的 methodological tour d'horizon；2025 有涵蓋 2012–2025 的新 survey（arXiv 2508.06906）。

**實務警告**：這條線幾乎都綁 SCIP（Ecole 生態），因為 CPLEX callback 開放度不足以插入學到的規則。對我們是**讀來理解方向，不是可導入的技術**。

## 可用工具對照

| 工具 | 方法 | 語言/介面 | 備註 |
| --- | --- | --- | --- |
| CPLEX 內建 tune | 內部啟發式 | CPLEX 原生（`tune` / tuning callback） | 零成本，MUST 當 baseline 先跑；學界普遍打得過它 |
| **irace** | racing + F-race | R，包 CLI wrapper | 最好上手，統計上有 racing 早停 |
| **SMAC3** | Bayesian optimization | Python，包 CLI wrapper | 目前主流，樣本效率最好 |
| ParamILS | iterated local search | Ruby/Perl 老工具 | 歷史意義為主，新專案別選 |
| GGA / GGA+ | gender-based GA | — | 平行度高時有優勢 |
| MPILS | ILS + 統計剪枝 | 論文原型 | 方法可借鑑，未見公開釋出 |

> 這些 configurator 都是**黑箱包 solver**：只要能用命令列跑一次求解並吐出時間/gap，就能接。我們的 `dotnet run -- experiment` 已具備這個介面形狀。
</patterns>

<workflow>
## 實驗方法論鐵則（做之前先立規矩）

1. **確認 instance family 同質**：MPILS / AC 全系列的前提都是「同一類問題」。把差異極大的 instance 混在一起調，結果沒有意義。
2. **建 baseline**：CPLEX default vs 內建 `tune`，各跑 3–5 個 random seed。**這一步就會讓你看到 variability 的量級**——如果 default 自己的 seed 之間就差 2x，那任何小於 2x 的「改善」都不算數。
3. **train / test 切開**：instance 分兩堆，調參只看 train，最終數字報 test。
4. **一次只動一個旋鈕**（掃描階段），且 `verbose: false` 關 log I/O。
5. **指標**：shifted geometric mean of runtime（shift 常用 1s 或 10s）；timeout 用 PAR10（罰 10 倍時限）之類的明確規則，NEVER 把 timeout 當成「剛好等於時限」。
6. **可重現**：固定 `randomSeed` + `parallelMode = 1` + 用 `detTimeLimit`（determinstic ticks）而非牆鐘時間當停止條件。

## 落地到 OptimFoundation 的建議順序

| 階段 | 做什麼 | 對應既有機制 |
| --- | --- | --- |
| 0 | 過正確性 gate | `Foundation Coding` 解驗證協定 |
| 1 | 手動診斷（Klotz & Newman 症狀表） | `Foundation Tuning` 範圍界線 + solver 決策表 |
| 2 | baseline：default vs `tune`，各 3–5 seed | Experiment API（`Trial.Capture` + `Experiment.Save`） |
| 3 | 手動掃 10–20 個旋鈕的少數 variant | 同上，一次一旋鈕 |
| 4 | 仍不達標才上 irace / SMAC3 包 `dotnet run -- experiment` | 需新寫 CLI wrapper |
| 5 | hold-out instance 驗收，報 shifted geomean | — |

**NEVER 一開始就丟 76 個參數給 configurator**——搜索空間爆炸、樣本不夠、結論全是噪音。MPILS 那篇的整個貢獻就是在講「從小池子開始，動態擴充」。
</workflow>

<hatch>
- 只有單一 instance（不是一個 family）→ 自動調參沒有統計基礎，只走 A 線手動診斷 + Experiment API 記錄
- 問題規模小到秒解 → 不要 tuning，變異數比訊號大
- LLM 相關研究（OR-LLM-Agent 等）目前集中在**自然語言 → 模型/程式碼生成**，不是 solver 參數配置；別把它誤當成本主題的解法
</hatch>

<example>
## 引用清單

**手動方法論（A）**
- Klotz & Newman, *Practical guidelines for solving difficult mixed integer linear programs*, Surveys in OR & Mgmt Sci 18(1), 2013 — https://www.sciencedirect.com/science/article/abs/pii/S1876735413000020
- Klotz & Newman, *Practical guidelines for solving difficult linear programs*, 2012 — https://people.mines.edu/anewman/wp-content/uploads/sites/158/2019/11/27-LP_practice123112.pdf

**自動調參（B）**
- Hutter, Hoos, Leyton-Brown, *Automated Configuration of MIP Solvers*, CPAIOR 2010 — https://ml.informatik.uni-freiburg.de/wp-content/uploads/papers/10-CPAIOR-MIP-Config.pdf ／ 專案頁 https://www.cs.ubc.ca/labs/algorithms/Projects/MIP-Config/
- Himmich et al., *MPILS: An Automatic Tuner for MILP Solvers*, C&OR 2023 — https://www.sciencedirect.com/science/article/abs/pii/S0305054823002083

**Per-instance 配置（C）**
- Iommazzo et al., *Learning to Configure Mathematical Programming Solvers by Mathematical Programming* — https://arxiv.org/pdf/2401.05041
- *Instance-wise algorithm configuration with graph neural networks* — https://arxiv.org/pdf/2202.04910
- *Automatic MILP Solver Configuration By Learning Problem Similarities* — https://arxiv.org/pdf/2307.00670
- *The Algorithm Configuration Problem*（形式化 survey） — https://arxiv.org/pdf/2403.00898

**ML for solvers（D）**
- *Machine Learning Algorithms for Improving Exact Classical Solvers*（2012–2025 survey） — https://arxiv.org/pdf/2508.06906
- awesome-ml4co 論文清單 — https://github.com/Thinklab-SJTU/awesome-ml4co

**實驗方法論（必讀）**
- Lodi & Tramontani, *Performance Variability in Mixed-Integer Programming*, INFORMS TutORials 2013 — https://pubsonline.informs.org/doi/abs/10.1287/educ.2013.0112
- Danna, *Performance variability in mixed integer programming*, MIP 2008 — https://coral.ise.lehigh.edu/mip-2008/talks/danna.pdf
- Eggensperger et al., *Pitfalls and Best Practices in Algorithm Configuration* — https://arxiv.org/pdf/1705.06058
</example>

<fatal_implications>
- NEVER 未過正確性 gate 就談 tuning 研究（調快一個錯的模型毫無意義）
- NEVER 單一 seed 單次執行就宣稱參數有效
- NEVER 把 train instance 的改善數字當成交付結論
- NEVER 因為讀了本檔就跳過 `Foundation Tuning/CLAUDE.md` 的範圍界線判斷——文獻是地圖，規範才是路
- NEVER 把 D 線（學習取代 solver 內部決策）或模型層 reformulation 當成 Phase 3 的手段——本階段模型凍結，那些是回 Phase 1 才談的事
</fatal_implications>
