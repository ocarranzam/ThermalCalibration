# Validación y trazabilidad

Cómo se comprueba que la especificación de la fase 1 es coherente y cómo se valida la aplicación contra ella: historias (HU), reglas de negocio (RN), escenarios de prueba (TD) y normas.

| Campo | Valor |
|---|---|
| Versión | 0.6 |
| Cambios en 0.6 | Sprints 1 y 2: parámetros del sistema (D-08), tipos de termopar, empresas y equipos (HU-01). Cobertura de HU-01 (§5.1.1), HU-02 con los parámetros, pruebas 179/179, auditoría del contrato 88/88 (v1.3.0) y ejemplo de RUC de HU-01 corregido. |
| Cambios en 0.5 | D-07 (criterio `Range` con mínimo y/o máximo, límites sugeridos): escenarios nuevos en HU-02 y HU-08 (152 en total), pruebas 94/94 y auditoría del contrato 33/33. |
| Cambios en 0.4 | Decisiones D-05 (criterio de límite: máximo o banda) y D-06 (hasta 27 canales, puntos mínimos por tipo) a partir de DATA-1: nuevos escenarios en HU-02, HU-03, HU-05, HU-08 y HU-17 (149 en total), cobertura de HU-02 ampliada (§5.1) y auditoría del contrato 29/29 (§5.2). |
| Cambios en 0.3 | Artefactos de código, pruebas y auditoría en la §1; estado de implementación por historia (§5.3); mantenimiento al añadir entidades (§7). Plan de las siguientes entidades en [implementation-plan.md](implementation-plan.md). |
| Cambios en 0.2 | Hallazgos H-01 a H-08 corregidos en 03 v0.6 y 06 v0.5. Diagramas separados en `docs/diagrams/gherkin/` y `docs/diagrams/sequence/`. |
| Fecha | 2026-09-26 |
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
| **Contrato OpenAPI** | Interfaz HTTP de lo implementado; `redocly lint` sin errores ni advertencias | [api/thermal-v1.yaml](api/thermal-v1.yaml) |
| **Código** | Implementación (Clean Architecture) | [src/](../src/) |
| **Pruebas automatizadas** | Unitarias e integración, vinculadas a cada historia con `[Trait("Story", "HU-xx")]` | [tests/](../tests/) (§5.1) |
| **Auditoría del contrato** | Respuestas reales de la API contra el contrato | [tools/contract-check/](../tools/contract-check/README.md) (§5.2) |
| **Plan de implementación** | Orden de las entidades pendientes y definición de terminado | [implementation-plan.md](implementation-plan.md) (§5.3) |
| **Registros reales** | Ensayos de campo analizados: contraste de las reglas del sistema con datos reales y base de los perfiles de eficacia | [data/](data/README.md) (DATA-1: cámara ambiental Memmert, 72 h, 12 × tipo T, óptima) |

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
| [HU-01-registrar-empresa-cliente-y-equipo.feature](diagrams/gherkin/HU-01-registrar-empresa-cliente-y-equipo.feature) | 8 | — | — |
| [HU-02-gestionar-tipos-de-equipo-y-limite-maximo.feature](diagrams/gherkin/HU-02-gestionar-tipos-de-equipo-y-limite-maximo.feature) | 19 | RN-04, 06, 07, 08, 20 | TD-14, TD-19 |
| [HU-03-configurar-una-sesion-de-medicion.feature](diagrams/gherkin/HU-03-configurar-una-sesion-de-medicion.feature) | 12 | RN-01, 06 | TD-01, TD-11 |
| [HU-04-advertir-la-mezcla-de-termopares-t-y-k.feature](diagrams/gherkin/HU-04-advertir-la-mezcla-de-termopares-t-y-k.feature) | 6 | RN-05 | TD-10 |
| [HU-05-advertir-limite-no-definido.feature](diagrams/gherkin/HU-05-advertir-limite-no-definido.feature) | 4 | RN-09 | TD-14 |
| [HU-06-iniciar-al-llegar-datos-y-capturar-cada-2-minutos.feature](diagrams/gherkin/HU-06-iniciar-al-llegar-datos-y-capturar-cada-2-minutos.feature) | 10 | RN-02 | TD-01, TD-11, TD-12 |
| [HU-07-registrar-lecturas-invalidas-sin-detener-la-sesion.feature](diagrams/gherkin/HU-07-registrar-lecturas-invalidas-sin-detener-la-sesion.feature) | 7 | RN-11 | TD-09, TD-15 |
| [HU-08-alertar-lecturas-fuera-de-limite.feature](diagrams/gherkin/HU-08-alertar-lecturas-fuera-de-limite.feature) | 12 | RN-07, 10, 13, 18, 19 | TD-02, TD-15, TD-20, TD-21 |
| [HU-09-detectar-perdida-de-comunicacion-reconectar-y-validar-el-adquisidor.feature](diagrams/gherkin/HU-09-detectar-perdida-de-comunicacion-reconectar-y-validar-el-adquisidor.feature) | 10 | RN-12, 13, 14, 15 | TD-06, TD-07, TD-08 |
| [HU-10-cerrar-la-sesion-duracion-planificada.feature](diagrams/gherkin/HU-10-cerrar-la-sesion-duracion-planificada.feature) | 9 | RN-03, 04, 14, 15 | TD-05, TD-10, TD-12, TD-13 |
| [HU-11-exportar-una-sesion-a-excel.feature](diagrams/gherkin/HU-11-exportar-una-sesion-a-excel.feature) | 11 | — | Todos |
| [HU-12-consultar-el-historial-de-sesiones.feature](diagrams/gherkin/HU-12-consultar-el-historial-de-sesiones.feature) | 8 | — | — |
| [HU-13-evaluar-escalar-y-fallar-por-perdida-de-sensores.feature](diagrams/gherkin/HU-13-evaluar-escalar-y-fallar-por-perdida-de-sensores.feature) | 11 | RN-14, 15 | TD-03, TD-04, TD-05, TD-15, TD-16, TD-18 |
| [HU-14-ejecutar-una-sesion-simulada-con-el-set-de-datos-de-prueba.feature](diagrams/gherkin/HU-14-ejecutar-una-sesion-simulada-con-el-set-de-datos-de-prueba.feature) | 5 | RN-16 | Todos |
| [HU-15-priorizar-alertas-criticas-y-advertencias.feature](diagrams/gherkin/HU-15-priorizar-alertas-criticas-y-advertencias.feature) | 4 | RN-18 | TD-02, TD-04, TD-16, TD-20, TD-21 |
| [HU-16-planificar-la-duracion-y-respetar-el-descanso-del-adquisidor.feature](diagrams/gherkin/HU-16-planificar-la-duracion-y-respetar-el-descanso-del-adquisidor.feature) | 9 | RN-04, 17 | TD-10, TD-12, TD-17, TD-19 |
| [HU-17-advertir-menos-puntos-que-el-minimo-del-tipo-de-equipo.feature](diagrams/gherkin/HU-17-advertir-menos-puntos-que-el-minimo-del-tipo-de-equipo.feature) | 7 | RN-20 | TD-15, TD-22 |
| **Total** | **152** (136 `Scenario` y 16 `Scenario Outline`) | | |

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

