# Overnight Tutorial Writer

## 用法
把以下這段貼到 Claude Code，執行一次後它會自己跑完整晚。

---

## 貼給 Claude Code 的內容

### Step 1：先執行這個，只執行一次

```bash
mkdir -p C:\Users\zxcbi\Desktop\Projects\OptimizationFramework\ClaudeAIAssistant\tutorial\assets
```

然後建立這個初始化檔案 `tutorial/progress.json`：

```json
{
  "status": "in_progress",
  "chapters": [
    { "id": "assets",      "title": "共用樣式與導航",         "done": false },
    { "id": "chapter_01",  "title": "環境準備與專案結構",     "done": false },
    { "id": "chapter_02",  "title": "MILP 建模理論",          "done": false },
    { "id": "chapter_03",  "title": "定義變數 Variable",      "done": false },
    { "id": "chapter_04",  "title": "目標函數 Objective",     "done": false },
    { "id": "chapter_05",  "title": "約束 Constraint",        "done": false },
    { "id": "chapter_06",  "title": "建立模型 Model",         "done": false },
    { "id": "chapter_07",  "title": "求解器設定 Solver",      "done": false },
    { "id": "chapter_08",  "title": "讀取結果 Solution",      "done": false },
    { "id": "chapter_09",  "title": "敏感度分析與錯誤處理",   "done": false },
    { "id": "chapter_10",  "title": "完整整合範例",           "done": false },
    { "id": "index",       "title": "首頁",                   "done": false }
  ]
}
```

### Step 2：執行 /loop

在 Claude Code 輸入：

```
/loop
```

接著貼入以下 loop body（這是每次迭代執行的 prompt）：

---

```
讀取 tutorial/progress.json。

找到第一個 "done": false 的章節。如果全部都是 "done": true，把 status 改成 "completed" 然後停止。

根據該章節的 id，執行對應工作：

**assets**
建立 tutorial/assets/style.css（深色主題，背景 #0f1117，文字 #e2e8f0，程式碼區塊樣式，左側導航欄 260px 固定）
建立 tutorial/assets/nav.js（讀取當前頁面 URL，自動 highlight 對應章節連結）

**chapter_01**
建立 tutorial/chapter_01.html
內容：環境準備，安裝 OptimizationFramework，確認 CPLEX/Gurobi 授權，專案資料夾結構說明，Hello World 測試程式

**chapter_02**
建立 tutorial/chapter_02.html
內容：什麼是 MILP（混合整數線性規劃），連續變數 vs 二元變數，目標函數，約束條件，用工廠排程問題舉例說明數學式，MathJax 渲染公式

**chapter_03**
建立 tutorial/chapter_03.html
內容：OptimizationFramework 的 Variable class，連續變數（生產數量）與二元變數（產線是否啟動）的定義方式，API 說明，工廠案例程式碼，常見錯誤

**chapter_04**
建立 tutorial/chapter_04.html
內容：ObjectiveFunction 設定，最大化利潤公式，係數設定，工廠案例的利潤計算，minimize vs maximize

**chapter_05**
建立 tutorial/chapter_05.html
內容：Constraint 單一約束，ConstraintSet 批次管理，產能約束、需求約束、產線切換約束，等式與不等式，範圍約束

**chapter_06**
建立 tutorial/chapter_06.html
內容：Model class，把變數、目標函數、約束全部組合進 Model，驗證 model 合法性，印出 model 摘要

**chapter_07**
建立 tutorial/chapter_07.html
內容：SolverConfiguration，選擇求解器（CPLEX/Gurobi），設定時間限制、MIP gap、執行緒數，工廠案例的推薦設定

**chapter_08**
建立 tutorial/chapter_08.html
內容：Solution 物件，讀取變數值、目標函數值、求解狀態，把工廠排程結果轉成人看得懂的表格輸出

**chapter_09**
建立 tutorial/chapter_09.html
內容：敏感度分析（如果 framework 有支援），infeasible 的診斷方法，unbounded 的處理，常見例外與對應解法

**chapter_10**
建立 tutorial/chapter_10.html
內容：把前面所有章節整合成一個完整可執行的工廠排程程式，完整程式碼加逐行說明，執行結果截圖說明，延伸題目建議

**index**
建立 tutorial/index.html
首頁：專案介紹，學習路線圖，各章節卡片連結，預計學習時間

---

寫完後：
1. 用 bash 確認檔案存在且大小 > 0
2. 把 progress.json 裡那個章節的 "done" 改成 true
3. 輸出一行：✅ [章節id] 完成，下一個：[下一個未完成章節id]

注意事項：
- 先讀 OptimizationFramework source code 確認 API 名稱再寫
- 每個 HTML 引入 assets/style.css 和 assets/nav.js
- 程式碼用 highlight.js（CDN），數學式用 MathJax（CDN）
- 不要等我確認，直接做決策繼續
```

---

### /loop 的終止條件

Claude Code 在 `/loop` 模式下，當 loop body 的回應裡出現你指定的終止字串時停止。

建議設定終止條件為：

```
status === "completed"
```

或者更簡單，讓 Claude 在最後一章完成時輸出：

```
🎉 ALL DONE
```

然後在 `/loop` 設定：遇到 `ALL DONE` 就停止。

---

### 整晚執行的預估

| 章節 | 預估 token | 預估時間 |
|------|-----------|---------|
| assets (CSS+JS) | ~2k | 2 分鐘 |
| chapter 01~10 | ~8k 每章 | 10 分鐘每章 |
| index | ~3k | 3 分鐘 |
| **合計** | **~90k** | **~2 小時** |

整晚時間綽綽有餘，就算某章寫慢一點也沒問題。

