#!/usr/bin/env node
// Valida y renderiza a SVG los diagramas de secuencia de docs/diagrams/sequence/*.md con
// @mermaid-js/mermaid-cli (mmdc). Un error de sintaxis en un diagrama hace fallar el script.
// Uso: node docs/diagrams/render-sequence-svg.mjs      (Node.js >= 18; descarga mmdc con npx la primera vez)
// Salida: docs/diagrams/sequence/svg/<diagrama>.svg
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const MERMAID_CLI = '@mermaid-js/mermaid-cli@12.0.0';
const here = path.dirname(fileURLToPath(import.meta.url));
const sourceDir = path.join(here, 'sequence');
const outputDir = path.join(sourceDir, 'svg');
const workDir = fs.mkdtempSync(path.join(os.tmpdir(), 'thermal-mermaid-'));
fs.mkdirSync(outputDir, { recursive: true });

// Fondo blanco: el SVG se lee igual con el tema claro u oscuro del editor.
const config = path.join(workDir, 'config.json');
fs.writeFileSync(config, JSON.stringify({ theme: 'default', sequence: { showSequenceNumbers: true } }));

const failures = [];
const files = fs.readdirSync(sourceDir).filter(f => f.endsWith('.md')).sort();

for (const file of files) {
  const markdown = fs.readFileSync(path.join(sourceDir, file), 'utf8').replace(/\r\n/g, '\n');
  const blocks = [...markdown.matchAll(/```mermaid\n([\s\S]*?)\n```/g)].map(m => m[1]);
  if (blocks.length !== 1) {
    failures.push(`${file}: se esperaba 1 bloque mermaid y hay ${blocks.length}`);
    continue;
  }

  const name = path.basename(file, '.md');
  const input = path.join(workDir, `${name}.mmd`);
  const output = path.join(outputDir, `${name}.svg`);
  fs.writeFileSync(input, blocks[0], 'utf8');

  try {
    // npx es un .cmd en Windows y necesita shell: las rutas van entre comillas.
    const quote = value => JSON.stringify(value);
    execSync(
      `npx --yes -p ${MERMAID_CLI} mmdc -i ${quote(input)} -o ${quote(output)} -b white -c ${quote(config)} -q`,
      { stdio: ['ignore', 'pipe', 'pipe'] },
    );
    const svg = fs.readFileSync(output, 'utf8');
    if (!svg.includes('<svg') || /Syntax error/i.test(svg)) throw new Error('SVG inválido o con error de sintaxis');
    console.log(`OK    ${file} → sequence/svg/${name}.svg (${Math.round(svg.length / 1024)} KB)`);
  } catch (error) {
    const detail = (error.stderr?.toString() || error.message).split('\n').filter(Boolean).slice(0, 4).join(' | ');
    failures.push(`${file}: ${detail}`);
    console.log(`ERROR ${file}`);
  }
}

fs.rmSync(workDir, { recursive: true, force: true });
if (failures.length) {
  console.log(`\n${failures.length} diagrama(s) con errores:`);
  failures.forEach(f => console.log(` - ${f}`));
  process.exitCode = 1;
} else {
  console.log(`\n${files.length} diagramas válidos y renderizados.`);
}
