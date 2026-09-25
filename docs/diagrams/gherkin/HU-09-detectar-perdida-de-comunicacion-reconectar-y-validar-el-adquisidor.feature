# HU-09 · Detectar pérdida de comunicación, reconectar y validar el adquisidor
# Generado desde docs/specs/functional/03-user-stories.md (v0.6). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como laboratorio quiero que el sistema detecte la pérdida de comunicación con el puerto COM, intente reconectar, registre los huecos de datos y verifique que reconecta el mismo adquisidor con el mismo grupo de sensores para no perder la sesión por una desconexión momentánea y no mezclar datos de otro equipo de medición.
# Perfil: Sistema · Escenarios de prueba: TD-06, TD-07, TD-08

@HU-09 @AS @RN-12 @RN-13 @RN-14 @RN-15 @TD-06 @TD-07 @TD-08
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
