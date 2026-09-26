# Normas técnicas de referencia

Normas y guías internacionales que sustentan las reglas del Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración.

| Campo | Valor |
|---|---|
| Versión | 0.2 (DIN 12880 y USP <1079.4> por D-06) |
| Fecha de revisión | 2026-09-25 |
| Usado por | [01-vision-document.md §9](../specs/functional/01-vision-document.md#9-base-normativa-del-umbral-de-pérdida-de-sensores) y las preguntas abiertas P-11 a P-13 |

> Las fichas de esta carpeta son **resúmenes propios** con referencias a los apartados de cada documento. No sustituyen al texto oficial: ante cualquier duda, prevalece el documento original.

## 1. Índice

| Código | Título | Emisor | Edición revisada | Tipo | Ficha | Copia local |
|---|---|---|---|---|---|---|
| WHO TRS 961, Anexo 9, Supl. 8 | *Temperature mapping of storage areas* | Organización Mundial de la Salud | 2015 | Guía técnica | [WHO-TRS-961-Supl8.md](WHO-TRS-961-Supl8.md) | No (ver §2) |
| IEC 60068-3-5 | *Environmental testing – Part 3-5: Confirmation of the performance of temperature chambers* | IEC | Ed. 2.0, 2018 | Norma internacional | [IEC-60068-3-5.md](IEC-60068-3-5.md) | No (ver §2) |
| EURAMET cg-20 | *Calibration of Climatic Chambers* | EURAMET | v3.0, 03/2011 | Guía de calibración | [EURAMET-cg-20.md](EURAMET-cg-20.md) | [PDF](pdf/EURAMET_cg-20_v3.0_2011.pdf) |
| DKD-R 5-7 | *Kalibrierung von Klimaschränken* (Calibración de cámaras climáticas) | PTB / DKD | 09/2018, rev. 0 | Guía de calibración | [DKD-R-5-7.md](DKD-R-5-7.md) | [PDF](pdf/DKD-R_5-7_2018-09.pdf) |
| DIN 12880:2007-05 | *Elektrische Laborgeräte – Wärmeschränke und Brutschränke* (estufas e incubadoras) | DIN | 2007-05 | Norma nacional (Alemania) | [DIN-12880.md](DIN-12880.md) | No (de pago) |
| USP <1079.4> | *Temperature Mapping for the Qualification of Storage Areas* | USP | Kit APEC | Capítulo general (farmacopea) | [USP-1079-4.md](USP-1079-4.md) | No (© USP; enlace oficial) |

## 2. Licencias y copias locales

| Documento | Licencia o condición de uso | Decisión |
|---|---|---|
| EURAMET cg-20 | © EURAMET e.V. Se puede reproducir **solo completo** y no para la venta. Los extractos requieren permiso de la Secretaría de EURAMET. | Se guarda la copia completa en `pdf/`. Las fichas resumen el contenido con palabras propias. |
| DKD-R 5-7 (09/2018) | Creative Commons **CC BY-NC-ND 3.0 DE**. Permite redistribuir sin modificar, sin fines comerciales y citando la fuente, y autoriza expresamente el uso de su contenido en laboratorios con fines comerciales. | Se guarda la copia en `pdf/`. |
| DIN 12880:2007-05 | © DIN. Norma de pago. | **No** se guarda copia. La ficha se basa en el resumen público del fabricante (Memmert). |
| USP <1079.4> | © USP. Publicado por la USP en su kit APEC. | **No** se guarda copia. Enlace a la publicación oficial. |
| WHO TRS 961 Supl. 8 | © OMS 2015. Descarga gratuita desde el sitio de la OMS, pero la reproducción o distribución requiere permiso de WHO Press. | **No** se guarda copia. Enlace al sitio oficial. |
| IEC 60068-3-5:2018 | © IEC. Norma de pago, con todos los derechos reservados. | **No** se guarda copia. El laboratorio debe adquirir la norma a través de la IEC o de su organismo nacional de normalización. La ficha se basa en la vista previa pública de la norma. |

Huella SHA-256 de las copias locales (primeros 16 caracteres), para verificar que no se alteraron:

| Archivo | SHA-256 |
|---|---|
| `pdf/EURAMET_cg-20_v3.0_2011.pdf` | `E7A1CD2FB5FCC08C…` |
| `pdf/DKD-R_5-7_2018-09.pdf` | `B328C4935EC17E7B…` |

## 3. Matriz de trazabilidad: norma → especificación

| Tema | Qué dice la referencia | Especificación actual | Estado |
|---|---|---|---|
| Cantidad y ubicación de sensores | IEC 60068-3-5 §4.4 y DKD-R 5-7 §5 y §7.1.1: mínimo **9** puntos (8 esquinas y el centro) en volúmenes < 2000 L, y 15 puntos o una rejilla de 1 m en volúmenes mayores. DIN 12880: **9** puntos en estufas e incubadoras pequeñas y **27** con cámara de más de 50 L. USP <1079.4>: sin número fijo en refrigeradoras y congeladoras (según tamaño y riesgo; habitualmente 9–21). | Hasta **27 canales** por sesión y **puntos mínimos por tipo de equipo** (D-06): 9 por defecto y 27 en incubadoras. Con menos, advertencia, confirmación y marca en el Excel (RN-20). Alcance acotado a equipos de hasta 2000 L. | **Alineado** dentro del alcance. Evaluar qué posiciones faltan por fallas queda para una fase futura. |
| Criterio de límite por tipo | OMS PQS E003: refrigeradoras de vacunas **+2 … +8 °C** (con calificación de riesgo de congelación) y congeladoras −25 … −15 °C. AABB 5.1.8.1: sangre **+1 … +6 °C**, plasma −18 °C o menos, plaquetas +20 … +24 °C. Incubadoras: consigna ± tolerancia (habitual 37 ± 1 °C). | Criterio por tipo (D-05, D-07): `Range` (mínimo y/o máximo) o `Band` (consigna ± tolerancia), con límites **sugeridos** en el catálogo inicial. | **Alineado.** Los valores sugeridos los confirma el laboratorio (P-03). |
| Intervalo de registro | OMS: de 1 a 15 min. IEC 60068-3-5 §4.4: al menos 1 por minuto para confirmar el desempeño. DKD-R 5-7 §7.3: al menos **30 valores en 30 min** para la inestabilidad temporal. | 120 s (RN-02), configurable en `SamplingIntervalSeconds`. | **Cumple la OMS, pero no IEC ni DKD.** Pregunta abierta P-11. |
| Duración del ensayo | OMS: de 24 a 72 h para cámaras frías y congeladoras. IEC 60068-3-5 §4.5 y DKD-R 5-7 §7.3: al menos 30 min después de la estabilización. | Sesión base de 1 h. Mayor duración por tipo de equipo o a pedido del cliente, hasta varios días (RN-04). | **Alineado.** La duración se acuerda por sesión y queda registrada, como pide EURAMET cg-20 §3. Mínimos por tipo pendientes (P-12). |
| Un solo tipo de dispositivo | OMS, paso 1: usar un solo tipo de dispositivo por estudio de mapeo. | La mezcla T/K se permite con advertencia y confirmación (RN-05). | **Alineado.** La advertencia sigue la recomendación. |
| Sensores y tolerancias | IEC 60068-3-5 §4.2: termopares o PT100 calibrados, con respuesta térmica de 10 a 40 s al 50 %, preferiblemente termopares de clase 1 según IEC 60584-1. | Termopares T o K; el inventario y la calibración de sensores quedan para una fase futura. | **Pendiente** en la fase de inventario de sensores. |
| Registrador independiente | IEC 60068-3-5 §4.4: el registrador debe ser independiente del sistema de control del equipo. | El adquisidor propio es independiente del equipo bajo prueba. | **Alineado.** |
| Falla de sensores | Ninguna referencia fija un porcentaje admisible. La OMS pide un informe de desviación con posible remapeo parcial o total. DKD-R 5-7 §7.1.1: el resultado solo vale para el volumen que abarcan los puntos medidos y no se permite extrapolar. | Umbral interno del 60 % por muestra (RN-14) y alerta por cada falla individual. | **Criterio interno.** Ver la nota sobre las esquinas en §4. |
| Fuera de límite sostenido | OMS TRS 961 Supl. 8, paso 4: fuera de rango como máximo 30 min tras abrir la puerta. | Crítica si un canal sigue fuera de límite 30 min seguidos, con la causa probable (sensor o equipo) (01 RN-19, §6.4). | **Alineado** con la ventana de la OMS. |
| Escalamiento y falla por pérdida de datos | Ninguna referencia fija un número de mediciones. Ventanas de 30 min: OMS (excursión máxima tras abrir la puerta), IEC 60068-3-5 §4.5 (estabilización) y DKD-R 5-7 §7.3 (30 valores en 30 min). | Crítica en la 3.ª muestra afectada consecutiva. Sesión fallida a los 30 min consecutivos (01 §6.2 y §9.1). | **Criterio interno basado en las ventanas normativas.** |
| Contenido del informe | EURAMET cg-20 §7 y DKD-R 5-7: condiciones de trabajo, volumen calibrado, croquis de ubicación de los sensores, carga y método de cálculo. | El Excel incluye el equipo, los canales con su ubicación, las condiciones y los datos crudos. Los certificados quedan para una fase futura. | **Parcial.** Aplica en la fase de certificados. |

## 4. Hallazgos para decidir

1. **Intervalo de 2 min vs. 30 valores en 30 min (DKD-R 5-7 §7.3).** Si el laboratorio va a calibrar según DKD-R 5-7 o a confirmar el desempeño según IEC 60068-3-5, el intervalo debería ser de **60 s o menos**, al menos para el punto central de referencia. El sistema ya lo soporta cambiando `SamplingIntervalSeconds`: está marcado como **punto de cambio PC-01**, con la lista de ubicaciones en [01 §12](../specs/functional/01-vision-document.md#pc-01--intervalo-de-muestreo). Decisión del 2026-09-25: se mantiene 120 s en la fase 1. Con 60 s, 24 h serían 1441 muestras (la base ya no limita el número de muestra) y el mínimo de 1 h pasaría a 61 muestras válidas.
2. **Pérdida de un sensor de esquina.** Según DKD-R 5-7 §7.1.1, si falla un sensor de esquina, el volumen efectivamente medido se reduce, y el resultado no puede extrapolarse al volumen completo. El umbral del 60 % es útil para la **integridad de la captura**, pero para una calibración conviene que el revisor evalúe **qué** posiciones faltaron, no solo cuántas. Esto refuerza la pregunta P-13.
3. **Edición vigente de DKD-R 5-7.** La PTB publicó una edición 2025-01 que reemplaza a las anteriores, y a partir del 2028-01-01 solo se podrá usar la edición vigente. El enlace publicado no pudo descargarse el 2026-09-25. Queda pendiente obtenerla y revisar si cambian los apartados citados.

## 5. Mantenimiento de esta carpeta

- Al incorporar una norma nueva: añadir su ficha (usando las existentes como plantilla), una fila en el §1, su licencia en el §2 y los temas que afecte en el §3.
- Guardar copia local **solo** si la licencia lo permite, y registrar su huella SHA-256.
- Revisar las ediciones vigentes una vez al año, o antes de cada fase del proyecto.
