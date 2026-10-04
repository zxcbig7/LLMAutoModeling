# Phase 3 Tuning 標準流程與防盲調研究提案

研究日期：2026-09-19；修訂日期：2026-09-20。狀態：研究提案，尚未修改或啟用既有流程規則。

## 結論與本次邊界

現有 Phase 3 已有假設先行、證據來源、R0 校準、多 seed、holdout、去重與停損。真正需要補的是：**在第一次讀大檔、第一次求解、每次新實驗之前，檢查資料是否足夠、成本是否可承擔，並由工具阻擋不合格操作。** 單純加長 prompt，仍可能讓同一個 agent 一邊忽略規則、一邊替自己的結論找理由。

建議保留 S0–S5 與 T0–T7，在最前面加 S-1 admission，並把「找不到證據」設為合法結果。LLM 負責提出假設與解讀有來源的摘要；讀取器、validator 與 runner 負責限制輸入、核對事實與執行預算。這三種工具目前是待開發能力，不能視為已存在。

「模型檔約 2,000 萬行、盲讀後耗時半天」來自使用者回報。本次沒有重建事故、沒有讀取巨大模型，也沒有執行 solver；因此不判定當時的實際瓶頸。檔案行數不是 solver 的 row count，也不能單獨推論模型難度或應改哪個參數。

本文件是框架研究交付，不是某個專案的 Phase 3 執行紀錄。正式流程權威仍是 `.claude/skills/AGENTS.md` 與 `.claude/skills/tuning/solver-tuning-guide.md`；未來採用提案時應修改既有權威檔，避免形成第二套規範。

## 1. 證據層級與已存在的防護

下文區分四種狀態：**規範已明訂**、**source 可見能力**、**runtime 尚未證實**、**建議新增能力**。讀到文件或 source，不等於已證明本機部署版本與歷次實驗遵守它。外部來源、版本適用性與 source 盤點見 [研究來源附錄](phase3-tuning-sources.md)。

| 主題 | 已核實的規範 | 仍需補強的接點 |
| --- | --- | --- |
| 正確性與模型邊界 | `solver-tuning-guide.md:86` 先驗正確；`:121` 凍結可寫範圍 | 首次實跑之前先確認身份、讀取與總成本 |
| 證據先行 | `solver-tuning-guide.md:574` 無計畫不得建立 variant 或執行；`:594` 每個 variant 要因果鏈 | 用 validator 檢查證據存在且匹配；缺證據時不得靠敘事通過 |
| 參考資料 | `solver-tuning-guide.md:580` 明列 History、主 CSV、meta、trajectory、R0/holdout | 加來源身份、完整度、抽取覆蓋範圍與缺失狀態 |
| 實驗設計 | `solver-tuning-guide.md:507` 固定 5 tuning seeds、3 holdout seeds；`:598` 一輪一顆 | 預算不足就停止 admission，不以少跑冒充既有 gate 通過 |
| 重複實驗 | `solver-tuning-guide.md:653` 比完整有效 config | `:1014` 又以 diff 字串相同判重；baseline 改變後 diff 不足以代表完整設定 |
| 防止捏造數字 | `solver-tuning-guide.md:983` 定義 TUNING-FACTS，`:1016` 要求引用 facts | 抽取與比對仍主要靠人工開檔，需機械驗證且限制輸出 |
| Context | `solver-tuning-guide.md:1091` 禁全文 log/JSON/Trial；`:1095` 單 agent ≤800 行 | 未覆蓋所有 artifact、bytes、超長單行、累積 tokens、掃描期限 |
| 停損 | `solver-tuning-guide.md:1036` 有 A–I，含連續 3 輪無改善及總預算 | 加每次啟動前阻擋與執行中 watchdog，保留現有輪次停損值 |
| Promotion | `solver-tuning-guide.md:783` holdout；`:848` production 重跑；總規範 `AGENTS.md:176` 允許 retain | 預先保留驗證成本；無法驗證時不得把候選宣告成已完成 promotion |

本表中的短檔名均相對於 `.claude/skills/tuning/`；「總規範 AGENTS」指 `.claude/skills/AGENTS.md`。完整導引與行號索引見根目錄 `CodeMap.md`。

### 優先修正的具體缺口

