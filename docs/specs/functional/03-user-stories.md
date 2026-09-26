# 03 · Historias de usuario y formato del Excel

Módulos: **Sesión de Medición** (SM), **Adquisición Serial** (AS) y **Exportación a Excel** (EX).

| Campo | Valor |
|---|---|
| Versión | 0.9 (borrador para revisión) |
| Cambios en 0.9 | D-07: criterio `Range` (mínimo y/o máximo) en lugar de `Maximum`, y límites sugeridos en el catálogo inicial (HU-02, HU-08, Excel). |
| Cambios en 0.8 | D-05: criterio de límite por tipo (máximo o banda alrededor de la consigna) en HU-02, HU-03, HU-05, HU-08 y el Excel. D-06: hasta 27 canales (HU-03) y puntos mínimos por tipo de equipo (HU-02, HU-17; 27 en incubadoras de más de 50 L). |
| Cambios en 0.7 | HU-01: marca y modelo obligatorios y registro de un modelo inferido (a partir del primer registro real, [DATA-1](../../data/DATA-1-analisis.md)). |
| Cambios en 0.6 | Trazabilidad: TD-19 en HU-02, RN-13 citada en HU-08 y HU-09, enlace del índice a HU-10 corregido. Cada historia tiene su `.feature` en [docs/diagrams/gherkin/](../../diagrams/gherkin/) (ver [validation.md](../../validation.md)). |
| Cambios en 0.5 | Mínimo de 9 puntos de medición con advertencia y confirmación (HU-17). Escenario TD-22. |
| Cambios en 0.4 | Fuera de límite sostenido 30 min → alerta crítica con causa probable, sensor o equipo (HU-08). Descanso de 15 min confirmado. Escenarios TD-20 y TD-21. |
| Cambios en 0.3 | Duración planificada (base de 1 h, por tipo de equipo o por pedido del cliente) y descanso del adquisidor (HU-16). Alertas críticas y advertencias (HU-15). Escalamiento de la pérdida de sensores y falla de sesión (HU-13). Cierre por duración planificada (HU-10). Escenarios TD-16 a TD-19. |
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
| [HU-02](#hu-02--gestionar-tipos-de-equipo-y-límite-máximo) | Gestionar tipos de equipo y límite máximo | SM | Admin | TD-14, TD-19 |
| [HU-03](#hu-03--configurar-una-sesión-de-medición) | Configurar una sesión de medición | SM, AS | Técnico | TD-01, TD-11 |
| [HU-04](#hu-04--advertir-la-mezcla-de-termopares-t-y-k) | Advertir la mezcla de termopares T y K | SM | Técnico | TD-10 |
| [HU-05](#hu-05--advertir-límite-no-definido) | Advertir límite no definido | SM | Técnico | TD-14 |
| [HU-06](#hu-06--iniciar-al-llegar-datos-y-capturar-cada-2-minutos) | Iniciar al llegar datos y capturar cada 2 minutos | AS | Sistema | TD-01, TD-11, TD-12 |
| [HU-07](#hu-07--registrar-lecturas-inválidas-sin-detener-la-sesión) | Registrar lecturas inválidas sin detener la sesión | AS | Sistema | TD-09, TD-15 |
| [HU-08](#hu-08--alertar-lecturas-fuera-de-límite) | Alertar lecturas fuera de límite | SM | Sistema | TD-02, TD-15, TD-20, TD-21 |
| [HU-09](#hu-09--detectar-pérdida-de-comunicación-reconectar-y-validar-el-adquisidor) | Detectar pérdida de comunicación, reconectar y validar el adquisidor | AS | Sistema | TD-06, TD-07, TD-08 |
| [HU-10](#hu-10--cerrar-la-sesión-duración-planificada) | Cerrar la sesión (duración planificada) | SM | Técnico, Sistema | TD-05, TD-10, TD-12, TD-13 |
| [HU-11](#hu-11--exportar-una-sesión-a-excel) | Exportar una sesión a Excel | EX | Técnico, Supervisor | Todos |
| [HU-12](#hu-12--consultar-el-historial-de-sesiones) | Consultar el historial de sesiones | SM | Todos | — |
| [HU-13](#hu-13--evaluar-escalar-y-fallar-por-pérdida-de-sensores) | Evaluar, escalar y fallar por pérdida de sensores | SM | Sistema | TD-03, TD-04, TD-05, TD-15, TD-16, TD-18 |
| [HU-14](#hu-14--ejecutar-una-sesión-simulada-con-el-set-de-datos-de-prueba) | Ejecutar una sesión simulada con el set de datos de prueba | AS | Técnico, Admin | Todos |
| [HU-15](#hu-15--priorizar-alertas-críticas-y-advertencias) | Priorizar alertas: críticas y advertencias | SM | Técnico, Supervisor | TD-02, TD-04, TD-16, TD-20, TD-21 |
| [HU-16](#hu-16--planificar-la-duración-y-respetar-el-descanso-del-adquisidor) | Planificar la duración y respetar el descanso del adquisidor | SM | Técnico, Supervisor | TD-10, TD-12, TD-17, TD-19 |
| [HU-17](#hu-17--advertir-menos-puntos-que-el-mínimo-del-tipo-de-equipo) | Advertir menos puntos que el mínimo del tipo de equipo | SM | Técnico | TD-15, TD-22 y los de 5 canales |

---

## HU-01 · Registrar empresa cliente y equipo

**Como** técnico de calibración **quiero** registrar la empresa cliente (RUC) y sus equipos **para** asociar cada sesión de medición a un equipo identificable y mantener su historial.

Reglas: el RUC es único (`UQ_Company_TaxId`). El número de serie es único dentro de la empresa (`UQ_Equipment_Company_Serial`). La **marca y el modelo son obligatorios** (`CK_Equipment_BrandModel`), porque son la clave de los perfiles de eficacia por modelo; si el cliente no conoce el modelo, se registra el inferido con la ficha del fabricante y se marca como pendiente de confirmar (`IsModelConfirmed` = 0; ver [DATA-1](../../data/DATA-1-analisis.md)). El tipo de equipo es obligatorio y se elige del catálogo de tipos activos. Validación del RUC peruano en la aplicación: 11 dígitos, prefijo 10, 15, 17 o 20, y dígito verificador módulo 11.

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
    And existe la empresa con RUC "20601234565"
    When registro para la empresa "20601234565" un equipo con serie "SN-88231"
    Then el equipo queda registrado

  Scenario Outline: Datos obligatorios del equipo
    When intento registrar un equipo sin <campo>
    Then el sistema no guarda el equipo e indica los campos obligatorios faltantes

    Examples:
      | campo            |
      | número de serie  |
      | tipo de equipo   |
      | marca            |
      | modelo           |

  Scenario: Registrar un equipo con el modelo inferido
    Given el cliente no recuerda el modelo de su cámara ambiental marca "Memmert"
    When registro el equipo con el modelo "TTC256" marcado como "inferido"
    Then el equipo queda registrado con el modelo pendiente de confirmar en la placa
    And las sesiones de ese equipo se agrupan igual en el perfil de eficacia del modelo "TTC256", indicando que el modelo es inferido
```

---

## HU-02 · Gestionar tipos de equipo y límite máximo

**Como** administrador **quiero** mantener el catálogo de tipos de equipo con su temperatura máxima admisible, o dejarla pendiente, **para** que las sesiones evalúen el límite correcto sin alterar las mediciones ya registradas.

Reglas: RN-04, RN-06, RN-07, RN-08, RN-20, D-05, D-06 y D-07. Cada tipo tiene un **criterio de límite** (`Range`, con mínimo y/o máximo, o `Band`, con una tolerancia; pueden quedar pendientes o venir **sugeridos** por la norma en el catálogo inicial, D-07) y sus **puntos de medición mínimos** (1 a 27; 9 por defecto). Solo el rol `Admin` crea o edita tipos. Un tipo con equipos asociados no se borra: se desactiva (`IsActive` = 0). Cada tipo tiene una **duración mínima de sesión** (60 min por defecto), por si su forma de funcionar exige más de 1 h de datos. El administrador también mantiene los parámetros de `AppSetting`: el umbral de pérdida de sensores (60 %), las muestras para escalar (3), los minutos para fallar (30), el descanso del adquisidor (15 min) y la duración máxima planificable (7 días).

```gherkin
Feature: Catálogo de tipos de equipo con límite máximo

  Background:
    Given que he iniciado sesión con el rol "Admin"

  Scenario: Registrar un tipo de equipo con límite definido
    When registro el tipo de equipo "Ultracongeladora" con límite máximo "-60,0" °C
    Then el tipo queda activo con límite máximo -60,00 °C
    And su duración mínima de sesión es 60 minutos

  Scenario: Registrar un tipo de equipo con rango de temperatura
    When registro el tipo de equipo "Refrigeradora de vacunas" con criterio "Range", mínimo "2,0" °C y máximo "8,0" °C
    Then el tipo queda activo con rango de +2,00 a +8,00 °C y el límite confirmado

  Scenario: Confirmar los límites sugeridos del catálogo inicial
    Given el tipo "Refrigeradora" tiene el rango sugerido de +2,00 a +8,00 °C (OMS PQS E003)
    And el catálogo lo muestra con la etiqueta "Límite sugerido, pendiente de confirmar"
    When el administrador guarda el tipo con el rango de +1,00 a +6,00 °C para un banco de sangre
    Then el límite queda confirmado con +1,00 a +6,00 °C
    And el catálogo deja de mostrarlo como sugerido

  Scenario: Registrar un tipo de equipo con banda de tolerancia
    When registro el tipo de equipo "Cámara ambiental" con criterio "Band" y tolerancia "2,0" K
    Then el tipo queda activo con banda de ±2,00 K alrededor de la consigna de cada sesión
    And no tiene límite máximo
    And sus puntos de medición mínimos son 9

  Scenario: Fijar los puntos de medición mínimos de un tipo de equipo
    When fijo en 27 los puntos de medición mínimos de "Incubadora" (DIN 12880, más de 50 L)
    Then las sesiones nuevas de incubadoras exigen 27 puntos para no advertir
    And las sesiones ya registradas conservan su mínimo copiado

  Scenario Outline: Rechazar un valor que no corresponde al criterio de límite
    When intento registrar un tipo con criterio "<criterio>", mínimo "<minimo>", máximo "<maximo>" y tolerancia "<tolerancia>"
    Then el sistema rechaza el registro con el mensaje "<mensaje>"

    Examples:
      | criterio | minimo | maximo | tolerancia | mensaje                                                      |
      | Range    |        | -5,0   | 1,0        | La tolerancia solo se usa con el modo Band                   |
      | Range    | 8,0    | 2,0    |            | El límite mínimo debe ser menor que el máximo                |
      | Band     |        | 5,0    | 2,0        | Los límites mínimo y máximo solo se usan con el modo Range   |
      | Band     |        |        | 0          | La tolerancia debe ser mayor que 0                           |

  Scenario Outline: Rechazar puntos de medición mínimos fuera de rango
    When intento fijar los puntos de medición mínimos de "Congeladora" en <puntos>
    Then el sistema rechaza el valor con el mensaje "Los puntos de medición mínimos deben estar entre 1 y 27"

    Examples:
      | puntos |
      | 0      |
      | 28     |

  Scenario: Exigir una duración mínima mayor para un tipo de equipo
    When fijo la duración mínima de sesión de "Incubadora" en 120 minutos
    Then las sesiones nuevas de incubadoras se planifican con al menos 120 minutos
    And las sesiones ya registradas conservan su duración planificada

  Scenario: Rechazar una duración mínima menor que la base
    When intento fijar la duración mínima de sesión de "Congeladora" en 45 minutos
    Then el sistema rechaza el valor con el mensaje "La duración mínima no puede ser menor que 60 minutos"

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

**Como** técnico de calibración **quiero** configurar la sesión eligiendo el equipo, el puerto COM, el adquisidor detectado y de 1 a 27 canales con su tipo de termopar y su ubicación **para** iniciar una captura trazable.

Reglas: RN-01, RN-06 y D-05, D-06. Hasta 27 canales. Si el tipo de equipo usa criterio de banda, la **consigna** de la sesión es obligatoria. La sesión se crea con `Status` = `Configured`. El técnico responsable es el usuario autenticado. Un puerto COM no puede estar en dos sesiones `Running` a la vez (supuesto S-01). La aplicación exige un adquisidor detectado para iniciar, aunque `AcquisitionDeviceId` admita NULL mientras la sesión está en configuración.

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
      | 27          | 27      | 27  | acepta la configuración                                            |
      | 27          | 12      | 28  | rechaza con "El número de canal debe estar entre 1 y 27"           |
      | 8           | 3       | 9   | rechaza con "El adquisidor solo tiene 8 canales"                   |

  Scenario: Consigna obligatoria en un equipo con banda de tolerancia
    Given el equipo es de tipo "Cámara ambiental", con criterio "Band" y tolerancia ±2,00 K
    When intento guardar la configuración sin consigna
    Then el sistema pide "Indique la consigna de temperatura de la sesión"
    When indico la consigna 20,0 °C y guardo
    Then la sesión queda en estado "Configured" con banda 20,00 ± 2,00 °C

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

Reglas: RN-09 y D-05. `MeasurementSession.MaxTemperatureC` = NULL (criterio máximo) o `ToleranceK` = NULL (criterio banda). Alerta `LimitNotDefined`, severidad `Info`.

```gherkin
Feature: Advertencia de límite máximo no definido

  Scenario: Iniciar una sesión sin límite definido
    Given el tipo "Refrigeradora" tiene el límite pendiente
    And tengo configurada una sesión para un equipo tipo "Refrigeradora"
    When pulso "Iniciar captura"
    Then el sistema muestra "El tipo de equipo Refrigeradora no tiene límite máximo definido. Se capturarán las lecturas sin evaluar límite."
    And al pulsar "Continuar" la sesión pasa a "Esperando datos" con límite aplicado vacío
    And se registra una alerta "LimitNotDefined" con severidad "Info" reconocida por mí

  Scenario: Banda con tolerancia pendiente
    Given el tipo "Incubadora" usa criterio "Band" con la tolerancia pendiente
    And tengo configurada una sesión de una incubadora con consigna 37,0 °C
    When pulso "Iniciar captura"
    Then el sistema muestra "El tipo de equipo Incubadora no tiene tolerancia definida. Se capturarán las lecturas sin evaluar límite."
    And se registra una alerta "LimitNotDefined" con severidad "Info"

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
    And en una sesión base de 1 h la última muestra es la 31, a las 09:00:00
    And en una sesión de 24 h pedida por el cliente la última es la 721, a las 08:00:00 del día siguiente

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

Reglas: RN-07, RN-10, RN-13, RN-18, RN-19, D-05 y [01 §6.4](01-vision-document.md#64-fuera-de-límite-sostenido-sensor-o-equipo). Comparación estricta: `TemperatureC > MaxTemperatureC` con criterio rango (y `TemperatureC < MinTemperatureC` para el mínimo); con criterio banda, `TemperatureC > SetpointC + ToleranceK` (`AboveLimit`) o `TemperatureC < SetpointC − ToleranceK` (`BelowLimit`, `IsBelowLimit` = 1). Solo se evalúan lecturas con estado `OK`. Alerta `AboveLimit` con severidad `Warning` por **cada** lectura fuera de límite. Es una **advertencia**: se registra y se resalta, pero **no** interrumpe al técnico con avisos, porque la temperatura puede variar por distintas causas (HU-15).

```gherkin
Feature: Alerta por lectura mayor que el límite máximo

  Background:
    Given una sesión en curso de una "Congeladora" con límite aplicado -5,00 °C, iniciada a las 08:00:00 y planificada de 2 h

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

  Scenario Outline: Evaluación estricta del rango de una refrigeradora
    Given la sesión es de una "Refrigeradora" con rango de +2,00 a +8,00 °C, en lugar del límite -5,00 °C
    When el canal 2 reporta <valor> °C con estado "OK"
    Then la lectura queda <resultado>

    Examples:
      | valor | resultado                                                  |
      | 8,01  | fuera de límite por arriba, con una alerta "AboveLimit"    |
      | 8,00  | dentro del rango, sin alerta                               |
      | 2,00  | dentro del rango, sin alerta                               |
      | 1,99  | fuera de límite por abajo, con una alerta "BelowLimit"     |

  Scenario Outline: Evaluación estricta de la banda de tolerancia
    Given la sesión es de una "Cámara ambiental" con consigna 20,00 °C y tolerancia ±2,00 K, en lugar del límite máximo
    When el canal 3 reporta <valor> °C con estado "OK"
    Then la lectura queda <resultado>

    Examples:
      | valor | resultado                                                  |
      | 22,01 | fuera de límite por arriba, con una alerta "AboveLimit"    |
      | 22,00 | dentro de la banda, sin alerta                             |
      | 18,00 | dentro de la banda, sin alerta                             |
      | 17,99 | fuera de límite por abajo, con una alerta "BelowLimit"     |

  Scenario: Un sensor por debajo de la banda 30 minutos
    Given la sesión es de una "Cámara ambiental" con consigna 20,00 °C y tolerancia ±2,00 K, en lugar del límite máximo
    When el canal 10 está por debajo de 18,00 °C desde la muestra 20 y los demás canales están dentro de la banda
    Then a los 30 min se genera una alerta "BelowLimitSustained" con severidad "Critical" y causa probable "Sensor"

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
    And el contador de advertencias de la sesión aumenta en 1
    But no se muestra ningún aviso emergente ni sonido

  Scenario: Un solo sensor fuera de límite 30 minutos: posible falla del sensor
    Given los canales 1, 2, 4 y 5 están cerca de -18 °C
    When el canal 3 (Inferior) está fuera de límite desde la muestra 20 (08:38:00)
    Then cada lectura del canal 3 genera solo una advertencia "AboveLimit"
    And en la muestra 35 (09:08:00), a los 30 min, se genera una alerta "AboveLimitSustained" con severidad "Critical" para el canal 3
    And la alerta indica la causa probable "Sensor" con el mensaje "S3 (Inferior) lleva 30 min fuera de límite mientras los demás sensores están dentro: posible falla o mala colocación del termopar, o una zona localizada"
    And sugiere "Revisar la colocación y la conexión del termopar, o compararlo con el sensor vecino"
    And las lecturas del canal 3 se conservan como válidas

  Scenario: La mayoría de sensores fuera de límite 30 minutos: posible falla del equipo
    When desde la muestra 30 todos los canales suben y quedan fuera de límite
    Then a los 30 min de cada canal fuera de límite se genera una alerta "AboveLimitSustained" con causa probable "Equipment"
    And las alertas críticas que llegan en la misma muestra se muestran agrupadas en un solo aviso
    And el mensaje sugiere "Revisar el equipo bajo prueba (compresor, puerta) e informar al supervisor"

  Scenario: Una excursión breve no escala
    When el canal 4 (Puerta) está fuera de límite en las muestras 21 y 22 y vuelve a estar dentro en la 23
    Then solo existen advertencias "AboveLimit"
    And no se genera "AboveLimitSustained"

  Scenario: Una lectura dentro del límite o inválida reinicia la cuenta
    Given el canal 3 lleva 25 min fuera de límite
    When la siguiente lectura del canal 3 está dentro del límite, o es inválida
    Then la cuenta de los 30 min vuelve a empezar

  Scenario: El supervisor confirma la causa al reconocer la alerta
    Given que he iniciado sesión con el rol "Supervisor"
    When reconozco la alerta "AboveLimitSustained" del canal 3 con la nota "Termopar desprendido, recolocado a las 09:30"
    Then la alerta registra mi usuario, la fecha y hora y la nota
    But la causa probable calculada por el sistema no cambia

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

Reglas: RN-12, RN-13 (los huecos no se modifican una vez cerrados), RN-14 (las muestras perdidas cuentan como afectadas) y RN-15. [02-serial-protocol.md §9](02-serial-protocol.md#9-pérdida-y-recuperación-de-la-comunicación).

```gherkin
Feature: Pérdida y recuperación de la comunicación serial

  Background:
    Given una sesión en curso en "COM3" con el adquisidor "ADQ-ARD-0001", grupo de sensores "GRP-A", iniciada a las 08:00:00 y planificada de 4 h a pedido del cliente

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

  Scenario: Reaparece otro adquisidor: la sesión falla
    Given hay un hueco abierto desde las 09:38:00
    When a las 09:47:43 en "COM3" responde un adquisidor con identificador "ADQ-ARD-0002"
    Then el sistema no reanuda la captura y envía "STOP" al adquisidor conectado
    And la sesión queda "Invalid" (fallida) con motivo "DeviceMismatch" y fin 09:47:43
    And se registra una alerta "DeviceMismatch" con severidad "Critical"
    And el hueco se cierra sin fecha de recuperación y con nota "Reconectó un adquisidor distinto (ADQ-ARD-0002)"
    And la pantalla muestra "Sesión fallida: el adquisidor conectado (ADQ-ARD-0002) no es el de la sesión (ADQ-ARD-0001)"

  Scenario: El corte de comunicación dura 30 minutos: la sesión falla
    Given hay un hueco abierto desde la muestra 64, programada a las 10:06:00
    When llega la hora programada de la muestra 79 (10:36:00) sin haber recuperado la comunicación
    Then la sesión queda "Invalid" (fallida) con motivo "DataLoss" y fin 10:36:00
    And se registra una alerta "SessionFailed" con severidad "Critical"
    And el sistema deja de intentar la reconexión

  Scenario: Reaparece el mismo adquisidor con otro grupo de sensores: la sesión falla
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
    When el técnico cierra la sesión a las 10:30:00, antes de cumplir las 4 h planificadas
    Then el hueco se cierra sin fecha de recuperación y con las muestras perdidas contadas hasta el cierre
    And la sesión queda con motivo de cierre "CommunicationLost"
    And su estado es "Incomplete" porque no alcanzó la duración planificada

  Scenario: Varios huecos en una sesión
    Given la sesión tuvo 2 pérdidas de comunicación recuperadas
    Then la sesión registra 2 huecos, cada uno con inicio, fin y muestras perdidas
```

---

## HU-10 · Cerrar la sesión (duración planificada)

**Como** técnico de calibración **quiero** que la sesión se cierre sola al cumplir su duración planificada, y poder cerrarla antes si hace falta, **para** que el estado final refleje si se cumplieron la duración planificada y la cantidad mínima de datos válidos.

Reglas: RN-03, RN-04, RN-14, RN-15. `CK_MeasurementSession_MinDuration` (completa solo si alcanzó `PlannedDurationMinutes`) y `CK_MeasurementSession_MaxDuration` (no más allá de lo planificado) en la base. La cantidad de muestras válidas se obtiene de `vSessionSampleCoverage`. Al cerrar se envía `STOP`, se fija `EndedAt` y empieza el descanso del adquisidor (HU-16).

| Situación | `Status` | `CloseReason` |
|---|---|---|
| Cierre automático al cumplir la duración planificada, con ≥ 31 muestras válidas | `Completed` | `PlannedDuration` |
| Cierre automático al cumplir la duración planificada, con < 31 muestras válidas | `Incomplete` | `PlannedDuration` |
| Cierre manual antes de cumplir la duración planificada | `Incomplete` | `Manual` |
| Cierre durante un hueco de comunicación abierto | `Incomplete` (antes de la duración planificada) | `CommunicationLost` |
| Reconexión con otro adquisidor u otro grupo de sensores | `Invalid` (fallida) | `DeviceMismatch` |
| 30 min consecutivos de muestras afectadas o perdidas | `Invalid` (fallida) | `DataLoss` |
| Cancelación (datos descartables) | `Cancelled` | `Cancelled` |

```gherkin
Feature: Cierre de la sesión de medición

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And una sesión base de 1 h en curso, iniciada a las 08:00:00

  Scenario: Cierre automático de la sesión base con todas las muestras válidas
    Given las muestras 1 a 31 son válidas
    When se almacena la muestra 31 a las 09:00:00
    Then el sistema envía "STOP" al adquisidor
    And la sesión queda "Completed" con fin 09:00:00 y motivo "PlannedDuration"
    And no se programan más muestras

  Scenario: Cierre automático con muestras afectadas
    Given 10 de las 31 muestras están afectadas por pérdida de sensores
    When se almacena la muestra 31 a las 09:00:00
    Then la sesión queda "Incomplete" con motivo "PlannedDuration"
    And el resumen indica "21 muestras válidas de las 31 necesarias"

  Scenario: Extender la sesión para recuperar muestras válidas
    Given a las 08:50:00 la sesión tiene 5 muestras afectadas
    When extiendo la duración planificada a 75 minutos indicando el motivo "Recuperar datos tras falla de sensor"
    Then la sesión se cierra automáticamente a las 09:15:00
    And queda "Completed" si al cierre tiene al menos 31 muestras válidas

  Scenario: Cerrar antes de la duración planificada
    When a las 08:45:00 pulso "Finalizar sesión"
    Then el sistema advierte "La sesión dura 45 min de los 60 planificados. Quedará como INCOMPLETA."
    And si confirmo, la sesión queda "Incomplete" con fin 08:45:00 y motivo "Manual"
    And si no confirmo, la sesión sigue en curso

  Scenario: La base rechaza una sesión completa que no alcanzó su duración planificada
    Given una sesión planificada de 120 minutos
    When cualquier proceso intenta guardarla como "Completed" con 90 minutos de duración
    Then la base de datos rechaza el cambio

  Scenario: Cierre automático de una sesión larga pedida por el cliente
    Given una sesión planificada de 24 h a pedido del cliente, con la referencia "OS-2026-0142"
    When se almacena la muestra 721 a las 08:00:00 del día siguiente
    Then la sesión queda "Completed" con motivo "PlannedDuration"

  Scenario: Aviso previo al cierre automático
    When faltan 10 minutos para cumplir la duración planificada
    Then la pantalla muestra "La sesión se cerrará automáticamente a las 09:00:00"

  Scenario: Cancelar una sesión
    When pulso "Cancelar sesión", indico el motivo "Termopar mal instalado" y confirmo
    Then la sesión queda "Cancelled" con motivo "Cancelled" y la nota indicada
    And sus lecturas se conservan pero la sesión no se puede exportar

  Scenario: Cerrar la aplicación con una sesión en curso
    When intento cerrar la aplicación
    Then el sistema advierte que hay una sesión en curso y pide finalizarla antes
```

> **Recuperación tras la caída de la aplicación:** si la aplicación se reinicia y encuentra una sesión `Running` en esa PC, la reanuda: abre un hueco desde la última muestra almacenada hasta el momento del reinicio, valida la identidad del adquisidor como en una reconexión (HU-09) y continúa con la siguiente muestra programada. Si ya pasó la duración planificada, la cierra con `EndedAt` = instante programado de la última muestra y motivo `PlannedDuration`. Si el hueco por el reinicio llegó a 30 min, la sesión falla (`DataLoss`).

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

  Scenario Outline: Exportar una sesión fallida
    Given la sesión 125 está "Invalid" con motivo "<motivo>"
    When la exporto a Excel
    Then la hoja "Resumen" muestra "<aviso>" en rojo
    And el nombre del archivo termina en "_FALLIDA.xlsx"

    Examples:
      | motivo         | aviso                                                                                      |
      | DeviceMismatch | SESIÓN FALLIDA: al reconectar respondió otro adquisidor u otro grupo de sensores           |
      | DataLoss       | SESIÓN FALLIDA: 30 min consecutivos sin datos suficientes (muestras 20 a 35)               |

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

## HU-13 · Evaluar, escalar y fallar por pérdida de sensores

**Como** supervisor **quiero** que el sistema marque las muestras en las que más del 60 % de los sensores dejaron de enviar datos válidos, que me avise de forma crítica si la pérdida no se restablece en la 3.ª medición, y que dé la sesión por fallida si dura 30 min, **para** actuar a tiempo sin invalidar la sesión por fallas aisladas.

Reglas: RN-14, RN-15 (b), tabla de umbrales en [01 §6.1](01-vision-document.md#61-umbral-de-muestras-afectadas-según-el-número-de-canales), escalamiento en [01 §6.2](01-vision-document.md#62-escalamiento-de-la-pérdida-de-sensores) y base normativa en [01 §9.1](01-vision-document.md#91-base-normativa-del-escalamiento-la-falla-de-sesión-y-la-duración). Solo cuenta como dato válido una lectura `OK`. La política se copia en la sesión (`SensorLossThresholdPct`, `SensorLossCriticalAfterSamples`, `SensorLossFailMinutes`). Cálculo de referencia en la vista `vSessionSampleCoverage`.

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
    And se genera una alerta "SensorLoss" con severidad "Warning" y el mensaje "Muestra 30: 7 de 10 canales sin datos válidos (70 %), supera el umbral de 60 %"
    And no se muestra un aviso emergente
    And la sesión sigue en curso

  Scenario: Se restablece antes de la 3.ª medición
    When las muestras 10 y 11 quedan afectadas y la muestra 12 vuelve a ser válida
    Then solo existe la advertencia "SensorLoss" de la muestra 10
    And no se genera ninguna alerta crítica

  Scenario: No se restablece en la 3.ª medición: alerta crítica
    When las muestras 30, 31 y 32 quedan afectadas
    Then en la muestra 32 se genera una alerta "SensorLossPersistent" con severidad "Critical"
    And la pantalla muestra un aviso destacado "Pérdida de sensores sin recuperar desde la muestra 30 (4 min). Revise conexiones y termopares."
    And el aviso permanece hasta que alguien lo reconoce

  Scenario: Un episodio de varias muestras genera una sola advertencia y una sola crítica
    When las muestras 30 a 39 quedan afectadas y la muestra 40 vuelve a ser válida
    Then existe una sola alerta "SensorLoss" y una sola "SensorLossPersistent" para las muestras 30 a 39
    And la pantalla muestra "10 muestras afectadas" en el contador de la sesión
    And la sesión sigue en curso porque el episodio duró 18 min, menos de 30

  Scenario: 30 minutos consecutivos afectados: la sesión falla
    Given una sesión de 5 canales iniciada a las 08:00:00
    When desde la muestra 20 (08:38:00) 4 de los 5 canales siguen abiertos sin recuperarse
    Then en la muestra 22 se genera la alerta crítica "SensorLossPersistent"
    And en la muestra 35 (09:08:00), a los 30 min, se genera la alerta crítica "SessionFailed"
    And la sesión queda "Invalid" (fallida) con motivo "DataLoss" y fin 09:08:00
    And el sistema envía "STOP" y deja de capturar

  Scenario: 15 muestras consecutivas (28 min) no hacen fallar la sesión
    When las muestras 50 a 64 quedan afectadas y la 65 vuelve a ser válida
    Then la sesión sigue en curso

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
    Then las muestras 64, 65 y 66 quedan afectadas y cuentan para el conteo consecutivo
    And la alerta crítica del hueco es "CommunicationLost", no "SensorLoss" ni "SensorLossPersistent"

  Scenario: La pérdida de sensores puede dejar incompleta una sesión base
    Given una sesión base de 1 h con 5 canales (31 muestras)
    When en las muestras 5 a 14 fallan 4 de los 5 canales (80 %)
    Then 10 muestras quedan afectadas y 21 son válidas
    And al cumplirse la hora la sesión queda "Incomplete"
    And el sistema sugiere extender la sesión para completar las 31 muestras válidas

  Scenario: Monitoreo en pantalla
    Then la pantalla de la sesión muestra en todo momento: muestras programadas, válidas y afectadas, las que faltan para llegar a 31 válidas, y el conteo de muestras afectadas consecutivas con los minutos que faltan para la falla
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
      | escenario | estado     | motivo          | programadas | validas | afectadas |
      | TD-01     | Completed  | PlannedDuration | 61          | 61      | 0         |
      | TD-04     | Completed  | PlannedDuration | 61          | 51      | 10        |
      | TD-05     | Incomplete | PlannedDuration | 31          | 21      | 10        |
      | TD-06     | Completed  | PlannedDuration | 61          | 56      | 5         |
      | TD-07     | Invalid    | DeviceMismatch  | 54          | 49      | 5         |
      | TD-10     | Completed  | PlannedDuration | 31          | 31      | 0         |
      | TD-13     | Incomplete | Manual          | 23          | 23      | 0         |
      | TD-16     | Invalid    | DataLoss        | 35          | 19      | 16        |
      | TD-17     | Completed  | PlannedDuration | 2161        | 2161    | 0         |
      | TD-18     | Completed  | PlannedDuration | 91          | 71      | 20        |
      | TD-19     | Incomplete | Manual          | 46          | 46      | 0         |
      | TD-20     | Completed  | PlannedDuration | 61          | 61      | 0         |
      | TD-21     | Completed  | PlannedDuration | 61          | 61      | 0         |
      | TD-22     | Completed  | PlannedDuration | 31          | 31      | 0         |

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

## HU-15 · Priorizar alertas: críticas y advertencias

**Como** técnico de calibración **quiero** que el sistema solo me interrumpa con las alertas críticas y registre las demás sin avisos **para** no tener que atender cada variación de temperatura, que es esperable por distintas causas, y actuar rápido cuando algo compromete la sesión.

Reglas: RN-18, clasificación en [01 §6.3](01-vision-document.md#63-clasificación-de-alertas). La severidad se asigna al crear la alerta y no cambia. Una situación que empeora genera **otra** alerta de mayor severidad (p. ej. `SensorLoss` → `SensorLossPersistent`).

```gherkin
Feature: Presentación de alertas según su severidad

  Background:
    Given una sesión en curso que estoy monitoreando

  Scenario Outline: Las advertencias se registran sin interrumpir
    When se genera una alerta "<tipo>"
    Then aumenta el contador de advertencias de la sesión
    And la alerta aparece en la lista de alertas
    But no se muestra un aviso emergente ni suena ninguna alarma

    Examples:
      | tipo                   |
      | AboveLimit             |
      | SensorFault            |
      | TypeMismatch           |
      | SensorLoss             |

  Scenario Outline: Las alertas críticas se notifican y exigen reconocimiento
    When se genera una alerta "<tipo>"
    Then se muestra un aviso destacado en rojo con el mensaje de la alerta
    And se envía la notificación en tiempo real a todas las pantallas que monitorean la sesión
    And el aviso permanece visible hasta que un usuario lo reconoce
    And el reconocimiento registra el usuario y la fecha y hora

    Examples:
      | tipo                 |
      | AboveLimitSustained  |
      | SensorLossPersistent |
      | SessionFailed        |
      | DeviceMismatch       |
      | CommunicationLost    |

  Scenario: Filtrar la lista de alertas por severidad
    When filtro la lista de alertas por "Críticas"
    Then veo solo las alertas con severidad "Critical"
    And cada una indica si está reconocida y por quién

  Scenario: Críticas pendientes al cerrar
    Given la sesión tiene 1 alerta crítica sin reconocer
    When la sesión se cierra
    Then el resumen de cierre muestra "1 alerta crítica sin reconocer"
    And el Excel la muestra como "Pendiente" en la columna de confirmación
```

---

## HU-16 · Planificar la duración y respetar el descanso del adquisidor

**Como** técnico de calibración **quiero** que la sesión se planifique con la duración base de 1 h, o con la que exija el tipo de equipo o pida el cliente, y que el adquisidor descanse entre sesiones, **para** medir cada equipo el tiempo necesario y no saturar el equipo de medición mientras paso a calibrar el siguiente.

Reglas: RN-04, RN-17. Parámetros en `AppSetting`: `BaseSessionMinutes` (60), `MaxSessionMinutes` (10 080 = 7 días), `RestPeriodMinutes` (15, confirmado: descanso del kit de medición). Columnas: `MeasurementSession.PlannedDurationMinutes`, `DurationSource`, `ClientRequestReference`, `RestOverrideById` y `RestOverrideReason`.

```gherkin
Feature: Duración planificada y descanso del adquisidor

  Background:
    Given que he iniciado sesión con el rol "Technician"

  Scenario: Sesión base de 1 hora
    Given el tipo "Congeladora" exige 60 minutos
    When configuro una sesión para una congeladora sin pedido especial del cliente
    Then la duración planificada es 60 minutos con origen "Base"

  Scenario: Duración exigida por el tipo de equipo
    Given el tipo "Incubadora" exige 120 minutos
    When configuro una sesión para una incubadora
    Then la duración planificada es 120 minutos con origen "EquipmentType"
    And no puedo planificar menos de 120 minutos

  Scenario: Duración solicitada por el cliente
    When planifico 72 horas indicando la referencia "OS-2026-0142" del pedido del cliente
    Then la duración planificada es 4320 minutos con origen "ClientRequest"
    And el Resumen del Excel mostrará la referencia "OS-2026-0142"

  Scenario: El pedido del cliente exige referencia
    When planifico 24 horas sin indicar la referencia del pedido
    Then el sistema pide "Indique la referencia del pedido del cliente (p. ej. orden de servicio)"

  Scenario: Superar el máximo planificable
    When intento planificar 10 días
    Then el sistema rechaza el valor con "La duración máxima que se puede planificar es 7 días"

  Scenario: Extender una sesión en curso
    Given una sesión base en curso
    When la extiendo a 90 minutos con la referencia "Pedido verbal del cliente, J. Pérez"
    Then la duración planificada pasa a 90 minutos con origen "ClientRequest"
    And el cierre automático se reprograma

  Scenario: El adquisidor está en descanso
    Given el adquisidor "ADQ-ARD-0001" terminó una sesión a las 10:00:00
    And el descanso configurado es de 15 minutos
    When a las 10:06:00 intento iniciar una sesión nueva con ese adquisidor
    Then el sistema muestra "El adquisidor descansa hasta las 10:15:00 (faltan 9 min)"
    And el botón "Iniciar captura" permanece deshabilitado
    But puedo seguir configurando la sesión

  Scenario: Autorizar un inicio anticipado
    Given el adquisidor está en descanso
    And que he iniciado sesión con el rol "Supervisor"
    When autorizo el inicio anticipado con el motivo "Cliente en espera, equipo de reemplazo no disponible"
    Then la sesión puede iniciarse
    And la sesión registra quién autorizó y el motivo

  Scenario: Un técnico no puede saltarse el descanso
    Given el adquisidor está en descanso
    When intento autorizar un inicio anticipado con el rol "Technician"
    Then el sistema me deniega la acción por falta de permisos
```

---

## HU-17 · Advertir menos puntos que el mínimo del tipo de equipo

**Como** técnico de calibración **quiero** que el sistema me advierta si configuro menos puntos de medición que los que exige la norma del tipo de equipo (9 en general, 8 esquinas y el centro según IEC 60068-3-5 y DKD-R 5-7; 27 en incubadoras de más de 50 L según DIN 12880) **para** saber que la sesión no cumple el mínimo normativo, y dejar constancia si aun así debo iniciarla.

Reglas: RN-20 y D-06. `EquipmentType.MinMeasurementPoints` (9 por defecto; 27 en incubadoras), copiado en la sesión. `MeasurementSession.IsBelowMinimumPoints` = 1. Alerta `BelowMinimumPoints` con severidad `Warning`. La confirmación rellena `BelowMinimumAcknowledgedAt`. La base impide pasar a `Running` sin confirmación (`CK_MeasurementSession_BelowMinAck`). Mismo patrón que la mezcla T/K (HU-04).

```gherkin
Feature: Advertencia por menos puntos de medición que el mínimo normativo

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And el tipo de equipo de la sesión exige 9 puntos de medición

  Scenario: Sesión con 9 puntos no muestra advertencia
    Given asigné 9 canales en las 8 esquinas y el centro
    When pulso "Iniciar captura"
    Then la sesión pasa a "Esperando datos" sin advertencia de puntos

  Scenario: Sugerir las posiciones normalizadas
    When asigno la ubicación de un canal
    Then la lista de sugerencias muestra primero las 8 esquinas y el centro
    And indica cuáles de esas 9 posiciones todavía no están asignadas

  Scenario: Menos de 9 puntos exige confirmación
    Given asigné 5 canales
    When pulso "Iniciar captura"
    Then el sistema muestra la advertencia:
      """
      La sesión tiene 5 puntos de medición. IEC 60068-3-5 y DKD-R 5-7 exigen al menos 9
      (las 8 esquinas y el centro) para calibrar el volumen útil de equipos menores de 2000 L.
      Si continúa, la sesión quedará marcada como por debajo del mínimo normativo.
      """
    And registra una alerta "BelowMinimumPoints" con severidad "Warning"
    And la captura no inicia hasta que yo confirme

  Scenario: Una incubadora de más de 50 L exige 27 puntos
    Given el equipo es una "Incubadora", cuyo tipo exige 27 puntos de medición (DIN 12880)
    And asigné 12 canales
    When pulso "Iniciar captura"
    Then el sistema muestra "La sesión tiene 12 puntos de medición. El tipo de equipo Incubadora exige al menos 27 (DIN 12880, más de 50 L)."
    And registra una alerta "BelowMinimumPoints" con severidad "Warning"
    And la captura no inicia hasta que yo confirme

  Scenario: Confirmar e iniciar
    Given se mostró la advertencia de puntos
    When marco "Entiendo que no cumple el mínimo normativo" y pulso "Confirmar e iniciar"
    Then la alerta queda reconocida por mí con fecha y hora
    And la sesión registra la fecha y hora de la confirmación y queda marcada como por debajo del mínimo

  Scenario: Advertencias combinadas al iniciar
    Given la sesión tiene 5 canales, mezcla tipos T y K, y el tipo de equipo no tiene límite
    When pulso "Iniciar captura"
    Then el sistema muestra las tres advertencias en un mismo diálogo
    And exige confirmar por separado los puntos y la mezcla

  Scenario: La base impide iniciar sin confirmación
    Given una sesión marcada por debajo del mínimo y sin fecha de confirmación
    When cualquier proceso intenta cambiar su estado a "Running"
    Then la base de datos rechaza el cambio
```

---

## Formato del Excel exportado

### Reglas generales

| Aspecto | Especificación |
|---|---|
| Formato | Office Open XML (.xlsx), compatible con Excel 2016 o posterior y con LibreOffice. |
| Nombre del archivo | `{RUC}_{Serie}_{yyyyMMdd-HHmm de inicio}_S{MeasurementSessionId}.xlsx`. Si la sesión es `Invalid` (fallida) se añade `_FALLIDA`. Si es una simulación se antepone `SIM_`. Los caracteres no válidos en Windows (`\/:*?"<>\|`) se reemplazan por `-`. |
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
| `SESIÓN FALLIDA: al reconectar respondió otro adquisidor u otro grupo de sensores ({DeviceId} / {SensorGroupId}). Los datos se conservan solo como evidencia.` | Rojo `#FF0000`, texto blanco |
| `SESIÓN FALLIDA: {n} min consecutivos sin datos suficientes (muestras {desde} a {hasta}). Los datos se conservan solo como evidencia.` | Rojo `#FF0000`, texto blanco |
| `SESIÓN INCOMPLETA: {n} muestras válidas de las 31 necesarias` o `cerrada a los {m} min de los {p} planificados` | Rojo claro `#FFC7CE` |
| `PUNTOS DE MEDICIÓN: {n} de los 9 que exigen IEC 60068-3-5 y DKD-R 5-7 (8 esquinas y centro). Advertencia confirmada por {técnico} el {fecha hora}.` | Ámbar `#FFEB9C` |
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
| Límite aplicado (°C) | Criterio rango: `MinTemperatureC … MaxTemperatureC` (o solo uno de ellos). Criterio banda: `SetpointC ± ToleranceK`. Sin valor: "No definido (no se evaluó)". Si era sugerido al iniciar, se añade "(sugerido por la norma)" | 2,00 … 8,00 · o 20,00 ± 2,00 |
| Criterio de límite | `LimitMode` traducido | Rango: fuera de límite si la lectura es mayor que el máximo o menor que el mínimo. Banda: fuera de límite si se aleja de la consigna más que la tolerancia |
| Umbral de pérdida de sensores | `SensorLossThresholdPct` | 60 % (muestra afectada si más del 60 % de los canales no tiene dato válido) |
| Técnico responsable | `AppUser.FullName` (técnico) | María Quispe |
| Adquisidor | `AcquisitionDevice.DeviceIdentifier`, `Platform`, `FirmwareVersion`, `AcquisitionMode` | ADQ-ARD-0001 (Arduino, fw 1.2.0, POLL) |
| Grupo de sensores | `MeasurementSession.SensorGroupId` o "No informado" | GRP-A |
| Puerto COM | `MeasurementSession.ComPort` | COM3 |
| Intervalo de muestreo | `SamplingIntervalSeconds` / 60 | 2 min |
| Duración planificada | `PlannedDurationMinutes` y `DurationSource` traducido: Base / Tipo de equipo / Pedido del cliente | 1440 min (Pedido del cliente) |
| Referencia del pedido | `ClientRequestReference` o "—" | OS-2026-0142 |
| Política de pérdida de sensores | `SensorLossCriticalAfterSamples`, `SensorLossFailMinutes` | Crítica en la 3.ª muestra consecutiva; falla a los 30 min |
| Inicio anticipado autorizado | `RestOverrideById` y `RestOverrideReason`, o "No" | No |
| Inicio (primera muestra recibida) | `StartedAt` | 01/10/2026 08:00:00 |
| Fin | `EndedAt` | 02/10/2026 08:00:00 |
| Zona horaria | Desfase de `StartedAt` | UTC-05:00 |
| Duración | `EndedAt − StartedAt` en formato `hh:mm` | 24:00 |
| Estado | `Status` traducido: Completa / Incompleta / Fallida | Completa |
| Motivo de cierre | `CloseReason` traducido: Manual / Duración planificada cumplida / Pérdida de comunicación / Adquisidor o grupo distinto / Pérdida sostenida de datos | Duración planificada cumplida |
| Mezcla de termopares T/K | `HasMixedThermocoupleTypes` | No |
| Muestras programadas / válidas / afectadas / perdidas | `vSessionSampleCoverage` y `CommunicationGap` | 721 / 711 / 10 / 3 |
| Lecturas fuera de límite | Conteo de `IsAboveLimit` = 1 | 12 |
| Lecturas inválidas | Conteo de `SensorStatus` ≠ `OK` | 4 |
| Alertas críticas / advertencias | Conteo por severidad | 2 / 16 |
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
| A | `Muestra` | Número de muestra (1, 2, 3… hasta la última de la duración planificada) |
| B | `Fecha y hora` | `ReadAt` de la muestra. Si es una muestra perdida, la hora programada (`StartedAt + (n − 1) × 2 min`) en cursiva. |
| C | `Estado de la muestra` | `Válida`, `Afectada` (pérdida de sensores sobre el umbral, fondo ámbar `#FFEB9C`) o `Perdida` (hueco de comunicación, fondo amarillo claro `#FFF2CC`) |
| D… | `S1 (T) Superior`, `S2 (T) Centro`, … | Una columna por canal activo: `S{canal} ({tipo}) {ubicación}`. Solo aparecen los canales usados en la sesión. |

**Contenido y formato de cada celda de sensor:**

| Caso | `SensorStatus` / condición | Contenido de la celda | Formato |
|---|---|---|---|
| Lectura válida dentro del límite | `OK`, `IsAboveLimit` = 0 | Número, p. ej. `-18,25` | Normal |
| Lectura **por debajo del límite** (mínimo del rango o banda) | `OK`, `IsBelowLimit` = 1 | Número, p. ej. `17,90` | Fondo azul claro `#DDEBF7`, fuente azul oscura `#1F4E78`, negrita |
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
- Una sesión base de 1 h produce 31 filas. Una de 24 h con 10 canales, 721 filas × 13 columnas; con 27 canales, 30 columnas. Una de 7 días, 5 041 filas.

### Hoja "Alertas"

Una fila por alerta de la sesión, ordenadas por `OccurredAt` y después por `AlertId`.

| Columna | Encabezado | Origen |
|---|---|---|
| A | `Fecha y hora` | `Alert.OccurredAt` |
| B | `Tipo de alerta` | `AlertType` traducido: Fuera de límite, Fuera de límite sostenido, Bajo el límite, Bajo el límite sostenido, Mezcla de termopares, Menos puntos que el mínimo, Tipo no coincidente, Falla de sensor, Pérdida de sensores, Pérdida de sensores persistente, Sesión fallida, Pérdida de comunicación, Adquisidor o grupo distinto, Límite no definido |
| C | `Severidad` | `Severity`: Info, Advertencia, Crítica |
| D | `Sensor` | `S{ChannelNumber} ({ubicación})` o vacío si la alerta es de toda la sesión |
| E | `Muestra` | `Reading.SampleNumber` o la muestra de inicio del episodio, o vacío |
| F | `Valor (°C)` | `ValueC` (numérico) o vacío |
| G | `Límite (°C)` | `LimitC` (numérico) o vacío |
| H | `Mensaje` | `Message` |
| I | `Causa probable` | `SuspectedCause` traducido: Sensor / Equipo, solo en "Fuera de límite sostenido" |
| J | `Confirmada por` | `AppUser.FullName` de `AcknowledgedById` o "Pendiente" |
| K | `Fecha de confirmación` | `AcknowledgedAt` |

Las filas de severidad `Critical` van en rojo intenso, y la hoja se puede filtrar por severidad. Las `AboveLimit` llevan el mismo resaltado rojo claro que en "Lecturas", y las `MixedThermocoupleTypes` y `SensorLoss` el ámbar. Si no hay alertas, la hoja muestra "Sin alertas registradas".

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
