// Contrasta respuestas reales de la API con docs/api/thermal-v1.yaml: código de estado documentado,
// Content-Type, schema del cuerpo (Ajv) y cabeceras declaradas. Ver README.md de esta carpeta.
//
// Uso:  node contract-check.mjs [baseUrl]      (por defecto http://localhost:8080)
// Env:  ADMIN_TOKEN y TECHNICIAN_TOKEN (JWT de `dotnet user-jwts`).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import * as yaml from 'js-yaml';
import AjvModule from 'ajv';
import addFormatsModule from 'ajv-formats';
import { cases as equipmentTypeCases } from './cases/equipment-types.mjs';

const Ajv = AjvModule.default ?? AjvModule;
const addFormats = addFormatsModule.default ?? addFormatsModule;

// Añadir aquí los casos de cada entidad nueva (un archivo por recurso en ./cases).
const SUITES = [equipmentTypeCases];

const here = path.dirname(fileURLToPath(import.meta.url));
const base = process.argv[2] ?? 'http://localhost:8080';
const tokens = { admin: process.env.ADMIN_TOKEN, technician: process.env.TECHNICIAN_TOKEN };
if (!tokens.admin || !tokens.technician) {
  console.error('Faltan ADMIN_TOKEN y TECHNICIAN_TOKEN (ver README.md).');
  process.exit(2);
}

const spec = yaml.load(fs.readFileSync(path.resolve(here, '../../docs/api/thermal-v1.yaml'), 'utf8'));
const ajv = new Ajv({ strict: false, allErrors: true });
addFormats(ajv);

// OpenAPI 3.0 → JSON Schema: `nullable` pasa a tipo con null y las $ref apuntan a un esquema registrado.
const toJsonSchema = schema => JSON.parse(JSON.stringify(schema), (_, value) => {
  if (value && typeof value === 'object') {
    if (value.nullable === true && value.type) {
      value.type = [value.type, 'null'];
      delete value.nullable;
    }
    if (typeof value.$ref === 'string') value.$ref = value.$ref.replace('#/components/schemas/', 'schemas#/');
  }
  return value;
});
ajv.addSchema({ $id: 'schemas', ...toJsonSchema(spec.components.schemas) }, 'schemas');
const deref = node => (node?.$ref ? node.$ref.split('/').slice(1).reduce((acc, key) => acc[key], spec) : node);

async function request({ method, path: urlPath, operation, token, body, rawBody, headers = {} }) {
  const allHeaders = { ...headers };
  if (token) allHeaders.Authorization = `Bearer ${tokens[token]}`;
  if (body !== undefined) allHeaders['Content-Type'] = 'application/json';
  const res = await fetch(base + urlPath, {
    method,
    headers: allHeaders,
    body: rawBody ?? (body !== undefined ? JSON.stringify(body) : undefined),
  });
  const text = await res.text();
  const problems = [];
  const op = spec.paths[operation]?.[method.toLowerCase()];
  const documented = op && deref(op.responses[String(res.status)]);

  if (!op) problems.push(`operación ${method} ${operation} no existe en el contrato`);
  else if (!documented) problems.push(`código ${res.status} no documentado`);
  else {
    const contentType = (res.headers.get('content-type') || '').split(';')[0];
    if (documented.content) {
      const media = documented.content[contentType];
      if (!media) problems.push(`content-type "${contentType}" no documentado`);
      else {
        const schema = toJsonSchema(media.schema);
        const validate = ajv.compile(schema.$ref ? { $ref: schema.$ref } : schema);
        let json;
        try { json = JSON.parse(text); } catch { problems.push('el cuerpo no es JSON'); }
        if (json !== undefined && !validate(json)) problems.push(`schema: ${ajv.errorsText(validate.errors)}`);
      }
    }
    for (const name of Object.keys(documented.headers || {})) {
      if (!res.headers.get(name)) problems.push(`falta la cabecera ${name}`);
    }
  }

  let json = null;
  try { json = JSON.parse(text); } catch { /* sin cuerpo JSON */ }
  return { status: res.status, etag: res.headers.get('etag'), json, problems };
}

const results = [];
for (const suite of SUITES) {
  await suite(async (label, req, expectedStatus) => {
    const result = await request(req);
    if (expectedStatus && result.status !== expectedStatus) {
      result.problems.push(`se esperaba ${expectedStatus}`);
    }
    results.push({ label, ...result });
    return result;
  });
}

for (const r of results) {
  console.log(`${r.problems.length ? 'DRIFT' : 'OK   '} ${String(r.status).padEnd(4)} ${r.label}${r.problems.length ? ` → ${r.problems.join('; ')}` : ''}`);
}
const ok = results.filter(r => r.problems.length === 0).length;
console.log(`\n${ok}/${results.length} conformes`);
process.exitCode = ok === results.length ? 0 : 1;
