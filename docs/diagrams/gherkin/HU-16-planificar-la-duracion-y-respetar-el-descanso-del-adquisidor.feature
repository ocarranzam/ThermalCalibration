# HU-16 · Planificar la duración y respetar el descanso del adquisidor
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero que la sesión se planifique con la duración base de 1 h, o con la que exija el tipo de equipo o pida el cliente, y que el adquisidor descanse entre sesiones, para medir cada equipo el tiempo necesario y no saturar el equipo de medición mientras paso a calibrar el siguiente.
# Perfil: Técnico, Supervisor · Escenarios de prueba: TD-10, TD-12, TD-17, TD-19

@HU-16 @SM @RN-04 @RN-17 @TD-10 @TD-12 @TD-17 @TD-19
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
