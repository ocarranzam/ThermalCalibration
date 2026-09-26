# Manual de usuario

| Campo | Valor |
|---|---|
| Sistema | ThermalCalibration |
| Versión del sistema | [COMPLETAR] |
| Lectores | Técnicos de calibración y supervisores |
| Estado | **Plantilla.** Las capturas y los pasos dependen de la interfaz de usuario, que aún no está definida (c4 §3.3) |

> Guía de redacción: frases cortas, en segunda persona ("pulsa", "elige"), sin términos técnicos (nada de API, base de datos ni JSON). Cada procedimiento con pasos numerados, una captura y el resultado esperado. Los nombres de botones y mensajes, exactamente como aparecen en pantalla (los mensajes salen de las historias de usuario).

## Índice

1. Bienvenida y para qué sirve el sistema
2. Antes de empezar
3. Conceptos básicos
4. Primeros pasos
5. Flujos principales
6. Alertas: qué significan y qué hacer
7. Historial y exportación a Excel
8. Sesiones de práctica (simulación)
9. Resolución de problemas y preguntas frecuentes (FAQ)
10. Glosario
11. Ayuda y soporte

---

## 1. Bienvenida y para qué sirve el sistema

**Contenido:** en un párrafo, qué hace el sistema por el técnico: registra automáticamente la temperatura de los sensores cada 2 minutos, avisa de los problemas y genera el Excel sin transcribir a mano. Qué **no** hace en esta fase: certificados ni análisis estadístico.
**Estado:** ✅

## 2. Antes de empezar

**Contenido:** qué necesitas (usuario y contraseña, la PC del laboratorio encendida, el adquisidor conectado por USB, los termopares instalados), y buenas prácticas antes de medir: 9 puntos en las 8 esquinas y el centro, un solo tipo de termopar por sesión.
**Estado:** ✅ (contenido) / ⏳ (capturas)

## 3. Conceptos básicos

