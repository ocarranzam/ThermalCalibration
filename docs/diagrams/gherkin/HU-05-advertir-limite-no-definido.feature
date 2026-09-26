# HU-05 · Advertir límite no definido
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero saber que el tipo de equipo no tiene límite máximo definido para entender que la sesión se capturará sin evaluar ese límite.
# Perfil: Técnico · Escenarios de prueba: TD-14

@HU-05 @SM @RN-09 @TD-14
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
