# HU-06 · Iniciar al llegar datos y capturar cada 2 minutos
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como laboratorio quiero que la sesión empiece con la primera muestra que llega del adquisidor, y que desde ahí se capture una lectura por canal cada 2 minutos, almacenada con su marca de tiempo y su trama original, para disponer de datos crudos confiables y auditables aunque el adquisidor ya estuviera midiendo antes de conectarse.
# Perfil: Sistema · Escenarios de prueba: TD-01, TD-11, TD-12

@HU-06 @AS @RN-02 @TD-01 @TD-11 @TD-12
Feature: Inicio por llegada de datos y captura periódica

  Background:
    Given una sesión configurada con los canales 1 a 5 activos, todos tipo "T"
    And el límite aplicado será -5,00 °C

  Scenario: La sesión inicia con la primera muestra recibida (modo POLL)
    When pulso "Iniciar captura" a las 07:59:40
    And el adquisidor responde a la primera solicitud con tramas válidas a las 08:00:00
    Then la sesión pasa a "Running" con inicio a las 08:00:00
    And esas lecturas se almacenan como muestra 1

  Scenario: Esperar datos sin consumir números de muestra
    When pulso "Iniciar captura" y el adquisidor no devuelve tramas válidas
    Then la sesión se muestra como "Esperando datos" y sigue en estado "Configured"
    And el sistema reintenta cada 10 segundos sin crear lecturas
    And si pasan 5 minutos sin datos, me avisa y me permite seguir esperando o cancelar

  Scenario: Descartar datos generados antes de iniciar (modo STREAM)
    Given el adquisidor en modo "STREAM" transmitía bloques antes de que la PC abriera el puerto
    When la PC abre el puerto y recibe una línea incompleta y dos bloques completos
    And pulso "Iniciar captura" a las 07:59:00
    And llega el siguiente bloque completo a las 08:00:00
    Then la línea incompleta y los dos bloques previos se descartan sin crear lecturas
    And la sesión inicia a las 08:00:00 con ese bloque como muestra 1

  Scenario: Programar las muestras desde el inicio
    Given la sesión inició a las "2026-10-01 08:00:00 -05:00"
    Then la muestra 2 se programa a las 08:02:00
    And la muestra 31 se programa a las 09:00:00
    And en una sesión base de 1 h la última muestra es la 31, a las 09:00:00
    And en una sesión de 24 h pedida por el cliente la última es la 721, a las 08:00:00 del día siguiente

  Scenario: Almacenar una muestra completa
    Given la sesión inició a las 08:00:00
    When a las 08:30:00 el sistema solicita la muestra 16
    And el adquisidor responde con 5 tramas válidas y el fin de muestra
    Then se almacenan 5 lecturas con número de muestra 16
    And cada lectura guarda la marca de tiempo 08:30:00, la temperatura, el estado "OK", el tipo reportado y la trama original
    And la pantalla de monitoreo muestra los 5 valores y la hora de la última muestra

  Scenario: No acumular deriva de tiempo
    Given la muestra 15 se completó a las 08:28:04 por demoras del adquisidor
    Then la muestra 16 se solicita a las 08:30:00 y no a las 08:30:04

  Scenario: Asignar bloques en modo STREAM por tiempo transcurrido
    Given la sesión inició a las 08:00:00 en modo "STREAM"
    When llega un bloque a las 08:30:02
    Then se almacena como muestra 16 con marca de tiempo 08:30:02

  Scenario: Aceptar 1 o 2 decimales
    When el adquisidor responde "-17.9" en el canal 2 y "-18.25" en el canal 1
    Then se almacenan -17,90 °C y -18,25 °C respectivamente

  Scenario: Descartar una trama tardía de otra muestra
    Given el sistema espera la muestra 17
    When llega una trama válida con número de muestra 16
    Then la trama se descarta sin crear lectura
    And la muestra 16 no se modifica

  Scenario: Una lectura por canal y muestra
    Given el canal 2 ya tiene una lectura válida para la muestra 17
    When llega otra trama válida del canal 2 para la muestra 17 en un reintento
    Then se conserva la primera lectura y se ignora la segunda
