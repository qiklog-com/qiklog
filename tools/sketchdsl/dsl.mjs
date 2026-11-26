#!/usr/bin/env node
// sketchdsl: a tiny text format for "explainer" diagrams.
// Usage: node dsl.mjs input.dsl -o out.svg [--view flat|iso] [--theme paper|cloud|ink]
import fs from 'node:fs';

// ───────────────────────── parse ─────────────────────────

const KINDS = new Set(['user', 'server', 'app', 'browser', 'lock', 'db', 'registry', 'vault',
  'identity', 'ci', 'cloud', 'globe', 'terminal', 'container', 'laptop']);

function readQuoted(line, i) {
  let s = '';
  let j = i + 1;
  while (j < line.length && line[j] !== '"') {
    if (line[j] === '\\') { s += line[j + 1] === 'n' ? '\n' : line[j + 1]; j += 2; continue; }
    s += line[j++];
  }
  return [s, j + 1];
}

function tokenize(line) {
  const out = [];
  let i = 0;
  while (i < line.length) {
    if (/\s/.test(line[i])) { i++; continue; }
    if (line[i] === '"') { const [s, j] = readQuoted(line, i); out.push({ str: s }); i = j; continue; }
    let w = '';
    while (i < line.length && !/\s/.test(line[i])) {
      if (line[i] === '"') { const [s, j] = readQuoted(line, i); w += s; i = j; continue; }
      w += line[i++];
    }
    out.push({ word: w });
  }
  return out;
}

const pos = (s) => { const [c, r] = s.replace(/^@/, '').split(',').map(Number); return { c, r }; };

