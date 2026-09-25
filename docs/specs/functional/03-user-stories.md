# 03 · Historias de usuario y formato del Excel

Módulos: **Sesión de Medición** (SM), **Adquisición Serial** (AS) y **Exportación a Excel** (EX).

| Campo | Valor |
|---|---|
| Versión | 0.2 (borrador para revisión) |
| Cambios en 0.2 | Muestra 1 en t = 0 y 721 muestras en 24 h. Inicio al llegar la primera muestra. Umbral de pérdida de sensores del 60 % (HU-13). Sesión no válida por cambio de adquisidor o de grupo de sensores (HU-09). Sesión simulada con el set de datos de prueba (HU-14). Ajustes al Excel. |
| Relacionado | [01-vision-document.md](01-vision-document.md), [02-serial-protocol.md](02-serial-protocol.md), [05-data-model.md](05-data-model.md), [06-test-data.md](06-test-data.md) |

Convenciones:

- Las historias usan Gherkin con palabras clave en inglés (`Feature`, `Background`, `Scenario`, `Scenario Outline`, `Given`, `When`, `Then`, `And`, `But`) y el texto en español.
- Las temperaturas se escriben con coma decimal, como las ve el usuario. En el protocolo serial se transmiten con punto.
- Numeración de muestras: la muestra 1 es la primera recibida (t = 0) y la muestra *n* corresponde a t = (n − 1) × 2 min. Con inicio a las 08:00:00, la muestra 16 es a las 08:30:00 y la 31 a las 09:00:00.
- `RN-xx` remite a las reglas de negocio de [01-vision-document.md §6](01-vision-document.md#6-reglas-de-negocio-principales).
- Los nombres entre comillas invertidas (`Status`, `AlertType`...) son columnas o valores de [05-data-model.md](05-data-model.md).
- La columna **Escenarios de prueba** remite a los casos `TD-xx` de [06-test-data.md](06-test-data.md) que ejercitan cada historia sin hardware.

## Índice

| Id | Historia | Módulo | Perfil | Escenarios de prueba |
|---|---|---|---|---|
| [HU-01](#hu-01--registrar-empresa-cliente-y-equipo) | Registrar empresa cliente y equipo | SM | Técnico, Admin | — |
| [HU-02](#hu-02--gestionar-tipos-de-equipo-y-límite-máximo) | Gestionar tipos de equipo y límite máximo | SM | Admin | TD-14 |
| [HU-03](#hu-03--configurar-una-sesión-de-medición) | Configurar una sesión de medición | SM, AS | Técnico | TD-01, TD-11 |
| [HU-04](#hu-04--advertir-la-mezcla-de-termopares-t-y-k) | Advertir la mezcla de termopares T y K | SM | Técnico | TD-10 |
| [HU-05](#hu-05--advertir-límite-no-definido) | Advertir límite no definido | SM | Técnico | TD-14 |
| [HU-06](#hu-06--iniciar-al-llegar-datos-y-capturar-cada-2-minutos) | Iniciar al llegar datos y capturar cada 2 minutos | AS | Sistema | TD-01, TD-11, TD-12 |
| [HU-07](#hu-07--registrar-lecturas-inválidas-sin-detener-la-sesión) | Registrar lecturas inválidas sin detener la sesión | AS | Sistema | TD-09, TD-15 |
| [HU-08](#hu-08--alertar-lecturas-fuera-de-límite) | Alertar lecturas fuera de límite | SM | Sistema | TD-02, TD-15 |
| [HU-09](#hu-09--detectar-pérdida-de-comunicación-reconectar-y-validar-el-adquisidor) | Detectar pérdida de comunicación, reconectar y validar el adquisidor | AS | Sistema | TD-06, TD-07, TD-08 |
| [HU-10](#hu-10--cerrar-la-sesión-duración-mínima-y-máxima) | Cerrar la sesión (duración mínima y máxima) | SM | Técnico, Sistema | TD-05, TD-10, TD-12, TD-13 |
| [HU-11](#hu-11--exportar-una-sesión-a-excel) | Exportar una sesión a Excel | EX | Técnico, Supervisor | Todos |
| [HU-12](#hu-12--consultar-el-historial-de-sesiones) | Consultar el historial de sesiones | SM | Todos | — |
| [HU-13](#hu-13--evaluar-la-pérdida-de-sensores-por-muestra) | Evaluar la pérdida de sensores por muestra | SM | Sistema | TD-03, TD-04, TD-05, TD-15 |
| [HU-14](#hu-14--ejecutar-una-sesión-simulada-con-el-set-de-datos-de-prueba) | Ejecutar una sesión simulada con el set de datos de prueba | AS | Técnico, Admin | Todos |

---

## HU-01 · Registrar empresa cliente y equipo

**Como** técnico de calibración **quiero** registrar la empresa cliente (RUC) y sus equipos **para** asociar cada sesión de medición a un equipo identificable y mantener su historial.

Reglas: el RUC es único (`UQ_Company_TaxId`). El número de serie es único dentro de la empresa (`UQ_Equipment_Company_Serial`). El tipo de equipo es obligatorio y se elige del catálogo de tipos activos. Validación del RUC peruano en la aplicación: 11 dígitos, prefijo 10, 15, 17 o 20, y dígito verificador módulo 11.

```gherkin
Feature: Registro de empresas cliente y equipos

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And existe el tipo de equipo "Congeladora" activo

  Scenario: Registrar una empresa nueva
    When registro la empresa con RUC "20100070970" y razón social "Laboratorios Andinos S.A.C."
    Then la empresa queda registrada y activa
    And aparece en el buscador de empresas por RUC y por razón social

  Scenario: Rechazar un RUC duplicado
    Given existe la empresa con RUC "20100070970"
    When intento registrar otra empresa con RUC "20100070970"
    Then el sistema rechaza el registro con el mensaje "Ya existe una empresa con el RUC 20100070970"
    And me ofrece abrir la empresa existente

  Scenario Outline: Rechazar un RUC con formato inválido
    When intento registrar una empresa con RUC "<ruc>"
    Then el sistema rechaza el registro con el mensaje "<mensaje>"

    Examples:
      | ruc          | mensaje                                        |
      | 2010007097   | El RUC debe tener 11 dígitos                   |
      | 2010007097A  | El RUC solo admite dígitos                     |
      | 30100070970  | El RUC debe empezar con 10, 15, 17 o 20        |
      | 20100070971  | El dígito verificador del RUC no es válido     |

  Scenario: Registrar un equipo asociado a la empresa
    Given existe la empresa con RUC "20100070970"
    When registro un equipo para esa empresa con:
      | campo           | valor        |
      | Tipo de equipo  | Congeladora  |
      | Marca           | Haier        |
      | Modelo          | HBF-205      |
      | Número de serie | SN-88231     |
    Then el equipo queda registrado y asociado a la empresa
    And su historial de sesiones está vacío

  Scenario: Rechazar un número de serie duplicado en la misma empresa
    Given la empresa con RUC "20100070970" tiene un equipo con serie "SN-88231"
    When intento registrar otro equipo de esa empresa con serie "SN-88231"
    Then el sistema rechaza el registro con el mensaje "La empresa ya tiene un equipo con la serie SN-88231"

  Scenario: Permitir la misma serie en otra empresa
    Given la empresa con RUC "20100070970" tiene un equipo con serie "SN-88231"
    And existe la empresa con RUC "20601234561"
    When registro para la empresa "20601234561" un equipo con serie "SN-88231"
    Then el equipo queda registrado

  Scenario: Datos obligatorios del equipo
    When intento registrar un equipo sin número de serie o sin tipo de equipo
    Then el sistema no guarda el equipo e indica los campos obligatorios faltantes
```

---

## HU-02 · Gestionar tipos de equipo y límite máximo

**Como** administrador **quiero** mantener el catálogo de tipos de equipo con su temperatura máxima admisible, o dejarla pendiente, **para** que las sesiones evalúen el límite correcto sin alterar las mediciones ya registradas.

Reglas: RN-06, RN-07, RN-08. Solo el rol `Admin` crea o edita tipos. Un tipo con equipos asociados no se borra: se desactiva (`IsActive` = 0). El administrador también mantiene el umbral de pérdida de sensores (60 % por defecto), que se copia en cada sesión al iniciarla.

```gherkin
Feature: Catálogo de tipos de equipo con límite máximo

  Background:
    Given que he iniciado sesión con el rol "Admin"

  Scenario: Registrar un tipo de equipo con límite definido
    When registro el tipo de equipo "Ultracongeladora" con límite máximo "-60,0" °C
    Then el tipo queda activo con límite máximo -60,00 °C

  Scenario: Registrar un tipo de equipo con límite pendiente
    When registro el tipo de equipo "Cámara de vacunas" sin límite máximo
    Then el tipo queda activo con el límite en estado "Pendiente"
    And el catálogo lo muestra con la etiqueta "Límite pendiente de definir"

  Scenario: Rechazar un nombre de tipo duplicado
    Given existe el tipo de equipo "Congeladora"
    When intento registrar el tipo de equipo "Congeladora"
    Then el sistema rechaza el registro con el mensaje "Ya existe el tipo de equipo Congeladora"

  Scenario: Rechazar un límite con formato inválido
    When intento registrar el límite máximo "-5,123" para "Congeladora"
    Then el sistema rechaza el valor con el mensaje "El límite admite como máximo 2 decimales"

  Scenario: Editar el límite sin afectar sesiones registradas
    Given el tipo "Congeladora" tiene límite máximo -5,00 °C
    And la sesión 101 de una congeladora se inició con ese límite
    When cambio el límite máximo de "Congeladora" a -8,00 °C
    Then las sesiones que se inicien desde ahora aplican -8,00 °C
    And la sesión 101 conserva el límite aplicado -5,00 °C
    And las lecturas y alertas de la sesión 101 no cambian

  Scenario: Editar el límite de una sesión en curso no la afecta
    Given la sesión 102 de una congeladora está en curso con límite aplicado -5,00 °C
    When cambio el límite máximo de "Congeladora" a -8,00 °C
    Then la sesión 102 sigue evaluando las lecturas contra -5,00 °C

  Scenario: Definir un límite que estaba pendiente
    Given el tipo "Refrigeradora" tiene el límite pendiente
    And la sesión 103 de una refrigeradora se capturó sin evaluar límite
    When defino el límite máximo de "Refrigeradora" en 8,00 °C
    Then la sesión 103 sigue sin límite aplicado y sin alertas de límite

  Scenario: Cambiar el umbral de pérdida de sensores
    Given el umbral de pérdida de sensores es 60 %
    And la sesión 104 se inició con ese umbral
    When cambio el umbral a 50 %
    Then las sesiones que se inicien desde ahora aplican 50 %
    And la sesión 104 conserva el umbral 60 % y sus muestras afectadas no cambian

  Scenario Outline: Rechazar un umbral fuera de rango
    When intento fijar el umbral de pérdida de sensores en <valor> %
    Then el sistema rechaza el valor con el mensaje "El umbral debe ser mayor que 0 y menor que 100"

    Examples:
      | valor |
      | 0     |
      | 100   |

  Scenario: Un técnico no puede editar límites
    Given que he iniciado sesión con el rol "Technician"
    When intento editar el límite máximo de "Congeladora"
    Then el sistema me deniega la acción por falta de permisos

  Scenario: Desactivar un tipo con equipos asociados
    Given el tipo "Conservadora" tiene equipos registrados
    When intento eliminar el tipo "Conservadora"
    Then el sistema no lo elimina y me ofrece desactivarlo
    And un tipo desactivado no se ofrece al registrar equipos nuevos
```

---

## HU-03 · Configurar una sesión de medición

**Como** técnico de calibración **quiero** configurar la sesión eligiendo el equipo, el puerto COM, el adquisidor detectado y de 1 a 10 canales con su tipo de termopar y su ubicación **para** iniciar una captura trazable.

Reglas: RN-01. La sesión se crea con `Status` = `Configured`. El técnico responsable es el usuario autenticado. Un puerto COM no puede estar en dos sesiones `Running` a la vez (supuesto S-01). La aplicación exige un adquisidor detectado para iniciar, aunque `AcquisitionDeviceId` admita NULL mientras la sesión está en configuración.

```gherkin
Feature: Configuración de la sesión de medición

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And existe el equipo "SN-88231" tipo "Congeladora" de la empresa "20100070970"

  Scenario: Configurar una sesión válida con 5 canales tipo T
    Given selecciono el equipo "SN-88231"
    And selecciono el puerto "COM3" de la lista de puertos disponibles
    When el sistema detecta en "COM3" el adquisidor "ADQ-ARD-0001" con 10 canales, modo "POLL" y grupo de sensores "GRP-A"
    And asigno los canales:
      | canal | tipo | ubicación |
      | 1     | T    | Superior  |
      | 2     | T    | Centro    |
      | 3     | T    | Inferior  |
      | 4     | T    | Puerta    |
      | 5     | T    | Fondo     |
    And guardo la configuración
    Then la sesión queda en estado "Configured"
    And registra el equipo, mi usuario como técnico, el puerto "COM3", el adquisidor "ADQ-ARD-0001" y el grupo de sensores "GRP-A"
    And el botón "Iniciar captura" queda habilitado

  Scenario: Registrar automáticamente un adquisidor nuevo
    Given el adquisidor "ADQ-RPI-0007" no está registrado
    When el sistema lo detecta en "COM5" con plataforma "RaspberryPi", firmware "0.9.1", 8 canales y modo "STREAM"
    Then el adquisidor se registra en el catálogo con esos datos
    And solo puedo asignar los canales del 1 al 8

  Scenario: Vista previa de un adquisidor que ya transmite
    Given el adquisidor en "COM5" está en modo "STREAM" y ya transmite bloques
    When configuro la sesión
    Then la pantalla muestra los últimos valores recibidos por canal como "Vista previa"
    And esos valores no se almacenan como lecturas de la sesión

  Scenario: Adquisidor sin grupo de sensores informado
    When el adquisidor detectado no informa grupo de sensores
    Then la sesión se configura sin grupo de sensores
    And el sistema indica "El cambio de grupo de sensores se verificará por los tipos informados por canal"

  Scenario: No se detecta ningún adquisidor en el puerto
    Given selecciono el puerto "COM4"
    When el adquisidor no responde a "IDN" dentro de 2 segundos tras el arranque
    Then el sistema muestra "No se detectó un adquisidor en COM4. Verifique la conexión y el cable USB."
    And el botón "Iniciar captura" permanece deshabilitado
    And puedo reintentar la detección o elegir otro puerto

  Scenario: El puerto está ocupado por otra sesión en curso
    Given la sesión 110 está en curso en el puerto "COM3"
    When selecciono el puerto "COM3" para una nueva sesión
    Then el sistema muestra "COM3 está en uso por la sesión 110"
    And no permite continuar con ese puerto

  Scenario: Versión de protocolo incompatible
    When el adquisidor en "COM3" responde con versión de protocolo "2.0"
    Then el sistema muestra "El adquisidor usa el protocolo 2.0, incompatible con esta aplicación (1.x)"
    And no permite iniciar la captura

  Scenario Outline: Validar la cantidad y numeración de canales
    Given el adquisidor detectado tiene <disponibles> canales
    When intento guardar una configuración con <activos> canales activos, el mayor número de canal es <max>
    Then el sistema <resultado>

    Examples:
      | disponibles | activos | max | resultado                                                          |
      | 10          | 0       | 0   | rechaza con "Debe activar al menos 1 canal"                        |
      | 10          | 1       | 1   | acepta la configuración                                            |
      | 10          | 10      | 10  | acepta la configuración                                            |
      | 8           | 3       | 9   | rechaza con "El adquisidor solo tiene 8 canales"                   |

  Scenario: Cada canal activo requiere tipo y ubicación
    When activo el canal 2 sin indicar tipo de termopar o sin ubicación
    Then el sistema no guarda la configuración y resalta el canal 2

  Scenario: Canal duplicado
    When intento asignar el canal 3 dos veces
    Then el sistema rechaza la configuración con "El canal 3 ya está asignado"

  Scenario: Advertencia de tipo informado distinto en la lectura de prueba
    Given asigné el canal 3 como tipo "T"
    When ejecuto la lectura de prueba y el adquisidor informa tipo "K" en el canal 3
    Then el sistema muestra en el canal 3 "El adquisidor informa tipo K; usted declaró T"
    But me permite iniciar la captura
```

---

## HU-04 · Advertir la mezcla de termopares T y K

**Como** técnico de calibración **quiero** que el sistema me advierta si mezclo termopares T y K **para** evitar una mala práctica o, si es inevitable, dejar constancia de que la acepté.

Reglas: RN-05. `MeasurementSession.HasMixedThermocoupleTypes` = 1. Alerta `MixedThermocoupleTypes` con severidad `Warning`, sin canal. La confirmación rellena `MixedTypesAcknowledgedAt` y `Alert.AcknowledgedById`/`AcknowledgedAt`. La base impide pasar a `Running` sin confirmación (`CK_MeasurementSession_MixedAck`). La guía de la OMS para mapeo de temperatura también recomienda usar un solo tipo de dispositivo por estudio.

```gherkin
Feature: Advertencia por mezcla de tipos de termopar

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And tengo una sesión en estado "Configured" con adquisidor detectado

  Scenario: Sesión sin mezcla no muestra advertencia
    Given todos los canales activos son tipo "T"
    When pulso "Iniciar captura"
    Then la sesión pasa a "Esperando datos" sin advertencia de mezcla
    And la sesión queda con "Mezcla de termopares" = No

  Scenario: Detectar la mezcla y exigir confirmación
    Given los canales 1 a 4 son tipo "T" y el canal 5 es tipo "K"
    When pulso "Iniciar captura"
    Then el sistema muestra la advertencia:
      """
      La sesión mezcla termopares tipo T (canales 1, 2, 3, 4) y tipo K (canal 5).
      Por buenas prácticas, todos los termopares de una sesión deberían ser del mismo tipo.
      Si continúa, la mezcla quedará registrada en la sesión y en el Excel exportado.
      """
    And registra una alerta "MixedThermocoupleTypes" con severidad "Warning"
    And marca la sesión con "Mezcla de termopares" = Sí
    And la captura no inicia hasta que yo confirme

  Scenario: Confirmar la advertencia e iniciar
    Given se mostró la advertencia de mezcla
    When marco "Entiendo que no es una buena práctica" y pulso "Confirmar e iniciar"
    Then la alerta queda reconocida por mí con fecha y hora
    And la sesión registra la fecha y hora de la confirmación
    And la sesión pasa a "Esperando datos" y luego a "Running" con la primera muestra recibida

  Scenario: Cancelar la advertencia para corregir los canales
    Given se mostró la advertencia de mezcla
    When pulso "Volver y corregir"
    Then la sesión permanece en estado "Configured"
    And la alerta de mezcla queda registrada sin reconocer

  Scenario: Corregir la mezcla después de la advertencia
    Given se registró una alerta de mezcla sin reconocer
    When cambio el canal 5 a tipo "T" y pulso "Iniciar captura"
    Then la sesión inicia sin pedir confirmación
    And la sesión queda con "Mezcla de termopares" = No
    And la alerta de mezcla anterior se conserva en el historial de la sesión

  Scenario: La base impide iniciar una sesión mezclada sin confirmación
    Given una sesión marcada con mezcla y sin fecha de confirmación
    When cualquier proceso intenta cambiar su estado a "Running"
    Then la base de datos rechaza el cambio
```

---

## HU-05 · Advertir límite no definido

**Como** técnico de calibración **quiero** saber que el tipo de equipo no tiene límite máximo definido **para** entender que la sesión se capturará sin evaluar ese límite.

Reglas: RN-09. `MeasurementSession.MaxTemperatureC` = NULL. Alerta `LimitNotDefined`, severidad `Info`.

```gherkin
Feature: Advertencia de límite máximo no definido

  Scenario: Iniciar una sesión sin límite definido
    Given el tipo "Refrigeradora" tiene el límite pendiente
    And tengo configurada una sesión para un equipo tipo "Refrigeradora"
    When pulso "Iniciar captura"
    Then el sistema muestra "El tipo de equipo Refrigeradora no tiene límite máximo definido. Se capturarán las lecturas sin evaluar límite."
    And al pulsar "Continuar" la sesión pasa a "Esperando datos" con límite aplicado vacío
    And se registra una alerta "LimitNotDefined" con severidad "Info" reconocida por mí

  Scenario: Las lecturas no se marcan fuera de límite
    Given una sesión en curso con límite aplicado vacío
    When se recibe una lectura de 25,00 °C
    Then la lectura se almacena con "fuera de límite" = No
    And no se genera alerta "AboveLimit"

  Scenario: Advertencias combinadas
    Given la sesión mezcla tipos T y K y el tipo de equipo no tiene límite definido
    When pulso "Iniciar captura"
    Then el sistema muestra ambas advertencias en un mismo diálogo
    And exige la confirmación explícita de la mezcla antes de iniciar
```

---

## HU-06 · Iniciar al llegar datos y capturar cada 2 minutos

**Como** laboratorio **quiero** que la sesión empiece con la primera muestra que llega del adquisidor, y que desde ahí se capture una lectura por canal cada 2 minutos, almacenada con su marca de tiempo y su trama original, **para** disponer de datos crudos confiables y auditables aunque el adquisidor ya estuviera midiendo antes de conectarse.

Reglas: RN-02. [02-serial-protocol.md §5 y §6.4](02-serial-protocol.md#64-inicio-de-la-sesión-cuando-llegan-datos).

```gherkin
Feature: Inicio por llegada de datos y captura periódica

  Background:
    Given una sesión configurada con los canales 1 a 5 activos, todos tipo "T"
    And el límite aplicado será -5,00 °C

  Scenario: La sesión inicia con la primera muestra recibida (modo POLL)
    When pulso "Iniciar captura" a las 07:59:40
    And el adquisidor responde a la primera solicitud con tramas válidas a las 08:00:00
    Then la sesión pasa a "Running" con inicio a las 08:00:00
    And esas lecturas se almacenan como muestra 1

  Scenario: Esperar datos sin consumir números de muestra
    When pulso "Iniciar captura" y el adquisidor no devuelve tramas válidas
    Then la sesión se muestra como "Esperando datos" y sigue en estado "Configured"
    And el sistema reintenta cada 10 segundos sin crear lecturas
    And si pasan 5 minutos sin datos, me avisa y me permite seguir esperando o cancelar

  Scenario: Descartar datos generados antes de iniciar (modo STREAM)
    Given el adquisidor en modo "STREAM" transmitía bloques antes de que la PC abriera el puerto
    When la PC abre el puerto y recibe una línea incompleta y dos bloques completos
    And pulso "Iniciar captura" a las 07:59:00
    And llega el siguiente bloque completo a las 08:00:00
    Then la línea incompleta y los dos bloques previos se descartan sin crear lecturas
    And la sesión inicia a las 08:00:00 con ese bloque como muestra 1

  Scenario: Programar las muestras desde el inicio
    Given la sesión inició a las "2026-10-01 08:00:00 -05:00"
    Then la muestra 2 se programa a las 08:02:00
    And la muestra 31 se programa a las 09:00:00
    And la muestra 721 se programa a las 08:00:00 del día siguiente

  Scenario: Almacenar una muestra completa
    Given la sesión inició a las 08:00:00
    When a las 08:30:00 el sistema solicita la muestra 16
    And el adquisidor responde con 5 tramas válidas y el fin de muestra
    Then se almacenan 5 lecturas con número de muestra 16
    And cada lectura guarda la marca de tiempo 08:30:00, la temperatura, el estado "OK", el tipo reportado y la trama original
    And la pantalla de monitoreo muestra los 5 valores y la hora de la última muestra

  Scenario: No acumular deriva de tiempo
    Given la muestra 15 se completó a las 08:28:04 por demoras del adquisidor
    Then la muestra 16 se solicita a las 08:30:00 y no a las 08:30:04

  Scenario: Asignar bloques en modo STREAM por tiempo transcurrido
    Given la sesión inició a las 08:00:00 en modo "STREAM"
    When llega un bloque a las 08:30:02
    Then se almacena como muestra 16 con marca de tiempo 08:30:02

  Scenario: Aceptar 1 o 2 decimales
    When el adquisidor responde "-17.9" en el canal 2 y "-18.25" en el canal 1
    Then se almacenan -17,90 °C y -18,25 °C respectivamente

  Scenario: Descartar una trama tardía de otra muestra
    Given el sistema espera la muestra 17
    When llega una trama válida con número de muestra 16
    Then la trama se descarta sin crear lectura
    And la muestra 16 no se modifica

  Scenario: Una lectura por canal y muestra
    Given el canal 2 ya tiene una lectura válida para la muestra 17
    When llega otra trama válida del canal 2 para la muestra 17 en un reintento
    Then se conserva la primera lectura y se ignora la segunda
```

---

## HU-07 · Registrar lecturas inválidas sin detener la sesión

**Como** laboratorio **quiero** que las lecturas de un sensor desconectado, las tramas corruptas y los tipos no coincidentes queden registrados y marcados sin detener la sesión **para** que el resto de los sensores siga midiendo y quede evidencia del problema.

Reglas: RN-11. Validaciones V1–V10 de [02-serial-protocol.md §7](02-serial-protocol.md#7-validación-de-tramas-en-la-pc). Alertas `SensorFault` y `TypeMismatch` por episodio (pregunta abierta P-07). Si las lecturas inválidas superan el umbral en una misma muestra, se aplica además la HU-13.

```gherkin
Feature: Registro de lecturas inválidas

  Background:
    Given una sesión en curso con los canales 1 a 5 declarados tipo "T" y límite aplicado -5,00 °C

  Scenario Outline: Estados de falla del sensor reportados por el adquisidor
    When en la muestra 20 el adquisidor responde en el canal 4 con estado "<codigo>"
    Then se almacena la lectura del canal 4 con estado "<estado>" y temperatura vacía
    And se genera una alerta "SensorFault" para el canal 4 si es el inicio de un episodio
    And los canales 1, 2, 3 y 5 se almacenan normalmente
    And la muestra 20 sigue siendo válida porque solo falla el 20 % de los canales
    And la sesión sigue en curso

    Examples:
      | codigo | estado       |
      | OC     | OpenCircuit  |
      | SC     | ShortCircuit |
      | OR     | OutOfRange   |

  Scenario: Una falla persistente no repite la alerta en cada muestra
    Given el canal 4 reportó "OC" en las muestras 20 y 21
    Then existe una sola alerta "SensorFault" del canal 4 para ese episodio
    When en la muestra 22 el canal 4 vuelve a "OK" y en la muestra 25 reporta "OC" otra vez
    Then se genera una segunda alerta "SensorFault" para el canal 4

  Scenario: Trama con checksum inválido recuperada en el reintento
    When en la muestra 30 la trama del canal 2 llega con checksum inválido
    Then el sistema repite la solicitud de la muestra 30
    And si el reintento trae una trama válida del canal 2, se almacena como lectura "OK"

  Scenario: Trama corrupta persistente
    When en los 3 intentos de la muestra 30 la trama del canal 2 es corrupta
    But los demás canales responden correctamente
    Then se almacena la lectura del canal 2 con estado "InvalidFrame" y temperatura vacía
    And se guarda la última trama corrupta del canal 2 como trama original
    And la sesión sigue en curso

  Scenario: Tipo reportado no coincide con el declarado
    When en la muestra 40 el adquisidor responde "$RD,40,3,K,-18.40,OK*CS" y el canal 3 está declarado "T"
    Then se almacena la lectura del canal 3 con estado "TypeMismatch", tipo reportado "K" y temperatura -18,40 °C
    And la lectura no se evalúa contra el límite
    And la lectura cuenta como "sin dato válido" para el umbral de pérdida de sensores
    And se genera una alerta "TypeMismatch" con el mensaje "Canal 3: el adquisidor reporta termopar tipo K pero la sesión declara tipo T."

  Scenario: Valor fuera del rango físico del tipo declarado
    When el adquisidor informa 400,00 °C con estado "OK" en el canal 1 tipo "T" (rango -200 a 350 °C)
    Then se almacena la lectura con estado "OutOfRange" y temperatura vacía
    And se genera una alerta "SensorFault" para el canal 1

  Scenario: Adquisidor reiniciado durante la sesión
    When el adquisidor envía "BOOT" durante la sesión en curso
    And la siguiente solicitud de muestra recibe "NAK,READ,E04"
    Then el sistema reenvía "START" con los mismos canales y repite la solicitud
    And la muestra se captura normalmente
```

---

## HU-08 · Alertar lecturas fuera de límite

**Como** supervisor **quiero** que cada lectura mayor que el límite aplicado genere una alerta asociada al sensor y a la marca de tiempo **para** revisar si fue una variación mínima o una falla del sensor.

Reglas: RN-07, RN-10. Comparación estricta: `TemperatureC > MaxTemperatureC`. Solo se evalúan lecturas con estado `OK`. Alerta `AboveLimit` con severidad `Warning` por **cada** lectura fuera de límite.

```gherkin
Feature: Alerta por lectura mayor que el límite máximo

  Background:
    Given una sesión en curso de una "Congeladora" con límite aplicado -5,00 °C, iniciada a las 08:00:00

  Scenario Outline: Evaluación estricta del límite máximo
    When el canal 5 reporta <valor> °C con estado "OK" en la muestra 32
    Then la lectura se marca como fuera de límite = <fuera>
    And <alerta>

    Examples:
      | valor  | fuera | alerta                                     |
      | -5,10  | No    | no se genera alerta                        |
      | -5,00  | No    | no se genera alerta                        |
      | -4,99  | Sí    | se genera una alerta "AboveLimit"          |
      | -4,90  | Sí    | se genera una alerta "AboveLimit"          |
      | 2,00   | Sí    | se genera una alerta "AboveLimit"          |

  Scenario: Contenido de la alerta fuera de límite
    When el canal 5 (ubicación "Puerta") reporta -4,90 °C en la muestra 32 a las 09:02:00
    Then se registra una alerta "AboveLimit" con:
      | campo        | valor                                                             |
      | Canal        | 5                                                                 |
      | Lectura      | muestra 32 del canal 5                                            |
      | Fecha y hora | 09:02:00                                                          |
      | Valor        | -4,90                                                             |
      | Límite       | -5,00                                                             |
      | Mensaje      | S5 (Puerta): -4,90 °C supera el límite máximo de -5,00 °C          |
    And la pantalla de monitoreo resalta el valor del canal 5 en rojo

  Scenario: La alerta no invalida la sesión
    Given se generaron 12 alertas "AboveLimit" en la sesión
    Then la sesión sigue en curso
    And al cerrarla puede quedar como "Completed"

  Scenario: Reconocer alertas
    Given que he iniciado sesión con el rol "Supervisor"
    When reconozco la alerta "AboveLimit" de la muestra 32 del canal 5
    Then la alerta registra mi usuario y la fecha y hora del reconocimiento
    But el valor, el límite y el mensaje de la alerta no se pueden modificar
```

---

## HU-09 · Detectar pérdida de comunicación, reconectar y validar el adquisidor

**Como** laboratorio **quiero** que el sistema detecte la pérdida de comunicación con el puerto COM, intente reconectar, registre los huecos de datos y verifique que reconecta el mismo adquisidor con el mismo grupo de sensores **para** no perder la sesión por una desconexión momentánea y no mezclar datos de otro equipo de medición.

Reglas: RN-12, RN-14 (las muestras perdidas cuentan como afectadas) y RN-15. [02-serial-protocol.md §9](02-serial-protocol.md#9-pérdida-y-recuperación-de-la-comunicación).

```gherkin
Feature: Pérdida y recuperación de la comunicación serial

  Background:
    Given una sesión en curso en "COM3" con el adquisidor "ADQ-ARD-0001", grupo de sensores "GRP-A", iniciada a las 08:00:00

  Scenario: Detectar la desconexión física del puerto
    When a las 10:05:10 el sistema operativo informa que "COM3" dejó de existir
    Then se abre un hueco de comunicación con inicio 10:05:10
    And se registra una alerta "CommunicationLost" con severidad "Critical"
    And la pantalla muestra "Sin comunicación con el adquisidor desde 10:05:10. Reintentando..."
    And la sesión sigue en estado "Running"

  Scenario: Detectar el adquisidor que no responde
    When la solicitud de la muestra 64 queda sin respuesta en 3 intentos consecutivos
    Then se abre un hueco de comunicación con inicio en el primer intento sin respuesta
    And no se almacenan lecturas para la muestra 64

  Scenario: Reconectar y reanudar la captura con el mismo adquisidor y grupo
    Given hay un hueco abierto desde las 10:05:10
    When a las 10:11:40 "COM3" vuelve a estar disponible y el adquisidor responde "IDN" con "ADQ-ARD-0001" y grupo "GRP-A"
    Then el sistema reenvía "START" con los mismos canales
    And el hueco se cierra con fin 10:11:40 y 3 muestras perdidas (muestras 64, 65 y 66)
    And las muestras 64, 65 y 66 cuentan como afectadas
    And la captura continúa con la muestra 67 a las 10:12:00

  Scenario: El adquisidor reaparece en otro puerto COM
    Given hay un hueco abierto y "COM3" ya no existe
    When "COM6" aparece y su adquisidor responde "IDN" con "ADQ-ARD-0001" y grupo "GRP-A"
    Then el sistema reanuda la sesión en "COM6"
    And registra en las notas del hueco "Reconectado en COM6"

  Scenario: Reaparece otro adquisidor: la sesión no es válida
    Given hay un hueco abierto desde las 09:38:00
    When a las 09:47:43 en "COM3" responde un adquisidor con identificador "ADQ-ARD-0002"
    Then el sistema no reanuda la captura y envía "STOP" al adquisidor conectado
    And la sesión queda "Invalid" con motivo "DeviceMismatch" y fin 09:47:43
    And se registra una alerta "DeviceMismatch" con severidad "Critical"
    And el hueco se cierra sin fecha de recuperación y con nota "Reconectó un adquisidor distinto (ADQ-ARD-0002)"
    And la pantalla muestra "La sesión no es válida: el adquisidor conectado (ADQ-ARD-0002) no es el de la sesión (ADQ-ARD-0001)"

  Scenario: Reaparece el mismo adquisidor con otro grupo de sensores: la sesión no es válida
    Given hay un hueco abierto
    When el adquisidor responde "IDN" con "ADQ-ARD-0001" pero con grupo de sensores "GRP-B"
    Then la sesión queda "Invalid" con motivo "DeviceMismatch"
    And se registra una alerta "DeviceMismatch" indicando "Grupo de sensores GRP-B distinto de GRP-A"

  Scenario: Cambio de grupo detectado por la huella de tipos
    Given la sesión no tiene grupo de sensores informado y su muestra 1 informó los tipos "T,T,T,T,K"
    When tras reconectar el primer bloque informa los tipos "T,T,T,T,T"
    Then la sesión queda "Invalid" con motivo "DeviceMismatch"

  Scenario: El técnico cierra la sesión durante un hueco
    Given hay un hueco abierto desde las 10:05:10 y la sesión tiene al menos 31 muestras válidas
    When el técnico cierra la sesión a las 10:30:00
    Then el hueco se cierra sin fecha de recuperación y con las muestras perdidas contadas hasta el cierre
    And la sesión queda con motivo de cierre "CommunicationLost"
    And su estado es "Completed"

  Scenario: Varios huecos en una sesión
    Given la sesión tuvo 2 pérdidas de comunicación recuperadas
    Then la sesión registra 2 huecos, cada uno con inicio, fin y muestras perdidas
```

---

## HU-10 · Cerrar la sesión (duración mínima y máxima)

**Como** técnico de calibración **quiero** cerrar la sesión cuando termino y que el sistema la cierre solo a las 24 horas **para** que el estado final refleje si se cumplieron la duración mínima y la cantidad mínima de datos válidos.

Reglas: RN-03, RN-04, RN-14. `CK_MeasurementSession_MinDuration` y `CK_MeasurementSession_MaxDuration` en la base. La cantidad de muestras válidas se obtiene de `vSessionSampleCoverage`. Al cerrar se envía `STOP` y se fija `EndedAt`.

| Situación | `Status` | `CloseReason` |
|---|---|---|
| Cierre manual con duración ≥ 1 h **y** ≥ 31 muestras válidas | `Completed` | `Manual` |
| Cierre manual con duración < 1 h, o con < 31 muestras válidas | `Incomplete` | `Manual` |
| Cierre automático al tomar la muestra 721 | `Completed` o `Incomplete` según las muestras válidas | `MaxDuration` |
| Cierre durante un hueco de comunicación abierto | `Completed` o `Incomplete` según las mismas reglas | `CommunicationLost` |
| Reconexión con otro adquisidor u otro grupo de sensores | `Invalid` | `DeviceMismatch` |
| Cancelación (datos descartables) | `Cancelled` | `Cancelled` |

```gherkin
Feature: Cierre de la sesión de medición

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And una sesión en curso iniciada a las 08:00:00

  Scenario: Cerrar como completa después de 1 hora
    Given se capturó la muestra 31 a las 09:00:00 y hay 38 muestras válidas
    When a las 09:15:00 pulso "Finalizar sesión" y confirmo
    Then el sistema envía "STOP" al adquisidor
    And la sesión queda "Completed" con fin 09:15:00 y motivo "Manual"

  Scenario: Cerrar exactamente al cumplir 1 hora
    Given las muestras 1 a 31 son válidas
    When a las 09:00:00, después de almacenar la muestra 31, pulso "Finalizar sesión"
    Then la sesión queda "Completed"

  Scenario: Impedir cerrar como completa con menos de 1 hora
    When a las 08:45:00 pulso "Finalizar sesión"
    Then el sistema advierte "La sesión dura 45 min. Con menos de 1 hora quedará como INCOMPLETA."
    And si confirmo, la sesión queda "Incomplete" con fin 08:45:00 y motivo "Manual"
    And si no confirmo, la sesión sigue en curso

  Scenario: Más de 1 hora pero sin suficientes muestras válidas
    Given la sesión dura 1 h 10 min con 36 muestras, de las cuales 10 están afectadas por pérdida de sensores
    When pulso "Finalizar sesión"
    Then el sistema advierte "La sesión tiene 26 muestras válidas de las 31 necesarias. Quedará como INCOMPLETA."
    And si confirmo, la sesión queda "Incomplete"

  Scenario: La base rechaza una sesión completa de menos de 1 hora
    When cualquier proceso intenta guardar la sesión como "Completed" con 59 minutos de duración
    Then la base de datos rechaza el cambio

  Scenario: Cierre automático a las 24 horas
    When se almacena la muestra 721 a las 08:00:00 del día siguiente
    Then el sistema envía "STOP" al adquisidor
    And la sesión queda "Completed" con fin a las 08:00:00 del día siguiente y motivo "MaxDuration"
    And no se programan más muestras

  Scenario: Aviso previo al cierre automático
    When faltan 10 minutos para cumplir las 24 horas
    Then la pantalla muestra "La sesión se cerrará automáticamente a las 08:00:00"

  Scenario: Cancelar una sesión
    When pulso "Cancelar sesión", indico el motivo "Termopar mal instalado" y confirmo
    Then la sesión queda "Cancelled" con motivo "Cancelled" y la nota indicada
    And sus lecturas se conservan pero la sesión no se puede exportar

  Scenario: Cerrar la aplicación con una sesión en curso
    When intento cerrar la aplicación
    Then el sistema advierte que hay una sesión en curso y pide finalizarla antes
```

> **Recuperación tras la caída de la aplicación:** si la aplicación se reinicia y encuentra una sesión `Running` en esa PC, la reanuda: abre un hueco desde la última muestra almacenada hasta el momento del reinicio, valida la identidad del adquisidor como en una reconexión (HU-09) y continúa con la siguiente muestra programada. Si ya pasaron las 24 h, la cierra con `EndedAt` = instante programado de la muestra 721 y motivo `MaxDuration`.

---

## HU-11 · Exportar una sesión a Excel

**Como** técnico o supervisor **quiero** exportar una sesión cerrada a un archivo .xlsx con formato estándar **para** revisarla y entregarla sin transcripciones manuales.

Reglas: se exportan las sesiones `Completed`, `Incomplete` e `Invalid` (estas últimas con un aviso, como evidencia). El archivo se genera desde la base de datos. Cada exportación se registra en `SessionExport`. El formato está en la [sección de formato del Excel](#formato-del-excel-exportado).

```gherkin
Feature: Exportación de la sesión a Excel

  Background:
    Given que he iniciado sesión con el rol "Technician"

  Scenario: Exportar una sesión completa
    Given la sesión 120 está "Completed"
    When pulso "Exportar a Excel" y elijo la carpeta de destino
    Then se genera el archivo "20100070970_SN-88231_20261001-0800_S120.xlsx"
    And contiene las hojas "Resumen", "Lecturas", "Alertas" y "Comunicación" en ese orden
    And se registra la exportación con mi usuario, la fecha y hora, y el nombre del archivo

  Scenario: Exportar una sesión incompleta
    Given la sesión 121 está "Incomplete" con 26 muestras válidas
    When la exporto a Excel
    Then la hoja "Resumen" muestra "SESIÓN INCOMPLETA: 26 muestras válidas de las 31 necesarias" resaltado

  Scenario: Exportar una sesión no válida
    Given la sesión 125 está "Invalid" porque reconectó el adquisidor "ADQ-ARD-0002"
    When la exporto a Excel
    Then la hoja "Resumen" muestra "SESIÓN NO VÁLIDA: al reconectar respondió otro adquisidor u otro grupo de sensores" en rojo
    And el nombre del archivo termina en "_NO-VALIDA.xlsx"

  Scenario: Aviso de mezcla de termopares en el Excel
    Given la sesión 122 mezcla tipos T y K
    When la exporto a Excel
    Then la hoja "Resumen" muestra en la parte superior el aviso "ATENCIÓN: esta sesión mezcla termopares tipo T y tipo K. No es una buena práctica."
    And el aviso incluye quién confirmó la advertencia y cuándo

  Scenario: Aviso de datos simulados
    Given la sesión 130 es una simulación del escenario "TD-04"
    When la exporto a Excel
    Then todas las hojas muestran en la primera fila "DATOS SIMULADOS – NO VÁLIDOS PARA CALIBRACIÓN (escenario TD-04)"
    And el nombre del archivo empieza con "SIM_"

  Scenario: Resaltar lecturas fuera de límite e inválidas
    Given la sesión 120 tiene una lectura de -4,90 °C en S5 muestra 32 con límite -5,00 °C
    And una lectura "OpenCircuit" en S4 muestra 20
    When la exporto a Excel
    Then en la hoja "Lecturas" la celda S5 de la muestra 32 contiene el número -4,90 con fondo rojo
    And la celda S4 de la muestra 20 contiene el texto "ABIERTO" con fondo gris

  Scenario: Incluir las muestras perdidas y afectadas en la hoja de lecturas
    Given la sesión 120 perdió las muestras 64 a 66 por un hueco de comunicación
    And las muestras 30 a 39 quedaron afectadas por pérdida de sensores
    When la exporto a Excel
    Then la hoja "Lecturas" contiene las filas 64, 65 y 66 con su hora programada, estado "Perdida" y "SIN DATOS" en cada sensor
    And las filas 30 a 39 tienen estado "Afectada"
    And la hoja "Comunicación" lista el hueco con 3 muestras perdidas y el episodio de pérdida de sensores de 10 muestras

  Scenario: Sesión sin límite definido
    Given la sesión 123 se capturó sin límite aplicado
    When la exporto a Excel
    Then la hoja "Resumen" muestra "Límite máximo aplicado: No definido (no se evaluó)"
    And ninguna celda de "Lecturas" aparece resaltada como fuera de límite

  Scenario: Exportar varias veces la misma sesión
    Given la sesión 120 ya se exportó una vez
    When la exporto de nuevo
    Then se genera un archivo con el mismo contenido de datos
    And se registra una segunda exportación
    And si el archivo ya existe en la carpeta, el sistema pregunta si desea reemplazarlo o agregar un sufijo

  Scenario Outline: No se exportan sesiones abiertas o canceladas
    Given la sesión 124 está "<estado>"
    When intento exportarla a Excel
    Then el sistema muestra "<mensaje>"

    Examples:
      | estado     | mensaje                                                 |
      | Configured | La sesión aún no se ha iniciado                          |
      | Running    | Finalice la sesión antes de exportarla                   |
      | Cancelled  | Las sesiones canceladas no se exportan                   |

  Scenario: Error al escribir el archivo
    Given la carpeta de destino no tiene permisos de escritura
    When exporto la sesión 120
    Then el sistema muestra "No se pudo guardar el archivo en la carpeta seleccionada"
    And no se registra la exportación
```

---

## HU-12 · Consultar el historial de sesiones

**Como** supervisor **quiero** consultar las sesiones por empresa, equipo y rango de fechas **para** revisar el historial de mediciones de cada equipo.

```gherkin
Feature: Historial de sesiones

  Background:
    Given que he iniciado sesión con el rol "Supervisor"
    And existen sesiones de varias empresas y equipos

  Scenario: Filtrar por empresa
    When filtro por la empresa con RUC "20100070970"
    Then veo solo las sesiones de equipos de esa empresa, ordenadas por fecha de inicio descendente

  Scenario: Filtrar por equipo y rango de fechas
    When filtro por el equipo "SN-88231" entre "2026-10-01" y "2026-10-31"
    Then veo las sesiones de ese equipo cuya fecha de inicio está dentro del rango, ambos días incluidos

  Scenario: Columnas del listado
    Then cada fila muestra: número de sesión, empresa, equipo (tipo, marca, modelo, serie), técnico, fecha y hora de inicio y fin, duración, estado, número de canales, muestras válidas y afectadas, indicador de mezcla T/K, número de alertas y número de huecos

  Scenario: Las simulaciones no aparecen por defecto
    Given existen sesiones simuladas
    When consulto el historial sin cambiar los filtros
    Then no veo las sesiones simuladas
    When marco "Incluir simulaciones"
    Then las veo con la etiqueta "SIMULACIÓN" y el código de escenario

  Scenario: Abrir el detalle de una sesión
    When abro la sesión 120
    Then veo su configuración, sus lecturas, alertas, huecos y muestras afectadas en modo de solo lectura
    And puedo exportarla a Excel si está "Completed", "Incomplete" o "Invalid"

  Scenario: Rango de fechas inválido
    When filtro con la fecha inicial "2026-10-31" y la final "2026-10-01"
    Then el sistema muestra "La fecha inicial debe ser anterior o igual a la final"

  Scenario: Sin resultados
    When filtro por un equipo sin sesiones
    Then el sistema muestra "No hay sesiones para los filtros seleccionados"

  Scenario: Un técnico también consulta el historial
    Given que he iniciado sesión con el rol "Technician"
    When consulto el historial
    Then veo las sesiones de todos los técnicos en modo de solo lectura
```

---

## HU-13 · Evaluar la pérdida de sensores por muestra

**Como** supervisor **quiero** que el sistema marque las muestras en las que más del 60 % de los sensores dejaron de enviar datos válidos **para** que esas muestras no cuenten como datos válidos, sin invalidar la sesión por fallas aisladas.

Reglas: RN-14, tabla de umbrales en [01 §6.1](01-vision-document.md#61-umbral-de-muestras-afectadas-según-el-número-de-canales) y base normativa en [01 §9](01-vision-document.md#9-base-normativa-del-umbral-de-pérdida-de-sensores). Solo cuenta como dato válido una lectura `OK`. Umbral copiado en `MeasurementSession.SensorLossThresholdPct`. Cálculo de referencia en la vista `vSessionSampleCoverage`.

```gherkin
Feature: Pérdida de sensores sobre el umbral

  Background:
    Given una sesión en curso con 10 canales activos y umbral de pérdida de sensores de 60 %

  Scenario: Exactamente el 60 % no afecta la muestra
    When en la muestra 20 los canales 1 a 6 reportan "OC" y los canales 7 a 10 reportan "OK"
    Then la muestra 20 es válida
    And se generan alertas "SensorFault" para los canales 1 a 6
    And no se genera alerta "SensorLoss"

  Scenario: Más del 60 % afecta la muestra
    When en la muestra 30 los canales 1 a 7 no entregan una lectura "OK"
    Then la muestra 30 queda afectada
    And se genera una alerta "SensorLoss" con severidad "Critical" y el mensaje "Muestra 30: 7 de 10 canales sin datos válidos (70 %), supera el umbral de 60 %"
    And la sesión sigue en curso

  Scenario: Un episodio de varias muestras genera una sola alerta
    When las muestras 30 a 39 quedan afectadas y la muestra 40 vuelve a ser válida
    Then existe una sola alerta "SensorLoss" para las muestras 30 a 39
    And la pantalla muestra "10 muestras afectadas" en el contador de la sesión

  Scenario Outline: Cualquier lectura no OK cuenta como sin dato válido
    When en la muestra 50 siete canales tienen estado "<estado>"
    Then la muestra 50 queda afectada

    Examples:
      | estado       |
      | OpenCircuit  |
      | ShortCircuit |
      | OutOfRange   |
      | InvalidFrame |
      | TypeMismatch |

  Scenario: Las muestras perdidas por un hueco de comunicación están afectadas
    When se pierden las muestras 64 a 66 por un hueco de comunicación
    Then las muestras 64, 65 y 66 quedan afectadas
    And la alerta del hueco es "CommunicationLost", no "SensorLoss"

  Scenario: La pérdida de sensores puede dejar la sesión incompleta
    Given una sesión de 5 canales que dura 1 h 10 min (36 muestras)
    When en las muestras 5 a 14 fallan 4 de los 5 canales (80 %)
    Then 10 muestras quedan afectadas y 26 son válidas
    And al cerrarla la sesión queda "Incomplete"

  Scenario: Monitoreo en pantalla
    Then la pantalla de la sesión muestra en todo momento: muestras programadas, muestras válidas, muestras afectadas y las que faltan para llegar a 31 válidas
```

---

## HU-14 · Ejecutar una sesión simulada con el set de datos de prueba

**Como** técnico o administrador **quiero** ejecutar una sesión completa con el adquisidor simulado y un escenario del set de datos de prueba **para** probar la captura, las alertas, los huecos, el cierre y la exportación sin tener sensores ni adquisidor.

Reglas: RN-16. [06-test-data.md](06-test-data.md). El simulador cumple el mismo protocolo que el firmware (02 F-13). El reloj de la simulación puede acelerarse, pero las marcas de tiempo almacenadas son las **simuladas** (como si la sesión hubiera durado el tiempo real), para que se apliquen las mismas reglas de duración.

```gherkin
Feature: Sesión simulada con datos de prueba

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And la aplicación tiene habilitado el modo simulación
    And existe la empresa de prueba "EMPRESA DE PRUEBA" con el equipo "SIM-Congeladora"

  Scenario: Elegir un escenario y ejecutarlo
    When elijo el puerto "SIM" y el escenario "TD-04 · Caída de sensores sobre el umbral (70 %)"
    Then la configuración de canales, tipos, ubicaciones, límite y umbral se carga desde el escenario
    And el adquisidor detectado es "ADQ-SIM-0001" con plataforma "Simulator"
    When pulso "Iniciar captura" con aceleración "x120"
    Then la sesión se ejecuta en aproximadamente 1 minuto real
    And queda marcada como simulación con el código de escenario "TD-04"

  Scenario Outline: El resultado coincide con lo esperado en el escenario
    When ejecuto el escenario "<escenario>" hasta su cierre
    Then el estado final es "<estado>" con motivo "<motivo>"
    And las muestras programadas, válidas y afectadas son <programadas> / <validas> / <afectadas>
    And las alertas por tipo coinciden con "expected.alertsByType" del escenario

    Examples:
      | escenario | estado     | motivo         | programadas | validas | afectadas |
      | TD-01     | Completed  | Manual         | 61          | 61      | 0         |
      | TD-03     | Completed  | Manual         | 61          | 61      | 0         |
      | TD-04     | Completed  | Manual         | 61          | 51      | 10        |
      | TD-05     | Incomplete | Manual         | 36          | 26      | 10        |
      | TD-06     | Completed  | Manual         | 61          | 56      | 5         |
      | TD-07     | Invalid    | DeviceMismatch | 54          | 49      | 5         |
      | TD-08     | Invalid    | DeviceMismatch | 32          | 29      | 3         |
      | TD-12     | Completed  | MaxDuration    | 721         | 721     | 0         |

  Scenario: Validación automática al terminar
    When termina una sesión simulada
    Then la aplicación compara la sesión almacenada con "scenario.json" y "readings.csv"
    And muestra "Escenario TD-04: OK" o la lista de diferencias encontradas

  Scenario: Los datos simulados nunca se mezclan con los reales
    When intento usar el adquisidor "ADQ-SIM-0001" con un equipo de un cliente real
    Then el sistema lo impide con "El adquisidor simulado solo puede usarse con la empresa de prueba"

  Scenario: El modo simulación está deshabilitado en producción
    Given la aplicación está configurada como "Producción"
    When abro la configuración de la sesión
    Then el puerto "SIM" no aparece en la lista
```

---

## Formato del Excel exportado

### Reglas generales

| Aspecto | Especificación |
|---|---|
| Formato | Office Open XML (.xlsx), compatible con Excel 2016 o posterior y con LibreOffice. |
| Nombre del archivo | `{RUC}_{Serie}_{yyyyMMdd-HHmm de inicio}_S{MeasurementSessionId}.xlsx`. Si la sesión es `Invalid` se añade `_NO-VALIDA`. Si es una simulación se antepone `SIM_`. Los caracteres no válidos en Windows (`\/:*?"<>\|`) se reemplazan por `-`. |
| Orden de hojas | Resumen, Lecturas, Alertas, Comunicación. |
| Fechas y horas | Celdas de tipo fecha con el formato `dd/mm/yyyy hh:mm:ss`, en hora local del laboratorio. La zona horaria (p. ej. `UTC-05:00`) se indica en el Resumen. |
| Temperaturas | Celdas **numéricas** (no texto) con formato `0.00`, en °C. El separador decimal lo pone la configuración regional de Excel. |
| Protección | Hojas protegidas sin contraseña, para evitar ediciones accidentales. El usuario puede quitar la protección. |
| Encabezados | Fila de encabezado en negrita, inmovilizada (freeze panes) y con autofiltro en las hojas tabulares. |
| Simulación | Si `IsSimulation` = 1, la primera fila de **todas** las hojas dice "DATOS SIMULADOS – NO VÁLIDOS PARA CALIBRACIÓN (escenario {TestScenarioCode})" sobre fondo morado claro `#E4DFEC`. |
| Propiedades del documento | Título = "Sesión {Id} – {Empresa} – {Serie}". Autor = usuario que exporta. |
| Fuente de datos | La base de datos en el momento de exportar. Nunca la memoria de la aplicación. |

### Hoja "Resumen"

Formato ficha (etiqueta en la columna A, valor en la columna B), seguido de la tabla de canales.

**Bloque de avisos** (primeras filas, solo si corresponde, en negrita):

| Aviso | Fondo |
|---|---|
| `SESIÓN NO VÁLIDA: al reconectar respondió otro adquisidor u otro grupo de sensores ({DeviceId} / {SensorGroupId}). Los datos se conservan solo como evidencia.` | Rojo `#FF0000`, texto blanco |
| `SESIÓN INCOMPLETA: {n} muestras válidas de las 31 necesarias` o `duración menor a 1 hora` | Rojo claro `#FFC7CE` |
| `ATENCIÓN: esta sesión mezcla termopares tipo T y tipo K. No es una buena práctica. Advertencia confirmada por {técnico} el {fecha hora}.` | Ámbar `#FFEB9C` |
| `PÉRDIDA DE SENSORES: {n} muestras afectadas (más del {umbral} % de los canales sin datos válidos).` | Ámbar `#FFEB9C` |
| `Límite máximo no definido para el tipo de equipo: las lecturas no se evaluaron contra un límite.` | Ámbar `#FFEB9C` |

**Ficha:**

| Etiqueta | Origen | Ejemplo |
|---|---|---|
| Sesión N.º | `MeasurementSession.MeasurementSessionId` | 120 |
| Empresa | `Company.Name` | Laboratorios Andinos S.A.C. |
| RUC | `Company.TaxId` | 20100070970 |
| Tipo de equipo | `EquipmentType.Name` | Congeladora |
| Marca / Modelo | `Equipment.Brand` / `Equipment.Model` | Haier / HBF-205 |
| N.º de serie | `Equipment.SerialNumber` | SN-88231 |
| Código interno | `Equipment.InternalCode` | ACT-00451 |
| Límite máximo aplicado (°C) | `MeasurementSession.MaxTemperatureC` o "No definido (no se evaluó)" | -5,00 |
| Criterio de límite | Texto fijo | Fuera de límite si la lectura es mayor que el límite |
| Umbral de pérdida de sensores | `SensorLossThresholdPct` | 60 % (muestra afectada si más del 60 % de los canales no tiene dato válido) |
| Técnico responsable | `AppUser.FullName` (técnico) | María Quispe |
| Adquisidor | `AcquisitionDevice.DeviceIdentifier`, `Platform`, `FirmwareVersion`, `AcquisitionMode` | ADQ-ARD-0001 (Arduino, fw 1.2.0, POLL) |
| Grupo de sensores | `MeasurementSession.SensorGroupId` o "No informado" | GRP-A |
| Puerto COM | `MeasurementSession.ComPort` | COM3 |
| Intervalo de muestreo | `SamplingIntervalSeconds` / 60 | 2 min |
| Inicio (primera muestra recibida) | `StartedAt` | 01/10/2026 08:00:00 |
| Fin | `EndedAt` | 02/10/2026 08:00:00 |
| Zona horaria | Desfase de `StartedAt` | UTC-05:00 |
| Duración | `EndedAt − StartedAt` en formato `hh:mm` | 24:00 |
| Estado | `Status` traducido: Completa / Incompleta / No válida | Completa |
| Motivo de cierre | `CloseReason` traducido: Manual / Duración máxima / Pérdida de comunicación / Adquisidor o grupo distinto | Duración máxima |
| Mezcla de termopares T/K | `HasMixedThermocoupleTypes` | No |
| Muestras programadas / válidas / afectadas / perdidas | `vSessionSampleCoverage` y `CommunicationGap` | 721 / 711 / 10 / 3 |
| Lecturas fuera de límite | Conteo de `IsAboveLimit` = 1 | 12 |
| Lecturas inválidas | Conteo de `SensorStatus` ≠ `OK` | 4 |
| Alertas | Conteo total | 18 |
| Simulación | `IsSimulation`, `TestScenarioCode` | No |
| Notas | `MeasurementSession.Notes` | — |
| Exportado por / el | Usuario y fecha de la exportación | Juan Pérez, 02/10/2026 09:10:00 |

**Tabla de canales** (debajo de la ficha, con una fila en blanco de separación):

| Sensor | Canal | Tipo de termopar | Ubicación | Etiqueta del sensor | Lecturas OK | Lecturas inválidas |
|---|---|---|---|---|---|---|
| S1 | 1 | T | Superior | TC-014 | 721 | 0 |
| S2 | 2 | T | Centro | TC-015 | 718 | 3 |
| … | … | … | … | … | … | … |
| S5 | 5 | K | Puerta | TC-102 | 721 | 0 |

Si la sesión es mixta, las filas de canales cuyo tipo es minoritario se resaltan en ámbar. En caso de empate, se resaltan los tipo K.

**Leyenda** (al final de la hoja): significado de los colores y de los códigos de la hoja "Lecturas".

### Hoja "Lecturas"

Una fila por **muestra programada**, de 1 hasta la última muestra de la sesión, **incluidas las perdidas**. Una columna por **canal activo**, en orden de número de canal.

| Columna | Encabezado | Contenido |
|---|---|---|
| A | `Muestra` | Número de muestra (1…721) |
| B | `Fecha y hora` | `ReadAt` de la muestra. Si es una muestra perdida, la hora programada (`StartedAt + (n − 1) × 2 min`) en cursiva. |
| C | `Estado de la muestra` | `Válida`, `Afectada` (pérdida de sensores sobre el umbral, fondo ámbar `#FFEB9C`) o `Perdida` (hueco de comunicación, fondo amarillo claro `#FFF2CC`) |
| D… | `S1 (T) Superior`, `S2 (T) Centro`, … | Una columna por canal activo: `S{canal} ({tipo}) {ubicación}`. Solo aparecen los canales usados en la sesión. |

**Contenido y formato de cada celda de sensor:**

| Caso | `SensorStatus` / condición | Contenido de la celda | Formato |
|---|---|---|---|
| Lectura válida dentro del límite | `OK`, `IsAboveLimit` = 0 | Número, p. ej. `-18,25` | Normal |
| Lectura **fuera de límite** | `OK`, `IsAboveLimit` = 1 | Número, p. ej. `-4,90` | Fondo rojo claro `#FFC7CE`, fuente roja oscura `#9C0006`, negrita |
| Termopar abierto | `OpenCircuit` | Texto `ABIERTO` | Fondo gris `#D9D9D9`, fuente gris oscura |
| Cortocircuito | `ShortCircuit` | Texto `CORTO` | Fondo gris `#D9D9D9` |
| Fuera de rango | `OutOfRange` | Texto `F. RANGO` | Fondo gris `#D9D9D9` |
| Trama corrupta | `InvalidFrame` | Texto `TRAMA INV.` | Fondo gris `#D9D9D9` |
| Tipo no coincidente | `TypeMismatch` | Número informado en cursiva, p. ej. `-18,40` | Fondo naranja `#F8CBAD`, con comentario de celda "Tipo reportado K ≠ declarado T. Valor no confiable." |
| Muestra perdida por hueco de comunicación | sin fila en `Reading` | Texto `SIN DATOS` | Fondo amarillo claro `#FFF2CC` |

Notas:

- Las celdas con texto no afectan las fórmulas numéricas que el usuario agregue después: Excel las ignora en `PROMEDIO`, `MIN` y `MAX`.
- La base es la vista `dbo.vSessionReadingPivot` para los valores y `dbo.vSessionSampleCoverage` para las filas (incluidas las perdidas) y el estado de la muestra. Para el formato, el generador también consulta `SensorStatus` e `IsAboveLimit` por celda.
- Una sesión de 24 h con 10 canales produce 721 filas × 13 columnas.

### Hoja "Alertas"

Una fila por alerta de la sesión, ordenadas por `OccurredAt` y después por `AlertId`.

| Columna | Encabezado | Origen |
|---|---|---|
| A | `Fecha y hora` | `Alert.OccurredAt` |
| B | `Tipo de alerta` | `AlertType` traducido: Fuera de límite, Mezcla de termopares, Tipo no coincidente, Falla de sensor, Pérdida de sensores, Pérdida de comunicación, Adquisidor o grupo distinto, Límite no definido |
| C | `Severidad` | `Severity`: Info, Advertencia, Crítica |
| D | `Sensor` | `S{ChannelNumber} ({ubicación})` o vacío si la alerta es de toda la sesión |
| E | `Muestra` | `Reading.SampleNumber` o la muestra de inicio del episodio, o vacío |
| F | `Valor (°C)` | `ValueC` (numérico) o vacío |
| G | `Límite (°C)` | `LimitC` (numérico) o vacío |
| H | `Mensaje` | `Message` |
| I | `Confirmada por` | `AppUser.FullName` de `AcknowledgedById` o "Pendiente" |
| J | `Fecha de confirmación` | `AcknowledgedAt` |

Las filas `AboveLimit` llevan el mismo resaltado rojo que en "Lecturas", las `DeviceMismatch` el rojo intenso y las `MixedThermocoupleTypes` y `SensorLoss` el ámbar. Si no hay alertas, la hoja muestra "Sin alertas registradas".

### Hoja "Comunicación"

Dos tablas, separadas por una fila en blanco.

**Tabla 1 – Huecos de comunicación**, una fila por hueco en orden cronológico:

| Columna | Encabezado | Origen |
|---|---|---|
| A | `N.º` | Correlativo 1, 2, … |
| B | `Inicio` | `CommunicationGap.LostAt` |
| C | `Fin` | `RecoveredAt`, o "No recuperado" si es NULL |
| D | `Duración` | `Fin − Inicio` en formato `hh:mm:ss` (hasta `EndedAt` de la sesión si no se recuperó) |
| E | `Muestras perdidas` | `MissedSamples` |
| F | `Rango de muestras` | p. ej. `64–66` (calculado a partir de las muestras sin lecturas) |
| G | `Notas` | `Notes` (p. ej. "Reconectado en COM6" o "Reconectó un adquisidor distinto (ADQ-ARD-0002)") |

Fila final de totales: número de huecos, duración total y total de muestras perdidas. Si no hubo huecos, la tabla muestra "Sin pérdidas de comunicación".

**Tabla 2 – Muestras afectadas por pérdida de sensores**, una fila por episodio de muestras afectadas consecutivas **con** lecturas (los huecos ya están en la tabla 1):

| Columna | Encabezado | Origen |
|---|---|---|
| A | `N.º` | Correlativo |
| B | `Desde (muestra / hora)` | Primera muestra afectada del episodio |
| C | `Hasta (muestra / hora)` | Última muestra afectada del episodio |
| D | `Muestras afectadas` | Cantidad |
| E | `Máx. canales sin dato válido` | p. ej. `7 de 10 (70 %)` |
| F | `Canales afectados` | p. ej. `S1, S2, S3, S4, S5, S6, S7` |

Si no hubo muestras afectadas, la tabla muestra "Sin pérdida de sensores sobre el umbral".