### 5.1 Cobertura automatizada de HU-02 (2026-09-25)

Pruebas en [tests/](../tests/), vinculadas con `[Trait("Story", "HU-02")]`. Resultado de `dotnet test --solution Thermal.slnx` (2026-09-26, tras los sprints 1 y 2 y la referencia Scalar): **180/180 correctas** (156 unitarias, 24 de integración con SQL Server 2022 en Docker), de todas las historias.

| Escenario Gherkin de HU-02 | Pruebas | Estado |
|---|---|---|
| Registrar un tipo de equipo con límite definido | `EquipmentTypeTests.Create_WithDefinedLimit_…`, `EquipmentTypeHandlersTests.Create_ValidCommand_…`, `EquipmentTypePersistenceTests.Add_Assigns…` | ✅ |
| Exigir una duración mínima mayor para un tipo de equipo | `ChangeMinSessionDuration_To120Minutes_…` | ✅ (la planificación con ese mínimo se probará con `MeasurementSession`) |
| Rechazar una duración mínima menor que la base | `ChangeMinSessionDuration_BelowBase_…`, `Create_MinSessionDuration_RespectsDatabaseRange` | ✅ |
| Registrar un tipo de equipo con límite pendiente | `Create_WithoutLimit_LeavesLimitPending`, `TemperatureLimitTests.Create_WithNull_…`, `ReadStore_Returns…` | ✅ |
| Rechazar un nombre de tipo duplicado | `Create_DuplicateName_ThrowsConflict…`, `Update_RenameToExistingName_…`, `Add_DuplicateNameWithDifferentCase_…`, `NameExists_IgnoresCase…` | ✅ |
| Rechazar un límite con formato inválido | `Create_WithThreeDecimalLimit_IsRejected`, `TemperatureLimitTests.Create_AcceptsAtMostTwoDecimals`, `Create_InvalidLimit_ThrowsBeforeQuerying…` | ✅ |
| Editar el límite sin afectar sesiones registradas | `ChangeLimit_ReplacesTheCatalogLimit`, `Update_ExistingType_…`, `Update_SetsUpdatedAt…` | ⚠️ Parcial: la copia del límite en la sesión (RN-08) requiere `MeasurementSession` |
| Editar el límite de una sesión en curso no la afecta | — | ⏳ Pendiente de `MeasurementSession` |
| Definir un límite que estaba pendiente | `ChangeLimit_FromPendingToDefined_…` | ⚠️ Parcial: el lado de la sesión 103 requiere `MeasurementSession` |
| Cambiar el umbral de pérdida de sensores | `SystemSettingsHandlersTests.Update_ValidValues_…`, `SettingsPersistenceTests.Replace_ChangedValue_…`, contract-check "PUT If-Match vigente" (parámetros) | ⚠️ Parcial: que la sesión 104 conserve el umbral (RN-08) requiere `MeasurementSession` |
| Rechazar un umbral fuera de rango | `SystemSettingsTests.Create_OutOfRange_IsRejected` (0 y 100, y el resto de rangos de D-08), `Create_ThresholdWithThreeDecimals_…`, contract-check "PUT umbral 100" | ✅ |
| Un técnico no puede editar límites | `EquipmentTypesControllerAuthorizationTests.WriteActions_RequireAdminRole` | ✅ (verificado además con la API en ejecución: 403) |
| Desactivar un tipo con equipos asociados | `Deactivate_MarksTheTypeInactive_…`, `Update_WithIsActiveFalse_…`, `CreateEquipment_InactiveType_IsRejected`, `UpdateEquipment_SameInactiveType_IsAllowed` | ⚠️ Parcial: la API no tiene `DELETE` (responde 405) y se desactiva con `PUT` e `isActive: false`; un tipo desactivado ya no se acepta para equipos nuevos y los equipos existentes lo conservan. Ofrecer la desactivación al intentar borrar es de la interfaz |
| Registrar un tipo de equipo con banda de tolerancia (D-05) | `Create_WithBand_StoresToleranceAndNoMaximum`, `Create_BandWith27Points_KeepsTheCriterion`, `Add_BandTypeWith27Points_RoundTrips`, contract-check "POST banda 27 puntos" | ✅ |
| Fijar los puntos de medición mínimos de un tipo de equipo (D-06) | `ChangeMinMeasurementPoints_ForIncubator_Requires27`, `Update_ToBand_…` | ⚠️ Parcial: la copia en la sesión requiere `MeasurementSession` |
| Rechazar un valor que no corresponde al criterio de límite (D-05, D-07) | `Create_WithValueOfTheOtherMode_IsRejected` (4 casos), `Create_WithMinimumNotBelowMaximum_IsRejected`, contract-check "rango con tolerancia", "rango con mínimo ≥ máximo", "banda con mínimo", "banda con máximo", "modo inválido" | ✅ |
| Registrar un tipo de equipo con rango de temperatura (D-07) | `Create_WithRange_StoresMinimumAndMaximum`, `Update_ToRange_StoresMinimumAndMaximum`, contract-check "POST rango +2 … +8 °C" | ✅ |
| Confirmar los límites sugeridos del catálogo inicial (D-07) | `ReadStore_ReturnsSeededTypes…` (refrigeradora +2 … +8 °C sugerida), `ChangeLimit_OnSuggestedSeed_ConfirmsTheLimit`, `Create_ByAdministrator_HasConfirmedLimit` | ✅ (la etiqueta del catálogo es de la interfaz; la API expone `isLimitSuggested`) |
| Rechazar puntos de medición mínimos fuera de rango (D-06) | `Create_MinMeasurementPoints_IsBetween1And27`, contract-check "POST 28 puntos" | ✅ |

