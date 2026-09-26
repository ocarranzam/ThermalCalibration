# HU-10 · Cerrar la sesión (duración planificada)
# Generado desde docs/specs/functional/03-user-stories.md (v0.9). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero que la sesión se cierre sola al cumplir su duración planificada, y poder cerrarla antes si hace falta, para que el estado final refleje si se cumplieron la duración planificada y la cantidad mínima de datos válidos.
# Perfil: Técnico, Sistema · Escenarios de prueba: TD-05, TD-10, TD-12, TD-13

@HU-10 @SM @RN-03 @RN-04 @RN-14 @RN-15 @TD-05 @TD-10 @TD-12 @TD-13
Feature: Cierre de la sesión de medición

  Background:
    Given que he iniciado sesión con el rol "Technician"
    And una sesión base de 1 h en curso, iniciada a las 08:00:00

  Scenario: Cierre automático de la sesión base con todas las muestras válidas
    Given las muestras 1 a 31 son válidas
    When se almacena la muestra 31 a las 09:00:00
    Then el sistema envía "STOP" al adquisidor
    And la sesión queda "Completed" con fin 09:00:00 y motivo "PlannedDuration"
    And no se programan más muestras

  Scenario: Cierre automático con muestras afectadas
    Given 10 de las 31 muestras están afectadas por pérdida de sensores
    When se almacena la muestra 31 a las 09:00:00
    Then la sesión queda "Incomplete" con motivo "PlannedDuration"
    And el resumen indica "21 muestras válidas de las 31 necesarias"

  Scenario: Extender la sesión para recuperar muestras válidas
    Given a las 08:50:00 la sesión tiene 5 muestras afectadas
    When extiendo la duración planificada a 75 minutos indicando el motivo "Recuperar datos tras falla de sensor"
    Then la sesión se cierra automáticamente a las 09:15:00
    And queda "Completed" si al cierre tiene al menos 31 muestras válidas

  Scenario: Cerrar antes de la duración planificada
    When a las 08:45:00 pulso "Finalizar sesión"
    Then el sistema advierte "La sesión dura 45 min de los 60 planificados. Quedará como INCOMPLETA."
    And si confirmo, la sesión queda "Incomplete" con fin 08:45:00 y motivo "Manual"
    And si no confirmo, la sesión sigue en curso

  Scenario: La base rechaza una sesión completa que no alcanzó su duración planificada
    Given una sesión planificada de 120 minutos
    When cualquier proceso intenta guardarla como "Completed" con 90 minutos de duración
    Then la base de datos rechaza el cambio

  Scenario: Cierre automático de una sesión larga pedida por el cliente
    Given una sesión planificada de 24 h a pedido del cliente, con la referencia "OS-2026-0142"
    When se almacena la muestra 721 a las 08:00:00 del día siguiente
    Then la sesión queda "Completed" con motivo "PlannedDuration"

  Scenario: Aviso previo al cierre automático
    When faltan 10 minutos para cumplir la duración planificada
    Then la pantalla muestra "La sesión se cerrará automáticamente a las 09:00:00"

  Scenario: Cancelar una sesión
    When pulso "Cancelar sesión", indico el motivo "Termopar mal instalado" y confirmo
    Then la sesión queda "Cancelled" con motivo "Cancelled" y la nota indicada
    And sus lecturas se conservan pero la sesión no se puede exportar

  Scenario: Cerrar la aplicación con una sesión en curso
    When intento cerrar la aplicación
    Then el sistema advierte que hay una sesión en curso y pide finalizarla antes
