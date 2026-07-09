using System.Net;
using Microsoft.AspNetCore.Mvc;
using ToDoList.Data.Repositories;

namespace ToDoList.Api.Controllers
{
    /// <summary>
    /// Localhost-only diagnostics viewer over the sync audit log, device
    /// registry, and the debug.log file. Every action 404s for non-loopback
    /// callers — the API has no auth, so this is the access control. View it
    /// on the server box or through an SSH tunnel.
    /// </summary>
    [ApiController]
    [Route("admin/logs")]
    public class LogViewerController : ControllerBase
    {
        private static readonly string debugLogPath =
            Path.Combine(AppContext.BaseDirectory, "debug.log");

        private readonly SyncLogRepository syncLogRepository;
        private readonly DeviceRepository deviceRepository;

        public LogViewerController(SyncLogRepository syncLogRepository, DeviceRepository deviceRepository)
        {
            this.syncLogRepository = syncLogRepository;
            this.deviceRepository = deviceRepository;
        }

        private bool IsLocal()
        {
            IPAddress? remote = HttpContext.Connection.RemoteIpAddress;
            return remote != null && IPAddress.IsLoopback(remote);
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!IsLocal())
            {
                return NotFound();
            }

            return new ContentResult
            {
                ContentType = "text/html",
                Content = ViewerHtml
            };
        }

        [HttpGet("data")]
        public async Task<IActionResult> Data(
            [FromQuery] string? deviceId,
            [FromQuery] string? action,
            [FromQuery] DateTime? from,
            [FromQuery] DateTime? to,
            [FromQuery] int limit = 200)
        {
            if (!IsLocal())
            {
                return NotFound();
            }

            limit = Math.Clamp(limit, 1, 2000);
            var entries = await syncLogRepository.QueryAsync(deviceId, action, from, to, limit);
            return Ok(entries);
        }

        [HttpGet("devices")]
        public async Task<IActionResult> Devices()
        {
            if (!IsLocal())
            {
                return NotFound();
            }

            return Ok(await deviceRepository.GetAllAsync());
        }

        [HttpGet("debug")]
        public IActionResult DebugLog([FromQuery] int lines = 200)
        {
            if (!IsLocal())
            {
                return NotFound();
            }

            lines = Math.Clamp(lines, 1, 5000);
            if (!System.IO.File.Exists(debugLogPath))
            {
                return Content(string.Empty, "text/plain");
            }

            // Tail the file. The debug log is capped at 10 MB by rotation, so
            // reading it whole is bounded.
            string[] all = System.IO.File.ReadAllLines(debugLogPath);
            IEnumerable<string> tail = all.Length > lines ? all[^lines..] : all;
            return Content(string.Join("\n", tail), "text/plain");
        }