1. `.claude/skills/tuning/SKILL.md:58` 先 MUST production 實跑，`:63` 才估算預算；`solver-tuning-guide.md:1149` 的 T0 也先跑。昂貴模型在成本審查之前就能耗掉一次完整求解。
2. `solver-tuning-guide.md:549` 要求 `MipGap = 0` 探針，`:555` 允許 B/C 放大時限，但未給探針硬預算。Gap 設為零不保證取得真最佳值；未完成證明時只能記 unknown。
3. `solver-tuning-guide.md:1059` 成本式未顯列 S0、探針、native tune、warm-up、holdout、promotion 及非 solve 時間；`:1062` 啟動估算又依賴尚未取得的 R0 sgm。
4. `.claude/skills/tuning/SKILL.md:88` 與 `solver-tuning-guide.md:696` 將 native tune 稱為「零成本」。無須改程式不等於無計算成本；`:703` 的 `read` 是 CPLEX 讀模型，不能誤稱該指令要求 AI 全文讀取。
5. `solver-tuning-guide.md:565` 要求六類剖面之一，`:352` 以稀疏 trajectory 推 Node-cost，缺少 Unknown 分流；沒有觀測到事件也可能是 callback、匯出或覆蓋範圍不足。
6. `solver-tuning-guide.md:1041` 以 bound 起訖相同否證整類旋鈕。這不足以排除中間軌跡、primal 改善或達成時間差異；負面結論應限定本次 config、指標、資料與觀測範圍。
7. `solver-tuning-guide.md:465` 的 informational callback 相容性有 IBM 官方文件支持；source 將它概括成會停用 dynamic search 的註解應修正，不能當作 runtime 事實。規範同句的「不影響效能」仍過強：相容不代表 overhead 為零，仍需核對實際 callback 類型與執行 log。官方文件與版本註記見來源附錄。

### Source 可見能力與部署限制

- `../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptExperiment.cs:145` 依序建立 engine、Build、ApplyTo、Trial.Capture，最後 Save；本次 source 盤點未見每 trial checkpoint、campaign watchdog 或 process RAM 限制，不能宣稱已有硬執行上限。
- `../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.cs:431` 的模型 export 在 stopwatch 之前，`:446` 執行 Solve；因此 RunTimeMs 不是 load/build/export 的完整 wall time。
- `../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/MetaCsvWriter.cs:53` 記模型數量、machine/solver 名稱與已設 baseline 參數；這不是完整 resolved defaults/runtime 設定匯出，缺少的能力需另行補建。
- `../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/OptEngine.cs:86` 的 trajectory 有抽樣與 2,000 點上限，未見 truncated 欄位；尾端沒有點，不能直接推論 solver 停滯。
- 同檔 `:84` 的 callback 繼承 `MIPInfoCallback`，`:439` 掛載；IBM 文件指出此 informational callback 與 dynamic search 相容。相容性不證明本次部署採用同一組件，也不證明自訂採樣沒有成本。
- `dlls/ILOG.CPLEX.dll` 檔案版本讀值為 `22.1.1.0`；`dlls/VERSION.txt:1` 的 provenance 不足以證明目前 source 與部署 DLL 一致，亦未在本次求解中核實實際載入版本。

全域規範也有來源漂移：router 指向的 `../../LLMDevFramework/MILP Model/AGENTS.md` 不存在；現有 `CLAUDE.md:51` 仍要求 Optimal gate，部分 tuning 範圍也與本 repo 的 A/B/C 及 phase 邊界不一致。本 repo 的 `AGENTS.md:18`–`:20` 優先；P0 應讓全域 router 導引本 repo 權威，不複製相衝規則。全域框架屬另一 repo，本次未修改。

## 2. 分析前必備的資料與缺失處理

「必備」指足以支持這次問題的證據，不要求一次讀完所有 artifact。先檢查 manifest 與 schema，再由工具抽取必要欄位。缺少 trajectory 時仍可描述終止結果，但不能假裝知道中間發生什麼。