Además: evaluación estricta del límite de HU-08 (Scenario Outline con -5,10 / -5,00 / -4,99 / -4,90 / 2,00), **evaluación de la banda** 20,00 ± 2,00 °C por arriba y por abajo (22,01 / 22,00 / 20,00 / 18,00 / 17,99, D-05), **evaluación del rango** +2 … +8 °C (8,01 / 8,00 / 5,00 / 2,00 / 1,99) y rango con solo mínimo (D-07) y lecturas sin límite de HU-05 (máximo y banda pendientes), en `TemperatureLimitTests`; concurrencia optimista (`If-Match` → 412) en pruebas unitarias y de integración; `UpdatedAt` nunca anterior a `CreatedAt` (`ThermalDbContextTests`, `Update_RightAfterCreate_…`).

Parámetros del sistema (D-08): rangos de los 8 parámetros, división exacta de la duración base por el intervalo y `MinValidSamples` derivado del intervalo (PC-01) en `SystemSettingsTests`; versión del conjunto (`ETag` = huella de las `RowVersion`) y 412 con una versión anterior en `SettingsPersistenceTests`; catálogo de termopares T y K en `ThermocoupleTypes_AreListedByCode`.

#### 5.1.1 Cobertura automatizada de HU-01 (2026-09-26)

