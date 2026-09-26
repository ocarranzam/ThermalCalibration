# HU-15 · Priorizar alertas: críticas y advertencias
# Generado desde docs/specs/functional/03-user-stories.md (v0.8). No editar a mano:
# la fuente es la historia de usuario; ante cualquier diferencia, prevalece 03.
# Como técnico de calibración quiero que el sistema solo me interrumpa con las alertas críticas y registre las demás sin avisos para no tener que atender cada variación de temperatura, que es esperable por distintas causas, y actuar rápido cuando algo compromete la sesión.
# Perfil: Técnico, Supervisor · Escenarios de prueba: TD-02, TD-04, TD-16, TD-20, TD-21

@HU-15 @SM @RN-18 @TD-02 @TD-04 @TD-16 @TD-20 @TD-21
Feature: Presentación de alertas según su severidad

  Background:
    Given una sesión en curso que estoy monitoreando

  Scenario Outline: Las advertencias se registran sin interrumpir
    When se genera una alerta "<tipo>"
    Then aumenta el contador de advertencias de la sesión
    And la alerta aparece en la lista de alertas
    But no se muestra un aviso emergente ni suena ninguna alarma

    Examples:
      | tipo                   |
      | AboveLimit             |
      | SensorFault            |
      | TypeMismatch           |
      | SensorLoss             |

  Scenario Outline: Las alertas críticas se notifican y exigen reconocimiento
    When se genera una alerta "<tipo>"
    Then se muestra un aviso destacado en rojo con el mensaje de la alerta
    And se envía la notificación en tiempo real a todas las pantallas que monitorean la sesión
    And el aviso permanece visible hasta que un usuario lo reconoce
    And el reconocimiento registra el usuario y la fecha y hora

    Examples:
      | tipo                 |
      | AboveLimitSustained  |
      | SensorLossPersistent |
      | SessionFailed        |
      | DeviceMismatch       |
      | CommunicationLost    |

  Scenario: Filtrar la lista de alertas por severidad
    When filtro la lista de alertas por "Críticas"
    Then veo solo las alertas con severidad "Critical"
    And cada una indica si está reconocida y por quién

  Scenario: Críticas pendientes al cerrar
    Given la sesión tiene 1 alerta crítica sin reconocer
    When la sesión se cierra
    Then el resumen de cierre muestra "1 alerta crítica sin reconocer"
    And el Excel la muestra como "Pendiente" en la columna de confirmación