| 資料 | 現有來源／欄位 | 要增加或驗證的內容 | 缺失時的允許動作 |
| --- | --- | --- | --- |
| 正確性與進場情境 | `status.json` 的 solveVerified、solveStatus、verifiedOn；Phase 2 結果 | 對應哪個 instance、資料、code 與驗證時間；必要的規則驗證結果 | 身份無法連回時，不進效能實驗；先回補正確性交接 |
| Model/data/code 身份 | Model.md、資料路徑、code、既有 archive | 可重現的 model/data/code fingerprint、build ID、產物來源；路徑或時間戳單獨不算相同內容證明 | 工具先查現有 digest；必須補算時計入 I/O 預算，未完成就標 identity 未確認 |
| Baseline 與完整設定 | History provenance、meta 已設 baseline 參數、archive config snapshot | 區分顯式設定、null/auto、版本預設與實際 resolved 值；停止／環境／策略契約版本；目前未證實可完整匯出 effective 設定 | 可用顯式 snapshot 加已核對版本預設作有限替代；auto 實際值未知就明記，不能編造 |
| 參數合法性 | `cplex-parameter-reference.md`；coding API 白名單 | 對照部署 adapter 支援、CPLEX 版本值域與官方語義 | 任一不一致先停止該 candidate，不能依記憶換值 |
| 部署環境 | meta environment、DLL provenance | CPLEX/adapter/DLL 版本與 hash、OS、CPU、Threads、RAM、callback/trajectory 模式、CPU 競爭 | 不匹配則結果不可直接併入同一比較；環境不明先收集，非先重跑 |
| Trial 主表 | 現有 34 欄，見下列清單 | RunId/TrialId 對應、單位、null/NaN 語義、trial 是否完整 | 欄位缺失不得以 0 填補；縮小能回答的問題或回補 evidence |
| 終止原因與 censoring | Status、Note 可供部分判讀 | 精確 stop reason、是否 time/node/memory/其他限制、正常完成或 watchdog 中止 | 先用帶來源 log 對應；仍不明就 unknown，不把相同 runtime 當相同品質 |
| Trajectory | `-trajectory.csv`、TrajectoryPoints、TFeasMs/TStallMs/DeltaBound | 收集是否啟用、覆蓋時間、抽樣間距、最後觀測點、截斷、事件缺失原因 | 只比較已觀測欄位；無法判瓶頸則 Unknown，不從稀疏點推 Node-cost |
| Model statistics | VarCount、ConstraintCount；既有 solver log 或模型生產端統計 | 按診斷需要補非零元素、型別、presolve/root、係數範圍等，標 original/reduced 階段 | 先用現有統計；沒有就承認不能判該結構問題，不全文讀 LP 猜數量 |
| 數值與環境警訊 | 有來源的 solver log | numerical warning、dynamic search 訊息、memory/nodefile、版本與參數套用警告 | log 缺失代表該警訊未驗證，不代表沒有警訊；必要時跑有界診斷 |
| 歷史與成本 | History 的契約、R 節、否證方向；RunTimeMs | 全量 config 去重 key、適用身份、停止原因、每階段 wall time 與剩餘預算 | 跨 session 先查索引摘要；沒有新證據不重試已拒絕設定 |

主表現有 34 欄為：`RunId, TrialId, Model, ModelType, TrialLabel, BasedOn, ConfigChanges, Seed, VsBaseline, RunAt, Status, ObjectiveValue, BestBound, MipGap, RunTimeMs, TFeasMs, TStallMs, DeltaBound, NodeCount, IterationCount, TrajectoryPoints, VarCount, BinaryVarCount, IntegerVarCount, ContinuousVarCount, SemiContinuousVarCount, SemiIntegerVarCount, ConstraintCount, QuadraticConstraintCount, IndicatorConstraintCount, SosCount, LazyConstraintCount, UserCutCount, Note`。來源：`../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/ExpCsvWriter.cs` 的 `Header`；仍需核對本次 archive 的實際表頭。

`RunTimeMs` 的 source 定義不涵蓋整條 load/build/export pipeline，不能直接代表使用者等了多久。新增成本記錄應分開 `solveWallMs` 與 `campaignWallMs`，並保留 load、build、export、parse、tool startup 與驗證分項；這些不是宣稱現有 schema 已完整提供。

