# HU-11 · Exportar una sesión a Excel
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico o supervisor quiero exportar una sesión cerrada a un archivo .xlsx con formato estándar para revisarla y entregarla sin transcripciones manuales.
# Perfil: Técnico, Supervisor · Escenarios de prueba: Todos

@HU-11 @EX @TD-04
Feature: Exportación de la sesión a Excel

  Background:
    Given que he iniciado sesión con el rol "Technician"

  Scenario: Exportar una sesión completa
    Given la sesión 120 está "Completed"
    When pulso "Exportar a Excel" y elijo la carpeta de destino
    Then se genera el archivo "20100070970_SN-88231_20261001-0800_S120.xlsx"
    And contiene las hojas "Resumen", "Lecturas", "Alertas" y "Comunicación" en ese orden
    And se registra la exportación con mi usuario, la fecha y hora, y el nombre del archivo

  Scenario: Exportar una sesión incompleta
    Given la sesión 121 está "Incomplete" con 26 muestras válidas
    When la exporto a Excel
    Then la hoja "Resumen" muestra "SESIÓN INCOMPLETA: 26 muestras válidas de las 31 necesarias" resaltado

  Scenario Outline: Exportar una sesión fallida
    Given la sesión 125 está "Invalid" con motivo "<motivo>"
    When la exporto a Excel
    Then la hoja "Resumen" muestra "<aviso>" en rojo
    And el nombre del archivo termina en "_FALLIDA.xlsx"

    Examples:
      | motivo         | aviso                                                                                      |
      | DeviceMismatch | SESIÓN FALLIDA: al reconectar respondió otro adquisidor u otro grupo de sensores           |
      | DataLoss       | SESIÓN FALLIDA: 30 min consecutivos sin datos suficientes (muestras 20 a 35)               |

  Scenario: Aviso de mezcla de termopares en el Excel
    Given la sesión 122 mezcla tipos T y K
    When la exporto a Excel
    Then la hoja "Resumen" muestra en la parte superior el aviso "ATENCIÓN: esta sesión mezcla termopares tipo T y tipo K. No es una buena práctica."
    And el aviso incluye quién confirmó la advertencia y cuándo

  Scenario: Aviso de datos simulados
    Given la sesión 130 es una simulación del escenario "TD-04"
    When la exporto a Excel
    Then todas las hojas muestran en la primera fila "DATOS SIMULADOS – NO VÁLIDOS PARA CALIBRACIÓN (escenario TD-04)"
    And el nombre del archivo empieza con "SIM_"

  Scenario: Resaltar lecturas fuera de límite e inválidas
    Given la sesión 120 tiene una lectura de -4,90 °C en S5 muestra 32 con límite -5,00 °C
    And una lectura "OpenCircuit" en S4 muestra 20
    When la exporto a Excel
    Then en la hoja "Lecturas" la celda S5 de la muestra 32 contiene el número -4,90 con fondo rojo
    And la celda S4 de la muestra 20 contiene el texto "ABIERTO" con fondo gris

  Scenario: Incluir las muestras perdidas y afectadas en la hoja de lecturas
    Given la sesión 120 perdió las muestras 64 a 66 por un hueco de comunicación
    And las muestras 30 a 39 quedaron afectadas por pérdida de sensores
    When la exporto a Excel
    Then la hoja "Lecturas" contiene las filas 64, 65 y 66 con su hora programada, estado "Perdida" y "SIN DATOS" en cada sensor
    And las filas 30 a 39 tienen estado "Afectada"
    And la hoja "Comunicación" lista el hueco con 3 muestras perdidas y el episodio de pérdida de sensores de 10 muestras

  Scenario: Sesión sin límite definido
    Given la sesión 123 se capturó sin límite aplicado
    When la exporto a Excel
    Then la hoja "Resumen" muestra "Límite máximo aplicado: No definido (no se evaluó)"
    And ninguna celda de "Lecturas" aparece resaltada como fuera de límite

  Scenario: Exportar varias veces la misma sesión
    Given la sesión 120 ya se exportó una vez
    When la exporto de nuevo
    Then se genera un archivo con el mismo contenido de datos
    And se registra una segunda exportación
    And si el archivo ya existe en la carpeta, el sistema pregunta si desea reemplazarlo o agregar un sufijo

  Scenario Outline: No se exportan sesiones abiertas o canceladas
    Given la sesión 124 está "<estado>"
    When intento exportarla a Excel
    Then el sistema muestra "<mensaje>"

    Examples:
      | estado     | mensaje                                                 |
      | Configured | La sesión aún no se ha iniciado                          |
      | Running    | Finalice la sesión antes de exportarla                   |
      | Cancelled  | Las sesiones canceladas no se exportan                   |

  Scenario: Error al escribir el archivo
    Given la carpeta de destino no tiene permisos de escritura
    When exporto la sesión 120
    Then el sistema muestra "No se pudo guardar el archivo en la carpeta seleccionada"
    And no se registra la exportación
