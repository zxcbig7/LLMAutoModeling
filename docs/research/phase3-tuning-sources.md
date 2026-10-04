# Phase 3 調參研究：CPLEX 官方依據與適用邊界

研究日期：2026-09-19。本文是框架研究附錄，沒有載入模型、執行 solver 或調整 production 參數。

版本界線：框架盤點回報 `dlls/ILOG.CPLEX.dll` metadata 為 `22.1.1.0`；這不是本輪實際 runtime 版本確認。本文引用 IBM 文件的 22.1.0、22.1.1、22.1.2 版本，各來源明列版本。落地時仍須核對實際 CPLEX runtime、API 與 OptimFoundation 白名單，不能把網頁版本當本機版本。

標記定義：**事實**有直接官方來源；**推論**是根據事實得到的限制；**建議**是要加入框架的工程規則，並非 IBM 規定的通用門檻。

## 1. 大模型要交給工具計算摘要，不能灌入 LLM context

**事實**：IBM 為大到不適合顯示全文的問題提供 Interactive Optimizer 的 `display problem stats`；可取得 rows、columns、nonzeros、變數/限制式類型及數值範圍。也能按指定範圍顯示 constraint、name、bound。此命令針對已在 CPLEX session 的模型；不是直接對磁碟文字檔做免費摘要。[IBM 22.1.2：Displaying problem statistics](https://www.ibm.com/docs/en/icos/22.1.2?topic=problem-displaying-statistics)

**事實**：SAV 是 CPLEX binary 格式，保留平台精度，對重複讀寫模型有效率優勢；不是文字檔。若已求解且存在 basis，SAV 可能包含 basis。[IBM 22.1.2：SAV file format](https://www.ibm.com/docs/en/icos/22.1.2?topic=cplex-sav-file-format-numerically-accurate-binary-files)

**推論**：模型完整載入 solver 的 RAM，和模型全文送入 LLM context，是兩種不同成本。禁止後者不代表前者零成本；20,000,000 行不能直接換算 solver 所需 RAM，也不能直接代表 rows 或 nonzeros。SAV 可用於重現，但不能假設更換檔案格式後 warm-start 狀態仍相同。

**建議**：先取檔案 metadata、已有 manifest 與既有 log，再決定是否需要受限的離線解析或 solver 匯入。依序使用：既有摘要 → 聚合統計 → 指定 constraint family → 少量指定 constraint。每次提升資料層級都要寫明問題、預期證據、輸出大小上限與停止條件。

**建議**：檔案容量上限、每次輸出 byte/token 上限、累積 context 預算、parser timeout、solver 匯入 timeout/RAM 上限應由工具 gate 執行。不要把「計算總行數」放成必經步驟；它本身需要掃描大檔。也不能只限制行數：一行 LP 可能很長。串流掃描只控制記憶體和回傳量，仍可能有大量 I/O，必須受時間預算約束。

**現有框架限制**：框架盤點指出 `ModelInspector` 會透過 `ReadModel` 匯入後呼叫 `Solve`，並預設開啟 LP/MPS/Sol export。它不能被當成無求解、低成本的 stats 工具；本研究沒有呼叫它。若要新增 stats-only 路徑，須另做 API 與副作用驗證。

## 2. 每個 run 必備證據與按需診斷資料

下表是**建議的資料契約**，不是 IBM 要求每個模型都有每個欄位。先建立每個 run 必備的身分、正確性、契約與基線資料，即可依現有 Phase 3 規則進行有界 R0；不要求先收齊昂貴的模型統計、完整時間拆解或 Kappa。只有特定診斷需要時才補取第二張表的資料。缺值須有原因，例如 `not_applicable`、`not_collected`、`unsupported`；缺少診斷所需證據時，結論標 `Unknown`，下一步是有界取證，不能補猜測值。

### 每個 run 必備

| 證據 | 最低內容 | 用來排除的誤判 |
| --- | --- | --- |
| 身分與可重現性 | run ID、code/data/model/config 的版本或已有 manifest/hash、CPLEX runtime、wrapper/DLL 版本、OS/CPU/RAM、threads、seed、parallel mode、warm start/callback 設定 | 比到不同模型、設定或執行環境 |
| 目標契約 | 實際業務時限、可接受 feasibility/optimality tolerance、主 KPI、instance 範圍、成功與退步門檻 | 調快了不需要的指標 |
| 正確性 | Phase 2 解驗證紀錄、當前 run 的驗證結果及原始 warnings | 調參掩蓋錯模或不合法解 |
| 基線與實驗歷史 | baseline 與失敗候選、各 trial 預算、status/stop reason、incumbent 是否存在、適用的 objective/bound/gap、主 KPI 結果、有效參數快照 | 重做失敗實驗；只挑最快 seed；拿無效結果比較 |

**建議**：優先沿用可信的已有 hash。hash 尚不存在且確有驗證需要時，才在預算內以串流計算並記錄成本；不能為了補 hash 自動掃描巨大 LP/MPS/SAV export。metadata 不能冒充內容 hash；尚未計算就明列身分驗證的限制，必要的相同性檢查仍須在比較或 promotion 前完成。

### 按診斷需要補取

| 證據 | 可補內容 | 用來排除的誤判 |
| --- | --- | --- |
| 基本模型統計 | 原始與 presolved rows/columns/nonzeros、binary/integer/continuous、constraint types、coefficient/objective/RHS/bounds 範圍 | 把檔案大等同模型難；不知道 presolve 改變多少 |
| 時間與資源拆解 | data load、build、import/export、presolve/root/search、validation 各段時間；process peak memory、tree memory、磁碟用量 | 把建模、I/O、RAM 問題錯判成搜尋策略 |
| 求解進展 | 首解時間、incumbent/bound/gap 隨時間、nodes/iterations、root/cuts 摘要 | 未觀察搜尋過程就歸因於某個 solver 機制 |
| 數值診斷 | 可取得的 residual/integrality violation、modeling assistance、按需 Kappa | 把數值病態錯判成單純搜尋太慢 |

**事實**：CPLEX MIP log 可提供 presolve/root、incumbent、best bound、cuts、iterations、nodes 與部分 time/tree-memory 資訊；內容受 `MIPDisplay`、`MIPInterval` 控制，無可行整數解時 `Best Integer` 和 gap 可以是空白。[IBM 22.1.2：Interpreting the node log](https://www.ibm.com/docs/en/icos/22.1.2?topic=mip-progress-reports-interpreting-node-log)

**推論**：log 未列某事件，不代表事件沒發生；tree memory 也不能當成整個 process 的 peak RAM。若欄位不在現有 log，先標缺證，不得用結尾兩行反推完整過程。

## 3. 數值診斷有條件，也有成本

**事實**：`DataCheck` 的 modeling assistance level 2 可在 optimization 開始時警告過大/過小 coefficient、bound、RHS 等潛在問題。它是警告機制，文件未承諾能證明模型正確。[IBM 22.1.0：Data consistency checking and modeling assistance](https://www.ibm.com/docs/en/icos/22.1.0?topic=parameters-data-consistency-checking-modeling-assistance)

**事實**：MIP Kappa 是搜尋過程中 simplex basis condition number 的統計；sifting 或沒有 crossover 的 barrier 不能提供這類 basis 統計。計算 Kappa 會增加成本，sample/full 都可能拖慢某些模型；預設 automatic 並非保證蒐集。[IBM 22.1.2：MIP kappa computation](https://www.ibm.com/docs/en/icos/22.1.2?topic=parameters-mip-kappa-computation)

**事實**：`display solution quality` 可顯示相關解品質與已蒐集的 MIP Kappa；residual 是否過大須按模型和資料量級評估。部分不穩定 basis 或顯著 residual 值值得追查，不能只看 solve status。[IBM 22.1.1：MIP kappa and ill-conditioned models](https://www.ibm.com/docs/en/icos/22.1.1?topic=tmpp-mip-kappa-detecting-coping-ill-conditioned-mip-models)

**建議**：Kappa 缺失填 `not_collected` 或具體不適用原因，不能填 0；也不能為取得 Kappa 任意更換演算法後，把有額外診斷成本的 run 當 production baseline。數值 warnings 觸發獨立診斷分支；證據指向 coefficient、bound 或 formulation 時退回 Phase 1/2，不能靠放寬 tolerance 宣稱調快。

## 4. Native tuner 是一批真正的 optimization，不是免費分析

**事實**：IBM 在模型原本可求到 optimal 的情境提醒，tuner 會做多次 optimization，可能花原始預設 run 的 6–8 倍時間。這是該情境的成本提醒，不是本模型上限或通用估計公式。[IBM 22.1.2：If CPLEX solves your problem to optimality](https://www.ibm.com/docs/en/icos/22.1.2?topic=tool-if-cplex-solves-your-problem-optimality)

**事實**：原始 run 若因 time limit 以外原因沒完成，IBM 建議先處理原因再 tune；例如 out-of-memory 應先處理記憶體策略，再把必要設定以 fixed parameters 保留。[IBM 22.1.2：If CPLEX finds solutions but does not prove optimality](https://www.ibm.com/docs/en/icos/22.1.2?topic=mtt-if-cplex-finds-solutions-but-does-not-prove-optimality)

**事實**：tuning 有兩層時間預算。整個 session 由 general `TimeLimit`/`DetTimeLimit` 限制；每次 trial 由 `Tune.TimeLimit`/`Tune.DetTimeLimit` 限制。session 可以同時設有限的秒/ticks 上限，但 trial 的秒/ticks 上限不能同時設有限值。[IBM 22.1.2：Tuning and time limits](https://www.ibm.com/docs/en/icos/22.1.2?topic=tool-tuning-time-limits)

**事實**：Interactive Optimizer 可以用 `.prm` 指定 tuner 必須保留的固定參數；PRM header 需正確版本，IBM 建議由 optimizer 輸出，不能照抄別版 header。[IBM 22.1.0：Fixed parameters to respect](https://www.ibm.com/docs/en/icos/22.1.0?topic=optimizer-fixed-parameters-respect)

**事實，僅限所引 C API 契約**：`CPXtuneparam` 只採用所列 tuning controls，其餘既有環境參數不能假定自動保留，需傳 fixed parameters；除 tuning informational callback 外的 callbacks 會被忽略。結束後環境保留 fixed+tuned 設定，即使 tuning 因限制未完成也可能已有候選；problem object 沒有解。此 API 不適用 network/QCP。[IBM 22.1.2：CPXXtuneparam and CPXtuneparam](https://www.ibm.com/docs/en/icos/22.1.2?topic=z-cpxxtuneparam-cpxtuneparam)

**推論**：tuner 的 `completed` 與候選通過 production 驗收是兩回事。若 production 依賴 callbacks，原始模型檔不足以重現實際 pipeline。上面的 C API 契約不是 OptimFoundation 已支援 native tuner 的證據，也不能替代本機 .NET adapter 驗證。

**建議**：native tuner 僅列可選候選產生器。入口先核對問題類型、callback 相容性、固定的品質/資源設定、session/trial 雙層預算、終止與清理；出口匯出實際參數與 tuning status，再回原框架 benchmark、解驗證與 promotion。不能以 native tuner 結果直接覆寫 production。

## 5. Deterministic 不等於任何環境都重現

**事實**：CPLEX 的 deterministic parallel mode 是在同模型、同參數、同平台下重現路徑/結果的承諾；parallel callbacks 的呼叫順序仍可能不固定，callback 實作需自行避免破壞 determinism。[IBM 22.1.1：Parallel mode switch](https://www.ibm.com/docs/en/icos/22.1.1?topic=parameters-parallel-mode-switch)

**事實**：tuning 使用有限的秒級 trial 上限會是 nondeterministic；即使 trial 用 ticks，整體 session 撞到秒級上限也會失去 deterministic 結果保證。opportunistic mode 亦會影響 determinism。[IBM 22.1.0：Tuning time limits and determinism](https://www.ibm.com/docs/en/icos/22.1.0?topic=tool-tuning-time-limits-determinism)

**事實**：CPLEX random seed 的預設值會隨 release 改變，因此「使用 default seed」不足以跨版本重現。[IBM 22.1.2：Random seed](https://www.ibm.com/docs/en/icos/22.1.2?topic=parameters-random-seed)

**建議**：明設並記錄 seed、threads、parallel mode、版本、同一份 model/data 及 start 狀態。演算法比較可使用 deterministic ticks；部署目標若是 wall time，仍須獨立在實際 wall budget 下驗證。外層 wall watchdog 是保護資源的措施，觸發後結果必須標為中斷，不能仍宣稱 deterministic 完整實驗。

### 5.1 Informational callback 不能一概套用 legacy control 的限制

**事實**：官方 .NET `Cplex.MIPInfoCallback` 類別明列 compatible with dynamic search，並指 user callback 在 parallel execution 中只由一個 thread 呼叫。這個網頁 URL 為 22.1.2，正文 assembly metadata 則標 22.1.1.0，版本差異須如實保留。[IBM .NET：MIPInfoCallback](https://www.ibm.com/docs/en/icos/22.1.2?topic=in-cplexmipinfocallback-class)

**事實**：search switch 文件分開定義 generic/informational callbacks 相容，legacy query/control callbacks 不相容；不能把「legacy」當成所有 callback 都停用 dynamic search 的判準。[IBM 22.1.2：MIP dynamic search switch](https://www.ibm.com/docs/en/icos/22.1.2?topic=parameters-mip-dynamic-search-switch)

**推論**：僅看到類別繼承 `MIPInfoCallback` 加 `Model.Use`，不足以判定 framework 停用 dynamic search；還要檢查其他 callback、effective search 參數及 log。官方對 informational callback 描述為不犧牲 performance，但這不能證明任意自訂 callback 的資料整理、序列化、I/O 都零成本。[IBM 22.1.2：What is an informational callback?](https://www.ibm.com/docs/en/icos/22.1.2?topic=callbacks-what-is-informational-callback)

**建議**：把「相容 dynamic search」與「觀測 overhead 已量測」列成兩個欄位；後者需同模型/seed/threads 的 telemetry 開關配對量測，尚未量測就寫未知。禁止由相容性推出零成本。

## 6. Seed holdout 與 instance holdout 解答不同問題

**事實**：IBM 的 `tools runseeds n` 對同一個目前已在記憶體的 MILP/MIQP/MIQCP，用不同 seed 重複求解以評估 variability。[IBM 22.1.2：Evaluating variability](https://www.ibm.com/docs/en/icos/22.1.2?topic=cplex-evaluating-variability)

**事實**：native tuner 也可讀一組 LP/MPS/SAV models，對整組調參；多模型評分有平均時間和 minmax 方向。[IBM 22.1.2：Files of models to tune](https://www.ibm.com/docs/en/icos/22.1.2?topic=optimizer-files-models-tune)、[IBM 22.1.0：Tuning measure](https://www.ibm.com/docs/en/icos/22.1.0?topic=parameters-tuning-measure)

**推論**：五個 tuning seeds 加三個未見 seeds，最多支持「這個 instance 在這些 seeds 上的穩健性」，不能證明未來不同需求、容量、日期、規模的 instance 都會改善。上述官方功能沒有提供 universal best parameters 的保證。

**建議**：若 production 涵蓋多種資料，就在調參前固定代表性 training instances 與 holdout instances，涵蓋規模、稀疏性、難度/業務情境。seed holdout 和 instance holdout 都保留；看過 holdout 再調參就不是 untouched holdout。單 instance 調參仍可接受，但 promotion 描述須明列適用範圍。

**建議，實驗設計而非 IBM 原文**：time-limit hit 是對「達到目標所需時間」的右設限觀測（right-censored）。兩個 run 都跑滿 300 秒，只表示皆未在預算內達標，不能推論速度或品質相同。預先定義固定預算下的 feasibility rate、incumbent、best bound、absolute/relative gap，或預定 target 的達標率/time-to-target；未達標不填虛假的完成時間。找不到 incumbent 的 objective/gap 應標缺值與原因，不拿框架 sentinel 參與平均。

## 7. 建議接進流程的不可跳過條件

以下全部是**框架建議**，具體數字需由團隊的資源與目標訂定，不能冒稱 solver 官方門檻：

1. Metadata gate：先查格式、容量、既有摘要與觀測成本，超出 context 預算的原始檔不得傳給 LLM。
2. Evidence gate：先具備身分、正確性、目標契約與基線資料，再以有界 R0 取證；昂貴診斷按需補。瓶頸缺證時標 `Unknown` 並提出有界取證動作，不能直接列參數套餐。
3. Hypothesis gate：每個候選記錄「證據位置 → 可反駁假說 → 參數/官方語意 → 預期指標 → 失敗判準」。未知原因就寫未知。
4. Budget gate：單次、單輪、全任務三層預算，加 process RAM、磁碟、import/export 及 tool-output cap。不得因剛耗完預算自動加碼。
5. Experiment gate：凍結模型/資料/品質條件，採配對 baseline/candidate，沿用現有 Phase 3 一輪一顆旋鈕規則；本研究不授權多參數同輪。未來若研究參數交互作用，須先另案修訂實驗契約。
6. Stop gate：遇 infeasible/驗證退步/資源失控，立即回退；連續無實質改善或沒有新證據時停止擴搜並報限制。
7. Promotion gate：通過 seed 與適用 instance 驗證、品質驗證、production 重跑，保留原 baseline、完整參數和 rollback 路徑。

## 8. 查證方法與仍未知的事項

- 已實際 open 並取得正文：22.1.2 `Displaying problem statistics`、`SAV file format`、`If CPLEX solves your problem to optimality`、`CPXXtuneparam and CPXtuneparam`；22.1.0 `Fixed parameters to respect`。
- 其餘引用已讀到 web search 返回的 IBM 頁面索引正文/摘要；部分直接 open 得到 403 或 cache miss。沒有宣稱讀到它們的原站全文，來源版本與 URL 以實際返回結果為準。
- 未執行目前專案的 solver；目前 instance 的實際 RAM、匯入時間、數值狀況、哪個參數有益，全部尚未實證。
- 未驗證當地 .NET native tuner 接法、完整 effective parameter 匯出方式與 stats-only adapter 能力；這些是實作前要核對的 capability，本文不提供未驗證 API 簽名。
- 未找到 IBM 對 LLM context byte/token 門檻、統一最低 seeds 數、universal promotion 百分比的官方規定；本文相關預算與 gate 是工程建議。