Pruebas vinculadas con `[Trait("Story", "HU-01")]`: `CompanyTests`, `EquipmentTests`, `CompanyAndEquipmentHandlersTests`, `CompanyAndEquipmentPersistenceTests` y `CompanyAndSettingsAuthorizationTests`, más los casos de [companies.mjs](../tools/contract-check/cases/companies.mjs).

| Escenario Gherkin de HU-01 | Pruebas | Estado |
|---|---|---|
| Registrar una empresa nueva | `Create_ValidCompany_IsActive`, `CreateCompany_New_…`, `AddCompany_AssignsDatabaseValuesAndIsSearchable` (buscador por RUC y por razón social), contract-check "POST empresa válida", "GET buscar …" | ✅ |
| Rechazar un RUC duplicado | `CreateCompany_DuplicateTaxId_ThrowsConflictWithExistingId`, `AddCompany_DuplicateTaxId_…`, contract-check "POST RUC duplicado" (409 con `existingId`) | ✅ (abrir la empresa existente es de la interfaz, con `existingId`) |
| Rechazar un RUC con formato inválido (4 ejemplos) | `Create_InvalidTaxId_IsRejected` (los 4 mensajes), `CheckConstraint_RejectsInvalidTaxId` (`CK_Company_TaxId`), contract-check (4 casos) | ✅ |
| Registrar un equipo asociado a la empresa | `Create_ValidEquipment_…`, `CreateEquipment_Valid_AddsAndReturnsTypeName`, `AddEquipment_IsListedWithTheTypeName`, contract-check "POST equipo válido" | ⚠️ Parcial: "su historial de sesiones está vacío" requiere `MeasurementSession` (HU-12) |
| Rechazar un número de serie duplicado en la misma empresa | `CreateEquipment_DuplicateSerial_ThrowsConflict`, `AddEquipment_DuplicateSerialInSameCompany_IsRejected`, contract-check "POST serie duplicada", "PUT serie de otro equipo" | ✅ |
| Permitir la misma serie en otra empresa | `AddEquipment_SameSerialInOtherCompany_IsAllowed`, contract-check "POST misma serie en otra empresa" | ✅ |
| Datos obligatorios del equipo (4 ejemplos) | `Create_MissingRequiredData_IsRejected`, `CreateEquipment_UnknownType_IsRejected`, contract-check "POST sin serie / tipo / marca / modelo" (mensajes en español) | ✅ |
| Registrar un equipo con el modelo inferido | Dato de desarrollo DATA-1 en [02-dev-data.sql](db/02-dev-data.sql), `Create_InferredModel_IsPendingConfirmation`, `ConfirmModel_SetsModelAndConfirms`, contract-check "POST equipo con modelo inferido", "PUT confirmar modelo" | ⚠️ Parcial: el perfil de eficacia por modelo es de una fase posterior |

