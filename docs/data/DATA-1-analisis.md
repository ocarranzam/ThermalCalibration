# DATA-1 · Cámara ambiental Memmert, 72 h, 12 termopares tipo T

Análisis del primer registro real de temperatura obtenido en campo. Sirve para caracterizar el equipo, para contrastar las reglas del sistema con datos reales y como primer perfil para los futuros **perfiles de eficacia por modelo**.

| Campo | Valor |
|---|---|
| Archivo | `DATA-1-.xlsx` (hoja `TEMPERATURA`). Datos del cliente: **no se versiona** en git (`.gitignore`); se conserva localmente |
| Perfil calculado | [DATA-1-perfil.json](DATA-1-perfil.json), generado por [analyze_thermal_data.py](analyze_thermal_data.py) |
| Fecha del análisis | 2026-09-25 |
| Resultado | **Equipo en condición óptima**: homogeneidad y estabilidad dentro de lo que declara el fabricante para la familia identificada |

## 1. Datos del registro

| Dato | Valor | Origen |
|---|---|---|
| Tipo de equipo | **Cámara ambiental** (cámara de ensayos ambientales) | Entrevistado |
| Marca | **Memmert** (Memmert GmbH + Co. KG, Alemania) | Entrevistado |
| Modelo | **TTC256** (inferido; alternativa: CTC256). Ver §4 | Inferido del tipo, de la marca y de los datos; **por confirmar en la placa del equipo** |
| Volumen útil | 256 L (el único volumen de la familia CTC/TTC) | Ficha del fabricante |
| Sensores | 12 termopares **tipo T** | Entrevistado y encabezado del archivo |
| Intervalo | **5 min** (300 s), regular en todo el registro | Datos: 0 marcas de tiempo incoherentes |
| Periodo | 01/09/2024 12:30:05 → 04/09/2024 12:30:05: **72 h**, 865 muestras | Datos |
| Resolución | 0,1 °C (valores con 0 o 1 decimal) | Datos |
| Lecturas | 10 380, de las cuales faltan 13 (**disponibilidad 99,87 %**) | Datos |

## 2. Resultados

### 2.1 Comportamiento general

- Temperatura media de **19,43 °C** (todos los valores entre 18,3 y 22,4 °C). La consigna no está en el archivo: si era 20 °C, la cámara trabajó unos 0,5 K por debajo (ver §6).
- **Estabilización:** en las 2 primeras horas la media baja de 19,9 a 19,4 °C. El análisis de estado estacionario empieza en la muestra 25 (2 h).
- **Deriva lenta:** entre −0,11 y −0,27 °C/día en 10 sensores, y positiva en S6 (+0,07) y S10 (+0,15). La media diaria pasa de 19,58 (día 1) a 19,36 °C (día 3). Es compatible con cambios del ambiente del laboratorio.
- **Evento al final del ensayo** (04/09, desde las 10:55): pico aislado en S9 (21,4 °C, recuperado en 15 min) y, desde las 11:35, calentamientos sucesivos de 1 a 3 K (S1, S11, luego S2, S3, S12… S6–S8, S10) que se recuperan en unos 30 min; S9 deja de registrar (12 muestras) y S7 falta en la última. El patrón es el de una **apertura de puerta o el retiro de los termopares** al terminar. Se excluye del análisis estacionario (muestras 846 a 865).

### 2.2 Estado estacionario (muestras 25–845, 68,3 h, sin datos faltantes)

Métricas con los criterios de DIN 12880 (fluctuación en el tiempo por punto y homogeneidad entre puntos):

| Sensor | Media (°C) | Desv. estándar (K) | Mín–máx (°C) | Fluctuación (K) |
|---|---|---|---|---|
| S1 | 19,69 | 0,127 | 19,3–20,0 | ±0,35 |
| S2 | 19,05 | 0,206 | 18,7–19,8 | ±0,55 |
| S3 | 19,26 | 0,145 | 18,8–19,7 | ±0,45 |
| S4 | 19,56 | 0,171 | 19,2–20,1 | ±0,45 |
| S5 | 19,44 | 0,148 | 19,1–19,9 | ±0,40 |
| S6 | 19,54 | 0,174 | 19,1–19,9 | ±0,40 |
| S7 | 19,83 | 0,188 | 19,5–20,4 | ±0,45 |
| S8 | 19,43 | 0,193 | 19,1–20,0 | ±0,45 |
| S9 | 19,75 | 0,202 | 19,2–20,3 | ±0,55 |
| S10 | 18,84 | 0,217 | 18,3–19,3 | ±0,50 |
| S11 | 19,48 | 0,116 | 19,0–19,8 | ±0,40 |
| S12 | 19,31 | 0,242 | 19,0–20,1 | ±0,55 |

