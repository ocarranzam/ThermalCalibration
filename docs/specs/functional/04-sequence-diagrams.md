# 04 · Diagramas de secuencia

| Campo | Valor |
|---|---|
| Versión | 0.7 (borrador para revisión) |
| Cambios en 0.7 | Validación con `@mermaid-js/mermaid-cli` y versión SVG de cada diagrama. Correcciones S-01 a S-07 ([validation.md §6.1](../../validation.md#61-diagramas-de-secuencia-2026-09-25)): fórmulas con parámetros en lugar del literal 120 (PC-01), cancelación en el diagrama 4, aviso de menos de 9 puntos en el Excel y eventos del simulador dentro del bucle. |
| Cambios en 0.6 | Cada diagrama pasa a su propio archivo en [docs/diagrams/sequence/](../../diagrams/sequence/). Este documento queda como índice, con los participantes y las convenciones. |
| Cambios en 0.5 | Diagrama 1: advertencia y confirmación con menos de 9 puntos de medición. |
| Cambios en 0.4 | Diagrama 2: escalamiento de la lectura fuera de límite sostenida (30 min) con causa probable. |
| Cambios en 0.3 | Duración planificada y descanso del adquisidor (diagrama 1), escalamiento y falla por pérdida de sensores (diagrama 2), falla por 30 min sin comunicación (diagrama 3) y cierre por duración planificada (diagrama 4). |
| Cambios en 0.2 | Inicio con la primera muestra recibida, modo `STREAM`, evaluación de la pérdida de sensores, invalidación por cambio de adquisidor o de grupo, cierre según las muestras válidas y el nuevo diagrama 5 (sesión simulada). |
| Relacionado | [02-serial-protocol.md](02-serial-protocol.md), [03-user-stories.md](03-user-stories.md), [05-data-model.md](05-data-model.md), [06-test-data.md](06-test-data.md) |

## Diagramas

| # | Diagrama | SVG | Historias |
|---|---|---|---|
| 1 | [Configuración, detección del adquisidor, advertencia de mezcla e inicio al llegar datos](../../diagrams/sequence/01-configuracion-e-inicio.md) | [svg](../../diagrams/sequence/svg/01-configuracion-e-inicio.svg) | HU-03, HU-04, HU-05, HU-16, HU-17 e inicio de HU-06 |
| 2 | [Ciclo de captura cada 2 minutos](../../diagrams/sequence/02-ciclo-de-captura.md) | [svg](../../diagrams/sequence/svg/02-ciclo-de-captura.svg) | HU-06, HU-07, HU-08, HU-13 |
| 3 | [Pérdida y recuperación de la comunicación serial](../../diagrams/sequence/03-perdida-de-comunicacion.md) | [svg](../../diagrams/sequence/svg/03-perdida-de-comunicacion.svg) | HU-09 |
| 4 | [Cierre, cancelación y exportación a Excel](../../diagrams/sequence/04-cierre-y-exportacion.md) | [svg](../../diagrams/sequence/svg/04-cierre-y-exportacion.svg) | HU-10, HU-11 |
| 5 | [Sesión simulada con el set de datos de prueba](../../diagrams/sequence/05-sesion-simulada.md) | [svg](../../diagrams/sequence/svg/05-sesion-simulada.svg) | HU-14 |

Los diagramas están en Mermaid. GitHub los muestra como gráfico. En VS Code se ven con la vista previa de Markdown (`Ctrl+Shift+V`) y la extensión **Markdown Preview Mermaid Support** (`bierner.markdown-mermaid`), recomendada en [.vscode/extensions.json](../../../.vscode/extensions.json).

Cada diagrama tiene además su versión **SVG** (fondo blanco, tamaño real), generada con `@mermaid-js/mermaid-cli` por `node docs/diagrams/render-sequence-svg.mjs`. El script también **valida** la sintaxis: si un diagrama tiene un error, falla y no genera su SVG. Regenerarlos después de cualquier cambio en un diagrama.

En los diagramas, los valores que dependen del intervalo de muestreo se escriben con el nombre del parámetro (`intervalo`, `SensorLossFailMinutes`…) y el valor por defecto entre paréntesis, según la regla **[PC-01]**.

## Participantes comunes

| Participante | Descripción |
|---|---|
| Técnico | Usuario con el rol `Technician`. |
| UI | Interfaz de la aplicación de captura. |
| Sesión | Servicio de sesión de medición: reglas de negocio, ciclo de vida, alertas, pérdida de sensores. |
| Serial | Servicio de adquisición serial: puerto, protocolo, validación de tramas, programador de muestreo. |
| ADQ | Adquisidor (Arduino, Raspberry Pi o simulado) conectado por el puerto COM. |
| BD | SQL Server 2022, base `ThermalCalibration`. |
| Excel | Generador del archivo .xlsx. |

Los checksums de las tramas se abrevian como `*CS`. Los valores reales están en [02-serial-protocol.md](02-serial-protocol.md).
