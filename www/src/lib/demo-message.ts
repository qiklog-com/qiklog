/** Keep in lockstep with tests/QikLog.Core.Tests/DemoMessage.cs and www/api/demo-send.js */
export const MAX_LENGTH = 120;

export function sanitize(raw: string | null | undefined): string {
  if (!raw) return '';
  let out = '';
  for (const ch of raw.trim()) {
    const code = ch.charCodeAt(0);
    if (code <= 0x1f || code === 0x7f) continue;
    out += ch;
    if (out.length >= MAX_LENGTH) break;
  }
  return out;
}

export function escapeJson(value: string): string {
  return value
    .replaceAll('\\', '\\\\')
    .replaceAll('"', '\\"')
    .replaceAll('\n', '\\n')
    .replaceAll('\r', '\\r')
    .replaceAll('\t', '\\t');
}

export function escapeCSharp(value: string): string {
  return escapeJson(value);
}

export function escapeJavaScript(value: string): string {
  return escapeJson(value);
}

export function escapeFor(kind: 'json' | 'csharp' | 'js', value: string): string {
  if (kind === 'csharp') return escapeCSharp(value);
  if (kind === 'js') return escapeJavaScript(value);
  return escapeJson(value);
}