function readOpts(toks) {
  const o = { opts: {}, flags: {} };
  for (let i = 0; i < toks.length; i++) {
    const t = toks[i];
    if (t.str !== undefined) { if (o.label === undefined) o.label = t.str; continue; }
    const w = t.word;
    if (w.startsWith('@')) o.at = pos(w);
    else if (w === 'to') o.to = pos(toks[++i].word);
    else if (/^#\d+$/.test(w)) o.step = Number(w.slice(1));
    else if (w.includes('=')) { const k = w.slice(0, w.indexOf('=')); o.opts[k] = w.slice(w.indexOf('=') + 1); }
    else o.flags[w] = true;
  }
  return o;
}

const newPanel = () => ({ caption: '', nodes: [], edges: [], groups: [], notes: [] });

export function parse(src) {
  const d = { title: '', subtitle: '', accent: [], view: 'flat', theme: null, titlepos: 'top', cell: null, panels: [], card: null };
  let section = null;
  let panel = null;
  let column = null;
  const ensurePanel = () => { if (!panel) { panel = newPanel(); d.panels.push(panel); } return panel; };

  src.split('\n').forEach((raw, idx) => {
    const line = raw.replace(/\s+#\s.*$/, '');
    if (!line.trim() || /^\s*#(\s|$)/.test(line)) return;
    const indent = line.match(/^\s*/)[0].length;
    const text = line.trim();
    const toks = tokenize(text);
    const head = toks[0].word ?? toks[0].str;
    const fail = (m) => { throw new Error(`line ${idx + 1}: ${m}\n  ${raw}`); };

    if (indent === 0 && ['diagram', 'panel', 'card'].includes(head)) {
      section = head;
      column = null;
      if (head === 'diagram') d.title = toks[1]?.str ?? '';
      if (head === 'panel') { panel = newPanel(); d.panels.push(panel); }
      if (head === 'card') d.card = { style: toks[1]?.word ?? 'terminal', columns: [] };
      return;
    }

    if (section === 'diagram' && indent > 0) {
      const rest = toks.slice(1);
      if (head === 'subtitle') d.subtitle = rest[0]?.str ?? '';
      else if (head === 'accent') d.accent = rest.map((t) => t.str ?? t.word);
      else if (head === 'view') d.view = rest[0].word;
      else if (head === 'theme') { d.theme = rest[0].word; Object.assign(d, { themeOpts: readOpts(rest.slice(1)).opts }); }
      else if (head === 'title') d.titlepos = rest[0].word;
      else if (head === 'cell') d.cell = rest[0].word.split(',').map(Number);
      else fail(`unknown diagram setting "${head}"`);
      return;
    }

    if (section === 'card') {
      if (head === 'column') { column = { title: toks[1]?.str ?? '', items: [] }; d.card.columns.push(column); return; }
      if (!column) fail('card content needs a column first');
      if (text.startsWith('- ')) column.items.push({ type: 'bullet', text: text.slice(2) });
      else if (/^\d+\.\s/.test(text)) column.items.push({ type: 'step', n: text.match(/^\d+/)[0], text: text.replace(/^\d+\.\s+/, '') });
      else if (head === 'comment') column.items.push({ type: 'comment', text: toks[1]?.str ?? '' });
      else if (head === 'token') {
        const parts = text.slice(5).split('|').map((p) => tokenize(p.trim()));
        column.items.push({ type: 'token', parts: parts.map((p) => { const [t, color] = p[0].word.split(':'); return { text: t, color, label: p[1]?.str ?? '' }; }) });
      } else fail(`unknown card item "${head}"`);
      return;
    }

    // panel content (explicit panel, or top-level nodes make an implicit one)
    const p = ensurePanel();
    if (head === 'caption') { p.caption = toks[1]?.str ?? ''; return; }
    if (head === 'note') { const o = readOpts(toks.slice(1)); p.notes.push({ text: o.label, at: o.at, opts: o.opts }); return; }
    if (head === 'group') {
      const o = readOpts(toks.slice(2));
      if (!o.at) fail('group needs @col,row');
      p.groups.push({ id: toks[1].word, label: o.label ?? '', from: o.at, to: o.to ?? o.at, opts: o.opts, flags: o.flags });
      return;
    }
    if (toks[1] && ['->', '<->', '--'].includes(toks[1].word)) {
      const o = readOpts(toks.slice(3));
      p.edges.push({ from: head, to: toks[2].word, op: toks[1].word, label: o.label ?? '', step: o.step, opts: o.opts, flags: o.flags });
      return;
    }
    if (KINDS.has(head)) {
      const o = readOpts(toks.slice(2));
      if (!o.at) fail('node needs @col,row');
      p.nodes.push({ kind: head, id: toks[1].word, label: o.label ?? toks[1].word, ...o.at, opts: o.opts, flags: o.flags });
      return;
    }
    fail(`don't know what "${head}" is`);
  });

  for (const p of d.panels) {
    let n = 0;
    for (const e of p.edges) {
      if (e.flags.nonum) continue;
      if (e.step !== undefined) n = e.step; else e.step = ++n;
    }
  }
  return d;
}

// ───────────────────────── themes ─────────────────────────

const THEMES = {
  paper: {
    bg: '#f6efe4', ink: '#2b2b2b', muted: '#5a5248', accent: '#c8551b', edge: '#2b2b2b', grid: null,
    font: "'Patrick Hand', 'Comic Neue', cursive", titleFont: "'Fredoka', 'Patrick Hand', sans-serif", mono: "'Patrick Hand', monospace",
    fontImport: 'family=Patrick+Hand&family=Fredoka:wght@500;600', hand: true,
    noteFill: '#f9f4ec', cardBg: '#1f2123', cardText: '#ece6dc', cardAccent: '#f08a3c', cardDim: '#3a3c40',
    tints: { lavender: ['#e4ddf4'], mint: ['#dcefd9'], sky: ['#d9e8f6'], peach: ['#fbe5c6'], rose: ['#f6d8d6'], sand: ['#efe4cf'], teal: ['#d2efe9'], white: ['#ffffff'] },
    blockTop: '#ffffff', blockEdge: '#2b2b2b', badgeBg: '#c8551b', badgeFg: '#ffffff',
  },
  cloud: {
    bg: '#ffffff', ink: '#1f2328', muted: '#57606a', accent: '#0d9488', edge: '#6e7781', grid: '#e9edf1',
    font: "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif", titleFont: "'Inter', system-ui, sans-serif", mono: "'JetBrains Mono', ui-monospace, monospace",
    fontImport: 'family=Inter:wght@400;600;700&family=JetBrains+Mono:wght@500', hand: false,
    noteFill: '#f6f8fa', cardBg: '#0d1117', cardText: '#c9d1d9', cardAccent: '#2dd4bf', cardDim: '#30363d',
    tints: {
      teal: ['#ccfbf1', '#0d9488'], sky: ['#dbeafe', '#3b82f6'], lavender: ['#ede9fe', '#7c3aed'], mint: ['#dcfce7', '#16a34a'],
      peach: ['#ffedd5', '#ea580c'], sand: ['#fef3c7', '#d97706'], rose: ['#ffe4e6', '#e11d48'], slate: ['#f1f5f9', '#64748b'], white: ['#ffffff', '#8c959f'],
    },
    blockTop: '#ffffff', blockEdge: '#afb8c1', iconInk: '#24292f', badgeBg: '#f5b400', badgeFg: '#1f2328',
  },
  ink: {
    bg: '#0d1117', ink: '#f0f6fc', muted: '#8b949e', accent: '#2dd4bf', edge: '#8b949e', grid: '#161b22',
    font: "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif", titleFont: "'Inter', system-ui, sans-serif", mono: "'JetBrains Mono', ui-monospace, monospace",
    fontImport: 'family=Inter:wght@400;600;700&family=JetBrains+Mono:wght@500', hand: false,
    noteFill: '#161b22', cardBg: '#161b22', cardText: '#c9d1d9', cardAccent: '#2dd4bf', cardDim: '#30363d',
    tints: {
      teal: ['#134e4a', '#2dd4bf'], sky: ['#172554', '#60a5fa'], lavender: ['#2e1065', '#a78bfa'], mint: ['#14532d', '#4ade80'],
      peach: ['#431407', '#fb923c'], sand: ['#422006', '#fbbf24'], rose: ['#4c0519', '#fb7185'], slate: ['#1e293b', '#94a3b8'], white: ['#21262d', '#8b949e'],
    },
    blockTop: '#21262d', blockEdge: '#484f58', iconInk: '#e6edf3', badgeBg: '#2dd4bf', badgeFg: '#0d1117',
  },
};

// ───────────────────────── helpers ─────────────────────────

const f = (n) => String(Math.round(n * 10) / 10);
const esc = (s) => String(s).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');

function rng(seed) {
  return () => {
    seed = (seed + 0x6d2b79f5) | 0;
    let t = Math.imul(seed ^ (seed >>> 15), 1 | seed);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function shade(hex, amt) {
  const n = parseInt(hex.slice(1), 16);
  const ch = (v) => Math.max(0, Math.min(255, Math.round(amt < 0 ? v * (1 + amt) : v + (255 - v) * amt)));
  return '#' + [n >> 16, (n >> 8) & 255, n & 255].map((v) => ch(v).toString(16).padStart(2, '0')).join('');
}

class Bounds {
  constructor() { this.x0 = Infinity; this.y0 = Infinity; this.x1 = -Infinity; this.y1 = -Infinity; }
  add(x, y) { this.x0 = Math.min(this.x0, x); this.y0 = Math.min(this.y0, y); this.x1 = Math.max(this.x1, x); this.y1 = Math.max(this.y1, y); }
  rect(x, y, w, h) { this.add(x, y); this.add(x + w, y + h); }
}

function textWidth(s, size, th, mono = false) {
  const k = mono ? 0.6 : th.hand ? 0.46 : 0.56;
  return Math.max(...String(s).split('\n').map((l) => l.length)) * size * k;
}

// Multi-line text. opts: size, anchor, weight, fill, family, lh, firstWeight, firstFill, firstSize
function text(x, y, s, o) {
  const lines = String(s).split('\n');
  const lh = o.lh ?? o.size * 1.25;
  const spans = lines.map((l, i) => {
    const first = i === 0;
    const attrs = [
      `x="${f(x)}"`, i === 0 ? '' : `dy="${f(lh)}"`,
      first && o.firstWeight ? `font-weight="${o.firstWeight}"` : '',
      first && o.firstFill ? `fill="${o.firstFill}"` : '',
      first && o.firstSize ? `font-size="${o.firstSize}"` : '',
    ].filter(Boolean).join(' ');
    return `<tspan ${attrs}>${esc(l)}</tspan>`;
  }).join('');
  return `<text x="${f(x)}" y="${f(y)}" font-family="${esc(o.family)}" font-size="${o.size}" text-anchor="${o.anchor ?? 'start'}"`
    + `${o.weight ? ` font-weight="${o.weight}"` : ''} fill="${o.fill}"${o.extra ?? ''}>${spans}</text>`;
}

// Hand-drawn segments: a slightly bowed quadratic per segment.
function roughPath(R, pts, amp = 1.4) {
  const j = () => (R() - 0.5) * 2 * amp;
  let d = `M${f(pts[0].x + j() * 0.4)},${f(pts[0].y + j() * 0.4)}`;
  for (let i = 1; i < pts.length; i++) {
    const a = pts[i - 1];
    const b = pts[i];
    const last = i === pts.length - 1;
    d += ` Q${f((a.x + b.x) / 2 + j())},${f((a.y + b.y) / 2 + j())} ${f(b.x + (last ? j() * 0.4 : 0))},${f(b.y + (last ? j() * 0.4 : 0))}`;
  }
  return d;
}

function roughRect(R, x, y, w, h, stroke, sw = 2, passes = 2) {
  let out = '';
  for (let p = 0; p < passes; p++) {
    const o = () => (R() - 0.5) * 3;
    const pts = [{ x: x + o(), y: y + o() }, { x: x + w + o(), y: y + o() }, { x: x + w + o(), y: y + h + o() }, { x: x + o(), y: y + h + o() }, { x: x + o() + 1.5, y: y + o() - 1 }];
    out += `<path d="${roughPath(R, pts, 1.2)}" fill="none" stroke="${stroke}" stroke-width="${p ? sw * 0.6 : sw}" stroke-linecap="round" stroke-linejoin="round"/>`;
  }
  return out;
}

// ───────────────────────── icons (0..100 box, stroke only) ─────────────────────────

const ICONS = {
  user: '<circle cx="50" cy="30" r="17"/><path d="M16 94 C16 62 84 62 84 94 Z"/>',
  server: '<rect x="18" y="12" width="64" height="22" rx="4"/><rect x="18" y="39" width="64" height="22" rx="4"/><rect x="18" y="66" width="64" height="22" rx="4"/><circle cx="29" cy="23" r="2.5"/><circle cx="29" cy="50" r="2.5"/><circle cx="29" cy="77" r="2.5"/>',
  lock: '<path d="M33 46 V33 a17 17 0 0 1 34 0 V46"/><rect x="22" y="46" width="56" height="42" rx="7"/><circle cx="50" cy="63" r="5"/><path d="M50 68 V76"/>',
  app: '<rect x="12" y="18" width="76" height="64" rx="7"/><path d="M12 33 H88"/><circle cx="21" cy="25.5" r="2"/><circle cx="28" cy="25.5" r="2"/><circle cx="35" cy="25.5" r="2"/>',
  browser: '<rect x="12" y="18" width="76" height="64" rx="7"/><path d="M12 33 H88"/><circle cx="21" cy="25.5" r="2"/><circle cx="28" cy="25.5" r="2"/><circle cx="35" cy="25.5" r="2"/><circle cx="50" cy="51" r="8"/><path d="M36 74 C36 62 64 62 64 74"/>',
  db: '<ellipse cx="50" cy="24" rx="30" ry="10"/><path d="M20 24 V76 A30 10 0 0 0 80 76 V24"/><path d="M20 50 A30 10 0 0 0 80 50"/>',
  registry: '<rect x="16" y="54" width="30" height="30" rx="3"/><rect x="54" y="54" width="30" height="30" rx="3"/><rect x="35" y="16" width="30" height="30" rx="3"/><path d="M31 54 V62 M69 54 V62 M50 46 V54"/>',
  vault: '<rect x="14" y="18" width="72" height="64" rx="8"/><circle cx="50" cy="50" r="15"/><circle cx="50" cy="50" r="4"/><path d="M50 35 V41 M50 59 V65 M35 50 H41 M59 50 H65"/><path d="M22 82 V88 M78 82 V88"/>',
  identity: '<rect x="22" y="12" width="56" height="76" rx="8"/><circle cx="50" cy="40" r="11"/><path d="M33 72 C33 56 67 56 67 72"/><path d="M42 20 H58"/>',
  ci: '<circle cx="50" cy="50" r="22"/><circle cx="50" cy="50" r="8"/><path d="M50 14 V24 M50 76 V86 M14 50 H24 M76 50 H86 M24.5 24.5 L31.6 31.6 M68.4 68.4 L75.5 75.5 M24.5 75.5 L31.6 68.4 M68.4 31.6 L75.5 24.5"/>',
  cloud: '<path d="M28 74 H74 a16 16 0 0 0 0 -32 a22 22 0 0 0 -42 -6 a19 19 0 0 0 -4 38 Z"/>',
  globe: '<circle cx="50" cy="50" r="34"/><ellipse cx="50" cy="50" rx="14" ry="34"/><path d="M16 50 H84 M22 32 H78 M22 68 H78"/>',
  terminal: '<rect x="12" y="18" width="76" height="64" rx="7"/><path d="M26 40 L38 50 L26 60 M44 62 H62"/>',
  container: '<rect x="14" y="24" width="72" height="52" rx="6"/><rect x="24" y="34" width="14" height="32" rx="2"/><rect x="43" y="34" width="14" height="32" rx="2"/><rect x="62" y="34" width="14" height="32" rx="2"/>',
  laptop: '<rect x="22" y="22" width="56" height="40" rx="4"/><path d="M12 72 H88 L82 80 H18 Z"/><path d="M34 36 L42 42 L34 48 M46 50 H58"/>',
};

const icon = (kind, x, y, size, stroke, sw = 4.5, extraTransform = '') =>
  `<g transform="translate(${f(x)},${f(y)})${extraTransform} scale(${size})" fill="none" stroke="${stroke}" stroke-width="${sw}" stroke-linecap="round" stroke-linejoin="round">${ICONS[kind] ?? ICONS.server}</g>`;

// ───────────────────────── shared geometry ─────────────────────────

function routeGrid(a, b, mode) {
  const same = (x, y) => Math.abs(x - y) < 1e-6;
  if (mode === 'straight' || same(a.c, b.c) || same(a.r, b.r)) return [a, b];
  if (mode === 'vh') return [a, { c: a.c, r: b.r }, b];
  if (mode === 'z') { const m = (a.c + b.c) / 2; return [a, { c: m, r: a.r }, { c: m, r: b.r }, b]; }
  if (mode === 'n') { const m = (a.r + b.r) / 2; return [a, { c: a.c, r: m }, { c: b.c, r: m }, b]; }
  return [a, { c: b.c, r: a.r }, b]; // hv
}

const groupRect = (g) => {
  const pad = Number(g.opts.pad ?? 0.5);
  return { c0: Math.min(g.from.c, g.to.c) - pad, c1: Math.max(g.from.c, g.to.c) + pad, r0: Math.min(g.from.r, g.to.r) - pad, r1: Math.max(g.from.r, g.to.r) + pad };
};

// Clip a screen-space polyline end so it stops at a shape boundary.
function clipEnd(pts, atStart, distFn) {
  const [p, q] = atStart ? [pts[0], pts[1]] : [pts[pts.length - 1], pts[pts.length - 2]];
  const dx = q.x - p.x;
  const dy = q.y - p.y;
  const len = Math.hypot(dx, dy) || 1;
  const ux = dx / len;
  const uy = dy / len;
  const d = Math.min(distFn(ux, uy), len - 4);
  const np = { x: p.x + ux * d, y: p.y + uy * d };
  if (atStart) pts[0] = np; else pts[pts.length - 1] = np;
}

function offsetPts(pts, px) {
  if (!px) return pts;
  const a = pts[0];
  const b = pts[pts.length - 1];
  const len = Math.hypot(b.x - a.x, b.y - a.y) || 1;
  const nx = (b.y - a.y) / len;
  const ny = -(b.x - a.x) / len;
  return pts.map((p) => ({ x: p.x + nx * px, y: p.y + ny * px }));
}

function longestSeg(pts, at = 0.5) {
  let best = 0;
  let bi = 0;
  for (let i = 1; i < pts.length; i++) {
    const l = Math.hypot(pts[i].x - pts[i - 1].x, pts[i].y - pts[i - 1].y);
    if (l > best) { best = l; bi = i; }
  }
  const a = pts[bi - 1];
  const b = pts[bi];
  return { x: a.x + (b.x - a.x) * at, y: a.y + (b.y - a.y) * at, horiz: Math.abs(b.x - a.x) >= Math.abs(b.y - a.y) };
}

function arrowHead(pts, color, size = 9, filled = false) {
  const b = pts[pts.length - 1];
  const a = pts[pts.length - 2];
  const ang = Math.atan2(b.y - a.y, b.x - a.x);
  const p1 = { x: b.x - size * Math.cos(ang - 0.45), y: b.y - size * Math.sin(ang - 0.45) };
  const p2 = { x: b.x - size * Math.cos(ang + 0.45), y: b.y - size * Math.sin(ang + 0.45) };
  return filled
    ? `<path d="M${f(p1.x)},${f(p1.y)} L${f(b.x)},${f(b.y)} L${f(p2.x)},${f(p2.y)} Z" fill="${color}"/>`
    : `<path d="M${f(p1.x)},${f(p1.y)} L${f(b.x)},${f(b.y)} L${f(p2.x)},${f(p2.y)}" fill="none" stroke="${color}" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>`;
}

function reversePairs(edges) {
  const set = new Set(edges.map((e) => `${e.from}>${e.to}`));
  return (e) => (set.has(`${e.to}>${e.from}`) ? 1 : 0);
}

// ───────────────────────── card (terminal) ─────────────────────────

function renderCard(card, th, x, y, w) {
  const pad = 36;
  const colW = (w - pad * 2) / card.columns.length;
  const lh = 30;
  const tokenH = 110;
  let maxH = 0;
  const parts = [];
  card.columns.forEach((col, ci) => {
    const cx = x + pad + ci * colW + (ci ? 24 : 0);
    let cy = y + 96;
    parts.push(text(cx, cy, col.title, { size: 26, family: th.font, fill: th.cardAccent }));
    cy += 40;
    for (const it of col.items) {
      if (it.type === 'bullet') {
        parts.push(`<circle cx="${f(cx + 6)}" cy="${f(cy - 7)}" r="3" fill="${th.cardText}"/>`);
        parts.push(text(cx + 22, cy, it.text, { size: 21, family: th.font, fill: th.cardText }));
        cy += lh;
      } else if (it.type === 'step') {
        parts.push(text(cx, cy, `${it.n}.`, { size: 21, family: th.font, fill: th.cardAccent }));
        parts.push(text(cx + 26, cy, it.text, { size: 21, family: th.font, fill: th.cardText }));
        cy += lh;
      } else if (it.type === 'comment') {
        cy += 12;
        parts.push(text(cx, cy, `// ${it.text}`, { size: 22, family: th.font, fill: th.cardAccent }));
        cy += lh + 6;
      } else if (it.type === 'token') {
        const colors = { orange: '#f08a3c', green: '#9bd35a', blue: '#4ea1f3', red: '#f85149', teal: '#2dd4bf', purple: '#c297ff' };
        const fs = 30;
        const sep = ' . ';
        const total = it.parts.reduce((s, p) => s + textWidth(p.text, fs, th, true), 0) + textWidth(sep, fs, th, true) * (it.parts.length - 1);
        const bw = total + 50;
        parts.push(`<rect x="${f(cx)}" y="${f(cy - 4)}" width="${f(bw)}" height="48" rx="8" fill="#000" stroke="${th.cardDim}" stroke-width="2"/>`);
        let tx = cx + 25;
        it.parts.forEach((p, i) => {
          const tw = textWidth(p.text, fs, th, true);
          parts.push(text(tx, cy + 30, p.text, { size: fs, family: th.mono, fill: colors[p.color] ?? p.color ?? th.cardText, weight: 600 }));
          const mid = tx + tw / 2;
          parts.push(`<path d="M${f(mid)},${f(cy + 52)} V${f(cy + 72)} M${f(mid - 6)},${f(cy + 58)} L${f(mid)},${f(cy + 52)} L${f(mid + 6)},${f(cy + 58)} M${f(mid - 9)},${f(cy + 50)} H${f(mid + 9)}" stroke="${th.cardText}" stroke-width="1.6" fill="none" stroke-linecap="round"/>`);
          parts.push(text(mid, cy + 94, p.label, { size: 19, family: th.font, fill: th.cardText, anchor: 'middle', lh: 22 }));
          tx += tw;
          if (i < it.parts.length - 1) { parts.push(text(tx, cy + 30, sep, { size: fs, family: th.mono, fill: th.cardText })); tx += textWidth(sep, fs, th, true); }
        });
        cy += tokenH + 22;
      }
    }
    maxH = Math.max(maxH, cy - y);
  });
  const h = maxH + 10;
  const divs = card.columns.slice(1).map((_, i) => {
    const dx = x + pad + (i + 1) * colW;
    return `<path d="M${f(dx)},${f(y + 76)} V${f(y + h - 20)}" stroke="${th.cardDim}" stroke-width="2"/>`;
  }).join('');
  const chrome = `<rect x="${f(x + 4)}" y="${f(y + 8)}" width="${f(w)}" height="${f(h)}" rx="22" fill="#000" opacity=".12"/>`
    + `<rect x="${f(x)}" y="${f(y)}" width="${f(w)}" height="${f(h)}" rx="22" fill="${th.cardBg}"/>`
    + `<path d="M${f(x)},${f(y + 60)} H${f(x + w)}" stroke="#000" stroke-opacity=".25"/>`
    + ['#ff5f57', '#febc2e', '#28c840'].map((c, i) => `<circle cx="${f(x + 40 + i * 32)}" cy="${f(y + 31)}" r="10" fill="${c}"/>`).join('');
  return { svg: chrome + divs + parts.join(''), h };
}

// ───────────────────────── title ─────────────────────────

function renderTitle(d, th, cx, y, center) {
  const size = center ? 92 : 40;
  const words = d.title.split(' ');
  const accent = new Set(d.accent.flatMap((a) => a.split(' ')));
  const spans = words.map((w) => `<tspan fill="${accent.has(w) ? th.accent : th.ink}">${esc(w)}</tspan>`).join(' ');
  const anchor = center ? 'middle' : 'start';
  let out = `<text x="${f(cx)}" y="${f(y + size * 0.8)}" font-family="${esc(th.titleFont)}" font-weight="${center ? 600 : 700}" font-size="${size}" text-anchor="${anchor}">${spans}</text>`;
  let h = size;
  if (center) {
    const tw = textWidth(d.title, size, th) * 1.05;
    out += `<path d="M${f(cx - tw * 0.55)},${f(y + size + 14)} Q${f(cx)},${f(y + size)} ${f(cx + tw * 0.55)},${f(y + size + 12)}" fill="none" stroke="${th.muted}" stroke-width="3" stroke-linecap="round" opacity=".7"/>`;
    h += 22;
  }
  if (d.subtitle) {
    const ss = center ? 30 : 18;
    out += text(cx, y + h + ss + 6, d.subtitle, { size: ss, family: th.font, fill: center ? th.ink : th.muted, anchor, extra: center ? ' letter-spacing="1.5"' : '' });
    h += ss + 18;
  }
  return { svg: out, h };
}

// ───────────────────────── flat view ─────────────────────────

function renderFlat(d, th) {
  const [W, H] = d.cell ?? [170, 150];
  const R = rng(7);
  const margin = 50;
  const out = [];
  const B = new Bounds();
  let px = margin;
  const top = margin + (d.titlepos === 'top' ? 0 : 0);
  const titleTop = d.titlepos !== 'middle' ? renderTitle(d, th, margin, margin, false) : null;
  const panelTop = titleTop ? margin + titleTop.h + 30 : margin;
  const panelLayouts = [];

  d.panels.forEach((p, pi) => {
    const cs = [...p.nodes.map((n) => n.c), ...p.groups.flatMap((g) => { const r = groupRect(g); return [r.c0 + 0.5, r.c1 - 0.5]; })];
    const rs = [...p.nodes.map((n) => n.r), ...p.groups.flatMap((g) => { const r = groupRect(g); return [r.r0 + 0.5, r.r1 - 0.5]; })];
    const cmin = Math.min(...cs);
    const cmax = Math.max(...cs);
    const rmin = Math.min(...rs);
    const rmax = Math.max(...rs);
    const capH = p.caption ? 90 : 10;
    const ox = px;
    const oy = panelTop + capH;
    const gx = (c) => ox + (c - cmin) * W + W / 2;
    const gy = (r) => oy + (r - rmin) * H + H / 2;
    const pw = (cmax - cmin + 1) * W;
    let ph = (rmax - rmin + 1) * H;
    const byId = Object.fromEntries(p.nodes.map((n) => [n.id, n]));
    const gById = Object.fromEntries(p.groups.map((g) => [g.id, g]));

    if (p.caption) out.push(text(ox + pw / 2, panelTop + 30, p.caption, { size: 24, family: th.font, fill: th.ink, anchor: 'middle', lh: 32 }));

    // groups
    for (const g of p.groups) {
      const r = groupRect(g);
      const x = gx(r.c0 + 0.5) - W / 2;
      const y = gy(r.r0 + 0.5) - H / 2;
      const w = (r.c1 - r.c0) * W;
      const h = (r.r1 - r.r0) * H;
      const t = th.tints[g.opts.tint ?? 'white'] ?? th.tints.white;
      out.push(`<rect x="${f(x)}" y="${f(y)}" width="${f(w)}" height="${f(h)}" rx="18" fill="${t[0]}" fill-opacity=".55"/>`);
      out.push(`<path d="${roughPath(R, [{ x: x + 16, y }, { x: x + w - 16, y }, { x: x + w, y: y + 16 }, { x: x + w, y: y + h - 16 }, { x: x + w - 16, y: y + h }, { x: x + 16, y: y + h }, { x, y: y + h - 16 }, { x, y: y + 16 }, { x: x + 16, y }], 1)}" fill="none" stroke="${t[1] ?? th.ink}" stroke-width="1.8" stroke-dasharray="${g.flags.solid ? '' : '7 6'}"/>`);
      out.push(text(x + 18, y + 28, g.label, { size: 19, family: th.font, fill: t[1] ?? th.muted, weight: th.hand ? undefined : 600 }));
    }

    // nodes
    const half = (n) => (n.kind === 'user' ? 36 : 44);
    for (const n of p.nodes) {
      const x = gx(n.c);
      const y = gy(n.r);
      const t = th.tints[n.opts.tint ?? 'white'] ?? th.tints.white;
      if (n.kind === 'user') {
        out.push(icon('user', x - 34, y - 36, 0.68, th.ink, 5.5));
      } else {
        const s = 84;
        out.push(`<rect x="${f(x - s / 2 + 3)}" y="${f(y - s / 2 + 4)}" width="${s}" height="${s}" rx="12" fill="#000" opacity=".06"/>`);
        out.push(`<rect x="${f(x - s / 2)}" y="${f(y - s / 2)}" width="${s}" height="${s}" rx="12" fill="${t[0]}"/>`);
        out.push(th.hand ? roughRect(R, x - s / 2, y - s / 2, s, s, th.ink, 2.2) : `<rect x="${f(x - s / 2)}" y="${f(y - s / 2)}" width="${s}" height="${s}" rx="12" fill="none" stroke="${t[1] ?? th.ink}" stroke-width="2"/>`);
        out.push(icon(n.kind, x - 28, y - 28, 0.56, th.hand ? th.ink : (t[1] ?? th.ink), 5));
      }
      out.push(text(x, y + half(n) + 26, n.label, { size: 20, family: th.font, fill: th.ink, anchor: 'middle', lh: 25 }));
      if (n.opts.badge) {
        const bw = textWidth(n.opts.badge, 14, th) + 20;
        out.push(`<rect x="${f(x + 18)}" y="${f(y - 58)}" width="${f(bw)}" height="24" rx="12" fill="${th.badgeBg}"/>`);
        out.push(text(x + 18 + bw / 2, y - 41, n.opts.badge, { size: 14, family: th.font, fill: th.badgeFg, anchor: 'middle', weight: 600 }));
      }
    }

    // edges
    const rev = reversePairs(p.edges);
    for (const e of p.edges) {
      const a = byId[e.from];
      if (!a) throw new Error(`edge from unknown node "${e.from}"`);
      const bn = byId[e.to];
      const bg = gById[e.to];
      if (!bn && !bg) throw new Error(`edge to unknown node "${e.to}"`);
      let target = bn ? { c: bn.c, r: bn.r } : (() => { const r = groupRect(bg); return { c: Math.min(Math.max(a.c, r.c0), r.c1), r: Math.min(Math.max(a.r, r.r0), r.r1) }; })();
      const grid = routeGrid({ c: a.c, r: a.r }, target, e.opts.route ?? 'hv');
      let pts = grid.map((q) => ({ x: gx(q.c), y: gy(q.r) }));
      if (bg) { const r = groupRect(bg); pts[pts.length - 1] = { x: gx(target.c) + (target.c === r.c0 ? 0 : 0), y: gy(target.r) }; }
      pts = offsetPts(pts, Number(e.opts.off ?? (rev(e) ? 12 : 0)));
      const box = (n) => (ux, uy) => Math.min((half(n) + 10) / Math.abs(ux || 1e-9), (half(n) + 10) / Math.abs(uy || 1e-9));
      clipEnd(pts, true, box(a));
      if (bn) clipEnd(pts, false, box(bn));
      const dash = e.flags.solid ? '' : ' stroke-dasharray="9 7"';
      out.push(`<path d="${th.hand ? roughPath(R, pts, 1) : 'M' + pts.map((q) => `${f(q.x)},${f(q.y)}`).join(' L')}" fill="none" stroke="${th.edge}" stroke-width="2.2" stroke-linecap="round"${dash}/>`);
      if (e.op !== '--') out.push(arrowHead(pts, th.edge, 11));
      if (e.op === '<->') out.push(arrowHead([...pts].reverse(), th.edge, 11));
      if (e.label || e.step) {
        const m = longestSeg(pts, Number(e.opts.at ?? 0.5));
        const lbl = (e.step && !e.flags.nonum ? `${e.step}. ` : '') + e.label;
        const lines = lbl.split('\n').length;
        const [nx, ny] = (e.opts.nudge ?? '0,0').split(',').map(Number);
        const later = rev(e) && p.edges.indexOf(e) > p.edges.findIndex((o) => o.from === e.to && o.to === e.from);
        const where = e.opts.lpos ?? (m.horiz ? (later ? 'below' : 'above') : (later ? 'left' : 'right'));
        let lx = m.x + nx;
        let ly = m.y + ny;
        let anchor = 'middle';
        if (where === 'above') ly -= 12 + (lines - 1) * 24;
        if (where === 'below') ly += 28;
        if (where === 'right') { lx += 14; anchor = 'start'; ly -= (lines - 1) * 12 - 6; }
        if (where === 'left') { lx -= 14; anchor = 'end'; ly -= (lines - 1) * 12 - 6; }
        out.push(text(lx, ly, lbl, { size: 19, family: th.font, fill: th.ink, anchor, lh: 24 }));
        const lw = textWidth(lbl, 19, th);
        B.rect(anchor === 'middle' ? lx - lw / 2 : anchor === 'end' ? lx - lw : lx, ly - 20, lw, lines * 24);
      }
    }

    // notes (default: under the panel)
    const lowest = Math.max(oy + ph - H / 2, ...p.nodes.map((n) => gy(n.r) + half(n) + 14 + n.label.split('\n').length * 25));
    let noteBottom = lowest + 10;
    for (const nt of p.notes) {
      const lines = nt.text.split('\n');
      const w = Number(nt.opts.w ?? textWidth(nt.text, 21, th) + 50);
      const h = lines.length * 30 + 30;
      const cx = nt.at ? gx(nt.at.c) : ox + pw / 2;
      const y = nt.at ? gy(nt.at.r) - h / 2 : noteBottom + 10;
      out.push(`<rect x="${f(cx - w / 2)}" y="${f(y)}" width="${f(w)}" height="${f(h)}" rx="14" fill="${th.noteFill}"/>`);
      out.push(th.hand ? roughRect(R, cx - w / 2, y, w, h, th.muted, 1.6, 1) : `<rect x="${f(cx - w / 2)}" y="${f(y)}" width="${f(w)}" height="${f(h)}" rx="14" fill="none" stroke="${th.muted}" stroke-opacity=".5"/>`);
      out.push(text(cx - w / 2 + 24, y + 38, nt.text, { size: 21, family: th.font, fill: th.ink, lh: 30 }));
      if (!nt.at) noteBottom = y + h + 10;
      B.rect(cx - w / 2, y, w, h);
    }
    ph = noteBottom - oy;
    B.rect(ox, panelTop, pw, capH + ph);
    panelLayouts.push({ ox, pw });
    px += pw + 70;
  });

  // dividers between panels
  for (let i = 1; i < panelLayouts.length; i++) {
    const x = panelLayouts[i].ox - 35;
    out.push(`<path d="${roughPath(R, [{ x, y: panelTop }, { x, y: B.y1 }], 1)}" stroke="${th.muted}" stroke-width="2" stroke-dasharray="8 8" fill="none"/>`);
  }

  let y = B.y1 + 40;
  const fullW = Math.max(B.x1 - margin, 600);
  if (titleTop) out.unshift(titleTop.svg);
  else {
    const t = renderTitle(d, th, margin + fullW / 2, y, true);
    out.push(t.svg);
    y += t.h + 40;
  }
  if (d.card) {
    const c = renderCard(d.card, th, margin, y, fullW);
    out.push(c.svg);
    y += c.h;
  }
  return wrap(th, margin * 2 + fullW, y + margin, out.join('\n'), '');
}

// ───────────────────────── isometric view ─────────────────────────

function renderIso(d, th) {
  const TW = d.cell?.[0] ?? 210;
  const TH = TW * Math.tan(Math.PI / 6);
  const S = 0.5; // block footprint in cells
  const P = (c, r, z = 0) => ({ x: (c - r) * TW / 2, y: (c + r) * TH / 2 - z });
  const B = new Bounds();
  const L = { grid: [], groups: [], edges: [], blocks: [], labels: [] };
  let cOff = 0;
  let flowCss = false;

  d.panels.forEach((p) => {
    const nodes = p.nodes.map((n) => ({ ...n, c: n.c + cOff }));
    const groups = p.groups.map((g) => ({ ...g, from: { c: g.from.c + cOff, r: g.from.r }, to: { c: g.to.c + cOff, r: g.to.r } }));
    const byId = Object.fromEntries(nodes.map((n) => [n.id, n]));
    const gById = Object.fromEntries(groups.map((g) => [g.id, g]));

    // group stacking: larger groups first; a group sits on top of any group containing it
    const inside = (r, c, rr) => c >= r.c0 && c <= r.c1 && rr >= r.r0 && rr <= r.r1;
    groups.forEach((g) => { g.rect = groupRect(g); g.area = (g.rect.c1 - g.rect.c0) * (g.rect.r1 - g.rect.r0); });
    const sorted = [...groups].sort((a, b) => b.area - a.area);
    for (const g of sorted) {
      const cc = (g.rect.c0 + g.rect.c1) / 2;
      const cr = (g.rect.r0 + g.rect.r1) / 2;
      g.base = Math.max(0, ...sorted.filter((o) => o !== g && o.top !== undefined && inside(o.rect, cc, cr)).map((o) => o.top));
      g.top = g.base + (g.flags.slab ? 16 : 2);
    }
    const zAt = (c, r) => Math.max(0, ...groups.filter((g) => inside(g.rect, c, r)).map((g) => g.top));
    nodes.forEach((n) => { n.z = zAt(n.c, n.r); });

    // floor grid
    if (th.grid) {
      const cs = [...nodes.map((n) => n.c), ...groups.flatMap((g) => [g.rect.c0, g.rect.c1])];
      const rs = [...nodes.map((n) => n.r), ...groups.flatMap((g) => [g.rect.r0, g.rect.r1])];
      const c0 = Math.floor(Math.min(...cs)) - 0.5;
      const c1 = Math.ceil(Math.max(...cs)) + 0.5;
      const r0 = Math.floor(Math.min(...rs)) - 0.5;
      const r1 = Math.ceil(Math.max(...rs)) + 0.5;
      for (let c = c0; c <= c1 + 1e-6; c += 0.25) { const a = P(c, r0); const b = P(c, r1); L.grid.push(`M${f(a.x)},${f(a.y)} L${f(b.x)},${f(b.y)}`); }
      for (let r = r0; r <= r1 + 1e-6; r += 0.25) { const a = P(c0, r); const b = P(c1, r); L.grid.push(`M${f(a.x)},${f(a.y)} L${f(b.x)},${f(b.y)}`); }
      [P(c0, r0), P(c1, r0), P(c1, r1), P(c0, r1)].forEach((q) => B.add(q.x, q.y));
    }

    // groups as plates / slabs
    for (const g of sorted) {
      const { c0, c1, r0, r1 } = g.rect;
      const t = th.tints[g.opts.tint ?? 'slate'] ?? th.tints.slate;
      const stroke = t[1] ?? th.muted;
      const top = [P(c0, r0, g.top), P(c1, r0, g.top), P(c1, r1, g.top), P(c0, r1, g.top)];
      const poly = (pts) => pts.map((q) => `${f(q.x)},${f(q.y)}`).join(' ');
      let s = '';
      if (g.flags.slab) {
        const bot = [P(c0, r0, g.base), P(c1, r0, g.base), P(c1, r1, g.base), P(c0, r1, g.base)];
        s += `<polygon points="${poly([top[3], top[2], bot[2], bot[3]])}" fill="${shade(t[0], -0.06)}" fill-opacity=".9" stroke="${stroke}" stroke-opacity=".5"/>`;
        s += `<polygon points="${poly([top[1], top[2], bot[2], bot[1]])}" fill="${shade(t[0], -0.14)}" fill-opacity=".9" stroke="${stroke}" stroke-opacity=".5"/>`;
      }
      s += `<polygon points="${poly(top)}" fill="${t[0]}" fill-opacity="${g.flags.slab ? 0.75 : 0.45}" stroke="${stroke}" stroke-width="1.5"${g.flags.slab ? '' : ' stroke-dasharray="6 5"'}/>`;
      // label lies on the floor along the back-right edge
      const lo = Number(g.opts.loff ?? 0.12);
      const o = g.opts.label === 'front' ? P(c0 + lo, r1 - 0.1, g.top) : P(c0 + lo, r0 - 0.06, g.top);
      s += `<text transform="matrix(0.866,0.5,-0.866,0.5,${f(o.x)},${f(o.y)})" font-family="${esc(th.font)}" font-size="21" font-weight="600" fill="${stroke}">${esc(g.label)}</text>`;
      L.groups.push(s);
      top.forEach((q) => B.add(q.x, q.y));
    }

    // edges
    const rev = reversePairs(p.edges);
    for (const e of p.edges) {
      const a = byId[e.from];
      const bn = byId[e.to];
      const bg = gById[e.to];
      if (!a || (!bn && !bg)) throw new Error(`edge ${e.from} -> ${e.to}: unknown node`);
      const target = bn ? { c: bn.c, r: bn.r } : { c: Math.min(Math.max(a.c, bg.rect.c0), bg.rect.c1), r: Math.min(Math.max(a.r, bg.rect.r0), bg.rect.r1) };
      const zEnd = bn ? bn.z : bg.top;
      const grid = routeGrid({ c: a.c, r: a.r }, target, e.opts.route ?? 'straight');
      let pts = grid.map((q, i) => P(q.c, q.r, i === 0 ? a.z : zEnd));
      pts = offsetPts(pts, Number(e.opts.off ?? (rev(e) ? 10 : 0)));
      const diamond = (n) => (ux, uy) => 1 / (Math.abs(ux) / (S * TW / 2) + Math.abs(uy) / (S * TH / 2)) + 10;
      clipEnd(pts, true, a.kind === 'user' ? () => 34 : diamond(a));
      if (bn) clipEnd(pts, false, bn.kind === 'user' ? () => 34 : diamond(bn));
      const flow = e.flags.flow;
      flowCss ||= flow;
      const dash = flow ? ' stroke-dasharray="7 7" class="flow"' : e.flags.dashed ? ' stroke-dasharray="6 6"' : '';
      const color = flow ? th.accent : th.edge;
      let s = `<path d="M${pts.map((q) => `${f(q.x)},${f(q.y)}`).join(' L')}" fill="none" stroke="${color}" stroke-width="${flow ? 2.6 : 1.8}" stroke-linecap="round"${dash}/>`;
      if (e.op !== '--') s += arrowHead(pts, color, 12, true);
      if (e.op === '<->') s += arrowHead([...pts].reverse(), color, 12, true);
      L.edges.push(s);
      if (e.label || (e.step && !e.flags.nonum)) {
        const m = longestSeg(pts, Number(e.opts.at ?? 0.5));
        const [nx, ny] = (e.opts.nudge ?? '0,0').split(',').map(Number);
        L.labels.push(pill(m.x + nx, m.y + ny, e.label, th, { step: e.flags.nonum ? null : e.step, size: 14, B }));
      }
    }

    // blocks, back to front
    for (const n of [...nodes].sort((a, b) => (a.c + a.r) - (b.c + b.r))) {
      const t = th.tints[n.opts.tint ?? 'white'] ?? th.tints.white;
      const h = 30 * Number(n.opts.h ?? 1);
      const ink = th.iconInk ?? th.ink;
      let s = '';
      if (n.kind === 'user') {
        const g = P(n.c, n.r, n.z);
        s += `<ellipse cx="${f(g.x)}" cy="${f(g.y)}" rx="30" ry="${f(30 * TH / TW)}" fill="#000" opacity=".12"/>`;
        s += icon('user', g.x - 30, g.y - 70, 0.6, ink, 6);
        L.blocks.push(s);
        L.labels.push(pill(g.x, g.y + 34, n.label, th, { size: 15, B, strong: true }));
        B.rect(g.x - 30, g.y - 70, 60, 70);
        continue;
      }
      const c0 = n.c - S / 2;
      const c1 = n.c + S / 2;
      const r0 = n.r - S / 2;
      const r1 = n.r + S / 2;
      const T = [P(c0, r0, n.z + h), P(c1, r0, n.z + h), P(c1, r1, n.z + h), P(c0, r1, n.z + h)];
      const Bt = [P(c0, r0, n.z), P(c1, r0, n.z), P(c1, r1, n.z), P(c0, r1, n.z)];
      const poly = (pts) => pts.map((q) => `${f(q.x)},${f(q.y)}`).join(' ');
      const topFill = t[0];
      const edge = th.blockEdge;
      s += `<polygon points="${poly([Bt[3], Bt[2], Bt[1]].map((q) => ({ x: q.x + 6, y: q.y + 4 })))}" fill="#000" opacity=".07"/>`;
      s += `<polygon points="${poly([T[3], T[2], Bt[2], Bt[3]])}" fill="${shade(topFill, -0.1)}" stroke="${edge}" stroke-linejoin="round"/>`;
      s += `<polygon points="${poly([T[1], T[2], Bt[2], Bt[1]])}" fill="${shade(topFill, -0.2)}" stroke="${edge}" stroke-linejoin="round"/>`;
      s += `<polygon points="${poly(T)}" fill="${topFill}" stroke="${edge}" stroke-linejoin="round"/>`;
      // icon lying on the top face
      const ctr = P(n.c, n.r, n.z + h);
      const k = (S * TW * 0.42) / 100;
      s += `<g transform="translate(${f(ctr.x)},${f(ctr.y)}) scale(${f(k * 100) / 100},${f(k * TH / TW * 100) / 100}) translate(-50,-50)" fill="none" stroke="${t[1] ?? ink}" stroke-width="6" stroke-linecap="round" stroke-linejoin="round">${ICONS[n.kind]}</g>`;
      L.blocks.push(s);
      [...T, ...Bt].forEach((q) => B.add(q.x, q.y));
      const front = Bt[2];
      L.labels.push(pill(front.x, front.y + 22, n.label, th, { size: 15, B, strong: true }));
      if (n.opts.badge) {
        const tp = T[1];
        L.labels.push(badge(tp.x + 4, tp.y - 22, n.opts.badge, th, B));
      }
    }

    if (p.caption) {
      const cs = nodes.map((n) => n.c);
      const rs = nodes.map((n) => n.r);
      const q = P(Math.min(...cs) - 1, Math.min(...rs) - 1);
      L.labels.push(text(q.x, q.y - 40, p.caption, { size: 20, family: th.font, fill: th.muted, anchor: 'middle' }));
    }
    for (const nt of p.notes) {
      const q = nt.at ? P(nt.at.c + cOff, nt.at.r) : P(Math.max(...nodes.map((n) => n.c)) + 1.5, Math.max(...nodes.map((n) => n.r)) + 1.5);
      L.labels.push(pill(q.x, q.y, nt.text, th, { size: 15, B, left: true }));
    }

    cOff += Math.max(...p.nodes.map((n) => n.c)) - Math.min(...p.nodes.map((n) => n.c)) + 4;
  });

  const margin = 40;
  const title = d.title ? renderTitle(d, th, 0, 0, false) : { svg: '', h: 0 };
  const titleH = title.h ? title.h + 20 : 0;
  // drop grid that sticks out far beyond the content
  const w = B.x1 - B.x0 + margin * 2;
  let h = B.y1 - B.y0 + margin * 2 + titleH;
  const tx = -B.x0 + margin;
  const ty = -B.y0 + margin + titleH;
  let body = `<g transform="translate(${f(tx)},${f(ty)})">`
    + (L.grid.length ? `<path d="${L.grid.join(' ')}" stroke="${th.grid}" stroke-width="1" fill="none"/>` : '')
    + L.groups.join('') + L.edges.join('') + L.blocks.join('') + L.labels.join('') + '</g>';
  body = `<g transform="translate(${margin},${margin})">${title.svg}</g>` + body;
  if (d.card) {
    const c = renderCard(d.card, th, margin, h, w - margin * 2);
    body += c.svg;
    h += c.h + margin;
  }
  const css = flowCss ? '.flow{animation:flow 1.1s linear infinite}@keyframes flow{to{stroke-dashoffset:-28}}@media (prefers-reduced-motion:reduce){.flow{animation:none}}' : '';
  return wrap(th, w, h, body, css);
}

function pill(x, y, label, th, o) {
  if (!label && !o.step) return '';
  const size = o.size;
  const lines = String(label).split('\n');
  const lh = size * 1.3;
  const w = textWidth(label, size, th) + 22 + (o.step ? size * 1.6 : 0);
  const h = lines.length * lh + 10;
  const x0 = o.left ? x : x - w / 2;
  const y0 = y - h / 2;
  o.B?.rect(x0, y0, w, h);
  let s = `<rect x="${f(x0)}" y="${f(y0)}" width="${f(w)}" height="${f(h)}" rx="${f(Math.min(h / 2, 10))}" fill="${th.bg}" fill-opacity=".94" stroke="${th.blockEdge}" stroke-opacity=".7"/>`;
  let tx = x0 + 11;
  if (o.step) {
    s += `<circle cx="${f(tx + size * 0.6)}" cy="${f(y0 + 5 + lh / 2)}" r="${f(size * 0.68)}" fill="${th.accent}"/>`;
    s += text(tx + size * 0.6, y0 + 5 + lh / 2 + size * 0.36, String(o.step), { size: size * 0.9, family: th.font, fill: th.bg, anchor: 'middle', weight: 700 });
    tx += size * 1.6;
  }
  s += text(tx, y0 + 5 + size, label, { size, family: th.font, fill: o.strong ? th.muted : th.ink, lh, firstWeight: o.strong ? 600 : undefined, firstFill: o.strong ? th.ink : undefined, firstSize: o.strong ? size + 1 : undefined });
  return s;
}

function badge(x, y, label, th, B) {
  const size = 13;
  const w = textWidth(label, size, th) + 18;
  B?.rect(x, y - 12, w, 24);
  return `<rect x="${f(x)}" y="${f(y - 12)}" width="${f(w)}" height="24" rx="12" fill="${th.badgeBg}"/>`
    + text(x + w / 2, y + 4.5, label, { size, family: th.font, fill: th.badgeFg, anchor: 'middle', weight: 600 });
}

function wrap(th, w, h, body, css) {
  return `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ${f(w)} ${f(h)}" width="${f(w)}" height="${f(h)}">
<style>@import url('https://fonts.googleapis.com/css2?${th.fontImport.replace(/&/g, '&amp;')}&amp;display=swap');${css}</style>
<rect width="100%" height="100%" fill="${th.bg}"/>
${body}
</svg>
`;
}

// ───────────────────────── cli ─────────────────────────

export function render(src, { view, theme } = {}) {
  const d = parse(src);
  const v = view ?? d.view ?? 'flat';
  const th = { ...THEMES[theme ?? d.theme ?? (v === 'iso' ? 'cloud' : 'paper')] };
  if (d.themeOpts?.accent) th.accent = d.themeOpts.accent;
  return v === 'iso' ? renderIso(d, th) : renderFlat(d, th);
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const args = process.argv.slice(2);
  const get = (k) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const input = args.find((a, i) => !a.startsWith('-') && !['-o', '--view', '--theme'].includes(args[i - 1]));
  if (!input) { console.error('usage: dsl.mjs input.dsl -o out.svg [--view flat|iso] [--theme paper|cloud|ink]'); process.exit(1); }
  try {
    const svg = render(fs.readFileSync(input, 'utf8'), { view: get('--view'), theme: get('--theme') });
    const o = get('-o') ?? input.replace(/\.dsl$/, '.svg');
    fs.writeFileSync(o, svg);
    console.log(`wrote ${o}`);
  } catch (err) {
    console.error(err.message);
    process.exit(1);
  }
}