        private const string ViewerHtml = """
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>YepList — Sync Diagnostics</title>
<style>
  :root { color-scheme: light dark; }
  body { font: 14px/1.5 system-ui, sans-serif; margin: 0; padding: 1rem; }
  h1 { font-size: 1.2rem; margin: 0 0 .75rem; }
  .controls { display: flex; flex-wrap: wrap; gap: .5rem; align-items: end; margin-bottom: 1rem; }
  .controls label { display: flex; flex-direction: column; font-size: .75rem; opacity: .8; }
  input, select, button { font: inherit; padding: .3rem .4rem; }
  .tabs { display: flex; gap: .25rem; margin-bottom: .5rem; }
  .tabs button { cursor: pointer; }
  .tabs button.active { font-weight: 600; text-decoration: underline; }
  table { border-collapse: collapse; width: 100%; font-size: 12px; }
  th, td { border: 1px solid #8884; padding: .25rem .4rem; text-align: left; white-space: nowrap; }
  th { position: sticky; top: 0; background: #8882; }
  .wrap { overflow-x: auto; }
  .stale { color: #c0392b; font-weight: 600; }
  .pull { opacity: .75; }
  pre { font-size: 12px; overflow-x: auto; border: 1px solid #8884; padding: .5rem; }
  .muted { opacity: .6; }
</style>
</head>
<body>
<h1>YepList — Sync Diagnostics <span class="muted" id="status"></span></h1>
<div class="tabs">
  <button data-tab="log" class="active">Sync log</button>
  <button data-tab="debug">Debug log</button>
</div>

<div id="tab-log">
  <div class="controls">
    <label>Device <select id="device"><option value="">(all)</option></select></label>
    <label>Action
      <select id="action">
        <option value="">(all)</option>
        <option>pull</option><option>create</option><option>update</option>
        <option>toggle</option><option>delete</option><option>reorder</option>
      </select>
    </label>
    <label>Since <input type="datetime-local" id="from"></label>
    <label>Limit <input type="number" id="limit" value="200" min="1" max="2000" style="width:6rem"></label>
    <button id="refresh">Refresh</button>
    <label style="flex-direction:row;gap:.3rem;align-items:center">
      <input type="checkbox" id="auto"> auto (5s)
    </label>
  </div>
  <div class="wrap">
    <table>
      <thead><tr>
        <th>Time (UTC)</th><th>Device</th><th>Action</th><th>Entity</th><th>Id</th>
        <th>Result</th><th>Since</th><th>Lists</th><th>Items</th><th>Cats</th><th>Del</th><th>Detail</th>
      </tr></thead>
      <tbody id="rows"></tbody>
    </table>
  </div>
</div>

<div id="tab-debug" style="display:none">
  <div class="controls">
    <label>Lines <input type="number" id="dlines" value="200" min="1" max="5000" style="width:6rem"></label>
    <button id="drefresh">Refresh</button>
  </div>
  <pre id="debug"></pre>
</div>

<script>
const $ = s => document.querySelector(s);
const fmt = v => v == null ? '' : String(v);

async function loadDevices() {
  const r = await fetch('admin/logs/devices');
  const devices = await r.json();
  const sel = $('#device');
  for (const d of devices) {
    const o = document.createElement('option');
    o.value = d.deviceId;
    o.textContent = (d.name || d.deviceId) + (d.platform ? ' (' + d.platform + ')' : '');
    sel.appendChild(o);
  }
}

async function loadLog() {
  const p = new URLSearchParams();
  if ($('#device').value) p.set('deviceId', $('#device').value);
  if ($('#action').value) p.set('action', $('#action').value);
  if ($('#from').value) p.set('from', new Date($('#from').value).toISOString());
  p.set('limit', $('#limit').value || '200');
  const r = await fetch('admin/logs/data?' + p.toString());
  const rows = await r.json();
  const tb = $('#rows');
  tb.innerHTML = '';
  for (const e of rows) {
    const tr = document.createElement('tr');
    if (e.result === 'stale') tr.className = 'stale';
    else if (e.action === 'pull') tr.className = 'pull';
    const cells = [
      (e.createdAt || '').replace('T',' ').replace(/\..*/,''),
      e.deviceName, e.action, e.entityType, e.entityId, e.result,
      e.sinceValue ? e.sinceValue.replace('T',' ').replace(/\..*/,'') : '',
      e.listsCount, e.itemsCount, e.categoriesCount, e.deletedCount, e.detail
    ];
    for (const c of cells) {
      const td = document.createElement('td');
      td.textContent = fmt(c);
      tr.appendChild(td);
    }
    tb.appendChild(tr);
  }
  $('#status').textContent = '· ' + rows.length + ' rows · ' + new Date().toLocaleTimeString();
}

async function loadDebug() {
  const r = await fetch('admin/logs/debug?lines=' + ($('#dlines').value || '200'));
  $('#debug').textContent = await r.text();
}

$('#refresh').onclick = loadLog;
$('#drefresh').onclick = loadDebug;
let timer = null;
$('#auto').onchange = e => {
  if (e.target.checked) { timer = setInterval(loadLog, 5000); } else { clearInterval(timer); }
};
document.querySelectorAll('.tabs button').forEach(b => b.onclick = () => {
  document.querySelectorAll('.tabs button').forEach(x => x.classList.remove('active'));
  b.classList.add('active');
  const tab = b.dataset.tab;
  $('#tab-log').style.display = tab === 'log' ? '' : 'none';
  $('#tab-debug').style.display = tab === 'debug' ? '' : 'none';
  if (tab === 'debug') loadDebug();
});

loadDevices();
loadLog();
</script>
</body>
</html>
""";
    }
}
