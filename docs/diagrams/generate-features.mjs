#!/usr/bin/env node
// Genera un archivo .feature por historia a partir de docs/specs/functional/03-user-stories.md
// y comprueba la trazabilidad con 06-test-data.md y test-data/scenarios/index.json.
// Uso: node docs/diagrams/generate-features.mjs   (Node.js >= 18, sin dependencias)
// Sale con código 1 si encuentra inconsistencias. Resultados documentados en docs/validation.md.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const OUT = path.join(HERE, 'gherkin');
const read = p => fs.readFileSync(path.join(ROOT, p), 'utf8').replace(/\r\n/g, '\n');
const md = read('docs/specs/functional/03-user-stories.md');
const td06 = read('docs/specs/functional/06-test-data.md');
const vision = read('docs/specs/functional/01-vision-document.md');
const index = JSON.parse(read('test-data/scenarios/index.json'));
const version = (md.match(/^\| Versión \| (\S+)/m) || [])[1] || '?';

const slug = s => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase()
  .replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
const tdCodes = s => [...new Set([...s.matchAll(/TD-\d\d/g)].map(x => x[0]))];

// Índice de historias de 03: módulo, perfil y escenarios de prueba.
const idx = {};
for (const line of md.split('\n')) {
  const m = line.match(/^\| \[(HU-\d\d)\]\([^)]*\) \| (.+?) \| (.+?) \| (.+?) \| (.+?) \|$/);
  if (m) idx[m[1]] = { module: m[3], profile: m[4], tdText: m[5], td: tdCodes(m[5]) };
}

const problems = [];
const stories = [];
fs.mkdirSync(OUT, { recursive: true });
for (const f of fs.readdirSync(OUT).filter(f => f.endsWith('.feature'))) fs.unlinkSync(path.join(OUT, f));

