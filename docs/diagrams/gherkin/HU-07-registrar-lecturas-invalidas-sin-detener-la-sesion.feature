# HU-07 · Registrar lecturas inválidas sin detener la sesión
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como laboratorio quiero que las lecturas de un sensor desconectado, las tramas corruptas y los tipos no coincidentes queden registrados y marcados sin detener la sesión para que el resto de los sensores siga midiendo y quede evidencia del problema.
# Perfil: Sistema · Escenarios de prueba: TD-09, TD-15

@HU-07 @AS @RN-11 @TD-09 @TD-15
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
