/**
 * Tiny syntax highlighter for the landing page code samples (curl, C#, JavaScript).
 * No dependency: the samples are short and fixed in shape, and the visitor's message is
 * spliced into them, so every token is HTML-escaped before it is wrapped in a span.
 * Output classes: tok-cmd, tok-flag, tok-url, tok-str, tok-key, tok-kw, tok-type,
 * tok-num, tok-punct, tok-var.
 */
export type HighlightLang = 'curl' | 'csharp' | 'js';

const KEYWORDS: Record<HighlightLang, Set<string>> = {
  curl: new Set(),
  csharp: new Set(['using', 'var', 'new', 'await', 'true', 'false', 'null']),
  js: new Set(['await', 'const', 'let', 'new', 'true', 'false', 'null', 'function', 'return']),
};

const escapeHtml = (s: string): string =>
  s.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

const span = (cls: string, text: string): string => `<span class="tok-${cls}">${escapeHtml(text)}</span>`;

/** Reads a quoted string starting at i (handles backslash escapes). Returns end index (exclusive). */
function readString(code: string, i: number): number {
  const quote = code[i];
  let j = i + 1;
  while (j < code.length && code[j] !== quote) {
    if (code[j] === '\\' && j + 1 < code.length) j += 2;
    else j += 1;
  }
  return Math.min(j + 1, code.length);
}

/** Highlights a JSON fragment (used inside curl's -d '...'): keys vs values. */
function highlightJson(json: string): string {
  let out = '';
  let i = 0;
  while (i < json.length) {
    const c = json[i];
    if (c === '"') {
      const end = readString(json, i);
      const after = json.slice(end).match(/^\s*:/);
      out += span(after ? 'key' : 'str', json.slice(i, end));
      i = end;
    } else if (/[{}[\],:]/.test(c)) {
      out += span('punct', c);
      i += 1;
    } else if (/[-0-9]/.test(c)) {
      const m = json.slice(i).match(/^-?\d+(\.\d+)?/);
      const text = m ? m[0] : c;
      out += m ? span('num', text) : escapeHtml(text);
      i += text.length;
    } else {
      out += escapeHtml(c);
      i += 1;
    }
  }
  return out;
}

export function highlight(code: string, lang: HighlightLang): string {
  const keywords = KEYWORDS[lang];
  let out = '';
  let i = 0;
  let atLineStart = true;

  while (i < code.length) {
    const rest = code.slice(i);
    const c = code[i];

    if (c === '\n') {
      out += '\n';
      i += 1;
      atLineStart = true;
      continue;
    }

    if (c === ' ' || c === '\t') {
      out += c;
      i += 1;
      continue;
    }

    // Strings. curl's single-quoted -d body is JSON: highlight inside it.
    if (c === '"' || c === "'" || c === '`') {
      const end = readString(code, i);
      const text = code.slice(i, end);
      if (lang === 'curl' && c === "'" && /^'\s*[{[]/.test(text)) {
        const closed = text.length > 1 && text.endsWith("'");
        out += span('str', "'") + highlightJson(text.slice(1, closed ? -1 : undefined)) + (closed ? span('str', "'") : '');
      } else {
        const isKey = lang === 'js' && /^\s*:/.test(code.slice(end));
        out += span(isKey ? 'key' : 'str', text);
      }
      i = end;
      atLineStart = false;
      continue;
    }

    // Shell variables: $QIKLOG_API_KEY
    let m = rest.match(/^\$[A-Za-z_][A-Za-z0-9_]*/);
    if (m && lang === 'curl') {
      out += span('var', m[0]);
      i += m[0].length;
      atLineStart = false;
      continue;
    }

    m = rest.match(/^https?:\/\/[^\s"'`\\]+/);
    if (m) {
      out += span('url', m[0]);
      i += m[0].length;
      atLineStart = false;
      continue;
    }

    if (lang === 'curl') {
      m = rest.match(/^--?[A-Za-z][A-Za-z-]*/);
      if (m) {
        out += span('flag', m[0]);
        i += m[0].length;
        atLineStart = false;
        continue;
      }
    }

    m = rest.match(/^[A-Za-z_][A-Za-z0-9_]*/);
    if (m) {
      const word = m[0];
      const next = code.slice(i + word.length);
      let cls: string | null = null;
      if (lang === 'curl' && atLineStart && word === 'curl') cls = 'cmd';
      else if (keywords.has(word)) cls = 'kw';
      else if (lang === 'js' && /^\s*:/.test(next)) cls = 'key';
      else if (lang === 'csharp' && /^\s*=(?!=)/.test(next) && /^[a-z]/.test(word)) cls = 'key';
      else if (lang === 'csharp' && /^[A-Z]/.test(word)) cls = 'type';
      else if (/^\s*\(/.test(next)) cls = 'cmd';
      out += cls ? span(cls, word) : escapeHtml(word);
      i += word.length;
      atLineStart = false;
      continue;
    }

    m = rest.match(/^\d+(\.\d+)?/);
    if (m) {
      out += span('num', m[0]);
      i += m[0].length;
      atLineStart = false;
      continue;
    }

    out += /[{}()[\];,.=:<>\\]/.test(c) ? span('punct', c) : escapeHtml(c);
    i += 1;
    atLineStart = false;
  }

  return out;
}

/** Maps a code-deck tab id to its highlighter language. */
export function langForTab(tabId: string): HighlightLang {
  if (tabId === 'csharp') return 'csharp';
  if (tabId === 'javascript') return 'js';
  return 'curl';
}