Además: autorización (`Admin` y `Technician` registran y editan; `Supervisor` solo consulta), concurrencia optimista de empresa y equipo (`If-Match` → 412) y un tipo de equipo desactivado que el equipo conserva al editarlo.

### 5.2 Auditoría código ↔ contrato ↔ Gherkin (2026-09-25)

Se levantó la API con `docker compose up` y se contrastaron **24 solicitudes reales** (ahora automatizadas en [tools/contract-check/](../tools/contract-check/README.md), casos de [equipment-types.mjs](../tools/contract-check/cases/equipment-types.mjs)) con [thermal-v1.yaml](api/thermal-v1.yaml): código de estado documentado, `Content-Type`, schema del cuerpo (validado con Ajv) y cabeceras (`ETag`, `Location`). Resultado final: **24/24 conformes**, y `redocly lint` sin errores ni advertencias. Tras D-05 y D-06 (contrato v1.1.0, 5 casos nuevos): **29/29 conformes**. Tras D-07 (contrato v1.2.0, 4 casos nuevos): **33/33 conformes**. Tras los sprints 1 y 2 (contrato v1.3.0; casos de [settings.mjs](../tools/contract-check/cases/settings.mjs) y [companies.mjs](../tools/contract-check/cases/companies.mjs), 55 nuevos) contra la base del compose recreada: **88/88 conformes** (2026-09-26).

