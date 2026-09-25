# Normas técnicas de referencia

Normas y guías internacionales que sustentan las reglas del Sistema de Monitoreo Térmico para Calibración de Equipos de Refrigeración.

| Campo | Valor |
|---|---|
| Versión | 0.1 |
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

## 2. Licencias y copias locales

| Documento | Licencia o condición de uso | Decisión |
|---|---|---|
| EURAMET cg-20 | © EURAMET e.V. Se puede reproducir **solo completo** y no para la venta. Los extractos requieren permiso de la Secretaría de EURAMET. | Se guarda la copia completa en `pdf/`. Las fichas resumen el contenido con palabras propias. |
| DKD-R 5-7 (09/2018) | Creative Commons **CC BY-NC-ND 3.0 DE**. Permite redistribuir sin modificar, sin fines comerciales y citando la fuente, y autoriza expresamente el uso de su contenido en laboratorios con fines comerciales. | Se guarda la copia en `pdf/`. |
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
| Cantidad y ubicación de sensores | IEC 60068-3-5 §4.4 y DKD-R 5-7 §5 y §7.1.1: mínimo **9** puntos (8 esquinas y el centro) en volúmenes < 2000 L, y 15 puntos o una rejilla de 1 m en volúmenes mayores. | 1 a 10 canales, con la ubicación en texto libre (RN-01). | **Diferencia.** Pregunta abierta P-13. |
| Intervalo de registro | OMS: de 1 a 15 min. IEC 60068-3-5 §4.4: al menos 1 por minuto para confirmar el desempeño. DKD-R 5-7 §7.3: al menos **30 valores en 30 min** para la inestabilidad temporal. | 120 s (RN-02), configurable en `SamplingIntervalSeconds`. | **Cumple la OMS, pero no IEC ni DKD.** Pregunta abierta P-11. |
| Duración del ensayo | OMS: de 24 a 72 h para cámaras frías y congeladoras. IEC 60068-3-5 §4.5 y DKD-R 5-7 §7.3: al menos 30 min después de la estabilización. | 1 h mínimo y 24 h máximo (RN-03, RN-04). | **Parcial.** Pregunta abierta P-12. |
| Un solo tipo de dispositivo | OMS, paso 1: usar un solo tipo de dispositivo por estudio de mapeo. | La mezcla T/K se permite con advertencia y confirmación (RN-05). | **Alineado.** La advertencia sigue la recomendación. |
| Sensores y tolerancias | IEC 60068-3-5 §4.2: termopares o PT100 calibrados, con respuesta térmica de 10 a 40 s al 50 %, preferiblemente termopares de clase 1 según IEC 60584-1. | Termopares T o K; el inventario y la calibración de sensores quedan para una fase futura. | **Pendiente** en la fase de inventario de sensores. |
| Registrador independiente | IEC 60068-3-5 §4.4: el registrador debe ser independiente del sistema de control del equipo. | El adquisidor propio es independiente del equipo bajo prueba. | **Alineado.** |
| Falla de sensores | Ninguna referencia fija un porcentaje admisible. La OMS pide un informe de desviación con posible remapeo parcial o total. DKD-R 5-7 §7.1.1: el resultado solo vale para el volumen que abarcan los puntos medidos y no se permite extrapolar. | Umbral interno del 60 % por muestra (RN-14) y alerta por cada falla individual. | **Criterio interno.** Ver la nota sobre las esquinas en §4. |
| Contenido del informe | EURAMET cg-20 §7 y DKD-R 5-7: condiciones de trabajo, volumen calibrado, croquis de ubicación de los sensores, carga y método de cálculo. | El Excel incluye el equipo, los canales con su ubicación, las condiciones y los datos crudos. Los certificados quedan para una fase futura. | **Parcial.** Aplica en la fase de certificados. |

## 4. Hallazgos para decidir

1. **Intervalo de 2 min vs. 30 valores en 30 min (DKD-R 5-7 §7.3).** Si el laboratorio va a calibrar según DKD-R 5-7 o a confirmar el desempeño según IEC 60068-3-5, el intervalo debería ser de **60 s o menos**, al menos para el punto central de referencia. El sistema ya lo soporta cambiando `SamplingIntervalSeconds`. Con 60 s, la muestra máxima sería la 1441 en 24 h, así que habría que ampliar `CK_Reading_SampleNumber` y revisar los conteos (31 muestras en 1 h pasarían a ser 61).
2. **Pérdida de un sensor de esquina.** Según DKD-R 5-7 §7.1.1, si falla un sensor de esquina, el volumen efectivamente medido se reduce, y el resultado no puede extrapolarse al volumen completo. El umbral del 60 % es útil para la **integridad de la captura**, pero para una calibración conviene que el revisor evalúe **qué** posiciones faltaron, no solo cuántas. Esto refuerza la pregunta P-13.
3. **Edición vigente de DKD-R 5-7.** La PTB publicó una edición 2025-01 que reemplaza a las anteriores, y a partir del 2028-01-01 solo se podrá usar la edición vigente. El enlace publicado no pudo descargarse el 2026-09-25. Queda pendiente obtenerla y revisar si cambian los apartados citados.

## 5. Mantenimiento de esta carpeta

- Al incorporar una norma nueva: añadir su ficha (usando las existentes como plantilla), una fila en el §1, su licencia en el §2 y los temas que afecte en el §3.
- Guardar copia local **solo** si la licencia lo permite, y registrar su huella SHA-256.
- Revisar las ediciones vigentes una vez al año, o antes de cada fase del proyecto.