摘要必須附上欄位的實際定義：`TFeasMs` 是首次在已記錄 trajectory 中看到 incumbent 的時間，null 不能單獨證明沒有解；`TStallMs` 是最後一次觀測到 bound 變動的時間戳，不是停滯持續時間，沒有觀測到變動也可為 null；`DeltaBound` 是末點減首點，未依 minimize/maximize 正規化方向。來源為 `../OptimFoundation/OptimFoundation/src/OptimFoundation.Core/Experiments/SolveMetrics.cs:53`、`:66`、`:78`。這些是目前 source 的計算語義，分析部署 archive 前仍要核對版本與 trajectory 覆蓋範圍。

建議證據 manifest 至少含：`path, bytes, sourceKind, runId, trialId, modelIdentity, dataIdentity, configIdentity, producerVersion, extractorVersion, coverage, truncated, missingFields`。identity 可引用同一 manifest 中的有效 digest，不要求把原始模型交給 LLM。首次 hashing 也可能掃完整檔案，必須有 deadline 與成本紀錄；抽樣或部分 fingerprint 不可當作全內容一致的證明。

## 3. 建議的標準步驟與既有流程接點

下表為流程提案，不是已生效的操作指令。預設沿用既有停止契約、五個 tuning seeds、三個 holdout seeds及三輪無改善停損；要改它們必須明確另修規範，不能在某次調參中偷改。

| 步驟 | 必做工作 | 出口與不通過時處理 |
| --- | --- | --- |
| S-1：首次實跑前 | 只取 metadata、部署摘要、既有結果索引；定 reader policy、campaign wall/RAM 預算與收尾保留額 | policy 或總預算缺失則 metadata-only；不讀大內容、不啟動求解 |
| S0：正確性與身份 | 保留既有正確性 gate；核對 status/verifiedOn、model/data/code/build、baseline、進場 A/B/C | 需要既有規範要求的實跑時，先通過 run reservation；不得以舊的 solveVerified 跳過 |
| 有界證據收集 | 工具驗 schema、身份、coverage，抽主表／meta／歷史與必要 log；列明 missing | 產生帶來源摘要；不夠判斷則寫 evidence gap，不建立策略候選 |
| S1：環境定版 | 沿用同機且有效的 Phase 2 環境；只有既有條件命中才做 sizing | 凍結版本、資源與觀測模式；變更即重審可比性與 R0 |
| S2：R0 | 依現有五個 seeds 校準，確認主指標與 θ；先預留整組成本 | 預算不足不開跑；部分結果可保留但不得宣告 R0 gate 通過 |
| 診斷分流 | 每個瓶頸分類連到觀測；不足時標 Unknown，列最小可補資料 | 只有具體問題、deadline、成功/失敗判準與保留額的 probe 可啟動 |
| S2.5：可選 native tune | 有剩餘預算、可用工具、匹配模型身份才啟用；固定契約並設定 tuning 專屬總上限 | 建議只成為候選來源，拆成符合既有規範的 variant 再驗；不可直接 promote |
| S3：plan gate | T2 先提交來源→觀測→假設→唯一改動→可證偽預測；驗完整 config、合法值與去重 | 缺證據、重複 config、版本不匹配或無量化目標，不進 T3 |
| S3：run admission | T3 啟動前核對身份未變、CPU/RAM、剩餘 budget、trial 上限、watchdog 與 archive 位置 | reservation 不足就停止；執行中達硬上限則 checkpoint 並標中止 |
| S3：分析與續跑 | T4 只用已驗證 facts；T0 依既有 A–I、每輪三輪停損及剩餘預算決定 | 繼續需要新的可區分假設；retain/Unknown 都是合法結果 |
| S4：holdout | 使用未參與調參的三個 seeds，環境與契約一致；不以 holdout 選下一組 config | 證據不維持就 retain；未執行不能聲稱泛化驗證完成 |
| S5：promotion／retain | 有可靠勝者才寫回 baseline、build、實際 production 驗證；其餘 retain + 證據 | 完整記錄停止原因與未驗證項目；不把候選改善當 production 已改善 |

S-1 由 T0 協調，T1 檢查範圍與 evidence admission；T2 設計，T3 執行與抽取，T4 解讀，T5 獨立裁決，T6 promotion，T7 驗證。保留既有角色責任，不新增一堆互相傳原始資料的 agent。

## 4. 大檔讀取協定：限制進 context，也限制實際工作量

