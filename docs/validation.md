# Validación y trazabilidad

Cómo se comprueba que la especificación de la fase 1 es coherente y cómo se valida la aplicación contra ella: historias (HU), reglas de negocio (RN), escenarios de prueba (TD) y normas.

| Campo | Valor |
|---|---|
| Versión | 0.2 |
| Cambios en 0.2 | Hallazgos H-01 a H-08 corregidos en 03 v0.6 y 06 v0.5. Diagramas separados en `docs/diagrams/gherkin/` y `docs/diagrams/sequence/`. |
| Fecha | 2026-09-25 |
| Fuentes | [03-user-stories.md](specs/functional/03-user-stories.md) v0.6, [06-test-data.md](specs/functional/06-test-data.md) v0.5, [01-vision-document.md](specs/functional/01-vision-document.md), [test-data/scenarios/index.json](../test-data/scenarios/index.json), [standards/README.md](standards/README.md) |
| Diagramas | [docs/diagrams/gherkin/](diagrams/gherkin/) (un `.feature` por historia, generado) y [docs/diagrams/sequence/](diagrams/sequence/) (secuencia en Mermaid) |

> Este documento no define reglas nuevas. Ante cualquier diferencia, prevalecen 01, 03 y 06.

---

## 1. Mapa de artefactos

