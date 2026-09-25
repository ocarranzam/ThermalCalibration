#!/usr/bin/env node
/* =====================================================================
   Generador del set de datos de prueba
   Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración

   Uso:   node test-data/generate-test-data.mjs
   Salida: test-data/scenarios/<código>/{scenario.json, readings.csv, transcript.log}
           test-data/scenarios/index.json

   Especificación: docs/specs/functional/06-test-data.md
   Los datos son pseudoaleatorios con semilla fija: cada ejecución produce
   exactamente los mismos archivos. Los resultados esperados se calculan con
   las mismas reglas de la especificación (oráculo de referencia).
   ===================================================================== */

import { mkdirSync, rmSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = dirname(fileURLToPath(import.meta.url));
const OUT = join(ROOT, 'scenarios');

const PROTOCOL_VERSION = '1.0';
// [PC-01] Punto de cambio: intervalo de muestreo (docs/specs/functional/01-vision-document.md §12).
// Todo lo demás (muestras mínimas, última muestra, 30 min de falla) se deriva de este valor.
const INTERVAL_S = 120;
const DAY_S = 86400;
const BASE_SESSION_MIN = 60;                                // sesión base de 1 h
const MIN_VALID_SAMPLES = (BASE_SESSION_MIN * 60) / INTERVAL_S + 1; // 31 (1 h de datos válidos)
const SENSOR_LOSS_CRITICAL_AFTER = 3;                       // 3.ª muestra afectada consecutiva: alerta crítica
const SENSOR_LOSS_FAIL_MIN = 30;                            // 30 min consecutivos sin datos suficientes: sesión fallida
const ABOVE_LIMIT_CRITICAL_MIN = 30;                        // un canal fuera de límite 30 min seguidos: alerta crítica
const MIN_MEASUREMENT_POINTS = 9;                           // IEC 60068-3-5 / DKD-R 5-7: 8 esquinas + centro
const RETRY_DELAY_S = 5;
const RECONNECT_LEAD_S = 20;      // el puerto reaparece 20 s antes de la muestra de reanudación
const IDENTITY_CHECK_S = 3;       // la comprobación de IDN termina 3 s después

// Rango físico por tipo de termopar, en centésimas de °C (dbo.ThermocoupleType)
const THERMOCOUPLE_RANGE = { T: [-20000, 35000], K: [-20000, 126000] };
const DEVICE_STATUS_TO_SENSOR_STATUS = { OC: 'OpenCircuit', SC: 'ShortCircuit', OR: 'OutOfRange' };
const FAULT_STATUSES = ['OpenCircuit', 'ShortCircuit', 'OutOfRange', 'InvalidFrame'];
const PERSISTENT_FRAME_FAULTS = ['BAD_CHECKSUM', 'TRUNCATED', 'MISSING'];

const POSITIONS = [
  ['Superior', 1.2], ['Centro', 0.0], ['Inferior', -0.8], ['Puerta', 2.2], ['Fondo', -0.5],
  ['Lateral izq.', 0.4], ['Lateral der.', 0.3], ['Esquina sup. izq.', 1.0], ['Esquina inf. der.', -1.0], ['Bandeja media', 0.1],
];

/* ---------------------------------------------------------------------
   Utilidades
   --------------------------------------------------------------------- */

function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function gauss(rnd) {
  const u = Math.max(rnd(), 1e-12);
  return Math.sqrt(-2 * Math.log(u)) * Math.cos(2 * Math.PI * rnd());
}

const randInt = (rnd, min, max) => min + Math.floor(rnd() * (max - min + 1));
const offsetOf = (sample) => (sample - 1) * INTERVAL_S;
const toCents = (value, decimals) => (decimals === 1 ? Math.round(value * 10) * 10 : Math.round(value * 100));
const centsToText = (cents, decimals) => (cents / 100).toFixed(decimals);
const centsToNumber = (cents) => cents / 100;

function checksum(payload) {
  let cs = 0;
  for (const ch of payload) cs ^= ch.charCodeAt(0);
  return cs.toString(16).toUpperCase().padStart(2, '0');
}

const frame = (payload) => `$${payload}*${checksum(payload)}`;

// Cambia un dígito del valor conservando el checksum original: el XOR ya no coincide
function corruptChecksum(line) {
  const star = line.lastIndexOf('*');
  const body = line.slice(0, star);
  for (let i = body.length - 1; i > 0; i--) {
    if (/[0-9]/.test(body[i])) {
      const flipped = String((Number(body[i]) + 1) % 10);
      return body.slice(0, i) + flipped + body.slice(i + 1) + line.slice(star);
    }
  }
  return line.slice(0, star) + '*00';
}

const truncate = (line) => line.slice(0, Math.ceil(line.length * 0.6));

function channelMask(channels) {
  let mask = 0;
  for (const c of channels) mask |= 1 << (c.channelNumber - 1);
  return mask.toString(16).toUpperCase().padStart(3, '0');
}

function defaultChannels(count, types = null) {
  return Array.from({ length: count }, (_, i) => ({
    channelNumber: i + 1,
    thermocoupleType: types ? types[i] : 'T',
    position: POSITIONS[i][0],
    offsetC: POSITIONS[i][1],
  }));
}

/* ---------------------------------------------------------------------
   Reglas de la especificación (oráculo)
   --------------------------------------------------------------------- */

// 02-serial-protocol.md §7: orden V1..V10
function classify(row, limitCents) {
  if (PERSISTENT_FRAME_FAULTS.includes(row.frameFault)) {
    return { sensorStatus: 'InvalidFrame', temperatureCents: null, isAboveLimit: false };
  }
  if (row.deviceStatus !== 'OK') {
    return { sensorStatus: DEVICE_STATUS_TO_SENSOR_STATUS[row.deviceStatus], temperatureCents: null, isAboveLimit: false };
  }
  if (row.reportedType !== row.declaredType) {
    return { sensorStatus: 'TypeMismatch', temperatureCents: row.cents, isAboveLimit: false };
  }
  const [min, max] = THERMOCOUPLE_RANGE[row.declaredType];
  if (row.cents < min || row.cents > max) {
    return { sensorStatus: 'OutOfRange', temperatureCents: null, isAboveLimit: false };
  }
  return { sensorStatus: 'OK', temperatureCents: row.cents, isAboveLimit: limitCents !== null && row.cents > limitCents };
}

// Muestra afectada: proporción de canales sin lectura OK estrictamente mayor que el umbral
const isAffected = (active, valid, thresholdPct) => (active - valid) * 100 > thresholdPct * active;

/* ---------------------------------------------------------------------
   Construcción de un escenario
   --------------------------------------------------------------------- */

function build(def) {
  const rnd = mulberry32(def.seed);
  const mode = def.mode ?? 'Poll';
  const thresholdPct = def.thresholdPct ?? 60;
  const limitCents = def.limit === null ? null : Math.round(def.limit * 100);
  const device = { deviceIdentifier: 'ADQ-SIM-0001', platform: 'Simulator', firmwareVersion: '1.0.0', channelCount: 10, sensorGroupId: 'GRP-A', ...def.device };
  const channels = def.channels.map((c) => ({ ...c, phase: rnd() * 2 * Math.PI }));
  const events = def.events ?? [];

  const plannedSeconds = def.plannedMinutes * 60;

  // Cierre y huecos
  const lost = events.find((e) => e.type === 'COMM_LOST');
  let restored = events.find((e) => e.type === 'COMM_RESTORED');
  let mismatch = restored && (restored.deviceIdentifier !== device.deviceIdentifier || restored.sensorGroupId !== device.sensorGroupId);

  let endedAt;
  let closeReason;
  if (mismatch) {
    endedAt = offsetOf(restored.sample) - RECONNECT_LEAD_S + IDENTITY_CHECK_S;
    closeReason = 'DeviceMismatch';
  } else if (def.close?.type === 'Manual') {
    endedAt = def.close.atOffsetSeconds;
    closeReason = 'Manual';
  } else {
    endedAt = plannedSeconds;
    closeReason = 'PlannedDuration';
  }
  let programmedSamples = Math.floor(endedAt / INTERVAL_S) + 1;
  if (lost && !restored) closeReason = 'CommunicationLost';

  let gapSamples = new Set();
  if (lost) {
    const last = restored && !mismatch ? restored.sample - 1 : programmedSamples;
    for (let n = lost.sample; n <= last; n++) gapSamples.add(n);
  }

  // Lecturas simuladas
  const rows = [];
  const jitter = {};
  for (let n = 1; n <= programmedSamples; n++) {
    if (gapSamples.has(n)) continue;
    const t = offsetOf(n);
    jitter[n] = mode === 'Stream' && n > 1 ? randInt(rnd, 0, 2) : 0;
    for (const ch of channels) {
      const base = def.setpoint + ch.offsetC
        + (def.cycleAmp ?? 0.8) * Math.sin((2 * Math.PI * t) / ((def.cyclePeriodMin ?? 40) * 60) + ch.phase)
        + (def.dailyAmp ?? 0) * Math.sin((2 * Math.PI * t) / DAY_S)
        + (def.noise ?? 0.15) * gauss(rnd);
      const row = {
        sample: n,
        offsetSeconds: t + jitter[n],
        channel: ch.channelNumber,
        declaredType: ch.thermocoupleType,
        reportedType: ch.reportedType ?? ch.thermocoupleType,
        value: base,
        decimals: ch.decimals ?? 2,
        deviceStatus: 'OK',
        frameFault: 'NONE',
      };
      def.mutate?.(row);
      row.cents = toCents(row.value, row.decimals);
      Object.assign(row, classify(row, limitCents));
      rows.push(row);
    }
  }

  // Resultados esperados
  const hasMixed = new Set(channels.map((c) => c.thermocoupleType)).size > 1;
  const alerts = [];
  if (hasMixed) alerts.push({ alertType: 'MixedThermocoupleTypes', severity: 'Warning', sampleNumber: null, channel: null });
  const isBelowMinPoints = channels.length < MIN_MEASUREMENT_POINTS;
  if (isBelowMinPoints) alerts.push({ alertType: 'BelowMinimumPoints', severity: 'Warning', sampleNumber: null, channel: null, activeChannels: channels.length, minimumPoints: MIN_MEASUREMENT_POINTS });
  if (limitCents === null) alerts.push({ alertType: 'LimitNotDefined', severity: 'Info', sampleNumber: null, channel: null });

  const byChannel = Object.fromEntries(channels.map((c) => [c.channelNumber, { fault: false, mismatch: false, aboveRun: 0, aboveEscalated: false }]));
  const affectedSamples = [];
  let previousAffected = false;   // episodios de SensorLoss (solo muestras con lecturas)
  let run = 0;                    // muestras afectadas consecutivas (incluye huecos de comunicación)
  let escalated = false;
  let failedAt = null;
  const rowsBySample = new Map();
  for (const r of rows) {
    if (!rowsBySample.has(r.sample)) rowsBySample.set(r.sample, []);
    rowsBySample.get(r.sample).push(r);
  }

  for (let n = 1; n <= programmedSamples; n++) {
    let affected;
    let hasData;
    if (gapSamples.has(n)) {
      if (n === lost.sample) alerts.push({ alertType: 'CommunicationLost', severity: 'Critical', sampleNumber: n, channel: null });
      affected = true;
      hasData = false;
      for (const st of Object.values(byChannel)) { st.aboveRun = 0; st.aboveEscalated = false; }
    } else {
      const sampleRows = rowsBySample.get(n);
      // Causa probable si un canal lleva 30 min fuera de límite: mayoría de canales válidos fuera de límite = equipo
      const okRows = sampleRows.filter((r) => r.sensorStatus === 'OK');
      const suspectedCause = okRows.filter((r) => r.isAboveLimit).length * 2 >= okRows.length ? 'Equipment' : 'Sensor';
      for (const r of sampleRows) {
        const st = byChannel[r.channel];
        const fault = FAULT_STATUSES.includes(r.sensorStatus);
        if (fault && !st.fault) alerts.push({ alertType: 'SensorFault', severity: 'Warning', sampleNumber: n, channel: r.channel, sensorStatus: r.sensorStatus });
        st.fault = fault;
        const mm = r.sensorStatus === 'TypeMismatch';
        if (mm && !st.mismatch) alerts.push({ alertType: 'TypeMismatch', severity: 'Warning', sampleNumber: n, channel: r.channel, reportedType: r.reportedType });
        st.mismatch = mm;
        if (r.isAboveLimit) {
          alerts.push({ alertType: 'AboveLimit', severity: 'Warning', sampleNumber: n, channel: r.channel, valueC: centsToNumber(r.temperatureCents), limitC: centsToNumber(limitCents) });
          st.aboveRun++;
          if (!st.aboveEscalated && (st.aboveRun - 1) * INTERVAL_S >= ABOVE_LIMIT_CRITICAL_MIN * 60) {
            alerts.push({ alertType: 'AboveLimitSustained', severity: 'Critical', sampleNumber: n, channel: r.channel, valueC: centsToNumber(r.temperatureCents), limitC: centsToNumber(limitCents), consecutiveSamples: st.aboveRun, suspectedCause });
            st.aboveEscalated = true;
          }
        } else {
          st.aboveRun = 0;
          st.aboveEscalated = false;
        }
      }
      const valid = sampleRows.filter((r) => r.sensorStatus === 'OK').length;
      affected = isAffected(channels.length, valid, thresholdPct);
      hasData = true;
      if (affected && !previousAffected) alerts.push({ alertType: 'SensorLoss', severity: 'Warning', sampleNumber: n, channel: null, channelsWithoutData: channels.length - valid, activeChannels: channels.length });
      previousAffected = affected;
    }

    if (!affected) {
      run = 0;
      escalated = false;
      continue;
    }
    affectedSamples.push(n);
    run++;
    // Escalamiento: la pérdida no se restablece en la 3.ª medición consecutiva
    if (hasData && run >= SENSOR_LOSS_CRITICAL_AFTER && !escalated) {
      alerts.push({ alertType: 'SensorLossPersistent', severity: 'Critical', sampleNumber: n, channel: null, consecutiveSamples: run });
      escalated = true;
    }
    // Falla de sesión: 30 min consecutivos sin datos suficientes
    if ((run - 1) * INTERVAL_S >= SENSOR_LOSS_FAIL_MIN * 60) {
      failedAt = n;
      alerts.push({ alertType: 'SessionFailed', severity: 'Critical', sampleNumber: n, channel: null, consecutiveSamples: run });
      break;
    }
  }

  if (failedAt !== null) {
    // La sesión termina en la muestra de la falla: se descarta lo posterior
    programmedSamples = failedAt;
    endedAt = offsetOf(failedAt);
    closeReason = 'DataLoss';
    for (let i = rows.length - 1; i >= 0; i--) if (rows[i].sample > failedAt) rows.splice(i, 1);
    gapSamples = new Set([...gapSamples].filter((n) => n <= failedAt));
    if (restored && restored.sample > failedAt) {
      restored = undefined;
      mismatch = false;
    }
  }
  if (mismatch) alerts.push({ alertType: 'DeviceMismatch', severity: 'Critical', sampleNumber: null, channel: null, deviceIdentifier: restored.deviceIdentifier, sensorGroupId: restored.sensorGroupId });

  const validSamples = programmedSamples - affectedSamples.length;
  const status = failedAt !== null || mismatch ? 'Invalid'
    : endedAt >= plannedSeconds && validSamples >= MIN_VALID_SAMPLES ? 'Completed' : 'Incomplete';

  const communicationGaps = lost && gapSamples.size > 0 ? [{
    lostAtOffsetSeconds: offsetOf(lost.sample),
    recoveredAtOffsetSeconds: restored && !mismatch ? offsetOf(restored.sample) - RECONNECT_LEAD_S + IDENTITY_CHECK_S : null,
    firstMissedSample: lost.sample,
    lastMissedSample: Math.max(...gapSamples),
    missedSamples: gapSamples.size,
    notes: mismatch ? `Reconectó un adquisidor distinto (${restored.deviceIdentifier} / ${restored.sensorGroupId})`
      : failedAt !== null ? 'Sesión fallida por pérdida sostenida de datos' : null,
  }] : [];

  const readingsByStatus = {};
  for (const r of rows) readingsByStatus[r.sensorStatus] = (readingsByStatus[r.sensorStatus] ?? 0) + 1;
  const alertsByType = {};
  for (const a of alerts) alertsByType[a.alertType] = (alertsByType[a.alertType] ?? 0) + 1;

  const scenario = {
    code: def.code,
    title: def.title,
    category: def.category,
    description: def.description,
    protocolVersion: PROTOCOL_VERSION,
    seed: def.seed,
    config: {
      isSimulation: true,
      equipmentType: def.equipmentType,
      maxTemperatureC: def.limit,
      equipmentTypeMinSessionDurationMinutes: def.equipmentTypeMinSessionDurationMinutes ?? BASE_SESSION_MIN,
      plannedDurationMinutes: def.plannedMinutes,
      durationSource: def.durationSource,
      clientRequestReference: def.clientRequestReference ?? null,
      samplingIntervalSeconds: INTERVAL_S,
      sensorLossThresholdPct: thresholdPct,
      sensorLossCriticalAfterSamples: SENSOR_LOSS_CRITICAL_AFTER,
      sensorLossFailMinutes: SENSOR_LOSS_FAIL_MIN,
      aboveLimitCriticalMinutes: ABOVE_LIMIT_CRITICAL_MIN,
      minMeasurementPoints: MIN_MEASUREMENT_POINTS,
      acquisitionMode: mode,
      device,
      channels: channels.map(({ channelNumber, thermocoupleType, position }) => ({ channelNumber, thermocoupleType, position })),
      close: def.close ?? { type: 'PlannedDuration' },
    },
    events,
    expected: {
      status,
      closeReason,
      startedAtOffsetSeconds: 0,
      endedAtOffsetSeconds: endedAt,
      hasMixedThermocoupleTypes: hasMixed,
      isBelowMinimumPoints: isBelowMinPoints,
      programmedSamples,
      samplesWithData: programmedSamples - gapSamples.size,
      affectedSamples,
      validSamples,
      readingRows: rows.length,
      readingsByStatus,
      aboveLimitReadings: rows.filter((r) => r.isAboveLimit).length,
      alertsByType,
      alerts,
      communicationGaps,
      discardedPreStartBlocks: mode === 'Stream' ? 2 : 0,
    },
  };

  return { scenario, rows, transcript: buildTranscript(def, { mode, device, channels, rows, rowsBySample, gapSamples, programmedSamples, lost, restored, mismatch, failedAt, endedAt, jitter }) };
}

/* ---------------------------------------------------------------------
   Transcripción serial (golden transcript)
   --------------------------------------------------------------------- */

function rdLine(r, seq) {
  const value = r.deviceStatus === 'OK' ? centsToText(r.cents, r.decimals) : '';
  return frame(`RD,${seq},${r.channel},${r.reportedType},${value},${r.deviceStatus}`);
}

function buildTranscript(def, ctx) {
  const { mode, device, channels, rowsBySample, gapSamples, programmedSamples, lost, restored, mismatch, endedAt, jitter } = ctx;
  const lines = [];
  const push = (t, dir, text) => lines.push({ t, dir, text });
  const idn = (d) => frame(`IDN,${PROTOCOL_VERSION},${d.deviceIdentifier},${d.platform},${d.firmwareVersion},${d.channelCount},${mode.toUpperCase()},${d.sensorGroupId}`);
  const mask = channelMask(channels);
  const reboots = new Set((def.events ?? []).filter((e) => e.type === 'DEVICE_REBOOT').map((e) => e.sample));

  const emitBlock = (t0, seq, sampleRows, attempt) => {
    let t = t0;
    let sent = 0;
    for (const r of sampleRows) {
      t += 0.12;
      const good = rdLine(r, seq);
      let text = good;
      if (r.frameFault === 'MISSING') continue;
      if (r.frameFault === 'BAD_CHECKSUM' || (r.frameFault === 'BAD_CHECKSUM_RECOVERED' && attempt === 1)) text = corruptChecksum(good);
      if (r.frameFault === 'TRUNCATED') text = truncate(good);
      push(t, 'ADQ', text);
      sent++;
    }
    push(t + 0.05, 'ADQ', frame(`EOS,${seq},${sent}`));
  };

  if (mode === 'Stream') {
    // El adquisidor ya transmitía antes de que la PC abriera el puerto
    const seq0 = 1037;
    const previewRows = rowsBySample.get(1);
    push(-250, '#', 'PC abre el puerto 115200 8N1; el adquisidor ya estaba transmitiendo');
    push(-249.8, 'ADQ', rdLine(previewRows[0], seq0).slice(9));
    push(-249, 'PC', frame('IDN'));
    push(-248.95, 'ADQ', idn(device));
    push(-240, '#', 'Bloque previo al inicio: solo vista previa, no se almacena');
    emitBlock(-240, seq0 + 1, previewRows, 1);
    push(-120, '#', 'Bloque previo al inicio: solo vista previa, no se almacena');
    emitBlock(-120, seq0 + 2, previewRows, 1);
    push(-60, '#', 'El técnico pulsa Iniciar captura');
    push(-60, 'PC', frame(`START,${mask}`));
    push(-59.95, 'ADQ', frame('ACK,START'));
    for (let n = 1; n <= programmedSamples; n++) {
      const t0 = offsetOf(n) + jitter[n];
      if (n === 1) push(t0, '#', 'Primer bloque tras Iniciar: muestra 1, StartedAt');
      emitBlock(t0, seq0 + 2 + n, rowsBySample.get(n), 1);
    }
    push(endedAt + 0.5, 'PC', frame('STOP'));
    push(endedAt + 0.55, 'ADQ', frame('ACK,STOP'));
    return lines;
  }

  push(-6, '#', 'PC abre el puerto 115200 8N1');
  push(-5, 'ADQ', frame(`BOOT,${device.deviceIdentifier},${device.firmwareVersion}`));
  push(-4.9, 'PC', frame('IDN'));
  push(-4.85, 'ADQ', idn(device));
  push(-1, '#', 'El técnico pulsa Iniciar captura');
  push(-1, 'PC', frame(`START,${mask}`));
  push(-0.95, 'ADQ', frame('ACK,START'));

  for (let n = 1; n <= programmedSamples; n++) {
    let t0 = offsetOf(n);
    if (gapSamples.has(n)) {
      if (n === lost.sample) {
        for (let a = 0; a < 3; a++) push(t0 + a * RETRY_DELAY_S, 'PC', frame(`READ,${n}`));
        push(t0 + 3 * RETRY_DELAY_S, '#', '3 intentos sin respuesta: pérdida de comunicación (CommunicationGap abierto)');
      }
      continue;
    }
    if (restored && n === restored.sample) {
      const tr = offsetOf(n) - RECONNECT_LEAD_S;
      push(tr, '#', 'Reconexión: el puerto vuelve a estar disponible');
      push(tr + 0.5, 'ADQ', frame(`BOOT,${restored.deviceIdentifier},${device.firmwareVersion}`));
      push(tr + 3, 'PC', frame('IDN'));
      push(tr + 3.05, 'ADQ', idn({ ...device, deviceIdentifier: restored.deviceIdentifier, sensorGroupId: restored.sensorGroupId }));
      push(tr + 3.1, 'PC', frame(`START,${mask}`));
      push(tr + 3.15, 'ADQ', frame('ACK,START'));
    }
    if (reboots.has(n)) {
      push(t0 - 40, '#', 'El adquisidor se reinicia durante la sesión');
      push(t0 - 40, 'ADQ', frame(`BOOT,${device.deviceIdentifier},${device.firmwareVersion}`));
      push(t0, 'PC', frame(`READ,${n}`));
      push(t0 + 0.05, 'ADQ', frame('NAK,READ,E04'));
      push(t0 + 0.1, 'PC', frame(`START,${mask}`));
      push(t0 + 0.15, 'ADQ', frame('ACK,START'));
      t0 += 0.2;
    }
    const sampleRows = rowsBySample.get(n);
    const attempts = sampleRows.some((r) => PERSISTENT_FRAME_FAULTS.includes(r.frameFault)) ? 3
      : sampleRows.some((r) => r.frameFault === 'BAD_CHECKSUM_RECOVERED') ? 2 : 1;
    for (let a = 1; a <= attempts; a++) {
      const ta = t0 + (a - 1) * RETRY_DELAY_S;
      push(ta, 'PC', frame(`READ,${n}`));
      emitBlock(ta, n, sampleRows, a);
    }
  }

  if (mismatch) {
    const tr = offsetOf(restored.sample) - RECONNECT_LEAD_S;
    push(tr, '#', 'Reconexión: el puerto vuelve a estar disponible');
    push(tr + 0.5, 'ADQ', frame(`BOOT,${restored.deviceIdentifier},${device.firmwareVersion}`));
    push(tr + 3, 'PC', frame('IDN'));
    push(tr + 3.05, 'ADQ', idn({ ...device, deviceIdentifier: restored.deviceIdentifier, sensorGroupId: restored.sensorGroupId }));
    push(tr + 3.1, '#', 'DeviceId o SensorGroupId distinto al de la sesión: sesión no válida (Invalid / DeviceMismatch)');
    push(tr + 3.2, 'PC', frame('STOP'));
    push(tr + 3.25, 'ADQ', frame('ACK,STOP'));
  } else if (ctx.failedAt !== null) {
    push(endedAt + 1, '#', '30 min consecutivos sin datos suficientes: sesión fallida (Invalid / DataLoss)');
    push(endedAt + 1, 'PC', frame('STOP'));
    push(endedAt + 1.05, 'ADQ', frame('ACK,STOP'));
  } else if (!(lost && !restored)) {
    push(endedAt + 0.5, '#', def.close?.type === 'Manual' ? 'Cierre manual del técnico' : 'Cierre automático al cumplir la duración planificada');
    push(endedAt + 0.5, 'PC', frame('STOP'));
    push(endedAt + 0.55, 'ADQ', frame('ACK,STOP'));
  }
  return lines;
}

/* ---------------------------------------------------------------------
   Catálogo de escenarios
   --------------------------------------------------------------------- */

function randomStressPlan(seed) {
  const rnd = mulberry32(seed);
  const plan = { doors: [], oc: [], mismatch: null, burst: null, frameFaults: new Map() };
  for (let i = 0; i < 6; i++) plan.doors.push(randInt(rnd, 5, 170));
  for (let i = 0; i < 4; i++) plan.oc.push({ ch: randInt(rnd, 1, 8), from: randInt(rnd, 5, 170), len: randInt(rnd, 1, 6) });
  plan.mismatch = { ch: randInt(rnd, 1, 8), from: randInt(rnd, 100, 170), len: 3 };
  plan.burst = { from: randInt(rnd, 40, 90), len: randInt(rnd, 2, 4) };
  plan.gap = { from: randInt(rnd, 110, 160), len: randInt(rnd, 2, 6) };
  // La última muestra (181) queda sin fallas de trama para que el cierre no coincida con un reintento
  for (let n = 1; n <= 180; n++) {
    for (let ch = 1; ch <= 8; ch++) {
      const p = rnd();
      const fault = p < 0.004 ? 'BAD_CHECKSUM_RECOVERED' : p < 0.006 ? 'BAD_CHECKSUM' : p < 0.008 ? 'MISSING' : p < 0.009 ? 'TRUNCATED' : null;
      if (fault) plan.frameFaults.set(`${n}:${ch}`, fault);
    }
  }
  return plan;
}

const stressPlan = randomStressPlan(20260925);

const client = (ref) => ({ durationSource: 'ClientRequest', clientRequestReference: ref });
const base = { plannedMinutes: 60, durationSource: 'Base' };

const SCENARIOS = [
  {
    code: 'TD-01', slug: 'optimo-2h', category: 'Funcionamiento óptimo', title: 'Funcionamiento óptimo, 10 canales T, 2 h',
    description: 'Congeladora estable alrededor de -18 °C con variación normal entre zonas. Duración de 2 h a pedido del cliente, con cierre automático. Sin fallas ni alertas.',
    seed: 101, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-01'),
    channels: defaultChannels(10),
  },
  {
    code: 'TD-02', slug: 'variaciones-medida', category: 'Variaciones de medida', title: 'Aperturas de puerta y valores en el límite',
    description: 'S4 (Puerta) registra excursiones: -5,00 (cumple), -4,99 y -4,90 (fuera de límite), +2,10 y -3,50 (fuera de límite). S2 reporta con 1 decimal. S1 tiene un pico de ruido dentro del límite. Son alertas de advertencia: no interrumpen al técnico.',
    seed: 102, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 180, ...client('OS-TD-02'),
    channels: defaultChannels(5).map((c) => (c.channelNumber === 2 ? { ...c, decimals: 1 } : c)),
    mutate(r) {
      const door = { 20: -5.0, 21: -4.99, 22: -4.9, 50: 2.1, 51: -3.5, 52: -6.2 };
      if (r.channel === 4 && door[r.sample] !== undefined) r.value = door[r.sample];
      if (r.channel === 1 && r.sample === 70) r.value = -15.8;
    },
  },
  {
    code: 'TD-03', slug: 'caida-sensores-60', category: 'Caída de sensores', title: 'Caída de sensores en el umbral (60 %, no afecta)',
    description: 'Muestras 20 a 25: 6 de 10 canales con termopar abierto (60 %). No supera el umbral: las muestras siguen siendo válidas.',
    seed: 103, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-03'),
    channels: defaultChannels(10),
    mutate(r) { if (r.sample >= 20 && r.sample <= 25 && r.channel <= 6) r.deviceStatus = 'OC'; },
  },
  {
    code: 'TD-04', slug: 'caida-sensores-70', category: 'Caída de sensores', title: 'Caída de sensores sobre el umbral (70 %) con escalamiento',
    description: 'Muestras 30 a 39: 7 de 10 canales sin dato válido (OC, SC, trama ausente y trama truncada). Advertencia en la muestra 30, alerta crítica en la 32 (no se restableció en la 3.ª medición). Dura 18 min, menos de 30: no falla la sesión. Sigue completa porque conserva ≥ 31 muestras válidas.',
    seed: 104, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-04'),
    channels: defaultChannels(10),
    mutate(r) {
      if (r.sample < 30 || r.sample > 39) return;
      if (r.channel <= 3) r.deviceStatus = 'OC';
      else if (r.channel <= 5) r.deviceStatus = 'SC';
      else if (r.channel === 6) r.frameFault = 'MISSING';
      else if (r.channel === 7) r.frameFault = 'TRUNCATED';
    },
  },
  {
    code: 'TD-05', slug: 'caida-sensores-incompleta', category: 'Caída de sensores', title: 'Sesión base de 1 h que queda incompleta por pérdida de sensores',
    description: 'Sesión base de 1 h. Muestras 5 a 14: 4 de 5 canales abiertos (80 %). Quedan 21 muestras válidas (< 31): al cumplir la hora la sesión se cierra como incompleta.',
    seed: 105, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    ...base,
    channels: defaultChannels(5),
    mutate(r) { if (r.sample >= 5 && r.sample <= 14 && r.channel <= 4) r.deviceStatus = 'OC'; },
  },
  {
    code: 'TD-06', slug: 'perdida-comunicacion', category: 'Comunicación', title: 'Pérdida de comunicación y recuperación con el mismo adquisidor',
    description: 'Sin respuesta desde la muestra 40; reconecta el mismo adquisidor y grupo antes de la muestra 45. Hueco de 5 muestras (8 min), menos de 30 min: la sesión continúa.',
    seed: 106, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-06'),
    channels: defaultChannels(5),
    events: [
      { type: 'COMM_LOST', sample: 40 },
      { type: 'COMM_RESTORED', sample: 45, deviceIdentifier: 'ADQ-SIM-0001', sensorGroupId: 'GRP-A' },
    ],
  },
  {
    code: 'TD-07', slug: 'otro-adquisidor', category: 'Comunicación', title: 'Reconexión con otro adquisidor (sesión fallida)',
    description: 'Pérdida en la muestra 50; al reconectar responde ADQ-SIM-0002. La sesión se declara fallida (no válida).',
    seed: 107, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-07'),
    channels: defaultChannels(5),
    events: [
      { type: 'COMM_LOST', sample: 50 },
      { type: 'COMM_RESTORED', sample: 55, deviceIdentifier: 'ADQ-SIM-0002', sensorGroupId: 'GRP-A' },
    ],
  },
  {
    code: 'TD-08', slug: 'otro-grupo-sensores', category: 'Comunicación', title: 'Reconexión con otro grupo de sensores (sesión fallida)',
    description: 'Pérdida en la muestra 30; reconecta el mismo adquisidor pero con el grupo de sensores GRP-B. La sesión se declara fallida (no válida).',
    seed: 108, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-08'),
    channels: defaultChannels(5),
    events: [
      { type: 'COMM_LOST', sample: 30 },
      { type: 'COMM_RESTORED', sample: 33, deviceIdentifier: 'ADQ-SIM-0001', sensorGroupId: 'GRP-B' },
    ],
  },
  {
    code: 'TD-09', slug: 'tramas-y-tipos', category: 'Tramas y tipos', title: 'Tramas corruptas, tipo no coincidente y fallas del sensor',
    description: 'Checksum recuperado en reintento (m8 S2), checksum persistente (m12 S2), trama truncada (m15 S4), trama ausente (m18 S5), tipo K informado en S3 declarado T (m20-25), valor imposible para tipo T (m40 S1), OR del adquisidor (m45 S4), cortocircuito (m50 S5) y reinicio del adquisidor antes de la muestra 30.',
    seed: 109, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-09'),
    channels: defaultChannels(5),
    events: [{ type: 'DEVICE_REBOOT', sample: 30 }],
    mutate(r) {
      const k = `${r.sample}:${r.channel}`;
      if (k === '8:2') r.frameFault = 'BAD_CHECKSUM_RECOVERED';
      if (k === '12:2') r.frameFault = 'BAD_CHECKSUM';
      if (k === '15:4') r.frameFault = 'TRUNCATED';
      if (k === '18:5') r.frameFault = 'MISSING';
      if (r.channel === 3 && r.sample >= 20 && r.sample <= 25) r.reportedType = 'K';
      if (k === '40:1') r.value = 400.0;
      if (k === '45:4') r.deviceStatus = 'OR';
      if (k === '50:5') r.deviceStatus = 'SC';
    },
  },
  {
    code: 'TD-10', slug: 'mezcla-tk-1h', category: 'Configuración', title: 'Mezcla T/K en una sesión base de 1 h',
    description: 'S1-S4 tipo T y S5 tipo K. Sesión base: cierre automático a los 3600 s con 31 muestras válidas, el caso límite de sesión completa.',
    seed: 110, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    ...base,
    channels: defaultChannels(5, ['T', 'T', 'T', 'T', 'K']),
  },
  {
    code: 'TD-11', slug: 'stream-datos-previos', category: 'Inicio por llegada de datos', title: 'Adquisidor que transmite antes de conectar (modo STREAM)',
    description: 'El adquisidor ya emitía bloques antes de abrir el puerto: se descarta una línea parcial y dos bloques de vista previa. La sesión base inicia con el primer bloque recibido después de pulsar Iniciar.',
    seed: 111, mode: 'Stream', equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    ...base,
    channels: defaultChannels(5),
  },
  {
    code: 'TD-12', slug: 'cliente-24h', category: 'Duración', title: 'Sesión ocasional de 24 h a pedido del cliente',
    description: '10 canales, variación diaria suave. Duración de 24 h solicitada por el cliente. Se cierra automáticamente con la muestra 721.',
    seed: 112, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18, dailyAmp: 0.6,
    plannedMinutes: 1440, ...client('OS-TD-12'),
    channels: defaultChannels(10),
  },
  {
    code: 'TD-13', slug: 'sesion-corta', category: 'Duración', title: 'Sesión base cerrada a los 45 min (incompleta)',
    description: 'Cierre manual antes de cumplir la hora planificada.',
    seed: 113, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    ...base, close: { type: 'Manual', atOffsetSeconds: 2700 },
    channels: defaultChannels(5),
  },
  {
    code: 'TD-14', slug: 'sin-limite', category: 'Configuración', title: 'Refrigeradora sin límite definido',
    description: 'Límite pendiente (NULL). Valores de +2 a +10 °C sin alertas de límite; solo la alerta LimitNotDefined.',
    seed: 114, equipmentType: 'Refrigeradora', limit: null, setpoint: 5, cycleAmp: 1.5,
    plannedMinutes: 120, ...client('OS-TD-14'),
    channels: defaultChannels(5),
    mutate(r) { if (r.channel === 4 && r.sample >= 30 && r.sample <= 32) r.value = 9.5; },
  },
  {
    code: 'TD-15', slug: 'aleatorio-combinado', category: 'Aleatorio combinado', title: 'Estrés aleatorio combinado, 8 canales, 6 h',
    description: 'Combinación pseudoaleatoria (semilla 20260925) de aperturas de puerta, episodios de termopar abierto, tramas corruptas, un episodio de tipo no coincidente, una caída de 6 de 8 sensores (75 %) durante 4 muestras y un hueco de comunicación.',
    seed: 115, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 360, ...client('OS-TD-15'),
    channels: defaultChannels(8),
    events: [
      { type: 'COMM_LOST', sample: stressPlan.gap.from },
      { type: 'COMM_RESTORED', sample: stressPlan.gap.from + stressPlan.gap.len, deviceIdentifier: 'ADQ-SIM-0001', sensorGroupId: 'GRP-A' },
    ],
    mutate(r) {
      for (const d of stressPlan.doors) {
        const bump = { 0: 10, 1: 13.5, 2: 6 }[r.sample - d];
        if (r.channel === 4 && bump !== undefined) r.value += bump;
      }
      for (const e of stressPlan.oc) if (r.channel === e.ch && r.sample >= e.from && r.sample < e.from + e.len) r.deviceStatus = 'OC';
      const m = stressPlan.mismatch;
      if (r.channel === m.ch && r.sample >= m.from && r.sample < m.from + m.len) r.reportedType = 'K';
      const b = stressPlan.burst;
      if (r.channel <= 6 && r.sample >= b.from && r.sample < b.from + b.len) r.deviceStatus = 'OC';
      const f = stressPlan.frameFaults.get(`${r.sample}:${r.channel}`);
      if (f && r.deviceStatus === 'OK') r.frameFault = f;
    },
  },
  {
    code: 'TD-16', slug: 'falla-perdida-sostenida', category: 'Caída de sensores', title: 'Pérdida de sensores sostenida 30 min: sesión fallida',
    description: 'Desde la muestra 20, 4 de 5 canales abiertos sin recuperarse. Advertencia en la 20, crítica en la 22 y, al cumplirse 30 min consecutivos (muestra 35), la sesión se declara fallida y se detiene.',
    seed: 116, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-16'),
    channels: defaultChannels(5),
    mutate(r) { if (r.sample >= 20 && r.channel <= 4) r.deviceStatus = 'OC'; },
  },
  {
    code: 'TD-17', slug: 'cliente-72h', category: 'Duración', title: 'Sesión de varios días a pedido del cliente (72 h)',
    description: 'Caso extremo: 72 h solicitadas por el cliente por exigencia de control. 2161 muestras, cierre automático. Prueba que no hay un tope fijo de 24 h ni de 721 muestras.',
    seed: 117, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18, dailyAmp: 0.6,
    plannedMinutes: 4320, ...client('OS-2026-0142'),
    channels: defaultChannels(5),
  },
  {
    code: 'TD-18', slug: 'escalamiento-umbrales', category: 'Caída de sensores', title: 'Límites del escalamiento: 2, 3 y 15 muestras consecutivas',
    description: 'Tres episodios con 4 de 5 canales abiertos. Muestras 10-11 (2): solo advertencia. Muestras 30-32 (3): crítica en la 32. Muestras 50-64 (15, 28 min): crítica en la 52, sin llegar a los 30 min de la falla de sesión.',
    seed: 118, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 180, ...client('OS-TD-18'),
    channels: defaultChannels(5),
    mutate(r) {
      const inEpisode = (r.sample >= 10 && r.sample <= 11) || (r.sample >= 30 && r.sample <= 32) || (r.sample >= 50 && r.sample <= 64);
      if (inEpisode && r.channel <= 4) r.deviceStatus = 'OC';
    },
  },
  {
    code: 'TD-19', slug: 'duracion-por-tipo', category: 'Duración', title: 'Duración mínima por tipo de equipo (incubadora, 2 h)',
    description: 'El tipo Incubadora exige 2 h (configuración de prueba). El técnico cierra a los 90 min: aunque tiene más de 31 muestras válidas, no cumplió la duración planificada y queda incompleta.',
    seed: 119, equipmentType: 'Incubadora', limit: null, setpoint: 37, cycleAmp: 0.3,
    equipmentTypeMinSessionDurationMinutes: 120, plannedMinutes: 120, durationSource: 'EquipmentType',
    close: { type: 'Manual', atOffsetSeconds: 5400 },
    channels: defaultChannels(5),
  },
  {
    code: 'TD-20', slug: 'fuera-limite-sensor', category: 'Variaciones de medida', title: 'Un sensor fuera de límite 30 min: posible falla del sensor',
    description: 'Desde la muestra 20, solo S3 (Inferior) marca alrededor de -2 °C mientras los demás siguen cerca de -18 °C. Cada lectura es una advertencia; a los 30 min (muestra 35) se genera la crítica AboveLimitSustained con causa probable "Sensor". En la muestra 46 S3 vuelve a la normalidad (termopar recolocado).',
    seed: 120, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-20'),
    channels: defaultChannels(5),
    mutate(r) { if (r.channel === 3 && r.sample >= 20 && r.sample <= 45) r.value = -2.0 + (r.value + 18.8) * 0.5; },
  },
  {
    code: 'TD-21', slug: 'fuera-limite-equipo', category: 'Variaciones de medida', title: 'Todos los sensores fuera de límite 30 min: posible falla del equipo',
    description: 'Desde la muestra 30 el compresor deja de enfriar: todos los canales suben 3 °C por muestra hasta +15 °C sobre lo normal y superan el límite. A los 30 min de cada canal fuera de límite se genera una crítica AboveLimitSustained con causa probable "Equipment". La sesión no falla: los datos son válidos.',
    seed: 121, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    plannedMinutes: 120, ...client('OS-TD-21'),
    channels: defaultChannels(5),
    mutate(r) { if (r.sample >= 30) r.value += Math.min(15, (r.sample - 29) * 3); },
  },
  {
    code: 'TD-22', slug: 'nueve-puntos', category: 'Configuración', title: 'Sesión base con exactamente 9 puntos (mínimo normativo)',
    description: '9 canales: 8 esquinas y el centro, según IEC 60068-3-5 y DKD-R 5-7. Caso límite: no genera la advertencia BelowMinimumPoints. Con 8 canales (TD-15) sí se genera.',
    seed: 122, equipmentType: 'Congeladora', limit: -5.0, setpoint: -18,
    ...base,
    channels: ['Esquina sup. izq. frontal', 'Esquina sup. der. frontal', 'Esquina sup. izq. fondo', 'Esquina sup. der. fondo',
      'Esquina inf. izq. frontal', 'Esquina inf. der. frontal', 'Esquina inf. izq. fondo', 'Esquina inf. der. fondo', 'Centro']
      .map((position, i) => ({ channelNumber: i + 1, thermocoupleType: 'T', position, offsetC: i < 4 ? 1.0 : i < 8 ? -0.8 : 0 })),
  },
];

/* ---------------------------------------------------------------------
   Escritura
   --------------------------------------------------------------------- */

function readingsCsv(rows) {
  const header = 'SampleNumber,OffsetSeconds,Channel,DeclaredType,ReportedType,TransmittedValue,DeviceStatus,FrameFault,ExpectedSensorStatus,ExpectedTemperatureC,ExpectedIsAboveLimit';
  const lines = rows.map((r) => [
    r.sample, r.offsetSeconds, r.channel, r.declaredType, r.reportedType,
    r.deviceStatus === 'OK' ? centsToText(r.cents, r.decimals) : '',
    r.deviceStatus, r.frameFault, r.sensorStatus,
    r.temperatureCents === null ? '' : centsToText(r.temperatureCents, 2),
    r.isAboveLimit ? 1 : 0,
  ].join(','));
  return [header, ...lines].join('\n') + '\n';
}

function transcriptLog(scenario, lines) {
  const head = [
    `# Transcripción serial de referencia · ${scenario.code} · ${scenario.title}`,
    '# Formato: <offset_s>\\t<origen>\\t<línea>   origen = PC | ADQ | # (comentario)',
    '# offset_s: segundos desde StartedAt (muestra 1). Negativo = antes del inicio.',
    '# Se omiten PING / ACK,PING entre muestras.',
  ];
  const sorted = [...lines].sort((a, b) => a.t - b.t);
  return [...head, ...sorted.map((l) => `${l.t.toFixed(3)}\t${l.dir}\t${l.text}`)].join('\n') + '\n';
}

rmSync(OUT, { recursive: true, force: true });
mkdirSync(OUT, { recursive: true });

const index = [];
for (const def of SCENARIOS) {
  const { scenario, rows, transcript } = build(def);
  const dir = join(OUT, `${def.code}-${def.slug}`);
  mkdirSync(dir, { recursive: true });
  writeFileSync(join(dir, 'scenario.json'), JSON.stringify(scenario, null, 2) + '\n');
  writeFileSync(join(dir, 'readings.csv'), readingsCsv(rows));
  writeFileSync(join(dir, 'transcript.log'), transcriptLog(scenario, transcript));
  const e = scenario.expected;
  index.push({
    code: def.code, folder: dir.slice(OUT.length + 1), category: def.category, title: def.title,
    channels: def.channels.length, mode: scenario.config.acquisitionMode,
    status: e.status, closeReason: e.closeReason, durationSeconds: e.endedAtOffsetSeconds,
    programmedSamples: e.programmedSamples, validSamples: e.validSamples, affectedSamples: e.affectedSamples.length,
    alertsByType: e.alertsByType,
  });
}
writeFileSync(join(OUT, 'index.json'), JSON.stringify(index, null, 2) + '\n');

// Resumen en Markdown para 06-test-data.md
const hhmm = (s) => `${String(Math.floor(s / 3600)).padStart(2, '0')}:${String(Math.floor((s % 3600) / 60)).padStart(2, '0')}`;
console.log('| Código | Categoría | Canales | Modo | Duración | Muestras (prog./válidas/afect.) | Estado esperado | Motivo | Alertas esperadas |');
console.log('|---|---|---|---|---|---|---|---|---|');
for (const s of index) {
  const alerts = Object.entries(s.alertsByType).map(([k, v]) => `${k} ×${v}`).join(', ') || '—';
  console.log(`| ${s.code} | ${s.category} | ${s.channels} | ${s.mode} | ${hhmm(s.durationSeconds)} | ${s.programmedSamples} / ${s.validSamples} / ${s.affectedSamples} | ${s.status} | ${s.closeReason} | ${alerts} |`);
}
