"""Análisis de un registro real de temperatura (docs/data/DATA-n-.xlsx) y perfil del registro en JSON.

Formato de entrada (el de DATA-1): hoja con la fecha en la columna A, la hora acumulada en B,
el número de muestra en C y un sensor por columna desde D (encabezado en la fila 3, datos desde la 4).

Uso:
    python docs/data/analyze_thermal_data.py docs/data/DATA-1-.xlsx docs/data/DATA-1-perfil.json \
        --interval-min 5 --stabilization-h 2 --exclude-from-sample 846 \
        --equipment-type "Cámara ambiental" --brand Memmert --model TTC256 --model-status inferred --thermocouple T

El perfil sirve de base para los perfiles de eficacia por marca y modelo: métricas en estado
estacionario comparables con las del fabricante (DIN 12880) y con los criterios del sistema (RN-14).
Requiere openpyxl.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import statistics as st
from pathlib import Path

import openpyxl

LOSS_THRESHOLD_PCT = 60  # RN-14: muestra afectada si más del 60 % de los canales no tiene dato válido


def load(path: Path) -> tuple[list[dt.datetime], list[list[float | None]], str]:
    sheet = openpyxl.load_workbook(path, data_only=True).worksheets[0]
    header = [c.value for c in sheet[3]]
    channels = [i for i, v in enumerate(header) if isinstance(v, int) and i >= 3]
    rows = [r for r in sheet.iter_rows(min_row=4, values_only=True) if r[2] is not None]
    first_date, first_time = rows[0][0], rows[0][1]
    start = dt.datetime.combine(first_date.date(), first_time if isinstance(first_time, dt.time) else first_time.time())
    data = [[r[i] for r in rows] for i in channels]
    return [start], data, sheet.title


def profile(data: list[list[float | None]], start: dt.datetime, interval_min: int,
            stabilization_h: float, exclude_from: int | None) -> dict:
    n = len(data[0])
    channels = len(data)
    first = int(stabilization_h * 60 / interval_min)
    last = (exclude_from - 1) if exclude_from else n
    window = [ch[first:last] for ch in data]

    def stats(values: list[float]) -> dict:
        return {
            "mean": round(st.mean(values), 3),
            "sd": round(st.pstdev(values), 3),
            "min": min(values),
            "max": max(values),
            "fluctuationK": round((max(values) - min(values)) / 2, 2),
        }

    per_sensor = []
    for index, (full, steady) in enumerate(zip(data, window), start=1):
        valid = [v for v in full if v is not None]
        steady_valid = [v for v in steady if v is not None]
        missing = [i + 1 for i, v in enumerate(full) if v is None]
        per_sensor.append({
            "sensor": index,
            "validReadings": len(valid),
            "missingSamples": missing,
            "full": stats(valid),
            "steady": stats(steady_valid),
        })

    means = [s["steady"]["mean"] for s in per_sensor]
    grand = st.mean(means)
    instant = []
    affected = 0
    for i in range(n):
        values = [ch[i] for ch in data if ch[i] is not None]
        instant.append(max(values) - min(values))
        if (channels - len(values)) * 100 > LOSS_THRESHOLD_PCT * channels:
            affected += 1
    windows_30 = [
        max(ch[i:i + 7]) - min(ch[i:i + 7])
        for ch in window for i in range(len(ch) - 6)
        if None not in ch[i:i + 7]
    ]
    steady_instant = instant[first:last]
    total = n * channels
    missing_total = sum(len(s["missingSamples"]) for s in per_sensor)

    return {
        "start": start.isoformat(),
        "end": (start + dt.timedelta(minutes=interval_min * (n - 1))).isoformat(),
        "samples": n,
        "intervalSeconds": interval_min * 60,
        "durationHours": round((n - 1) * interval_min / 60, 2),
        "channels": channels,
        "readings": {"total": total, "missing": missing_total, "availabilityPct": round(100 * (total - missing_total) / total, 2)},
        "steadyWindow": {
            "fromSample": first + 1,
            "toSample": last,
            "hours": round((last - first - 1) * interval_min / 60, 1),
            "meanC": round(grand, 2),
            "uniformityK": round((max(means) - min(means)) / 2, 2),
            "maxDeviationFromMeanK": round(max(abs(m - grand) for m in means), 2),
            "fluctuationK": {
                "median": st.median(s["steady"]["fluctuationK"] for s in per_sensor),
                "max": max(s["steady"]["fluctuationK"] for s in per_sensor),
            },
            "meanSdK": round(st.mean(s["steady"]["sd"] for s in per_sensor), 3),
            "instantGradientK": {"mean": round(st.mean(steady_instant), 2), "max": round(max(steady_instant), 1)},
            "instability30MinK": {"mean": round(st.mean(windows_30), 2), "max": round(max(windows_30), 1)},
        },
        "sensorLoss": {"thresholdPct": LOSS_THRESHOLD_PCT, "affectedSamples": affected},
        "sensors": per_sensor,
    }


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("xlsx", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--interval-min", type=int, required=True)
    parser.add_argument("--stabilization-h", type=float, default=2)
    parser.add_argument("--exclude-from-sample", type=int, default=None)
    parser.add_argument("--equipment-type", required=True, help="p. ej. 'Cámara ambiental'")
    parser.add_argument("--brand", required=True)
    parser.add_argument("--model", required=True, help="obligatorio: clave de los perfiles de eficacia por modelo")
    parser.add_argument("--model-status", choices=["confirmed", "inferred"], required=True,
                        help="'inferred' si el modelo se dedujo de los datos y no de la placa del equipo")
    parser.add_argument("--thermocouple", choices=["T", "K"], required=True)
    args = parser.parse_args()

    starts, data, sheet = load(args.xlsx)
    equipment = {"type": args.equipment_type, "brand": args.brand, "model": args.model,
                 "modelStatus": args.model_status, "thermocoupleType": args.thermocouple}
    result = {"source": args.xlsx.name, "sheet": sheet, "equipment": equipment,
              **profile(data, starts[0], args.interval_min, args.stabilization_h, args.exclude_from_sample)}
    args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    w = result["steadyWindow"]
    print(f"{result['samples']} muestras × {result['channels']} canales, {result['durationHours']} h; "
          f"estacionario {w['hours']} h: media {w['meanC']} °C, homogeneidad ±{w['uniformityK']} K, "
          f"fluctuación mediana ±{w['fluctuationK']['median']} K; afectadas RN-14: {result['sensorLoss']['affectedSamples']}")


if __name__ == "__main__":
    main()
