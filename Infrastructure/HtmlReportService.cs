using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BaqueanoAutoTest.Infrastructure;

public class HtmlReportService
{
    private readonly string _screenshotFolder;
    private readonly string _reportFolder;
    private readonly ILogger<HtmlReportService> _logger;

    public HtmlReportService(IConfiguration config, ILogger<HtmlReportService> logger)
    {
        var configured = config["TestSettings:ScreenshotFolder"] ?? "Screenshots";
        _screenshotFolder = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);

        _reportFolder = AppContext.BaseDirectory;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(List<TestResult> results)
    {
        foreach (var old in Directory.GetFiles(_reportFolder, "TestReport_*.html"))
        {
            try { File.Delete(old); } catch { }
        }

        var reportPath = Path.Combine(_reportFolder,
            $"TestReport_{DateTime.Now:yyyyMMdd_HHmmss}.html");

        var items = results
            .OrderBy(r => r.ExecutedAt)
            .Select(r =>
            {
                string? relPath = null;
                if (r.ScreenshotPath != null && File.Exists(r.ScreenshotPath))
                    relPath = Path.GetRelativePath(_reportFolder, r.ScreenshotPath)
                                  .Replace('\\', '/');
                return new
                {
                    testName   = r.TestName,
                    category   = r.Category,
                    passed     = r.Passed,
                    message    = r.Message ?? string.Empty,
                    executedAt = r.ExecutedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    imagePath  = relPath
                };
            })
            .ToList();

        var itemsJson = JsonSerializer.Serialize(items,
            new JsonSerializerOptions { WriteIndented = false });

        int total = results.Count;
        int pass  = results.Count(r => r.Passed);
        int fail  = total - pass;
        double rate = total > 0 ? Math.Round((double)pass / total * 100, 1) : 0;

        var html = BuildHtml(itemsJson, total, pass, fail, rate,
            DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

        await File.WriteAllTextAsync(reportPath, html, System.Text.Encoding.UTF8);
        _logger.LogInformation("Reporte HTML generado: {Path}", reportPath);
        return reportPath;
    }

    private static string BuildHtml(string itemsJson, int total, int pass, int fail,
                                    double rate, string runDate)
    {
        return Template
            .Replace("@@ITEMS_JSON@@", itemsJson)
            .Replace("@@TOTAL@@",    total.ToString())
            .Replace("@@PASS@@",     pass.ToString())
            .Replace("@@FAIL@@",     fail.ToString())
            .Replace("@@RATE@@",     rate.ToString("F1"))
            .Replace("@@RUN_DATE@@", runDate);
    }

    private const string Template = """
        <!DOCTYPE html>
        <html lang="es">
        <head>
          <meta charset="UTF-8">
          <meta name="viewport" content="width=device-width,initial-scale=1">
          <title>BaqueanoAutoTest — Reporte @@RUN_DATE@@</title>
          <style>
            *{box-sizing:border-box;margin:0;padding:0}
            body{font-family:system-ui,-apple-system,sans-serif;background:#f1f5f9;color:#1e293b}

            /* ════ HEADER ════ */
            header{background:linear-gradient(135deg,#1e293b 0%,#0f172a 100%);
                   color:#fff;padding:28px 36px 24px}
            .h-top{display:flex;justify-content:space-between;align-items:flex-start;flex-wrap:wrap;gap:12px}
            .logo{font-size:26px;font-weight:800;letter-spacing:-0.5px}
            .subtitle{font-size:13px;color:#94a3b8;margin-top:3px}
            .run-date{font-size:11px;color:#64748b;margin-top:4px}
            .summary{display:flex;gap:12px;flex-wrap:wrap;margin-top:20px}
            .stat{background:rgba(255,255,255,.06);border:1px solid rgba(255,255,255,.1);
                  border-radius:10px;padding:12px 22px;text-align:center;min-width:86px}
            .stat .num{display:block;font-size:28px;font-weight:700}
            .stat .lbl{display:block;font-size:10px;text-transform:uppercase;
                       letter-spacing:1px;color:#94a3b8;margin-top:2px}
            .s-total .num{color:#e2e8f0}
            .s-pass  .num{color:#4ade80}
            .s-fail  .num{color:#f87171}
            .s-rate  .num{color:#60a5fa}

            /* ════ DEVICE TAB BAR ════ */
            .device-tabs{background:#fff;border-bottom:2px solid #e2e8f0;
                         display:flex;overflow-x:auto;
                         scrollbar-width:none;-ms-overflow-style:none}
            .device-tabs::-webkit-scrollbar{display:none}

            .dtab{flex:0 0 auto;min-width:130px;padding:16px 20px 14px;
                  border:none;background:none;cursor:pointer;
                  border-bottom:3px solid transparent;margin-bottom:-2px;
                  display:flex;flex-direction:column;align-items:center;gap:5px;
                  transition:background .15s,border-color .15s;position:relative}
            .dtab:hover{background:#f8fafc}
            .dtab.active{border-bottom-color:#1e293b;background:#f8fafc}

            .dtab-icon{font-size:22px;line-height:1}
            .dtab-label{font-size:13px;font-weight:600;color:#475569}
            .dtab.active .dtab-label{color:#1e293b}

            .dtab-scores{display:flex;align-items:center;gap:5px;font-size:11px;font-weight:600}
            .ds-pass{color:#16a34a}
            .ds-fail{color:#dc2626}
            .ds-sep{color:#cbd5e1}
            .dtab-rate{background:#f1f5f9;color:#475569;border-radius:999px;
                       padding:2px 8px;font-size:10px;font-weight:700}
            .dtab.active .dtab-rate{background:#1e293b;color:#fff}

            /* ════ CONTEXT BAR ════ */
            .ctx{display:flex;justify-content:space-between;align-items:center;
                 padding:12px 0 10px;border-bottom:1px solid #e2e8f0;margin-bottom:18px}
            .ctx-left{font-size:13px;color:#64748b}
            .ctx-right{font-size:12px;color:#94a3b8}

            /* ════ GRID 3×3 ════ */
            main{max-width:1380px;margin:0 auto;padding:0 24px 32px}
            .grid{display:grid;grid-template-columns:repeat(3,1fr);gap:18px}

            /* ════ CARD ════ */
            .card{background:#fff;border-radius:10px;overflow:hidden;
                  box-shadow:0 1px 4px rgba(0,0,0,.07);
                  border-top:4px solid #e2e8f0;transition:box-shadow .25s}
            .card:hover{box-shadow:0 8px 24px rgba(0,0,0,.12)}
            .c-pass{border-top-color:#22c55e}
            .c-fail{border-top-color:#ef4444}

            /* image zoom */
            .card-img-wrap{position:relative;width:100%;aspect-ratio:16/9;
                           background:#f8fafc;overflow:hidden;cursor:zoom-in}
            .card-img{width:100%;height:100%;object-fit:cover;object-position:top;
                      display:block;
                      transition:transform .4s cubic-bezier(.25,.46,.45,.94);
                      transform-origin:center top}
            .card-img-wrap:hover .card-img{transform:scale(1.6)}
            .card-img-wrap::after{content:'🔍';position:absolute;bottom:6px;right:8px;
                                  font-size:18px;opacity:0;transition:opacity .2s;
                                  pointer-events:none;filter:drop-shadow(0 1px 3px rgba(0,0,0,.5))}
            .card-img-wrap:hover::after{opacity:1}
            .no-img{width:100%;height:100%;display:flex;align-items:center;
                    justify-content:center;color:#94a3b8;font-size:12px;background:#f1f5f9}

            .card-body{padding:12px}
            .card-title{font-size:12px;font-weight:700;color:#1e293b;
                        margin-bottom:7px;word-break:break-all;line-height:1.4}
            .badges{display:flex;gap:4px;flex-wrap:wrap;margin-bottom:6px}
            .badge{font-size:10px;font-weight:600;padding:2px 7px;
                   border-radius:999px;text-transform:uppercase;letter-spacing:.3px}
            .b-cat  {background:#e0f2fe;color:#0369a1}
            .b-pass {background:#dcfce7;color:#15803d}
            .b-fail {background:#fee2e2;color:#dc2626}
            .b-desk {background:#f1f5f9;color:#475569}
            .b-tab  {background:#ecfdf5;color:#065f46}
            .b-mob  {background:#f3e8ff;color:#7e22ce}
            .card-msg{font-size:11px;color:#64748b;margin-bottom:5px;line-height:1.45}
            .card-date{font-size:10px;color:#9ca3af}

            /* ════ LIGHTBOX ════ */
            #lightbox{display:none;position:fixed;inset:0;
                      background:rgba(0,0,0,.9);z-index:9999;
                      align-items:center;justify-content:center;
                      cursor:zoom-out;padding:20px}
            #lightbox.open{display:flex}
            #lb-img{max-width:92vw;max-height:90vh;border-radius:8px;
                    box-shadow:0 32px 80px rgba(0,0,0,.7);
                    animation:lbIn .22s ease;cursor:default}
            @keyframes lbIn{from{opacity:0;transform:scale(.9)}to{opacity:1;transform:scale(1)}}
            #lb-close{position:fixed;top:18px;right:24px;color:#fff;font-size:34px;
                      cursor:pointer;opacity:.7;transition:opacity .15s;
                      background:none;border:none;padding:4px 8px;line-height:1}
            #lb-close:hover{opacity:1}
            #lb-caption{position:fixed;bottom:0;left:0;right:0;
                        background:linear-gradient(transparent,rgba(0,0,0,.75));
                        color:#fff;text-align:center;padding:24px 16px 18px;
                        font-size:13px;pointer-events:none}

            /* ════ PAGINATION ════ */
            .pagination{display:flex;justify-content:center;gap:5px;
                        margin-top:28px;flex-wrap:wrap}
            .btn{padding:7px 14px;border:1px solid #e2e8f0;background:#fff;
                 border-radius:6px;cursor:pointer;font-size:13px;font-weight:500;
                 color:#374151;transition:all .15s;line-height:1}
            .btn:hover:not(:disabled){background:#f1f5f9;border-color:#94a3b8}
            .btn:disabled{opacity:.35;cursor:not-allowed}
            .btn.active{background:#1e293b;color:#fff;border-color:#1e293b}

            /* ════ RESPONSIVE ════ */
            @media(max-width:900px){.grid{grid-template-columns:repeat(2,1fr)}}
            @media(max-width:580px){.grid{grid-template-columns:1fr}}
          </style>
        </head>
        <body>

        <!-- Lightbox -->
        <div id="lightbox" onclick="closeLb(event)">
          <button id="lb-close" onclick="closeLb()">&#10005;</button>
          <img id="lb-img" src="" alt="">
          <div id="lb-caption"></div>
        </div>

        <!-- Header -->
        <header>
          <div class="h-top">
            <div>
              <div class="logo">&#9889; BaqueanoAutoTest</div>
              <div class="subtitle">Reporte de Ejecución Autónoma — Responsive Testing</div>
              <div class="run-date">Ejecutado: @@RUN_DATE@@</div>
            </div>
          </div>
          <div class="summary">
            <div class="stat s-total"><span class="num">@@TOTAL@@</span><span class="lbl">Total</span></div>
            <div class="stat s-pass" ><span class="num">@@PASS@@</span> <span class="lbl">PASS</span></div>
            <div class="stat s-fail" ><span class="num">@@FAIL@@</span> <span class="lbl">FAIL</span></div>
            <div class="stat s-rate" ><span class="num">@@RATE@@%</span><span class="lbl">Éxito</span></div>
          </div>
        </header>

        <!-- Device filter tabs -->
        <div class="device-tabs" id="deviceTabs"></div>

        <main>
          <!-- Context line -->
          <div class="ctx">
            <span class="ctx-left" id="ctxInfo"></span>
            <span class="ctx-right" id="ctxStats"></span>
          </div>

          <!-- Grid -->
          <div id="grid" class="grid"></div>
          <div class="pagination" id="pager"></div>
        </main>

        <script>
          const ITEMS = @@ITEMS_JSON@@;
          const TAB = 'TAB-', MOB = 'MOB-';
          const PS  = 9;
          let pg = 0, filter = 'all';

          /* ── helpers ─────────────────────────────────────────────────────── */
          const esc   = s => String(s??'').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
          const escJs = s => String(s??'').replace(/\\/g,'\\\\').replace(/'/g,"\\'").replace(/\n/g,' ');

          function getItems(phase) {
            switch(phase) {
              case 'tablet':  return ITEMS.filter(i =>  i.testName.startsWith(TAB));
              case 'mobile':  return ITEMS.filter(i =>  i.testName.startsWith(MOB));
              case 'desktop': return ITEMS.filter(i => !i.testName.startsWith(TAB) && !i.testName.startsWith(MOB));
              default:        return ITEMS;
            }
          }

          /* ── Device tab bar ──────────────────────────────────────────────── */
          const PHASES = [
            { id:'all',     icon:'📋', label:'Todos' },
            { id:'desktop', icon:'🖥️', label:'PC / Desktop' },
            { id:'tablet',  icon:'📟', label:'Tablet' },
            { id:'mobile',  icon:'📱', label:'Celular' },
          ];

          function rateClass(pass, total) {
            if (!total)        return '';
            if (pass===total)  return 'style="color:#16a34a"';
            if (pass>=total*.5)return 'style="color:#f97316"';
            return 'style="color:#dc2626"';
          }

          function buildTabs() {
            const tabItems  = getItems('tablet');
            const mobItems  = getItems('mobile');
            const deskItems = getItems('desktop');
            const multiPhase = tabItems.length>0 || mobItems.length>0;

            // If only desktop results — hide the tab bar entirely
            if (!multiPhase) {
              document.getElementById('deviceTabs').style.display='none';
              return;
            }

            let html = '';
            for (const p of PHASES) {
              const its   = getItems(p.id);
              if (p.id !== 'all' && its.length === 0) continue;
              const pass  = its.filter(i => i.passed).length;
              const fail  = its.length - pass;
              const rate  = its.length ? Math.round(pass/its.length*100) : 0;
              const act   = filter===p.id ? 'active' : '';
              html += `<button class="dtab ${act}" onclick="setFilter('${p.id}')">
                <span class="dtab-icon">${p.icon}</span>
                <span class="dtab-label">${p.label}</span>
                <div class="dtab-scores">
                  <span class="ds-pass">✅ ${pass}</span>
                  <span class="ds-sep">|</span>
                  <span class="ds-fail">❌ ${fail}</span>
                </div>
                <span class="dtab-rate" ${rateClass(pass,its.length)}>${rate}%</span>
              </button>`;
            }
            document.getElementById('deviceTabs').innerHTML = html;
          }

          /* ── Filter ──────────────────────────────────────────────────────── */
          function setFilter(f) {
            filter = f;
            pg = 0;
            buildTabs();
            render();
          }

          /* ── Card ────────────────────────────────────────────────────────── */
          function card(item) {
            const isTab = item.testName.startsWith(TAB);
            const isMob = item.testName.startsWith(MOB);
            const vBadge = isTab
              ? '<span class="badge b-tab">📟 Tablet</span>'
              : isMob
                ? '<span class="badge b-mob">📱 Celular</span>'
                : '<span class="badge b-desk">🖥️ Desktop</span>';

            const imgHtml = item.imagePath
              ? `<img class="card-img"
                      src="${esc(item.imagePath)}"
                      alt="${esc(item.testName)}"
                      loading="lazy"
                      onclick="openLb('${escJs(item.imagePath)}','${escJs(item.testName)} | ${escJs(item.category)} | ${item.passed?'PASS':'FAIL'} | ${escJs(item.executedAt)}')"
                      onerror="this.parentElement.innerHTML='<div class=\'no-img\'>Sin imagen</div>'">`
              : `<div class="no-img">Sin captura</div>`;

            return `
              <div class="card ${item.passed?'c-pass':'c-fail'}">
                <div class="card-img-wrap">${imgHtml}</div>
                <div class="card-body">
                  <div class="card-title">${esc(item.testName)}</div>
                  <div class="badges">
                    <span class="badge b-cat">${esc(item.category)}</span>
                    <span class="badge ${item.passed?'b-pass':'b-fail'}">${item.passed?'PASS':'FAIL'}</span>
                    ${vBadge}
                  </div>
                  <div class="card-msg">${esc(item.message)}</div>
                  <div class="card-date">${esc(item.executedAt)}</div>
                </div>
              </div>`;
          }

          /* ── Render ──────────────────────────────────────────────────────── */
          function render() {
            const items = getItems(filter);
            const tp  = Math.ceil(items.length / PS);
            const s   = pg * PS;
            const e   = Math.min(s + PS, items.length);

            // Context bar
            const phaseLabel = PHASES.find(p=>p.id===filter)?.label ?? 'Todos';
            const ctxPass    = items.filter(i=>i.passed).length;
            document.getElementById('ctxInfo').textContent =
              items.length
                ? `Vista: ${phaseLabel}  ·  Mostrando ${s+1}–${e} de ${items.length} tests`
                : `Vista: ${phaseLabel}  ·  Sin resultados`;
            document.getElementById('ctxStats').textContent =
              items.length
                ? `✅ ${ctxPass} PASS  ❌ ${items.length-ctxPass} FAIL`
                : '';

            // Grid
            document.getElementById('grid').innerHTML =
              items.slice(s, e).map(card).join('');

            // Pagination
            let from = Math.max(0, pg-3);
            let to   = Math.min(tp-1, from+6);
            if (to-from < 6) from = Math.max(0, to-6);
            let h = `<button class="btn" onclick="go(pg-1)" ${pg===0?'disabled':''}>&#171; Anterior</button>`;
            for (let i = from; i <= to; i++)
              h += `<button class="btn ${i===pg?'active':''}" onclick="go(${i})">${i+1}</button>`;
            h += `<button class="btn" onclick="go(pg+1)" ${pg===tp-1||tp===0?'disabled':''}>Siguiente &#187;</button>`;
            document.getElementById('pager').innerHTML = h;
          }

          function go(p) {
            pg = Math.max(0, Math.min(Math.ceil(getItems(filter).length/PS)-1, p));
            window.scrollTo({top:0,behavior:'smooth'});
            render();
          }

          /* ── Lightbox ────────────────────────────────────────────────────── */
          function openLb(src, caption) {
            document.getElementById('lb-img').src = src;
            document.getElementById('lb-img').alt = caption;
            document.getElementById('lb-caption').textContent = caption;
            document.getElementById('lightbox').classList.add('open');
            document.body.style.overflow = 'hidden';
          }
          function closeLb(e) {
            if (e && e.target === document.getElementById('lb-img')) return;
            document.getElementById('lightbox').classList.remove('open');
            document.body.style.overflow = '';
          }
          document.addEventListener('keydown', e => { if(e.key==='Escape') closeLb(); });

          /* ── Init ────────────────────────────────────────────────────────── */
          buildTabs();
          render();
        </script>
        </body>
        </html>
        """;
}