1. 所有 artifact 先 `stat`，只取得路徑、bytes、mtime、種類與已有 manifest。未知行數不為了「先數行」掃完整檔案；二進位 SAV 不當文字讀。
2. 巨大 LP/MPS/SAV 永不全文送入 LLM，也不分頁、逐塊搬入直到等同全文。需要模型特徵時，用 solver 生產端既有統計或有界工具輸出摘要。
3. LLM 的主要輸入是 schema 化摘要與必要局部證據。每次讀取必須寫明問題、必要欄位、最大輸出與停止條件；不能先讀再想用途。
4. reader policy 必須同時限制單行長度、單次 bytes/行數、每 query tokens、累積 evidence tokens、掃描 wall time與 process memory。缺任一必要設定時 metadata-only 或 fail closed。
5. 初始建議值：`256 KiB` 以上禁止全文進 context；每次輸出最多 `16 KiB` 且 `200 行`；單行最多 `2 KiB`；單 query 最多 `4,000 tokens`；每次分析 evidence 總額 `12,000 tokens`。
6. 上述是待實作、可配置的保守 policy 初值，**不是 CPLEX 或模型 context 的官方限制**。依部署工具的已知 context 配額配置，預留指令與回答空間，不讓 agent 猜剩餘視窗。
7. 掃描與 parser 必須串流、有取消能力，且實際配置每 query 的時間與 RAM 上限；顯示被截斷不代表掃描已停止。沒有可執行的 deadline 就不要啟動未知成本掃描。
8. 禁止對未通過 reader admission 的 artifact 使用 `Get-Content -Raw`、`ReadAllText`、全文 JSON 反序列化進記憶體、無界 `rg` 或逐頁全部讀。小型已核准摘要可依 policy 正常讀取。
9. `rg -m`、只取前幾列、工具 output limit 不一定限制巨長單行與底層掃描成本；需要 reader 自己掌握 byte、時間、memory 與取消。不能把輸出截短當作 I/O 已有界。
10. 每份摘要附來源、extractor 版本、coverage、truncated 與缺失欄位。首尾抽樣只能描述首尾樣本，不可推論整份模型、全部 warning 或完整軌跡都相同。
11. 相同 query 連續兩次沒有新增可用證據，就停止這條讀取路徑；改用已有摘要、縮小問題或記 Unknown。這是新提案的讀取停損，與既有三輪調參停損分開。
12. 歷史全文會隨 round 增長；保留原始 History，另由工具產生有界跨輪索引與否證摘要供查詢，不能每輪把所有 R 節重新灌入 context。

manifest 與摘要應優先內嵌既有 `TuningHistory.md` 的固定區塊，原始證據仍放 `Experiments/`。若未來新增獨立 artifact 檔名，必須同步總規範白名單、schema、archive 與 resume 規則；本提案不自行授權新增 project 子資料夾。

## 5. 成本契約與無效工作的停損

Campaign 預算是「使用者願意花在本次調參上的總 wall time與 RAM」，solver 的 TimeLimit 是「每個 trial 的求解停止契約」；兩者需同時存在，不能互相代替。已授權的有效預算跨 session 沿用，無需反覆詢問。

成本帳應包含：S0 production、必要 sizing、R0、所有 probe、native tune、各 variant×seeds、warm-up、holdout、promotion，以及 load/build/export/parse、工具啟動與 evidence 核驗。既有規範 `solver-tuning-guide.md:1059` 的估算式只適合作為其中一部分。

啟動前先用已驗證的歷史 wall time、現行 TimeLimit 與 load/build/export 上界做保守估算；沒有 R0 時不得假裝已有 R0 sgm。無可靠上界的工作必須由 process deadline 截止。未知成本不是零成本，也不能以「先跑看看」略過 admission。

建議每次啟動條件為：`已用 wall + 本次 reservation + 必要收尾／驗證保留額 ≤ campaign wall budget`。reservation 包含本 trial 非 solve 工作與硬截止；一組尚不可中斷判定的 R0/holdout 亦須先確認整組可負擔。超時時不能繼續偷偷透支下一輪。