| Id | Desviación encontrada | Corrección |
|---|---|---|
| A-01 | El contrato decía "Minimal APIs"; el código usa controladores. | Contrato actualizado (v1.0.1). |
| A-02 | El ejemplo 403 del contrato tenía un `detail` que la API no envía. | Ejemplo alineado con la respuesta real. |
| A-03 | Los 400 de formato (campo obligatorio ausente, propiedad desconocida, cuerpo vacío) los genera ASP.NET Core: mensajes en inglés, claves `name`, `$.color` o `""`, sin `detail` ni `instance`. No estaba documentado. | Descripción de `errors` y dos ejemplos nuevos en el contrato. |
| A-04 | En Docker las fechas salían en UTC (`+00:00`), contra el supuesto S-04 (America/Lima, `-05:00`). | `TZ: America/Lima` en SQL Server y en la API ([docker-compose.yml](../docker-compose.yml)). |
| A-05 | **Defecto:** `updatedAt` podía quedar un segundo **antes** que `createdAt`, porque SQL Server redondea `DATETIMEOFFSET(0)` y la API truncaba. | La API redondea igual que SQL Server (`ThermalDbContext.RoundToSecond`) + 2 pruebas nuevas. |
| A-06 | `If-Match: *`, el prefijo `W/` y un `If-Match` ilegible (→ 412) no estaban documentados. | Descripción del parámetro `If-Match`. |
| A-07 | No hay `DELETE` (405 con `Allow: GET, PUT`) y un `PUT` sin cambios no modifica `updatedAt` ni el `ETag`: sin documentar. | Convenciones y descripción del `PUT` en el contrato; fila de HU-02 en la §5.1. |
| A-08 | Los `servers` del contrato no incluían los entornos de desarrollo. | Servidores de `docker compose` (8080) y `dotnet run` (5001). |
| A-09 | `dotnet user-jwts create --audience X` **reescribe** `appsettings.Development.json` con solo esa audiencia y rompe los tokens del otro entorno. | Las tres audiencias quedan en `appsettings.Development.json`, y el README indica un único comando con las tres. |
| A-10 | El modelo de dominio mostraba `EquipmentTypeId Id`; el código usa `int`. | [domain-model.md](architecture/domain-model.md) actualizado. |
| A-12 | Los mensajes de campo obligatorio ausente los daba ASP.NET Core en inglés ("The Name field is required."), distintos de los de las historias. | `[Required(ErrorMessage = …)]` en español en todos los contratos de la API; ejemplo del contrato actualizado (v1.3.0). |
| A-13 | El ejemplo de HU-01 "existe la empresa con RUC 20601234561" tenía un dígito verificador inválido: la regla de la misma historia lo rechazaría. | Ejemplo cambiado a 20601234565 en 03 y en el `.feature` generado. |
| A-11 | El README describía `docker-compose up`, pero no existía ningún archivo de Compose ni Dockerfile. | [docker-compose.yml](../docker-compose.yml), [Dockerfile](../src/Thermal.Api/Dockerfile) y `.dockerignore`. |

Los criterios Gherkin de HU-02 **no** se modificaron: siguen siendo la especificación. Las diferencias con el código son funcionalidad aún no implementada (sesiones, alertas, interfaz), no contradicciones; están en la §5.1.

### 5.3 Estado de implementación por historia (2026-09-26)

Resumen para planificar las siguientes sesiones. El orden, las dependencias y la definición de terminado están en [implementation-plan.md](implementation-plan.md). Al terminar cada entidad se añade aquí su tabla de cobertura (con la forma de la §5.1) y se actualiza esta tabla.