| Artefacto | Qué valida | Dónde |
|---|---|---|
| Reglas de negocio RN-01 a RN-20 | Qué debe cumplir el sistema | [01 §6](specs/functional/01-vision-document.md#6-reglas-de-negocio-principales) |
| Historias HU-01 a HU-17 | Criterios de aceptación en Gherkin | [03-user-stories.md](specs/functional/03-user-stories.md) |
| **Features Gherkin** | Los mismos criterios, en archivos `.feature` ejecutables con etiquetas | [docs/diagrams/gherkin/](diagrams/gherkin/) |
| **Diagramas de secuencia** | Flujos de configuración, captura, comunicación, cierre y simulación (Mermaid). Índice y participantes en [04](specs/functional/04-sequence-diagrams.md) | [docs/diagrams/sequence/](diagrams/sequence/) |
| Escenarios TD-01 a TD-22 | Datos reproducibles y resultado esperado (oráculo) | [test-data/scenarios/](../test-data/scenarios/) |
| Restricciones de base | Segunda línea de defensa (`CHECK`, vistas) | [db/01-schema.sql](db/01-schema.sql) |
| Matriz norma → especificación | Alineación con OMS, IEC 60068-3-5, EURAMET cg-20 y DKD-R 5-7 | [standards/README.md §3](standards/README.md#3-matriz-de-trazabilidad-norma--especificación) |

## 2. Features Gherkin (`docs/diagrams/gherkin/`)

En VS Code, los `.feature` se resaltan con la extensión `alexkrechik.cucumberautocomplete`, y los diagramas de secuencia se ven como gráfico en la vista previa de Markdown (`Ctrl+Shift+V`) con `bierner.markdown-mermaid`. Ambas están recomendadas en [.vscode/extensions.json](../.vscode/extensions.json). GitHub muestra los diagramas Mermaid sin extensiones.

Cada historia de 03 tiene su archivo `.feature`, con el mismo texto y estas etiquetas en la línea `Feature`:

| Etiqueta | Significado | Ejemplo |
|---|---|---|
| `@HU-nn` | Historia | `@HU-13` |
| `@SM`, `@AS`, `@EX` | Módulo: Sesión de Medición, Adquisición Serial, Exportación | `@SM` |
| `@RN-nn` | Reglas citadas en la línea "Reglas:" de la historia | `@RN-14 @RN-15` |
| `@TD-nn` | Escenarios de prueba que la ejercitan | `@TD-16` |

Con las etiquetas se ejecuta un subconjunto, p. ej. todo lo que toca la pérdida de sensores: `--tags "@RN-14"` (Reqnroll/SpecFlow en .NET: `--filter "Category=RN-14"`).

**Regenerar** (después de cualquier cambio en 03 o 06):

```bash
node docs/diagrams/generate-features.mjs
```

El script borra y vuelve a crear los `.feature`, y hace las comprobaciones de la §6. Termina con código 1 si encuentra inconsistencias. Los `.feature` no se editan a mano: se corrige la historia en 03 y se regenera.

| Archivo | Escenarios | Reglas | Escenarios de prueba |
|---|---|---|---|
| [HU-01-registrar-empresa-cliente-y-equipo.feature](diagrams/gherkin/HU-01-registrar-empresa-cliente-y-equipo.feature) | 7 | — | — |
| [HU-02-gestionar-tipos-de-equipo-y-limite-maximo.feature](diagrams/gherkin/HU-02-gestionar-tipos-de-equipo-y-limite-maximo.feature) | 13 | RN-04, 06, 07, 08 | TD-14, TD-19 |
| [HU-03-configurar-una-sesion-de-medicion.feature](diagrams/gherkin/HU-03-configurar-una-sesion-de-medicion.feature) | 11 | RN-01 | TD-01, TD-11 |
| [HU-04-advertir-la-mezcla-de-termopares-t-y-k.feature](diagrams/gherkin/HU-04-advertir-la-mezcla-de-termopares-t-y-k.feature) | 6 | RN-05 | TD-10 |
| [HU-05-advertir-limite-no-definido.feature](diagrams/gherkin/HU-05-advertir-limite-no-definido.feature) | 3 | RN-09 | TD-14 |
| [HU-06-iniciar-al-llegar-datos-y-capturar-cada-2-minutos.feature](diagrams/gherkin/HU-06-iniciar-al-llegar-datos-y-capturar-cada-2-minutos.feature) | 10 | RN-02 | TD-01, TD-11, TD-12 |
| [HU-07-registrar-lecturas-invalidas-sin-detener-la-sesion.feature](diagrams/gherkin/HU-07-registrar-lecturas-invalidas-sin-detener-la-sesion.feature) | 7 | RN-11 | TD-09, TD-15 |
| [HU-08-alertar-lecturas-fuera-de-limite.feature](diagrams/gherkin/HU-08-alertar-lecturas-fuera-de-limite.feature) | 9 | RN-07, 10, 13, 18, 19 | TD-02, TD-15, TD-20, TD-21 |
| [HU-09-detectar-perdida-de-comunicacion-reconectar-y-validar-el-adquisidor.feature](diagrams/gherkin/HU-09-detectar-perdida-de-comunicacion-reconectar-y-validar-el-adquisidor.feature) | 10 | RN-12, 13, 14, 15 | TD-06, TD-07, TD-08 |
| [HU-10-cerrar-la-sesion-duracion-planificada.feature](diagrams/gherkin/HU-10-cerrar-la-sesion-duracion-planificada.feature) | 9 | RN-03, 04, 14, 15 | TD-05, TD-10, TD-12, TD-13 |
| [HU-11-exportar-una-sesion-a-excel.feature](diagrams/gherkin/HU-11-exportar-una-sesion-a-excel.feature) | 11 | — | Todos |
| [HU-12-consultar-el-historial-de-sesiones.feature](diagrams/gherkin/HU-12-consultar-el-historial-de-sesiones.feature) | 8 | — | — |
| [HU-13-evaluar-escalar-y-fallar-por-perdida-de-sensores.feature](diagrams/gherkin/HU-13-evaluar-escalar-y-fallar-por-perdida-de-sensores.feature) | 11 | RN-14, 15 | TD-03, TD-04, TD-05, TD-15, TD-16, TD-18 |
| [HU-14-ejecutar-una-sesion-simulada-con-el-set-de-datos-de-prueba.feature](diagrams/gherkin/HU-14-ejecutar-una-sesion-simulada-con-el-set-de-datos-de-prueba.feature) | 5 | RN-16 | Todos |
| [HU-15-priorizar-alertas-criticas-y-advertencias.feature](diagrams/gherkin/HU-15-priorizar-alertas-criticas-y-advertencias.feature) | 4 | RN-18 | TD-02, TD-04, TD-16, TD-20, TD-21 |
| [HU-16-planificar-la-duracion-y-respetar-el-descanso-del-adquisidor.feature](diagrams/gherkin/HU-16-planificar-la-duracion-y-respetar-el-descanso-del-adquisidor.feature) | 9 | RN-04, 17 | TD-10, TD-12, TD-17, TD-19 |
| [HU-17-advertir-menos-de-9-puntos-de-medicion.feature](diagrams/gherkin/HU-17-advertir-menos-de-9-puntos-de-medicion.feature) | 6 | RN-20 | TD-15, TD-22 |
| **Total** | **139** (128 `Scenario` y 11 `Scenario Outline`) | | |

## 3. Cobertura de las reglas de negocio

| Regla | Tema | Historias | Escenarios de prueba |
|---|---|---|---|
| RN-01 | 1 a 10 canales, tipo y ubicación | HU-03 | TD-01, TD-11 |
| RN-02 | **[PC-01]** Muestreo cada 120 s, muestra 1 en t = 0 | HU-06 | TD-01, TD-11, TD-12 |
| RN-03 | Completa: duración planificada y ≥ 31 muestras válidas | HU-10 | TD-05, TD-10, TD-13 |
| RN-04 | Duración planificada (base, tipo, cliente; máx. 7 días) | HU-02, HU-10, HU-16 | TD-12, TD-17, TD-19 |
| RN-05 | Mezcla T/K con advertencia y confirmación | HU-04 | TD-10 |
| RN-06 | Límite máximo por tipo, puede quedar pendiente | HU-02 | TD-14 |
| RN-07 | Fuera de límite estricto (`>`) | HU-02, HU-08 | TD-02 |
| RN-08 | Límite y política copiados al iniciar | HU-02 | — (prueba unitaria y de base) |
| RN-09 | Límite no definido → `LimitNotDefined` | HU-05 | TD-14, TD-19 |
| RN-10 | Fuera de límite no invalida la sesión | HU-08 | TD-02, TD-20, TD-21 |
| RN-11 | Lecturas inválidas marcadas, sin detener | HU-07 | TD-09, TD-15 |
| RN-12 | Pérdida de comunicación → hueco y reintento | HU-09 | TD-06, TD-15 |
| RN-13 | Lecturas, alertas y huecos inmutables | HU-08, HU-09 | — (prueba de dominio y de base) |
| RN-14 | Muestra afectada si > 60 % sin dato válido | HU-09, HU-10, HU-13 | TD-03, TD-04, TD-05, TD-18 |
| RN-15 | Sesión fallida (`DeviceMismatch`, `DataLoss`) | HU-09, HU-10, HU-13 | TD-07, TD-08, TD-16 |
| RN-16 | Sesiones simuladas marcadas | HU-14 | Todos |
| RN-17 | Descanso de 15 min del adquisidor | HU-16 | — (sesiones consecutivas; ningún TD lo cubre) |
| RN-18 | Críticas notificadas, advertencias solo registradas | HU-08, HU-15 | TD-02, TD-04, TD-16, TD-20, TD-21 |
| RN-19 | Fuera de límite 30 min → crítica con causa probable | HU-08 | TD-20, TD-21 |
| RN-20 | Mínimo de 9 puntos de medición | HU-17 | TD-22 (9 canales) y los de menos canales |

Historias sin regla citada: HU-01 (reglas de unicidad propias), HU-11 y HU-12 (formato del Excel y consulta). HU-01 y HU-12 no tienen escenario TD: se validan con pruebas de aplicación y de base.

## 4. Oráculo de los escenarios de prueba

Resultado esperado de cada escenario según [index.json](../test-data/scenarios/index.json). La aplicación, ejecutada con el adquisidor simulado, debe reproducirlo exactamente (validador SIM-09 de [06 §3](specs/functional/06-test-data.md#3-adquisidor-simulado)).

| TD | Historias (03) | Canales | Estado | Motivo | Muestras prog. / válidas / afect. |
|---|---|---|---|---|---|
| TD-01 | HU-03, HU-06 | 10 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-02 | HU-08, HU-15 | 5 | Completed | PlannedDuration | 91 / 91 / 0 |
| TD-03 | HU-13 | 10 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-04 | HU-13, HU-15 | 10 | Completed | PlannedDuration | 61 / 51 / 10 |
| TD-05 | HU-10, HU-13 | 5 | Incomplete | PlannedDuration | 31 / 21 / 10 |
| TD-06 | HU-09 | 5 | Completed | PlannedDuration | 61 / 56 / 5 |
| TD-07 | HU-09 | 5 | Invalid | DeviceMismatch | 54 / 49 / 5 |
| TD-08 | HU-09 | 5 | Invalid | DeviceMismatch | 32 / 29 / 3 |
| TD-09 | HU-07 | 5 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-10 | HU-04, HU-10, HU-16 | 5 | Completed | PlannedDuration | 31 / 31 / 0 |
| TD-11 | HU-03, HU-06 | 5 | Completed | PlannedDuration | 31 / 31 / 0 |
| TD-12 | HU-06, HU-10, HU-16 | 10 | Completed | PlannedDuration | 721 / 721 / 0 |
| TD-13 | HU-10 | 5 | Incomplete | Manual | 23 / 23 / 0 |
| TD-14 | HU-02, HU-05 | 5 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-15 | HU-07, HU-08, HU-13, HU-17 | 8 | Completed | PlannedDuration | 181 / 174 / 7 |
| TD-16 | HU-13, HU-15 | 5 | Invalid | DataLoss | 35 / 19 / 16 |
| TD-17 | HU-16 | 5 | Completed | PlannedDuration | 2161 / 2161 / 0 |
| TD-18 | HU-13 | 5 | Completed | PlannedDuration | 91 / 71 / 20 |
| TD-19 | HU-02, HU-16 | 5 | Incomplete | Manual | 46 / 46 / 0 |
| TD-20 | HU-08, HU-15 | 5 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-21 | HU-08, HU-15 | 5 | Completed | PlannedDuration | 61 / 61 / 0 |
| TD-22 | HU-17 | 9 | Completed | PlannedDuration | 31 / 31 / 0 |

Alertas esperadas por tipo: [06 §5](specs/functional/06-test-data.md#5-catálogo-de-escenarios) y `expected.alerts[]` de cada `scenario.json`.

## 5. Niveles de validación de la aplicación

Resumen de [06 §7](specs/functional/06-test-data.md#7-uso-en-las-pruebas), con los artefactos de este documento:

| Nivel | Entrada | Criterio de aceptación |
|---|---|---|
| Aceptación (BDD) | `docs/diagrams/gherkin/*.feature` | Los 139 escenarios pasan. Cada `@TD-nn` se ejecuta con su escenario de prueba. |
| Unitario: parser de tramas | `transcript.log` | Cada línea `ADQ` se clasifica como su fila de `readings.csv` (V1–V10). |
| Unitario: reglas de dominio | `readings.csv`, `scenario.json` | Resultados iguales a `expected`, incluida la severidad de cada alerta. |
| Integración con el simulador | Carpeta del escenario | SIM-09 informa "OK" en los 22 escenarios. |
| Base de datos | [01-schema.sql](db/01-schema.sql) | Los 22 escenarios cargan sin violar restricciones; `Completed` se rechaza en TD-19. |
| Presentación de alertas | TD-02, TD-04, TD-16, TD-20, TD-21 | Número de avisos críticos según 06 §7. |
| Excel | Sesiones simuladas | Celdas, filas y avisos según 06 §7. |
| Rendimiento | TD-17 (72 h) | Exportación en menos de 15 s. |
| Invariante PC-01 | Código fuente | Sin literales `120` ni `31`: todo se deriva de `SamplingIntervalSeconds` ([01 §12](specs/functional/01-vision-document.md#pc-01--intervalo-de-muestreo)). |

## 6. Verificación de la especificación (2026-09-25)

Comprobaciones automáticas de `generate-features.mjs`, más un análisis con el parser oficial `@cucumber/gherkin`:

| # | Comprobación | Resultado |
|---|---|---|
| V-01 | Los 17 `.feature` se analizan sin errores con `@cucumber/gherkin` | ✅ 17/17, 139 escenarios |
| V-02 | Cada `Scenario Outline` tiene `Examples` y cada `<parámetro>` su columna | ✅ |
| V-03 | Catálogo de 06 §5 = `index.json` (canales, modo, duración, muestras, estado, motivo, alertas por tipo) | ✅ 22/22 |
| V-04 | Cada escenario tiene `scenario.json`, `readings.csv` y `transcript.log` | ✅ 22/22 |
| V-05 | Ejemplos de HU-14 = `index.json` | ✅ 14/14 |
| V-06 | Índice de 03 (Escenarios de prueba) = 06 §5.1 (Historias) | ✅ (tras corregir H-01 a H-06) |
| V-07 | Enlaces del índice de 03 apuntan a su historia | ✅ (tras corregir H-08) |
| V-08 | Toda regla RN está citada por alguna historia | ✅ (tras corregir H-07) |

### Hallazgos corregidos

Ninguno cambiaba el comportamiento esperado: eran inconsistencias de trazabilidad entre documentos. Se corrigieron en 03 v0.6 y 06 v0.5 el 2026-09-25, y el generador termina sin inconsistencias.

| Id | Hallazgo | Corrección aplicada |
|---|---|---|
| H-01 | 06 §5.1 asignaba TD-19 a HU-02, pero el índice de 03 no lo listaba. | TD-19 añadido en HU-02 (03). |
| H-02 | 03 asignaba TD-14 a HU-02; 06 §5.1 solo listaba HU-05. | HU-02 añadida en TD-14 (06). |
| H-03 | 03 asignaba TD-11 a HU-03; 06 §5.1 solo listaba HU-06. | HU-03 añadida en TD-11 (06). |
| H-04 | 03 asignaba TD-12 a HU-06; 06 §5.1 listaba HU-10 y HU-16. | HU-06 añadida en TD-12 (06). |
| H-05 | 03 asignaba TD-16 a HU-15; 06 §5.1 solo listaba HU-13. | HU-15 añadida en TD-16 (06). |
| H-06 | 03 asignaba TD-10 a HU-16; 06 §5.1 listaba HU-04 y HU-10. | HU-16 añadida en TD-10 (06). |
| H-07 | RN-13 (inmutabilidad) no aparecía en la línea "Reglas:" de ninguna historia. | RN-13 citada en HU-08 (alertas) y HU-09 (huecos). |
| H-08 | El índice de 03 enlazaba HU-10 con el título antiguo "duración mínima y máxima" y el enlace no llevaba a la historia. | Título y ancla actualizados a "duración planificada". |

Quedan abiertos los puntos que condicionan la validación normativa, sin afectar la coherencia interna: P-11 / PC-01 (intervalo de 120 s frente a los 60 s de IEC 60068-3-5 y DKD-R 5-7), P-12 (duración mínima por tipo) y la edición 2025-01 de DKD-R 5-7 ([standards/README.md §4](standards/README.md#4-hallazgos-para-decidir)).

## 7. Mantenimiento

1. Cambiar una historia en 03 o un escenario en 06 o en el generador de datos.
2. Si cambió el set de datos: `node test-data/generate-test-data.mjs`.
3. `node docs/diagrams/generate-features.mjs` y revisar que termine sin inconsistencias.
4. Actualizar las tablas de las §2 a §4 y el registro de la §6 de este documento.