| Indicador | Valor |
|---|---|
| **Variación estándar de la temperatura** (desviación estándar media por punto) | **0,18 K** |
| **Fluctuación en el tiempo** (±(máx − mín)/2 por punto) | mediana **±0,45 K**, máxima ±0,55 K; 9 de 12 puntos en ±0,5 K o menos |
| **Homogeneidad espacial** (±(media máx − media mín)/2) | **±0,5 K** (S10, el más frío, 18,84 °C; S7, el más cálido, 19,83 °C) |
| Desviación máxima de un punto respecto a la media | 0,6 K (S10) |
| Gradiente instantáneo entre puntos | medio 1,07 K, máximo 2,0 K |
| Inestabilidad en 30 min (máx − mín en 7 muestras, idea de DKD-R 5-7 §7.3) | media 0,22 K, máxima 0,7 K |

> **Incertidumbre de medición:** un termopar tipo T de clase 1 (IEC 60584-1) tiene una tolerancia de ±0,5 °C. No hay certificados de calibración de los sensores, así que parte de la diferencia de 1 K entre S7 y S10 puede deberse a la desviación propia de cada termopar y no a la cámara.

## 3. ¿Está en condición óptima?

**Sí.** En estado estacionario la cámara cumple lo que Memmert declara para su familia CTC/TTC:

| Indicador | Fabricante (CTC/TTC, DIN 12880) | Medido | Evaluación |
|---|---|---|---|
| Variación de la temperatura en el tiempo | ±0,2 … 0,5 K (según la consigna) | ±0,45 K (mediana); desv. estándar 0,18 K | ✅ Dentro del rango (3 puntos en ±0,55 K, en el límite de la resolución de 0,1 °C) |
| Homogeneidad en la cámara | ±0,5 … 2 K (según la consigna) | ±0,5 K | ✅ En el **mejor** extremo del rango |

Además, no hubo pérdida de sensores durante el ensayo (0 muestras afectadas según RN-14) y la disponibilidad de datos fue del 99,87 %.

## 4. Identificación del modelo

El entrevistado recuerda la marca, pero no el modelo. Como el modelo es la clave de los perfiles de eficacia, se infiere con la información pública del fabricante:

| Familia Memmert | Nombre del fabricante | Técnica | Rango | Coincidencia con DATA-1 |
|---|---|---|---|---|
| **TTC** (256 L) | *Temperature test chamber*, dentro de las ***Environmental test chambers*** | Compresor doble | −42 a +190 °C, sin humedad | **Alta:** "cámara ambiental" es el nombre del fabricante para CTC/TTC; se midió solo temperatura; homogeneidad y fluctuación dentro de su ficha |
| **CTC** (256 L) | *Climatic test chamber* (*Environmental test chambers*) | Compresor doble | −42 a +190 °C; con humedad de +10 a +95 °C | Alta: misma familia y métricas; solo se distingue de la TTC por el control de humedad |
| HPP (108, 256, 749 L) | *Constant climate chamber* | Peltier | 0 a +70 °C | Media: apta para ensayos cercanos al ambiente, pero el fabricante la llama cámara de clima constante o de estabilidad |
| ICH (108, 256, 749 L) | *Climate chamber* (estabilidad ICH Q1A) | Compresor | −10 a +60 °C | Baja: orientada a estabilidad farmacéutica con humedad |

**Conclusión:** modelo **TTC256** (probable) o **CTC256** si la cámara tiene control de humedad. Se registra como `TTC256` con estado **inferido** hasta verificar la placa o la factura del equipo.

## 5. Contraste con las reglas del sistema