**Contenido:** explicación sencilla, con un dibujo si ayuda, de: sesión de medición, sensor o canal, muestra (una lectura de todos los sensores cada 2 min), límite máximo, sesión completa, incompleta y fallida, y descanso del equipo de medición (15 min).
**Fuente:** [01 §7 Glosario](../specs/functional/01-vision-document.md#7-glosario), traducido a lenguaje sencillo. **Estado:** ✅

## 4. Primeros pasos

### 4.1 Iniciar sesión en el sistema

[CAPTURA 1: pantalla de inicio de sesión]

### 4.2 La pantalla principal

[CAPTURA 2: pantalla principal con sus partes señaladas]

**Contenido:** para qué sirve cada parte del menú y cómo volver al inicio.
**Estado:** ⏳ (depende de la interfaz y de la autenticación)

## 5. Flujos principales

Cada flujo: cuándo se usa, pasos numerados, capturas y resultado esperado.

### 5.1 Registrar una empresa cliente y sus equipos (HU-01)

[CAPTURA 3: formulario de empresa] [CAPTURA 4: formulario de equipo]

### 5.2 Configurar una sesión de medición (HU-03, HU-16)

**Contenido:** elegir el equipo, el puerto y los canales con su tipo y su ubicación; duración planificada (1 h, la del tipo de equipo o la que pida el cliente, con su referencia).
[CAPTURA 5: configuración de canales]

### 5.3 Iniciar la captura y confirmar las advertencias (HU-04, HU-05, HU-17)

**Contenido:** qué hacer ante "menos de 9 puntos", "mezcla de termopares T y K" y "límite no definido"; por qué hay que confirmar; qué significa "Esperando datos".
[CAPTURA 6: diálogo de advertencias combinadas]

### 5.4 Monitorear una sesión en curso (HU-06, HU-13)

**Contenido:** leer los valores por sensor, los colores (rojo = fuera de límite, gris = sensor con falla), los contadores de muestras válidas y afectadas, y el tiempo que falta.
[CAPTURA 7: pantalla de monitoreo]

### 5.5 Extender o finalizar una sesión (HU-10, HU-16)

**Contenido:** cierre automático al cumplirse la duración; cómo extenderla; qué pasa si la finalizas antes (queda incompleta); cancelar una sesión.
[CAPTURA 8: aviso de cierre anticipado]

### 5.6 Si se desconecta el adquisidor (HU-09)

**Contenido:** qué verás ("Sin comunicación… Reintentando"), qué revisar (cable USB), cuándo la sesión se da por fallida (30 min o si se conecta otro adquisidor).

**Estado de la sección 5:** ⏳ (contenido funcional ✅ en las historias; pasos y capturas dependen de la interfaz)

## 6. Alertas: qué significan y qué hacer

**Contenido:** tabla con cada alerta en lenguaje sencillo: cómo se ve (aviso destacado para las **críticas**, solo contador para las advertencias), qué significa y qué hacer. Incluye cómo **reconocer** una alerta crítica y dejar una nota (supervisor), y la diferencia entre "posible falla del sensor" y "posible falla del equipo".
**Fuente:** [01 §6.3 y §6.4](../specs/functional/01-vision-document.md#63-clasificación-de-alertas), HU-08 y HU-15. **Estado:** ✅ (contenido) / ⏳ (capturas)

## 7. Historial y exportación a Excel

### 7.1 Buscar sesiones anteriores (HU-12)

### 7.2 Exportar una sesión a Excel (HU-11)

**Contenido:** qué sesiones se pueden exportar, dónde se guarda el archivo y cómo leerlo: hojas Resumen, Lecturas, Alertas y Comunicación; significado de los colores y de los avisos ("SESIÓN INCOMPLETA", "SESIÓN FALLIDA", "DATOS SIMULADOS").
[CAPTURA 9: hoja Resumen] [CAPTURA 10: hoja Lecturas con colores]
**Fuente:** [03 §Formato del Excel](../specs/functional/03-user-stories.md#formato-del-excel-exportado). **Estado:** ⏳

## 8. Sesiones de práctica (simulación)

**Contenido:** para qué sirven (capacitación sin sensores), cómo se reconocen ("SIMULACIÓN") y aviso de que nunca valen para una calibración.
**Estado:** ⏳ (HU-14)

## 9. Resolución de problemas y preguntas frecuentes (FAQ)

**Contenido:** formato de pregunta, causa probable y qué hacer. Lista inicial, a partir de los mensajes de las historias:

| Situación o mensaje | Qué hacer |
|---|---|
| "No se detectó un adquisidor en COM…" | [COMPLETAR: revisar cable, puerto, reintentar] |
| "El adquisidor descansa hasta las …" | [COMPLETAR: esperar o pedir autorización a un supervisor] |
| "COM… está en uso por la sesión …" | [COMPLETAR] |
| Un sensor aparece como "ABIERTO" o "CORTO" | [COMPLETAR] |
| "Pérdida de sensores sin recuperar…" | [COMPLETAR] |
| La sesión quedó "INCOMPLETA" | [COMPLETAR: faltaron muestras válidas o se cerró antes; extender la próxima vez] |
| La sesión quedó "FALLIDA" | [COMPLETAR] |
| No puedo crear ni editar tipos de equipo | Solo el administrador puede hacerlo |
| "Ya existe el tipo de equipo …" / "Ya existe una empresa con el RUC …" | [COMPLETAR] |
| No puedo exportar una sesión | [COMPLETAR: debe estar cerrada; las canceladas no se exportan] |

**Estado:** ⏳ (se completa con los mensajes reales de la interfaz)

## 10. Glosario

**Contenido:** los términos de la sección 3, en orden alfabético, en una línea cada uno.

## 11. Ayuda y soporte

**Contenido:** a quién acudir [COMPLETAR], qué datos dar (número de sesión, hora y captura del mensaje).
