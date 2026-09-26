# Manual de administrador

| Campo | Valor |
|---|---|
| Sistema | ThermalCalibration |
| Versión del sistema | [COMPLETAR] |
| Lectores | Administrador del sistema (rol `Admin`) y soporte de TI |
| Estado | **Plantilla** |

## Índice

1. Introducción y responsabilidades del administrador
2. Roles y permisos
3. Gestión de usuarios
4. Catálogos maestros
5. Configuración global (parámetros del sistema)
6. Adquisidores y equipo de medición
7. Supervisión de sesiones y datos
8. Revisión de logs y auditoría
9. Tareas periódicas de mantenimiento
10. Resolución de incidencias
11. Anexos

---

## 1. Introducción y responsabilidades del administrador

**Contenido:** qué administra (usuarios, catálogos, parámetros, adquisidores) y qué no le corresponde (infraestructura, que va en el manual de despliegue). Principio clave: los parámetros y los límites se **copian en cada sesión al iniciarla**, así que un cambio nunca altera las sesiones ya registradas (RN-08).
**Estado:** ✅

## 2. Roles y permisos

### 2.1 Roles del sistema

**Contenido:** descripción de `Admin`, `Technician` y `Supervisor` y para qué perfil es cada uno.
**Fuente:** [01 §4](../specs/functional/01-vision-document.md#4-perfiles-de-usuario), `CK_AppUser_Role`. **Estado:** ✅

### 2.2 Matriz de permisos

**Contenido:** tabla acción × rol. Base inicial, a completar con cada entidad:

| Acción | Admin | Supervisor | Technician |
|---|---|---|---|
| Consultar tipos de equipo | ✅ | ✅ | ✅ |
| Crear o editar tipos de equipo y límites | ✅ | ❌ | ❌ |
| Editar parámetros globales | ✅ | ❌ | ❌ |
| Consultar empresas, equipos y parámetros | ✅ | ✅ | ✅ |
| Registrar o editar empresas y equipos | ✅ | ❌ | ✅ |
| Configurar, iniciar y cerrar sesiones | [COMPLETAR] | ✅ (cerrar) | ✅ |
| Autorizar un inicio durante el descanso | ✅ | ✅ | ❌ |
| Reconocer alertas críticas | [COMPLETAR] | ✅ | [COMPLETAR] |
| Consultar el historial y exportar a Excel | ✅ | ✅ | ✅ |

**Fuente:** historias (HU-02, HU-16, HU-08) y RA-02 de [05 §4](../specs/functional/05-data-model.md#4-reglas-de-negocio). **Estado:** ⚠️ Parcial (implementado: tipos de equipo, parámetros, empresas y equipos)

## 3. Gestión de usuarios

**Contenido:** alta, cambio de rol, desactivación (no se borran usuarios con sesiones registradas), restablecimiento de acceso.
**Estado:** 🔒 Depende de la decisión de autenticación (cuentas propias o Windows/AD)

## 4. Catálogos maestros

### 4.1 Tipos de equipo y límite máximo (HU-02)

**Contenido:** crear un tipo; definir o dejar pendiente el límite (2 decimales como máximo; fuera de límite si la lectura es **estrictamente mayor**); duración mínima de sesión (60 a 43 200 min); desactivar en lugar de borrar; qué pasa con las sesiones en curso al cambiar un límite; conflicto por edición simultánea ("El recurso cambió": volver a cargar y repetir).
[CAPTURA 1: formulario de tipo de equipo]
**Estado:** ✅ (funcionalidad implementada) / ⏳ (capturas)

### 4.2 Tipos de termopar

**Contenido:** catálogo fijo T/K con su rango físico; solo consulta (`GET /api/v1/thermocouple-types`).
**Estado:** ✅ (funcionalidad implementada, sprint 1)

## 5. Configuración global (parámetros del sistema)

**Contenido:** tabla de cada parámetro de `AppSetting` en lenguaje de negocio: significado, valor por defecto, rango permitido, efecto y la advertencia de que solo afecta a las sesiones nuevas:

| Parámetro | Por defecto | Rango | Efecto |
|---|---|---|---|
| Intervalo de muestreo **[PC-01]** | 120 s | 30 a 900 s, y debe dividir exactamente la duración base | Punto de cambio: con 60 s cambian las muestras mínimas (31 → 61); la API las informa en `minValidSamples` |
| Umbral de pérdida de sensores | 60 % | Mayor que 0 y menor que 100, con 2 decimales como máximo | Muestra afectada si se supera |
| Muestras para escalar a crítica | 3 | 2 a 10 | |
| Minutos para declarar la sesión fallida | 30 | 10 a 240 | |
| Minutos de fuera de límite sostenido | 30 | 10 a 240 | |
| Descanso del kit de medición | 15 min | 0 a 240 min (0 lo desactiva) | |
| Duración base y máxima planificable | 60 min / 7 días | Base 30 a 240 min; máxima desde la base hasta 43 200 min (30 días) | |

**Fuente:** [01 §6 y §12](../specs/functional/01-vision-document.md#6-reglas-de-negocio-principales), `AppSetting` en [01-schema.sql](../db/01-schema.sql), rangos de la decisión D-08 (01 §10). El mínimo de puntos de medición ya no es un parámetro global: lo fija cada tipo de equipo (D-06, §4.1). Edición con `PUT /api/v1/settings` (solo `Admin`, con `If-Match`: si otro administrador guardó antes, "El recurso cambió"). **Estado:** ✅ (funcionalidad implementada, sprint 1) / ⏳ (capturas)

## 6. Adquisidores y equipo de medición

**Contenido:** consulta de los adquisidores registrados automáticamente, significado del grupo de sensores, descanso entre sesiones y autorización de un inicio anticipado.
**Estado:** ⏳ (ola 3)

## 7. Supervisión de sesiones y datos

**Contenido:** sesiones en curso por PC y puerto, sesiones fallidas y sus motivos, alertas críticas sin reconocer al cierre, e inmutabilidad: lecturas, alertas y huecos no se editan ni se borran (RN-13); las alertas solo admiten reconocimiento.
**Estado:** ⏳ (ola 4)

## 8. Revisión de logs y auditoría

### 8.1 Dónde están los logs

**Contenido:** destino de los logs de la aplicación [COMPLETAR: archivo, Visor de eventos de Windows o sistema centralizado], rotación y retención.

### 8.2 Qué buscar

**Contenido:** tabla de eventos relevantes (inicio y fin de sesión, pérdida y recuperación de comunicación, sesión fallida, errores de base de datos, accesos denegados 401/403), con su nivel y qué hacer. Cómo correlacionar un error que ve el usuario con el log mediante el `traceId` de la respuesta de error.

### 8.3 Auditoría de datos

**Contenido:** qué queda registrado en la base como evidencia: quién reconoció cada alerta y cuándo, quién autorizó un inicio anticipado, exportaciones (`SessionExport`), fecha de última edición de los catálogos.
**Estado de la sección 8:** ⏳ (falta definir la configuración de logs de producción)

## 9. Tareas periódicas de mantenimiento

**Contenido:** calendario (diario: revisar alertas críticas pendientes; semanal: sesiones fallidas; mensual: espacio en disco y respaldos; anual: revisar las ediciones vigentes de las normas).
**Estado:** ⏳

## 10. Resolución de incidencias

**Contenido:** tabla de síntoma, causa probable y acción, con escalamiento a TI cuando corresponde (servicio detenido, base inaccesible, puerto COM ocupado).
**Estado:** ⏳

## 11. Anexos

- A. Valores iniciales de los catálogos y parámetros.
- B. Referencia rápida de permisos.
- C. Contactos de soporte [COMPLETAR].
