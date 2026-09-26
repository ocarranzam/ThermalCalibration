// Casos de auditoría de /api/v1/companies y /api/v1/equipment (HU-01, sprint 2).
const C = '/api/v1/companies';
const CI = '/api/v1/companies/{id}';
const CE = '/api/v1/companies/{id}/equipment';
const EI = '/api/v1/equipment/{id}';

// RUC válido y distinto en cada ejecución: prefijo 20, 8 dígitos y dígito verificador módulo 11.
const WEIGHTS = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
let sequence = Date.now() % 100_000_000;
const newTaxId = () => {
  const body = `20${String(sequence++ % 100_000_000).padStart(8, '0')}`;
  const sum = WEIGHTS.reduce((acc, weight, i) => acc + Number(body[i]) * weight, 0);
  return body + String((11 - (sum % 11)) % 10);
};

export async function cases(check) {
  const taxId = newTaxId();
  const post = (label, body, expected, extra = {}) =>
    check(label, { method: 'POST', path: C, operation: C, token: 'technician', body, ...extra }, expected);

  const created = await post('POST empresa válida (técnico)', { taxId, name: `Laboratorios Andinos ${taxId}`, email: 'calidad@andinos.pe' }, 201);
  const id = created.json?.id;
  const duplicate = await post('POST RUC duplicado', { taxId, name: 'Otra' }, 409);
  if (duplicate.json && duplicate.json.existingId !== id) duplicate.problems.push('falta existingId de la empresa existente');
  await post('POST RUC de 10 dígitos', { taxId: '2010007097', name: 'X' }, 400);
  await post('POST RUC con letra', { taxId: '2010007097A', name: 'X' }, 400);
  await post('POST RUC prefijo 30', { taxId: '30100070970', name: 'X' }, 400);
  await post('POST RUC dígito verificador', { taxId: '20100070971', name: 'X' }, 400);
  await post('POST sin razón social', { taxId: newTaxId() }, 400);
  await post('POST correo inválido', { taxId: newTaxId(), name: 'X', email: 'calidad' }, 400);
  await post('POST propiedad extra', { taxId: newTaxId(), name: 'X', color: 'rojo' }, 400);
  await post('POST sin token', { taxId: newTaxId(), name: 'X' }, 401, { token: undefined });
  await post('POST text/plain', undefined, 415, { headers: { 'Content-Type': 'text/plain' }, rawBody: 'x' });

  await check('GET buscar por RUC', { method: 'GET', path: `${C}?taxId=${taxId}`, operation: C, token: 'technician' }, 200);
  const byName = await check('GET buscar por razón social', { method: 'GET', path: `${C}?name=andinos%20${taxId}`, operation: C, token: 'technician' }, 200);
  if (byName.json && byName.json.length !== 1) byName.problems.push('se esperaba 1 resultado');
  const got = await check('GET empresa', { method: 'GET', path: `${C}/${id}`, operation: CI, token: 'technician' }, 200);
  await check('GET empresa inexistente', { method: 'GET', path: `${C}/99999`, operation: CI, token: 'technician' }, 404);

  const putCompany = (label, body, expected, extra = {}) =>
    check(label, { method: 'PUT', path: `${C}/${id}`, operation: CI, token: 'admin', body, ...extra }, expected);
  await putCompany('PUT empresa If-Match vigente', { name: `Laboratorios Andinos ${taxId}`, phone: '999 888 777', isActive: true }, 200,
    { headers: { 'If-Match': got.etag } });
  await putCompany('PUT empresa If-Match viejo', { name: 'X', isActive: true }, 412, { headers: { 'If-Match': got.etag } });
  await putCompany('PUT empresa sin isActive', { name: 'X' }, 400);
  await putCompany('PUT empresa con RUC (no editable)', { taxId, name: 'X', isActive: true }, 400);

  // Equipos
  const equipmentPath = `${C}/${id}/equipment`;
  const postEquipment = (label, body, expected, extra = {}) =>
    check(label, { method: 'POST', path: equipmentPath, operation: CE, token: 'technician', body, ...extra }, expected);
  const freezer = { equipmentTypeId: 2, brand: 'Haier', model: 'HBF-205', serialNumber: 'SN-88231' };

  const equipment = await postEquipment('POST equipo válido', freezer, 201);
  if (equipment.json && equipment.json.isModelConfirmed !== true) equipment.problems.push('el modelo debería quedar confirmado');
  const inferred = await postEquipment('POST equipo con modelo inferido', { ...freezer, brand: 'Memmert', model: 'TTC256', isModelConfirmed: false, serialNumber: 'DATA-1' }, 201);
  if (inferred.json && inferred.json.isModelConfirmed !== false) inferred.problems.push('el modelo debería quedar pendiente');
  await postEquipment('POST serie duplicada', freezer, 409);
  await postEquipment('POST sin serie', { ...freezer, serialNumber: undefined }, 400);
  await postEquipment('POST sin tipo', { ...freezer, equipmentTypeId: undefined }, 400);
  await postEquipment('POST sin marca', { ...freezer, brand: undefined }, 400);
  await postEquipment('POST sin modelo', { ...freezer, model: '' }, 400);
  await postEquipment('POST tipo inexistente', { ...freezer, equipmentTypeId: 99999, serialNumber: 'SN-X' }, 400);
  await check('POST equipo de empresa inexistente', { method: 'POST', path: `${C}/99999/equipment`, operation: CE, token: 'technician', body: freezer }, 404);

  const other = await post('POST otra empresa', { taxId: newTaxId(), name: `Otra ${taxId}` }, 201);
  await check('POST misma serie en otra empresa', { method: 'POST', path: `${C}/${other.json?.id}/equipment`, operation: CE, token: 'technician', body: freezer }, 201);

  const list = await check('GET equipos de la empresa', { method: 'GET', path: equipmentPath, operation: CE, token: 'technician' }, 200);
  if (list.json && list.json.length !== 2) list.problems.push('se esperaban 2 equipos');
  await check('GET equipos de empresa inexistente', { method: 'GET', path: `${C}/99999/equipment`, operation: CE, token: 'technician' }, 404);

  const equipmentId = inferred.json?.id;
  const at = `/api/v1/equipment/${equipmentId}`;
  const gotEquipment = await check('GET equipo', { method: 'GET', path: at, operation: EI, token: 'technician' }, 200);
  await check('GET equipo inexistente', { method: 'GET', path: '/api/v1/equipment/99999', operation: EI, token: 'technician' }, 404);
  const putEquipment = (label, body, expected, extra = {}) =>
    check(label, { method: 'PUT', path: at, operation: EI, token: 'technician', body, ...extra }, expected);
  const confirmed = { equipmentTypeId: 2, brand: 'Memmert', model: 'TTC256', isModelConfirmed: true, serialNumber: 'DATA-1', isActive: true };
  await putEquipment('PUT confirmar modelo', confirmed, 200, { headers: { 'If-Match': gotEquipment.etag } });
  await putEquipment('PUT equipo If-Match viejo', confirmed, 412, { headers: { 'If-Match': gotEquipment.etag } });
  await putEquipment('PUT serie de otro equipo', { ...confirmed, serialNumber: 'SN-88231' }, 409);
  await putEquipment('PUT sin isModelConfirmed', { ...confirmed, isModelConfirmed: undefined }, 400);
  await putEquipment('PUT equipo sin token', confirmed, 401, { token: undefined });
}
