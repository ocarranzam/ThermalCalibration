// Casos de auditoría del recurso /api/v1/equipment-types (HU-02).
// check(label, request, expectedStatus) devuelve { status, etag, json, problems }.
const C = '/api/v1/equipment-types';
const I = '/api/v1/equipment-types/{id}';

export async function cases(check) {
  const name = `Auditoria ${Date.now()}`;
  const post = (label, body, expected, extra = {}) =>
    check(label, { method: 'POST', path: C, operation: C, token: 'admin', body, ...extra }, expected);

  const created = await post('POST válido', { name, maxTemperatureC: -60, minSessionDurationMinutes: 60, description: 'Vacunas' }, 201);
  const id = created.json?.id;
  const at = `${C}/${id}`;
  const put = (label, body, expected, extra = {}) =>
    check(label, { method: 'PUT', path: at, operation: I, token: 'admin', body, ...extra }, expected);

  await post('POST límite pendiente', { name: `${name} P`, maxTemperatureC: null }, 201);
  const band = await post('POST banda 27 puntos (D-05, D-06)', { name: `${name} B`, limitMode: 'Band', toleranceK: 0.5, minMeasurementPoints: 27 }, 201);
  if (band.json && (band.json.limitMode !== 'Band' || band.json.minMeasurementPoints !== 27 || band.json.maxTemperatureC !== null)) {
    band.problems.push('la respuesta no refleja la banda de 27 puntos');
  }
  await post('POST máximo con tolerancia', { name: 'X', limitMode: 'Maximum', toleranceK: 1 }, 400);
  await post('POST banda con máximo', { name: 'X', limitMode: 'Band', maxTemperatureC: 5 }, 400);
  await post('POST modo inválido', { name: 'X', limitMode: 'Rango' }, 400);
  await post('POST 28 puntos', { name: 'X', minMeasurementPoints: 28 }, 400);
  await post('POST 3 decimales', { name: 'X', maxTemperatureC: -5.123 }, 400);
  await post('POST duración 45', { name: 'X', minSessionDurationMinutes: 45 }, 400);
  await post('POST sin nombre', { maxTemperatureC: 2 }, 400);
  await post('POST propiedad extra', { name: 'Y', color: 'rojo' }, 400);
  await post('POST cuerpo vacío', undefined, 400, { headers: { 'Content-Type': 'application/json' }, rawBody: '' });
  await post('POST duplicado', { name: name.toUpperCase() }, 409);
  await post('POST técnico', { name: 'W' }, 403, { token: 'technician' });
  await post('POST sin token', { name: 'W' }, 401, { token: undefined });
  await post('POST text/plain', undefined, 415, { headers: { 'Content-Type': 'text/plain' }, rawBody: 'hola' });

  const got = await check('GET técnico', { method: 'GET', path: at, operation: I, token: 'technician' }, 200);
  await check('GET inexistente', { method: 'GET', path: `${C}/99999`, operation: I, token: 'technician' }, 404);
  await check('GET sin token', { method: 'GET', path: at, operation: I }, 401);

  await put('PUT If-Match vigente', { name, maxTemperatureC: -70, isActive: true }, 200, { headers: { 'If-Match': got.etag } });
  await put('PUT If-Match viejo', { name, isActive: true }, 412, { headers: { 'If-Match': got.etag } });
  await put('PUT If-Match inválido', { name, isActive: true }, 412, { headers: { 'If-Match': '"no-base64!"' } });
  await put('PUT If-Match *', { name, maxTemperatureC: -70, isActive: true }, 200, { headers: { 'If-Match': '*' } });
  await put('PUT sin isActive', { name }, 400);
  await put('PUT nombre existente', { name: 'Refrigeradora', isActive: true }, 409);
  await put('PUT desactivar', { name, isActive: false }, 200);
  await check('PUT inexistente', { method: 'PUT', path: `${C}/99999`, operation: I, token: 'admin', body: { name: 'Nada', isActive: true } }, 404);
  await put('PUT técnico', { name, isActive: true }, 403, { token: 'technician' });
  await put('PUT text/plain', undefined, 415, { headers: { 'Content-Type': 'text/plain' }, rawBody: 'x' });
}