| Historia | Escenarios | Entidades | Ola | Estado |
|---|---|---|---|---|
| HU-01 Registrar empresa cliente y equipo | 8 | `Company`, `Equipment` | 1 | ⚠️ Parcial: 6 completos y 2 parciales (§5.1.1) |
| HU-02 Gestionar tipos de equipo y límite máximo | 19 | `EquipmentType`, `AppSetting` | 0 y 1 | ⚠️ Parcial: 13 completos, 5 parciales y 1 pendiente (§5.1) |
| HU-03 Configurar una sesión de medición | 12 | `MeasurementSession`, `SessionChannel`, `AcquisitionDevice` | 3 y 4 | ⏳ Pendiente |
| HU-04 Advertir la mezcla de termopares T y K | 6 | `MeasurementSession`, `Alert` | 4 | ⏳ Pendiente |
| HU-05 Advertir límite no definido | 4 | `MeasurementSession`, `Alert` | 4 | ⚠️ Parcial: regla del límite pendiente en `TemperatureLimit` (1 escenario) |
| HU-06 Iniciar al llegar datos y capturar cada 2 minutos | 10 | `MeasurementSession`, `Reading`, protocolo serial | 3 y 4 | ⏳ Pendiente |
| HU-07 Registrar lecturas inválidas sin detener la sesión | 7 | `Reading`, `Alert`, `ThermocoupleType` | 1 y 4 | ⏳ Pendiente |
| HU-08 Alertar lecturas fuera de límite | 12 | `Reading`, `Alert` | 4 | ⚠️ Parcial: evaluación estricta del límite en `TemperatureLimit` (evaluación estricta del máximo, del rango y de la banda) |
| HU-09 Pérdida de comunicación y validación del adquisidor | 10 | `CommunicationGap`, `AcquisitionDevice` | 3 y 4 | ⏳ Pendiente |
| HU-10 Cerrar la sesión | 9 | `MeasurementSession` | 4 | ⏳ Pendiente |
| HU-11 Exportar una sesión a Excel | 11 | `SessionExport` | 5 | ⏳ Pendiente |
| HU-12 Consultar el historial de sesiones | 8 | Consultas sobre vistas | 5 | ⏳ Pendiente |
| HU-13 Evaluar, escalar y fallar por pérdida de sensores | 11 | `MeasurementSession`, `Alert` | 4 | ⏳ Pendiente |
| HU-14 Sesión simulada con el set de datos de prueba | 5 | Simulador, todas | 3 y 6 | ⏳ Pendiente |
| HU-15 Priorizar alertas: críticas y advertencias | 4 | `Alert`, SignalR | 4 | ⏳ Pendiente |
| HU-16 Planificar la duración y respetar el descanso | 9 | `MeasurementSession`, `AppSetting` | 1 y 4 | ⏳ Pendiente |
| HU-17 Advertir menos puntos que el mínimo del tipo de equipo | 7 | `MeasurementSession`, `Alert` | 4 | ⏳ Pendiente (puntos mínimos por tipo ya en `EquipmentType`) |
| **Total** | **152** | | | 19 escenarios completos (12 %) |

### 5.4 Cambios por el primer registro real (D-05, D-06 y D-07, 2026-09-25)

El análisis de [DATA-1](data/DATA-1-analisis.md) (cámara ambiental Memmert, 12 termopares tipo T, 72 h) obligó a ajustar la especificación a la realidad. Las decisiones se aplicaron de extremo a extremo: especificación (01, 02, 03, 05), esquema verificado en SQL Server, dominio, contrato v1.1.0, API, pruebas (82/82) y auditoría del contrato (29/29).

| Decisión | Qué cambió | Base |
|---|---|---|
| **D-05** (antes P-19) | Criterio de límite por tipo: `Maximum` (refrigeración) o `Band` (consigna ± tolerancia: incubadoras, cámaras ambientales). Consigna obligatoria en la sesión con banda; lecturas por debajo de la banda (`IsBelowLimit`, alertas `BelowLimit` y `BelowLimitSustained`). | DATA-1: cámara a ~19,4 °C; un máximo no describe su tolerancia |
| **D-06** (antes P-18) | Hasta **27 canales** por sesión (base, protocolo, vista pivote, Excel) y **puntos mínimos por tipo de equipo** (9 por defecto; 27 en incubadoras de más de 50 L). Alcance acotado a equipos de hasta 2000 L, de uso individual, pequeña y mediana escala, hospitales y clínicas. | DATA-1 usó 12 termopares; IEC 60068-3-5, DKD-R 5-7, USP <1079.4>: 9 puntos; DIN 12880: 27 puntos en incubadoras de más de 50 L ([standards/](standards/README.md)) |
| **D-07** | `Range` (mínimo y/o máximo absolutos) reemplaza a `Maximum`: la refrigeración también tiene mínimo (la congelación daña vacunas y sangre). Límites **sugeridos** en el catálogo inicial (`IsLimitSuggested`): refrigeradora y conservadora +2 … +8 °C, incubadora ±1,0 K, cámara ambiental ±2,0 K. Contrato v1.2.0, pruebas 94/94, auditoría 33/33. | OMS PQS E003 (+2 … +8 °C, riesgo de congelación), AABB 5.1.8.1 (+1 … +6 °C; plasma −18 °C o menos) |

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

### 6.1 Diagramas de secuencia (2026-09-25)