runner 需要可取消 process watchdog、RAM 上限、退出碼與終止原因、checkpoint、原始產物保存。watchdog 的終止屬操作中止，必須另列 censored/partial；不得當成 solver 自然完成、Optimal 或普通完整 TimeLimit trial。只有驗證能實際終止 process 與其必要子程序，才可宣稱 hard limit；只提醒超時、無法停止的 wrapper 不算。這些是建議新增的執行能力。

`MemoryLimitMb` 在現有 adapter 對應的 solver 記憶體旋鈕不是 process 總 RAM 配額；來源為 `../OptimFoundation/OptimFoundation/src/OptimFoundation.Cplex/CplexConfig.cs:268`。process memory 上限需要額外執行層能力，不能改名稱就假裝已實作。

所有 probe 都要寫明要回答的問題、預期可觀測值、wall/RAM 上限及停止條件。預算不夠就不做；`MipGap = 0` 或加長 TimeLimit 仍未證明最佳時，記「真最佳未知」，不得把目前 incumbent 填成真最佳，也不得據此計算虛構品質損失。

停止契約照既有規範固定，禁止為了贏而悄悄延長 candidate 時限。若使用短時診斷，只能放在明確隔離的 probe 項，不能混入正式排名，也不能冒充完整五 seed 校準。

同一台機器的 trial 需維持可比的 CPU/RAM 條件；其他 solver/process 競爭或 memory 壓力超過契約時，先暫停 admission，已受影響的試驗標示不可直接比較。這不表示只要抖動就無限重跑，重跑仍受既有去重與預算限制。

## 6. 防止 AI 把猜測寫成結論

每次分析強制分三欄：**Observation** 是帶 artifact/欄位/範圍的事實；**Hypothesis** 是尚待驗證的瓶頸解釋；**Prediction** 是指定 config 改動後，哪個可量測指標在什麼門檻下改變。三者不可用同一段肯定句混在一起。

| 容易誤判的觀測 | 合法結論 | 不能直接推出的結論 |
| --- | --- | --- |
| trajectory 缺失或點數稀疏 | 中間收斂資訊不足；檢查觀測模式與 coverage | 每個 node 一定很貴，應立即改 NodeAlgorithm |
| bound 首尾相同 | 本次首尾觀測未見淨變化 | 整類 cuts/emphasis 都無效，永遠不必再查 |
| 所有 trial 跑到時限 | runtime 被停止條件截尾；依 A/B/C 檢查品質與可行性 | 各 config 一樣好、沒有任何調整空間 |
| 缺 log 或未命中 warning 摘要 | 此來源／範圍沒有已知證據 | 全部求解過程沒有數值或環境警告 |
| NaN、空值、被 watchdog 中止 | 該值不可用，保留 missing/censored 原因 | 把值填成 0、沒有改善或已證明不變 |
| 同 instance 新 seed 仍改善 | 對此 instance 的未用 seeds 有支持 | 已能推廣至不同規模、結構或其他 instance |
| 五個 tuning、三個 holdout seeds 完成 | 通過既有流程要求，仍需報變異與適用範圍 | 這個數量提供普遍統計保證或證明全域最佳參數 |

參數的官方語義只能支持「為何值得測」，不能取代本次模型的 empirical evidence。查表顯示可用的值，也不代表 adapter 已正確套用；resolved config 與 log 應能佐證，無法取得的部分明記未核實。

## 7. 分期落地與精確修改位置

| 階段 | 修改位置 | 工作與交付 |
| --- | --- | --- |
| P0：規範先修 | `.claude/skills/tuning/SKILL.md:54`、`:86`、`:90` | S0 前接 S-1；S2.5 改為可選且計成本；S3 分 plan/run admission |
| P0：讀取與未知 | `.claude/skills/tuning/solver-tuning-guide.md:330`、`:559`、`:1089` | 補 Unknown、coverage、所有 artifact 的 reader policy；保留既有摘要原則 |
| P0：成本與否證 | `.claude/skills/tuning/solver-tuning-guide.md:547`、`:696`、`:1036`、`:1054` | 有界 probe/native tune，修全程成本，收斂否證適用範圍，增加 prelaunch guard |
| P0：驗收入口 | `.claude/skills/tuning/checklist.md:8`、`:48`、`:155`；`.claude/commands/Ph3_Tuning/review.md:3` | reviewer 必查身份、讀取、成本與 missing；沿用既有 Blocker/Finding/Next experiment/Verified |
| P1：工具可執行 | 新增有界 inspector/extractor、facts validator、runner admission | 能限制資源、驗 schema/身份/config、生成摘要與成本帳；具體 API/路徑另做設計 |
| P1：同步授權範圍 | `.claude/skills/tuning/SKILL.md:23`；`solver-tuning-guide.md:1018`；`.claude/skills/AGENTS.md:159` | 現規範明訂不依賴外部腳本，採工具前須同步修改；新 artifact 要同步白名單與 schema |
| P2：框架 telemetry | 獨立 OptimFoundation 開發任務及 API/DLL 發布流程 | 如需新增 termination reason、完整 wall 分項、callback 模式、model stats，實作後測試與更新部署來源 |