| Tema | Especificación | DATA-1 | Consecuencia |
|---|---|---|---|
| Número de canales | RN-01: de 1 a 27 (**D-06**, antes de 1 a 10) | **12** | ✅ Admitido tras D-06 (el sistema anterior, de 10 canales, no podía capturarlo) |
| Puntos mínimos | RN-20: por tipo de equipo (**D-06**); "Cámara ambiental": 9 (IEC 60068-3-5, hasta 2000 L) | 12 | ✅ Cumple (sin posiciones registradas) |
| Intervalo de muestreo | RN-02: 120 s **[PC-01]** | 300 s | ✅ Admisible como parámetro (`SamplingIntervalSeconds`), dentro del rango de la OMS (1–15 min); no cumple los 60 s de IEC 60068-3-5 y DKD-R 5-7 (P-11) |
| Duración | RN-04: base 1 h, por pedido del cliente hasta 7 días | 72 h | ✅ Equivalente al escenario TD-17 (72 h) |
| Pérdida de sensores | RN-14: afectada si falta más del 60 % | Máx. 2 de 12 faltantes | ✅ 0 muestras afectadas. S9 sin datos 55 min al final: solo `SensorFault` (con 12 canales, afecta desde 8 sin dato) |
| Límite | RN-06/07: criterio por tipo (**D-05**): "Cámara ambiental" usa **banda** alrededor de la consigna | Cámara a ~19,4 °C, consigna desconocida | ✅ Modelado con banda; la tolerancia queda pendiente. Referencia: la ficha del fabricante admite ±0,5 … 2 K de homogeneidad |
| Tipo de equipo | Catálogo inicial | Cámara ambiental | Añadido: criterio `Band`, tolerancia pendiente, 9 puntos mínimos |
| Marca y modelo | Opcionales en `Equipment` | Modelo desconocido en campo | Pasan a ser **obligatorios**: son la clave de los perfiles de eficacia |

## 6. Datos que conviene pedir al entrevistado

> **Decisión (2026-09-25):** se avanza sin estos datos, porque es difícil contactar al entrevistado. El modelo queda como inferido (`IsModelConfirmed` = 0) y la tolerancia de la cámara, pendiente.

1. **Placa del equipo** (modelo y número de serie) para confirmar TTC256 o CTC256.
2. **Consigna** de temperatura del ensayo (¿20 °C?) y si tenía control de humedad.
3. **Ubicación** de los 12 termopares (esquinas, centro, puerta…), para evaluar la homogeneidad por posición.
4. Certificados de **calibración** de los termopares y del registrador.
5. Qué ocurrió el 04/09 desde las 10:55 (apertura de puerta, retiro de sensores).

## 7. Base para el perfil de eficacia

El [perfil JSON](DATA-1-perfil.json) guarda, por registro, el equipo (tipo, marca, **modelo** y si está confirmado), las condiciones (intervalo, duración, canales, termopar) y las métricas en estado estacionario (homogeneidad, fluctuación, variación estándar, deriva, disponibilidad y pérdida de sensores). Con varios registros del mismo modelo se podrá calcular un perfil de eficacia: comparación con la ficha del fabricante, repetibilidad entre ensayos y tendencia en el tiempo. Queda planificado en [implementation-plan.md](../implementation-plan.md).

## Fuentes

Copias locales en [docs/equipment-catalog/memmert/](../equipment-catalog/README.md) (descarga pública del 2026-09-25):


- Memmert, catálogo *Climate chambers* (D13643): valores de CTC/TTC, "Temperature variation in time ±0.2…0.5 K" y "Temperature uniformity in chamber ±0.5…2 K" ([PDF](https://assets.fishersci.com/TFS-Assets/CCG/EU/Memmert/Product-Information/BR-Climate-Chambers-english-D13643_01.pdf)).
- Memmert, fichas técnicas [CTC256](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Environmental-test-chambers-CTC256.pdf), [HPP260](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Constant-climate-chamber-HPP260.pdf) e [ICH260L](https://www.memmertusa.com/Content/files/Manuals/DataSheets/Memmert-Climate-chamber-ICH260L.pdf).
- Memmert, [Environmental test chamber CTC256](https://www.memmert.com/en/products/climate-chambers/environmental-test-chambers/ctc256) y [manual de CTC/TTC](https://www.memmertusa.com/Content/files/Manuals/BA-CTC-TTC-EN-D10804.pdf).
- Memmert, [Climate chambers](https://www.memmert.com/en/products/climate-chambers) (familias HPP, ICH, CTC y TTC).
