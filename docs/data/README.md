# Registros reales de temperatura

Datos medidos en campo, con su análisis y su perfil. Son la base de los futuros **perfiles de eficacia por marca y modelo**, y sirven para contrastar las reglas del sistema con ensayos reales.

| Registro | Equipo | Marca / modelo | Sensores | Intervalo | Duración | Resultado | Análisis |
|---|---|---|---|---|---|---|---|
| DATA-1 | Cámara ambiental | Memmert / **TTC256** (inferido) | 12 × tipo T | 5 min | 72 h | Óptimo: homogeneidad ±0,5 K, fluctuación ±0,45 K | [DATA-1-analisis.md](DATA-1-analisis.md) |

## Añadir un registro

1. Guardar el archivo como `DATA-n-.xlsx` en esta carpeta (los `.xlsx` son datos de clientes y **no se versionan**: están en `.gitignore`; solo se versionan el análisis y el perfil), con el formato de DATA-1: fecha en A, hora acumulada en B, número de muestra en C y un sensor por columna desde D.
2. Anotar los datos del entrevistado. La **marca y el modelo son obligatorios**: si no se conoce el modelo, inferirlo con la ficha del fabricante y marcarlo como `inferred` hasta confirmarlo en la placa.
3. Generar el perfil:

   ```bash
   python docs/data/analyze_thermal_data.py docs/data/DATA-n-.xlsx docs/data/DATA-n-perfil.json \
     --interval-min <min> --stabilization-h 2 [--exclude-from-sample <n>] \
     --equipment-type "<tipo>" --brand <marca> --model <modelo> --model-status confirmed|inferred --thermocouple T|K
   ```

4. Redactar `DATA-n-analisis.md` con la forma de DATA-1 y añadir una fila a esta tabla.