P0 能先修正錯誤預設，但文字規則仍不是強制執行。P1 才讓違規讀取與超額執行在進入 LLM／solver 前被擋下。P2 必須獨立成 framework 開發工作；Phase 3 的唯讀邊界不能因缺 telemetry 就自行突破。

若 agent 仍能用任意 shell/Read 繞過 reader 或 runner，wrapper 無法保證阻擋。真正強制化需在 tool boundary 限制能力或採 allowlist，只允許受 admission 管理的讀取與執行入口；若平台做不到，必須明列仍是軟性規則，不能宣稱已有不可繞過的安全閘門。

現有 `ModelInspector` 不應列為安全的靜態讀檔器：`../OptimFoundation/OptimFoundation/Templates/ModelInspector/Program.cs:28-34` 預設 export 開啟，`:45-46` 透過 `ReadModel` 匯入，`:55-56` 呼叫 `Solve`；MaxPrint 只限制 console 輸出，部署行為仍須核對版本。若要模型摘要工具，應另外設計預設不 solve、不 export、可限資源的入口，不能只改名稱就沿用。

完整 config 去重 key 應包含 model/data/code/契約/環境身份與完整有效設定，量測 seed 依既有規範排除。`ConfigChanges` 仍只適合作為人類摘要，不能代替全量 key；未解析的 auto/預設值與版本須保留在 identity 中，避免假等價。

## 8. 可直接採用的規則草案

以下十二條是**建議文字，尚未生效**；採納後應寫入既有權威文件對應段落，而非要求每次 tuning 再讀本研究報告。

1. **MUST** 在第一次實驗 artifact 內容讀取或求解前完成 S-1：artifact metadata、reader policy、campaign wall/RAM budget與身份索引；設定不足只允許 metadata-only。已知小型入口規範與 reader policy 的載入不依賴 campaign 預算，但仍須確認檔案大小並遵守工具輸出上限。
2. **NEVER** 將巨大 LP/MPS/SAV 或未通過 reader admission 的其他 artifact 全文、分頁累積全文帶入 context；未知行數不得為計數而無界掃描。
3. **MUST** 同時限制 bytes、行數、單行、query／累積 tokens、scan wall time與 RAM；輸出截斷不得宣稱掃描成本已受控。
4. **MUST** 讓每項數值和結論能回連到 matching run/config identity、schema、來源、coverage與 missing；無法追溯不得用於排名。
5. **MUST** 把 Observation、Hypothesis、Prediction 分開；證據不足標 Unknown，**NEVER** 把缺值、缺 trajectory、未見 warning 或 timeout 當 0 或反證。
6. **MUST** 通過 plan gate 才建立 candidate；參數值查部署版本來源，完整 config 去重，跨 session 不因 label/seed 改名重試已拒絕設定。
7. **MUST** 每次執行前 reservation，包含非 solve 成本與收尾／驗證保留額；無有效總預算或剩餘不足時禁止啟動，已授權預算無需重問。
8. **MUST** 每個 probe/native tune 有明確問題與硬 deadline，全部計入 campaign；**NEVER** 將不改程式等同零成本或把 MipGap=0 當作已知真最佳。
9. **MUST** 保留既有停止契約、R0/holdout要求與三輪無改善停損；預算不足就交付未完成狀態，**NEVER** 少跑卻宣告 gate 通過。
10. **MUST** 記錄 watchdog 中止、censoring、CPU/RAM 競爭與部署漂移；它們不得與正常完整 trial 混成相同語義。
11. **MUST** 兩次相同 query 無新證據即停止該讀取路徑；整類否證須有足夠且適用的證據，首尾 bound 相同只支持局部觀測。
12. **MUST** 用三個未參與調參的 seeds 驗證改善並實際驗證 production；若宣稱跨 instance 適用，另用未參與調參的 instances 驗證。沒有可靠勝者就 retain，需模型/資料/API變更則退回對應 phase或另立 framework 任務。

