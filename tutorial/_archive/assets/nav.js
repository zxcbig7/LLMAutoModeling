/* ============================================================
   OptimFoundation MILP 教學 — 共用導航
   · 由單一章節清單渲染左側導航（每頁一致、免重複維護）
   · 依當前頁面 URL 自動 highlight 對應連結
   · 自動產生上一章 / 下一章 pager（若頁面有 #pager）
   · 行動裝置漢堡選單
   ============================================================ */
(function () {
  "use strict";

  // 章節順序 = 學習路線。num 空字串者不計入 pager 的「章」。
  var CHAPTERS = [
    { file: "index.html",      num: "",   title: "首頁 / 學習路線" },
    { file: "chapter_01.html", num: "01", title: "環境準備與專案結構" },
    { file: "chapter_02.html", num: "02", title: "MILP 建模理論" },
    { file: "chapter_03.html", num: "03", title: "定義變數 Variable" },
    { file: "chapter_04.html", num: "04", title: "目標函數 Objective" },
    { file: "chapter_05.html", num: "05", title: "約束 Constraint" },
    { file: "chapter_06.html", num: "06", title: "建立模型 Model" },
    { file: "chapter_07.html", num: "07", title: "求解器設定 Solver" },
    { file: "chapter_08.html", num: "08", title: "讀取結果 Solution" },
    { file: "chapter_09.html", num: "09", title: "敏感度分析與錯誤處理" },
    { file: "chapter_10.html", num: "10", title: "完整整合範例" }
  ];

  // 取得當前頁面檔名（去掉路徑與 query/hash）；空字串視為首頁
  function currentFile() {
    var path = window.location.pathname.split("/").pop() || "index.html";
    return path.split("?")[0].split("#")[0] || "index.html";
  }

  var current = currentFile();
  var idx = CHAPTERS.findIndex(function (c) { return c.file === current; });
  if (idx < 0) idx = 0;

  // ── 渲染左側導航 ──────────────────────────────────────────
  function renderSidebar() {
    var aside = document.getElementById("sidebar");
    if (!aside) return;

    var html = '' +
      '<div class="brand">' +
        '<a href="index.html">OptimFoundation' +
          '<span>MILP 開發教學 · CPLEX</span>' +
        '</a>' +
      '</div>' +
      '<div class="toc-sep">章節</div>' +
      '<nav class="toc">';

    CHAPTERS.forEach(function (c) {
      var active = c.file === current ? " active" : "";
      var label = c.num
        ? '<span class="n">' + c.num + '</span><span>' + c.title + '</span>'
        : '<span class="n">＊</span><span>' + c.title + '</span>';
      var aria = active ? ' aria-current="page"' : '';
      html += '<a class="toc-link' + active + '" href="' + c.file + '"' + aria + '>' + label + '</a>';
    });

    html += '</nav>';
    aside.innerHTML = html;

    // 確保 active 連結在視窗內可見
    var act = aside.querySelector(".toc-link.active");
    if (act && act.scrollIntoView) {
      act.scrollIntoView({ block: "nearest" });
    }
  }

  // ── 上一章 / 下一章 ───────────────────────────────────────
  function renderPager() {
    var pager = document.getElementById("pager");
    if (!pager) return;

    var prev = idx > 0 ? CHAPTERS[idx - 1] : null;
    var next = idx < CHAPTERS.length - 1 ? CHAPTERS[idx + 1] : null;
    var html = "";

    if (prev) {
      html += '<a class="prev" href="' + prev.file + '">' +
                '<span class="dir">← 上一章</span>' +
                '<span class="ttl">' + prev.title + '</span></a>';
    } else {
      html += '<span class="spacer"></span>';
    }

    if (next) {
      html += '<a class="next" href="' + next.file + '">' +
                '<span class="dir">下一章 →</span>' +
                '<span class="ttl">' + next.title + '</span></a>';
    } else {
      html += '<span class="spacer"></span>';
    }

    pager.innerHTML = html;
  }

  // ── 行動裝置漢堡選單 ──────────────────────────────────────
  function setupToggle() {
    var btn = document.createElement("button");
    btn.id = "nav-toggle";
    btn.type = "button";
    btn.setAttribute("aria-label", "切換導航");
    btn.innerHTML = "☰";
    btn.addEventListener("click", function () {
      document.body.classList.toggle("nav-open");
    });
    document.body.appendChild(btn);

    // 點選連結後自動收合
    document.addEventListener("click", function (e) {
      var t = e.target.closest ? e.target.closest("#sidebar a") : null;
      if (t) document.body.classList.remove("nav-open");
    });
  }

  function init() {
    renderSidebar();
    renderPager();
    setupToggle();
    document.title = (CHAPTERS[idx].num ? "第 " + CHAPTERS[idx].num + " 章 · " : "") +
                     CHAPTERS[idx].title + " — OptimFoundation MILP 教學";
  }

  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }
})();
