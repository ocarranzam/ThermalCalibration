# HU-12 · Consultar el historial de sesiones
# Generado desde docs/specs/functional/03-user-stories.md (v0.6). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como supervisor quiero consultar las sesiones por empresa, equipo y rango de fechas para revisar el historial de mediciones de cada equipo.
# Perfil: Todos · Escenarios de prueba: —

@HU-12 @SM
Feature: Historial de sesiones

  Background:
    Given que he iniciado sesión con el rol "Supervisor"
    And existen sesiones de varias empresas y equipos

  Scenario: Filtrar por empresa
    When filtro por la empresa con RUC "20100070970"
    Then veo solo las sesiones de equipos de esa empresa, ordenadas por fecha de inicio descendente

  Scenario: Filtrar por equipo y rango de fechas
    When filtro por el equipo "SN-88231" entre "2026-10-01" y "2026-10-31"
    Then veo las sesiones de ese equipo cuya fecha de inicio está dentro del rango, ambos días incluidos

  Scenario: Columnas del listado
    Then cada fila muestra: número de sesión, empresa, equipo (tipo, marca, modelo, serie), técnico, fecha y hora de inicio y fin, duración, estado, número de canales, muestras válidas y afectadas, indicador de mezcla T/K, número de alertas y número de huecos

  Scenario: Las simulaciones no aparecen por defecto
    Given existen sesiones simuladas
    When consulto el historial sin cambiar los filtros
    Then no veo las sesiones simuladas
    When marco "Incluir simulaciones"
    Then las veo con la etiqueta "SIMULACIÓN" y el código de escenario

  Scenario: Abrir el detalle de una sesión
    When abro la sesión 120
    Then veo su configuración, sus lecturas, alertas, huecos y muestras afectadas en modo de solo lectura
    And puedo exportarla a Excel si está "Completed", "Incomplete" o "Invalid"

  Scenario: Rango de fechas inválido
    When filtro con la fecha inicial "2026-10-31" y la final "2026-10-01"
    Then el sistema muestra "La fecha inicial debe ser anterior o igual a la final"

  Scenario: Sin resultados
    When filtro por un equipo sin sesiones
    Then el sistema muestra "No hay sesiones para los filtros seleccionados"

  Scenario: Un técnico también consulta el historial
    Given que he iniciado sesión con el rol "Technician"
    When consulto el historial
    Then veo las sesiones de todos los técnicos en modo de solo lectura
