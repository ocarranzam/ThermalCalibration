# HU-17 · Advertir menos puntos que el mínimo del tipo de equipo
# Generado desde docs/specs/functional/03-user-stories.md (v0.9). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero que el sistema me advierta si configuro menos puntos de medición que los que exige la norma del tipo de equipo (9 en general, 8 esquinas y el centro según IEC 60068-3-5 y DKD-R 5-7; 27 en incubadoras de más de 50 L según DIN 12880) para saber que la sesión no cumple el mínimo normativo, y dejar constancia si aun así debo iniciarla.
# Perfil: Técnico · Escenarios de prueba: TD-15, TD-22 y los de 5 canales

@HU-17 @SM @RN-20 @TD-15 @TD-22
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