## 9. 採用後的驗收情境

| 情境 | 必須觀察到的行為 |
| --- | --- |
| 使用者指定 2,000 萬行模型 | 先 stat/manifest；不掃行數、不全文或逐塊灌入；只取得足以回答問題的摘要 |
| 小 bytes 檔含巨長單行／大型壓縮 JSON | 單行、解壓後輸出與 parser memory 都受限；回報 truncated/unsupported，不能只靠檔案總 bytes |
| 資料或 config 與 baseline 不匹配 | admission FAIL；列出身份差異，不能跨 instance/config 偷併 baseline |
| 缺 solver log | 可報已驗證主表事實；警訊與必要前提標未驗證，需補證據才放行相關判斷 |
| 全部 timeout | 保留終止原因與 censoring，依進場主指標比較；沒有可用值則 Unknown，不能 runtime 平手即結案 |
| 剩餘預算不足一次 trial 加收尾 | 啟動前拒絕；保存已完成結果，不等整輪跑完才超支停止 |
| 同 query 兩次無新證據 | 停止該讀取路徑，寫明問題與證據缺口，不再無限換命令讀同一內容 |
| 跨 session 重試已拒絕 config | 用完整 key 與 identity 偵測；沒有符合既有例外的明確 replication 授權就禁止 |
| 同機 CPU 競爭／memory 壓力 | 暫停 admission 或標已受影響 trial；不直接拿污染結果 promote |
| DLL 過期或版本不同 | 版本 gate FAIL；記 source/build/deployed 差異，先依既有 DLL provenance 流程處理 |
| 預算只能跑三個 R0 seeds | 保存部分結果並說 R0 未通過，不能重新命名成五 seed 完整校準 |
| callback 文件與 source 註解矛盾 | 依 IBM 對 MIPInfoCallback 的相容性說明修正註解；核對部署與有界 runtime log，不將相容宣稱為零 overhead |

驗收應測上述失敗情境與實際阻擋效果，不只測「文件寫了 MUST」。工具即使交回合法 JSON，若底層仍掃遍超大檔案或超額執行，仍不合格。

## 10. 開始一輪實際 Tuning 前，使用者需提供什麼

- 目標範圍：只改善一個 instance，還是要適用一組具代表性的 instances；若是後者，指定哪些可調參、哪些只留驗證。
- 主要目標與不能改的契約：更快完成、固定時限內更小 gap、或更可靠找到可行解；資料、目標語義、TimeLimit/MipGap 等哪些保持不變。
- 本次總 wall time與可用 RAM 預算，以及需要保留的驗證時間；已有有效授權時直接沿用。
- 上一輪 `.csv`、`-meta.csv`、`.json` 三件套的路徑，存在時的 trajectory/log，再加 `TuningHistory.md`、`status.json` 與部署版本摘要。

上述資料由工具在原處擷取與核對，使用者不必把完整模型貼進對話。若目前只有模型與 code、沒有有效 baseline 證據，第一個工作項應是「在預算內建立可驗證的 baseline 摘要」，而不是開始掃旋鈕。

## 來源與適用限制

本機規範行號以本次工作目錄為準；調整文件後應同步 CodeMap 與引用。對部署 CPLEX 的具體語義、tuning tool 的時間限制及 callback 行為，使用 [來源附錄](phase3-tuning-sources.md) 中與實際版本相符的官方文件，不把其他 solver 經驗當成 CPLEX 事實。

本次沒有驗證任何候選參數可使特定模型加速；提案中的讀取配額、Unknown 分流、manifest、watchdog 與 admission 是待實作與驗收的流程設計。下一步應先落地 P0 與一個最小 P1 阻擋閉環，再在有明確預算的專案上驗證，避免一次擴成新的大型調參平台。
