# HU-08 · Alertar lecturas fuera de límite
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como supervisor quiero que cada lectura mayor que el límite aplicado genere una alerta asociada al sensor y a la marca de tiempo para revisar si fue una variación mínima o una falla del sensor.
# Perfil: Sistema · Escenarios de prueba: TD-02, TD-15, TD-20, TD-21

@HU-08 @SM @RN-07 @RN-10 @RN-13 @RN-18 @RN-19 @TD-02 @TD-15 @TD-20 @TD-21
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
