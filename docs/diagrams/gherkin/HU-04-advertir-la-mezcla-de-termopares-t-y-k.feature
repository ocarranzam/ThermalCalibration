# HU-04 · Advertir la mezcla de termopares T y K
# Generado desde docs/specs/functional/03-user-stories.md (v0.9). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero que el sistema me advierta si mezclo termopares T y K para evitar una mala práctica o, si es inevitable, dejar constancia de que la acepté.
# Perfil: Técnico · Escenarios de prueba: TD-10

@HU-04 @SM @RN-05 @TD-10
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
