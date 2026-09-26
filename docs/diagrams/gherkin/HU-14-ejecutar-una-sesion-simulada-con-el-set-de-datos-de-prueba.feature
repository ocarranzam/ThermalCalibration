# HU-14 · Ejecutar una sesión simulada con el set de datos de prueba
# Generado desde docs/specs/functional/03-user-stories.md (v0.9). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico o administrador quiero ejecutar una sesión completa con el adquisidor simulado y un escenario del set de datos de prueba para probar la captura, las alertas, los huecos, el cierre y la exportación sin tener sensores ni adquisidor.
# Perfil: Técnico, Admin · Escenarios de prueba: Todos

@HU-14 @AS @RN-16 @TD-01 @TD-04 @TD-05 @TD-06 @TD-07 @TD-10 @TD-13 @TD-16 @TD-17 @TD-18 @TD-19 @TD-20 @TD-21 @TD-22
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