Los 5 diagramas de [docs/diagrams/sequence/](diagrams/sequence/) se validaron con `@mermaid-js/mermaid-cli` 12.0.0 y se generó un SVG por diagrama ([svg/](diagrams/sequence/svg/)) con `node docs/diagrams/render-sequence-svg.mjs`. El script falla si algún diagrama tiene un error de sintaxis. Además se revisó cada uno visualmente.

| # | Comprobación | Resultado |
|---|---|---|
| V-09 | Sintaxis de los 5 diagramas (mmdc) | ✅ 5/5 |
| V-10 | Revisión visual y de contenido frente a la especificación | ✅ Tras corregir S-01 a S-07 |

| Id | Hallazgo | Corrección |
|---|---|---|
| S-01 | Diagrama 2: fórmulas con el literal `120` (`(n − 1) × 120 s`, `round(… / 120)`, `(consecutivas − 1) × 120 s`), contra la regla [PC-01]. | Escritas con `intervalo` y los parámetros copiados en la sesión (`SensorLossCriticalAfterSamples`, `SensorLossFailMinutes`, `AboveLimitCriticalMinutes`), con una nota de valores por defecto. |
| S-02 | Diagrama 3: "16 muestras perdidas consecutivas" solo vale con 120 s. | `SensorLossFailMinutes`, con "16 muestras con 120 s" como ejemplo. |
| S-03 | Diagrama 4: "31 muestras válidas", valor derivado del intervalo. | Mínimo expresado con su fórmula (`BaseSessionMinutes × 60 / intervalo + 1`), con 31 como ejemplo. |
| S-04 | Diagrama 4: el texto de la hoja Resumen se salía del dibujo y quedaba cortado. | Texto partido en dos líneas. |
| S-05 | Diagrama 4: faltaban el aviso de menos de 9 puntos (HU-17) y la cancelación de la sesión (HU-10); tampoco estaba el mensaje de confirmación de la interfaz a la sesión. | Añadidos. |
| S-06 | Diagrama 2: rama "si no" del umbral sin texto (`[ ]`) y condición del último `opt` superpuesta a su recuadro. | "No supera el umbral" y condición abreviada. |
| S-07 | Diagrama 5: tras "Iniciar captura" no había mensaje a la sesión ni `START`, y `COMM_RESTORED` aparecía después del bucle, cuando ocurre durante él. Faltaba `DEVICE_REBOOT` (SIM-05). | Inicio como en el diagrama 1, `START`, y los tres eventos del simulador dentro del bucle. |

Al introducir las correcciones, el validador detectó dos errores de sintaxis propios: en Mermaid el `;` separa instrucciones y no se puede usar dentro de un mensaje.

## 7. Mantenimiento

**Al cambiar la especificación:**

1. Cambiar una historia en 03 o un escenario en 06 o en el generador de datos.
2. Si cambió el set de datos: `node test-data/generate-test-data.mjs`.
3. `node docs/diagrams/generate-features.mjs` y revisar que termine sin inconsistencias.
4. Si cambió un diagrama de secuencia: `node docs/diagrams/render-sequence-svg.mjs` (valida y regenera los SVG) y revisarlo visualmente.
5. Actualizar las tablas de las §2 a §4 y el registro de la §6 de este documento.

**Al terminar una entidad** (definición de terminado en [implementation-plan.md §4](implementation-plan.md#4-definición-de-terminado-por-entidad)):

1. Añadir una subsección de cobertura con la forma de la §5.1 (escenario Gherkin → pruebas → estado). Los escenarios sin prueba se marcan como ⏳ o ⚠️ con el motivo.
2. Añadir sus casos a [tools/contract-check/](../tools/contract-check/README.md), ejecutarlo contra la API en Docker y registrar el resultado y las desviaciones corregidas, como en la §5.2.
3. Actualizar la tabla de la §5.3, el total de pruebas de la §5.1 y la §1 de [implementation-plan.md](implementation-plan.md).