for (const sec of md.split(/\n(?=## HU-\d\d · )/).slice(1)) {
  const [, id, title] = sec.match(/^## (HU-\d\d) · (.+)$/m);
  const g = sec.match(/```gherkin\n([\s\S]*?)\n```/);
  if (!g) { problems.push(`${id}: sin bloque gherkin`); continue; }
  const body = g[1];
  const lines = body.split('\n');
  const story = ((sec.match(/^\*\*Como\*\*.+$/m) || [''])[0]).replace(/\*\*/g, '');
  const rules = (sec.match(/^Reglas: (.+)$/m) || [null, ''])[1];
  const rn = [...new Set([...rules.matchAll(/RN-\d\d/g)].map(x => x[0]))];
  const count = re => lines.filter(l => re.test(l)).length;
  const scenarios = count(/^\s*Scenario:/), outlines = count(/^\s*Scenario Outline:/);
  if (outlines !== count(/^\s*Examples:/)) problems.push(`${id}: Scenario Outline sin Examples`);

  // Cada <placeholder> de un Scenario Outline debe tener su columna en Examples.
  for (const c of body.split(/\n(?=\s*Scenario)/).filter(c => /Scenario Outline:/.test(c))) {
    const [steps, ex = ''] = c.split('Examples:');
    const hdr = ex.split('\n').find(l => /^\s*\|/.test(l)) || '';
    const cols = new Set(hdr.split('|').map(s => s.trim()).filter(Boolean));
    for (const [, p] of steps.matchAll(/<([^>]+)>/g)) if (!cols.has(p)) problems.push(`${id}: <${p}> sin columna en Examples`);
  }

  const meta = idx[id] || { module: '', profile: '—', tdText: '—', td: [] };
  const td = [...new Set([...meta.td, ...tdCodes(body)])].sort();
  const tags = [id, ...meta.module.split(',').map(s => s.trim()).filter(Boolean), ...rn, ...td].map(t => '@' + t);
  const file = `${id}-${slug(title)}.feature`;
  const header = [
    `# ${id} · ${title}`,
    `# Generado desde docs/specs/functional/03-user-stories.md (v${version}). No editar a mano:`,
    `# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.`,
    `# ${story}`,
    `# Perfil: ${meta.profile} · Escenarios de prueba: ${meta.tdText}`,
    '',
    tags.join(' '),
  ].join('\n');
  fs.writeFileSync(path.join(OUT, file), `${header}\n${body}\n`, 'utf8');
  stories.push({ id, file, rn, td, scenarios, outlines });
}

// 06 §5.1 (columna "Historias") frente al índice de 03 (columna "Escenarios de prueba").
const td2hu = {};
for (const line of td06.split('\n')) {
  const c = line.split(' | ');
  if (/^\| TD-\d\d$/.test(c[0]) && c.length === 5) td2hu[c[0].slice(2)] = c[4].replace(/ \|$/, '');
}
for (const [td, hus] of Object.entries(td2hu)) {
  if (hus === 'Todas') continue;
  for (const hu of hus.match(/HU-\d\d/g) || [])
    if (!idx[hu]?.td.includes(td) && !/Todos/.test(idx[hu]?.tdText || '')) problems.push(`06 §5.1 asigna ${td} a ${hu}, pero el índice de 03 no lo lista`);
}
for (const [hu, m] of Object.entries(idx)) for (const td of m.td)
  if (td2hu[td] !== 'Todas' && !(td2hu[td] || '').includes(hu)) problems.push(`03 asigna ${td} a ${hu}, pero 06 §5.1 no lo lista`);

// 06 §5 (catálogo) frente a index.json, y archivos de cada escenario.
let catalogRows = 0;
for (const line of td06.split('\n')) {
  const m = line.match(/^\| (TD-\d\d) \| [^|]+\| (\d+) \| (\w+) \| (\d+):(\d+) \| (\d+) \/ (\d+) \/ (\d+) \| (\w+) \| (\w+) \| (.+?) \|$/);
  if (!m) continue;
  catalogRows++;
  const s = index.find(x => x.code === m[1]);
  if (!s) { problems.push(`${m[1]}: no está en index.json`); continue; }
  const doc = { channels: +m[2], mode: m[3], minutes: +m[4] * 60 + +m[5], programmedSamples: +m[6], validSamples: +m[7], affectedSamples: +m[8], status: m[9], closeReason: m[10] };
  const act = { ...s, minutes: Math.floor(s.durationSeconds / 60) };
  for (const k of Object.keys(doc)) if (doc[k] !== act[k]) problems.push(`${m[1]}.${k}: 06=${doc[k]}, index.json=${act[k]}`);
  const alerts = m[11] === '—' ? {} : Object.fromEntries(m[11].split(',').map(a => a.trim().split(' ×')).map(([t, n]) => [t, +n]));
  const norm = o => JSON.stringify(Object.entries(o).sort());
  if (norm(alerts) !== norm(s.alertsByType)) problems.push(`${m[1]}: alertas de 06 ≠ index.json`);
}
if (catalogRows !== index.length) problems.push(`06 §5 tiene ${catalogRows} filas e index.json ${index.length}`);
const dir = path.join(ROOT, 'test-data/scenarios');
for (const s of index) for (const n of ['scenario.json', 'readings.csv', 'transcript.log'])
  if (!fs.existsSync(path.join(dir, s.folder, n))) problems.push(`${s.folder}/${n} no existe`);

// Ejemplos de HU-14 frente a index.json.
const ex = md.match(/Scenario Outline: El resultado coincide[\s\S]*?Examples:\n([\s\S]*?)\n\n/)[1];
for (const l of ex.split('\n').slice(1)) {
  const c = l.split('|').map(s => s.trim()).filter(Boolean);
  const s = index.find(x => x.code === c[0]);
  const exp = s && [s.status, s.closeReason, s.programmedSamples, s.validSamples, s.affectedSamples].join('/');
  if (exp !== [c[1], c[2], ...c.slice(3, 6).map(Number)].join('/')) problems.push(`HU-14 ${c[0]}: ejemplo ≠ index.json`);
}

// Reglas de negocio sin historia que las cite.
const rnAll = [...new Set([...vision.matchAll(/^\| (RN-\d\d) \|/gm)].map(x => x[1]))];
const orphanRn = rnAll.filter(r => !stories.some(s => s.rn.includes(r)));

const total = stories.reduce((a, s) => a + s.scenarios + s.outlines, 0);
console.log(`${stories.length} features, ${total} escenarios → ${path.relative(ROOT, OUT)}`);
console.log(`Catálogo 06 §5: ${catalogRows} escenarios TD comprobados contra index.json`);
if (orphanRn.length) console.log(`Aviso: reglas sin historia en "Reglas:": ${orphanRn.join(', ')}`);
if (problems.length) { console.log(`\n${problems.length} inconsistencias:`); problems.forEach(p => console.log(' - ' + p)); process.exitCode = 1; }
else console.log('Sin inconsistencias.');
