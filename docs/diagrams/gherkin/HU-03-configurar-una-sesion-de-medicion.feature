# HU-03 · Configurar una sesión de medición
# Generado desde docs/specs/functional/03-user-stories.md (v0.9). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero configurar la sesión eligiendo el equipo, el puerto COM, el adquisidor detectado y de 1 a 27 canales con su tipo de termopar y su ubicación para iniciar una captura trazable.
# Perfil: Técnico · Escenarios de prueba: TD-01, TD-11

@HU-03 @SM @AS @RN-01 @RN-06 @TD-01 @TD-11
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
