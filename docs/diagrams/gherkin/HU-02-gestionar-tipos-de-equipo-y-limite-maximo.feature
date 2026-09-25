# HU-02 · Gestionar tipos de equipo y límite máximo
# Generado desde docs/specs/functional/03-user-stories.md (v0.6). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como administrador quiero mantener el catálogo de tipos de equipo con su temperatura máxima admisible, o dejarla pendiente, para que las sesiones evalúen el límite correcto sin alterar las mediciones ya registradas.
# Perfil: Admin · Escenarios de prueba: TD-14, TD-19

@HU-02 @SM @RN-04 @RN-06 @RN-07 @RN-08 @TD-14 @TD-19
Feature: Catálogo de tipos de equipo con límite máximo

  Background:
    Given que he iniciado sesión con el rol "Admin"

  Scenario: Registrar un tipo de equipo con límite definido
    When registro el tipo de equipo "Ultracongeladora" con límite máximo "-60,0" °C
    Then el tipo queda activo con límite máximo -60,00 °C
    And su duración mínima de sesión es 60 minutos

  Scenario: Exigir una duración mínima mayor para un tipo de equipo
    When fijo la duración mínima de sesión de "Incubadora" en 120 minutos
    Then las sesiones nuevas de incubadoras se planifican con al menos 120 minutos
    And las sesiones ya registradas conservan su duración planificada

  Scenario: Rechazar una duración mínima menor que la base
    When intento fijar la duración mínima de sesión de "Congeladora" en 45 minutos
    Then el sistema rechaza el valor con el mensaje "La duración mínima no puede ser menor que 60 minutos"

  Scenario: Registrar un tipo de equipo con límite pendiente
    When registro el tipo de equipo "Cámara de vacunas" sin límite máximo
    Then el tipo queda activo con el límite en estado "Pendiente"
    And el catálogo lo muestra con la etiqueta "Límite pendiente de definir"

  Scenario: Rechazar un nombre de tipo duplicado
    Given existe el tipo de equipo "Congeladora"
    When intento registrar el tipo de equipo "Congeladora"
    Then el sistema rechaza el registro con el mensaje "Ya existe el tipo de equipo Congeladora"

  Scenario: Rechazar un límite con formato inválido
    When intento registrar el límite máximo "-5,123" para "Congeladora"
    Then el sistema rechaza el valor con el mensaje "El límite admite como máximo 2 decimales"

  Scenario: Editar el límite sin afectar sesiones registradas
    Given el tipo "Congeladora" tiene límite máximo -5,00 °C
    And la sesión 101 de una congeladora se inició con ese límite
    When cambio el límite máximo de "Congeladora" a -8,00 °C
    Then las sesiones que se inicien desde ahora aplican -8,00 °C
    And la sesión 101 conserva el límite aplicado -5,00 °C
    And las lecturas y alertas de la sesión 101 no cambian

  Scenario: Editar el límite de una sesión en curso no la afecta
    Given la sesión 102 de una congeladora está en curso con límite aplicado -5,00 °C
    When cambio el límite máximo de "Congeladora" a -8,00 °C
    Then la sesión 102 sigue evaluando las lecturas contra -5,00 °C

  Scenario: Definir un límite que estaba pendiente
    Given el tipo "Refrigeradora" tiene el límite pendiente
    And la sesión 103 de una refrigeradora se capturó sin evaluar límite
    When defino el límite máximo de "Refrigeradora" en 8,00 °C
    Then la sesión 103 sigue sin límite aplicado y sin alertas de límite

  Scenario: Cambiar el umbral de pérdida de sensores
    Given el umbral de pérdida de sensores es 60 %
    And la sesión 104 se inició con ese umbral
    When cambio el umbral a 50 %
    Then las sesiones que se inicien desde ahora aplican 50 %
    And la sesión 104 conserva el umbral 60 % y sus muestras afectadas no cambian

  Scenario Outline: Rechazar un umbral fuera de rango
    When intento fijar el umbral de pérdida de sensores en <valor> %
    Then el sistema rechaza el valor con el mensaje "El umbral debe ser mayor que 0 y menor que 100"

    Examples:
      | valor |
      | 0     |
      | 100   |

  Scenario: Un técnico no puede editar límites
    Given que he iniciado sesión con el rol "Technician"
    When intento editar el límite máximo de "Congeladora"
    Then el sistema me deniega la acción por falta de permisos

  Scenario: Desactivar un tipo con equipos asociados
    Given el tipo "Conservadora" tiene equipos registrados
    When intento eliminar el tipo "Conservadora"
    Then el sistema no lo elimina y me ofrece desactivarlo
    And un tipo desactivado no se ofrece al registrar equipos nuevos
