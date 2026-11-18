namespace Tezgah.Api.Infrastructure;

/// <summary>"Gelen istekler" sayfası: tek dosya, bağımlılıksız. Dinamik değerler JS'te textContent ile yazılır (HTML enjeksiyonu yok).</summary>
public static class RequestLogPage
{
    public const string Html = """
        <!DOCTYPE html>
        <html lang="tr">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <title>Tezgah API · gelen istekler</title>
          <style>
            body { font-family: system-ui, sans-serif; margin: 0; padding: 1.5rem; background: #f5f6f8; color: #1b2a3a; }
            h1 { font-size: 1.2rem; margin: 0 0 1rem; }
            .filters { display: flex; gap: 1.2rem; align-items: center; margin-bottom: 1rem; flex-wrap: wrap; }
            input[type=search] { padding: .35rem .5rem; min-width: 16rem; }
            table { border-collapse: collapse; width: 100%; background: #fff; font-size: .9rem; }
            th, td { text-align: left; padding: .35rem .6rem; border-bottom: 1px solid #e3e6ea; vertical-align: top; }
            th { background: #eef1f5; }
            td.num { text-align: right; font-variant-numeric: tabular-nums; }
            .addr { font-family: ui-monospace, Consolas, monospace; word-break: break-all; }
            .badge { display: inline-block; min-width: 2.4rem; text-align: center; padding: .05rem .4rem; border-radius: 4px; color: #fff; font-weight: 600; }
            .s2 { background: #1a7f37; } .s3 { background: #6b7280; } .s4 { background: #d97706; } .s5 { background: #b42318; }
            summary { cursor: pointer; }
            pre { background: #fdf2f2; padding: .5rem; overflow-x: auto; font-size: .8rem; margin: .4rem 0 0; white-space: pre-wrap; }
            .empty, .note { color: #667; font-size: .85rem; }
            .note { margin-top: 1rem; }
          </style>
        </head>
        <body>
          <h1>Tezgah API · gelen istekler</h1>
          <div class="filters">
            <label><input type="checkbox" id="onlyErrors"> Yalnızca hatalar (4xx/5xx)</label>
            <label>Adres ara: <input type="search" id="search" placeholder="/api/..."></label>
          </div>
          <table>
            <thead>
              <tr><th>Saat</th><th>Yöntem</th><th>Adres</th><th>Durum</th><th>Süre</th><th>SQL</th><th>Kullanıcı</th></tr>
            </thead>
            <tbody id="rows"></tbody>
          </table>
          <p class="empty" id="empty" hidden>Gösterilecek istek yok.</p>
          <p class="note">
            Bu ekran yalnızca geliştirme ortamında açıktır. Ayrıntılı log: API'nin çalıştığı terminal.
            İstek göndermek için <code>backend/src/Tezgah.Api/Tezgah.Api.http</code> dosyasına bakın.
          </p>
          <script>
            const rows = document.getElementById('rows');
            const empty = document.getElementById('empty');
            const onlyErrors = document.getElementById('onlyErrors');
            const search = document.getElementById('search');
            const fmt = new Intl.DateTimeFormat('tr-TR', { timeZone: 'Europe/Istanbul', hour: '2-digit', minute: '2-digit', second: '2-digit' });
            const opened = new Set();
            let data = [];

            const keyOf = e => e.time + '|' + e.method + '|' + e.path;

            function cell(tr, text, cls) {
              const td = document.createElement('td');
              td.textContent = text;
              if (cls) td.className = cls;
              tr.appendChild(td);
              return td;
            }

            function render() {
              const q = search.value.trim().toLowerCase();
              const list = data.filter(e =>
                (!onlyErrors.checked || e.status >= 400) &&
                (!q || e.path.toLowerCase().includes(q)));
              rows.replaceChildren();
              for (const e of list) {
                const tr = document.createElement('tr');
                cell(tr, fmt.format(new Date(e.time)));
                cell(tr, e.method);
                const addr = cell(tr, '', 'addr');
                if (e.exceptionType) {
                  const d = document.createElement('details');
                  const k = keyOf(e);
                  d.open = opened.has(k);
                  d.addEventListener('toggle', () => { d.open ? opened.add(k) : opened.delete(k); });
                  const s = document.createElement('summary');
                  s.textContent = e.path;
                  const pre = document.createElement('pre');
                  pre.textContent = e.exceptionType + ': ' + e.exceptionMessage + '\n' + (e.stackTrace ?? '');
                  d.append(s, pre);
                  addr.appendChild(d);
                } else {
                  addr.textContent = e.path;
                }
                const st = cell(tr, '');
                const b = document.createElement('span');
                b.className = 'badge s' + Math.floor(e.status / 100);
                b.textContent = e.status;
                st.appendChild(b);
                cell(tr, e.durationMs.toFixed(1) + ' ms', 'num');
                cell(tr, e.sqlCount, 'num');
                cell(tr, e.user ?? '');
                rows.appendChild(tr);
              }
              empty.hidden = list.length > 0;
            }

            async function load() {
              try {
                const res = await fetch('/_istekler');
                if (res.ok) { data = await res.json(); render(); }
              } catch { /* sunucu kapalıysa bir sonraki turda tekrar denenir */ }
            }

            onlyErrors.addEventListener('change', render);
            search.addEventListener('input', render);
            load();
            setInterval(load, 3000);
          </script>
        </body>
        </html>
        """;
}
