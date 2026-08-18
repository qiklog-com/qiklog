/**
 * Server-side demo ingest proxy. The browser never sees the API key.
 * Env: QIKLOG_DEMO_API_KEY (required), QIKLOG_INGEST_URL (optional).
 */
const MAX_LENGTH = 120;
const MIN_INTERVAL_MS = 2000;
const hits = new Map();

function sanitize(raw) {
  if (!raw || typeof raw !== 'string') return '';
  let out = '';
  for (const ch of raw.trim()) {
    const code = ch.charCodeAt(0);
    if (code <= 0x1f || code === 0x7f) continue;
    out += ch;
    if (out.length >= MAX_LENGTH) break;
  }
  return out;
}

function clientKey(req) {
  const forwarded = req.headers['x-forwarded-for'];
  if (typeof forwarded === 'string' && forwarded.length > 0)
    return forwarded.split(',')[0].trim();
  return req.socket?.remoteAddress ?? 'unknown';
}

function rateLimited(key) {
  const now = Date.now();
  const last = hits.get(key) ?? 0;
  if (now - last < MIN_INTERVAL_MS) return true;
  hits.set(key, now);
  if (hits.size > 2000) {
    for (const [k, at] of hits) {
      if (now - at > 60_000) hits.delete(k);
    }
  }
  return false;
}

export default async function handler(req, res) {
  if (req.method !== 'POST') {
    res.setHeader('Allow', 'POST');
    return res.status(405).json({ error: 'method not allowed' });
  }

  if (rateLimited(clientKey(req)))
    return res.status(429).json({ error: 'rate limited' });

  const key = process.env.QIKLOG_DEMO_API_KEY;
  if (!key)
    return res.status(503).json({ error: 'demo ingest is not configured' });

  const body = typeof req.body === 'string' ? JSON.parse(req.body || '{}') : req.body ?? {};
  const message = sanitize(body.message);
  if (!message)
    return res.status(400).json({ error: 'message required' });

  const ingestUrl = process.env.QIKLOG_INGEST_URL ?? 'https://api.qiklog.com/v1/logs';
  const upstream = await fetch(ingestUrl, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${key}`,
    },
    body: JSON.stringify({
      source: 'demo',
      level: 'info',
      message,
    }),
  });

  if (upstream.status !== 202)
    return res.status(502).json({ error: 'ingest rejected' });

  return res.status(202).json({ ok: true, message });
}
