# DKD-R 5-7 – Calibración de cámaras climáticas

| Campo | Valor |
|---|---|
| Título original | *Richtlinie DKD-R 5-7: Kalibrierung von Klimaschränken* (Guía DKD-R 5-7: Calibración de cámaras climáticas) |
| Emisor | Physikalisch-Technische Bundesanstalt (PTB) para el Deutscher Kalibrierdienst (DKD), comité técnico de Temperatura y Humedad |
| Edición revisada | 09/2018, revisión 0. DOI [10.7795/550.20180828AH](https://doi.org/10.7795/550.20180828AH) |
| Edición vigente | **2025-01**: reemplaza a las anteriores, y desde el 2028-01-01 solo se podrá usar esa edición. Enlace publicado: <https://www.ptb.de/cms/fileadmin/internet/dienstleistungen/dkd/archiv/Publikationen/Richtlinien/DKD-R_5-7_2025-01.pdf> (no se pudo descargar el 2026-09-25: **pendiente**). |
| Tipo | Guía de calibración (requisitos mínimos para laboratorios acreditados) |
| Idioma | Alemán (existe una versión en inglés de ediciones anteriores) |
| Copia local | [pdf/DKD-R_5-7_2018-09.pdf](pdf/DKD-R_5-7_2018-09.pdf) |
| Licencia | CC BY-NC-ND 3.0 DE. Permite redistribuir sin modificar, sin fines comerciales y citando la fuente. Autoriza expresamente el uso de su contenido en laboratorios con fines comerciales. |
| Cita sugerida | Richtlinie DKD-R 5-7, Kalibrierung von Klimaschränken, Ausgabe 09/2018, Revision 0, Physikalisch-Technische Bundesanstalt, Braunschweig und Berlin. |

## 1. Alcance

Requisitos mínimos para calibrar las indicaciones de temperatura del aire y de humedad relativa de cámaras climáticas: el objetivo de la calibración, los métodos, el procedimiento y las contribuciones a la incertidumbre. Cubre cámaras con circulación de aire (de -90 a 500 °C) y sin ella (de -90 a 350 °C, hasta 2000 L).

## 2. Puntos relevantes para el proyecto

| Tema | Resumen (paráfrasis) | Apartado |
|---|---|---|
| Calibrabilidad | La cámara debe tener sensores e indicación propios, regulación, especificaciones del fabricante y documentación de los sensores, y trabajar a presión atmosférica. Si opera en un rango, se calibra en al menos 3 puntos del rango. Un solo punto es admisible, pero limita el resultado a ese punto. | §5 |
| Número de puntos | Volumen < 2000 L: **al menos 9 puntos**, en las 8 esquinas y el centro de un prisma que abarque el volumen útil (como en EN 60068-3-5). Volumen ≥ 2000 L: rejilla cúbica con 1 m de separación máxima entre puntos. | §5 a), §7.1.1 |
| Carga | Si no se acuerda otra cosa con el cliente, la carga ocupa al menos el 40 % del volumen útil (métodos B, y cámaras sin circulación). | §5 b), §6 |
| Métodos | (A) Volumen útil vacío. (B) Volumen útil con carga. (C) Puntos individuales, solo a pedido expreso del cliente, con el resultado válido solo en esos puntos. | §6 |
| Validez espacial | El resultado vale **solo para el volumen que abarcan los puntos medidos**. Se puede interpolar dentro de ese volumen, pero **no extrapolar** fuera de él. La ubicación de los puntos se muestra en un croquis en el certificado. | §7.1.1 |
| Inhomogeneidad espacial | Desviación máxima de un punto de esquina o de borde respecto del punto de referencia (normalmente el centro), en cada temperatura calibrada. | §7.2.1 |
| Inestabilidad temporal | Registro durante **al menos 30 min** después de alcanzar el estado estable, con **al menos 30 valores en esos 30 min** a intervalos aproximadamente constantes, como mínimo en el centro o punto de referencia. Se evalúa en todos los métodos. | §7.3 |
| Efecto de radiación | Se debe determinar cuando la temperatura del aire difiere de la ambiente (las paredes quedan a otra temperatura). | §7.4 |
| Falla de sensores | **No** define un porcentaje admisible. Consecuencia práctica de §7.1.1: si falta un punto de esquina, el volumen efectivamente calibrado se reduce. | — |

## 3. Relación con la especificación

| Regla del proyecto | Relación |
|---|---|
| RN-02 (intervalo de 120 s) | Da 15 valores en 30 min, **menos** que los 30 exigidos para la inestabilidad temporal. Si se calibra según esta guía, conviene usar 60 s o menos. Se mantiene 120 s en la fase 1: punto de cambio PC-01. |
| RN-01 y RN-20 (mínimo 9 puntos) | Alineado con §5 a) y §7.1.1: 9 puntos, con un canal de sobra (p. ej. junto a la carga). Menos puntos solo encajan en el método C (puntos individuales a pedido del cliente); el sistema lo advierte y lo marca. |
| RN-14 (umbral del 60 %) | Sirve para la integridad de la captura. Para la validez de una calibración según esta guía, importa **qué** posiciones faltan (esquinas o centro), no solo cuántas. El mínimo de 9 puntos ya se exige al iniciar (RN-20). Evaluar qué posiciones faltan por fallas queda para una fase futura. |
| RN-03 (duración mínima de 1 h) | Compatible con los 30 min de registro en estado estable, si la estabilización previa ocurre antes de iniciar la sesión. |
