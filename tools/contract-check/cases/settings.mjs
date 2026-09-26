// Casos de auditoría de /api/v1/settings (D-08) y /api/v1/thermocouple-types (sprint 1, HU-02).
// Los parámetros se restauran al final a los valores leídos al empezar.
const S = '/api/v1/settings';
const T = '/api/v1/thermocouple-types';

export async function cases(check) {
  const get = (label, token, expected) => check(label, { method: 'GET', path: S, operation: S, token }, expected);
  const put = (label, body, expected, extra = {}) =>
    check(label, { method: 'PUT', path: S, operation: S, token: 'admin', body, ...extra }, expected);

  const original = await get('GET parámetros técnico', 'technician', 200);
  await get('GET parámetros sin token', undefined, 401);
  const { minValidSamples, updatedAt, ...values } = original.json ?? {};

  const changed = { ...values, restPeriodMinutes: values.restPeriodMinutes === 20 ? 25 : 20 };
  const saved = await put('PUT If-Match vigente', changed, 200, { headers: { 'If-Match': original.etag } });
  if (saved.json && saved.json.restPeriodMinutes !== changed.restPeriodMinutes) {
    saved.problems.push('la respuesta no refleja el valor guardado');
  }
  await put('PUT If-Match viejo', values, 412, { headers: { 'If-Match': original.etag } });
  await put('PUT intervalo que no divide la base', { ...values, samplingIntervalSeconds: 70 }, 400);
  await put('PUT intervalo fuera de rango', { ...values, samplingIntervalSeconds: 10 }, 400);
  await put('PUT umbral 100', { ...values, sensorLossThresholdPct: 100 }, 400);
  await put('PUT umbral con 3 decimales', { ...values, sensorLossThresholdPct: 60.125 }, 400);
  await put('PUT máximo menor que la base', { ...values, maxSessionMinutes: values.baseSessionMinutes - 1 }, 400);
  const { restPeriodMinutes, ...missing } = values;
  await put('PUT sin un parámetro', missing, 400);
  await put('PUT propiedad extra', { ...values, color: 'rojo' }, 400);
  await put('PUT técnico', values, 403, { token: 'technician' });
  await put('PUT text/plain', undefined, 415, { headers: { 'Content-Type': 'text/plain' }, rawBody: 'x' });
  const restored = await put('PUT restaurar valores iniciales', values, 200, { headers: { 'If-Match': saved.etag } });
  if (restored.json && restored.json.minValidSamples !== minValidSamples) {
    restored.problems.push('minValidSamples no coincide con el original');
  }

  const types = await check('GET tipos de termopar', { method: 'GET', path: T, operation: T, token: 'technician' }, 200);
  if (types.json && !types.json.some(t => t.code === 'T')) types.problems.push('falta el termopar tipo T');
  await check('GET tipos de termopar sin token', { method: 'GET', path: T, operation: T }, 401);
}
