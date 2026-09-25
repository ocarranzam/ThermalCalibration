# EURAMET cg-20 v3.0 – Calibración de cámaras climáticas

| Campo | Valor |
|---|---|
| Título original | *Calibration of Climatic Chambers – Guidance for Calibration Laboratories* (EURAMET Calibration Guide No. 20) |
| Emisor | EURAMET e.V. (European Association of National Metrology Institutes) |
| Edición | Versión 3.0, 03/2011. ISBN 978-3-942992-17-6. |
| Tipo | Guía de calibración (recomendada también a los organismos de acreditación) |
| Acceso | Gratuito: <https://www.euramet.org/Media/docs/Publications/calguides/EURAMET_cg-20__v_3.0_Calibration_of_Climatic_Chambers.pdf> |
| Copia local | [pdf/EURAMET_cg-20_v3.0_2011.pdf](pdf/EURAMET_cg-20_v3.0_2011.pdf) |
| Licencia | © EURAMET e.V. Se puede reproducir solo completo y no para la venta. Los extractos requieren permiso de la Secretaría de EURAMET. |

## 1. Alcance

Establece los requisitos técnicos básicos para los laboratorios que calibran cámaras climáticas y armoniza sus prácticas. No reemplaza a las normas existentes: las referencia y resume, entre ellas DKD-R 5-7, IEC 60068-3-5, IEC 60068-3-6, IEC 60068-3-11 y NF X 15-140.

## 2. Puntos relevantes para el proyecto

| Tema | Resumen (paráfrasis) | Apartado |
|---|---|---|
| Qué es calibrar | Determinar la desviación entre la indicación de la cámara y los valores medidos dentro de ella. También se pueden caracterizar la distribución espacial, la estabilidad temporal, la inercia térmica, el tiempo de recuperación, etc. | §3 |
| Sensor junto a la carga | Un sensor independiente cerca de la carga da datos mucho más fiables que la indicación de la cámara. El laboratorio debería informarlo al cliente. | §3 |
| Alcance acordado | El laboratorio y el cliente acuerdan el alcance, la duración (estabilización previa y tiempo de registro), los parámetros a determinar y la carga. El laboratorio debe dejar constancia de estas condiciones. | §3 |
| Primera caracterización | Si la cámara se caracteriza por primera vez o se modificó, calibrarla vacía y con carga. | §3 |
| Criterios de aceptación de la cámara | El laboratorio define cuándo una cámara es calibrable (volumen, control, documentación técnica) y remite a la lista del §5 de DKD-R 5-7. | §4 |
| Contribuciones a la incertidumbre | Distribución espacial, estabilidad temporal, patrón de trabajo, efecto de radiación, diferencias temporales entre el aire, la sonda y la carga, efecto de la carga, condiciones ambientales y resolución. | §5 |
| Informe o certificado | Además de lo exigido por ISO/IEC 17025: condiciones de trabajo de la cámara, volumen calibrado con un **diagrama de la distribución de los sensores**, características de la carga y definición y método de cálculo de los parámetros derivados, con su incertidumbre. | §7 |
| Falla de sensores | **No** define un porcentaje admisible. | — |

## 3. Relación con la especificación

| Regla del proyecto | Relación |
|---|---|
| Ubicación por canal (`SessionChannel.Position`) | Base para el diagrama de distribución de sensores que se exigirá en los certificados (fase futura). |
| Registro de condiciones de la sesión | El Excel ya registra el equipo, el técnico, el adquisidor, los canales y las fechas. Faltaría registrar la carga y la estabilización previa (fase de certificados). |
| Análisis estadístico (fase futura) | La distribución espacial y la estabilidad temporal son contribuciones obligatorias a la incertidumbre. |
