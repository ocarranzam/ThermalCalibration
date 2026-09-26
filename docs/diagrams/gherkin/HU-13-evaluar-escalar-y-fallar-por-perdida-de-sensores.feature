# HU-13 · Evaluar, escalar y fallar por pérdida de sensores
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como supervisor quiero que el sistema marque las muestras en las que más del 60 % de los sensores dejaron de enviar datos válidos, que me avise de forma crítica si la pérdida no se restablece en la 3.ª medición, y que dé la sesión por fallida si dura 30 min, para actuar a tiempo sin invalidar la sesión por fallas aisladas.
# Perfil: Sistema · Escenarios de prueba: TD-03, TD-04, TD-05, TD-15, TD-16, TD-18

@HU-13 @SM @RN-14 @RN-15 @TD-03 @TD-04 @TD-05 @TD-15 @TD-16 @TD-18
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
